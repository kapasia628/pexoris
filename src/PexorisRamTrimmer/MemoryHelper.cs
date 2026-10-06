using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PexorisRamTrimmer
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PERFORMANCE_INFORMATION
    {
        public uint cb;
        public IntPtr CommitTotal;
        public IntPtr CommitLimit;
        public IntPtr CommitPeak;
        public IntPtr PhysicalTotal;
        public IntPtr PhysicalAvailable;
        public IntPtr SystemCache;
        public IntPtr KernelTotal;
        public IntPtr KernelPaged;
        public IntPtr KernelNonpaged;
        public IntPtr PageSize;
        public uint HandleCount;
        public uint ProcessCount;
        public uint ThreadCount;
    }

    public class MemoryStats
    {
        public ulong TotalPhysicalBytes { get; set; }
        public ulong AvailablePhysicalBytes { get; set; }
        public ulong UsedPhysicalBytes { get; set; }
        public uint MemoryLoadPercent { get; set; }

        public ulong SystemCacheBytes { get; set; }
        public ulong CommitTotalBytes { get; set; }
        public ulong CommitLimitBytes { get; set; }
        public ulong KernelPagedBytes { get; set; }
        public ulong KernelNonPagedBytes { get; set; }
        public uint ProcessCount { get; set; }
    }

    public class ProcessMemoryItem
    {
        public int Pid { get; set; }
        public string Name { get; set; }
        public long WorkingSetBytes { get; set; }
        public long PrivateBytes { get; set; }
        public double MemoryPercent { get; set; }
        public string Priority { get; set; }
    }

    public static class MemoryHelper
    {
        [DllImport("ntdll.dll")]
        private static extern int NtSetSystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength);

        [DllImport("psapi.dll")]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo(out PERFORMANCE_INFORMATION pPerformanceInformation, uint cb);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref long lpLuid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public long Luid;
            public int Attributes;
        }

        private const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        private const int TOKEN_QUERY = 0x00000008;
        private const int SE_PRIVILEGE_ENABLED = 0x00000002;
        private const int SystemMemoryListInformation = 80;

        private enum MemoryListCommand
        {
            MemoryEmptyWorkingSets = 2,
            MemoryFlushModifiedList = 3,
            MemoryPurgeStandbyList = 4,
            MemoryPurgeLowPriorityStandbyList = 5
        }

        private static bool _privilegesAcquired = false;

        public static void EnableRequiredPrivileges()
        {
            if (_privilegesAcquired) return;
            try
            {
                SetPrivilege("SeProfileSingleProcessPrivilege");
                SetPrivilege("SeIncreaseQuotaPrivilege");
                _privilegesAcquired = true;
            }
            catch { }
        }

        private static bool SetPrivilege(string privilegeName)
        {
            IntPtr token = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref token))
                    return false;

                long luid = 0;
                if (!LookupPrivilegeValue(null, privilegeName, ref luid))
                    return false;

                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                };

                return AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (token != IntPtr.Zero) CloseHandle(token);
            }
        }

        private static bool ExecuteMemoryListCommand(MemoryListCommand command)
        {
            EnableRequiredPrivileges();
            IntPtr pCommand = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                Marshal.WriteInt32(pCommand, (int)command);
                int status = NtSetSystemInformation(SystemMemoryListInformation, pCommand, sizeof(int));
                return status >= 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(pCommand);
            }
        }

        public static bool PurgeStandbyList()
        {
            return ExecuteMemoryListCommand(MemoryListCommand.MemoryPurgeStandbyList);
        }

        public static bool FlushModifiedList()
        {
            return ExecuteMemoryListCommand(MemoryListCommand.MemoryFlushModifiedList);
        }

        public static bool EmptySystemWorkingSets()
        {
            return ExecuteMemoryListCommand(MemoryListCommand.MemoryEmptyWorkingSets);
        }

        public static int EmptyProcessWorkingSets()
        {
            int count = 0;
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                try
                {
                    // Skip System, Idle and own process
                    if (p.Id == 0 || p.Id == 4 || p.Id == Process.GetCurrentProcess().Id)
                        continue;

                    if (EmptyWorkingSet(p.Handle))
                    {
                        count++;
                    }
                }
                catch { }
                finally
                {
                    p.Dispose();
                }
            }
            return count;
        }

        public static bool TrimProcess(int pid)
        {
            try
            {
                using (Process p = Process.GetProcessById(pid))
                {
                    return EmptyWorkingSet(p.Handle);
                }
            }
            catch
            {
                return false;
            }
        }

        public static MemoryStats GetSystemMemoryStats()
        {
            MemoryStats stats = new MemoryStats();
            try
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    stats.TotalPhysicalBytes = memStatus.ullTotalPhys;
                    stats.AvailablePhysicalBytes = memStatus.ullAvailPhys;
                    stats.UsedPhysicalBytes = memStatus.ullTotalPhys - memStatus.ullAvailPhys;
                    stats.MemoryLoadPercent = memStatus.dwMemoryLoad;
                }

                PERFORMANCE_INFORMATION perfInfo = new PERFORMANCE_INFORMATION();
                perfInfo.cb = (uint)Marshal.SizeOf(typeof(PERFORMANCE_INFORMATION));
                if (GetPerformanceInfo(out perfInfo, perfInfo.cb))
                {
                    ulong pageSize = (ulong)perfInfo.PageSize.ToInt64();
                    stats.SystemCacheBytes = (ulong)perfInfo.SystemCache.ToInt64() * pageSize;
                    stats.CommitTotalBytes = (ulong)perfInfo.CommitTotal.ToInt64() * pageSize;
                    stats.CommitLimitBytes = (ulong)perfInfo.CommitLimit.ToInt64() * pageSize;
                    stats.KernelPagedBytes = (ulong)perfInfo.KernelPaged.ToInt64() * pageSize;
                    stats.KernelNonPagedBytes = (ulong)perfInfo.KernelNonpaged.ToInt64() * pageSize;
                    stats.ProcessCount = perfInfo.ProcessCount;
                }
            }
            catch { }
            return stats;
        }

        public static List<ProcessMemoryItem> GetTopMemoryProcesses(ulong totalPhysBytes)
        {
            List<ProcessMemoryItem> list = new List<ProcessMemoryItem>();
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process p in processes)
                {
                    try
                    {
                        if (p.Id == 0) continue; // Skip idle

                        long ws = p.WorkingSet64;
                        long priv = p.PrivateMemorySize64;
                        double pct = totalPhysBytes > 0 ? ((double)ws / totalPhysBytes) * 100.0 : 0;

                        string pri = "Normal";
                        try { pri = p.PriorityClass.ToString(); } catch { }

                        list.Add(new ProcessMemoryItem
                        {
                            Pid = p.Id,
                            Name = p.ProcessName,
                            WorkingSetBytes = ws,
                            PrivateBytes = priv,
                            MemoryPercent = Math.Round(pct, 1),
                            Priority = pri
                        });
                    }
                    catch { }
                    finally
                    {
                        p.Dispose();
                    }
                }

                list.Sort((a, b) => b.WorkingSetBytes.CompareTo(a.WorkingSetBytes));
            }
            catch { }

            // Take top 60
            if (list.Count > 60)
            {
                list = list.GetRange(0, 60);
            }
            return list;
        }

        public static string FormatBytes(ulong bytes)
        {
            double mb = bytes / (1024.0 * 1024.0);
            if (mb >= 1024.0)
            {
                return string.Format("{0:0.##} GB", mb / 1024.0);
            }
            return string.Format("{0:0.#} MB", mb);
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 0) bytes = 0;
            return FormatBytes((ulong)bytes);
        }
    }
}
