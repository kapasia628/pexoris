using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace Pexoris.PortKiller
{
    public class PortItem
    {
        public string Protocol { get; set; }
        public int LocalPort { get; set; }
        public string LocalAddress { get; set; }
        public string RemoteAddress { get; set; }
        public string State { get; set; }
        public int Pid { get; set; }
        public string ProcessName { get; set; }
        public string ExecutablePath { get; set; }
        public string WindowTitle { get; set; }
        public bool IsSystem { get; set; }
    }

    public static class SocketHelper
    {
        private const int AF_INET = 2;
        private const int AF_INET6 = 23;

        private enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        private enum UDP_TABLE_CLASS
        {
            UDP_TABLE_BASIC,
            UDP_TABLE_OWNER_PID,
            UDP_TABLE_OWNER_MODULE
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddr;
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            public uint remoteAddr;
            public byte remotePort1;
            public byte remotePort2;
            public byte remotePort3;
            public byte remotePort4;
            public uint owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPROW_OWNER_PID
        {
            public uint localAddr;
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            public uint owningPid;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int pdwSize,
            bool bOrder,
            int ulAf,
            TCP_TABLE_CLASS TableClass,
            uint Reserved = 0);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(
            IntPtr pUdpTable,
            ref int pdwSize,
            bool bOrder,
            int ulAf,
            UDP_TABLE_CLASS TableClass,
            uint Reserved = 0);

        public static List<PortItem> GetAllActivePorts()
        {
            List<PortItem> list = new List<PortItem>();
            GetTcpConnections(list);
            GetUdpListeners(list);
            return list;
        }

        private static void GetTcpConnections(List<PortItem> list)
        {
            int bufferSize = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
            IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                uint ret = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != 0) return;

                int numEntries = Marshal.ReadInt32(tcpTablePtr);
                IntPtr rowPtr = (IntPtr)((long)tcpTablePtr + 4);

                int structSize = Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID));

                for (int i = 0; i < numEntries; i++)
                {
                    MIB_TCPROW_OWNER_PID row = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                    int localPort = (row.localPort1 << 8) + row.localPort2;
                    int remotePort = (row.remotePort1 << 8) + row.remotePort2;

                    string stateStr = ResolveTcpState(row.state);
                    int pid = (int)row.owningPid;

                    PortItem item = CreatePortItem("TCP", localPort, new IPAddress(row.localAddr).ToString(), remotePort, new IPAddress(row.remoteAddr).ToString(), stateStr, pid);
                    list.Add(item);

                    rowPtr = (IntPtr)((long)rowPtr + structSize);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTablePtr);
            }
        }

        private static void GetUdpListeners(List<PortItem> list)
        {
            int bufferSize = 0;
            GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
            IntPtr udpTablePtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                uint ret = GetExtendedUdpTable(udpTablePtr, ref bufferSize, true, AF_INET, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
                if (ret != 0) return;

                int numEntries = Marshal.ReadInt32(udpTablePtr);
                IntPtr rowPtr = (IntPtr)((long)udpTablePtr + 4);

                int structSize = Marshal.SizeOf(typeof(MIB_UDPROW_OWNER_PID));

                for (int i = 0; i < numEntries; i++)
                {
                    MIB_UDPROW_OWNER_PID row = (MIB_UDPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_PID));
                    int localPort = (row.localPort1 << 8) + row.localPort2;
                    int pid = (int)row.owningPid;

                    PortItem item = CreatePortItem("UDP", localPort, new IPAddress(row.localAddr).ToString(), 0, "*:*", "LISTENING", pid);
                    list.Add(item);

                    rowPtr = (IntPtr)((long)rowPtr + structSize);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(udpTablePtr);
            }
        }

        private static PortItem CreatePortItem(string proto, int localPort, string localAddr, int remotePort, string remoteAddr, string state, int pid)
        {
            PortItem item = new PortItem
            {
                Protocol = proto,
                LocalPort = localPort,
                LocalAddress = localAddr,
                RemoteAddress = remoteAddr != "*:*" ? remoteAddr + ":" + remotePort : "*:*",
                State = state,
                Pid = pid,
                ProcessName = "Unknown",
                ExecutablePath = "-",
                WindowTitle = "",
                IsSystem = false
            };

            if (pid == 0)
            {
                item.ProcessName = "System Idle";
                item.IsSystem = true;
                return item;
            }

            if (pid == 4)
            {
                item.ProcessName = "System (HTTP.sys / IIS)";
                item.ExecutablePath = "C:\\Windows\\System32\\ntoskrnl.exe";
                item.IsSystem = true;
                return item;
            }

            try
            {
                using (Process p = Process.GetProcessById(pid))
                {
                    item.ProcessName = p.ProcessName + ".exe";
                    item.WindowTitle = p.MainWindowTitle;
                    try
                    {
                        item.ExecutablePath = p.MainModule.FileName;
                    }
                    catch
                    {
                        item.ExecutablePath = "Protected / System Service";
                    }
                }
            }
            catch
            {
                item.ProcessName = "PID " + pid + " (Terminated/Inaccessible)";
            }

            return item;
        }

        private static string ResolveTcpState(uint state)
        {
            switch (state)
            {
                case 1: return "CLOSED";
                case 2: return "LISTENING";
                case 3: return "SYN_SENT";
                case 4: return "SYN_RCVD";
                case 5: return "ESTABLISHED";
                case 6: return "FIN_WAIT_1";
                case 7: return "FIN_WAIT_2";
                case 8: return "CLOSE_WAIT";
                case 9: return "CLOSING";
                case 10: return "LAST_ACK";
                case 11: return "TIME_WAIT";
                case 12: return "DELETE_TCB";
                default: return "UNKNOWN";
            }
        }

        public static bool TerminateProcess(int pid, out string error)
        {
            error = "";
            if (pid <= 4)
            {
                error = "PID " + pid + " is a protected Windows Core System process and cannot be directly terminated.";
                return false;
            }

            try
            {
                using (Process p = Process.GetProcessById(pid))
                {
                    p.Kill();
                    p.WaitForExit(1500);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Fallback to taskkill /F /T
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("taskkill.exe", string.Format("/F /T /PID {0}", pid))
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using (Process tk = Process.Start(psi))
                    {
                        tk.WaitForExit(2000);
                        if (tk.ExitCode == 0) return true;
                    }
                }
                catch { }

                error = ex.Message;
                return false;
            }
        }
    }
}
