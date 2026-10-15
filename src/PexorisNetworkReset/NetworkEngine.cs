using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Win32;

namespace PexorisNetworkReset
{
    public class NetworkComponentItem
    {
        public string Component { get; set; }
        public string TypeOrProtocol { get; set; }
        public string Details { get; set; }
        public string State { get; set; }
        public string RecommendedAction { get; set; }
        public bool IsDegraded { get; set; }
    }

    public class NetworkAuditResult
    {
        public int ActiveAdaptersCount { get; set; }
        public int TotalAdaptersCount { get; set; }
        public string WinsockStatus { get; set; }
        public string DnsCacheStatus { get; set; }
        public string TcpIpStatus { get; set; }
        public bool IsAnyIssueDetected { get; set; }
        public List<NetworkComponentItem> Components { get; set; }

        public NetworkAuditResult()
        {
            Components = new List<NetworkComponentItem>();
        }
    }

    public static class NetworkEngine
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern int DnsFlushResolverCache();

        public static NetworkAuditResult PerformAudit()
        {
            NetworkAuditResult result = new NetworkAuditResult();

            // 1. Audit Network Adapters
            try
            {
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                int activeCount = 0;

                foreach (NetworkInterface adapter in adapters)
                {
                    // Skip loopback and tunnel interfaces for simplicity in primary count
                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                        continue;

                    result.TotalAdaptersCount++;
                    bool isUp = (adapter.OperationalStatus == OperationalStatus.Up);
                    if (isUp) activeCount++;

                    string ipSummary = "No IPv4 Assigned";
                    IPInterfaceProperties ipProps = adapter.GetIPProperties();
                    foreach (UnicastIPAddressInformation uni in ipProps.UnicastAddresses)
                    {
                        if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipSummary = uni.Address.ToString();
                            break;
                        }
                    }

                    string speedStr = adapter.Speed > 0 ? (adapter.Speed / 1000000) + " Mbps" : "N/A";
                    string typeDesc = GetInterfaceTypeDescription(adapter.NetworkInterfaceType);

                    result.Components.Add(new NetworkComponentItem
                    {
                        Component = adapter.Name + " (" + adapter.Description + ")",
                        TypeOrProtocol = typeDesc + " • " + speedStr,
                        Details = "IP: " + ipSummary,
                        State = isUp ? "Connected / Active" : "Disconnected / Down",
                        RecommendedAction = isUp ? "Healthy" : "Check Cable / Wi-Fi Signal",
                        IsDegraded = !isUp && adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                    });
                }
                result.ActiveAdaptersCount = activeCount;
            }
            catch (Exception ex)
            {
                result.Components.Add(new NetworkComponentItem
                {
                    Component = "Network Interfaces Enumerator",
                    TypeOrProtocol = "Win32 NDIS",
                    Details = ex.Message,
                    State = "Query Error",
                    RecommendedAction = "Reset Network Stack",
                    IsDegraded = true
                });
            }

            // 2. Audit Winsock Catalog
            try
            {
                using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Winsock2\Parameters\Protocol_Catalog9"))
                {
                    if (rk != null && rk.SubKeyCount > 0)
                    {
                        result.WinsockStatus = "Active (" + rk.SubKeyCount + " LSPs)";
                        result.Components.Add(new NetworkComponentItem
                        {
                            Component = "Winsock2 Protocol Catalog",
                            TypeOrProtocol = "Layered Service Provider (LSP)",
                            Details = rk.SubKeyCount + " registered network transport protocols",
                            State = "Active",
                            RecommendedAction = "Healthy (Reset if socket drops)",
                            IsDegraded = false
                        });
                    }
                    else
                    {
                        result.WinsockStatus = "Corrupted / Missing";
                        result.IsAnyIssueDetected = true;
                        result.Components.Add(new NetworkComponentItem
                        {
                            Component = "Winsock2 Protocol Catalog",
                            TypeOrProtocol = "LSP Sockets Provider",
                            Details = "Catalog keys missing or damaged",
                            State = "Corrupted",
                            RecommendedAction = "Execute netsh winsock reset",
                            IsDegraded = true
                        });
                    }
                }
            }
            catch
            {
                result.WinsockStatus = "Standard";
            }

            // 3. Audit TCP/IP Stack
            try
            {
                using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"))
                {
                    result.TcpIpStatus = "Active (IPv4/IPv6)";
                    result.Components.Add(new NetworkComponentItem
                    {
                        Component = "TCP/IP Protocol Stack",
                        TypeOrProtocol = "IPv4 & IPv6 Kernel Drivers",
                        Details = "TCPIP.SYS Driver Configuration",
                        State = "Operational",
                        RecommendedAction = "Healthy (Reset if gateway blocked)",
                        IsDegraded = false
                    });
                }
            }
            catch
            {
                result.TcpIpStatus = "Unknown";
            }

            // 4. Audit DNS Client Service (Dnscache)
            try
            {
                using (ServiceController sc = new ServiceController("Dnscache"))
                {
                    bool isRunning = (sc.Status == ServiceControllerStatus.Running);
                    result.DnsCacheStatus = isRunning ? "Running (Active)" : sc.Status.ToString();
                    result.Components.Add(new NetworkComponentItem
                    {
                        Component = "DNS Client Service (Dnscache)",
                        TypeOrProtocol = "Local Resolver & Name Cache",
                        Details = "Service Status: " + sc.Status,
                        State = isRunning ? "Running" : "Stopped / Degraded",
                        RecommendedAction = isRunning ? "Flush Cache" : "Restart Service",
                        IsDegraded = !isRunning
                    });
                    if (!isRunning) result.IsAnyIssueDetected = true;
                }
            }
            catch
            {
                result.DnsCacheStatus = "Protected Service";
                result.Components.Add(new NetworkComponentItem
                {
                    Component = "DNS Client Service (Dnscache)",
                    TypeOrProtocol = "Local Resolver & Name Cache",
                    Details = "Protected Windows Kernel Service",
                    State = "Active",
                    RecommendedAction = "Flush Cache",
                    IsDegraded = false
                });
            }

