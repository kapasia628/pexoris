using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PexorisFirewallBlocker
{
    public class FirewallRuleItem
    {
        public string Name { get; set; }
        public string ApplicationName { get; set; }
        public string Direction { get; set; }
        public string Action { get; set; }
        public bool Enabled { get; set; }
        public string Profiles { get; set; }
        public bool IsPexorisRule { get; set; }

        public FirewallRuleItem()
        {
            Name = string.Empty;
            ApplicationName = string.Empty;
            Direction = "Outbound";
            Action = "Block";
            Enabled = true;
            Profiles = "All";
            IsPexorisRule = false;
        }
    }

    public class FirewallProfileStatus
    {
        public bool DomainEnabled { get; set; }
        public bool PrivateEnabled { get; set; }
        public bool PublicEnabled { get; set; }
        public int TotalBlockRules { get; set; }
        public int TotalPexorisRules { get; set; }
    }

    public enum RuleFilter
    {
        AllBlocked,
        PexorisOnly,
        AllActive
    }

    public static class FirewallEngine
    {
        private const int NET_FW_PROFILE2_DOMAIN = 1;
        private const int NET_FW_PROFILE2_PRIVATE = 2;
        private const int NET_FW_PROFILE2_PUBLIC = 4;
        private const int NET_FW_PROFILE2_ALL = 0x7FFFFFFF;

        private const int NET_FW_RULE_DIR_IN = 1;
        private const int NET_FW_RULE_DIR_OUT = 2;

        private const int NET_FW_ACTION_BLOCK = 0;
        private const int NET_FW_ACTION_ALLOW = 1;

        private static dynamic GetFirewallPolicy()
        {
            Type policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (policyType == null)
            {
                throw new InvalidOperationException("Windows Firewall COM interface (HNetCfg.FwPolicy2) is not registered on this system.");
            }
            return Activator.CreateInstance(policyType);
        }

        public static FirewallProfileStatus GetProfileStatus()
        {
            FirewallProfileStatus status = new FirewallProfileStatus();
            try
            {
                dynamic policy = GetFirewallPolicy();
                status.DomainEnabled = policy.FirewallEnabled[NET_FW_PROFILE2_DOMAIN];
                status.PrivateEnabled = policy.FirewallEnabled[NET_FW_PROFILE2_PRIVATE];
                status.PublicEnabled = policy.FirewallEnabled[NET_FW_PROFILE2_PUBLIC];

                int blockCount = 0;
                int pexorisCount = 0;

                foreach (dynamic rule in policy.Rules)
                {
                    try
                    {
                        string name = (string)rule.Name;
                        int action = (int)rule.Action;
                        bool enabled = (bool)rule.Enabled;

                        if (enabled && action == NET_FW_ACTION_BLOCK)
                        {
                            blockCount++;
                        }

                        if (!string.IsNullOrEmpty(name) && name.StartsWith("Pexoris_Block_"))
                        {
                            pexorisCount++;
                        }
                    }
                    catch
                    {
                        // Skip corrupted or unreadable individual rule entries
                    }
                }

                status.TotalBlockRules = blockCount;
                status.TotalPexorisRules = pexorisCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error reading firewall profile status: " + ex.Message);
            }
            return status;
        }

        public static List<FirewallRuleItem> GetRules(RuleFilter filter)
        {
            List<FirewallRuleItem> list = new List<FirewallRuleItem>();
            try
            {
                dynamic policy = GetFirewallPolicy();
                foreach (dynamic rule in policy.Rules)
                {
                    try
                    {
                        string name = (string)rule.Name;
                        string app = (string)rule.ApplicationName;
                        int dir = (int)rule.Direction;
                        int act = (int)rule.Action;
                        bool enabled = (bool)rule.Enabled;
                        int prof = (int)rule.Profiles;

                        bool isPexoris = !string.IsNullOrEmpty(name) && name.StartsWith("Pexoris_Block_");
                        bool isBlock = (act == NET_FW_ACTION_BLOCK);

                        if (filter == RuleFilter.PexorisOnly && !isPexoris)
                            continue;

                        if (filter == RuleFilter.AllBlocked && !isBlock)
                            continue;

                        if (filter == RuleFilter.AllActive && !enabled)
                            continue;

                        FirewallRuleItem item = new FirewallRuleItem();
                        item.Name = name ?? "(Unnamed Rule)";
                        item.ApplicationName = app ?? "(All Applications / Port)";
                        item.Direction = (dir == NET_FW_RULE_DIR_IN) ? "Inbound" : "Outbound";
                        item.Action = (act == NET_FW_ACTION_BLOCK) ? "Block" : "Allow";
                        item.Enabled = enabled;
                        item.IsPexorisRule = isPexoris;

                        if ((prof & NET_FW_PROFILE2_ALL) == NET_FW_PROFILE2_ALL || prof == 0x7FFFFFFF)
                            item.Profiles = "All Profiles";
                        else
                        {
                            List<string> pList = new List<string>();
                            if ((prof & NET_FW_PROFILE2_DOMAIN) != 0) pList.Add("Domain");
                            if ((prof & NET_FW_PROFILE2_PRIVATE) != 0) pList.Add("Private");
                            if ((prof & NET_FW_PROFILE2_PUBLIC) != 0) pList.Add("Public");
                            item.Profiles = pList.Count > 0 ? string.Join(", ", pList.ToArray()) : "Custom";
                        }

                        list.Add(item);
                    }
                    catch
                    {
                        // Skip corrupted rule
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error fetching firewall rules: " + ex.Message);
            }
            return list;
        }

        public static void BlockApplication(string executablePath, bool blockOutbound, bool blockInbound)
        {
            if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
            {
                throw new FileNotFoundException("Specified executable file does not exist: " + executablePath);
            }

            string fullPath = Path.GetFullPath(executablePath);
            string appName = Path.GetFileNameWithoutExtension(fullPath);

            dynamic policy = GetFirewallPolicy();
            Type ruleType = Type.GetTypeFromProgID("HNetCfg.FwRule");

            if (blockOutbound)
            {
                string outRuleName = "Pexoris_Block_Out_" + appName;
                try { policy.Rules.Remove(outRuleName); } catch { }

                dynamic outRule = Activator.CreateInstance(ruleType);
                outRule.Name = outRuleName;
                outRule.Description = "Outbound network access blocked by Pexoris FirewallBlocker";
                outRule.ApplicationName = fullPath;
                outRule.Action = NET_FW_ACTION_BLOCK;
                outRule.Direction = NET_FW_RULE_DIR_OUT;
                outRule.Enabled = true;
                outRule.Profiles = NET_FW_PROFILE2_ALL;
                policy.Rules.Add(outRule);
            }

            if (blockInbound)
            {
                string inRuleName = "Pexoris_Block_In_" + appName;
                try { policy.Rules.Remove(inRuleName); } catch { }

                dynamic inRule = Activator.CreateInstance(ruleType);
                inRule.Name = inRuleName;
                inRule.Description = "Inbound network access blocked by Pexoris FirewallBlocker";
                inRule.ApplicationName = fullPath;
                inRule.Action = NET_FW_ACTION_BLOCK;
                inRule.Direction = NET_FW_RULE_DIR_IN;
                inRule.Enabled = true;
                inRule.Profiles = NET_FW_PROFILE2_ALL;
                policy.Rules.Add(inRule);
            }
        }

        public static void UnblockRule(string ruleName)
        {
            if (string.IsNullOrEmpty(ruleName)) return;
            dynamic policy = GetFirewallPolicy();
            policy.Rules.Remove(ruleName);
        }

        public static void UnblockApplication(string executablePath)
        {
            if (string.IsNullOrEmpty(executablePath)) return;
            string fullPath = Path.GetFullPath(executablePath);

            dynamic policy = GetFirewallPolicy();
            List<string> toRemove = new List<string>();

            foreach (dynamic rule in policy.Rules)
            {
                try
                {
                    string app = (string)rule.ApplicationName;
                    if (!string.IsNullOrEmpty(app) && string.Equals(app, fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        toRemove.Add((string)rule.Name);
                    }
                }
                catch { }
            }

            foreach (string name in toRemove)
            {
                try { policy.Rules.Remove(name); } catch { }
            }
        }

        public static void ToggleRule(string ruleName)
        {
            if (string.IsNullOrEmpty(ruleName)) return;
            dynamic policy = GetFirewallPolicy();
            dynamic rule = policy.Rules.Item(ruleName);
            if (rule != null)
            {
                rule.Enabled = !((bool)rule.Enabled);
            }
        }

        public static int BlockTelemetryPresets()
        {
            int blockedCount = 0;
            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (string.IsNullOrEmpty(progFilesX86)) progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            string[] candidates = new string[]
            {
                Path.Combine(windir, @"System32\CompatTelRunner.exe"),
                Path.Combine(windir, @"System32\DeviceCensus.exe"),
                Path.Combine(windir, @"System32\wsqmcons.exe"),
                Path.Combine(progFilesX86, @"Microsoft\EdgeUpdate\MicrosoftEdgeUpdate.exe"),
                Path.Combine(windir, @"SystemApps\Microsoft.Windows.Search_cw5n1h2txyewy\SearchApp.exe"),
                Path.Combine(windir, @"SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\SearchHost.exe")
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        string appName = Path.GetFileNameWithoutExtension(path);
                        string ruleName = "Pexoris_Block_Telemetry_" + appName;

                        dynamic policy = GetFirewallPolicy();
                        try { policy.Rules.Remove(ruleName); } catch { }

                        Type ruleType = Type.GetTypeFromProgID("HNetCfg.FwRule");
                        dynamic rule = Activator.CreateInstance(ruleType);
                        rule.Name = ruleName;
                        rule.Description = "Windows telemetry outbound blocked by Pexoris FirewallBlocker";
                        rule.ApplicationName = path;
                        rule.Action = NET_FW_ACTION_BLOCK;
                        rule.Direction = NET_FW_RULE_DIR_OUT;
                        rule.Enabled = true;
                        rule.Profiles = NET_FW_PROFILE2_ALL;
                        policy.Rules.Add(rule);

                        blockedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Failed to block " + path + ": " + ex.Message);
                    }
                }
            }

            return blockedCount;
        }

        public static int UnblockTelemetryPresets()
        {
            int unblockedCount = 0;
            dynamic policy = GetFirewallPolicy();
            List<string> toRemove = new List<string>();

            foreach (dynamic rule in policy.Rules)
            {
                try
                {
                    string name = (string)rule.Name;
                    if (!string.IsNullOrEmpty(name) && name.StartsWith("Pexoris_Block_Telemetry_"))
                    {
                        toRemove.Add(name);
                    }
                }
                catch { }
            }

            foreach (string name in toRemove)
            {
                try
                {
                    policy.Rules.Remove(name);
                    unblockedCount++;
                }
                catch { }
            }

            return unblockedCount;
        }

        public static void SetAllProfilesState(bool enable)
        {
            string state = enable ? "on" : "off";
            RunCommand("netsh.exe", "advfirewall set allprofiles state " + state);
        }

        public static void ResetFirewallToDefaults()
        {
            RunCommand("netsh.exe", "advfirewall reset");
        }

        private static void RunCommand(string fileName, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = fileName;
            psi.Arguments = args;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            using (Process proc = Process.Start(psi))
            {
                if (proc != null)
                {
                    proc.WaitForExit(8000);
                }
            }
        }
    }
}
