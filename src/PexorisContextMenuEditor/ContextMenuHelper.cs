using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Pexoris.ContextMenuEditor
{
    public class ContextMenuItem
    {
        public string Name { get; set; }
        public string Scope { get; set; }
        public string RootKeyPath { get; set; }
        public string SubKeyName { get; set; }
        public string ValueData { get; set; }
        public bool IsEnabled { get; set; }

        public string DisplayStatus
        {
            get { return IsEnabled ? "Active" : "Disabled"; }
        }
    }

    public static class ContextMenuHelper
    {
        private const string CLSID_WIN11_CLASSIC = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";
        private const string CLSID_PARENT_KEY = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";

        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const int SHCNF_FLUSH = 0x1000;

        /// <summary>
        /// Checks if Windows 11 Classic Context Menu override is currently active.
        /// </summary>
        public static bool IsClassicMenuEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(CLSID_WIN11_CLASSIC))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("");
                        return (val != null);
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Enables Windows 10 Classic Context Menu on Windows 11.
        /// </summary>
        public static bool EnableClassicMenu()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(CLSID_WIN11_CLASSIC))
                {
                    if (key != null)
                    {
                        key.SetValue("", "", RegistryValueKind.String);
                    }
                }
                NotifyShell();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EnableClassicMenu Error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Restores standard Windows 11 Modern Context Menu.
        /// </summary>
        public static bool RestoreModernMenu()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(CLSID_PARENT_KEY, false);
                NotifyShell();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RestoreModernMenu Error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Smoothly restarts Windows Explorer.
        /// </summary>
        public static void RestartExplorer()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("explorer");
                foreach (Process p in procs)
                {
                    try { p.Kill(); p.WaitForExit(1500); } catch { }
                }
                Process.Start("explorer.exe");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RestartExplorer Error: " + ex.Message);
            }
        }

        public static void NotifyShell()
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }

        /// <summary>
        /// Scans registered ContextMenuHandlers across Windows shell locations.
        /// </summary>
        public static List<ContextMenuItem> ScanContextMenuItems()
        {
            List<ContextMenuItem> list = new List<ContextMenuItem>();

            var scanPaths = new[]
            {
                new { Root = Registry.ClassesRoot, Path = @"*\shellex\ContextMenuHandlers", Scope = "All Files (*)" },
                new { Root = Registry.ClassesRoot, Path = @"Directory\shellex\ContextMenuHandlers", Scope = "Folders (Directory)" },
                new { Root = Registry.ClassesRoot, Path = @"Folder\shellex\ContextMenuHandlers", Scope = "Folders (Shell)" },
                new { Root = Registry.ClassesRoot, Path = @"Drive\shellex\ContextMenuHandlers", Scope = "Drives" },
                new { Root = Registry.ClassesRoot, Path = @"Directory\Background\shellex\ContextMenuHandlers", Scope = "Desktop / Background" },
                new { Root = Registry.CurrentUser, Path = @"Software\Classes\*\shellex\ContextMenuHandlers", Scope = "User Files (*)" },
                new { Root = Registry.CurrentUser, Path = @"Software\Classes\Directory\shellex\ContextMenuHandlers", Scope = "User Folders" }
            };

            foreach (var sp in scanPaths)
            {
                try
                {
                    using (RegistryKey parentKey = sp.Root.OpenSubKey(sp.Path, false))
                    {
                        if (parentKey == null) continue;

                        string[] subKeyNames = parentKey.GetSubKeyNames();
                        foreach (string subName in subKeyNames)
                        {
                            string valData = "";
                            try
                            {
                                using (RegistryKey sk = parentKey.OpenSubKey(subName, false))
                                {
                                    if (sk != null)
                                    {
                                        object o = sk.GetValue("");
                                        if (o != null) valData = o.ToString();
                                    }
                                }
                            }
                            catch { }

                            bool isEnabled = !subName.StartsWith("-");
                            string cleanName = isEnabled ? subName : subName.TrimStart('-');

                            list.Add(new ContextMenuItem
                            {
                                Name = cleanName,
                                Scope = sp.Scope,
                                RootKeyPath = sp.Path,
                                SubKeyName = subName,
                                ValueData = valData,
                                IsEnabled = isEnabled
                            });
                        }
                    }
                }
                catch { }
            }

            return list;
        }

        /// <summary>
        /// Toggles (Enables / Disables) a specific context menu handler by prefixing with '-' in registry.
        /// </summary>
        public static bool ToggleItem(ContextMenuItem item)
        {
            if (item == null) return false;

            try
            {
                RegistryKey root = item.RootKeyPath.StartsWith("Software\\") ? Registry.CurrentUser : Registry.ClassesRoot;
                using (RegistryKey parentKey = root.OpenSubKey(item.RootKeyPath, true))
                {
                    if (parentKey == null) return false;

                    string currentKeyName = item.SubKeyName;
                    string newKeyName = item.IsEnabled ? "-" + item.Name : item.Name;

                    // Copy and rename subkey
                    using (RegistryKey srcKey = parentKey.OpenSubKey(currentKeyName, false))
                    {
                        if (srcKey == null) return false;

                        using (RegistryKey dstKey = parentKey.CreateSubKey(newKeyName))
                        {
                            if (dstKey != null)
                            {
                                foreach (string valName in srcKey.GetValueNames())
                                {
                                    dstKey.SetValue(valName, srcKey.GetValue(valName), srcKey.GetValueKind(valName));
                                }
                            }
                        }
                    }

                    parentKey.DeleteSubKeyTree(currentKeyName, false);
                    item.SubKeyName = newKeyName;
                    item.IsEnabled = !item.IsEnabled;
                    NotifyShell();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ToggleItem Error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Safely deletes a context menu handler subkey.
        /// </summary>
        public static bool DeleteItem(ContextMenuItem item)
        {
            if (item == null) return false;

            try
            {
                RegistryKey root = item.RootKeyPath.StartsWith("Software\\") ? Registry.CurrentUser : Registry.ClassesRoot;
                using (RegistryKey parentKey = root.OpenSubKey(item.RootKeyPath, true))
                {
                    if (parentKey == null) return false;
                    parentKey.DeleteSubKeyTree(item.SubKeyName, false);
                    NotifyShell();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeleteItem Error: " + ex.Message);
                return false;
            }
        }
    }
}
