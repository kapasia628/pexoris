using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PexorisProcessTamer
{
    public class ProcessItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double CpuPercent { get; set; }
        public long MemoryMb { get; set; }
        public ProcessPriorityClass Priority { get; set; }
        public int ThreadCount { get; set; }
        public bool IsTamed { get; set; }
        public bool IsWhitelisted { get; set; }

        public string PriorityDisplay
        {
            get
            {
                switch (Priority)
                {
                    case ProcessPriorityClass.RealTime: return "Realtime";
                    case ProcessPriorityClass.High: return "High";
                    case ProcessPriorityClass.AboveNormal: return "Above Normal";
                    case ProcessPriorityClass.Normal: return "Normal";
                    case ProcessPriorityClass.BelowNormal: return "Below Normal";
                    case ProcessPriorityClass.Idle: return "Idle (Tamed)";
                    default: return "Normal";
                }
            }
        }
    }

    public static class ProcessEngine
    {
        private static readonly HashSet<string> Whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "System", "Idle", "Registry", "smss", "csrss", "wininit", "services",
            "lsass", "svchost", "winlogon", "fontdrvhost", "dwm", "explorer",
            "PexorisProcessTamer", "taskhostw", "sihost", "RuntimeBroker"
        };

        private static Dictionary<int, TimeSpan> _lastCpuTimes = new Dictionary<int, TimeSpan>();
        private static DateTime _lastSampleTime = DateTime.MinValue;

        public static bool IsSystemWhitelisted(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return true;
            return Whitelist.Contains(processName);
        }

        public static List<ProcessItem> ScanProcesses()
        {
            List<ProcessItem> list = new List<ProcessItem>();
            DateTime now = DateTime.UtcNow;
            double elapsedMs = _lastSampleTime == DateTime.MinValue ? 0 : (now - _lastSampleTime).TotalMilliseconds;
            int processorCount = Environment.ProcessorCount;

            Dictionary<int, TimeSpan> currentCpuTimes = new Dictionary<int, TimeSpan>();

            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                try
                {
                    int pid = p.Id;
                    string name = p.ProcessName;
                    bool whitelisted = IsSystemWhitelisted(name);

                    // Skip System Idle Process
                    if (pid == 0) continue;

                    TimeSpan totalCpu = TimeSpan.Zero;
                    try { totalCpu = p.TotalProcessorTime; } catch { }
                    currentCpuTimes[pid] = totalCpu;

                    double cpuPercent = 0.0;
                    if (elapsedMs > 200 && _lastCpuTimes.ContainsKey(pid))
                    {
                        TimeSpan prevCpu = _lastCpuTimes[pid];
                        double cpuUsedMs = (totalCpu - prevCpu).TotalMilliseconds;
                        cpuPercent = (cpuUsedMs / (elapsedMs * processorCount)) * 100.0;
                        if (cpuPercent < 0) cpuPercent = 0;
                        if (cpuPercent > 100) cpuPercent = 100;
                    }

                    long memMb = 0;
                    try { memMb = p.WorkingSet64 / (1024 * 1024); } catch { }

                    ProcessPriorityClass prio = ProcessPriorityClass.Normal;
                    try { prio = p.PriorityClass; } catch { }

                    int threads = 0;
                    try { threads = p.Threads.Count; } catch { }

                    bool isTamed = (prio == ProcessPriorityClass.Idle || prio == ProcessPriorityClass.BelowNormal);

                    list.Add(new ProcessItem
                    {
                        Id = pid,
                        Name = name,
                        CpuPercent = Math.Round(cpuPercent, 1),
                        MemoryMb = memMb,
                        Priority = prio,
                        ThreadCount = threads,
                        IsTamed = isTamed,
                        IsWhitelisted = whitelisted
                    });
                }
                catch
                {
                    // Process exited during iteration
                }
                finally
                {
                    try { p.Dispose(); } catch { }
                }
            }

            _lastCpuTimes = currentCpuTimes;
            _lastSampleTime = now;

            // Sort by CPU descending, then Memory descending
            return list.OrderByDescending(x => x.CpuPercent).ThenByDescending(x => x.MemoryMb).ToList();
        }

        public static bool SetPriority(int pid, ProcessPriorityClass priority)
        {
            try
            {
                using (Process p = Process.GetProcessById(pid))
                {
                    if (IsSystemWhitelisted(p.ProcessName) && priority == ProcessPriorityClass.Idle)
                    {
                        return false; // Prevent throttling critical system components
                    }
                    p.PriorityClass = priority;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool KillProcess(int pid)
        {
            try
            {
                using (Process p = Process.GetProcessById(pid))
                {
                    if (IsSystemWhitelisted(p.ProcessName))
                    {
                        return false;
                    }
                    p.Kill();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static int AutoTameHogs(double cpuThresholdPercent)
        {
            int tamedCount = 0;
            try
            {
                List<ProcessItem> current = ScanProcesses();
                foreach (var item in current)
                {
                    if (!item.IsWhitelisted && item.CpuPercent >= cpuThresholdPercent && !item.IsTamed)
                    {
                        if (SetPriority(item.Id, ProcessPriorityClass.BelowNormal))
                        {
                            tamedCount++;
                        }
                    }
                }
            }
            catch { }
            return tamedCount;
        }
    }
}
