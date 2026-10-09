using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace PexorisWifiKeyRevealer
{
    public class WifiProfileInfo
    {
        public string Ssid { get; set; }
        public string Authentication { get; set; }
        public string Cipher { get; set; }
        public string Password { get; set; }
        public bool HasPassword { get; set; }
        public string ConnectionMode { get; set; }
        public string InterfaceName { get; set; }
    }

    public static class WifiEngine
    {
        public static List<WifiProfileInfo> GetAllProfiles()
        {
            List<WifiProfileInfo> list = new List<WifiProfileInfo>();

            // Step 1: List all Wi-Fi profiles
            List<string> profileNames = GetProfileNames();

            // Step 2: Query details & key=clear for each profile
            foreach (string name in profileNames)
            {
                WifiProfileInfo info = GetProfileDetails(name);
                if (info != null)
                {
                    list.Add(info);
                }
            }

            return list;
        }

        private static List<string> GetProfileNames()
        {
            List<string> names = new List<string>();
            try
            {
                string output = RunNetshCommand("wlan show profiles");
                if (string.IsNullOrEmpty(output)) return names;

                string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    // Match line: "    All User Profile     : NetworkName"
                    if (line.Contains(":") && (line.ToLower().Contains("all user profile") || line.ToLower().Contains("user profile")))
                    {
                        int colonIndex = line.IndexOf(':');
                        if (colonIndex >= 0 && colonIndex < line.Length - 1)
                        {
                            string profile = line.Substring(colonIndex + 1).Trim();
                            if (!string.IsNullOrEmpty(profile) && !names.Contains(profile))
                            {
                                names.Add(profile);
                            }
                        }
                    }
                }
            }
            catch { }
            return names;
        }

        private static WifiProfileInfo GetProfileDetails(string profileName)
        {
            WifiProfileInfo info = new WifiProfileInfo();
            info.Ssid = profileName;
            info.Authentication = "Unknown";
            info.Cipher = "Unknown";
            info.Password = "";
            info.HasPassword = false;
            info.ConnectionMode = "Auto";
            info.InterfaceName = "Wi-Fi";

            try
            {
                // Execute netsh wlan show profile name="SSID" key=clear
                string command = string.Format("wlan show profile name=\"{0}\" key=clear", profileName);
                string output = RunNetshCommand(command);
                if (string.IsNullOrEmpty(output)) return info;

                string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();

                    // Authentication
                    if (trimmed.StartsWith("Authentication", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = trimmed.IndexOf(':');
                        if (idx >= 0) info.Authentication = trimmed.Substring(idx + 1).Trim();
                    }
                    // Cipher
                    else if (trimmed.StartsWith("Cipher", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = trimmed.IndexOf(':');
                        if (idx >= 0) info.Cipher = trimmed.Substring(idx + 1).Trim();
                    }
                    // Connection mode
                    else if (trimmed.StartsWith("Connection mode", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = trimmed.IndexOf(':');
                        if (idx >= 0)
                        {
                            string mode = trimmed.Substring(idx + 1).Trim();
                            info.ConnectionMode = mode.Contains("auto") ? "Connect automatically" : "Manual";
                        }
                    }
                    // Key Content (Password)
                    else if (trimmed.StartsWith("Key Content", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = trimmed.IndexOf(':');
                        if (idx >= 0)
                        {
                            info.Password = trimmed.Substring(idx + 1).Trim();
                            info.HasPassword = !string.IsNullOrEmpty(info.Password);
                        }
                    }
                    // Security key state (Absent vs Present)
                    else if (trimmed.StartsWith("Security key", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = trimmed.IndexOf(':');
                        if (idx >= 0)
                        {
                            string state = trimmed.Substring(idx + 1).Trim();
                            if (state.Equals("Absent", StringComparison.OrdinalIgnoreCase))
                            {
                                info.Password = "[Open Network • No Password]";
                                info.HasPassword = false;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(info.Password))
                {
                    if (info.Authentication.ToLower().Contains("open"))
                    {
                        info.Password = "[Open / Unprotected]";
                    }
                    else
                    {
                        info.Password = "[Not Found or Protected]";
                    }
                }
            }
            catch
            {
                info.Password = "[Error Querying]";
            }

            return info;
        }

        public static bool DeleteProfile(string profileName, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                string command = string.Format("wlan delete profile name=\"{0}\"", profileName);
                string output = RunNetshCommand(command);
                if (output != null && (output.Contains("deleted") || output.Contains("success")))
                {
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static string ExportToCsv(List<WifiProfileInfo> profiles)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Network Name (SSID),Password,Security / Auth,Cipher,Connection Mode");

            foreach (var p in profiles)
            {
                string safeSsid = p.Ssid.Replace("\"", "\"\"");
                string safePass = p.Password.Replace("\"", "\"\"");
                sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\"",
                    safeSsid, safePass, p.Authentication, p.Cipher, p.ConnectionMode));
            }

            return sb.ToString();
        }

        public static string ExportToTextReport(List<WifiProfileInfo> profiles)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine(" PEXORIS WIFI KEY REVEALER — SAVED WI-FI SECURITY REPORT");
            sb.AppendLine(" Generated: " + DateTime.Now.ToString("F"));
            sb.AppendLine(" Total Saved Profiles: " + profiles.Count);
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            int index = 1;
            foreach (var p in profiles)
            {
                sb.AppendLine(string.Format("[{0:D2}] SSID: {1}", index++, p.Ssid));
                sb.AppendLine(string.Format("     Password:    {0}", p.Password));
                sb.AppendLine(string.Format("     Security:    {0} ({1})", p.Authentication, p.Cipher));
                sb.AppendLine(string.Format("     Mode:        {0}", p.ConnectionMode));
                sb.AppendLine("--------------------------------------------------------------------------------");
            }

            return sb.ToString();
        }

        private static string RunNetshCommand(string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netsh.exe", arguments);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.StandardOutputEncoding = Encoding.Default;

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);
                    return output;
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
