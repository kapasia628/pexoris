using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace PexorisAppUninstaller
{
    public class InstalledAppInfo
    {
        public string DisplayName { get; set; }
        public string DisplayVersion { get; set; }
        public string Publisher { get; set; }
        public string InstallDate { get; set; }
        public string UninstallString { get; set; }
        public string QuietUninstallString { get; set; }
        public string InstallLocation { get; set; }
        public long EstimatedSizeKb { get; set; }
        public string RegistryRoot { get; set; }
        public string SubKeyName { get; set; }
        public bool Is64Bit { get; set; }
        public bool IsMsi { get; set; }

        public string FormattedSize
        {
            get
            {
                if (EstimatedSizeKb <= 0) return "-";
                if (EstimatedSizeKb < 1024) return string.Format("{0} KB", EstimatedSizeKb);
                if (EstimatedSizeKb < 1024 * 1024) return string.Format("{0:F1} MB", EstimatedSizeKb / 1024.0);
                return string.Format("{0:F2} GB", EstimatedSizeKb / (1024.0 * 1024.0));
            }
        }
    }

    public static class UninstallEngine
    {
        public static List<InstalledAppInfo> ScanInstalledApps()
        {
            Dictionary<string, InstalledAppInfo> dict = new Dictionary<string, InstalledAppInfo>(StringComparer.OrdinalIgnoreCase);

            // 1. HKLM 64-bit
            ScanRegistryKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true, dict);

            // 2. HKLM 32-bit (WOW6432Node)
            if (Environment.Is64BitOperatingSystem)
            {
                ScanRegistryKey(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", false, dict);
            }

            // 3. HKCU (Per-user)
            ScanRegistryKey(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", Environment.Is64BitOperatingSystem, dict);

            List<InstalledAppInfo> result = new List<InstalledAppInfo>(dict.Values);
            result.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private static void ScanRegistryKey(RegistryHive hive, string subKeyPath, bool is64, Dictionary<string, InstalledAppInfo> dict)
        {
            try
            {
                RegistryView view = is64 ? RegistryView.Registry64 : RegistryView.Registry32;
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view))
                using (RegistryKey uninstKey = baseKey.OpenSubKey(subKeyPath))
                {
                    if (uninstKey == null) return;

                    string[] subKeys = uninstKey.GetSubKeyNames();
                    for (int i = 0; i < subKeys.Length; i++)
                    {
                        try
                        {
                            using (RegistryKey appKey = uninstKey.OpenSubKey(subKeys[i]))
                            {
                                if (appKey == null) continue;

                                string displayName = appKey.GetValue("DisplayName") as string;
                                if (string.IsNullOrEmpty(displayName)) continue;
                                displayName = displayName.Trim();

                                // Ignore system updates and components
                                int sysComp = 0;
                                object scObj = appKey.GetValue("SystemComponent");
                                if (scObj is int) sysComp = (int)scObj;
                                if (sysComp == 1) continue;

                                string parentKey = appKey.GetValue("ParentKeyName") as string;
                                if (!string.IsNullOrEmpty(parentKey)) continue;

                                string uninstallString = appKey.GetValue("UninstallString") as string;
                                string quietString = appKey.GetValue("QuietUninstallString") as string;
                                if (string.IsNullOrEmpty(uninstallString) && string.IsNullOrEmpty(quietString)) continue;

                                string version = (appKey.GetValue("DisplayVersion") as string) ?? "";
                                string publisher = (appKey.GetValue("Publisher") as string) ?? "";
                                string installDate = (appKey.GetValue("InstallDate") as string) ?? "";
                                string location = (appKey.GetValue("InstallLocation") as string) ?? "";

                                long sizeKb = 0;
                                object sizeObj = appKey.GetValue("EstimatedSize");
                                if (sizeObj is int) sizeKb = (int)sizeObj;
                                else if (sizeObj is long) sizeKb = (long)sizeObj;

                                bool isMsi = false;
                                object msiObj = appKey.GetValue("WindowsInstaller");
                                if (msiObj is int && (int)msiObj == 1) isMsi = true;

                                if (dict.ContainsKey(displayName)) continue;

                                InstalledAppInfo info = new InstalledAppInfo
                                {
                                    DisplayName = displayName,
                                    DisplayVersion = version.Trim(),
                                    Publisher = publisher.Trim(),
                                    InstallDate = FormatInstallDate(installDate),
                                    UninstallString = (uninstallString ?? "").Trim(),
                                    QuietUninstallString = (quietString ?? "").Trim(),
                                    InstallLocation = (location ?? "").Trim(),
                                    EstimatedSizeKb = sizeKb,
                                    RegistryRoot = hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU",
                                    SubKeyName = subKeys[i],
                                    Is64Bit = is64,
                                    IsMsi = isMsi
                                };

                                dict[displayName] = info;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static string FormatInstallDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate) || rawDate.Length != 8) return rawDate;
            try
            {
                // Format YYYYMMDD -> YYYY-MM-DD
                return string.Format("{0}-{1}-{2}",
                    rawDate.Substring(0, 4),
                    rawDate.Substring(4, 2),
                    rawDate.Substring(6, 2));
            }
            catch { return rawDate; }
        }

        public static void LaunchStandardUninstall(InstalledAppInfo app)
        {
            string cmd = app.UninstallString;
            if (string.IsNullOrEmpty(cmd)) cmd = app.QuietUninstallString;
            if (string.IsNullOrEmpty(cmd)) throw new InvalidOperationException("No uninstall command found for this application.");

            ExecuteCommandLine(cmd);
        }

        public static void LaunchQuietUninstall(InstalledAppInfo app)
        {
            if (!string.IsNullOrEmpty(app.QuietUninstallString))
            {
                ExecuteCommandLine(app.QuietUninstallString);
                return;
            }

            if (app.IsMsi || app.UninstallString.IndexOf("MsiExec.exe", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Convert msiexec /i or /x to /qn quiet mode
                string msiCmd = app.UninstallString;
                if (msiCmd.IndexOf("/I", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    msiCmd = msiCmd.Replace("/I", "/X").Replace("/i", "/X");
                }
                if (msiCmd.IndexOf("/qn", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    msiCmd += " /qn /norestart";
                }
                ExecuteCommandLine(msiCmd);
                return;
            }

            // Fallback standard uninstaller with silent flag
            string exeCmd = app.UninstallString;
            if (exeCmd.IndexOf("/SILENT", StringComparison.OrdinalIgnoreCase) < 0 &&
                exeCmd.IndexOf("/S", StringComparison.OrdinalIgnoreCase) < 0)
            {
                exeCmd += " /SILENT /VERYSILENT /SUPPRESSMSGBOXES";
            }
            ExecuteCommandLine(exeCmd);
        }

        private static void ExecuteCommandLine(string commandLine)
        {
            commandLine = commandLine.Trim();
            string fileName;
            string args = "";

            if (commandLine.StartsWith("\""))
            {
                int nextQuote = commandLine.IndexOf('\"', 1);
                if (nextQuote > 0)
                {
                    fileName = commandLine.Substring(1, nextQuote - 1);
                    if (commandLine.Length > nextQuote + 1)
                    {
                        args = commandLine.Substring(nextQuote + 1).Trim();
                    }
                }
                else
                {
                    fileName = commandLine.Trim('\"');
                }
            }
            else
            {
                int firstSpace = commandLine.IndexOf(' ');
                if (firstSpace > 0 && commandLine.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == false)
                {
                    fileName = commandLine.Substring(0, firstSpace);
                    args = commandLine.Substring(firstSpace + 1).Trim();
                }
                else
                {
                    fileName = commandLine;
                }
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
        }

        public static void ForcePurgeApp(InstalledAppInfo app)
        {
            // 1. Delete from Registry
            RegistryHive hive = app.RegistryRoot == "HKLM" ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
            RegistryView view = app.Is64Bit ? RegistryView.Registry64 : RegistryView.Registry32;

            string[] pathsToTry = new string[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            for (int i = 0; i < pathsToTry.Length; i++)
            {
                try
                {
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view))
                    using (RegistryKey uninstKey = baseKey.OpenSubKey(pathsToTry[i], true))
                    {
                        if (uninstKey != null && uninstKey.OpenSubKey(app.SubKeyName) != null)
                        {
                            uninstKey.DeleteSubKeyTree(app.SubKeyName);
                        }
                    }
                }
                catch { }
            }

            // 2. Remove InstallLocation folder if specified and safe
            if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
            {
                try
                {
                    string loc = app.InstallLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    string sysRoot = Path.GetPathRoot(winDir);

                    // Safety checks: never delete root, Windows, or System32
                    if (!loc.Equals(sysRoot, StringComparison.OrdinalIgnoreCase) &&
                        !loc.Equals(winDir, StringComparison.OrdinalIgnoreCase) &&
                        loc.Length > 8)
                    {
                        Directory.Delete(loc, true);
                    }
                }
                catch { }
            }
        }

        public static void ExportReport(List<InstalledAppInfo> apps, string filePath, bool asCsv)
        {
            StringBuilder sb = new StringBuilder();

            if (asCsv)
            {
                sb.AppendLine("DisplayName,DisplayVersion,Publisher,InstallDate,Size,RegistryRoot,UninstallString");
                for (int i = 0; i < apps.Count; i++)
                {
                    InstalledAppInfo a = apps[i];
                    sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\"",
                        EscapeCsv(a.DisplayName),
                        EscapeCsv(a.DisplayVersion),
                        EscapeCsv(a.Publisher),
                        EscapeCsv(a.InstallDate),
                        EscapeCsv(a.FormattedSize),
                        EscapeCsv(a.RegistryRoot),
                        EscapeCsv(a.UninstallString)));
                }
            }
            else
            {
                sb.AppendLine("================================================================================");
                sb.AppendLine(" PEXORIS APPUNINSTALLER — INSTALLED SOFTWARE INVENTORY REPORT");
                sb.AppendLine(string.Format(" Generated on: {0} • Total Applications: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), apps.Count));
                sb.AppendLine("================================================================================");
                sb.AppendLine();

                for (int i = 0; i < apps.Count; i++)
                {
                    InstalledAppInfo a = apps[i];
                    sb.AppendLine(string.Format("[{0}] {1} (v{2})", (i + 1).ToString("D3"), a.DisplayName, string.IsNullOrEmpty(a.DisplayVersion) ? "N/A" : a.DisplayVersion));
                    sb.AppendLine(string.Format("   Publisher    : {0}", string.IsNullOrEmpty(a.Publisher) ? "Unknown" : a.Publisher));
                    sb.AppendLine(string.Format("   Install Date : {0}", string.IsNullOrEmpty(a.InstallDate) ? "Unknown" : a.InstallDate));
                    sb.AppendLine(string.Format("   Size         : {0}", a.FormattedSize));
                    sb.AppendLine(string.Format("   Registry     : {0}\\{1}", a.RegistryRoot, a.SubKeyName));
                    sb.AppendLine(string.Format("   Uninstall Cmd: {0}", a.UninstallString));
                    sb.AppendLine();
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return val.Replace("\"", "\"\"");
        }
    }
}
