using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PexorisDiskUsage
{
    public class DiskItemInfo
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public long TotalSizeBytes { get; set; }
        public int FileCount { get; set; }
        public int SubfolderCount { get; set; }
        public double PercentageOfParent { get; set; }

        public DiskItemInfo()
        {
            Name = string.Empty;
            FullPath = string.Empty;
            IsDirectory = true;
            TotalSizeBytes = 0;
            FileCount = 0;
            SubfolderCount = 0;
            PercentageOfParent = 0.0;
        }

        public string FormattedSize
        {
            get
            {
                return FormatBytes(TotalSizeBytes);
            }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1073741824L * 1024L) // TB
            {
                return (bytes / (1024.0 * 1024.0 * 1024.0 * 1024.0)).ToString("0.00") + " TB";
            }
            if (bytes >= 1073741824L) // GB
            {
                return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("0.00") + " GB";
            }
            if (bytes >= 1048576L) // MB
            {
                return (bytes / (1024.0 * 1024.0)).ToString("0.0") + " MB";
            }
            if (bytes >= 1024L) // KB
            {
                return (bytes / 1024.0).ToString("0.0") + " KB";
            }
            return bytes + " B";
        }
    }

    public static class DiskEngine
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.U4)]
            public int wFunc;
            public string pFrom;
            public string pTo;
            public short fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        private const int FO_DELETE = 0x0003;
        private const short FOF_ALLOWUNDO = 0x0040;
        private const short FOF_NOCONFIRMATION = 0x0010;
        private const short FOF_SILENT = 0x0004;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        public static bool SendToRecycleBin(string path)
        {
            try
            {
                SHFILEOPSTRUCT fileOp = new SHFILEOPSTRUCT();
                fileOp.wFunc = FO_DELETE;
                fileOp.pFrom = path + '\0' + '\0';
                fileOp.fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT;

                int result = SHFileOperation(ref fileOp);
                return result == 0;
            }
            catch
            {
                return false;
            }
        }

        public static List<DriveInfo> GetFixedDrives()
        {
            List<DriveInfo> list = new List<DriveInfo>();
            try
            {
                DriveInfo[] all = DriveInfo.GetDrives();
                foreach (DriveInfo d in all)
                {
                    if (d.IsReady)
                    {
                        list.Add(d);
                    }
                }
            }
            catch { }
            return list;
        }

        public static List<DiskItemInfo> ScanDirectory(string rootPath, out long totalScannedBytes, out int totalFilesScanned)
        {
            List<DiskItemInfo> items = new List<DiskItemInfo>();
            totalScannedBytes = 0;
            totalFilesScanned = 0;

            if (!Directory.Exists(rootPath)) return items;

            DirectoryInfo dir = new DirectoryInfo(rootPath);

            // 1. Scan direct subdirectories
            DirectoryInfo[] subDirs = new DirectoryInfo[0];
            try
            {
                subDirs = dir.GetDirectories();
            }
            catch { }

            foreach (DirectoryInfo sub in subDirs)
            {
                // Skip virtual / special link junctions
                if ((sub.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    continue;

                int fCount = 0;
                int dCount = 0;
                long sz = CalculateDirectorySize(sub, ref fCount, ref dCount);

                DiskItemInfo item = new DiskItemInfo();
                item.Name = sub.Name;
                item.FullPath = sub.FullName;
                item.IsDirectory = true;
                item.TotalSizeBytes = sz;
                item.FileCount = fCount;
                item.SubfolderCount = dCount;

                items.Add(item);
                totalScannedBytes += sz;
                totalFilesScanned += fCount;
            }

            // 2. Scan root files
            FileInfo[] files = new FileInfo[0];
            try
            {
                files = dir.GetFiles();
            }
            catch { }

            foreach (FileInfo f in files)
            {
                DiskItemInfo item = new DiskItemInfo();
                item.Name = f.Name;
                item.FullPath = f.FullName;
                item.IsDirectory = false;
                item.TotalSizeBytes = f.Length;
                item.FileCount = 1;
                item.SubfolderCount = 0;

                items.Add(item);
                totalScannedBytes += f.Length;
                totalFilesScanned += 1;
            }

            // Calculate percentage
            if (totalScannedBytes > 0)
            {
                foreach (DiskItemInfo item in items)
                {
                    item.PercentageOfParent = Math.Round((item.TotalSizeBytes / (double)totalScannedBytes) * 100.0, 1);
                }
            }

            // Sort descending by size
            items.Sort(delegate(DiskItemInfo a, DiskItemInfo b) {
                return b.TotalSizeBytes.CompareTo(a.TotalSizeBytes);
            });

            return items;
        }

        private static long CalculateDirectorySize(DirectoryInfo d, ref int fileCount, ref int dirCount)
        {
            long size = 0;
            try
            {
                FileInfo[] files = d.GetFiles();
                fileCount += files.Length;
                foreach (FileInfo f in files)
                {
                    size += f.Length;
                }

                DirectoryInfo[] dirs = d.GetDirectories();
                dirCount += dirs.Length;
                foreach (DirectoryInfo sub in dirs)
                {
                    if ((sub.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                    {
                        size += CalculateDirectorySize(sub, ref fileCount, ref dirCount);
                    }
                }
            }
            catch { }
            return size;
        }

        public static List<DiskItemInfo> FindLargeFiles(string rootPath, long minSizeBytes, int maxResults)
        {
            List<DiskItemInfo> results = new List<DiskItemInfo>();
            try
            {
                DirectoryInfo dir = new DirectoryInfo(rootPath);
                SearchLargeFilesRecursive(dir, minSizeBytes, results, maxResults);
            }
            catch { }

            results.Sort(delegate(DiskItemInfo a, DiskItemInfo b) {
                return b.TotalSizeBytes.CompareTo(a.TotalSizeBytes);
            });

            return results;
        }

        private static void SearchLargeFilesRecursive(DirectoryInfo dir, long minSizeBytes, List<DiskItemInfo> results, int maxResults)
        {
            if (results.Count >= maxResults) return;

            try
            {
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo f in files)
                {
                    if (f.Length >= minSizeBytes)
                    {
                        DiskItemInfo it = new DiskItemInfo();
                        it.Name = f.Name;
                        it.FullPath = f.FullName;
                        it.IsDirectory = false;
                        it.TotalSizeBytes = f.Length;
                        it.FileCount = 1;
                        results.Add(it);
                        if (results.Count >= maxResults) return;
                    }
                }

                DirectoryInfo[] subDirs = dir.GetDirectories();
                foreach (DirectoryInfo sub in subDirs)
                {
                    if ((sub.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                    {
                        SearchLargeFilesRecursive(sub, minSizeBytes, results, maxResults);
                    }
                }
            }
            catch { }
        }

        public static string ExportToCsv(string scanTarget, long totalBytes, int fileCount, List<DiskItemInfo> items)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Pexoris DiskUsage Space Audit Report ===");
            sb.AppendLine("Report Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Scan Target: " + scanTarget);
            sb.AppendLine("Total Scanned Size: " + DiskItemInfo.FormatBytes(totalBytes));
            sb.AppendLine("Total Files Analyzed: " + fileCount);
            sb.AppendLine();
            sb.AppendLine("Name,Type,Size (Bytes),Formatted Size,Percentage,Files,Subfolders,Path");

            foreach (DiskItemInfo it in items)
            {
                sb.AppendLine(string.Format("\"{0}\",\"{1}\",{2},\"{3}\",{4}%,{5},{6},\"{7}\"",
                    it.Name.Replace("\"", "\"\""),
                    it.IsDirectory ? "Folder" : "File",
                    it.TotalSizeBytes,
                    it.FormattedSize,
                    it.PercentageOfParent,
                    it.FileCount,
                    it.SubfolderCount,
                    it.FullPath.Replace("\"", "\"\"")));
            }

            return sb.ToString();
        }
    }
}
