using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

namespace PexorisFontCacheRebuilder
{
    public class FontCacheItem
    {
        public string Name { get; set; }
        public string Scope { get; set; }
        public string FullPath { get; set; }
        public long SizeBytes { get; set; }
        public bool Exists { get; set; }
        public bool IsLocked { get; set; }
        public string Status { get; set; }
    }

    public static class FontCacheEngine
    {
        private const int WM_FONTCHANGE = 0x001D;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            UIntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out UIntPtr lpdwResult);

        public static List<FontCacheItem> ScanCacheFiles()
        {
            List<FontCacheItem> items = new List<FontCacheItem>();

            // 1. Classic GDI System Font Cache
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string gdiCache = Path.Combine(winDir, @"System32\FNTCACHE.DAT");
            items.Add(InspectFile("FNTCACHE.DAT", "System GDI", gdiCache));

            // 2. LocalService AppData FontCache directory
            string localServiceDir = Path.Combine(winDir, @"ServiceProfiles\LocalService\AppData\Local\FontCache");
            if (Directory.Exists(localServiceDir))
            {
                try
                {
                    string[] files = Directory.GetFiles(localServiceDir, "*.dat", SearchOption.AllDirectories);
                    foreach (string f in files)
                    {
                        items.Add(InspectFile(Path.GetFileName(f), "LocalService System", f));
                    }
                }
                catch
                {
                    items.Add(new FontCacheItem
                    {
                        Name = "FontCache (Service)",
                        Scope = "LocalService System",
                        FullPath = localServiceDir,
                        Status = "Protected Folder"
                    });
                }
            }
            else
            {
                items.Add(new FontCacheItem
                {
                    Name = "FontCache (LocalService)",
                    Scope = "LocalService System",
                    FullPath = localServiceDir,
                    Exists = false,
                    Status = "Not Present"
                });
            }

            // 3. LocalService WPF 3.0.0.0 cache
            string wpfSystem = Path.Combine(winDir, @"ServiceProfiles\LocalService\AppData\Local\FontCache3.0.0.0.dat");
            items.Add(InspectFile("FontCache3.0.0.0.dat", "System WPF", wpfSystem));

            // 4. Current User LocalAppData FontCache
            string userLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string userCacheDir = Path.Combine(userLocal, "FontCache");
            if (Directory.Exists(userCacheDir))
            {
                try
                {
                    string[] uFiles = Directory.GetFiles(userCacheDir, "*.dat", SearchOption.AllDirectories);
                    foreach (string f in uFiles)
                    {
                        items.Add(InspectFile(Path.GetFileName(f), "User Profile", f));
                    }
                }
                catch { }
            }

            string userMsCache = Path.Combine(userLocal, @"Microsoft\FontCache");
            if (Directory.Exists(userMsCache))
            {
                try
                {
                    string[] uFiles = Directory.GetFiles(userMsCache, "*.dat", SearchOption.AllDirectories);
                    foreach (string f in uFiles)
                    {
                        items.Add(InspectFile(Path.GetFileName(f), "User Profile", f));
                    }
                }
                catch { }
            }

            return items;
        }

