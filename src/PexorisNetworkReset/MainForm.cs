using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisNetworkReset
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        // Titlebar controls
        private Panel _pnlTitleBar;
        private PictureBox _pbTitleIcon;
        private Label _lblTitle;
        private Label _btnMinimize;
        private Label _btnClose;

        // Top Control Card
        private Panel _pnlTopCard;
        private PictureBox _pbCardIcon;
        private Label _lblCardHeadline;
        private Label _lblCardSub;
        private Label _lblStatusPill;

        // Stat boxes in top card
        private Panel _pnlStat1;
        private Panel _pnlStat2;
        private Panel _pnlStat3;
        private Panel _pnlStat4;

        private Label _lblStat1Val;
        private Label _lblStat2Val;
        private Label _lblStat3Val;
        private Label _lblStat4Val;

        // Explorer ListView
        private ListView _lvNetwork;

        // Action Buttons Bar
        private Panel _pnlActions;
        private PexorisButton _btnFullReset;
        private PexorisButton _btnFlushDns;
        private PexorisButton _btnRenewDhcp;
        private PexorisButton _btnResetFirewall;
        private PexorisButton _btnRefresh;

        // Footer
        private Panel _pnlFooter;
        private CheckBox _chkRestartAdapters;
        private Label _lblFooterStatus;
        private LinkLabel _lnkWebsite;

        private NetworkAuditResult _auditResult;

        public MainForm()
        {
            InitializeComponent();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    this.Icon = appIcon;
                    SendMessage(this.Handle, WM_SETICON, (IntPtr)ICON_SMALL, appIcon.Handle);
                    SendMessage(this.Handle, WM_SETICON, (IntPtr)ICON_BIG, appIcon.Handle);

                    if (_pbTitleIcon != null) _pbTitleIcon.Image = appIcon.ToBitmap();
                    if (_pbCardIcon != null) _pbCardIcon.Image = appIcon.ToBitmap();
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.ClientSize = new Size(860, 640);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgCanvas;
            this.Font = Theme.FontRegular;
            this.Text = "Pexoris NetworkReset";

            // 1. TitleBar (H=44)
            _pnlTitleBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(860, 44),
                BackColor = Color.FromArgb(9, 14, 23) // Sleek Dark Header
            };
            _pnlTitleBar.MouseDown += TitleBar_MouseDown;

            _pbTitleIcon = new PictureBox
            {
                Location = new Point(14, 11),
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            _pbTitleIcon.MouseDown += TitleBar_MouseDown;

            _lblTitle = new Label
            {
                Location = new Point(44, 12),
                Size = new Size(400, 20),
                Text = "Pexoris NetworkReset • Portable Utility",
                Font = Theme.FontBold,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            _lblTitle.MouseDown += TitleBar_MouseDown;

            _btnMinimize = new Label
            {
                Location = new Point(780, 0),
                Size = new Size(40, 44),
                Text = "—",
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            _btnMinimize.MouseEnter += (s, e) => { _btnMinimize.BackColor = Color.FromArgb(30, 41, 59); _btnMinimize.ForeColor = Color.White; };
            _btnMinimize.MouseLeave += (s, e) => { _btnMinimize.BackColor = Color.Transparent; _btnMinimize.ForeColor = Color.FromArgb(148, 163, 184); };

            _btnClose = new Label
            {
                Location = new Point(820, 0),
                Size = new Size(40, 44),
                Text = "✕",
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnClose.Click += (s, e) => Application.Exit();
            _btnClose.MouseEnter += (s, e) => { _btnClose.BackColor = Color.FromArgb(220, 38, 38); _btnClose.ForeColor = Color.White; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.BackColor = Color.Transparent; _btnClose.ForeColor = Color.FromArgb(148, 163, 184); };

            _pnlTitleBar.Controls.Add(_pbTitleIcon);
            _pnlTitleBar.Controls.Add(_lblTitle);
            _pnlTitleBar.Controls.Add(_btnMinimize);
            _pnlTitleBar.Controls.Add(_btnClose);

            // 2. Top Control Card (Y=52, H=118, W=820)
            _pnlTopCard = new Panel
            {
                Location = new Point(20, 52),
                Size = new Size(820, 118),
                BackColor = Theme.CardBg
            };
            _pnlTopCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath gp = Theme.GetRoundedPath(new RectangleF(0.5f, 0.5f, _pnlTopCard.Width - 1f, _pnlTopCard.Height - 1f), 8f))
                using (Pen pen = new Pen(Theme.BorderLight, 1.2f))
                {
                    e.Graphics.DrawPath(pen, gp);
                }
            };

            _pbCardIcon = new PictureBox
            {
                Location = new Point(16, 14),
                Size = new Size(44, 44),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            _lblCardHeadline = new Label
            {
                Location = new Point(68, 14),
                Size = new Size(480, 22),
                Text = "Pexoris NetworkReset • TCP/IP & Winsock Restoration",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero
            };

            _lblCardSub = new Label
            {
                Location = new Point(68, 37),
                Size = new Size(520, 18),
                Text = "1-Click complete reset of Winsock catalog, TCP/IP stack, DNS cache & DHCP leases.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub
            };

            _lblStatusPill = new Label
            {
                Location = new Point(620, 14),
                Size = new Size(184, 26),
                Text = "● Network: Checking...",
                Font = Theme.FontBold,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.PrimaryBlueLight,
                ForeColor = Theme.PrimaryBlue
            };

            // 4 Stat boxes (Y=62, H=46, W=190 each)
            int statW = 191;
            int statGap = 8;
            int startX = 16;
            int statY = 60;

            _pnlStat1 = CreateStatBox(startX, statY, statW, 46, "ACTIVE ADAPTERS", out _lblStat1Val);
            _pnlStat2 = CreateStatBox(startX + (statW + statGap), statY, statW, 46, "WINSOCK CATALOG", out _lblStat2Val);
            _pnlStat3 = CreateStatBox(startX + (statW + statGap) * 2, statY, statW, 46, "DNS RESOLVER", out _lblStat3Val);
            _pnlStat4 = CreateStatBox(startX + (statW + statGap) * 3, statY, statW, 46, "TCP/IP STACK", out _lblStat4Val);

            _pnlTopCard.Controls.Add(_pbCardIcon);
            _pnlTopCard.Controls.Add(_lblCardHeadline);
            _pnlTopCard.Controls.Add(_lblCardSub);
            _pnlTopCard.Controls.Add(_lblStatusPill);
            _pnlTopCard.Controls.Add(_pnlStat1);
            _pnlTopCard.Controls.Add(_pnlStat2);
            _pnlTopCard.Controls.Add(_pnlStat3);
            _pnlTopCard.Controls.Add(_pnlStat4);

            // 3. Explorer ListView (Y=178, H=320, W=820)
            _lvNetwork = new ListView
            {
                Location = new Point(20, 178),
                Size = new Size(820, 320),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = Theme.FontRegular,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };

            _lvNetwork.Columns.Add("Component / Network Adapter", 250);
            _lvNetwork.Columns.Add("Type / Protocol", 170);
            _lvNetwork.Columns.Add("Details / IP Address", 180);
            _lvNetwork.Columns.Add("State", 100);
            _lvNetwork.Columns.Add("Recommended Action", 110);

            try { SetWindowTheme(_lvNetwork.Handle, "Explorer", null); } catch { }

            // 4. Action Buttons Bar (Y=506, H=48, W=820)
            _pnlActions = new Panel
            {
                Location = new Point(20, 506),
                Size = new Size(820, 48),
                BackColor = Color.Transparent
            };

            _btnFullReset = new PexorisButton
            {
                Location = new Point(0, 4),
                Size = new Size(230, 40),
                Text = "⚡ Full Network Stack Reset",
                Style = PexorisButtonStyle.DestructiveRed
            };
            _btnFullReset.Click += BtnFullReset_Click;

            _btnFlushDns = new PexorisButton
            {
                Location = new Point(238, 4),
                Size = new Size(160, 40),
                Text = "🌐 Flush DNS & ARP",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            _btnFlushDns.Click += BtnFlushDns_Click;

            _btnRenewDhcp = new PexorisButton
            {
                Location = new Point(406, 4),
                Size = new Size(160, 40),
                Text = "🔄 Renew DHCP",
                Style = PexorisButtonStyle.SecondarySky
            };
            _btnRenewDhcp.Click += BtnRenewDhcp_Click;

            _btnResetFirewall = new PexorisButton
            {
                Location = new Point(574, 4),
                Size = new Size(140, 40),
                Text = "🛡️ Reset Firewall",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnResetFirewall.Click += BtnResetFirewall_Click;

            _btnRefresh = new PexorisButton
            {
                Location = new Point(722, 4),
                Size = new Size(98, 40),
                Text = "🔍 Refresh",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRefresh.Click += (s, e) => PerformAudit();

            _pnlActions.Controls.Add(_btnFullReset);
            _pnlActions.Controls.Add(_btnFlushDns);
            _pnlActions.Controls.Add(_btnRenewDhcp);
            _pnlActions.Controls.Add(_btnResetFirewall);
            _pnlActions.Controls.Add(_btnRefresh);

            // 5. Footer (Y=562, H=38, W=860)
            _pnlFooter = new Panel
            {
                Location = new Point(0, 562),
                Size = new Size(860, 38),
                BackColor = Color.White
            };
            _pnlFooter.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
                }
            };

            _chkRestartAdapters = new CheckBox
            {
                Location = new Point(22, 10),
                Size = new Size(240, 20),
                Text = "Restart Network Adapters automatically",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextBody,
                Checked = true
            };

            _lblFooterStatus = new Label
            {
                Location = new Point(270, 11),
                Size = new Size(460, 18),
                Text = "Auditing network interfaces & sockets...",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            _lnkWebsite = new LinkLabel
            {
                Location = new Point(740, 11),
                Size = new Size(100, 18),
                Text = "pexoris.com",
                Font = Theme.FontSmall,
                LinkColor = Theme.PrimaryBlue,
                TextAlign = ContentAlignment.MiddleRight
            };
            _lnkWebsite.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            _pnlFooter.Controls.Add(_chkRestartAdapters);
            _pnlFooter.Controls.Add(_lblFooterStatus);
            _pnlFooter.Controls.Add(_lnkWebsite);

            this.Controls.Add(_pnlTitleBar);
            this.Controls.Add(_pnlTopCard);
            this.Controls.Add(_lvNetwork);
            this.Controls.Add(_pnlActions);
            this.Controls.Add(_pnlFooter);

            this.ResumeLayout(false);

            this.Load += (s, e) => PerformAudit();
        }

        private Panel CreateStatBox(int x, int y, int w, int h, string labelText, out Label valLabel)
        {
            Panel p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath gp = Theme.GetRoundedPath(new RectangleF(0.5f, 0.5f, p.Width - 1f, p.Height - 1f), 6f))
                using (Pen pen = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawPath(pen, gp);
                }
            };

            Label lbl = new Label
            {
                Location = new Point(8, 6),
                Size = new Size(w - 16, 14),
                Text = labelText,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            valLabel = new Label
            {
                Location = new Point(8, 22),
                Size = new Size(w - 16, 20),
                Text = "Checking...",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero
            };

            p.Controls.Add(lbl);
            p.Controls.Add(valLabel);
            return p;
        }

        private void PerformAudit()
        {
            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Scanning network interfaces, TCP/IP stack & protocol catalogs...";

            _auditResult = NetworkEngine.PerformAudit();

            _lvNetwork.BeginUpdate();
            _lvNetwork.Items.Clear();

            foreach (var item in _auditResult.Components)
            {
                ListViewItem lvi = new ListViewItem(item.Component);
                lvi.SubItems.Add(item.TypeOrProtocol);
                lvi.SubItems.Add(item.Details);
                lvi.SubItems.Add(item.State);
                lvi.SubItems.Add(item.RecommendedAction);

                if (item.IsDegraded)
                {
                    lvi.ForeColor = Color.FromArgb(220, 38, 38); // Red
                    lvi.BackColor = Color.FromArgb(254, 242, 242);
                }
                else if (item.State.Contains("Connected") || item.State.Contains("Running") || item.State.Contains("Active"))
                {
                    lvi.ForeColor = Color.FromArgb(15, 23, 42);
                }
                else
                {
                    lvi.ForeColor = Theme.TextSub;
                }

                _lvNetwork.Items.Add(lvi);
            }

            _lvNetwork.EndUpdate();

            // Update stats
            _lblStat1Val.Text = string.Format("{0} Online / {1} Total", _auditResult.ActiveAdaptersCount, _auditResult.TotalAdaptersCount);
            _lblStat1Val.ForeColor = (_auditResult.ActiveAdaptersCount > 0) ? Color.FromArgb(5, 150, 105) : Color.FromArgb(220, 38, 38);

            _lblStat2Val.Text = _auditResult.WinsockStatus;
            _lblStat2Val.ForeColor = _auditResult.WinsockStatus.Contains("Active") ? Color.FromArgb(5, 150, 105) : Color.FromArgb(220, 38, 38);

            _lblStat3Val.Text = _auditResult.DnsCacheStatus;
            _lblStat3Val.ForeColor = _auditResult.DnsCacheStatus.Contains("Running") || _auditResult.DnsCacheStatus.Contains("Active") ? Color.FromArgb(5, 150, 105) : Color.FromArgb(220, 38, 38);

            _lblStat4Val.Text = _auditResult.TcpIpStatus;
            _lblStat4Val.ForeColor = Color.FromArgb(5, 150, 105);

            // Update status capsule pill
            if (_auditResult.IsAnyIssueDetected || _auditResult.ActiveAdaptersCount == 0)
            {
                _lblStatusPill.Text = "● Network: Degraded / Attention";
                _lblStatusPill.BackColor = Color.FromArgb(254, 226, 226);
                _lblStatusPill.ForeColor = Color.FromArgb(220, 38, 38);
            }
            else
            {
                _lblStatusPill.Text = "● Network: Connected & Active";
                _lblStatusPill.BackColor = Color.FromArgb(236, 253, 245);
                _lblStatusPill.ForeColor = Color.FromArgb(5, 150, 105);
            }

            _lblFooterStatus.Text = string.Format("Audited {0} network components • {1} active adapters • Ready",
                _auditResult.Components.Count, _auditResult.ActiveAdaptersCount);

            Cursor = Cursors.Default;
        }

        private void BtnFullReset_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(this,
                "Are you sure you want to perform a Full Network Stack Reset?\n\nThis will:\n• Reset the Winsock Catalog to factory settings\n• Re-initialize the TCP/IP stack (IPv4 & IPv6)\n• Release & renew DHCP IP leases\n• Flush local DNS resolver cache & purge ARP tables\n• Reload NetBIOS name tables" +
                (_chkRestartAdapters.Checked ? "\n• Restart physical network adapters" : "") +
                "\n\nActive network connections will momentarily disconnect. Continue?",
                "Confirm Full Network Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (dr != DialogResult.Yes) return;

            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Executing full network stack reset...";

            string msg = NetworkEngine.FullNetworkReset(_chkRestartAdapters.Checked);
            PerformAudit();

            Cursor = Cursors.Default;
            MessageBox.Show(this, msg + "\n\nTip: A system reboot is recommended if you previously experienced socket corruption.",
                "Network Stack Rebuilt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnFlushDns_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Flushing DNS resolver cache and purging ARP tables...";

            string msg = NetworkEngine.FlushDnsAndArp();
            PerformAudit();

            Cursor = Cursors.Default;
            MessageBox.Show(this, msg, "DNS & ARP Purged", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRenewDhcp_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Releasing and renewing DHCP leases...";

            string msg = NetworkEngine.RenewDhcp();
            PerformAudit();

            Cursor = Cursors.Default;
            MessageBox.Show(this, msg, "DHCP Renewed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnResetFirewall_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(this,
                "Reset Windows Defender Firewall to default policy rules?\n\nCustom inbound/outbound rules created by non-default apps will be restored to Windows defaults.",
                "Reset Windows Firewall", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Resetting Windows Defender Firewall rules...";

            string msg = NetworkEngine.ResetFirewall();
            PerformAudit();

            Cursor = Cursors.Default;
            MessageBox.Show(this, msg, "Firewall Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }
    }
}
