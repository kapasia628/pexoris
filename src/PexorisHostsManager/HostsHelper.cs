using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace PexorisHostsManager
{
    public class HostEntry
    {
        public bool IsEnabled { get; set; }
        public string IpAddress { get; set; }
        public string Hostname { get; set; }
        public string Comment { get; set; }
        public bool IsHeader { get; set; }
        public string RawText { get; set; }

        public HostEntry()
        {
            IsEnabled = true;
            IpAddress = "";
            Hostname = "";
            Comment = "";
            IsHeader = false;
        }

        public string ToLine()
        {
            if (IsHeader) return RawText;

            StringBuilder sb = new StringBuilder();
            if (!IsEnabled) sb.Append("# ");
            sb.Append((IpAddress ?? "127.0.0.1").PadRight(16));
            sb.Append(" ");
            sb.Append(Hostname ?? "");
            if (!string.IsNullOrEmpty(Comment))
            {
                sb.Append("  # ");
                sb.Append(Comment.TrimStart('#', ' '));
            }
            return sb.ToString();
        }
    }

    public static class HostsHelper
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
        private static extern bool DnsFlushResolverCache();

        public static string GetHostsPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
        }

        public static List<HostEntry> LoadHosts()
        {
            var list = new List<HostEntry>();
            string path = GetHostsPath();

            if (!File.Exists(path)) return list;

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                    {
                        list.Add(new HostEntry { IsHeader = true, RawText = "" });
                        continue;
                    }

                    // Check if it's a comment or potentially disabled entry
                    bool isCommented = trimmed.StartsWith("#");
                    string content = isCommented ? trimmed.Substring(1).Trim() : trimmed;

                    // Parse parts
                    string[] tokens = content.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length >= 2 && IsValidIp(tokens[0]))
                    {
                        string ip = tokens[0];
                        string host = tokens[1];
                        string comment = "";

                        // Inline comment after host
                        int hashIndex = line.IndexOf('#', line.IndexOf(host));
                        if (hashIndex > -1)
                        {
                            comment = line.Substring(hashIndex + 1).Trim();
                        }

                        list.Add(new HostEntry
                        {
                            IsEnabled = !isCommented,
                            IpAddress = ip,
                            Hostname = host,
                            Comment = comment,
                            IsHeader = false
                        });
                    }
                    else
                    {
                        // Genuine header / commentary line
                        list.Add(new HostEntry
                        {
                            IsHeader = true,
                            RawText = line
                        });
                    }
                }
            }
            catch { }

            return list;
        }

        public static bool SaveHosts(List<HostEntry> entries)
        {
            string path = GetHostsPath();
            try
            {
                // Remove Read-Only attribute if present
                if (File.Exists(path))
                {
                    FileAttributes attr = File.GetAttributes(path);
                    if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
                    }
                }

                // Write file
                List<string> outputLines = new List<string>();
                foreach (var entry in entries)
                {
                    outputLines.Add(entry.ToLine());
                }

                File.WriteAllLines(path, outputLines.ToArray(), Encoding.UTF8);

                // Flush DNS
                FlushDns();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool FlushDns()
        {
            try
            {
                if (DnsFlushResolverCache()) return true;
            }
            catch { }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("ipconfig.exe", "/flushdns")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(2000);
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string CreateBackup()
        {
            try
            {
                string path = GetHostsPath();
                if (!File.Exists(path)) return null;

                string backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pexoris", "HostsManager");
                Directory.CreateDirectory(backupDir);

                string backupFile = Path.Combine(backupDir, "hosts_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                File.Copy(path, backupFile, true);
                return backupFile;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsValidIp(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            IPAddress addr;
            return IPAddress.TryParse(ip, out addr);
        }

        public static List<HostEntry> GetTelemetryAdblockPreset()
        {
            string[] blockDomains = new string[] {
                "telemetry.microsoft.com",
                "v10.events.data.microsoft.com",
                "v20.events.data.microsoft.com",
                "watson.telemetry.microsoft.com",
                "diagtrack-telemetry.cloudapp.net",
                "browser.pipe.aria.microsoft.com",
                "activity.windows.com",
                "feedback.windows.com",
                "diagnostics.support.microsoft.com",
                "adservice.google.com",
                "doubleclick.net",
                "adclick.g.doubleclick.net",
                "pagead2.googlesyndication.com"
            };

            var preset = new List<HostEntry>();
            foreach (string domain in blockDomains)
            {
                preset.Add(new HostEntry
                {
                    IsEnabled = true,
                    IpAddress = "0.0.0.0",
                    Hostname = domain,
                    Comment = "Pexoris Privacy Block",
                    IsHeader = false
                });
            }
            return preset;
        }

        public static List<HostEntry> GetDevLocalhostPreset()
        {
            return new List<HostEntry>
            {
                new HostEntry { IsEnabled = true, IpAddress = "127.0.0.1", Hostname = "app.local", Comment = "Local Development" },
                new HostEntry { IsEnabled = true, IpAddress = "127.0.0.1", Hostname = "api.local", Comment = "Local API Mock" },
                new HostEntry { IsEnabled = true, IpAddress = "127.0.0.1", Hostname = "test.local", Comment = "Staging Local" }
            };
        }

        public static string GetDefaultWindowsHostsContent()
        {
            return @"# Copyright (c) 1993-2009 Microsoft Corp.
#
# This is a sample HOSTS file used by Microsoft TCP/IP for Windows.
#
# This file contains the mappings of IP addresses to host names. Each
# entry should be kept on an individual line. The IP address should
# be placed in the first column followed by the corresponding host name.
# The IP address and the host name should be separated by at least one
# space.
#
# Additionally, comments (such as these) may be inserted on individual
# lines or following the machine name denoted by a '#' symbol.
#
# For example:
#
#      102.54.94.97     rhino.acme.com          # source server
#       38.25.63.10     x.acme.com              # x client host

# localhost name resolution is handled within DNS itself.
#	127.0.0.1       localhost
#	::1             localhost
";
        }
    }
}
