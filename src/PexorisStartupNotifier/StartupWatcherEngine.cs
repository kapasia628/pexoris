using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace PexorisStartupNotifier
{
    public enum StartupLocationType
    {
        RegistryHKCU_Run,
        RegistryHKCU_RunOnce,
        RegistryHKLM_Run,
        RegistryHKLM_RunOnce,
        RegistryHKLM_Wow64,
        FolderUserStartup,
        FolderCommonStartup
    }

    public class StartupItem
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public string Scope { get; set; }
        public StartupLocationType LocationType { get; set; }
        public string LocationPath { get; set; }
        public bool TargetExists { get; set; }
        public DateTime DetectedTime { get; set; }

        public StartupItem()
        {
            Name = string.Empty;
            Command = string.Empty;
            Scope = "User";
            LocationPath = string.Empty;
            TargetExists = false;
            DetectedTime = DateTime.Now;
        }

        public string LocationDisplay
        {
            get
            {
                switch (LocationType)
                {
                    case StartupLocationType.RegistryHKCU_Run: return "HKCU\\Run";
                    case StartupLocationType.RegistryHKCU_RunOnce: return "HKCU\\RunOnce";
                    case StartupLocationType.RegistryHKLM_Run: return "HKLM\\Run";
                    case StartupLocationType.RegistryHKLM_RunOnce: return "HKLM\\RunOnce";
                    case StartupLocationType.RegistryHKLM_Wow64: return "HKLM\\Wow6432Node\\Run";
                    case StartupLocationType.FolderUserStartup: return "User Startup Folder";
                    case StartupLocationType.FolderCommonStartup: return "All Users Startup Folder";
                    default: return LocationPath;
                }
            }
        }
    }

    public class NewStartupEventArgs : EventArgs
    {
        public StartupItem Item { get; private set; }

        public NewStartupEventArgs(StartupItem item)
        {
            Item = item;
        }
    }

    public static class StartupWatcherEngine
    {
        private static bool _isWatching = false;
        private static Thread _monitorThread = null;
        private static List<FileSystemWatcher> _fsWatchers = new List<FileSystemWatcher>();
        private static Dictionary<string, StartupItem> _knownSnapshot = new Dictionary<string, StartupItem>();
        private static readonly object _syncLock = new object();

        public static event EventHandler<NewStartupEventArgs> OnNewStartupDetected;
        public static event EventHandler OnStateChanged;

        public static bool IsWatching
        {
            get { return _isWatching; }
        }

        public static List<StartupItem> ScanAllStartupItems()
        {
            List<StartupItem> list = new List<StartupItem>();

            // 1. HKCU Run
            ScanRegistryKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run",
                "User", StartupLocationType.RegistryHKCU_Run, list);

            // 2. HKCU RunOnce
            ScanRegistryKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                "User", StartupLocationType.RegistryHKCU_RunOnce, list);

            // 3. HKLM Run
            ScanRegistryKey(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run",
                "Machine", StartupLocationType.RegistryHKLM_Run, list);

            // 4. HKLM RunOnce
            ScanRegistryKey(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                "Machine", StartupLocationType.RegistryHKLM_RunOnce, list);

            // 5. HKLM Wow6432Node Run (for 32-bit apps on 64-bit Windows)
            ScanRegistryKey(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                "Machine (x86)", StartupLocationType.RegistryHKLM_Wow64, list);

            // 6. User Startup Folder
            string userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            ScanFolder(userStartup, "User", StartupLocationType.FolderUserStartup, list);

            // 7. Common Startup Folder
            string commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
            ScanFolder(commonStartup, "Machine", StartupLocationType.FolderCommonStartup, list);

            return list;
        }

        private static void ScanRegistryKey(RegistryKey root, string subKeyPath, string scope, StartupLocationType locType, List<StartupItem> list)
        {
            try
            {
                using (RegistryKey key = root.OpenSubKey(subKeyPath, false))
                {
                    if (key != null)
                    {
                        string[] valueNames = key.GetValueNames();
                        foreach (string vn in valueNames)
                        {
                            object val = key.GetValue(vn);
                            if (val != null)
                            {
                                string cmd = val.ToString();
                                StartupItem item = new StartupItem();
                                item.Name = string.IsNullOrEmpty(vn) ? "(Default)" : vn;
                                item.Command = cmd;
                                item.Scope = scope;
                                item.LocationType = locType;
                                item.LocationPath = root.Name + "\\" + subKeyPath;
                                item.TargetExists = CheckExecutableExists(cmd);
                                list.Add(item);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static void ScanFolder(string folderPath, string scope, StartupLocationType locType, List<StartupItem> list)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    string[] files = Directory.GetFiles(folderPath);
                    foreach (string f in files)
                    {
                        string name = Path.GetFileName(f);
                        if (name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                            continue;

                        StartupItem item = new StartupItem();
                        item.Name = Path.GetFileNameWithoutExtension(f);
                        item.Command = f;
                        item.Scope = scope;
                        item.LocationType = locType;
                        item.LocationPath = folderPath;
                        item.TargetExists = File.Exists(f);
                        list.Add(item);
                    }
                }
            }
            catch { }
        }

        private static bool CheckExecutableExists(string rawCommand)
        {
            if (string.IsNullOrEmpty(rawCommand)) return false;
            string clean = rawCommand.Trim();
            if (clean.StartsWith("\""))
            {
                int endQuote = clean.IndexOf('\"', 1);
                if (endQuote > 1)
                {
                    clean = clean.Substring(1, endQuote - 1);
                }
            }
            else
            {
                int space = clean.IndexOf(' ');
                if (space > 0)
                {
                    string potentialPath = clean.Substring(0, space);
                    if (File.Exists(potentialPath)) return true;
                }
            }

            try
            {
                if (File.Exists(clean)) return true;
            }
            catch { }

            return false;
        }

        public static void StartMonitoring()
        {
            lock (_syncLock)
            {
                if (_isWatching) return;

                // Take initial snapshot of existing items so we don't alert on existing ones
                _knownSnapshot.Clear();
                List<StartupItem> initialItems = ScanAllStartupItems();
                foreach (StartupItem it in initialItems)
                {
                    string key = it.LocationPath + "::" + it.Name;
                    _knownSnapshot[key] = it;
                }

                _isWatching = true;

                // Setup Folder Watchers
                SetupFolderWatcher(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
                SetupFolderWatcher(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup));

                // Start Registry Polling Thread
                _monitorThread = new Thread(MonitorRegistryLoop);
                _monitorThread.IsBackground = true;
                _monitorThread.Start();
            }

            if (OnStateChanged != null) OnStateChanged(null, EventArgs.Empty);
        }

        public static void StopMonitoring()
        {
            lock (_syncLock)
            {
                if (!_isWatching) return;
                _isWatching = false;

                foreach (FileSystemWatcher w in _fsWatchers)
                {
                    try
                    {
                        w.EnableRaisingEvents = false;
                        w.Dispose();
                    }
                    catch { }
                }
                _fsWatchers.Clear();
            }

            if (OnStateChanged != null) OnStateChanged(null, EventArgs.Empty);
        }

        private static void SetupFolderWatcher(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    FileSystemWatcher watcher = new FileSystemWatcher(path);
                    watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                    watcher.Created += (s, e) => {
                        if (e.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) return;
                        Thread.Sleep(300); // Allow file flush
                        CheckForNewItems();
                    };
                    watcher.EnableRaisingEvents = true;
                    _fsWatchers.Add(watcher);
                }
            }
            catch { }
        }

        private static void MonitorRegistryLoop()
        {
            while (_isWatching)
            {
                Thread.Sleep(2000); // Check every 2 seconds
                if (!_isWatching) break;
                CheckForNewItems();
            }
        }

        private static void CheckForNewItems()
        {
            lock (_syncLock)
            {
                List<StartupItem> current = ScanAllStartupItems();
                foreach (StartupItem it in current)
                {
                    string key = it.LocationPath + "::" + it.Name;
                    if (!_knownSnapshot.ContainsKey(key))
                    {
                        _knownSnapshot[key] = it;
                        // Fire notification event
                        if (OnNewStartupDetected != null)
                        {
                            OnNewStartupDetected(null, new NewStartupEventArgs(it));
                        }
                    }
                }
            }
        }

        public static bool DeleteStartupItem(StartupItem item)
        {
            try
            {
                switch (item.LocationType)
                {
                    case StartupLocationType.RegistryHKCU_Run:
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                        {
                            if (key != null) key.DeleteValue(item.Name, false);
                        }
                        break;

                    case StartupLocationType.RegistryHKCU_RunOnce:
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce", true))
                        {
                            if (key != null) key.DeleteValue(item.Name, false);
                        }
                        break;

                    case StartupLocationType.RegistryHKLM_Run:
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                        {
                            if (key != null) key.DeleteValue(item.Name, false);
                        }
                        break;

                    case StartupLocationType.RegistryHKLM_RunOnce:
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce", true))
                        {
                            if (key != null) key.DeleteValue(item.Name, false);
                        }
                        break;

                    case StartupLocationType.RegistryHKLM_Wow64:
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", true))
                        {
                            if (key != null) key.DeleteValue(item.Name, false);
                        }
                        break;

                    case StartupLocationType.FolderUserStartup:
                    case StartupLocationType.FolderCommonStartup:
                        if (File.Exists(item.Command))
                        {
                            File.Delete(item.Command);
                        }
                        break;
                }

                lock (_syncLock)
                {
                    string key = item.LocationPath + "::" + item.Name;
                    _knownSnapshot.Remove(key);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ExportToCsv(List<StartupItem> items)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Pexoris StartupNotifier Security Inventory Report ===");
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Total Monitored Locations: 7");
            sb.AppendLine("Total Auto-Start Entries: " + items.Count);
            sb.AppendLine();
            sb.AppendLine("Application Name,Scope,Location Type,Registry Key or Folder,Target Command,Target File Exists");

            foreach (StartupItem it in items)
            {
                sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\"",
                    it.Name.Replace("\"", "\"\""),
                    it.Scope,
                    it.LocationDisplay,
                    it.LocationPath.Replace("\"", "\"\""),
                    it.Command.Replace("\"", "\"\""),
                    it.TargetExists ? "YES" : "MISSING"));
            }

            return sb.ToString();
        }
    }
}
