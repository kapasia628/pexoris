using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using Microsoft.Win32;

namespace PexorisServiceOptimizer
{
    public enum ServiceSafetyLevel
    {
        Safe,       // Safe to disable for 99.9% of users (Telemetry, Diagnostics, Bloat)
        Gaming,     // Recommended to disable/manual for gaming latency & low RAM
        Advanced,   // Manual tweaks (Windows Update, Spooler, Bluetooth)
        Critical    // System vital - protected from disabling
    }

    public enum ServiceStartupMode
    {
        Automatic = 2,
        Manual = 3,
        Disabled = 4,
        Unknown = 0
    }

    public class ServiceDefinition
    {
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public ServiceSafetyLevel Safety { get; set; }
        public ServiceStartupMode DefaultStartup { get; set; }
        public ServiceStartupMode SafeStartup { get; set; }
        public ServiceStartupMode GamingStartup { get; set; }
    }

    public class ServiceItem
    {
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public ServiceSafetyLevel Safety { get; set; }
        public ServiceStartupMode CurrentStartup { get; set; }
        public ServiceStartupMode DefaultStartup { get; set; }
        public ServiceControllerStatus Status { get; set; }
        public bool IsRunning
        {
            get { return Status == ServiceControllerStatus.Running; }
        }
        public bool IsInstalled { get; set; }
    }

