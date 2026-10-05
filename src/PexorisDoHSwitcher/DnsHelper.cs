using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Pexoris.DoHSwitcher
{
    public class DnsProvider
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string PrimaryIp { get; set; }
        public string SecondaryIp { get; set; }
        public string DohTemplate { get; set; }
        public string Tag { get; set; }
        public long LatencyMs { get; set; }
        public string Description { get; set; }

        public DnsProvider(string name, string category, string pIp, string sIp, string doh, string tag, string desc)
        {
            Name = name;
            Category = category;
            PrimaryIp = pIp;
            SecondaryIp = sIp;
            DohTemplate = doh;
            Tag = tag;
            Description = desc;
            LatencyMs = -1;
        }
    }

    public class NetworkAdapterInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public NetworkInterfaceType Type { get; set; }
        public bool IsDhcp { get; set; }
        public List<string> DnsServers { get; set; }

        public NetworkAdapterInfo()
        {
            DnsServers = new List<string>();
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Type == NetworkInterfaceType.Wireless80211 ? "Wi-Fi" : "Ethernet");
        }
    }

    public static class DnsHelper
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern int DnsFlushResolverCache();

        public static List<DnsProvider> GetCuratedProviders()
        {
            return new List<DnsProvider>
            {
                new DnsProvider(
                    "Cloudflare 1.1.1.1",
                    "Speed & Privacy",
                    "1.1.1.1", "1.0.0.1",
                    "https://cloudflare-dns.com/dns-query",
                    "FASTEST",
                    "World's fastest recursive DNS. Strict zero-logging privacy policy."
                ),
                new DnsProvider(
                    "AdGuard DNS (Default)",
                    "Ad-Blocking",
                    "94.140.14.14", "94.140.15.15",
                    "https://dns.adguard-dns.com/dns-query",
                    "BLOCK ADS",
                    "Blocks system-wide ads, popups, and tracking domains automatically."
                ),
                new DnsProvider(
                    "Cloudflare Family Shield",
                    "Family / Adult Block",
                    "1.1.1.3", "1.0.0.3",
                    "https://family.cloudflare-dns.com/dns-query",
                    "FAMILY",
                    "Blocks malware, phishing, and adult/18+ content across all browsers."
                ),
                new DnsProvider(
                    "Google Public DNS",
                    "General Purpose",
                    "8.8.8.8", "8.8.4.4",
                    "https://dns.google/dns-query",
                    "RELIABLE",
                    "High-capacity global infrastructure with robust reliability."
                ),
                new DnsProvider(
                    "Quad9 (Recommended Security)",
                    "Threat Protection",
                    "9.9.9.9", "149.112.112.112",
                    "https://dns.quad9.net/dns-query",
                    "SECURITY",
                    "Swiss non-profit blocking malicious domains, botnets, and spyware."
                ),
                new DnsProvider(
                    "AdGuard Family Protection",
                    "Ads & Family",
                    "94.140.14.15", "94.140.15.16",
                    "https://dns.adguard-dns.com/dns-query",
                    "ADS + FAMILY",
                    "Combines ad-blocking with parental controls to filter adult websites."
                ),
                new DnsProvider(
                    "Cisco OpenDNS Home",
                    "Enterprise Cloud",
                    "208.67.222.222", "208.67.220.220",
                    "https://doh.opendns.com/dns-query",
                    "CISCO",
                    "Enterprise-grade cloud routing with phishing & botnet protection."
                ),
                new DnsProvider(
                    "CleanBrowsing Adult Filter",
                    "Family Shield",
                    "185.228.168.168", "185.228.169.168",
                    "https://doh.cleanbrowsing.org/doh/adult-filter/",
                    "CLEAN",
                    "Strict family protection blocking pornography, VPN proxies, and mixed content."
                )
            };
        }

        public static List<NetworkAdapterInfo> GetActiveAdapters()
        {
            var list = new List<NetworkAdapterInfo>();
            try
            {
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in adapters)
                {
                    if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                    if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                        adapter.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                        continue;

                    var info = new NetworkAdapterInfo
                    {
                        Id = adapter.Id,
                        Name = adapter.Name,
                        Description = adapter.Description,
                        Type = adapter.NetworkInterfaceType
                    };

                    IPInterfaceProperties ipProps = adapter.GetIPProperties();
                    if (ipProps != null)
                    {
                        foreach (IPAddress dns in ipProps.DnsAddresses)
                        {
                            if (dns.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                info.DnsServers.Add(dns.ToString());
                            }
                        }

                        var ipv4Props = ipProps.GetIPv4Properties();
                        if (ipv4Props != null)
                        {
                            info.IsDhcp = ipv4Props.IsDhcpEnabled;
                        }
                    }

                    list.Add(info);
                }
            }
            catch { }
            return list;
        }

        public static bool ApplyDns(string adapterName, DnsProvider provider, bool enableDoh, out string error)
        {
            error = null;
            try
            {
                // 1. Set Primary Static DNS
                string setPrimary = string.Format("interface ipv4 set dns name=\"{0}\" static {1}", adapterName, provider.PrimaryIp);
                RunNetsh(setPrimary);

                // 2. Add Secondary DNS
                if (!string.IsNullOrEmpty(provider.SecondaryIp))
                {
                    string addSecondary = string.Format("interface ipv4 add dns name=\"{0}\" {1} index=2", adapterName, provider.SecondaryIp);
                    RunNetsh(addSecondary);
                }

                // 3. Configure Windows 11 Native DoH Encryption (if template available)
                if (enableDoh && !string.IsNullOrEmpty(provider.DohTemplate))
                {
                    try
                    {
                        string enc1 = string.Format("dns add encryption server={0} dohtemplate={1} autoupgrade=yes udpfallback=no", provider.PrimaryIp, provider.DohTemplate);
                        RunNetsh(enc1);

                        if (!string.IsNullOrEmpty(provider.SecondaryIp))
                        {
                            string enc2 = string.Format("dns add encryption server={0} dohtemplate={1} autoupgrade=yes udpfallback=no", provider.SecondaryIp, provider.DohTemplate);
                            RunNetsh(enc2);
                        }
                    }
                    catch { }
                }

                FlushDnsCache();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool RestoreDhcp(string adapterName, out string error)
        {
            error = null;
            try
            {
                string cmd = string.Format("interface ipv4 set dns name=\"{0}\" dhcp", adapterName);
                RunNetsh(cmd);
                FlushDnsCache();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static void FlushDnsCache()
        {
            try
            {
                DnsFlushResolverCache();
            }
            catch { }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("ipconfig", "/flushdns")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(1000);
                }
            }
            catch { }
        }

        public static long PingServer(string ip)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(ip, 1200);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        return reply.RoundtripTime;
                    }
                }
            }
            catch { }
            return 999;
        }

        private static void RunNetsh(string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo("netsh", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process p = Process.Start(psi))
            {
                p.WaitForExit(3500);
            }
        }
    }
}
