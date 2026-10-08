using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;

namespace PexorisWinUpdateReset
{
    public class ServiceInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
        public string StartupType { get; set; }
        public bool IsHealthy { get; set; }
    }

    public class CacheFolderInfo
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public long TotalSizeBytes { get; set; }
        public int FileCount { get; set; }
        public string FormattedSize { get; set; }
        public bool Exists { get; set; }
    }

    public class SystemScanResult
    {
        public List<ServiceInfo> Services { get; set; }
        public List<CacheFolderInfo> CacheFolders { get; set; }
        public long TotalCacheBytes { get; set; }
        public int TotalCacheFiles { get; set; }
        public string FormattedTotalCache { get; set; }
        public bool AnyServiceStopped { get; set; }
        public string SummaryHeadline { get; set; }
    }

    public static class UpdateEngine
    {
        private static readonly string[] MonitoredServices = new string[] {
            "wuauserv",
            "bits",
            "cryptsvc",
            "dosvc",
            "msiserver"
        };

        private static readonly string WindowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        private static readonly string System32Dir = Environment.GetFolderPath(Environment.SpecialFolder.System);
        private static readonly string ProgramDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        public static string SoftwareDistributionPath
        {
            get { return Path.Combine(WindowsDir, "SoftwareDistribution"); }
        }

        public static string SoftwareDistributionDownloadPath
        {
            get { return Path.Combine(SoftwareDistributionPath, "Download"); }
        }

        public static string SoftwareDistributionDataStorePath
        {
            get { return Path.Combine(SoftwareDistributionPath, "DataStore"); }
        }

        public static string Catroot2Path
        {
            get { return Path.Combine(System32Dir, "catroot2"); }
        }

        public static string BitsQueuePath
        {
            get { return Path.Combine(ProgramDataDir, @"Microsoft\Network\Downloader"); }
        }

        public static SystemScanResult PerformScan()
        {
            SystemScanResult result = new SystemScanResult();
            result.Services = new List<ServiceInfo>();
            result.CacheFolders = new List<CacheFolderInfo>();

            long totalBytes = 0;
            int totalFiles = 0;
            bool anyStopped = false;

            // 1. Inspect Services
            foreach (string svcName in MonitoredServices)
            {
                ServiceInfo sInfo = new ServiceInfo();
                sInfo.Name = svcName;
                try
                {
                    using (ServiceController sc = new ServiceController(svcName))
                    {
                        sInfo.DisplayName = sc.DisplayName;
                        sInfo.Status = sc.Status.ToString();
                        sInfo.IsHealthy = (sc.Status == ServiceControllerStatus.Running);
                        if (sc.Status == ServiceControllerStatus.Stopped && (svcName == "wuauserv" || svcName == "bits" || svcName == "cryptsvc"))
                        {
                            anyStopped = true;
                        }
                    }
                }
                catch
                {
                    sInfo.DisplayName = svcName;
                    sInfo.Status = "Not Found";
                    sInfo.IsHealthy = false;
                }
                result.Services.Add(sInfo);
            }

            // 2. Inspect Cache Folders
            // A: SoftwareDistribution\Download
            CacheFolderInfo dlFolder = InspectDirectory("WU Download Payloads", SoftwareDistributionDownloadPath);
            result.CacheFolders.Add(dlFolder);
            totalBytes += dlFolder.TotalSizeBytes;
            totalFiles += dlFolder.FileCount;

            // B: SoftwareDistribution\DataStore
            CacheFolderInfo dsFolder = InspectDirectory("WU DataStore Database", SoftwareDistributionDataStorePath);
            result.CacheFolders.Add(dsFolder);
            totalBytes += dsFolder.TotalSizeBytes;
            totalFiles += dsFolder.FileCount;

            // C: System32\catroot2
            CacheFolderInfo catFolder = InspectDirectory("Catalog Signature Cache (catroot2)", Catroot2Path);
            result.CacheFolders.Add(catFolder);
            totalBytes += catFolder.TotalSizeBytes;
            totalFiles += catFolder.FileCount;

            // D: BITS Transfer Queue
            CacheFolderInfo bitsFolder = InspectDirectory("BITS Downloader Queue", BitsQueuePath);
            result.CacheFolders.Add(bitsFolder);
            totalBytes += bitsFolder.TotalSizeBytes;
            totalFiles += bitsFolder.FileCount;

            result.TotalCacheBytes = totalBytes;
            result.TotalCacheFiles = totalFiles;
            result.FormattedTotalCache = FormatBytes(totalBytes);
            result.AnyServiceStopped = anyStopped;

            if (totalBytes > 1024 * 1024 * 500) // > 500 MB
            {
                result.SummaryHeadline = string.Format("{0} cached update files occupying {1} disk space", totalFiles, result.FormattedTotalCache);
            }
            else if (anyStopped)
            {
                result.SummaryHeadline = "Critical Windows Update background services are currently stopped";
            }
            else
            {
                result.SummaryHeadline = string.Format("All update services healthy • {0} cached files ({1})", totalFiles, result.FormattedTotalCache);
            }

            return result;
        }

        private static CacheFolderInfo InspectDirectory(string name, string path)
        {
            CacheFolderInfo info = new CacheFolderInfo();
            info.Name = name;
            info.FullPath = path;
            info.Exists = Directory.Exists(path);

            if (!info.Exists)
            {
                info.TotalSizeBytes = 0;
                info.FileCount = 0;
                info.FormattedSize = "0 B (Missing)";
                return info;
            }

            long bytes = 0;
            int count = 0;
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                FileInfo[] files = dir.GetFiles("*", SearchOption.AllDirectories);
                count = files.Length;
                foreach (FileInfo f in files)
                {
                    try { bytes += f.Length; } catch { }
                }
            }
            catch
            {
                // Access permission or partial lock
            }

            info.TotalSizeBytes = bytes;
            info.FileCount = count;
            info.FormattedSize = string.Format("{0} ({1} files)", FormatBytes(bytes), count);
            return info;
        }

        public static bool ResetAllComponents(out string logMessage)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Starting complete Windows Update reset...");

            // 1. Stop all related services
            StopServiceSafe("wuauserv", sb);
            StopServiceSafe("bits", sb);
            StopServiceSafe("cryptsvc", sb);
            StopServiceSafe("dosvc", sb);
            StopServiceSafe("msiserver", sb);

            // Brief wait for handles release
            System.Threading.Thread.Sleep(800);

            // 2. Wipe / Purge SoftwareDistribution Download & DataStore
            PurgeDirectorySafe(SoftwareDistributionDownloadPath, sb, "SoftwareDistribution\\Download");
            PurgeDirectorySafe(SoftwareDistributionDataStorePath, sb, "SoftwareDistribution\\DataStore");

            // 3. Wipe catroot2 contents
            PurgeDirectorySafe(Catroot2Path, sb, "catroot2");

            // 4. Wipe BITS qmgr dat files
            try
            {
                if (Directory.Exists(BitsQueuePath))
                {
                    string[] datFiles = Directory.GetFiles(BitsQueuePath, "qmgr*.dat");
                    foreach (string df in datFiles)
                    {
                        try { File.Delete(df); sb.AppendLine("Deleted BITS queue: " + Path.GetFileName(df)); } catch { }
                    }
                }
            }
            catch { }

            // 5. Restart essential services in order
            StartServiceSafe("cryptsvc", sb);
            StartServiceSafe("bits", sb);
            StartServiceSafe("wuauserv", sb);
            StartServiceSafe("dosvc", sb);

            sb.AppendLine("All Windows Update components cleanly reset & restarted.");
            logMessage = sb.ToString();
            return true;
        }

        public static bool ClearDownloadCacheOnly(out string logMessage)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Stopping Windows Update service...");
            StopServiceSafe("wuauserv", sb);
            StopServiceSafe("bits", sb);

            System.Threading.Thread.Sleep(500);

            PurgeDirectorySafe(SoftwareDistributionDownloadPath, sb, "SoftwareDistribution\\Download");

            StartServiceSafe("bits", sb);
            StartServiceSafe("wuauserv", sb);

            sb.AppendLine("Update download cache cleared successfully.");
            logMessage = sb.ToString();
            return true;
        }

        public static bool RestartUpdateServices(out string logMessage)
        {
            StringBuilder sb = new StringBuilder();
            StopServiceSafe("wuauserv", sb);
            StopServiceSafe("bits", sb);
            StopServiceSafe("cryptsvc", sb);

            System.Threading.Thread.Sleep(600);

            StartServiceSafe("cryptsvc", sb);
            StartServiceSafe("bits", sb);
            StartServiceSafe("wuauserv", sb);

            sb.AppendLine("Windows Update services restarted cleanly.");
            logMessage = sb.ToString();
            return true;
        }

        private static void StopServiceSafe(string name, StringBuilder sb)
        {
            try
            {
                using (ServiceController sc = new ServiceController(name))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                        sb.AppendLine(string.Format("Stopped service: {0}", name));
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to net.exe or taskkill if blocked
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("net.exe", "stop " + name);
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process p = Process.Start(psi);
                    p.WaitForExit(3000);
                }
                catch { }
                sb.AppendLine(string.Format("Service {0} stop attempt finished: {1}", name, ex.Message));
            }
        }

        private static void StartServiceSafe(string name, StringBuilder sb)
        {
            try
            {
                using (ServiceController sc = new ServiceController(name))
                {
                    if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                        sb.AppendLine(string.Format("Started service: {0}", name));
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("net.exe", "start " + name);
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process p = Process.Start(psi);
                    p.WaitForExit(3000);
                }
                catch { }
                sb.AppendLine(string.Format("Service {0} start attempt finished: {1}", name, ex.Message));
            }
        }

        private static void PurgeDirectorySafe(string path, StringBuilder sb, string label)
        {
            if (!Directory.Exists(path)) return;

            int deletedCount = 0;
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo file in di.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        file.Attributes = FileAttributes.Normal;
                        file.Delete();
                        deletedCount++;
                    }
                    catch { }
                }

                foreach (DirectoryInfo subDir in di.GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        subDir.Delete(true);
                    }
                    catch { }
                }
                sb.AppendLine(string.Format("Purged {0} ({1} files removed)", label, deletedCount));
            }
            catch (Exception ex)
            {
                sb.AppendLine(string.Format("Partial purge on {0}: {1}", label, ex.Message));
            }
        }

        public static void OpenWindowsUpdateSettings()
        {
            try
            {
                Process.Start("ms-settings:windowsupdate");
            }
            catch
            {
                try { Process.Start("control.exe", "/name Microsoft.WindowsUpdate"); } catch { }
            }
        }

        public static void TriggerUpdateCheck()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("usoclient.exe", "StartScan");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            catch
            {
                try
                {
                    ProcessStartInfo psi2 = new ProcessStartInfo("wuauclt.exe", "/detectnow /updatenow");
                    psi2.CreateNoWindow = true;
                    psi2.UseShellExecute = false;
                    Process.Start(psi2);
                }
                catch { }
            }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB" };
            double dBytes = bytes;
            int order = 0;
            while (dBytes >= 1024 && order < units.Length - 1)
            {
                order++;
                dBytes /= 1024;
            }
            return string.Format("{0:0.##} {1}", dBytes, units[order]);
        }
    }
}