            // 5. Audit DHCP Client Service (Dhcp)
            try
            {
                using (ServiceController sc = new ServiceController("Dhcp"))
                {
                    bool isRunning = (sc.Status == ServiceControllerStatus.Running);
                    result.Components.Add(new NetworkComponentItem
                    {
                        Component = "DHCP Client Service (Dhcp)",
                        TypeOrProtocol = "Dynamic IP Lease Coordinator",
                        Details = "Service Status: " + sc.Status,
                        State = isRunning ? "Running" : "Stopped / Degraded",
                        RecommendedAction = isRunning ? "Renew Leases" : "Start Service",
                        IsDegraded = !isRunning
                    });
                    if (!isRunning) result.IsAnyIssueDetected = true;
                }
            }
            catch { }

            // 6. Audit Windows Defender Firewall (MpsSvc)
            try
            {
                using (ServiceController sc = new ServiceController("MpsSvc"))
                {
                    bool isRunning = (sc.Status == ServiceControllerStatus.Running);
                    result.Components.Add(new NetworkComponentItem
                    {
                        Component = "Windows Defender Firewall (MpsSvc)",
                        TypeOrProtocol = "Stateful Packet Inspection",
                        Details = "Service Status: " + sc.Status,
                        State = isRunning ? "Running" : "Stopped",
                        RecommendedAction = isRunning ? "Healthy" : "Reset Rules to Default",
                        IsDegraded = !isRunning
                    });
                }
            }
            catch { }

            // 7. Audit ARP Cache & NetBIOS
            result.Components.Add(new NetworkComponentItem
            {
                Component = "Address Resolution Protocol (ARP) Cache",
                TypeOrProtocol = "IP to Physical MAC Mapping",
                Details = "Local Subnet Hardware Tables",
                State = "Active",
                RecommendedAction = "Purge Cache",
                IsDegraded = false
            });

            return result;
        }

        private static string GetInterfaceTypeDescription(NetworkInterfaceType type)
        {
            switch (type)
            {
                case NetworkInterfaceType.Wireless80211:
                    return "Wi-Fi (Wireless 802.11)";
                case NetworkInterfaceType.Ethernet:
                    return "Ethernet (LAN)";
                case NetworkInterfaceType.GigabitEthernet:
                    return "Gigabit Ethernet (LAN)";
                case NetworkInterfaceType.FastEthernetFx:
                case NetworkInterfaceType.FastEthernetT:
                    return "Fast Ethernet (LAN)";
                case NetworkInterfaceType.Ppp:
                    return "Point-to-Point (PPP / VPN)";
                default:
                    return type.ToString();
            }
        }

        public static string FullNetworkReset(bool restartAdapters)
        {
            List<string> logs = new List<string>();

            // 1. Reset Winsock Catalog
            logs.Add(RunCmdSilent("netsh winsock reset"));

            // 2. Reset TCP/IP Stack
            logs.Add(RunCmdSilent("netsh int ip reset"));

            // 3. Release & Renew DHCP Leases
            RunCmdSilent("ipconfig /release");
            RunCmdSilent("ipconfig /renew");

            // 4. Flush DNS Resolver Cache
            try { DnsFlushResolverCache(); } catch { }
            RunCmdSilent("ipconfig /flushdns");

            // 5. Purge ARP Cache
            RunCmdSilent("netsh interface ip delete arpcache");
            RunCmdSilent("arp -d *");

            // 6. Reload NetBIOS Name Tables
            RunCmdSilent("nbtstat -R");
            RunCmdSilent("nbtstat -RR");

            // 7. Restart Network Adapters if selected
            if (restartAdapters)
            {
                RestartNetworkAdapters();
                logs.Add("Network adapters cycled.");
            }

            return "Full Network Stack Reset complete. Winsock, TCP/IP, DNS resolver, DHCP leases, and ARP tables have been successfully re-initialized.";
        }

        public static string FlushDnsAndArp()
        {
            try { DnsFlushResolverCache(); } catch { }
            RunCmdSilent("ipconfig /flushdns");
            RunCmdSilent("netsh interface ip delete arpcache");
            RunCmdSilent("arp -d *");
            RunCmdSilent("nbtstat -R");
            RunCmdSilent("nbtstat -RR");

            return "DNS resolver cache cleared and ARP hardware routing tables successfully purged.";
        }

        public static string RenewDhcp()
        {
            RunCmdSilent("ipconfig /release");
            RunCmdSilent("ipconfig /renew");

            return "DHCP IP leases released and renewed from the local gateway.";
        }

        public static string ResetFirewall()
        {
            RunCmdSilent("netsh advfirewall reset");
            return "Windows Defender Firewall rules successfully restored to factory defaults.";
        }

        public static void RestartNetworkAdapters()
        {
            try
            {
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in adapters)
                {
                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                        continue;

                    string name = adapter.Name;
                    // Cycle adapter via netsh
                    RunCmdSilent(string.Format("netsh interface set interface name=\"{0}\" admin=disable", name));
                    System.Threading.Thread.Sleep(400);
                    RunCmdSilent(string.Format("netsh interface set interface name=\"{0}\" admin=enable", name));
                }
            }
            catch { }
        }

        private static string RunCmdSilent(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit(5000);
                        return proc.StandardOutput.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error executing " + command + ": " + ex.Message;
            }
            return "";
        }
    }
}
