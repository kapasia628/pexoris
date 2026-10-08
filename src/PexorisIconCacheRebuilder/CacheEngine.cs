using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace PexorisIconCacheRebuilder
{
    public class CacheFileInfo
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string CacheType { get; set; } // "Icon Cache" or "Thumbnail Cache"
        public long FileSize { get; set; }
        public bool IsLocked { get; set; }
        public string Status { get; set; }

        public CacheFileInfo()
        {
            Status = "Detected";
        }
    }

    public static class CacheEngine
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        public static List<CacheFileInfo> ScanCacheFiles()
        {
            var list = new List<CacheFileInfo>();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 1. Classic IconCache.db
            string classicIconDb = Path.Combine(localAppData, "IconCache.db");
            if (File.Exists(classicIconDb))
            {
                list.Add(CreateCacheInfo(classicIconDb, "Icon Cache"));
            }

            // 2. Modern Explorer Cache Directory
            string explorerCacheDir = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
            if (Directory.Exists(explorerCacheDir))
            {
                try
                {
                    var files = Directory.GetFiles(explorerCacheDir, "*cache_*.db");
                    foreach (var file in files)
                    {
                        string name = Path.GetFileName(file).ToLowerInvariant();
                        string type = name.StartsWith("thumb") ? "Thumbnail Cache" : "Icon Cache";
                        list.Add(CreateCacheInfo(file, type));
                    }
                }
                catch { }
            }

            return list;
        }

        private static CacheFileInfo CreateCacheInfo(string fullPath, string type)
        {
            var info = new CacheFileInfo
            {
                FullPath = fullPath,
                FileName = Path.GetFileName(fullPath),
                CacheType = type,
                Status = "Ready"
            };

            try
            {
                var fi = new FileInfo(fullPath);
                info.FileSize = fi.Length;
                info.IsLocked = TestFileLocked(fullPath);
            }
            catch
            {
                info.IsLocked = true;
            }

            return info;
        }

        private static bool TestFileLocked(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return false;
                }
            }
            catch
            {
                return true;
            }
        }

        public static bool RebuildCaches(bool includeIcons, bool includeThumbnails, bool restartExplorer, out int deletedCount, out int errorCount, out string logMessage)
        {
            deletedCount = 0;
            errorCount = 0;
            var logs = new List<string>();

            logs.Add("Terminating Windows Explorer shell processes...");
            KillExplorer();
            Thread.Sleep(800);

            var files = ScanCacheFiles();
            foreach (var file in files)
            {
                bool target = false;
                if (includeIcons && file.CacheType == "Icon Cache") target = true;
                if (includeThumbnails && file.CacheType == "Thumbnail Cache") target = true;

                if (!target) continue;

                try
                {
                    if (File.Exists(file.FullPath))
                    {
                        File.SetAttributes(file.FullPath, FileAttributes.Normal);
                        File.Delete(file.FullPath);
                        deletedCount++;
                        logs.Add("Deleted: " + file.FileName);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    logs.Add("Error deleting " + file.FileName + ": " + ex.Message);
                }
            }

            // Notify Windows shell
            logs.Add("Broadcasting SHCNE_ASSOCCHANGED shell refresh notification...");
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }

            if (restartExplorer)
            {
                logs.Add("Restarting Windows Explorer shell...");
                Thread.Sleep(500);
                StartExplorer();
                Thread.Sleep(1000);
            }

            logMessage = string.Join(Environment.NewLine, logs.ToArray());
            return errorCount == 0;
        }

        public static void KillExplorer()
        {
            try
            {
                var procs = Process.GetProcessesByName("explorer");
                foreach (var p in procs)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(1500);
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void StartExplorer()
        {
            try
            {
                var procs = Process.GetProcessesByName("explorer");
                if (procs == null || procs.Length == 0)
                {
                    Process.Start("explorer.exe");
                }
            }
            catch { }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return string.Format("{0:0.0} MB", bytes / (1024.0 * 1024.0));
            if (bytes >= 1024)
                return string.Format("{0:0.0} KB", bytes / 1024.0);
            return bytes + " B";
        }
    }
}
