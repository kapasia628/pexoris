using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PexorisShortcutDoctor
{
    public enum ShortcutStatus
    {
        Healthy,
        BrokenTarget,
        AutoRepairable,
        MissingIcon
    }

    public class ShortcutItem
    {
        public string LnkPath { get; set; }
        public string Name { get; set; }
        public string Folder { get; set; }
        public string TargetPath { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public string IconLocation { get; set; }
        public ShortcutStatus Status { get; set; }
        public string SuggestedPath { get; set; }
        public long FileSize { get; set; }

        public ShortcutItem()
        {
            LnkPath = string.Empty;
            Name = string.Empty;
            Folder = string.Empty;
            TargetPath = string.Empty;
            Arguments = string.Empty;
            WorkingDirectory = string.Empty;
            IconLocation = string.Empty;
            Status = ShortcutStatus.Healthy;
            SuggestedPath = string.Empty;
            FileSize = 0;
        }
    }

    public class ScanStats
    {
        public int TotalScanned { get; set; }
        public int BrokenCount { get; set; }
        public int RepairableCount { get; set; }
        public int HealthyCount { get; set; }
    }

    public static class ShortcutEngine
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszProgressTitle;
        }

        private const uint FO_DELETE = 0x0003;
        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_SILENT = 0x0004;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        private static dynamic GetWshShell()
        {
            Type wshType = Type.GetTypeFromProgID("WScript.Shell");
            if (wshType == null)
            {
                throw new InvalidOperationException("WScript.Shell COM interface is not registered on this system.");
            }
            return Activator.CreateInstance(wshType);
        }

        public static List<ShortcutItem> ScanShortcuts(bool scanDesktop, bool scanStartMenu, bool scanQuickLaunch, bool scanAppData, string customFolder)
        {
            List<ShortcutItem> results = new List<ShortcutItem>();
            HashSet<string> scannedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> targetFolders = new List<string>();

            if (scanDesktop)
            {
                AddFolderSafe(targetFolders, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                AddFolderSafe(targetFolders, Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));
            }

            if (scanStartMenu)
            {
                AddFolderSafe(targetFolders, Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));
                AddFolderSafe(targetFolders, Environment.GetFolderPath(Environment.SpecialFolder.Programs));
                AddFolderSafe(targetFolders, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu));
                AddFolderSafe(targetFolders, Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms));
            }

            if (scanQuickLaunch)
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string ql = Path.Combine(appdata, @"Microsoft\Internet Explorer\Quick Launch");
                AddFolderSafe(targetFolders, ql);
                string userPinned = Path.Combine(appdata, @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
                AddFolderSafe(targetFolders, userPinned);
            }

            if (scanAppData)
            {
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appPrograms = Path.Combine(localApp, @"Programs");
                AddFolderSafe(targetFolders, appPrograms);
            }

            if (!string.IsNullOrEmpty(customFolder) && Directory.Exists(customFolder))
            {
                AddFolderSafe(targetFolders, customFolder);
            }

            dynamic wsh = GetWshShell();

            foreach (string rootDir in targetFolders)
            {
                if (!Directory.Exists(rootDir) || scannedFolders.Contains(rootDir)) continue;
                scannedFolders.Add(rootDir);

                List<string> lnkFiles = new List<string>();
                try
                {
                    GetLnkFilesRecursive(rootDir, lnkFiles, 0, 5);
                }
                catch { }

                foreach (string lnkPath in lnkFiles)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(lnkPath);
                        dynamic sc = wsh.CreateShortcut(lnkPath);

                        string target = sc.TargetPath;
                        string args = sc.Arguments;
                        string workDir = sc.WorkingDirectory;
                        string iconLoc = sc.IconLocation;

                        ShortcutItem item = new ShortcutItem();
                        item.LnkPath = lnkPath;
                        item.Name = Path.GetFileName(lnkPath);
                        item.Folder = Path.GetDirectoryName(lnkPath);
                        item.TargetPath = target ?? string.Empty;
                        item.Arguments = args ?? string.Empty;
                        item.WorkingDirectory = workDir ?? string.Empty;
                        item.IconLocation = iconLoc ?? string.Empty;
                        item.FileSize = fi.Exists ? fi.Length : 0;

                        AssessShortcutHealth(item);
                        results.Add(item);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error reading shortcut " + lnkPath + ": " + ex.Message);
                    }
                }
            }

            return results;
        }

        private static void AssessShortcutHealth(ShortcutItem item)
        {
            string target = item.TargetPath.Trim();

            // Web URLs, UWP Apps or Shell namespace GUIDs
            if (string.IsNullOrEmpty(target) ||
                target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith("ms-", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith("::{", StringComparison.OrdinalIgnoreCase))
            {
                item.Status = ShortcutStatus.Healthy;
                return;
            }

            // Expand environment variables
            string expanded = Environment.ExpandEnvironmentVariables(target);

            if (File.Exists(expanded) || Directory.Exists(expanded))
            {
                item.Status = ShortcutStatus.Healthy;
                return;
            }

            // Target does not exist - attempt smart resolution
            item.Status = ShortcutStatus.BrokenTarget;
            string suggested = FindAlternateExecutable(expanded);

            if (!string.IsNullOrEmpty(suggested))
            {
                item.Status = ShortcutStatus.AutoRepairable;
                item.SuggestedPath = suggested;
            }
        }

        private static string FindAlternateExecutable(string brokenPath)
        {
            try
            {
                string fileName = Path.GetFileName(brokenPath);
                if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                // Check other common system roots
                string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string localPrograms = Path.Combine(localApp, "Programs");

                // Check relative subpath replacement
                string drive = Path.GetPathRoot(brokenPath);
                string relPath = brokenPath.Substring(drive.Length);

                // 1. Check if same path exists on another drive (e.g. D:\ instead of C:\)
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo d in drives)
                {
                    if (d.IsReady && !string.Equals(d.Name, drive, StringComparison.OrdinalIgnoreCase))
                    {
                        string candidate = Path.Combine(d.Name, relPath);
                        if (File.Exists(candidate)) return candidate;
                    }
                }

                // 2. Search inside Local Programs
                if (Directory.Exists(localPrograms))
                {
                    string candidate = SearchFileShallow(localPrograms, fileName, 2);
                    if (!string.IsNullOrEmpty(candidate)) return candidate;
                }

                // 3. Search inside Program Files
                if (Directory.Exists(progFiles))
                {
                    string candidate = SearchFileShallow(progFiles, fileName, 2);
                    if (!string.IsNullOrEmpty(candidate)) return candidate;
                }

                // 4. Search inside Program Files (x86)
                if (!string.IsNullOrEmpty(progFilesX86) && Directory.Exists(progFilesX86))
                {
                    string candidate = SearchFileShallow(progFilesX86, fileName, 2);
                    if (!string.IsNullOrEmpty(candidate)) return candidate;
                }
            }
            catch { }

            return string.Empty;
        }

        private static string SearchFileShallow(string root, string targetFileName, int maxDepth)
        {
            if (maxDepth < 0 || !Directory.Exists(root)) return string.Empty;
            try
            {
                string[] files = Directory.GetFiles(root, targetFileName);
                if (files.Length > 0) return files[0];

                if (maxDepth > 0)
                {
                    string[] dirs = Directory.GetDirectories(root);
                    foreach (string d in dirs)
                    {
                        string found = SearchFileShallow(d, targetFileName, maxDepth - 1);
                        if (!string.IsNullOrEmpty(found)) return found;
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        public static bool RepairShortcut(string lnkPath, string newTarget)
        {
            if (!File.Exists(lnkPath) || !File.Exists(newTarget)) return false;
            try
            {
                dynamic wsh = GetWshShell();
                dynamic sc = wsh.CreateShortcut(lnkPath);
                sc.TargetPath = newTarget;
                string dir = Path.GetDirectoryName(newTarget);
                if (!string.IsNullOrEmpty(dir)) sc.WorkingDirectory = dir;
                sc.Save();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to repair shortcut: " + ex.Message);
                return false;
            }
        }

        public static void DeleteShortcut(string lnkPath, bool sendToRecycleBin)
        {
            if (!File.Exists(lnkPath)) return;

            if (sendToRecycleBin)
            {
                try
                {
                    SHFILEOPSTRUCT shf = new SHFILEOPSTRUCT();
                    shf.wFunc = FO_DELETE;
                    shf.pFrom = lnkPath + '\0' + '\0';
                    shf.fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT;
                    SHFileOperation(ref shf);
                    return;
                }
                catch { }
            }

            File.Delete(lnkPath);
        }

        public static void ExportReport(string exportPath, List<ShortcutItem> items)
        {
            using (StreamWriter sw = new StreamWriter(exportPath, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("Shortcut Name,Status,Target Path,Suggested Fix,Shortcut Location");
                foreach (ShortcutItem it in items)
                {
                    string statusStr = it.Status.ToString();
                    sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\"",
                        it.Name.Replace("\"", "\"\""),
                        statusStr,
                        it.TargetPath.Replace("\"", "\"\""),
                        it.SuggestedPath.Replace("\"", "\"\""),
                        it.Folder.Replace("\"", "\"\"")));
                }
            }
        }

        private static void AddFolderSafe(List<string> list, string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !list.Contains(path))
            {
                list.Add(path);
            }
        }

        private static void GetLnkFilesRecursive(string dir, List<string> list, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth || !Directory.Exists(dir)) return;

            try
            {
                string[] files = Directory.GetFiles(dir, "*.lnk");
                list.AddRange(files);

                string[] subDirs = Directory.GetDirectories(dir);
                foreach (string sub in subDirs)
                {
                    GetLnkFilesRecursive(sub, list, currentDepth + 1, maxDepth);
                }
            }
            catch { }
        }
    }
}