    public static class ServiceHelper
    {
        private static readonly List<ServiceDefinition> Catalog = new List<ServiceDefinition>
        {
            // === SAFE TO DISABLE (Telemetry, Tracking, Bloat) ===
            new ServiceDefinition {
                ServiceName = "DiagTrack",
                DisplayName = "Connected User Experiences and Telemetry",
                Description = "Collects system usage and event data and uploads telemetry to Microsoft servers.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Automatic,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "dmwappushservice",
                DisplayName = "WAP Push Message Routing Service",
                Description = "Routes telemetry and MMS push messages; frequently triggers background wakeups.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "RetailDemo",
                DisplayName = "Retail Demo Service",
                Description = "Powers in-store store kiosk mode. Completely unused on personal and office PCs.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "MapsBroker",
                DisplayName = "Downloaded Maps Manager",
                Description = "Manages downloaded offline maps for Windows Maps app. Unnecessary if not using offline maps.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Automatic,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "RemoteRegistry",
                DisplayName = "Remote Registry Service",
                Description = "Allows remote users to modify registry settings. Major security vulnerability if left active.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Disabled,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "Fax",
                DisplayName = "Fax Service",
                Description = "Enables sending and receiving faxes using legacy telephone modems.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "WerSvc",
                DisplayName = "Windows Error Reporting Service",
                Description = "Generates memory dumps and crash error reports for transmission to Microsoft.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "wisvc",
                DisplayName = "Windows Insider Service",
                Description = "Provides infrastructure support for Windows Insider preview builds and ring updates.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "WalletService",
                DisplayName = "WalletService",
                Description = "Used by Microsoft Edge and Store apps to store payment credentials and coupons.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "diagnosticshub.standardcollector.service",
                DisplayName = "Microsoft Diagnostics Hub Standard Collector",
                Description = "Collects real-time ETW trace events for developer diagnostics and telemetry.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "WMPNetworkSvc",
                DisplayName = "Windows Media Player Network Sharing Service",
                Description = "Shares Windows Media Player libraries to other networked players using UPnP.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Disabled,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "SharedAccess",
                DisplayName = "Internet Connection Sharing (ICS)",
                Description = "Provides network address translation and DHCP on home networks for shared connections.",
                Safety = ServiceSafetyLevel.Safe,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Manual,
                GamingStartup = ServiceStartupMode.Manual
            },

            // === GAMING TWEAKS (Xbox bloat, Latency, Background disk indexing) ===
            new ServiceDefinition {
                ServiceName = "XblAuthManager",
                DisplayName = "Xbox Live Auth Manager",
                Description = "Authentication service for Xbox Live. Disable if not playing Xbox Game Pass titles.",
                Safety = ServiceSafetyLevel.Gaming,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Manual,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "XblGameSave",
                DisplayName = "Xbox Live Game Save",
                Description = "Syncs cloud save data for Xbox Live games. Consumes background CPU cycles.",
                Safety = ServiceSafetyLevel.Gaming,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Manual,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "XboxNetApiSvc",
                DisplayName = "Xbox Live Networking Service",
                Description = "Supports peer-to-peer multiplayer networking for Microsoft Store and Xbox games.",
                Safety = ServiceSafetyLevel.Gaming,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Manual,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "XboxGipSvc",
                DisplayName = "Xbox Accessory Management Service",
                Description = "Manages connected wired/wireless Xbox controllers and headset attachments.",
                Safety = ServiceSafetyLevel.Gaming,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Manual,
                GamingStartup = ServiceStartupMode.Manual
            },
            new ServiceDefinition {
                ServiceName = "SysMain",
                DisplayName = "SysMain (SuperFetch)",
                Description = "Preloads apps into standby RAM. Highly recommended to keep on HDDs; optional on fast NVMe SSDs.",
                Safety = ServiceSafetyLevel.Gaming,
                DefaultStartup = ServiceStartupMode.Automatic,
                SafeStartup = ServiceStartupMode.Automatic,
                GamingStartup = ServiceStartupMode.Automatic
            },
            new ServiceDefinition {
                ServiceName = "WSearch",
                DisplayName = "Windows Search Indexer",
                Description = "Provides content indexing and fast file search. Can cause 100% disk usage micro-stutter in games.",
                Safety = ServiceSafetyLevel.Gaming,
                DefaultStartup = ServiceStartupMode.Automatic,
                SafeStartup = ServiceStartupMode.Automatic,
                GamingStartup = ServiceStartupMode.Manual
            },
            new ServiceDefinition {
                ServiceName = "SensorService",
                DisplayName = "Sensor Service",
                Description = "Monitors ambient light, motion, and accelerometer hardware. Useless on desktop gaming PCs.",
                Safety = ServiceSafetyLevel.Gaming,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Manual,
                GamingStartup = ServiceStartupMode.Disabled
            },
            new ServiceDefinition {
                ServiceName = "SensrSvc",
                DisplayName = "Sensor Monitoring Service",
                Description = "Monitors hardware sensors to expose data to location and screen auto-rotate.",
                Safety = ServiceSafetyLevel.Gaming,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Manual,
                GamingStartup = ServiceStartupMode.Disabled
            },

            // === ADVANCED / OPTIONAL SERVICES ===
            new ServiceDefinition {
                ServiceName = "Spooler",
                DisplayName = "Print Spooler",
                Description = "Manages print queues and handles printer communication. Disable only if you have zero printers.",
                Safety = ServiceSafetyLevel.Advanced,
                DefaultStartup = ServiceStartupMode.Automatic,
                SafeStartup = ServiceStartupMode.Automatic,
                GamingStartup = ServiceStartupMode.Automatic
            },
            new ServiceDefinition {
                ServiceName = "bthserv",
                DisplayName = "Bluetooth Support Service",
                Description = "Discovery and association of remote Bluetooth devices. Keep running if using BT mice/controllers.",
                Safety = ServiceSafetyLevel.Advanced,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Manual,
                GamingStartup = ServiceStartupMode.Manual
            },
            new ServiceDefinition {
                ServiceName = "wuauserv",
                DisplayName = "Windows Update",
                Description = "Enables the detection, download, and installation of updates for Windows and other programs.",
                Safety = ServiceSafetyLevel.Advanced,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Manual,
                GamingStartup = ServiceStartupMode.Manual
            },
            new ServiceDefinition {
                ServiceName = "BITS",
                DisplayName = "Background Intelligent Transfer Service",
                Description = "Transfers files in the background using idle network bandwidth for Windows Update.",
                Safety = ServiceSafetyLevel.Advanced,
                DefaultStartup = ServiceStartupMode.Manual,
                SafeStartup = ServiceStartupMode.Manual,
                GamingStartup = ServiceStartupMode.Manual
            },

            // === CRITICAL SYSTEM SERVICES (Protected from Accidental Disabling) ===
            new ServiceDefinition {
                ServiceName = "RpcSs",
                DisplayName = "Remote Procedure Call (RPC)",
                Description = "Core Windows subsystem backbone. Terminating RPC will instantly crash the OS.",
                Safety = ServiceSafetyLevel.Critical,
                DefaultStartup = ServiceStartupMode.Automatic,
                SafeStartup = ServiceStartupMode.Automatic,
                GamingStartup = ServiceStartupMode.Automatic
            },
            new ServiceDefinition {
                ServiceName = "EventLog",
                DisplayName = "Windows Event Log",
                Description = "Manages event logs and tracing. Critical for security auditing and driver initialization.",
                Safety = ServiceSafetyLevel.Critical,
                DefaultStartup = ServiceStartupMode.Automatic,
                SafeStartup = ServiceStartupMode.Automatic,
                GamingStartup = ServiceStartupMode.Automatic
            },
            new ServiceDefinition {
                ServiceName = "AudioSrv",
                DisplayName = "Windows Audio",
                Description = "Manages audio playback and devices for Windows-based programs.",
                Safety = ServiceSafetyLevel.Critical,
                DefaultStartup = ServiceStartupMode.Automatic,
                SafeStartup = ServiceStartupMode.Automatic,
                GamingStartup = ServiceStartupMode.Automatic
            },
            new ServiceDefinition {
                ServiceName = "Dnscache",
                DisplayName = "DNS Client",
                Description = "Caches DNS names and resolves web hostnames for internet connectivity.",
                Safety = ServiceSafetyLevel.Critical,
                DefaultStartup = ServiceStartupMode.Automatic,
                SafeStartup = ServiceStartupMode.Automatic,
                GamingStartup = ServiceStartupMode.Automatic
            }
        };

