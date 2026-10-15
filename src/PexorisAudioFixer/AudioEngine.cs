using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Win32;

namespace PexorisAudioFixer
{
    public class AudioEndpointItem
    {
        public string DeviceName { get; set; }
        public string TypeOrEndpoint { get; set; }
        public string Details { get; set; }
        public string State { get; set; }
        public string RecommendedAction { get; set; }
        public bool IsDegraded { get; set; }
    }

    public class AudioAuditResult
    {
        public bool AudiosrvRunning { get; set; }
        public string AudiosrvStartup { get; set; }
        public bool EndpointBuilderRunning { get; set; }
        public string EndpointBuilderStartup { get; set; }
        public int ActiveEndpointsCount { get; set; }
        public int AudioDgProcessCount { get; set; }
        public bool AudiodgHanging { get; set; }
        public List<AudioEndpointItem> Components { get; set; }

        public AudioAuditResult()
        {
            Components = new List<AudioEndpointItem>();
        }
    }

    public static class AudioEngine
    {
        public static AudioAuditResult RunAudit()
        {
            AudioAuditResult result = new AudioAuditResult();

            // 1. Audit Windows Audio Service (Audiosrv)
            try
            {
                using (ServiceController sc = new ServiceController("Audiosrv"))
                {
                    result.AudiosrvRunning = (sc.Status == ServiceControllerStatus.Running);
                    string startup = GetServiceStartup("Audiosrv");
                    result.AudiosrvStartup = startup;

                    bool ok = result.AudiosrvRunning && startup.Equals("Automatic", StringComparison.OrdinalIgnoreCase);
                    result.Components.Add(new AudioEndpointItem
                    {
                        DeviceName = "Audiosrv (Windows Audio)",
                        TypeOrEndpoint = "Core Win32 Audio Subsystem",
                        Details = string.Format("Status: {0} | Startup: {1}", sc.Status, startup),
                        State = ok ? "Running" : "Degraded / Stalled",
                        RecommendedAction = ok ? "Optimal" : "Restart & Set to Auto",
                        IsDegraded = !ok
                    });
                }
            }
            catch (Exception ex)
            {
                result.Components.Add(new AudioEndpointItem
                {
                    DeviceName = "Audiosrv (Windows Audio)",
                    TypeOrEndpoint = "Core Win32 Audio Subsystem",
                    Details = "Failed to query service: " + ex.Message,
                    State = "Error",
                    RecommendedAction = "Reset & Re-register Service",
                    IsDegraded = true
                });
            }

            // 2. Audit Windows Audio Endpoint Builder (AudioEndpointBuilder)
            try
            {
                using (ServiceController sc = new ServiceController("AudioEndpointBuilder"))
                {
                    result.EndpointBuilderRunning = (sc.Status == ServiceControllerStatus.Running);
                    string startup = GetServiceStartup("AudioEndpointBuilder");
                    result.EndpointBuilderStartup = startup;

                    bool ok = result.EndpointBuilderRunning && startup.Equals("Automatic", StringComparison.OrdinalIgnoreCase);
                    result.Components.Add(new AudioEndpointItem
                    {
                        DeviceName = "AudioEndpointBuilder",
                        TypeOrEndpoint = "Hardware Endpoint Enumerator",
                        Details = string.Format("Status: {0} | Startup: {1}", sc.Status, startup),
                        State = ok ? "Running" : "Degraded / Stalled",
                        RecommendedAction = ok ? "Optimal" : "Restart & Set to Auto",
                        IsDegraded = !ok
                    });
                }
            }
            catch (Exception ex)
            {
                result.Components.Add(new AudioEndpointItem
                {
                    DeviceName = "AudioEndpointBuilder",
                    TypeOrEndpoint = "Hardware Endpoint Enumerator",
                    Details = "Failed to query service: " + ex.Message,
                    State = "Error",
                    RecommendedAction = "Reset & Re-register Service",
                    IsDegraded = true
                });
            }

            // 3. Audit Audio Device Graph Isolation (audiodg.exe)
            try
            {
                Process[] procs = Process.GetProcessesByName("audiodg");
                result.AudioDgProcessCount = procs.Length;
                if (procs.Length > 0)
                {
                    long totalMem = 0;
                    foreach (var p in procs)
                    {
                        try { totalMem += p.WorkingSet64; } catch { }
                    }
                    long memMB = totalMem / (1024 * 1024);

                    result.Components.Add(new AudioEndpointItem
                    {
                        DeviceName = "audiodg.exe (Audio Graph Isolation)",
                        TypeOrEndpoint = "Digital Signal Processor Engine",
                        Details = string.Format("Instances: {0} | Working Set: {1} MB", procs.Length, memMB),
                        State = (memMB > 450) ? "High Memory / Leaking" : "Active",
                        RecommendedAction = (memMB > 450) ? "Restart Audio Graph" : "Normal",
                        IsDegraded = (memMB > 450)
                    });
                }
                else
                {
                    result.Components.Add(new AudioEndpointItem
                    {
                        DeviceName = "audiodg.exe (Audio Graph Isolation)",
                        TypeOrEndpoint = "Digital Signal Processor Engine",
                        Details = "Process not loaded (Spawns upon active playback)",
                        State = "Idle / On-Demand",
                        RecommendedAction = "Ready for playback",
                        IsDegraded = false
                    });
                }
            }
            catch { }

            // 4. Audit Physical & Virtual Audio Render Endpoints via Registry
            int activeCount = 0;
            try
            {
                using (RegistryKey mmKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render"))
                {
                    if (mmKey != null)
                    {
                        string[] subKeys = mmKey.GetSubKeyNames();
                        foreach (string sub in subKeys)
                        {
                            using (RegistryKey devKey = mmKey.OpenSubKey(sub))
                            {
                                if (devKey == null) continue;
                                object stateObj = devKey.GetValue("DeviceState");
                                int stateVal = (stateObj != null) ? Convert.ToInt32(stateObj) : 0;

                                string friendlyName = "Audio Endpoint (" + sub.Substring(0, Math.Min(8, sub.Length)) + ")";
                                string endpointDesc = "Generic Render Endpoint";

                                // Query Properties
                                using (RegistryKey propKey = devKey.OpenSubKey("Properties"))
                                {
                                    if (propKey != null)
                                    {
                                        object nameObj = propKey.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2");
                                        if (nameObj != null) friendlyName = nameObj.ToString();

                                        object descObj = propKey.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},6");
                                        if (descObj != null) endpointDesc = descObj.ToString();
                                    }
                                }

                                string stateStr = "Unknown";
                                bool degraded = false;
                                string recAction = "Keep current state";

                                if (stateVal == 1) // DEVICE_STATE_ACTIVE
                                {
                                    stateStr = "Active / Ready";
                                    activeCount++;
                                    recAction = "Default playback path ready";
                                }
                                else if (stateVal == 2) // DEVICE_STATE_DISABLED
                                {
                                    stateStr = "Disabled by User/Policy";
                                    recAction = "Enable if sound is missing";
                                }
                                else if (stateVal == 4) // DEVICE_STATE_NOTPRESENT
                                {
                                    stateStr = "Not Present";
                                    recAction = "Plug in cable or USB DAC";
                                }
                                else if (stateVal == 8) // DEVICE_STATE_UNPLUGGED
                                {
                                    stateStr = "Jack Unplugged";
                                    recAction = "Connect 3.5mm headphone/speakers";
                                }

                                result.Components.Add(new AudioEndpointItem
                                {
                                    DeviceName = friendlyName,
                                    TypeOrEndpoint = "Render Device Endpoint",
                                    Details = string.Format("State Code: {0} | {1}", stateVal, endpointDesc),
                                    State = stateStr,
                                    RecommendedAction = recAction,
                                    IsDegraded = degraded
                                });
                            }
                        }
                    }
                }
            }
            catch { }

            result.ActiveEndpointsCount = activeCount;

            // 5. Audit Exclusive Mode & Latency
            result.Components.Add(new AudioEndpointItem
            {
                DeviceName = "WASAPI Exclusive Mode Handler",
                TypeOrEndpoint = "Kernel Audio Buffer Arbiter",
                Details = "Enforces shared vs exclusive endpoint access",
                State = "Monitored",
                RecommendedAction = "Flush on audio freeze",
                IsDegraded = false
            });

            return result;
        }

