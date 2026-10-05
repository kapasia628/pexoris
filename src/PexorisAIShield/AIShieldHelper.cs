using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace Pexoris.AIShield
{
    public class ShieldItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public bool IsShielded { get; set; }
        public string PrivacyImpact { get; set; }
        public string RegistryPath { get; set; }
    }

    public static class AIShieldHelper
    {
        public static List<ShieldItem> ScanAll()
        {
            var list = new List<ShieldItem>();

            // 1. Windows Recall
            list.Add(new ShieldItem
            {
                Id = "recall",
                Name = "Windows Recall (AI Desktop Snapshots)",
                Category = "AI & Vision",
                Description = "Disables continuous background snapshots & optical analysis of screen activity.",
                IsShielded = IsRecallDisabled(),
                PrivacyImpact = "CRITICAL",
                RegistryPath = @"HKLM\Policies\Windows\WindowsAI\DisableAIDataAnalysis"
            });

            // 2. Windows Copilot
            list.Add(new ShieldItem
            {
                Id = "copilot",
                Name = "Windows Copilot & Taskbar Integration",
                Category = "AI & Chat",
                Description = "Completely disables Microsoft Copilot sidebar, taskbar button, and web queries.",
                IsShielded = IsCopilotDisabled(),
                PrivacyImpact = "HIGH",
                RegistryPath = @"HKCU\Policies\Windows\WindowsCopilot\TurnOffWindowsCopilot"
            });

            // 3. Start Menu Bing Web Search
            list.Add(new ShieldItem
            {
                Id = "bing_search",
                Name = "Bing Web Search & Cloud Start Suggestions",
                Category = "Search & Telemetry",
                Description = "Forces local search only. Prevents Start Menu typing from being sent to Bing servers.",
                IsShielded = IsBingSearchDisabled(),
                PrivacyImpact = "HIGH",
                RegistryPath = @"HKCU\Policies\Windows\Explorer\DisableSearchBoxSuggestions"
            });

            // 4. Diagnostic Data & DiagTrack Telemetry
            list.Add(new ShieldItem
            {
                Id = "diagtrack",
                Name = "Connected User Experiences & Telemetry",
                Category = "System Telemetry",
                Description = "Disables DiagTrack service and sets Windows diagnostic data collection to Security (0).",
                IsShielded = IsDiagTrackDisabled(),
                PrivacyImpact = "CRITICAL",
                RegistryPath = @"HKLM\Policies\Windows\DataCollection\AllowTelemetry"
            });

            // 5. Advertising ID
            list.Add(new ShieldItem
            {
                Id = "ad_id",
                Name = "Windows Advertising ID & User Profiling",
                Category = "Ad Profiling",
                Description = "Blocks unique device ad tracking ID used by Microsoft Store apps.",
                IsShielded = IsAdIdDisabled(),
                PrivacyImpact = "MEDIUM",
                RegistryPath = @"HKCU\CurrentVersion\AdvertisingInfo\Enabled"
            });

            // 6. Tailored Experiences
            list.Add(new ShieldItem
            {
                Id = "tailored_exp",
                Name = "Tailored Experiences & Feedback Tracking",
                Category = "Profiling",
                Description = "Stops Microsoft from analyzing apps and browsing habits to show targeted tips and ads.",
                IsShielded = IsTailoredExpDisabled(),
                PrivacyImpact = "MEDIUM",
                RegistryPath = @"HKCU\CurrentVersion\Privacy\TailoredExperiencesEnabled"
            });

            // 7. Cortana Voice Telemetry
            list.Add(new ShieldItem
            {
                Id = "cortana",
                Name = "Cortana & Voice Telemetry Collection",
                Category = "Voice & AI",
                Description = "Prevents speech recognition data and Cortana voice samples from being sent to cloud.",
                IsShielded = IsCortanaDisabled(),
                PrivacyImpact = "MEDIUM",
                RegistryPath = @"HKLM\Policies\Windows\Windows Search\AllowCortana"
            });

            // 8. Lock Screen Spotlight Ads
            list.Add(new ShieldItem
            {
                Id = "spotlight_ads",
                Name = "Lock Screen Spotlight Ads & Suggestions",
                Category = "UI Ads",
                Description = "Disables sponsored links, tips, and promotional overlays on Windows Lock Screen.",
                IsShielded = IsSpotlightAdsDisabled(),
                PrivacyImpact = "LOW",
                RegistryPath = @"HKCU\CurrentVersion\ContentDeliveryManager\RotatingLockScreen"
            });

            return list;
        }

        // ==========================================
        // INDIVIDUAL CHECKERS
        // ==========================================
        private static bool IsRecallDisabled()
        {
            int hklmVal = GetRegInt(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 0);
            int hkcuVal = GetRegInt(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 0);
            return hklmVal == 1 || hkcuVal == 1;
        }

        private static bool IsCopilotDisabled()
        {
            int hkcuVal = GetRegInt(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 0);
            int hklmVal = GetRegInt(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 0);
            return hkcuVal == 1 || hklmVal == 1;
        }

        private static bool IsBingSearchDisabled()
        {
            int val = GetRegInt(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 0);
            return val == 1;
        }

        private static bool IsDiagTrackDisabled()
        {
            int teleVal = GetRegInt(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", -1);
            return teleVal == 0;
        }

        private static bool IsAdIdDisabled()
        {
            int val = GetRegInt(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1);
            return val == 0;
        }

        private static bool IsTailoredExpDisabled()
        {
            int val = GetRegInt(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 1);
            return val == 0;
        }

        private static bool IsCortanaDisabled()
        {
            int val = GetRegInt(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 1);
            return val == 0;
        }

        private static bool IsSpotlightAdsDisabled()
        {
            int val = GetRegInt(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "RotatingLockScreenOverlayEnabled", 1);
            return val == 0;
        }

        // ==========================================
        // APPLY PROTECTION
        // ==========================================
        public static void SetProtection(string id, bool shield)
        {
            switch (id)
            {
                case "recall":
                    SetRegDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", shield ? 1 : 0);
                    SetRegDword(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", shield ? 1 : 0);
                    SetRegDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Recall", "AllowSnapshot", shield ? 0 : 1);
                    SetRegDword(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Recall", "AllowSnapshot", shield ? 0 : 1);
                    break;

                case "copilot":
                    SetRegDword(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", shield ? 1 : 0);
                    SetRegDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", shield ? 1 : 0);
                    SetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", shield ? 0 : 1);
                    break;

                case "bing_search":
                    SetRegDword(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", shield ? 1 : 0);
                    SetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", shield ? 0 : 1);
                    SetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", shield ? 0 : 1);
                    break;

                case "diagtrack":
                    SetRegDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", shield ? 0 : 1);
                    SetServiceState("DiagTrack", !shield);
                    break;

                case "ad_id":
                    SetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", shield ? 0 : 1);
                    break;

                case "tailored_exp":
                    SetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", shield ? 0 : 1);
                    break;

                case "cortana":
                    SetRegDword(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", shield ? 0 : 1);
                    break;

                case "spotlight_ads":
                    SetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "RotatingLockScreenOverlayEnabled", shield ? 0 : 1);
                    SetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled", shield ? 0 : 1);
                    break;
            }
        }

        public static void ApplyAll(bool shield)
        {
            string[] all = { "recall", "copilot", "bing_search", "diagtrack", "ad_id", "tailored_exp", "cortana", "spotlight_ads" };
            foreach (var id in all)
            {
                SetProtection(id, shield);
            }
        }

        public static void RestartExplorer()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("explorer"))
                {
                    try { p.Kill(); } catch { }
                }
                Process.Start("explorer.exe");
            }
            catch { }
        }

        // ==========================================
        // REGISTRY & SERVICE HELPERS
        // ==========================================
        private static int GetRegInt(RegistryKey root, string subKey, string valueName, int defaultVal)
        {
            try
            {
                using (RegistryKey key = root.OpenSubKey(subKey, false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(valueName);
                        if (val is int) return (int)val;
                    }
                }
            }
            catch { }
            return defaultVal;
        }

        private static void SetRegDword(RegistryKey root, string subKey, string valueName, int val)
        {
            try
            {
                using (RegistryKey key = root.CreateSubKey(subKey))
                {
                    if (key != null)
                    {
                        key.SetValue(valueName, val, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        private static void SetServiceState(string serviceName, bool start)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (start && sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                    }
                    else if (!start && sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                    }
                }
            }
            catch { }
        }
    }
}
