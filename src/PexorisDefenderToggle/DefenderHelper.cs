using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace PexorisDefenderToggle
{
    public enum ExclusionType
    {
        FolderPath,
        ProcessName,
        Extension
    }

    public class DefenderExclusionItem
    {
        public string Target { get; set; }
        public ExclusionType Type { get; set; }
        public string TypeDisplay { get; set; }
        public bool ExistsOnDisk { get; set; }

        public DefenderExclusionItem(string target, ExclusionType type)
        {
            Target = target;
            Type = type;
            TypeDisplay = type == ExclusionType.FolderPath ? "Folder / Path" : (type == ExclusionType.ProcessName ? "Process" : "Extension");
            ExistsOnDisk = true;
            if (type == ExclusionType.FolderPath)
            {
                ExistsOnDisk = Directory.Exists(target) || File.Exists(target);
            }
        }
    }

    public class DefenderStatusInfo
    {
        public bool IsRealtimeProtectionEnabled { get; set; }
        public bool IsTamperProtectionDetected { get; set; }
        public int ExclusionCount { get; set; }
        public string StatusSummary { get; set; }

        public DefenderStatusInfo()
        {
            IsRealtimeProtectionEnabled = true;
            IsTamperProtectionDetected = false;
            ExclusionCount = 0;
            StatusSummary = "Active • Guarding System";
        }
    }

    public static class DefenderHelper
    {
        private const string REG_POLICY_DEFENDER = @"SOFTWARE\Policies\Microsoft\Windows Defender";
        private const string REG_POLICY_REALTIME = @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection";
        private const string REG_REALTIME_KEY = @"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection";
        private const string REG_EXCLUSION_PATHS = @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths";
        private const string REG_EXCLUSION_PROCS = @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Processes";

        public static DefenderStatusInfo GetDefenderStatus()
        {
            DefenderStatusInfo info = new DefenderStatusInfo();

            try
            {
                // 1. Check policies
                using (RegistryKey polKey = Registry.LocalMachine.OpenSubKey(REG_POLICY_REALTIME, false))
                {
                    if (polKey != null)
                    {
                        object val = polKey.GetValue("DisableRealtimeMonitoring");
                        if (val != null && Convert.ToInt32(val) == 1)
                        {
                            info.IsRealtimeProtectionEnabled = false;
                            info.StatusSummary = "Paused via Policy • Developer Mode";
                            return info;
                        }
                    }
                }

                // 2. PowerShell status check
                string output = RunPowerShellCommand("(Get-MpPreference).DisableRealtimeMonitoring");
                if (!string.IsNullOrEmpty(output))
                {
                    string trimmed = output.Trim();
                    if (trimmed.Equals("True", StringComparison.OrdinalIgnoreCase))
                    {
                        info.IsRealtimeProtectionEnabled = false;
                        info.StatusSummary = "Paused • Real-time Scans Inactive";
                    }
                    else
                    {
                        info.IsRealtimeProtectionEnabled = true;
                        info.StatusSummary = "Active • Full Real-time Protection";
                    }
                }
            }
            catch
            {
                info.StatusSummary = "Status check error";
            }

            return info;
        }

        public static bool SetRealtimeProtection(bool enable)
        {
            bool disableParam = !enable;
            try
            {
                // Attempt Policy Registry configuration first
                using (RegistryKey polKey = Registry.LocalMachine.CreateSubKey(REG_POLICY_REALTIME))
                {
                    if (polKey != null)
                    {
                        polKey.SetValue("DisableRealtimeMonitoring", disableParam ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }

            try
            {
                // Official Windows Defender PowerShell preference cmdlet
                string cmd = string.Format("Set-MpPreference -DisableRealtimeMonitoring ${0}", disableParam ? "true" : "false");
                RunPowerShellCommand(cmd);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<DefenderExclusionItem> GetAllExclusions()
        {
            List<DefenderExclusionItem> list = new List<DefenderExclusionItem>();

            // 1. Try reading registry exclusions
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REG_EXCLUSION_PATHS, false))
                {
                    if (key != null)
                    {
                        foreach (string name in key.GetValueNames())
                        {
                            if (!string.IsNullOrEmpty(name))
                            {
                                list.Add(new DefenderExclusionItem(name, ExclusionType.FolderPath));
                            }
                        }
                    }
                }

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REG_EXCLUSION_PROCS, false))
                {
                    if (key != null)
                    {
                        foreach (string name in key.GetValueNames())
                        {
                            if (!string.IsNullOrEmpty(name))
                            {
                                list.Add(new DefenderExclusionItem(name, ExclusionType.ProcessName));
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. If registry is locked by Tamper Protection, query via PowerShell
            if (list.Count == 0)
            {
                try
                {
                    string psOutput = RunPowerShellCommand("$p = (Get-MpPreference).ExclusionPath; if($p) { $p -join ';;;' }; $pr = (Get-MpPreference).ExclusionProcess; if($pr) { '---PROCESSES---;;;' + ($pr -join ';;;') }");
                    if (!string.IsNullOrEmpty(psOutput))
                    {
                        string[] sections = psOutput.Split(new string[] { "---PROCESSES---;;;" }, StringSplitOptions.None);
                        if (sections.Length > 0 && !string.IsNullOrEmpty(sections[0]))
                        {
                            string[] paths = sections[0].Split(new string[] { ";;;" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string p in paths)
                            {
                                string clean = p.Trim();
                                if (!string.IsNullOrEmpty(clean) && !clean.Contains("Must be an administrator"))
                                {
                                    list.Add(new DefenderExclusionItem(clean, ExclusionType.FolderPath));
                                }
                            }
                        }

                        if (sections.Length > 1 && !string.IsNullOrEmpty(sections[1]))
                        {
                            string[] procs = sections[1].Split(new string[] { ";;;" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string pr in procs)
                            {
                                string clean = pr.Trim();
                                if (!string.IsNullOrEmpty(clean) && !clean.Contains("Must be an administrator"))
                                {
                                    list.Add(new DefenderExclusionItem(clean, ExclusionType.ProcessName));
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return list;
        }

        public static bool AddFolderExclusion(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return false;
            try
            {
                string cmd = string.Format("Add-MpPreference -ExclusionPath '{0}'", folderPath.Replace("'", "''"));
                RunPowerShellCommand(cmd);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool RemoveFolderExclusion(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return false;
            try
            {
                string cmd = string.Format("Remove-MpPreference -ExclusionPath '{0}'", folderPath.Replace("'", "''"));
                RunPowerShellCommand(cmd);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool AddProcessExclusion(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return false;
            try
            {
                string cmd = string.Format("Add-MpPreference -ExclusionProcess '{0}'", processName.Replace("'", "''"));
                RunPowerShellCommand(cmd);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool RemoveProcessExclusion(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return false;
            try
            {
                string cmd = string.Format("Remove-MpPreference -ExclusionProcess '{0}'", processName.Replace("'", "''"));
                RunPowerShellCommand(cmd);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool WhitelistDevCompilersPreset()
        {
            string[] compilers = new string[] {
                "rustc.exe", "cargo.exe", "csc.exe", "gcc.exe", "cl.exe", "go.exe", "node.exe"
            };

            try
            {
                foreach (string comp in compilers)
                {
                    AddProcessExclusion(comp);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string RunPowerShellCommand(string script)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" + script + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(3000);
                    return output;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