        private static FontCacheItem InspectFile(string name, string scope, string path)
        {
            FontCacheItem item = new FontCacheItem
            {
                Name = name,
                Scope = scope,
                FullPath = path,
                Exists = File.Exists(path)
            };

            if (item.Exists)
            {
                try
                {
                    FileInfo fi = new FileInfo(path);
                    item.SizeBytes = fi.Length;
                    item.IsLocked = IsFileLocked(path);
                    item.Status = item.IsLocked ? "Locked (In-Use)" : "Ready / Cleanable";
                }
                catch (Exception ex)
                {
                    item.Status = "Restricted: " + ex.Message;
                }
            }
            else
            {
                item.Status = "Clean / Rebuilt";
            }

            return item;
        }

        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    stream.Close();
                }
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetServiceStatus(string serviceName)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    return sc.Status.ToString();
                }
            }
            catch
            {
                return "Not Found";
            }
        }

        public static bool RebuildAllFontCaches(out string logDetails)
        {
            StringWriter sw = new StringWriter();
            int deletedCount = 0;
            long bytesFreed = 0;

            try
            {
                sw.WriteLine("[1/5] Stopping Font Cache Services...");
                StopServiceSafe("FontCache", sw);
                StopServiceSafe("FontCache3.0.0.0", sw);

                // Wait 1.5 seconds for file locks to disengage
                Thread.Sleep(1500);

                sw.WriteLine("[2/5] Purging runaway font driver hosts (fontdrvhost.exe)...");
                KillFontDrvHost(sw);

                sw.WriteLine("[3/5] Deleting corrupted font cache databases (.dat)...");

                // Target 1: System32 FNTCACHE.DAT
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string gdiCache = Path.Combine(winDir, @"System32\FNTCACHE.DAT");
                DeleteCacheFile(gdiCache, ref deletedCount, ref bytesFreed, sw);

                // Target 2: LocalService FontCache directory
                string localServiceDir = Path.Combine(winDir, @"ServiceProfiles\LocalService\AppData\Local\FontCache");
                if (Directory.Exists(localServiceDir))
                {
                    try
                    {
                        string[] files = Directory.GetFiles(localServiceDir, "*.dat", SearchOption.AllDirectories);
                        foreach (string f in files)
                        {
                            DeleteCacheFile(f, ref deletedCount, ref bytesFreed, sw);
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.WriteLine("  Notice on LocalService: " + ex.Message);
                    }
                }

                // Target 3: LocalService FontCache3.0.0.0.dat
                string wpfSystem = Path.Combine(winDir, @"ServiceProfiles\LocalService\AppData\Local\FontCache3.0.0.0.dat");
                DeleteCacheFile(wpfSystem, ref deletedCount, ref bytesFreed, sw);

                // Target 4: User LocalAppData FontCache
                string userLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string userCacheDir = Path.Combine(userLocal, "FontCache");
                if (Directory.Exists(userCacheDir))
                {
                    try
                    {
                        string[] uFiles = Directory.GetFiles(userCacheDir, "*.dat", SearchOption.AllDirectories);
                        foreach (string f in uFiles)
                        {
                            DeleteCacheFile(f, ref deletedCount, ref bytesFreed, sw);
                        }
                    }
                    catch { }
                }

                string userMsCache = Path.Combine(userLocal, @"Microsoft\FontCache");
                if (Directory.Exists(userMsCache))
                {
                    try
                    {
                        string[] uFiles = Directory.GetFiles(userMsCache, "*.dat", SearchOption.AllDirectories);
                        foreach (string f in uFiles)
                        {
                            DeleteCacheFile(f, ref deletedCount, ref bytesFreed, sw);
                        }
                    }
                    catch { }
                }

                sw.WriteLine("[4/5] Restarting Windows Font Cache Services...");
                StartServiceSafe("FontCache", sw);

                sw.WriteLine("[5/5] Broadcasting WM_FONTCHANGE to all Windows shells...");
                BroadcastFontChange();

                sw.WriteLine(string.Format("Success! Purged {0} corrupted cache files ({1:N1} KB freed).",
                    deletedCount, bytesFreed / 1024.0));

                logDetails = sw.ToString();
                return true;
            }
            catch (Exception ex)
            {
                sw.WriteLine("Error during cache rebuild: " + ex.Message);
                logDetails = sw.ToString();
                return false;
            }
        }

        private static void DeleteCacheFile(string path, ref int count, ref long freed, StringWriter sw)
        {
            if (File.Exists(path))
            {
                try
                {
                    FileInfo fi = new FileInfo(path);
                    long len = fi.Length;
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                    count++;
                    freed += len;
                    sw.WriteLine("  Deleted: " + Path.GetFileName(path) + string.Format(" ({0:N0} bytes)", len));
                }
                catch (Exception ex)
                {
                    sw.WriteLine("  Could not delete " + Path.GetFileName(path) + ": " + ex.Message);
                }
            }
        }

        public static void StopServiceSafe(string name, StringWriter sw)
        {
            try
            {
                using (ServiceController sc = new ServiceController(name))
                {
                    if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.StartPending)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                        sw.WriteLine("  Stopped service: " + name);
                    }
                    else
                    {
                        sw.WriteLine("  Service already stopped: " + name);
                    }
                }
            }
            catch
            {
                // Fallback to sc.exe
                RunCmdHidden("net", "stop " + name);
            }
        }

        public static void StartServiceSafe(string name, StringWriter sw)
        {
            try
            {
                using (ServiceController sc = new ServiceController(name))
                {
                    if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                        sw.WriteLine("  Started service: " + name);
                    }
                    else
                    {
                        sw.WriteLine("  Service already running: " + name);
                    }
                }
            }
            catch
            {
                RunCmdHidden("net", "start " + name);
            }
        }

        public static void KillFontDrvHost(StringWriter sw)
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("fontdrvhost");
                foreach (Process p in procs)
                {
                    try
                    {
                        p.Kill();
                        if (sw != null) sw.WriteLine(string.Format("  Terminated fontdrvhost.exe (PID {0})", p.Id));
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void BroadcastFontChange()
        {
            try
            {
                UIntPtr result;
                SendMessageTimeout(HWND_BROADCAST, (uint)WM_FONTCHANGE, UIntPtr.Zero, IntPtr.Zero, 0x0002 /* SMTO_ABORTIFHUNG */, 2000, out result);
            }
            catch { }
        }

        public static void RestartWindowsExplorer()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("explorer");
                foreach (Process p in procs)
                {
                    try { p.Kill(); } catch { }
                }
                Thread.Sleep(800);
                Process.Start("explorer.exe");
            }
            catch { }
        }

        private static void RunCmdHidden(string exe, string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process p = Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(4000);
                }
            }
            catch { }
        }
    }
}
