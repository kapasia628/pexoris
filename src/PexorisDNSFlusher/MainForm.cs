using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PexorisDNSFlusher
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_SETICON = 0x80;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        // UI Controls
        private Panel pnlTitleBar;
        private PictureBox picTitleIcon;
        private Label lblTitleText;
        private Label lblTitleBadge;
        private Label btnMinimize;
        private Label btnClose;

        private Panel cardTop;
        private PictureBox picCardIcon;
        private Label lblCardTitle;
        private Label lblCardSubtitle;
        private Label pillStatus;

        private PexorisButton btnTopFlushAll;
        private PexorisButton btnTopFlushDns;

        private Panel pnlTabRow;
        private PexorisButton btnTabAdapters;
        private PexorisButton btnTabLatency;
        private PexorisButton btnTabLogs;
        private Label lblQuickStats;

        private Panel pnlListWrapper;
        private ListView lvAdapters;
        private ListView lvLatency;
        private ListView lvLogs;

        private Panel pnlActions;
        private PexorisButton btnActionFlushAll;
        private PexorisButton btnActionFlushDns;
        private PexorisButton btnActionRenewDhcp;
        private PexorisButton btnActionResetWinsock;
        private PexorisButton btnActionTestPing;
        private PexorisButton btnActionRefresh;

        private Panel pnlFooter;
        private CheckBox chkPurgeArpNetbios;
        private Label lblFooterStatus;
        private LinkLabel lnkBrand;

        private ContextMenuStrip ctxMenu;

        private int _activeTab = 0; // 0=Adapters, 1=Latency, 2=Logs
        private List<NetworkAdapterInfo> _adapters = new List<NetworkAdapterInfo>();

        public MainForm()
        {
            InitializeComponent();
            ApplyCustomDropShadow();
            LoadAppIcon();
            RefreshAdapters();
            RefreshHostsCount();
            LogEvent("System", "Ready", "Pexoris DNSFlusher initialized. Portable mode active.");
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Pexoris DNSFlusher";
            Size = new Size(860, 640);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.BgCanvas;
            Font = Theme.FontRegular;
            DoubleBuffered = true;

            // 1. Title Bar (44px)
            pnlTitleBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(860, 44),
                BackColor = Theme.CardBg,
                Dock = DockStyle.Top
            };
            pnlTitleBar.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 43, 860, 43);
                }
            };
            pnlTitleBar.MouseDown += TitleBar_MouseDown;

            picTitleIcon = new PictureBox
            {
                Location = new Point(14, 11),
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            picTitleIcon.MouseDown += TitleBar_MouseDown;

            lblTitleText = new Label
            {
                Location = new Point(44, 12),
                AutoSize = true,
                Text = "Pexoris DNSFlusher",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero,
                Cursor = Cursors.Default
            };
            lblTitleText.MouseDown += TitleBar_MouseDown;

            lblTitleBadge = new Label
            {
                Location = new Point(175, 13),
                AutoSize = true,
                Text = "PORTABLE v1.0",
                Font = Theme.FontSmall,
                ForeColor = Theme.PrimaryCyanHover,
                BackColor = Color.FromArgb(236, 254, 255), // Cyan 50
                Padding = new Padding(4, 1, 4, 1)
            };
            lblTitleBadge.MouseDown += TitleBar_MouseDown;

            btnMinimize = new Label
            {
                Location = new Point(780, 0),
                Size = new Size(40, 44),
                Text = "—",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = Theme.TextSub,
                Cursor = Cursors.Hand
            };
            btnMinimize.MouseEnter += (s, e) => { btnMinimize.BackColor = Color.FromArgb(241, 245, 249); btnMinimize.ForeColor = Theme.TextHero; };
            btnMinimize.MouseLeave += (s, e) => { btnMinimize.BackColor = Color.Transparent; btnMinimize.ForeColor = Theme.TextSub; };
            btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;

            btnClose = new Label
            {
                Location = new Point(820, 0),
                Size = new Size(40, 44),
                Text = "✕",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Theme.TextSub,
                Cursor = Cursors.Hand
            };
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.FromArgb(239, 68, 68); btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = Theme.TextSub; };
            btnClose.Click += (s, e) => Close();

            pnlTitleBar.Controls.Add(picTitleIcon);
            pnlTitleBar.Controls.Add(lblTitleText);
            pnlTitleBar.Controls.Add(lblTitleBadge);
            pnlTitleBar.Controls.Add(btnMinimize);
            pnlTitleBar.Controls.Add(btnClose);

            // 2. Top Control Card (110px, Y=44)
            cardTop = new Panel
            {
                Location = new Point(16, 56),
                Size = new Size(828, 100),
                BackColor = Theme.CardBg
            };
            cardTop.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF rect = new RectangleF(0.5f, 0.5f, cardTop.Width - 1f, cardTop.Height - 1f);
                using (GraphicsPath path = Theme.GetRoundedPath(rect, 8f))
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawPath(p, path);
                }
            };

            picCardIcon = new PictureBox
            {
                Location = new Point(16, 18),
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            lblCardTitle = new Label
            {
                Location = new Point(92, 16),
                AutoSize = true,
                Text = "DNS Cache & TCP/IP Stack Resolver",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero
            };

            lblCardSubtitle = new Label
            {
                Location = new Point(92, 40),
                Size = new Size(480, 36),
                Text = "Wipe corrupted DNS cache entries, reset Winsock/IP routing tables, renew DHCP leases, and benchmark DNS provider latency in real-time.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub
            };

            pillStatus = new Label
            {
                Location = new Point(92, 75),
                AutoSize = true,
                Text = "● Resolver Active • Cache Clean",
                Font = Theme.FontSmall,
                ForeColor = Color.FromArgb(4, 120, 87),
                BackColor = Color.FromArgb(236, 253, 245),
                Padding = new Padding(6, 2, 6, 2)
            };

            btnTopFlushAll = new PexorisButton
            {
                Location = new Point(585, 26),
                Size = new Size(130, 48),
                Text = "⚡ Flush All",
                Style = PexorisButtonStyle.PrimaryCyan
            };
            btnTopFlushAll.Click += (s, e) => Action_FlushAll();

            btnTopFlushDns = new PexorisButton
            {
                Location = new Point(722, 26),
                Size = new Size(94, 48),
                Text = "Flush DNS",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            btnTopFlushDns.Click += (s, e) => Action_FlushDnsOnly();

            cardTop.Controls.Add(picCardIcon);
            cardTop.Controls.Add(lblCardTitle);
            cardTop.Controls.Add(lblCardSubtitle);
            cardTop.Controls.Add(pillStatus);
            cardTop.Controls.Add(btnTopFlushAll);
            cardTop.Controls.Add(btnTopFlushDns);

            // 3. Tab Switcher Row (Y=164)
            pnlTabRow = new Panel
            {
                Location = new Point(16, 164),
                Size = new Size(828, 32),
                BackColor = Color.Transparent
            };

            btnTabAdapters = new PexorisButton
            {
                Location = new Point(0, 0),
                Size = new Size(160, 32),
                Text = "Network Adapters (0)",
                Style = PexorisButtonStyle.PrimaryCyan
            };
            btnTabAdapters.Click += (s, e) => SwitchTab(0);

            btnTabLatency = new PexorisButton
            {
                Location = new Point(168, 0),
                Size = new Size(170, 32),
                Text = "DNS Latency Ping",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnTabLatency.Click += (s, e) => SwitchTab(1);

            btnTabLogs = new PexorisButton
            {
                Location = new Point(346, 0),
                Size = new Size(140, 32),
                Text = "Operation Logs",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnTabLogs.Click += (s, e) => SwitchTab(2);

            lblQuickStats = new Label
            {
                Location = new Point(510, 8),
                Size = new Size(318, 20),
                TextAlign = ContentAlignment.MiddleRight,
                Text = "Hosts entries: 0 | Adapters: 0",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            pnlTabRow.Controls.Add(btnTabAdapters);
            pnlTabRow.Controls.Add(btnTabLatency);
            pnlTabRow.Controls.Add(btnTabLogs);
            pnlTabRow.Controls.Add(lblQuickStats);

            // 4. Center List Area (Y=202, H=298)
            pnlListWrapper = new Panel
            {
                Location = new Point(16, 202),
                Size = new Size(828, 298),
                BackColor = Theme.CardBg
            };
            pnlListWrapper.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlListWrapper.Width - 1, pnlListWrapper.Height - 1);
                }
            };

            // ListView 1: Adapters
            lvAdapters = new ListView
            {
                Location = new Point(1, 1),
                Size = new Size(826, 296),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                Font = Theme.FontRegular,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvAdapters.Columns.Add("Adapter Name", 170);
            lvAdapters.Columns.Add("Type", 95);
            lvAdapters.Columns.Add("Status", 85);
            lvAdapters.Columns.Add("IPv4 Address", 140);
            lvAdapters.Columns.Add("Default Gateway", 130);
            lvAdapters.Columns.Add("DNS Servers", 185);

            // ListView 2: DNS Latency
            lvLatency = new ListView
            {
                Location = new Point(1, 1),
                Size = new Size(826, 296),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                Font = Theme.FontRegular,
                Visible = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvLatency.Columns.Add("DNS Provider", 170);
            lvLatency.Columns.Add("IP Address", 150);
            lvLatency.Columns.Add("Latency (Roundtrip)", 140);
            lvLatency.Columns.Add("Status", 110);
            lvLatency.Columns.Add("Assessment / Grade", 230);

            // ListView 3: Operation Logs
            lvLogs = new ListView
            {
                Location = new Point(1, 1),
                Size = new Size(826, 296),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                Font = Theme.FontRegular,
                Visible = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvLogs.Columns.Add("Timestamp", 100);
            lvLogs.Columns.Add("Module", 110);
            lvLogs.Columns.Add("Status", 100);
            lvLogs.Columns.Add("Details & Output", 490);

            pnlListWrapper.Controls.Add(lvAdapters);
            pnlListWrapper.Controls.Add(lvLatency);
            pnlListWrapper.Controls.Add(lvLogs);

            // Context Menu
            ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("Copy Row Info", null, (s, e) => CopySelectedRow());
            ctxMenu.Items.Add("Flush DNS Cache Now", null, (s, e) => Action_FlushDnsOnly());
            ctxMenu.Items.Add("Run Latency Ping Test", null, (s, e) => Action_TestPing());
            lvAdapters.ContextMenuStrip = ctxMenu;
            lvLatency.ContextMenuStrip = ctxMenu;
            lvLogs.ContextMenuStrip = ctxMenu;

            // 5. Action Buttons Bar (Y=508, H=50)
            pnlActions = new Panel
            {
                Location = new Point(16, 508),
                Size = new Size(828, 50),
                BackColor = Color.Transparent
            };

            btnActionFlushAll = new PexorisButton
            {
                Location = new Point(0, 6),
                Size = new Size(190, 40),
                Text = "⚡ 1-Click Flush & Reset All",
                Style = PexorisButtonStyle.PrimaryCyan
            };
            btnActionFlushAll.Click += (s, e) => Action_FlushAll();

            btnActionFlushDns = new PexorisButton
            {
                Location = new Point(198, 6),
                Size = new Size(135, 40),
                Text = "Flush DNS Cache",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            btnActionFlushDns.Click += (s, e) => Action_FlushDnsOnly();

            btnActionRenewDhcp = new PexorisButton
            {
                Location = new Point(341, 6),
                Size = new Size(130, 40),
                Text = "Renew IP / Lease",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnActionRenewDhcp.Click += (s, e) => Action_RenewDhcp();

            btnActionResetWinsock = new PexorisButton
            {
                Location = new Point(479, 6),
                Size = new Size(140, 40),
                Text = "Reset TCP/Winsock",
                Style = PexorisButtonStyle.DestructiveRed
            };
            btnActionResetWinsock.Click += (s, e) => Action_ResetWinsock();

            btnActionTestPing = new PexorisButton
            {
                Location = new Point(627, 6),
                Size = new Size(125, 40),
                Text = "Ping Latency",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnActionTestPing.Click += (s, e) => Action_TestPing();

            btnActionRefresh = new PexorisButton
            {
                Location = new Point(760, 6),
                Size = new Size(68, 40),
                Text = "Refresh",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnActionRefresh.Click += (s, e) => RefreshAll();

            pnlActions.Controls.Add(btnActionFlushAll);
            pnlActions.Controls.Add(btnActionFlushDns);
            pnlActions.Controls.Add(btnActionRenewDhcp);
            pnlActions.Controls.Add(btnActionResetWinsock);
            pnlActions.Controls.Add(btnActionTestPing);
            pnlActions.Controls.Add(btnActionRefresh);

            // 6. Footer (38px, Y=566)
            pnlFooter = new Panel
            {
                Location = new Point(0, 602),
                Size = new Size(860, 38),
                BackColor = Theme.CardBg,
                Dock = DockStyle.Bottom
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, 860, 0);
                }
            };

            chkPurgeArpNetbios = new CheckBox
            {
                Location = new Point(16, 9),
                AutoSize = true,
                Text = "Purge ARP cache & reload NetBIOS on flush",
                Checked = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            lblFooterStatus = new Label
            {
                Location = new Point(310, 10),
                Size = new Size(410, 20),
                Text = "Zero resident memory • 100% Portable WinForms • Run as Admin",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lnkBrand = new LinkLabel
            {
                Location = new Point(740, 10),
                Size = new Size(104, 20),
                Text = "pexoris.com",
                Font = Theme.FontBold,
                LinkColor = Theme.PrimaryCyanHover,
                ActiveLinkColor = Theme.PrimaryBlueHover,
                TextAlign = ContentAlignment.MiddleRight
            };
            lnkBrand.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://pexoris.com") { UseShellExecute = true }); } catch { }
            };

            pnlFooter.Controls.Add(chkPurgeArpNetbios);
            pnlFooter.Controls.Add(lblFooterStatus);
            pnlFooter.Controls.Add(lnkBrand);

            // Add all panels to form
            Controls.Add(pnlFooter);
            Controls.Add(pnlActions);
            Controls.Add(pnlListWrapper);
            Controls.Add(pnlTabRow);
            Controls.Add(cardTop);
            Controls.Add(pnlTitleBar);

            ResumeLayout(false);
        }

        private void SwitchTab(int tabIndex)
        {
            _activeTab = tabIndex;
            lvAdapters.Visible = (_activeTab == 0);
            lvLatency.Visible = (_activeTab == 1);
            lvLogs.Visible = (_activeTab == 2);

            btnTabAdapters.Style = (_activeTab == 0) ? PexorisButtonStyle.PrimaryCyan : PexorisButtonStyle.SecondaryOutline;
            btnTabLatency.Style = (_activeTab == 1) ? PexorisButtonStyle.PrimaryCyan : PexorisButtonStyle.SecondaryOutline;
            btnTabLogs.Style = (_activeTab == 2) ? PexorisButtonStyle.PrimaryCyan : PexorisButtonStyle.SecondaryOutline;

            btnTabAdapters.Invalidate();
            btnTabLatency.Invalidate();
            btnTabLogs.Invalidate();
        }

        private void LogEvent(string module, string status, string detail)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => LogEvent(module, status, detail)));
                return;
            }

            ListViewItem item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add(module);
            item.SubItems.Add(status);
            item.SubItems.Add(detail);

            if (status.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                status.IndexOf("Fail", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.ForeColor = Theme.DestructiveRed;
            }
            else if (status.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     status.IndexOf("Flushed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     status.IndexOf("Clean", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.ForeColor = Color.FromArgb(4, 120, 87);
            }

            lvLogs.Items.Insert(0, item);
        }

        private void RefreshAdapters()
        {
            lvAdapters.Items.Clear();
            _adapters = DnsHelper.GetNetworkAdapters();

            int upCount = 0;
            foreach (var a in _adapters)
            {
                if (a.IsUp) upCount++;
                ListViewItem item = new ListViewItem(a.Name);
                item.SubItems.Add(a.Type);
                item.SubItems.Add(a.Status);
                item.SubItems.Add(a.IpAddresses);
                item.SubItems.Add(a.Gateway);
                item.SubItems.Add(a.DnsServers);

                if (!a.IsUp)
                {
                    item.ForeColor = Theme.TextMuted;
                }
                else
                {
                    item.ForeColor = Theme.TextHero;
                }
                lvAdapters.Items.Add(item);
            }

            btnTabAdapters.Text = string.Format("Network Adapters ({0})", _adapters.Count);
            btnTabAdapters.Invalidate();
        }

        private void RefreshHostsCount()
        {
            int hostsCount = DnsHelper.GetCustomHostsCount();
            lblQuickStats.Text = string.Format("Custom Hosts: {0} | Adapters: {1} active", hostsCount, lvAdapters.Items.Count);
        }

        private void RefreshAll()
        {
            RefreshAdapters();
            RefreshHostsCount();
            LogEvent("Refresh", "Success", "Network adapters and DNS server configurations reloaded.");
        }

        private void Action_FlushAll()
        {
            Cursor = Cursors.WaitCursor;
            pillStatus.Text = "● Flushing DNS & Resetting Stack...";
            pillStatus.ForeColor = Theme.PrimaryCyanHover;
            pillStatus.BackColor = Color.FromArgb(236, 254, 255);
            pillStatus.Refresh();

            string resNative = DnsHelper.FlushDnsNative() ? "Native API Success" : "Native API N/A";
            string resCli = DnsHelper.FlushDnsCache();
            LogEvent("DNS Resolver", "Flushed", "DnsFlushResolverCache: " + resNative + " | CLI: " + resCli.Replace("\r", " ").Replace("\n", " ").Trim());

            if (chkPurgeArpNetbios.Checked)
            {
                string resArp = DnsHelper.ClearArpCache();
                LogEvent("ARP Cache", "Purged", resArp.Replace("\r", " ").Replace("\n", " ").Trim());

                string resNetbios = DnsHelper.ReloadNetBios();
                LogEvent("NetBIOS", "Reloaded", resNetbios.Replace("\r", " ").Replace("\n", " ").Trim());
            }

            Cursor = Cursors.Default;
            pillStatus.Text = "● Full DNS & Network Reset Completed";
            pillStatus.ForeColor = Color.FromArgb(4, 120, 87);
            pillStatus.BackColor = Color.FromArgb(236, 253, 245);

            MessageBox.Show("Windows DNS Resolver Cache successfully flushed!\n\nAll DNS caches, ARP entries, and NetBIOS name caches have been purged and restored.",
                "Pexoris DNSFlusher — Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            SwitchTab(2); // Show Logs
        }

        private void Action_FlushDnsOnly()
        {
            Cursor = Cursors.WaitCursor;
            pillStatus.Text = "● Flushing DNS Cache...";
            pillStatus.Refresh();

            DnsHelper.FlushDnsNative();
            string cliOutput = DnsHelper.FlushDnsCache();
            LogEvent("DNS Flush", "Success", cliOutput.Replace("\r", " ").Replace("\n", " ").Trim());

            Cursor = Cursors.Default;
            pillStatus.Text = "● DNS Resolver Cache Flushed";
            pillStatus.ForeColor = Color.FromArgb(4, 120, 87);
            pillStatus.BackColor = Color.FromArgb(236, 253, 245);

            MessageBox.Show("Successfully flushed the DNS Resolver Cache.\n\nStale DNS lookups and poison cache entries removed.",
                "DNS Flushed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Action_RenewDhcp()
        {
            if (MessageBox.Show("Renewing IP address will briefly drop your network connection for 2-5 seconds while requesting a new lease from your router/DHCP server.\n\nDo you want to continue?",
                "Renew IP Lease", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            Cursor = Cursors.WaitCursor;
            pillStatus.Text = "● Requesting DHCP Lease...";
            pillStatus.Refresh();

            string resRel = DnsHelper.ReleaseIp();
            LogEvent("DHCP Release", "Executed", resRel.Replace("\r", " ").Replace("\n", " ").Trim());

            string resRen = DnsHelper.RenewIp();
            LogEvent("DHCP Renew", "Success", resRen.Replace("\r", " ").Replace("\n", " ").Trim());

            RefreshAdapters();
            Cursor = Cursors.Default;
            pillStatus.Text = "● DHCP Lease Renewed";
            pillStatus.ForeColor = Color.FromArgb(4, 120, 87);
            pillStatus.BackColor = Color.FromArgb(236, 253, 245);

            MessageBox.Show("DHCP Lease renewed successfully!\n\nNetwork adapters re-synchronized with default gateway.",
                "DHCP Renewed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Action_ResetWinsock()
        {
            if (MessageBox.Show("WARNING: Resetting Winsock and the TCP/IP stack will restore the Windows network catalog to factory defaults.\n\nA computer restart may be required for full effect.\n\nDo you want to proceed?",
                "Reset Winsock & TCP/IP Stack", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            Cursor = Cursors.WaitCursor;
            string resWinsock = DnsHelper.ResetWinsock();
            LogEvent("Winsock Catalog", "Reset", resWinsock.Replace("\r", " ").Replace("\n", " ").Trim());

            string resIp = DnsHelper.ResetIpStack();
            LogEvent("TCP/IP Stack", "Reset", resIp.Replace("\r", " ").Replace("\n", " ").Trim());

            Cursor = Cursors.Default;
            pillStatus.Text = "● Winsock & TCP/IP Stack Reset";
            pillStatus.ForeColor = Theme.PrimaryCyanHover;
            pillStatus.BackColor = Color.FromArgb(236, 254, 255);

            MessageBox.Show("Winsock catalog and TCP/IP stack reset completed successfully!\n\nPlease restart your computer if you were troubleshooting deep connectivity issues.",
                "Winsock Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);

            SwitchTab(2); // Show Logs
        }

        private void Action_TestPing()
        {
            SwitchTab(1); // Switch to Latency tab
            lvLatency.Items.Clear();

            pillStatus.Text = "● Benchmarking DNS Latency...";
            pillStatus.Refresh();

            var targets = new[]
            {
                new { Host = "1.1.1.1", Provider = "Cloudflare DNS (Primary)" },
                new { Host = "1.0.0.1", Provider = "Cloudflare DNS (Secondary)" },
                new { Host = "8.8.8.8", Provider = "Google Public DNS (Primary)" },
                new { Host = "8.8.4.4", Provider = "Google Public DNS (Secondary)" },
                new { Host = "9.9.9.9", Provider = "Quad9 DNS (Malware Block)" },
                new { Host = "208.67.222.222", Provider = "Cisco OpenDNS" },
                new { Host = "76.76.2.0", Provider = "Control D DNS" },
                new { Host = "94.140.14.14", Provider = "AdGuard DNS (Ad Blocking)" }
            };

            ThreadPool.QueueUserWorkItem(_ =>
            {
                foreach (var t in targets)
                {
                    PingResult res = DnsHelper.TestPing(t.Host, t.Provider);
                    Invoke(new Action(() =>
                    {
                        ListViewItem item = new ListViewItem(res.Provider);
                        item.SubItems.Add(res.Host);

                        if (res.Success)
                        {
                            item.SubItems.Add(res.LatencyMs + " ms");
                            item.SubItems.Add("Active");

                            if (res.LatencyMs < 20)
                            {
                                item.SubItems.Add("★★★★★ Ultra Fast (Gaming & Streaming)");
                                item.ForeColor = Color.FromArgb(4, 120, 87);
                            }
                            else if (res.LatencyMs < 50)
                            {
                                item.SubItems.Add("★★★★☆ Fast (Recommended)");
                                item.ForeColor = Color.FromArgb(2, 132, 199);
                            }
                            else if (res.LatencyMs < 100)
                            {
                                item.SubItems.Add("★★★☆☆ Good (Standard Browsing)");
                                item.ForeColor = Theme.TextHero;
                            }
                            else
                            {
                                item.SubItems.Add("★★☆☆☆ High Latency");
                                item.ForeColor = Color.FromArgb(217, 119, 6);
                            }
                        }
                        else
                        {
                            item.SubItems.Add("Failed");
                            item.SubItems.Add("Unreachable");
                            item.SubItems.Add(res.ErrorMessage ?? "Request Timed Out");
                            item.ForeColor = Theme.DestructiveRed;
                        }

                        lvLatency.Items.Add(item);
                    }));
                }

                Invoke(new Action(() =>
                {
                    pillStatus.Text = "● Latency Benchmark Completed";
                    pillStatus.ForeColor = Color.FromArgb(4, 120, 87);
                    pillStatus.BackColor = Color.FromArgb(236, 253, 245);
                    LogEvent("Latency Ping", "Complete", "Tested " + targets.Length + " global DNS providers.");
                }));
            });
        }

        private void CopySelectedRow()
        {
            ListView activeLv = (_activeTab == 0) ? lvAdapters : ((_activeTab == 1) ? lvLatency : lvLogs);
            if (activeLv.SelectedItems.Count > 0)
            {
                ListViewItem item = activeLv.SelectedItems[0];
                List<string> parts = new List<string>();
                foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
                {
                    parts.Add(sub.Text);
                }
                Clipboard.SetText(string.Join(" | ", parts.ToArray()));
            }
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void LoadAppIcon()
        {
            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    Icon = appIcon;
                    SendMessage(Handle, WM_SETICON, ICON_SMALL, (int)appIcon.Handle);
                    SendMessage(Handle, WM_SETICON, ICON_BIG, (int)appIcon.Handle);
                    picTitleIcon.Image = appIcon.ToBitmap();
                    picCardIcon.Image = appIcon.ToBitmap();
                }
            }
            catch { }
        }

        private void ApplyCustomDropShadow()
        {
            // Handled via CreateParams
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }
    }
}
