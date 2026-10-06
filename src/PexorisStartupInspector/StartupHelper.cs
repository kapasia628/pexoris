using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace PexorisStartupInspector
{
    public enum StartupLocationType
    {
        Registry_HKCU_Run,
        Registry_HKCU_RunOnce,
        Registry_HKLM_Run,
        Registry_HKLM_RunOnce,
        Registry_HKLM_Run_WOW64,
        Folder_User_Startup,
        Folder_Common_Startup,
        Disabled_Backup
    }

    public enum StartupImpactLevel
    {
        High,
        Medium,
        Low,
        Crucial,
        Orphaned
    }

    public class StartupItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Command { get; set; }
        public string CleanExecutablePath { get; set; }
        public StartupLocationType LocationType { get; set; }
        public string LocationDisplay { get; set; }
        public string Publisher { get; set; }
        public StartupImpactLevel Impact { get; set; }
        public bool IsSafeToDisable { get; set; }
        public bool IsEnabled { get; set; }
        public bool FileExists { get; set; }
        public string OriginalRegistryKey { get; set; }
        public string FileSystemPath { get; set; }

        public StartupItem()
        {
            Id = Guid.NewGuid().ToString("N");
            IsEnabled = true;
            FileExists = true;
            IsSafeToDisable = false;
            Publisher = "Unknown";
            Impact = StartupImpactLevel.Medium;
        }
    }

    public static class StartupHelper
    {
        private const string PEXORIS_BACKUP_KEY = @"Software\Pexoris\StartupDisabled";

        public static List<StartupItem> ScanAllStartupItems()
        {
            List<StartupItem> list = new List<StartupItem>();

            // 1. Registry HKCU Run
            ScanRegistryKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run",
                StartupLocationType.Registry_HKCU_Run, "HKCU Run (Current User)", true, list);

            // 2. Registry HKCU RunOnce
            ScanRegistryKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                StartupLocationType.Registry_HKCU_RunOnce, "HKCU RunOnce", true, list);

            // 3. Registry HKLM Run (64-bit / standard)
            ScanRegistryKey(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run",
                StartupLocationType.Registry_HKLM_Run, "HKLM Run (All Users)", true, list);

            // 4. Registry HKLM RunOnce
            ScanRegistryKey(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                StartupLocationType.Registry_HKLM_RunOnce, "HKLM RunOnce", true, list);

            // 5. Registry HKLM WOW6432Node Run (32-bit apps on 64-bit Windows)
            ScanRegistryKey(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                StartupLocationType.Registry_HKLM_Run_WOW64, "HKLM Run (32-bit WOW64)", true, list);

            // 6. User Startup Folder
            string userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            ScanStartupFolder(userStartup, StartupLocationType.Folder_User_Startup, "User Startup Folder", list);

            // 7. Common Startup Folder
            string commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
            ScanStartupFolder(commonStartup, StartupLocationType.Folder_Common_Startup, "All Users Startup Folder", list);

            // 8. Pexoris Disabled Registry Backups
            ScanDisabledRegistryBackups(list);

            return list;
        }

        private static void ScanRegistryKey(RegistryKey rootKey, string subKeyPath,
            StartupLocationType locType, string locDisplay, bool isEnabled, List<StartupItem> list)
        {
            try
            {
                using (RegistryKey key = rootKey.OpenSubKey(subKeyPath, false))
                {
                    if (key == null) return;

                    string[] valNames = key.GetValueNames();
                    foreach (string name in valNames)
                    {
                        if (string.IsNullOrEmpty(name)) continue;

                        object val = key.GetValue(name);
                        string cmd = val != null ? val.ToString() : string.Empty;

                        StartupItem item = CreateItem(name, cmd, locType, locDisplay, isEnabled);
                        item.OriginalRegistryKey = (rootKey == Registry.CurrentUser ? "HKCU\\" : "HKLM\\") + subKeyPath;
                        list.Add(item);
                    }
                }
            }
            catch { /* Skip permission errors */ }
        }

        private static void ScanStartupFolder(string folderPath, StartupLocationType locType, string locDisplay, List<StartupItem> list)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

            try
            {
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    string filename = Path.GetFileName(file);

                    bool isPexorisDisabled = file.EndsWith(".pexoris_disabled", StringComparison.OrdinalIgnoreCase);
                    bool isEnabled = !isPexorisDisabled;

                    string displayName = filename;
                    if (isPexorisDisabled)
                    {
                        displayName = filename.Substring(0, filename.Length - ".pexoris_disabled".Length);
                    }

                    if (ext == ".lnk" || isPexorisDisabled || ext == ".bat" || ext == ".cmd" || ext == ".exe" || ext == ".vbs")
                    {
                        string targetPath = file;
                        if (ext == ".lnk")
                        {
                            targetPath = ResolveShortcutTarget(file);
                        }

                        StartupItem item = CreateItem(displayName, targetPath, locType, locDisplay, isEnabled);
                        item.FileSystemPath = file;
                        list.Add(item);
                    }
                }
            }
            catch { /* Skip permission issues */ }
        }

        private static void ScanDisabledRegistryBackups(List<StartupItem> list)
        {
            try
            {
                using (RegistryKey pexKey = Registry.CurrentUser.OpenSubKey(PEXORIS_BACKUP_KEY, false))
                {
                    if (pexKey == null) return;

                    string[] subKeys = pexKey.GetSubKeyNames();
                    foreach (string sk in subKeys)
                    {
                        using (RegistryKey itemKey = pexKey.OpenSubKey(sk, false))
                        {
                            if (itemKey == null) continue;

                            string name = (string)itemKey.GetValue("Name") ?? sk;
                            string cmd = (string)itemKey.GetValue("Command") ?? string.Empty;
                            string origKey = (string)itemKey.GetValue("OriginalKey") ?? string.Empty;
                            string locDisplay = (string)itemKey.GetValue("LocationDisplay") ?? "Disabled (Registry)";

                            StartupItem item = CreateItem(name, cmd, StartupLocationType.Disabled_Backup, locDisplay + " (Disabled)", false);
                            item.OriginalRegistryKey = origKey;
                            item.Id = sk;
                            list.Add(item);
                        }
                    }
                }
            }
            catch { }
        }

        private static StartupItem CreateItem(string name, string cmd, StartupLocationType locType, string locDisplay, bool isEnabled)
        {
            StartupItem item = new StartupItem();
            item.Name = name;
            item.Command = cmd;
            item.LocationType = locType;
            item.LocationDisplay = locDisplay;
            item.IsEnabled = isEnabled;

            string exePath = ExtractExecutablePath(cmd);
            item.CleanExecutablePath = exePath;

            if (!string.IsNullOrEmpty(exePath))
            {
                item.FileExists = File.Exists(exePath);
                if (item.FileExists)
                {
                    try
                    {
                        FileVersionInfo vi = FileVersionInfo.GetVersionInfo(exePath);
                        if (!string.IsNullOrEmpty(vi.CompanyName))
                        {
                            item.Publisher = vi.CompanyName.Trim();
                        }
                    }
                    catch { }
                }
                else
                {
                    item.Impact = StartupImpactLevel.Orphaned;
                    item.IsSafeToDisable = true;
                    item.Publisher = "Missing File";
                    return item;
                }
            }
            else
            {
                item.FileExists = false;
                item.Impact = StartupImpactLevel.Orphaned;
                item.IsSafeToDisable = true;
                item.Publisher = "Orphaned Entry";
                return item;
            }

            // Categorize Publisher & Impact
            AnalyzeImpactAndSafety(item);

            return item;
        }

        private static void AnalyzeImpactAndSafety(StartupItem item)
        {
            string lowerName = (item.Name ?? "").ToLowerInvariant();
            string lowerCmd = (item.Command ?? "").ToLowerInvariant();
            string lowerPub = (item.Publisher ?? "").ToLowerInvariant();

            // Crucial Windows / Driver services (Never disable by default!)
            if (lowerPub.Contains("realtek") || lowerName.Contains("realtek") || lowerCmd.Contains("rtkaud") ||
                lowerPub.Contains("nvidia") || lowerName.Contains("nvidia") || lowerCmd.Contains("nvstartup") ||
                lowerPub.Contains("intel") || lowerName.Contains("intel") || lowerCmd.Contains("igfxtray") ||
                lowerPub.Contains("advanced micro devices") || lowerPub.Contains("amd") || lowerName.Contains("radeon") ||
                lowerName.Contains("securityhealthsystray") || lowerCmd.Contains("securityhealth") ||
                lowerName.Contains("vanguard") || lowerCmd.Contains("vgtray"))
            {
                item.Impact = StartupImpactLevel.Crucial;
                item.IsSafeToDisable = false;
                return;
            }

            // High impact bloatware & auto-updaters (Very Safe to disable!)
            if (lowerName.Contains("discord") || lowerCmd.Contains("discord") ||
                lowerName.Contains("spotify") || lowerCmd.Contains("spotify") ||
                lowerName.Contains("steam") || lowerCmd.Contains("steam") ||
                lowerName.Contains("epicgames") || lowerCmd.Contains("epicgames") ||
                lowerName.Contains("msedge") || lowerCmd.Contains("msedge") || lowerName.Contains("microsoft edge") ||
                lowerName.Contains("onedrive") || lowerCmd.Contains("onedrive") ||
                lowerName.Contains("ccleaner") || lowerCmd.Contains("ccleaner") ||
                lowerName.Contains("skype") || lowerCmd.Contains("skype") ||
                lowerName.Contains("teams") || lowerCmd.Contains("teams") ||
                lowerName.Contains("adobe") || lowerCmd.Contains("adobe") ||
                lowerName.Contains("update") || lowerName.Contains("helper") ||
                lowerName.Contains("torrent") || lowerCmd.Contains("torrent") ||
                lowerName.Contains("cortana") || lowerCmd.Contains("cortana"))
            {
                item.Impact = StartupImpactLevel.High;
                item.IsSafeToDisable = true;
                return;
            }

            // Microsoft standard tools
            if (lowerPub.Contains("microsoft"))
            {
                item.Impact = StartupImpactLevel.Low;
                item.IsSafeToDisable = false;
                return;
            }

            // Other third-party startup applications
            item.Impact = StartupImpactLevel.Medium;
            item.IsSafeToDisable = true;
        }

        public static string ExtractExecutablePath(string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) return string.Empty;
            string trimmed = cmd.Trim();

            if (trimmed.StartsWith("\""))
            {
                int endQuote = trimmed.IndexOf('\"', 1);
                if (endQuote > 1)
                {
                    return trimmed.Substring(1, endQuote - 1);
                }
            }

            int spaceIdx = trimmed.IndexOf(' ');
            if (spaceIdx > 0)
            {
                string firstPart = trimmed.Substring(0, spaceIdx);
                if (File.Exists(firstPart))
                {
                    return firstPart;
                }
            }

            if (File.Exists(trimmed))
            {
                return trimmed;
            }

            if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            return trimmed;
        }

        private static string ResolveShortcutTarget(string shortcutPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    string target = shortcut.TargetPath;
                    if (!string.IsNullOrEmpty(target))
                    {
                        return target;
                    }
                }
            }
            catch { }
            return shortcutPath;
        }

        public static bool DisableItem(StartupItem item)
        {
            try
            {
                if (!string.IsNullOrEmpty(item.FileSystemPath))
                {
                    // Folder shortcut: rename with .pexoris_disabled suffix
                    if (File.Exists(item.FileSystemPath))
                    {
                        string targetNew = item.FileSystemPath + ".pexoris_disabled";
                        File.Move(item.FileSystemPath, targetNew);
                        item.FileSystemPath = targetNew;
                        item.IsEnabled = false;
                        return true;
                    }
                }
                else if (!string.IsNullOrEmpty(item.OriginalRegistryKey))
                {
                    // Registry item: store backup into HKCU\Software\Pexoris\StartupDisabled, then remove active entry
                    string backupSubKey = PEXORIS_BACKUP_KEY + "\\" + item.Id;
                    using (RegistryKey bKey = Registry.CurrentUser.CreateSubKey(backupSubKey))
                    {
                        bKey.SetValue("Name", item.Name);
                        bKey.SetValue("Command", item.Command);
                        bKey.SetValue("OriginalKey", item.OriginalRegistryKey);
                        bKey.SetValue("LocationDisplay", item.LocationDisplay);
                    }

                    // Delete active registry value
                    bool isHKCU = item.OriginalRegistryKey.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase);
                    string subPath = item.OriginalRegistryKey.Substring(5);

                    RegistryKey root = isHKCU ? Registry.CurrentUser : Registry.LocalMachine;
                    using (RegistryKey reg = root.OpenSubKey(subPath, true))
                    {
                        if (reg != null)
                        {
                            reg.DeleteValue(item.Name, false);
                        }
                    }

                    item.IsEnabled = false;
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static bool EnableItem(StartupItem item)
        {
            try
            {
                if (!string.IsNullOrEmpty(item.FileSystemPath) && item.FileSystemPath.EndsWith(".pexoris_disabled", StringComparison.OrdinalIgnoreCase))
                {
                    // Restore shortcut
                    string targetOrig = item.FileSystemPath.Substring(0, item.FileSystemPath.Length - ".pexoris_disabled".Length);
                    File.Move(item.FileSystemPath, targetOrig);
                    item.FileSystemPath = targetOrig;
                    item.IsEnabled = true;
                    return true;
                }
                else if (item.LocationType == StartupLocationType.Disabled_Backup || !item.IsEnabled)
                {
                    // Restore registry entry
                    if (!string.IsNullOrEmpty(item.OriginalRegistryKey))
                    {
                        bool isHKCU = item.OriginalRegistryKey.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase);
                        string subPath = item.OriginalRegistryKey.Substring(5);

                        RegistryKey root = isHKCU ? Registry.CurrentUser : Registry.LocalMachine;
                        using (RegistryKey reg = root.CreateSubKey(subPath))
                        {
                            if (reg != null)
                            {
                                reg.SetValue(item.Name, item.Command);
                            }
                        }

                        // Remove from Pexoris backup key
                        using (RegistryKey pexKey = Registry.CurrentUser.OpenSubKey(PEXORIS_BACKUP_KEY, true))
                        {
                            if (pexKey != null)
                            {
                                pexKey.DeleteSubKeyTree(item.Id, false);
                            }
                        }

                        item.IsEnabled = true;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static bool DeleteItemPermanently(StartupItem item)
        {
            try
            {
                if (!string.IsNullOrEmpty(item.FileSystemPath) && File.Exists(item.FileSystemPath))
                {
                    File.Delete(item.FileSystemPath);
                    return true;
                }

                if (!string.IsNullOrEmpty(item.OriginalRegistryKey))
                {
                    if (item.IsEnabled)
                    {
                        bool isHKCU = item.OriginalRegistryKey.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase);
                        string subPath = item.OriginalRegistryKey.Substring(5);

                        RegistryKey root = isHKCU ? Registry.CurrentUser : Registry.LocalMachine;
                        using (RegistryKey reg = root.OpenSubKey(subPath, true))
                        {
                            if (reg != null)
                            {
                                reg.DeleteValue(item.Name, false);
                            }
                        }
                    }
                    else
                    {
                        // Clean from backup key if disabled
                        using (RegistryKey pexKey = Registry.CurrentUser.OpenSubKey(PEXORIS_BACKUP_KEY, true))
                        {
                            if (pexKey != null)
                            {
                                pexKey.DeleteSubKeyTree(item.Id, false);
                            }
                        }
                    }
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