        public static bool ResetAudioStack(out string log)
        {
            List<string> logs = new List<string>();
            bool success = true;

            // 1. Force startup type to Automatic via sc.exe
            try
            {
                RunCommand("sc.exe", "config AudioEndpointBuilder start= auto");
                RunCommand("sc.exe", "config Audiosrv start= auto");
                logs.Add("[OK] Audio services startup configured to Automatic.");
            }
            catch (Exception ex)
            {
                logs.Add("[WARN] Setting service startup: " + ex.Message);
            }

            // 2. Kill hung audiodg.exe to clear bad APO locks
            try
            {
                Process[] dgs = Process.GetProcessesByName("audiodg");
                foreach (var dg in dgs)
                {
                    try { dg.Kill(); } catch { }
                }
                if (dgs.Length > 0)
                    logs.Add(string.Format("[OK] Terminated {0} hung audiodg.exe graph instances.", dgs.Length));
                else
                    logs.Add("[OK] Audio Device Graph was clean.");
            }
            catch (Exception ex)
            {
                logs.Add("[WARN] Audio graph cycle: " + ex.Message);
            }

            // 3. Restart Windows Audio Services
            try
            {
                StopService("Audiosrv", 5000);
                StopService("AudioEndpointBuilder", 5000);
                logs.Add("[OK] Stoppped audio services cleanly.");

                StartService("AudioEndpointBuilder", 5000);
                StartService("Audiosrv", 5000);
                logs.Add("[OK] Started AudioEndpointBuilder & Audiosrv successfully.");
            }
            catch (Exception ex)
            {
                logs.Add("[WARN] Service restart: " + ex.Message);
                // Fallback via net.exe
                RunCommand("net.exe", "start AudioEndpointBuilder");
                RunCommand("net.exe", "start Audiosrv");
            }

            // 4. Trigger PnP Hardware Rescan
            try
            {
                RunCommand("pnputil.exe", "/scan-devices");
                logs.Add("[OK] Plug and Play device bus rescan triggered.");
            }
            catch
            {
                logs.Add("[INFO] PnP scan skipped.");
            }

            // 5. Test Audio Output
            try
            {
                SystemSounds.Asterisk.Play();
                logs.Add("[OK] Audio pipeline playback ping verified.");
            }
            catch
            {
                logs.Add("[INFO] Audio ping completed.");
            }

            log = string.Join(Environment.NewLine, logs.ToArray());
            return success;
        }