        public static List<ServiceItem> ScanServices()
        {
            var results = new List<ServiceItem>();

            // Get running services dictionary for speed
            var scMap = new Dictionary<string, ServiceControllerStatus>(StringComparer.OrdinalIgnoreCase);
            try
            {
                ServiceController[] all = ServiceController.GetServices();
                foreach (var sc in all)
                {
                    try { scMap[sc.ServiceName] = sc.Status; } catch { }
                }
            }
            catch { }

            foreach (var def in Catalog)
            {
                var item = new ServiceItem
                {
                    ServiceName = def.ServiceName,
                    DisplayName = def.DisplayName,
                    Description = def.Description,
                    Safety = def.Safety,
                    DefaultStartup = def.DefaultStartup,
                    CurrentStartup = ServiceStartupMode.Unknown,
                    Status = ServiceControllerStatus.Stopped,
                    IsInstalled = false
                };

                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + def.ServiceName))
                    {
                        if (key != null)
                        {
                            item.IsInstalled = true;
                            object val = key.GetValue("Start");
                            if (val is int)
                            {
                                int startVal = (int)val;
                                if (Enum.IsDefined(typeof(ServiceStartupMode), startVal))
                                    item.CurrentStartup = (ServiceStartupMode)startVal;
                            }
                        }
                    }
                }
                catch { }

                if (scMap.ContainsKey(def.ServiceName))
                {
                    item.IsInstalled = true;
                    item.Status = scMap[def.ServiceName];
                }

                results.Add(item);
            }

            return results;
        }

        public static bool SetStartupMode(string serviceName, ServiceStartupMode mode)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName, true))
                {
                    if (key != null)
                    {
                        key.SetValue("Start", (int)mode, RegistryValueKind.DWord);
                        return true;
                    }
                }
            }
            catch { }

            // Fallback via sc.exe
            try
            {
                string scType = "demand";
                if (mode == ServiceStartupMode.Automatic) scType = "auto";
                else if (mode == ServiceStartupMode.Disabled) scType = "disabled";

                ProcessStartInfo psi = new ProcessStartInfo("sc.exe", string.Format("config \"{0}\" start= {1}", serviceName, scType))
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(3000);
                    return p.ExitCode == 0;
                }
            }
            catch { }

            return false;
        }

        public static bool StopServiceSafe(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(3));
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static bool StartServiceSafe(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(4));
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static int ApplySafeProfile()
        {
            int modified = 0;
            foreach (var def in Catalog)
            {
                if (def.Safety == ServiceSafetyLevel.Safe && def.SafeStartup == ServiceStartupMode.Disabled)
                {
                    if (SetStartupMode(def.ServiceName, ServiceStartupMode.Disabled))
                    {
                        StopServiceSafe(def.ServiceName);
                        modified++;
                    }
                }
            }
            return modified;
        }

        public static int ApplyGamingProfile()
        {
            int modified = 0;
            foreach (var def in Catalog)
            {
                if (def.Safety == ServiceSafetyLevel.Safe || def.Safety == ServiceSafetyLevel.Gaming)
                {
                    if (SetStartupMode(def.ServiceName, def.GamingStartup))
                    {
                        if (def.GamingStartup == ServiceStartupMode.Disabled)
                            StopServiceSafe(def.ServiceName);
                        modified++;
                    }
                }
            }
            return modified;
        }

        public static int RestoreDefaults()
        {
            int modified = 0;
            foreach (var def in Catalog)
            {
                if (SetStartupMode(def.ServiceName, def.DefaultStartup))
                {
                    if (def.DefaultStartup == ServiceStartupMode.Automatic)
                    {
                        StartServiceSafe(def.ServiceName);
                    }
                    modified++;
                }
            }
            return modified;
        }

        public static string CreateBackupRegistryFile()
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pexoris", "ServiceOptimizer");
                Directory.CreateDirectory(folder);
                string filePath = Path.Combine(folder, "Services_Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".reg");

                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    sw.WriteLine("Windows Registry Editor Version 5.00");
                    sw.WriteLine();
                    sw.WriteLine("; Pexoris Service Optimizer Backup - " + DateTime.Now.ToString("F"));
                    sw.WriteLine();

                    foreach (var def in Catalog)
                    {
                        try
                        {
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + def.ServiceName))
                            {
                                if (key != null)
                                {
                                    object val = key.GetValue("Start");
                                    if (val != null)
                                    {
                                        sw.WriteLine(@"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\" + def.ServiceName + "]");
                                        sw.WriteLine(string.Format("\"Start\"=dword:{0:x8}", (int)val));
                                        sw.WriteLine();
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                return filePath;
            }
            catch
            {
                return null;
            }
        }
    }
}
