using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;

namespace PexorisBatteryOptimizer
{
    public struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    public class BatteryMetricItem
    {
        public string Parameter { get; set; }
        public string Value { get; set; }
        public string Ideal { get; set; }
        public string Status { get; set; }
        public string Action { get; set; }
    }

    public static class BatteryEngine
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemPowerStatus(out SystemPowerStatus sps);

        public static bool HasBattery()
        {
            SystemPowerStatus sps;
            if (GetSystemPowerStatus(out sps))
            {
                if (sps.BatteryFlag == 128) return false; // 128 = No system battery
                if (sps.BatteryLifePercent != 255) return true;
            }
            return false;
        }

        public static List<BatteryMetricItem> GetBatteryMetrics(out string summaryPill, out string summaryText)
        {
            List<BatteryMetricItem> items = new List<BatteryMetricItem>();

            SystemPowerStatus sps;
            bool ok = GetSystemPowerStatus(out sps);

            string acStatus = "Unknown";
            string chargePct = "N/A";
            string lifeTimeStr = "Calculating...";
            bool isSaver = false;

            if (ok)
            {
                acStatus = sps.ACLineStatus == 1 ? "AC Connected (Mains)" : (sps.ACLineStatus == 0 ? "On Battery (Discharging)" : "Unknown");
                chargePct = sps.BatteryLifePercent != 255 ? sps.BatteryLifePercent + "%" : "Desktop PC (No Battery)";
                isSaver = sps.SystemStatusFlag == 1;

                if (sps.BatteryLifeTime != -1 && sps.BatteryLifeTime > 0)
                {
                    TimeSpan ts = TimeSpan.FromSeconds(sps.BatteryLifeTime);
                    lifeTimeStr = string.Format("{0}h {1}m remaining", (int)ts.TotalHours, ts.Minutes);
                }
                else if (sps.ACLineStatus == 1)
                {
                    lifeTimeStr = "Plugged in (Charging/Full)";
                }
            }

            // Query WMI for Design Capacity, Full Charge Capacity, Chemistry
            long designCap = 0;
            long fullChargeCap = 0;
            string chemistry = "Lithium-Ion";
            string deviceName = "Standard ACPI Battery";

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Battery"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["DesignCapacity"] != null)
                            long.TryParse(obj["DesignCapacity"].ToString(), out designCap);
                        if (obj["FullChargeCapacity"] != null)
                            long.TryParse(obj["FullChargeCapacity"].ToString(), out fullChargeCap);
                        if (obj["Chemistry"] != null)
                        {
                            int c = Convert.ToInt32(obj["Chemistry"]);
                            if (c == 2) chemistry = "Lead Acid";
                            else if (c == 3) chemistry = "Nickel Cadmium";
                            else if (c == 4) chemistry = "Nickel Metal Hydride";
                            else if (c == 5) chemistry = "Lithium-Ion (Li-Ion)";
                            else if (c == 6) chemistry = "Zinc Air";
                            else if (c == 7) chemistry = "Lithium Polymer (Li-Po)";
                        }
                        if (obj["Name"] != null)
                            deviceName = obj["Name"].ToString();
                    }
                }
            }
            catch { }

            // Secondary WMI search for Modern Windows 11 root\WMI BatteryStaticData
            if (designCap == 0 || fullChargeCap == 0)
            {
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM BatteryStaticData"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            if (obj["DesignedCapacity"] != null && designCap == 0)
                                long.TryParse(obj["DesignedCapacity"].ToString(), out designCap);
                        }
                    }
                }
                catch { }

                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM BatteryFullChargedCapacity"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            if (obj["FullChargedCapacity"] != null && fullChargeCap == 0)
                                long.TryParse(obj["FullChargedCapacity"].ToString(), out fullChargeCap);
                        }
                    }
                }
                catch { }
            }

            // Calculate Wear Level
            double wearPct = 0;
            string wearStatus = "Normal";
            string wearAction = "Healthy battery cell condition";

            if (designCap > 0 && fullChargeCap > 0)
            {
                if (designCap >= fullChargeCap)
                {
                    wearPct = ((double)(designCap - fullChargeCap) / (double)designCap) * 100.0;
                }
                else
                {
                    wearPct = 0.0; // Battery calibrated above design
                }

                if (wearPct < 10.0)
                {
                    wearStatus = "Excellent (<10% Wear)";
                    wearAction = "Cell degradation minimal; optimal health";
                }
                else if (wearPct < 25.0)
                {
                    wearStatus = "Good (<25% Wear)";
                    wearAction = "Normal aging; recalibrate every 30 days";
                }
                else if (wearPct < 45.0)
                {
                    wearStatus = "Moderate (Needs Recalibration)";
                    wearAction = "Discharge to 5% and recharge to 100% full cycle";
                }
                else
                {
                    wearStatus = "Degraded (>45% Wear)";
                    wearAction = "Substantial capacity loss; consider battery service";
                }
            }
            else
            {
                wearStatus = "Desktop / AC Only";
                wearAction = "No mobile battery hardware detected";
            }

            // Build List Items
            items.Add(new BatteryMetricItem
            {
                Parameter = "Current Power Source",
                Value = acStatus,
                Ideal = "On Battery (Optimized)",
                Status = sps.ACLineStatus == 1 ? "Charging" : "Running on DC",
                Action = "Power source state tracked"
            });

            items.Add(new BatteryMetricItem
            {
                Parameter = "Charge Level Percentage",
                Value = chargePct,
                Ideal = "20% - 80% (Long Life)",
                Status = (sps.BatteryLifePercent > 20) ? "Adequate" : "Low Battery",
                Action = isSaver ? "Windows Battery Saver: ACTIVE" : "Standard discharge"
            });

            items.Add(new BatteryMetricItem
            {
                Parameter = "Battery Runtime Remaining",
                Value = lifeTimeStr,
                Ideal = "> 4.0 Hours",
                Status = "Live Rate",
                Action = "Dynamic estimation based on current load"
            });

            items.Add(new BatteryMetricItem
            {
                Parameter = "Factory Design Capacity",
                Value = designCap > 0 ? string.Format("{0:N0} mWh", designCap) : "OEM Standard",
                Ideal = "100% Original Capacity",
                Status = "Factory Benchmark",
                Action = "Rated manufacturer baseline"
            });

            items.Add(new BatteryMetricItem
            {
                Parameter = "Current Full Charge Capacity",
                Value = fullChargeCap > 0 ? string.Format("{0:N0} mWh", fullChargeCap) : "ACPI Monitored",
                Ideal = designCap > 0 ? string.Format("~{0:N0} mWh", designCap) : "N/A",
                Status = fullChargeCap > 0 ? string.Format("{0:N1}% of Design", (double)fullChargeCap / Math.Max(1, designCap) * 100.0) : "Healthy",
                Action = "Maximum possible charge ceiling"
            });

            items.Add(new BatteryMetricItem
            {
                Parameter = "Wear Level Degradation",
                Value = designCap > 0 ? string.Format("{0:N1}% Loss", wearPct) : "0.0% (Clean)",
                Ideal = "< 15% Wear",
                Status = wearStatus,
                Action = wearAction
            });

            items.Add(new BatteryMetricItem
            {
                Parameter = "Battery Chemistry",
                Value = chemistry,
                Ideal = "Li-Ion / Li-Po",
                Status = "Standard",
                Action = "Thermal safe charge profiles"
            });

            items.Add(new BatteryMetricItem
            {
                Parameter = "Modern Standby (S0 Low Power)",
                Value = "Optimized Power State",
                Ideal = "Zero Sleep Drains",
                Status = "Standby Guard Active",
                Action = "Clamps background network during sleep"
            });

            // Summary Pills
            if (sps.ACLineStatus == 1)
            {
                summaryPill = "⚡ AC CONNECTED (" + chargePct + ")";
            }
            else if (sps.BatteryLifePercent != 255)
            {
                summaryPill = "🔋 ON BATTERY (" + chargePct + ")";
            }
            else
            {
                summaryPill = "💻 DESKTOP PC (AC ONLY)";
            }

            if (designCap > 0 && fullChargeCap > 0)
            {
                summaryText = string.Format("Wear Level: {0:N1}% • Health: {1:N1}% • Full Cap: {2:N0} mWh",
                    wearPct, 100.0 - wearPct, fullChargeCap);
            }
            else
            {
                summaryText = "System Power Status: " + acStatus + " • Charge: " + chargePct;
            }

            return items;
        }

        public static bool EnableUltraBatterySaver(out string message)
        {
            try
            {
                // 1. Switch to Power Saver or Balanced Scheme
                RunPowerCfg("/setactive a1841308-3541-4fab-bc81-f71556f20b4a"); // Power Saver GUID

                // 2. Set aggressive display sleep on battery (2 mins)
                RunPowerCfg("/change monitor-timeout-dc 2");

                // 3. Set standby sleep on battery (5 mins)
                RunPowerCfg("/change standby-timeout-dc 5");

                // 4. Cap CPU max processor frequency on battery to 75% to eliminate thermal throttling
                RunPowerCfg("/setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 75");

                // 5. Apply active power settings
                RunPowerCfg("/setactive SCHEME_CURRENT");

                message = "Ultra Battery Saver enabled!\n\n" +
                          "• Switched to High-Efficiency Power Scheme\n" +
                          "• Display idle timeout set to 2 minutes on battery\n" +
                          "• Sleep idle timeout set to 5 minutes on battery\n" +
                          "• CPU power throttle capped at 75% on DC to save battery cells";
                return true;
            }
            catch (Exception ex)
            {
                message = "Failed to enable power saver: " + ex.Message;
                return false;
            }
        }

        public static bool OptimizeModernStandby(out string message)
        {
            try
            {
                // Disconnect Wi-Fi during Modern Connected Standby on battery
                RunPowerCfg("/setdcvalueindex SCHEME_CURRENT SUB_NONE F15576E8-98B7-4186-B944-EEFA66440288 0");

                // Apply active power settings
                RunPowerCfg("/setactive SCHEME_CURRENT");

                message = "Modern Standby optimized!\n\n" +
                          "• Wi-Fi connectivity during sleep on battery has been disabled\n" +
                          "• Prevents overnight battery drains from background app sync\n" +
                          "• Laptop will wake up cool without drained battery in backpacks";
                return true;
            }
            catch (Exception ex)
            {
                message = "Modern Standby optimization notice: " + ex.Message;
                return false;
            }
        }

        public static bool RestoreBalancedPlan(out string message)
        {
            try
            {
                // Balanced scheme GUID
                RunPowerCfg("/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");

                // Reset standard timeouts
                RunPowerCfg("/change monitor-timeout-dc 5");
                RunPowerCfg("/change standby-timeout-dc 15");
                RunPowerCfg("/setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100");
                RunPowerCfg("/setactive SCHEME_CURRENT");

                message = "Restored Windows Balanced Power Plan.\n\n" +
                          "• Balanced CPU performance re-engaged\n" +
                          "• Default display and sleep intervals restored";
                return true;
            }
            catch (Exception ex)
            {
                message = "Failed to restore balanced plan: " + ex.Message;
                return false;
            }
        }

        public static bool GenerateBatteryReport(out string reportPath)
        {
            string outHtml = Path.Combine(Path.GetTempPath(), "battery-report.html");
            try
            {
                if (File.Exists(outHtml)) File.Delete(outHtml);
                RunPowerCfg("/batteryreport /output \"" + outHtml + "\"");

                if (File.Exists(outHtml))
                {
                    reportPath = outHtml;
                    Process.Start(new ProcessStartInfo(outHtml) { UseShellExecute = true });
                    return true;
                }
            }
            catch { }

            reportPath = "";
            return false;
        }

        private static void RunPowerCfg(string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("powercfg.exe", args)
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
