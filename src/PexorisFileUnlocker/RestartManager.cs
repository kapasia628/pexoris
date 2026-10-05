using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Pexoris.Unlocker
{
    public class LockedProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string WindowTitle { get; set; }
        public string ExecutablePath { get; set; }
        public string AppType { get; set; }
        public bool IsCritical { get; set; }
    }

    public static class RestartManagerHelper
    {
        private const int CCH_RM_MAX_APP_NAME = 255;
        private const int CCH_RM_MAX_SVC_NAME = 63;
        private const int ERROR_MORE_DATA = 234;

        [StructLayout(LayoutKind.Sequential)]
        private struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;
            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        private enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        private static extern int RmStartSession(out uint pSessionHandle, int dwFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        private static extern int RmRegisterResources(
            uint pSessionHandle,
            uint nFiles,
            string[] rgsFilenames,
            uint nApplications,
            [In] RM_UNIQUE_PROCESS[] rgApplications,
            uint nServices,
            string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        private static extern int RmGetList(
            uint pSessionHandle,
            out uint pnProcInfoNeeded,
            ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            out uint lpdwRebootReasons);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmEndSession(uint pSessionHandle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, int dwFlags);

        public const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;

        /// <summary>
        /// Finds all processes locking the specified file or directory.
        /// </summary>
        public static List<LockedProcessInfo> FindLockingProcesses(string path)
        {
            List<LockedProcessInfo> results = new List<LockedProcessInfo>();
            if (string.IsNullOrEmpty(path) || (!File.Exists(path) && !Directory.Exists(path)))
            {
                return results;
            }

            uint handle;
            string key = Guid.NewGuid().ToString();
            int res = RmStartSession(out handle, 0, key);
            if (res != 0) return results;

            try
            {
                string[] resources = new string[] { Path.GetFullPath(path) };
                res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);
                if (res != 0) return results;

                uint procInfoNeeded = 0;
                uint count = 0;
                uint rebootReasons = 0;

                // First call gets count
                res = RmGetList(handle, out procInfoNeeded, ref count, null, out rebootReasons);
                if (res == ERROR_MORE_DATA || procInfoNeeded > 0)
                {
                    RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[procInfoNeeded];
                    count = procInfoNeeded;
                    res = RmGetList(handle, out procInfoNeeded, ref count, processInfo, out rebootReasons);

                    if (res == 0)
                    {
                        HashSet<int> seenPids = new HashSet<int>();
                        for (int i = 0; i < count; i++)
                        {
                            int pid = processInfo[i].Process.dwProcessId;
                            if (pid <= 4 || seenPids.Contains(pid)) continue;
                            seenPids.Add(pid);

                            LockedProcessInfo item = new LockedProcessInfo();
                            item.ProcessId = pid;
                            item.ProcessName = (processInfo[i].strAppName ?? "").Trim('\0', ' ', '\t', '\r', '\n');
                            item.AppType = processInfo[i].ApplicationType.ToString().Replace("Rm", "");
                            item.IsCritical = (processInfo[i].ApplicationType == RM_APP_TYPE.RmCritical);

                            try
                            {
                                Process p = Process.GetProcessById(pid);
                                if (string.IsNullOrEmpty(item.ProcessName))
                                    item.ProcessName = p.ProcessName + ".exe";

                                item.WindowTitle = p.MainWindowTitle;
                                try { item.ExecutablePath = p.MainModule.FileName; }
                                catch { item.ExecutablePath = "Access Denied / System"; }
                            }
                            catch
                            {
                                if (string.IsNullOrEmpty(item.ProcessName))
                                    item.ProcessName = "PID: " + pid;
                                item.WindowTitle = "Terminated / Unknown";
                                item.ExecutablePath = "Unknown";
                            }

                            results.Add(item);
                        }
                    }
                }
            }
            finally
            {
                RmEndSession(handle);
            }

            return results;
        }

        /// <summary>
        /// Checks if a file is currently locked by trying to open it exclusively.
        /// </summary>
        public static bool IsFileLocked(string filePath)
        {
            if (!File.Exists(filePath)) return false;

            FileStream stream = null;
            try
            {
                stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return false;
        }

        /// <summary>
        /// Kills a process by PID with force.
        /// </summary>
        public static bool TerminateProcessById(int pid)
        {
            try
            {
                Process p = Process.GetProcessById(pid);
                p.Kill();
                p.WaitForExit(1500);
                return true;
            }
            catch
            {
                // Fallback to taskkill /F /PID
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/F /PID " + pid);
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process proc = Process.Start(psi);
                    proc.WaitForExit(1500);
                    return proc.ExitCode == 0;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// Schedules a file or directory for deletion on next Windows reboot.
        /// </summary>
        public static bool ScheduleDeleteOnReboot(string path)
        {
            try
            {
                return MoveFileEx(path, null, MOVEFILE_DELAY_UNTIL_REBOOT);
            }
            catch
            {
                return false;
            }
        }
    }
}
