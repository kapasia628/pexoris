using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Win32;

namespace PexorisTelemetryBlocker
{
    public class TelemetryItem
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string CurrentStatus { get; set; }
        public bool IsBlocked { get; set; }
        public string RiskLevel { get; set; }
        public string Description { get; set; }
    }

    public static class TelemetryEngine
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern uint DnsFlushResolverCache();

        public static List<TelemetryItem> ScanTelemetry()
        {
            var list = new List<TelemetryItem>();

            // 1. SERVICES
            list.Add(CheckService("DiagTrack", "Connected User Experiences & Telemetry", 
                "Primary telemetry agent collecting system events, crash dumps, and usage data.", "Critical"));
            list.Add(CheckService("dmwappushservice", "Device Management WAP Push Routing", 
                "Wireless Application Protocol push message telemetry daemon.", "High"));

            // 2. GROUP POLICIES
            list.Add(CheckRegPolicy(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0,
                "OS Diagnostic Telemetry Level", "Forces Windows Telemetry level to 0 (Security / Off).", "Critical"));

            list.Add(CheckRegPolicy(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy", 1,
                "Windows Advertising ID Tracking", "Blocks user profiling and personalized advertising ID beacons.", "High"));

            list.Add(CheckRegPolicy(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DoNotShowFeedbackNotifications", 1,
                "Feedback Prompts & Surveys", "Suppresses Windows customer satisfaction telemetry prompts.", "Medium"));

            list.Add(CheckRegPolicy(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0,
                "Cortana Background Listening", "Disables voice and text assistant telemetry transmission.", "Medium"));

            list.Add(CheckRegPolicy(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", 1,
                "Start Menu Bing Cloud Queries", "Prevents local Start Menu searches from leaking queries to Bing.", "High"));

            list.Add(CheckRegPolicy(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb", 0,
                "Connected Search Telemetry", "Restricts desktop shell search exclusively to offline local files.", "High"));

            // 3. SCHEDULED TASKS
            list.Add(CheckScheduledTask(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                "Compatibility Appraiser Task", "Scans binary telemetry and uploads app compatibility telemetry to Microsoft.", "Critical"));

            list.Add(CheckScheduledTask(@"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
                "Program Data Telemetry Updater", "Collects telemetry for installed programs and updates.", "High"));

            list.Add(CheckScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                "CEIP Consolidator Task", "Consolidates CEIP metrics and sends data to Microsoft SQM servers.", "High"));

            list.Add(CheckScheduledTask(@"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                "USB CEIP Hardware Collector", "Uploads USB peripheral hardware telemetry to Microsoft.", "Medium"));

            list.Add(CheckScheduledTask(@"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                "Disk Diagnostic Telemetry", "Collects disk drive usage patterns and SMART diagnostic metrics.", "Medium"));

            return list;
        }

        private static TelemetryItem CheckService(string serviceName, string displayName, string desc, string risk)
        {
            var item = new TelemetryItem
            {
                Name = displayName,
                Category = "Service",
                RiskLevel = risk,
                Description = desc
            };

            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    bool isRunning = false;
                    try { isRunning = (sc.Status == ServiceControllerStatus.Running); } catch { }

                    int startType = GetServiceStartType(serviceName);
                    bool isStartDisabled = (startType == 4); // 4 = Disabled

                    if (isStartDisabled && !isRunning)
                    {
                        item.CurrentStatus = "Blocked (Disabled)";
                        item.IsBlocked = true;
                    }
                    else if (isRunning)
                    {
                        item.CurrentStatus = "Active (Running)";
                        item.IsBlocked = false;
                    }
                    else
                    {
                        item.CurrentStatus = "Stopped (Not Disabled)";
                        item.IsBlocked = false;
                    }
                }
            }
            catch
            {
                item.CurrentStatus = "Blocked / Not Present";
                item.IsBlocked = true;
            }

            return item;
        }

        private static int GetServiceStartType(string serviceName)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("Start");
                        if (val != null) return Convert.ToInt32(val);
                    }
                }
            }
            catch { }
            return 2; // Default Auto
        }

        private static TelemetryItem CheckRegPolicy(string subKey, string valueName, int expectedBlockedVal, string displayName, string desc, string risk)
        {
            var item = new TelemetryItem
            {
                Name = displayName,
                Category = "Group Policy",
                RiskLevel = risk,
                Description = desc
            };

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subKey, false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(valueName);
                        if (val != null && Convert.ToInt32(val) == expectedBlockedVal)
                        {
                            item.CurrentStatus = "Blocked (Enforced)";
                            item.IsBlocked = true;
                            return item;
                        }
                    }
                }
            }
            catch { }

            item.CurrentStatus = "Active (Unrestricted)";
            item.IsBlocked = false;
            return item;
        }

        private static TelemetryItem CheckScheduledTask(string taskPath, string displayName, string desc, string risk)
        {
            var item = new TelemetryItem
            {
                Name = displayName,
                Category = "Scheduled Task",
                RiskLevel = risk,
                Description = desc
            };

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("schtasks.exe", "/Query /TN \"" + taskPath + "\" /FO LIST")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(1500);

                    if (output.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        item.CurrentStatus = "Blocked (Disabled)";
                        item.IsBlocked = true;
                    }
                    else if (output.IndexOf("Ready", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             output.IndexOf("Running", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        item.CurrentStatus = "Active (Ready)";
                        item.IsBlocked = false;
                    }
                    else
                    {
                        item.CurrentStatus = "Blocked / Missing";
                        item.IsBlocked = true;
                    }
                }
            }
            catch
            {
                item.CurrentStatus = "Blocked (Not Found)";
                item.IsBlocked = true;
            }

            return item;
        }

        public static bool BlockAllTelemetry(out int countSuccess, out int countFail)
        {
            countSuccess = 0;
            countFail = 0;

            // 1. Disable Services
            string[] services = new string[] { "DiagTrack", "dmwappushservice" };
            foreach (string svc in services)
            {
                try
                {
                    RunCmd("sc.exe", "stop " + svc);
                    RunCmd("sc.exe", "config " + svc + " start= disabled");
                    countSuccess++;
                }
                catch { countFail++; }
            }

            // 2. Set Registry Policies
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection"))
                {
                    if (key != null)
                    {
                        key.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);
                        key.SetValue("DoNotShowFeedbackNotifications", 1, RegistryValueKind.DWord);
                        countSuccess += 2;
                    }
                }
            }
            catch { countFail += 2; }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisabledByGroupPolicy", 1, RegistryValueKind.DWord);
                        countSuccess++;
                    }
                }
            }
            catch { countFail++; }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search"))
                {
                    if (key != null)
                    {
                        key.SetValue("AllowCortana", 0, RegistryValueKind.DWord);
                        key.SetValue("DisableWebSearch", 1, RegistryValueKind.DWord);
                        key.SetValue("ConnectedSearchUseWeb", 0, RegistryValueKind.DWord);
                        countSuccess += 3;
                    }
                }
            }
            catch { countFail += 3; }

            // 3. Disable Scheduled Tasks
            string[] tasks = new string[]
            {
                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
                @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"
            };

            foreach (string task in tasks)
            {
                try
                {
                    RunCmd("schtasks.exe", "/Change /TN \"" + task + "\" /Disable");
                    countSuccess++;
                }
                catch { countFail++; }
            }

            try { DnsFlushResolverCache(); } catch { }
            return countSuccess > 0;
        }

        public static bool RestoreDefaults(out int countSuccess, out int countFail)
        {
            countSuccess = 0;
            countFail = 0;

            // 1. Enable Services
            try
            {
                RunCmd("sc.exe", "config DiagTrack start= auto");
                RunCmd("sc.exe", "start DiagTrack");
                countSuccess++;
            }
            catch { countFail++; }

            try
            {
                RunCmd("sc.exe", "config dmwappushservice start= demand");
                countSuccess++;
            }
            catch { countFail++; }

            // 2. Remove Registry Overrides
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("AllowTelemetry"); } catch { }
                        try { key.DeleteValue("DoNotShowFeedbackNotifications"); } catch { }
                        countSuccess += 2;
                    }
                }
            }
            catch { countFail += 2; }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("DisabledByGroupPolicy"); } catch { }
                        countSuccess++;
                    }
                }
            }
            catch { countFail++; }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("AllowCortana"); } catch { }
                        try { key.DeleteValue("DisableWebSearch"); } catch { }
                        try { key.DeleteValue("ConnectedSearchUseWeb"); } catch { }
                        countSuccess += 3;
                    }
                }
            }
            catch { countFail += 3; }

            // 3. Enable Scheduled Tasks
            string[] tasks = new string[]
            {
                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
                @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"
            };

            foreach (string task in tasks)
            {
                try
                {
                    RunCmd("schtasks.exe", "/Change /TN \"" + task + "\" /Enable");
                    countSuccess++;
                }
                catch { countFail++; }
            }

            try { DnsFlushResolverCache(); } catch { }
            return countSuccess > 0;
        }

        private static void RunCmd(string exe, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo(exe, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using (Process p = Process.Start(psi))
            {
                p.WaitForExit(3000);
            }
        }
    }
}
