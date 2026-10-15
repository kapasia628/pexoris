using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PexorisSafeModeLauncher
{
    public enum SafeBootMode
    {
        Normal,
        Minimal,
        Network,
        CommandPrompt
    }

    public class BcdStatus
    {
        public SafeBootMode Mode = SafeBootMode.Normal;
        public string RawSafeBoot = "None";
        public bool AlternateShell = false;
        public string BootMenuPolicy = "Standard";
        public bool RecoveryEnabled = true;
        public string BitLockerStatus = "Unknown";
        public string WindowsVersion = "Windows 11 / 10";
        public List<BcdItem> Items = new List<BcdItem>();
    }

    public class BcdItem
    {
        public string Parameter { get; set; }
        public string Value { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }

        public BcdItem(string param, string val, string status, string cat)
        {
            Parameter = param;
            Value = val;
            Status = status;
            Category = cat;
        }
    }

    public static class BootEngine
    {
        public static BcdStatus Audit()
        {
            BcdStatus status = new BcdStatus();

            try
            {
                string bcdOutput = RunProcess("bcdedit.exe", "/enum {current}");
                if (string.IsNullOrEmpty(bcdOutput))
                {
                    bcdOutput = RunProcess("bcdedit.exe", "/enum");
                }

                // Parse SafeBoot
                Match matchSafe = Regex.Match(bcdOutput, @"safeboot\s+(\w+)", RegexOptions.IgnoreCase);
                if (matchSafe.Success)
                {
                    status.RawSafeBoot = matchSafe.Groups[1].Value.Trim();
                    if (status.RawSafeBoot.Equals("minimal", StringComparison.OrdinalIgnoreCase))
                        status.Mode = SafeBootMode.Minimal;
                    else if (status.RawSafeBoot.Equals("network", StringComparison.OrdinalIgnoreCase))
                        status.Mode = SafeBootMode.Network;
                }
                else
                {
                    status.RawSafeBoot = "None (Normal Boot)";
                    status.Mode = SafeBootMode.Normal;
                }

                // Parse AlternateShell
                Match matchAlt = Regex.Match(bcdOutput, @"safebootalternateshell\s+(\w+)", RegexOptions.IgnoreCase);
                if (matchAlt.Success && matchAlt.Groups[1].Value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    status.AlternateShell = true;
                    if (status.Mode == SafeBootMode.Minimal)
                    {
                        status.Mode = SafeBootMode.CommandPrompt;
                    }
                }

                // Parse BootMenuPolicy
                Match matchPolicy = Regex.Match(bcdOutput, @"bootmenupolicy\s+(\w+)", RegexOptions.IgnoreCase);
                if (matchPolicy.Success)
                {
                    status.BootMenuPolicy = matchPolicy.Groups[1].Value.Trim();
                }

                // Parse RecoveryEnabled
                Match matchRec = Regex.Match(bcdOutput, @"recoveryenabled\s+(\w+)", RegexOptions.IgnoreCase);
                if (matchRec.Success)
                {
                    status.RecoveryEnabled = matchRec.Groups[1].Value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
                }

                // Parse BitLocker Status
                string bdeOutput = RunProcess("manage-bde.exe", "-status C:");
                if (bdeOutput.IndexOf("Protection On", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    status.BitLockerStatus = "Protection On (Locked)";
                }
                else if (bdeOutput.IndexOf("Protection Off", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    status.BitLockerStatus = "Protection Off (Safe)";
                }
                else if (bdeOutput.IndexOf("Protection Suspended", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    status.BitLockerStatus = "Protection Suspended";
                }
                else
                {
                    status.BitLockerStatus = "Not Encrypted / Disabled";
                }

                // Add Items for ListView
                string modeText = "Normal Startup";
                string modeStatus = "Normal";
                if (status.Mode == SafeBootMode.Minimal) { modeText = "Minimal Safe Mode"; modeStatus = "Active"; }
                else if (status.Mode == SafeBootMode.Network) { modeText = "Safe Mode with Networking"; modeStatus = "Active"; }
                else if (status.Mode == SafeBootMode.CommandPrompt) { modeText = "Safe Mode with Command Prompt"; modeStatus = "Active"; }

                status.Items.Add(new BcdItem("Boot Target", modeText, modeStatus, "BCD Configuration"));
                status.Items.Add(new BcdItem("safeboot", status.RawSafeBoot, status.Mode == SafeBootMode.Normal ? "Clean" : "Modified", "BCD Flags"));
                status.Items.Add(new BcdItem("safebootalternateshell", status.AlternateShell ? "Yes (CMD.exe)" : "No (Explorer.exe)", status.AlternateShell ? "Active" : "Default", "Shell"));
                status.Items.Add(new BcdItem("bootmenupolicy", status.BootMenuPolicy, status.BootMenuPolicy.Equals("Legacy", StringComparison.OrdinalIgnoreCase) ? "F8 Menu Enabled" : "Standard UEFI", "Boot Menu"));
                status.Items.Add(new BcdItem("recoveryenabled", status.RecoveryEnabled ? "Yes" : "No", status.RecoveryEnabled ? "Enabled" : "Disabled", "WinRE"));
                status.Items.Add(new BcdItem("BitLocker Drive C:", status.BitLockerStatus, status.BitLockerStatus.Contains("Locked") ? "Warning" : "Safe", "Security"));

                // Query OS Version
                status.WindowsVersion = Environment.OSVersion.VersionString;
                status.Items.Add(new BcdItem("Operating System", status.WindowsVersion, "Ready", "System"));
            }
            catch (Exception ex)
            {
                status.Items.Add(new BcdItem("Audit Error", ex.Message, "Error", "Diagnostics"));
            }

            return status;
        }

        public static string SetSafeBoot(SafeBootMode mode)
        {
            try
            {
                switch (mode)
                {
                    case SafeBootMode.Minimal:
                        RunProcess("bcdedit.exe", "/set {current} safeboot minimal");
                        RunProcess("bcdedit.exe", "/deletevalue {current} safebootalternateshell");
                        return "Configured BCD for Minimal Safe Mode successfully.";

                    case SafeBootMode.Network:
                        RunProcess("bcdedit.exe", "/set {current} safeboot network");
                        RunProcess("bcdedit.exe", "/deletevalue {current} safebootalternateshell");
                        return "Configured BCD for Safe Mode with Networking successfully.";

                    case SafeBootMode.CommandPrompt:
                        RunProcess("bcdedit.exe", "/set {current} safeboot minimal");
                        RunProcess("bcdedit.exe", "/set {current} safebootalternateshell yes");
                        return "Configured BCD for Safe Mode with Command Prompt successfully.";

                    case SafeBootMode.Normal:
                    default:
                        return RestoreNormalBoot();
                }
            }
            catch (Exception ex)
            {
                return "Error configuring Safe Mode: " + ex.Message;
            }
        }

        public static string RestoreNormalBoot()
        {
            try
            {
                RunProcess("bcdedit.exe", "/deletevalue {current} safeboot");
                RunProcess("bcdedit.exe", "/deletevalue {current} safebootalternateshell");
                return "Cleared all Safe Mode flags. Windows will boot normally.";
            }
            catch (Exception ex)
            {
                return "Error restoring normal boot: " + ex.Message;
            }
        }

        public static string ToggleLegacyF8(bool enable)
        {
            try
            {
                if (enable)
                {
                    RunProcess("bcdedit.exe", "/set {default} bootmenupolicy legacy");
                    return "Enabled Legacy F8 Boot Menu on startup.";
                }
                else
                {
                    RunProcess("bcdedit.exe", "/set {default} bootmenupolicy standard");
                    return "Restored Standard Windows UEFI boot menu.";
                }
            }
            catch (Exception ex)
            {
                return "Error toggling boot menu policy: " + ex.Message;
            }
        }

        public static void RebootNow()
        {
            RunProcess("shutdown.exe", "/r /t 0");
        }

        public static void RebootUefi()
        {
            RunProcess("shutdown.exe", "/r /fw /t 0");
        }

        public static void RebootWinRE()
        {
            RunProcess("shutdown.exe", "/r /o /t 0");
        }

        private static string RunProcess(string exe, string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(4000);
                    string output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd();
                    return string.IsNullOrEmpty(output) ? error : output;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
