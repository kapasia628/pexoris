using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace Pexoris.PrintFixer
{
    public class PrintJobItem
    {
        public int JobId { get; set; }
        public string DocumentName { get; set; }
        public string PrinterName { get; set; }
        public string Status { get; set; }
        public string Owner { get; set; }
        public string Pages { get; set; }
        public string Size { get; set; }
        public string SubmittedTime { get; set; }
        public bool IsStuck { get; set; }

        public PrintJobItem()
        {
            DocumentName = "Unknown";
            PrinterName = "Default";
            Status = "Pending";
            Owner = "System";
            Pages = "1";
            Size = "0 KB";
            SubmittedTime = "";
            IsStuck = false;
        }
    }

    public class PrinterItem
    {
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public string Status { get; set; }
        public string PortName { get; set; }
        public string DriverName { get; set; }
        public int JobCount { get; set; }

        public PrinterItem()
        {
            Name = "";
            Status = "Ready";
            PortName = "";
            DriverName = "";
            JobCount = 0;
        }

        public override string ToString()
        {
            return Name + (IsDefault ? " (Default)" : "");
        }
    }

    public class PurgeResult
    {
        public bool Success { get; set; }
        public int DeletedFilesCount { get; set; }
        public string Message { get; set; }

        public PurgeResult()
        {
            Success = false;
            DeletedFilesCount = 0;
            Message = "";
        }
    }

    public static class SpoolerHelper
    {
        // ==========================================
        // WIN32 WINSPOOL.DRV NATIVE INTEROP
        // ==========================================
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool SetJob(IntPtr hPrinter, int jobId, int level, IntPtr pJob, int command);

        public const int JOB_CONTROL_PAUSE = 1;
        public const int JOB_CONTROL_RESUME = 2;
        public const int JOB_CONTROL_CANCEL = 3;
        public const int JOB_CONTROL_RESTART = 4;
        public const int JOB_CONTROL_DELETE = 5;

        public static string SpoolPrintersDirectory
        {
            get
            {
                return Path.Combine(Environment.SystemDirectory, @"spool\PRINTERS");
            }
        }

        // ==========================================
        // 1. SERVICE STATUS & RESTART
        // ==========================================
        public static string GetSpoolerStatusString()
        {
            try
            {
                using (ServiceController sc = new ServiceController("Spooler"))
                {
                    switch (sc.Status)
                    {
                        case ServiceControllerStatus.Running:
                            return "Running";
                        case ServiceControllerStatus.Stopped:
                            return "Stopped";
                        case ServiceControllerStatus.Paused:
                            return "Paused";
                        case ServiceControllerStatus.StartPending:
                            return "Starting...";
                        case ServiceControllerStatus.StopPending:
                            return "Stopping...";
                        default:
                            return sc.Status.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public static bool StopSpoolerService(int timeoutSeconds = 6)
        {
            try
            {
                using (ServiceController sc = new ServiceController("Spooler"))
                {
                    if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.StartPending)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(timeoutSeconds));
                    }
                }
                return true;
            }
            catch
            {
                // Fallback: Force kill spoolsv.exe if service is hung
                try
                {
                    Process[] procs = Process.GetProcessesByName("spoolsv");
                    for (int i = 0; i < procs.Length; i++)
                    {
                        procs[i].Kill();
                        procs[i].WaitForExit(3000);
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool StartSpoolerService(int timeoutSeconds = 8)
        {
            try
            {
                using (ServiceController sc = new ServiceController("Spooler"))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(timeoutSeconds));
                    }
                }
                return true;
            }
            catch (Exception)
            {
                // Fallback via cmd net start
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("net", "start Spooler");
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit(5000);
                        return p.ExitCode == 0;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool RestartSpoolerService()
        {
            StopSpoolerService(5);
            System.Threading.Thread.Sleep(500);
            return StartSpoolerService(8);
        }

        // ==========================================
        // 2. PURGE PRINT QUEUE & SPOOL FILES
        // ==========================================
        public static PurgeResult PurgeAllPrintQueuesAndFiles()
        {
            PurgeResult res = new PurgeResult();
            int deletedFiles = 0;

            try
            {
                // 1. Stop Spooler
                StopSpoolerService(5);
                System.Threading.Thread.Sleep(400);

                // 2. Delete all *.SPL and *.SHD files in C:\Windows\System32\spool\PRINTERS\
                string spoolDir = SpoolPrintersDirectory;
                if (Directory.Exists(spoolDir))
                {
                    string[] files = Directory.GetFiles(spoolDir, "*.*");
                    for (int i = 0; i < files.Length; i++)
                    {
                        string file = files[i];
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                            deletedFiles++;
                        }
                        catch
                        {
                            // If locked by another handle, continue
                        }
                    }
                }

                // 3. Restart Spooler
                bool started = StartSpoolerService(8);

                res.Success = started;
                res.DeletedFilesCount = deletedFiles;
                if (started)
                {
                    res.Message = string.Format("Print Spooler successfully restarted! {0} stuck spool file(s) cleared.", deletedFiles);
                }
                else
                {
                    res.Message = string.Format("Deleted {0} spool files, but Spooler service failed to start automatically.", deletedFiles);
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = "Purge error: " + ex.Message;
            }

            return res;
        }

        // ==========================================
        // 3. ENUMERATE PRINTERS
        // ==========================================
        public static List<PrinterItem> GetInstalledPrinters()
        {
            List<PrinterItem> list = new List<PrinterItem>();
            string defaultPrinterName = "";

            try
            {
                PrinterSettings ps = new PrinterSettings();
                defaultPrinterName = ps.PrinterName;
            }
            catch { }

            try
            {
                // Query WMI for rich details: Port, Driver, Status
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        PrinterItem item = new PrinterItem();
                        item.Name = (mo["Name"] != null) ? mo["Name"].ToString() : "Unknown";
                        item.PortName = (mo["PortName"] != null) ? mo["PortName"].ToString() : "";
                        item.DriverName = (mo["DriverName"] != null) ? mo["DriverName"].ToString() : "";

                        bool isDef = false;
                        if (mo["Default"] != null) bool.TryParse(mo["Default"].ToString(), out isDef);
                        if (!isDef && !string.IsNullOrEmpty(defaultPrinterName))
                        {
                            isDef = item.Name.Equals(defaultPrinterName, StringComparison.OrdinalIgnoreCase);
                        }
                        item.IsDefault = isDef;

                        // Status / WorkOffline
                        bool isOffline = false;
                        if (mo["WorkOffline"] != null) bool.TryParse(mo["WorkOffline"].ToString(), out isOffline);

                        uint printerStatus = 3; // 3 = Idle
                        if (mo["PrinterStatus"] != null) uint.TryParse(mo["PrinterStatus"].ToString(), out printerStatus);

                        if (isOffline)
                        {
                            item.Status = "Offline";
                        }
                        else
                        {
                            switch (printerStatus)
                            {
                                case 1: item.Status = "Other"; break;
                                case 2: item.Status = "Unknown"; break;
                                case 3: item.Status = "Idle / Ready"; break;
                                case 4: item.Status = "Printing"; break;
                                case 5: item.Status = "Warmup"; break;
                                case 6: item.Status = "Stopped"; break;
                                case 7: item.Status = "Offline"; break;
                                default: item.Status = "Ready"; break;
                            }
                        }

                        list.Add(item);
                    }
                }
            }
            catch
            {
                // Fallback via standard .NET InstalledPrinters
                foreach (string pName in PrinterSettings.InstalledPrinters)
                {
                    PrinterItem item = new PrinterItem();
                    item.Name = pName;
                    item.IsDefault = pName.Equals(defaultPrinterName, StringComparison.OrdinalIgnoreCase);
                    item.Status = "Ready";
                    list.Add(item);
                }
            }

            return list;
        }

        // ==========================================
        // 4. ENUMERATE ACTIVE PRINT JOBS
        // ==========================================
        public static List<PrintJobItem> GetAllPrintJobs(string filterPrinter = null)
        {
            List<PrintJobItem> jobs = new List<PrintJobItem>();

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PrintJob"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        PrintJobItem item = new PrintJobItem();

                        int jId = 0;
                        if (mo["JobId"] != null) int.TryParse(mo["JobId"].ToString(), out jId);
                        item.JobId = jId;

                        item.DocumentName = (mo["Document"] != null) ? mo["Document"].ToString() : "Untitled Document";

                        // Name format in WMI is "PrinterName, JobId"
                        string rawName = (mo["Name"] != null) ? mo["Name"].ToString() : "";
                        string pName = rawName;
                        if (rawName.Contains(","))
                        {
                            pName = rawName.Substring(0, rawName.IndexOf(',')).Trim();
                        }
                        item.PrinterName = pName;

                        if (!string.IsNullOrEmpty(filterPrinter) && !filterPrinter.Equals("All Printers", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!item.PrinterName.Equals(filterPrinter, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        item.Owner = (mo["Owner"] != null) ? mo["Owner"].ToString() : "User";

                        // Pages
                        int totalPages = 0;
                        if (mo["TotalPages"] != null) int.TryParse(mo["TotalPages"].ToString(), out totalPages);
                        int printedPages = 0;
                        if (mo["PagesPrinted"] != null) int.TryParse(mo["PagesPrinted"].ToString(), out printedPages);

                        if (totalPages > 0)
                        {
                            item.Pages = string.Format("{0} of {1}", printedPages, totalPages);
                        }
                        else
                        {
                            item.Pages = "N/A";
                        }

                        // Size
                        long sizeBytes = 0;
                        if (mo["Size"] != null) long.TryParse(mo["Size"].ToString(), out sizeBytes);
                        if (sizeBytes > 1024 * 1024)
                        {
                            item.Size = string.Format("{0:0.0} MB", sizeBytes / 1048576.0);
                        }
                        else if (sizeBytes > 1024)
                        {
                            item.Size = string.Format("{0:0.0} KB", sizeBytes / 1024.0);
                        }
                        else
                        {
                            item.Size = string.Format("{0} B", sizeBytes);
                        }

                        // Status & Stuck detection
                        string status = (mo["Status"] != null) ? mo["Status"].ToString() : "Spooling";
                        string jobStatus = (mo["JobStatus"] != null) ? mo["JobStatus"].ToString() : "";

                        if (!string.IsNullOrEmpty(jobStatus))
                        {
                            status = jobStatus;
                        }

                        item.Status = status;

                        // Check if stuck
                        if (status.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            status.IndexOf("Deleting", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            status.IndexOf("Offline", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            status.IndexOf("User Intervention", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            item.IsStuck = true;
                        }

                        // Submitted Time
                        if (mo["TimeSubmitted"] != null)
                        {
                            string dmtf = mo["TimeSubmitted"].ToString();
                            try
                            {
                                DateTime dt = ManagementDateTimeConverter.ToDateTime(dmtf);
                                item.SubmittedTime = dt.ToString("HH:mm:ss  yyyy-MM-dd");
                                if ((DateTime.Now - dt).TotalMinutes > 10)
                                {
                                    item.IsStuck = true; // Job older than 10 mins without finishing
                                }
                            }
                            catch
                            {
                                item.SubmittedTime = dmtf;
                            }
                        }

                        jobs.Add(item);
                    }
                }
            }
            catch { }

            return jobs;
        }

        // ==========================================
        // 5. CANCEL SPECIFIC JOB
        // ==========================================
        public static bool CancelJob(string printerName, int jobId)
        {
            // Try Win32 SetJob first
            IntPtr hPrinter = IntPtr.Zero;
            try
            {
                if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                {
                    bool ok = SetJob(hPrinter, jobId, 0, IntPtr.Zero, JOB_CONTROL_DELETE);
                    if (ok) return true;
                }
            }
            catch { }
            finally
            {
                if (hPrinter != IntPtr.Zero) ClosePrinter(hPrinter);
            }

            // Fallback via WMI
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    string.Format("SELECT * FROM Win32_PrintJob WHERE JobId = {0}", jobId)))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        mo.Delete();
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        // ==========================================
        // 6. PRINT TEST PAGE
        // ==========================================
        public static bool PrintTestPage(string printerName)
        {
            try
            {
                // Official Microsoft printui test page command
                ProcessStartInfo psi = new ProcessStartInfo("rundll32.exe", string.Format("printui.dll,PrintUIEntry /k /n \"{0}\"", printerName));
                psi.UseShellExecute = true;
                Process.Start(psi);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
