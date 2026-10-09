using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace PexorisPrivacyShield
{
    public class PrivacyRule
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TargetPath { get; set; }
        public bool IsRecommended { get; set; }
        public bool IsBlocked { get; set; }
        public string StatusText { get { return IsBlocked ? "Protected (Blocked)" : "Active (Tracking)"; } }

        public Func<bool> CheckStatus { get; set; }
        public Action ApplyBlock { get; set; }
        public Action RestoreDefault { get; set; }
    }

    public static class PrivacyEngine
    {
        public static List<PrivacyRule> GetRules()
        {
            List<PrivacyRule> rules = new List<PrivacyRule>();

            // 1. Windows Telemetry Data Collection
            rules.Add(new PrivacyRule
            {
                Id = "telemetry_datacollection",
                Category = "Diagnostic Telemetry",
                Name = "Windows Telemetry Data Collection",
                Description = "Disables diagnostic data payloads sent to Microsoft telemetry servers.",
                TargetPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection\AllowTelemetry",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 1, true) == 0,
                ApplyBlock = () => WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, true),
                RestoreDefault = () => DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", true)
            });

            // 2. Connected User Experiences (DiagTrack Service)
            rules.Add(new PrivacyRule
            {
                Id = "telemetry_diagtrack",
                Category = "Diagnostic Telemetry",
                Name = "Connected User Experiences (DiagTrack)",
                Description = "Disables the background DiagTrack service responsible for event logging.",
                TargetPath = @"HKLM\SYSTEM\CurrentControlSet\Services\DiagTrack\Start",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"SYSTEM\CurrentControlSet\Services\DiagTrack", "Start", 2, true) == 4,
                ApplyBlock = () =>
                {
                    WriteDword(@"SYSTEM\CurrentControlSet\Services\DiagTrack", "Start", 4, true);
                    RunCmd("net stop DiagTrack /y");
                },
                RestoreDefault = () =>
                {
                    WriteDword(@"SYSTEM\CurrentControlSet\Services\DiagTrack", "Start", 2, true);
                    RunCmd("net start DiagTrack");
                }
            });

            // 3. WAP Push Message Routing Service
            rules.Add(new PrivacyRule
            {
                Id = "telemetry_dmwappush",
                Category = "Diagnostic Telemetry",
                Name = "WAP Push Routing Service (dmwappushservice)",
                Description = "Disables dmwappushservice for enterprise telemetry dispatch.",
                TargetPath = @"HKLM\SYSTEM\CurrentControlSet\Services\dmwappushservice\Start",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"SYSTEM\CurrentControlSet\Services\dmwappushservice", "Start", 3, true) == 4,
                ApplyBlock = () =>
                {
                    WriteDword(@"SYSTEM\CurrentControlSet\Services\dmwappushservice", "Start", 4, true);
                    RunCmd("net stop dmwappushservice /y");
                },
                RestoreDefault = () => WriteDword(@"SYSTEM\CurrentControlSet\Services\dmwappushservice", "Start", 3, true)
            });

            // 4. Advertising ID for Tailored Ads
            rules.Add(new PrivacyRule
            {
                Id = "privacy_advertising_id",
                Category = "User Profiling & Ads",
                Name = "Unique Advertising ID Tracking",
                Description = "Blocks Windows apps from using your unique advertising ID for targeted ads.",
                TargetPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo\Enabled",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1, false) == 0,
                ApplyBlock = () => WriteDword(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, false),
                RestoreDefault = () => WriteDword(@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1, false)
            });

            // 5. Tailored Experiences & Diagnostic Feedback
            rules.Add(new PrivacyRule
            {
                Id = "privacy_tailored_exp",
                Category = "User Profiling & Ads",
                Name = "Tailored Diagnostic Experiences",
                Description = "Prevents Microsoft from using diagnostic data to offer personalized recommendations.",
                TargetPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy\TailoredExperiencesWithDiagnosticDataEnabled",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 1, false) == 0,
                ApplyBlock = () => WriteDword(@"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0, false),
                RestoreDefault = () => DeleteValue(@"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", false)
            });

            // 6. Activity History & Timeline Tracking
            rules.Add(new PrivacyRule
            {
                Id = "privacy_activity_history",
                Category = "User Profiling & Ads",
                Name = "Activity History & Cloud Timeline",
                Description = "Disables tracking of application usage and document activity history.",
                TargetPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\EnableActivityFeed",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 1, true) == 0,
                ApplyBlock = () =>
                {
                    WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0, true);
                    WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0, true);
                    WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0, true);
                },
                RestoreDefault = () =>
                {
                    DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", true);
                    DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", true);
                    DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", true);
                }
            });

            // 7. Bing Web Search in Start Menu
            rules.Add(new PrivacyRule
            {
                Id = "search_bing_integration",
                Category = "Search & Cloud Integration",
                Name = "Bing Web Search in Start Menu",
                Description = "Prevents Start Menu queries from transmitting keystrokes to Bing search servers.",
                TargetPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\DisableWebSearch",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", 0, true) == 1,
                ApplyBlock = () =>
                {
                    WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", 1, true);
                    WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb", 0, true);
                },
                RestoreDefault = () =>
                {
                    DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", true);
                    DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb", true);
                }
            });

            // 8. Cortana Voice Assistant
            rules.Add(new PrivacyRule
            {
                Id = "search_cortana",
                Category = "Search & Cloud Integration",
                Name = "Cortana Voice Assistant Telemetry",
                Description = "Disables Cortana background speech recognition and telemetry harvesting.",
                TargetPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\AllowCortana",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 1, true) == 0,
                ApplyBlock = () => WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0, true),
                RestoreDefault = () => DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", true)
            });

            // 9. Typing & Inking Telemetry (Keylogger Prevention)
            rules.Add(new PrivacyRule
            {
                Id = "input_typing_telemetry",
                Category = "Input & Keystroke Privacy",
                Name = "Typing & Inking Personalization Telemetry",
                Description = "Prevents Windows from transmitting handwritten notes and typed dictionary samples.",
                TargetPath = @"HKCU\Software\Microsoft\InputPersonalization\RestrictImplicitTextCollection",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"Software\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", 0, false) == 1,
                ApplyBlock = () =>
                {
                    WriteDword(@"Software\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", 1, false);
                    WriteDword(@"Software\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", 1, false);
                    WriteDword(@"Software\Microsoft\Personalization\Settings", "AcceptedPrivacyPolicy", 0, false);
                },
                RestoreDefault = () =>
                {
                    DeleteValue(@"Software\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", false);
                    DeleteValue(@"Software\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", false);
                }
            });

            // 10. Location Tracking & Sensor Service
            rules.Add(new PrivacyRule
            {
                Id = "sensor_location_tracking",
                Category = "Location & Sensors",
                Name = "System Location Tracking & Sensor Feed",
                Description = "Disables Windows location provider and hardware sensor geo-tracking.",
                TargetPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors\DisableLocation",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 0, true) == 1,
                ApplyBlock = () =>
                {
                    WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1, true);
                    WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableSensors", 1, true);
                },
                RestoreDefault = () =>
                {
                    DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", true);
                    DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableSensors", true);
                }
            });

            // 11. Customer Experience Improvement Program (CEIP)
            rules.Add(new PrivacyRule
            {
                Id = "telemetry_ceip_sqm",
                Category = "Diagnostic Telemetry",
                Name = "Customer Experience Improvement Program (SQM)",
                Description = "Disables Microsoft SQM Client automated instrumentation metrics.",
                TargetPath = @"HKLM\SOFTWARE\Policies\Microsoft\SQMClient\Windows\CEIPEnable",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 1, true) == 0,
                ApplyBlock = () => WriteDword(@"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 0, true),
                RestoreDefault = () => DeleteValue(@"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", true)
            });

            // 12. Windows Feedback Frequency Prompts
            rules.Add(new PrivacyRule
            {
                Id = "feedback_frequency",
                Category = "Diagnostic Telemetry",
                Name = "Windows Feedback Prompts Frequency",
                Description = "Forces Windows feedback generation frequency to Never (0 prompts).",
                TargetPath = @"HKCU\Software\Microsoft\Siuf\Rules\NumberOfSIUFInPeriod",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 1, false) == 0,
                ApplyBlock = () =>
                {
                    WriteDword(@"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0, false);
                    WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DoNotShowFeedbackNotifications", 1, true);
                },
                RestoreDefault = () =>
                {
                    DeleteValue(@"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", false);
                    DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DoNotShowFeedbackNotifications", true);
                }
            });

            // 13. App Diagnostic Access Permissions
            rules.Add(new PrivacyRule
            {
                Id = "app_diagnostic_access",
                Category = "Application Privacy",
                Name = "Third-Party App Diagnostics Access",
                Description = "Denies third-party Store applications from reading other apps' diagnostic info.",
                TargetPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsGetDiagnosticInfo",
                IsRecommended = true,
                CheckStatus = () => ReadDword(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsGetDiagnosticInfo", 0, true) == 2,
                ApplyBlock = () => WriteDword(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsGetDiagnosticInfo", 2, true),
                RestoreDefault = () => DeleteValue(@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsGetDiagnosticInfo", true)
            });

            // 14. Compatibility Appraiser Telemetry Task
            rules.Add(new PrivacyRule
            {
                Id = "task_compat_appraiser",
                Category = "Scheduled Telemetry Tasks",
                Name = "Microsoft Compatibility Appraiser Task",
                Description = "Disables daily scheduled telemetry assessment of installed software and binary hashes.",
                TargetPath = @"Task: \Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                IsRecommended = true,
                CheckStatus = () => IsTaskDisabled(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"),
                ApplyBlock = () => ToggleTask(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser", false),
                RestoreDefault = () => ToggleTask(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser", true)
            });

            // 15. CEIP Consolidator Telemetry Task
            rules.Add(new PrivacyRule
            {
                Id = "task_ceip_consolidator",
                Category = "Scheduled Telemetry Tasks",
                Name = "CEIP Consolidator Telemetry Task",
                Description = "Disables scheduled consolidation and upload of Windows usage metrics.",
                TargetPath = @"Task: \Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                IsRecommended = true,
                CheckStatus = () => IsTaskDisabled(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"),
                ApplyBlock = () => ToggleTask(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator", false),
                RestoreDefault = () => ToggleTask(@"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator", true)
            });

            // 16. Program Data Updater Task
            rules.Add(new PrivacyRule
            {
                Id = "task_program_data_updater",
                Category = "Scheduled Telemetry Tasks",
                Name = "Program Data Updater Inventory Task",
                Description = "Disables scheduled inventorying of application states and telemetry data.",
                TargetPath = @"Task: \Microsoft\Windows\Application Experience\ProgramDataUpdater",
                IsRecommended = true,
                CheckStatus = () => IsTaskDisabled(@"\Microsoft\Windows\Application Experience\ProgramDataUpdater"),
                ApplyBlock = () => ToggleTask(@"\Microsoft\Windows\Application Experience\ProgramDataUpdater", false),
                RestoreDefault = () => ToggleTask(@"\Microsoft\Windows\Application Experience\ProgramDataUpdater", true)
            });

            return rules;
        }

        public static void ScanAll(List<PrivacyRule> rules)
        {
            if (rules == null) return;
            foreach (var rule in rules)
            {
                try
                {
                    if (rule.CheckStatus != null)
                    {
                        rule.IsBlocked = rule.CheckStatus();
                    }
                }
                catch
                {
                    rule.IsBlocked = false;
                }
            }
        }

        #region Helper Methods

        private static int ReadDword(string subKey, string valueName, int defaultValue, bool isHklm)
        {
            try
            {
                RegistryKey root = isHklm ? Registry.LocalMachine : Registry.CurrentUser;
                using (RegistryKey key = root.OpenSubKey(subKey, false))
                {
                    if (key == null) return defaultValue;
                    object val = key.GetValue(valueName);
                    if (val is int) return (int)val;
                    if (val != null) return Convert.ToInt32(val);
                }
            }
            catch { }
            return defaultValue;
        }

        private static void WriteDword(string subKey, string valueName, int value, bool isHklm)
        {
            try
            {
                RegistryKey root = isHklm ? Registry.LocalMachine : Registry.CurrentUser;
                using (RegistryKey key = root.CreateSubKey(subKey))
                {
                    if (key != null)
                    {
                        key.SetValue(valueName, value, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        private static void DeleteValue(string subKey, string valueName, bool isHklm)
        {
            try
            {
                RegistryKey root = isHklm ? Registry.LocalMachine : Registry.CurrentUser;
                using (RegistryKey key = root.OpenSubKey(subKey, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(valueName, false);
                    }
                }
            }
            catch { }
        }

        private static bool IsTaskDisabled(string taskName)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("schtasks", "/Query /TN \"" + taskName + "\" /FO CSV /NH");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);
                    if (!string.IsNullOrEmpty(output))
                    {
                        return output.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
            }
            catch { }
            return false;
        }

        private static void ToggleTask(string taskName, bool enable)
        {
            try
            {
                string action = enable ? "/Enable" : "/Disable";
                ProcessStartInfo psi = new ProcessStartInfo("schtasks", "/Change /TN \"" + taskName + "\" " + action);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(3000);
                }
            }
            catch { }
        }

        private static void RunCmd(string cmd)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + cmd);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(4000);
                }
            }
            catch { }
        }

        #endregion
    }
}
