using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace PexorisDiskCleaner
{
    public class JunkCategory
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSelected { get; set; }
        public bool IsRecommended { get; set; }
        public long TotalBytes { get; set; }
        public int FileCount { get; set; }
        public List<string> TargetPaths { get; set; }
        public string Status { get; set; }

        public JunkCategory(string id, string name, string desc, bool recommended, params string[] paths)
        {
            Id = id;
            Name = name;
            Description = desc;
            IsSelected = recommended;
            IsRecommended = recommended;
            TargetPaths = new List<string>(paths);
            TotalBytes = 0;
            FileCount = 0;
            Status = "Pending Scan";
        }
    }

    public static class DiskEngine
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI   = 0x00000002;
        private const uint SHERB_NOSOUND        = 0x00000004;

        public static List<JunkCategory> GetDefaultCategories()
        {
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string userTemp = Path.GetTempPath();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string sysDrive = Path.GetPathRoot(winDir);

            List<JunkCategory> list = new List<JunkCategory>();

            // 1. Windows Update Download Cache
            list.Add(new JunkCategory(
                "wuaudownload",
                "Windows Update Download Cache",
                "Temporary installation payloads and CAB/MSU files in SoftwareDistribution\\Download",
                true,
                Path.Combine(winDir, @"SoftwareDistribution\Download")
            ));

            // 2. User Temp Files
            list.Add(new JunkCategory(
                "usertemp",
                "User Temporary Files",
                "Application temporary files, installer extractions, and caches in %TEMP%",
                true,
                userTemp
            ));

            // 3. System Temp Files
            list.Add(new JunkCategory(
                "systemtemp",
                "Windows System Temp Files",
                "Temporary logs and work files created by Windows background services in C:\\Windows\\Temp",
                true,
                Path.Combine(winDir, "Temp")
            ));

            // 4. Windows Error Reports & Dumps
            list.Add(new JunkCategory(
                "crashdumps",
                "Error Reports & Crash Dumps",
                "Application crash memory dumps and Windows Error Reporting archives",
                true,
                Path.Combine(localAppData, "CrashDumps"),
                Path.Combine(programData, @"Microsoft\Windows\WER\ReportArchive"),
                Path.Combine(programData, @"Microsoft\Windows\WER\ReportQueue"),
                Path.Combine(winDir, "Minidump")
            ));

            // 5. Delivery Optimization Files
            list.Add(new JunkCategory(
                "delivopt",
                "Delivery Optimization Cache",
                "P2P update sharing caches stored for local network distribution",
                true,
                Path.Combine(winDir, @"SoftwareDistribution\DeliveryOptimization"),
                Path.Combine(localAppData, @"Microsoft\Windows\DeliveryOptimization")
            ));

            // 6. DirectX Shader Cache
            list.Add(new JunkCategory(
                "dxshaders",
                "DirectX Shader Cache",
                "Compiled GPU shaders created to speed up graphics and gaming launch times",
                true,
                Path.Combine(localAppData, "D3DSCache"),
                Path.Combine(localAppData, @"NVIDIA\DXCache"),
                Path.Combine(localAppData, @"AMD\DxCache")
            ));

            // 7. Thumbnail Cache Databases
            list.Add(new JunkCategory(
                "thumbcache",
                "Windows Explorer Thumbnail Cache",
                "Cached preview thumbnails (thumbcache_*.db) that can safely be regenerated",
                false,
                Path.Combine(localAppData, @"Microsoft\Windows\Explorer")
            ));

            // 8. Prefetch Execution History
            list.Add(new JunkCategory(
                "prefetch",
                "Windows Prefetch Artifacts",
                "Outdated application launch tracking traces in C:\\Windows\\Prefetch",
                false,
                Path.Combine(winDir, "Prefetch")
            ));

            // 9. Windows Upgrade & Setup Leftovers
            list.Add(new JunkCategory(
                "upgradesetup",
                "Upgrade & Setup Leftovers",
                "Remnants of Windows feature updates ($Windows.~BT, $Windows.~WS, Panther)",
                true,
                Path.Combine(sysDrive, "$Windows.~BT"),
                Path.Combine(sysDrive, "$Windows.~WS"),
                Path.Combine(winDir, "Panther")
            ));

            // 10. Recycle Bin
            list.Add(new JunkCategory(
                "recyclebin",
                "Recycle Bin",
                "Deleted files waiting permanently in the Windows Recycle Bin",
                true,
                "RECYCLE_BIN_SPECIAL"
            ));

            return list;
        }

        public static void ScanCategory(JunkCategory cat)
        {
            cat.TotalBytes = 0;
            cat.FileCount = 0;

            if (cat.TargetPaths.Contains("RECYCLE_BIN_SPECIAL"))
            {
                try
                {
                    SHQUERYRBINFO info = new SHQUERYRBINFO();
                    info.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
                    int hr = SHQueryRecycleBin(null, ref info);
                    if (hr == 0)
                    {
                        cat.TotalBytes = info.i64Size;
                        cat.FileCount = (int)Math.Min(info.i64NumItems, int.MaxValue);
                    }
                }
                catch { }

                cat.Status = cat.FileCount > 0 ? string.Format("{0} files found", cat.FileCount) : "Clean";
                return;
            }

            foreach (string path in cat.TargetPaths)
            {
                if (!Directory.Exists(path)) continue;

                try
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    ScanDirectoryRecursive(di, cat);
                }
                catch { }
            }

            cat.Status = cat.FileCount > 0 ? string.Format("{0} files found", cat.FileCount) : "Clean";
        }

        private static void ScanDirectoryRecursive(DirectoryInfo dir, JunkCategory cat)
        {
            try
            {
                FileInfo[] files = dir.GetFiles();
                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        cat.TotalBytes += files[i].Length;
                        cat.FileCount++;
                    }
                    catch { }
                }

                DirectoryInfo[] subDirs = dir.GetDirectories();
                for (int i = 0; i < subDirs.Length; i++)
                {
                    ScanDirectoryRecursive(subDirs[i], cat);
                }
            }
            catch { }
        }

        public static int CleanCategory(JunkCategory cat, out long cleanedBytes)
        {
            cleanedBytes = 0;
            int cleanedFiles = 0;

            if (cat.TargetPaths.Contains("RECYCLE_BIN_SPECIAL"))
            {
                try
                {
                    long before = cat.TotalBytes;
                    SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                    cleanedBytes = before;
                    cleanedFiles = cat.FileCount;
                    cat.TotalBytes = 0;
                    cat.FileCount = 0;
                    cat.Status = "Emptied";
                }
                catch { }
                return cleanedFiles;
            }

            foreach (string path in cat.TargetPaths)
            {
                if (!Directory.Exists(path)) continue;

                try
                {
                    DirectoryInfo di = new DirectoryInfo(path);
                    CleanDirectoryRecursive(di, ref cleanedFiles, ref cleanedBytes);
                }
                catch { }
            }

            cat.TotalBytes = Math.Max(0, cat.TotalBytes - cleanedBytes);
            cat.FileCount = Math.Max(0, cat.FileCount - cleanedFiles);
            cat.Status = string.Format("Cleaned ({0} files freed)", cleanedFiles);
            return cleanedFiles;
        }

        private static void CleanDirectoryRecursive(DirectoryInfo dir, ref int cleanedFiles, ref long cleanedBytes)
        {
            try
            {
                FileInfo[] files = dir.GetFiles();
                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        long len = files[i].Length;
                        files[i].Attributes = FileAttributes.Normal;
                        files[i].Delete();
                        cleanedBytes += len;
                        cleanedFiles++;
                    }
                    catch { /* in-use or permission denied files safely skipped */ }
                }

                DirectoryInfo[] subDirs = dir.GetDirectories();
                for (int i = 0; i < subDirs.Length; i++)
                {
                    CleanDirectoryRecursive(subDirs[i], ref cleanedFiles, ref cleanedBytes);
                    try
                    {
                        subDirs[i].Delete(false); // only if empty
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 KB";
            if (bytes < 1024 * 1024)
            {
                return string.Format("{0:F1} KB", bytes / 1024.0);
            }
            if (bytes < 1024L * 1024L * 1024L)
            {
                return string.Format("{0:F2} MB", bytes / (1024.0 * 1024.0));
            }
            return string.Format("{0:F2} GB", bytes / (1024.0 * 1024.0 * 1024.0));
        }
    }
}
