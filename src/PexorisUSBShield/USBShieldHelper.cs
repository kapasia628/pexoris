using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace Pexoris.USBShield
{
    public class PolicyItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string RegPath { get; set; }
        public string ValueName { get; set; }
        public bool IsProtected { get; set; }
        public string StatusText { get; set; }
        public string Impact { get; set; }
    }

    public class UsbDriveItem
    {
        public string RootPath { get; set; }
        public string VolumeLabel { get; set; }
        public string FileSystem { get; set; }
        public long TotalSizeBytes { get; set; }
        public long FreeSizeBytes { get; set; }
        public bool IsImmunized { get; set; }

        public string DisplayText
        {
            get
            {
                string label = string.IsNullOrEmpty(VolumeLabel) ? "Removable Disk" : VolumeLabel;
                double gb = (double)TotalSizeBytes / (1024 * 1024 * 1024);
                return string.Format("{0} ({1}) - {2:0.1} GB [{3}]", RootPath, label, gb, FileSystem);
            }
        }
    }

    public static class USBShieldHelper
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam,
            uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

        private const int HWND_BROADCAST = 0xFFFF;
        private const uint WM_SETTINGCHANGE = 0x001A;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        public static void NotifyShellSettingsChanged()
        {
            try
            {
                UIntPtr result;
                SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero, "Policy", SMTO_ABORTIFHUNG, 1000, out result);
            }
            catch { }
        }

        // ==========================================
        // POLICY SCANNING
        // ==========================================
        public static List<PolicyItem> ScanPolicies()
        {
            List<PolicyItem> items = new List<PolicyItem>();

            // 1. HKCU NoDriveTypeAutoRun
            int hkcuVal = GetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 0x91);
            bool hkcuBlocked = (hkcuVal == 0xFF || hkcuVal == 0xDF || (hkcuVal & 0x04) != 0);
            items.Add(new PolicyItem
            {
                Id = "HKCU_AutoRun",
                Name = "User AutoPlay Policy (HKCU)",
                Category = "AutoPlay Policy",
                RegPath = @"HKCU\...\Policies\Explorer\NoDriveTypeAutoRun",
                ValueName = "NoDriveTypeAutoRun",
                IsProtected = hkcuBlocked,
                StatusText = hkcuBlocked ? "BLOCKED (0x" + hkcuVal.ToString("X") + ")" : "ACTIVE / ALLOWED (0x" + hkcuVal.ToString("X") + ")",
                Impact = "Prevents automatic execution of drives when inserted by current user."
            });

            // 2. HKLM NoDriveTypeAutoRun
            int hklmVal = GetRegDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 0x91);
            bool hklmBlocked = (hklmVal == 0xFF || hklmVal == 0xDF || (hklmVal & 0x04) != 0);
            items.Add(new PolicyItem
            {
                Id = "HKLM_AutoRun",
                Name = "System AutoPlay Policy (HKLM)",
                Category = "AutoPlay Policy",
                RegPath = @"HKLM\...\Policies\Explorer\NoDriveTypeAutoRun",
                ValueName = "NoDriveTypeAutoRun",
                IsProtected = hklmBlocked,
                StatusText = hklmBlocked ? "BLOCKED (0x" + hklmVal.ToString("X") + ")" : "ACTIVE / ALLOWED (0x" + hklmVal.ToString("X") + ")",
                Impact = "Prevents automatic media drive execution across all computer accounts."
            });

            // 3. HKLM NoAutorun
            int noAutorun = GetRegDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoAutorun", 0);
            bool noAutorunBlocked = (noAutorun == 1);
            items.Add(new PolicyItem
            {
                Id = "HKLM_NoAutorun",
                Name = "Global Autorun Disabled (NoAutorun)",
                Category = "Execution Prevention",
                RegPath = @"HKLM\...\Policies\Explorer\NoAutorun",
                ValueName = "NoAutorun",
                IsProtected = noAutorunBlocked,
                StatusText = noAutorunBlocked ? "BLOCKED (1)" : "EXPOSED (0)",
                Impact = "Completely blocks Windows Shell from processing autorun commands."
            });

            // 4. DisableAutoplay Handler
            int disableAutoplay = GetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers", "DisableAutoplay", 0);
            bool autoPlayHandlersBlocked = (disableAutoplay == 1);
            items.Add(new PolicyItem
            {
                Id = "DisableAutoplay",
                Name = "Shell AutoPlay Handlers",
                Category = "Execution Prevention",
                RegPath = @"HKCU\...\AutoplayHandlers\DisableAutoplay",
                ValueName = "DisableAutoplay",
                IsProtected = autoPlayHandlersBlocked,
                StatusText = autoPlayHandlersBlocked ? "BLOCKED (1)" : "EXPOSED (0)",
                Impact = "Suppresses AutoPlay popups and automatic folder explorer prompts."
            });

            // 5. StorageDevicePolicies WriteProtect
            int writeProtect = GetRegDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\StorageDevicePolicies", "WriteProtect", 0);
            bool isWriteProtected = (writeProtect == 1);
            items.Add(new PolicyItem
            {
                Id = "WriteProtect",
                Name = "USB Port Write-Protection",
                Category = "Data Leak & Tamper Guard",
                RegPath = @"HKLM\...\Control\StorageDevicePolicies\WriteProtect",
                ValueName = "WriteProtect",
                IsProtected = isWriteProtected,
                StatusText = isWriteProtected ? "WRITE-PROTECTED (Read-Only)" : "READ & WRITE ALLOWED",
                Impact = isWriteProtected ? "USB ports are locked in Read-Only mode. Data cannot be written or stolen." : "Full Read/Write access. Anyone can copy files to or from flash drives."
            });

            // 6. IniFileMapping Autorun.inf
            string iniMapping = GetRegString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\IniFileMapping\Autorun.inf", null, "");
            bool iniBlocked = string.Equals(iniMapping, "@SYS:DoesNotExist", StringComparison.OrdinalIgnoreCase);
            items.Add(new PolicyItem
            {
                Id = "IniFileMapping",
                Name = "Autorun.inf Parser Redirection",
                Category = "Kernel / Shell Immunity",
                RegPath = @"HKLM\...\IniFileMapping\Autorun.inf",
                ValueName = "(Default)",
                IsProtected = iniBlocked,
                StatusText = iniBlocked ? "IMMUNIZED (@SYS:DoesNotExist)" : "EXPOSED (Standard Parsing)",
                Impact = "Redirects all Autorun.inf parsing to a nonexistent file so worms can never trigger."
            });

            return items;
        }

        // ==========================================
        // REMOVABLE DRIVES SCANNING
        // ==========================================
        public static List<UsbDriveItem> ScanRemovableDrives()
        {
            List<UsbDriveItem> list = new List<UsbDriveItem>();
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo d in drives)
                {
                    if (!d.IsReady) continue;

                    // Strictly Removable USB drives
                    if (d.DriveType == DriveType.Removable)
                    {
                        bool immunized = false;
                        string autoRunPath = Path.Combine(d.RootDirectory.FullName, "autorun.inf");
                        try
                        {
                            if (Directory.Exists(autoRunPath) || File.Exists(autoRunPath))
                            {
                                immunized = true;
                            }
                        }
                        catch { }

                        list.Add(new UsbDriveItem
                        {
                            RootPath = d.RootDirectory.FullName,
                            VolumeLabel = d.VolumeLabel,
                            FileSystem = d.DriveFormat,
                            TotalSizeBytes = d.TotalSize,
                            FreeSizeBytes = d.TotalFreeSpace,
                            IsImmunized = immunized
                        });
                    }
                }
            }
            catch { }
            return list;
        }

        // ==========================================
        // REMEDIATION & TOGGLES
        // ==========================================
        public static bool BlockAllThreats()
        {
            bool success = true;
            try
            {
                // Set HKCU NoDriveTypeAutoRun = 0xFF
                SetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 0xFF);
                
                // Set HKLM NoDriveTypeAutoRun = 0xFF
                SetRegDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 0xFF);

                // Set HKLM NoAutorun = 1
                SetRegDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoAutorun", 1);

                // Set DisableAutoplay = 1
                SetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers", "DisableAutoplay", 1);

                // Set IniFileMapping\Autorun.inf = @SYS:DoesNotExist
                SetRegString(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\IniFileMapping\Autorun.inf", null, "@SYS:DoesNotExist");

                NotifyShellSettingsChanged();
            }
            catch
            {
                success = false;
            }
            return success;
        }

        public static bool EnableWriteProtect()
        {
            try
            {
                SetRegDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\StorageDevicePolicies", "WriteProtect", 1);
                NotifyShellSettingsChanged();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DisableWriteProtect()
        {
            try
            {
                SetRegDword(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\StorageDevicePolicies", "WriteProtect", 0);
                NotifyShellSettingsChanged();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool RestoreDefaults()
        {
            try
            {
                SetRegDword(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 0x91);
                SetRegDword(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 0x91);
                DeleteRegValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoAutorun");
                DeleteRegValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers", "DisableAutoplay");
                DeleteRegKey(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\IniFileMapping", "Autorun.inf");
                NotifyShellSettingsChanged();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ImmunizeDrive(string driveRoot)
        {
            try
            {
                string targetPath = Path.Combine(driveRoot, "autorun.inf");
                
                // If regular file exists, delete it
                if (File.Exists(targetPath))
                {
                    File.SetAttributes(targetPath, FileAttributes.Normal);
                    File.Delete(targetPath);
                }

                // If folder doesn't exist, create it as un-deletable folder
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                    string dummySub = Path.Combine(targetPath, "con.protect.pexoris");
                    if (!Directory.Exists(dummySub))
                    {
                        Directory.CreateDirectory(dummySub);
                    }
                }

                // Set ReadOnly, Hidden, System attributes
                File.SetAttributes(targetPath, FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool RemoveImmunization(string driveRoot)
        {
            try
            {
                string targetPath = Path.Combine(driveRoot, "autorun.inf");
                if (Directory.Exists(targetPath))
                {
                    File.SetAttributes(targetPath, FileAttributes.Normal);
                    Directory.Delete(targetPath, true);
                    return true;
                }
                else if (File.Exists(targetPath))
                {
                    File.SetAttributes(targetPath, FileAttributes.Normal);
                    File.Delete(targetPath);
                    return true;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ==========================================
        // REGISTRY UTILITIES
        // ==========================================
        private static int GetRegDword(RegistryKey root, string subKey, string valueName, int defaultValue)
        {
            try
            {
                using (RegistryKey k = root.OpenSubKey(subKey, false))
                {
                    if (k != null)
                    {
                        object val = k.GetValue(valueName);
                        if (val is int) return (int)val;
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private static string GetRegString(RegistryKey root, string subKey, string valueName, string defaultValue)
        {
            try
            {
                using (RegistryKey k = root.OpenSubKey(subKey, false))
                {
                    if (k != null)
                    {
                        object val = k.GetValue(valueName);
                        if (val is string) return (string)val;
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        private static void SetRegDword(RegistryKey root, string subKey, string valueName, int value)
        {
            using (RegistryKey k = root.CreateSubKey(subKey))
            {
                if (k != null)
                {
                    k.SetValue(valueName, value, RegistryValueKind.DWord);
                }
            }
        }

        private static void SetRegString(RegistryKey root, string subKey, string valueName, string value)
        {
            using (RegistryKey k = root.CreateSubKey(subKey))
            {
                if (k != null)
                {
                    k.SetValue(valueName, value, RegistryValueKind.String);
                }
            }
        }

        private static void DeleteRegValue(RegistryKey root, string subKey, string valueName)
        {
            try
            {
                using (RegistryKey k = root.OpenSubKey(subKey, true))
                {
                    if (k != null)
                    {
                        k.DeleteValue(valueName, false);
                    }
                }
            }
            catch { }
        }

        private static void DeleteRegKey(RegistryKey root, string subKey, string childKeyName)
        {
            try
            {
                using (RegistryKey k = root.OpenSubKey(subKey, true))
                {
                    if (k != null)
                    {
                        k.DeleteSubKeyTree(childKeyName, false);
                    }
                }
            }
            catch { }
        }
    }
}
