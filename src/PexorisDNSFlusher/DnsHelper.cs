using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace PexorisDNSFlusher
{
    public class NetworkAdapterInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string IpAddresses { get; set; }
        public string Gateway { get; set; }
        public string DnsServers { get; set; }
        public bool IsUp { get; set; }
    }

    public class PingResult
    {
        public string Host { get; set; }
        public string Provider { get; set; }
        public long LatencyMs { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class DnsHelper
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
        private static extern int DnsFlushResolverCacheNative();

        public static bool FlushDnsNative()
        {
            try
            {
                int res = DnsFlushResolverCacheNative();
                return res != 0;
            }
            catch
            {
                return false;
            }
        }

        public static string ExecuteCommand(string fileName, string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Default,
                    StandardErrorEncoding = Encoding.Default
                };

                using (Process proc = Process.Start(psi))
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(10000);

                    string combined = (stdout + "\n" + stderr).Trim();
                    return string.IsNullOrEmpty(combined) ? "Command completed with code " + proc.ExitCode : combined;
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public static string FlushDnsCache()
        {
            bool nativeSuccess = FlushDnsNative();
            string cliOutput = ExecuteCommand("ipconfig.exe", "/flushdns");
            return cliOutput;
        }

        public static string RegisterDns()
        {
            return ExecuteCommand("ipconfig.exe", "/registerdns");
        }

        public static string ReleaseIp()
        {
            return ExecuteCommand("ipconfig.exe", "/release");
        }

        public static string RenewIp()
        {
            return ExecuteCommand("ipconfig.exe", "/renew");
        }

        public static string ResetWinsock()
        {
            return ExecuteCommand("netsh.exe", "winsock reset");
        }

        public static string ResetIpStack()
        {
            return ExecuteCommand("netsh.exe", "int ip reset");
        }

        public static string ClearArpCache()
        {
            return ExecuteCommand("netsh.exe", "interface ip delete arpcache");
        }

        public static string ReloadNetBios()
        {
            return ExecuteCommand("nbtstat.exe", "-R");
        }

        public static List<NetworkAdapterInfo> GetNetworkAdapters()
        {
            List<NetworkAdapterInfo> list = new List<NetworkAdapterInfo>();
            try
            {
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in adapters)
                {
                    // Skip loopback
                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    IPInterfaceProperties props = adapter.GetIPProperties();
                    
                    List<string> ips = new List<string>();
                    foreach (UnicastIPAddressInformation uni in props.UnicastAddresses)
                    {
                        if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ips.Add(uni.Address.ToString());
                        }
                    }

                    List<string> gateways = new List<string>();
                    foreach (GatewayIPAddressInformation gw in props.GatewayAddresses)
                    {
                        gateways.Add(gw.Address.ToString());
                    }

                    List<string> dnsList = new List<string>();
                    foreach (IPAddress dns in props.DnsAddresses)
                    {
                        if (dns.AddressFamily == AddressFamily.InterNetwork || !dns.IsIPv6LinkLocal)
                        {
                            dnsList.Add(dns.ToString());
                        }
                    }

                    NetworkAdapterInfo info = new NetworkAdapterInfo
                    {
                        Name = adapter.Name,
                        Description = adapter.Description,
                        Status = adapter.OperationalStatus == OperationalStatus.Up ? "Connected" : "Disconnected",
                        Type = adapter.NetworkInterfaceType.ToString(),
                        IpAddresses = ips.Count > 0 ? string.Join(", ", ips.ToArray()) : "No IPv4",
                        Gateway = gateways.Count > 0 ? string.Join(", ", gateways.ToArray()) : "None",
                        DnsServers = dnsList.Count > 0 ? string.Join(", ", dnsList.ToArray()) : "None",
                        IsUp = adapter.OperationalStatus == OperationalStatus.Up
                    };

                    list.Add(info);
                }
            }
            catch { }
            return list;
        }

        public static PingResult TestPing(string host, string provider)
        {
            PingResult result = new PingResult { Host = host, Provider = provider, Success = false };
            try
            {
                using (Ping p = new Ping())
                {
                    PingReply reply = p.Send(host, 1800);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        result.Success = true;
                        result.LatencyMs = reply.RoundtripTime;
                    }
                    else
                    {
                        result.ErrorMessage = reply != null ? reply.Status.ToString() : "Timeout";
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            }
            return result;
        }

        public static int GetCustomHostsCount()
        {
            try
            {
                string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string hostsPath = Path.Combine(windir, @"system32\drivers\etc\hosts");
                if (!File.Exists(hostsPath)) return 0;

                string[] lines = File.ReadAllLines(hostsPath);
                int count = 0;
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                    {
                        count++;
                    }
                }
                return count;
            }
            catch
            {
                return 0;
            }
        }
    }
}