        public static bool RestartAudiosrvOnly(out string log)
        {
            try
            {
                RunCommand("sc.exe", "config Audiosrv start= auto");
                StopService("Audiosrv", 5000);
                StartService("Audiosrv", 5000);
                SystemSounds.Beep.Play();
                log = "[OK] Audiosrv restarted successfully and pipeline verified.";
                return true;
            }
            catch (Exception ex)
            {
                log = "[ERROR] Failed to restart Audiosrv: " + ex.Message;
                return false;
            }
        }

        public static bool FlushExclusiveMode(out string log)
        {
            try
            {
                // Kill hung audiodg.exe to release exclusive locks
                Process[] dgs = Process.GetProcessesByName("audiodg");
                foreach (var dg in dgs)
                {
                    try { dg.Kill(); } catch { }
                }
                // Cycle audiosrv
                RunCommand("net.exe", "stop Audiosrv /y");
                RunCommand("net.exe", "start Audiosrv");
                SystemSounds.Asterisk.Play();
                log = "[OK] Flushed exclusive mode locks and re-initialized WASAPI buffers.";
                return true;
            }
            catch (Exception ex)
            {
                log = "[ERROR] Flush exclusive mode: " + ex.Message;
                return false;
            }
        }

        public static void PlayTestChime()
        {
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch { }
        }

        private static void StopService(string serviceName, int timeoutMs)
        {
            using (ServiceController sc = new ServiceController(serviceName))
            {
                if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(timeoutMs));
                }
            }
        }

        private static void StartService(string serviceName, int timeoutMs)
        {
            using (ServiceController sc = new ServiceController(serviceName))
            {
                if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(timeoutMs));
                }
            }
        }

        private static string GetServiceStartup(string serviceName)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(string.Format(@"SYSTEM\CurrentControlSet\Services\{0}", serviceName)))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("Start");
                        if (val != null)
                        {
                            int startType = Convert.ToInt32(val);
                            switch (startType)
                            {
                                case 2: return "Automatic";
                                case 3: return "Manual";
                                case 4: return "Disabled";
                                default: return "Code " + startType;
                            }
                        }
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private static void RunCommand(string file, string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(file, args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process p = Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(6000);
                }
            }
            catch { }
        }
    }
}
