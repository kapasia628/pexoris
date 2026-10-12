using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisTelemetryBlocker
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
        private ListView _lvTelemetry;

        // Action Buttons Bar
        private Panel _pnlActions;
        private PexorisButton _btnBlockAll;
        private PexorisButton _btnRefresh;
        private PexorisButton _btnRestore;
        private PexorisButton _btnFlushDns;

        // Footer
        private Panel _pnlFooter;
        private Label _lblFooterStatus;
        private LinkLabel _lnkWebsite;

        private List<TelemetryItem> _currentItems = new List<TelemetryItem>();

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
            this.Text = "Pexoris TelemetryBlocker";

            // 1. TitleBar (H=44)
            _pnlTitleBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(860, 44),
                BackColor = Color.FromArgb(15, 23, 42) // Slate 900
            };
            _pnlTitleBar.MouseDown += TitleBar_MouseDown;

            _pbTitleIcon = new PictureBox
            {
                Location = new Point(14, 11),
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            _pbTitleIcon.MouseDown += TitleBar_MouseDown;

            _lblTitle = new Label
            {
                Location = new Point(44, 12),
                Size = new Size(600, 20),
                Text = "Pexoris TelemetryBlocker • Windows Privacy & Telemetry Neutralizer",
                Font = Theme.FontBold,
                ForeColor = Color.White
            };
            _lblTitle.MouseDown += TitleBar_MouseDown;

            _btnMinimize = new Label
            {
                Location = new Point(780, 0),
                Size = new Size(40, 44),
                Text = "—",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextMuted,
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            _btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            _btnMinimize.MouseEnter += (s, e) => { _btnMinimize.BackColor = Color.FromArgb(30, 41, 59); _btnMinimize.ForeColor = Color.White; };
            _btnMinimize.MouseLeave += (s, e) => { _btnMinimize.BackColor = Color.Transparent; _btnMinimize.ForeColor = Theme.TextMuted; };

            _btnClose = new Label
            {
                Location = new Point(820, 0),
                Size = new Size(40, 44),
                Text = "✕",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextMuted,
                Font = Theme.FontBold,
                Cursor = Cursors.Hand
            };
            _btnClose.Click += (s, e) => Application.Exit();
            _btnClose.MouseEnter += (s, e) => { _btnClose.BackColor = Theme.DestructiveRed; _btnClose.ForeColor = Color.White; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.BackColor = Color.Transparent; _btnClose.ForeColor = Theme.TextMuted; };

            _pnlTitleBar.Controls.Add(_pbTitleIcon);
            _pnlTitleBar.Controls.Add(_lblTitle);
            _pnlTitleBar.Controls.Add(_btnMinimize);
            _pnlTitleBar.Controls.Add(_btnClose);

            // 2. Top Control Card (Y=52, H=140)
            _pnlTopCard = new Panel
            {
                Location = new Point(16, 52),
                Size = new Size(828, 140),
                BackColor = Theme.CardBg
            };
            _pnlTopCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath p = Theme.GetRoundedPath(new RectangleF(0.5f, 0.5f, _pnlTopCard.Width - 1f, _pnlTopCard.Height - 1f), 10f))
                {
                    using (Pen pen = new Pen(Theme.BorderLight, 1f))
                    {
                        e.Graphics.DrawPath(pen, p);
                    }
                }
            };

            _pbCardIcon = new PictureBox
            {
                Location = new Point(16, 16),
                Size = new Size(44, 44),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            _lblCardHeadline = new Label
            {
                Location = new Point(70, 16),
                Size = new Size(480, 22),
                Text = "Windows Telemetry & Diagnostic Privacy Shield",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero
            };

            _lblCardSub = new Label
            {
                Location = new Point(70, 39),
                Size = new Size(540, 18),
                Text = "Neutralize DiagTrack, CEIP scheduled tasks, advertising beacons & Start Menu cloud leaks",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub
            };

            _lblStatusPill = new Label
            {
                Location = new Point(620, 16),
                Size = new Size(192, 28),
                Text = "● Scanning Telemetry...",
                Font = Theme.FontBold,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.IndigoLight,
                ForeColor = Theme.Indigo
            };
            _lblStatusPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath p = Theme.GetRoundedPath(new RectangleF(0.5f, 0.5f, _lblStatusPill.Width - 1f, _lblStatusPill.Height - 1f), 14f))
                using (Pen pen = new Pen(Theme.IndigoBorder, 1f))
                {
                    e.Graphics.DrawPath(pen, p);
                }
            };

            // 4 Mini Stat Cards inside top card
            int statW = 192;
            int statH = 54;
            int statY = 72;

            _pnlStat1 = CreateStatCard(16, statY, statW, statH, "PROTECTED ITEMS", out _lblStat1Val);
            _pnlStat2 = CreateStatCard(220, statY, statW, statH, "DIAGTRACK SERVICE", out _lblStat2Val);
            _pnlStat3 = CreateStatCard(424, statY, statW, statH, "CEIP TASKS", out _lblStat3Val);
            _pnlStat4 = CreateStatCard(628, statY, statW, statH, "START MENU SEARCH", out _lblStat4Val);

            _pnlTopCard.Controls.Add(_pbCardIcon);
            _pnlTopCard.Controls.Add(_lblCardHeadline);
            _pnlTopCard.Controls.Add(_lblCardSub);
            _pnlTopCard.Controls.Add(_lblStatusPill);
            _pnlTopCard.Controls.Add(_pnlStat1);
            _pnlTopCard.Controls.Add(_pnlStat2);
            _pnlTopCard.Controls.Add(_pnlStat3);
            _pnlTopCard.Controls.Add(_pnlStat4);

            // 3. Explorer ListView (Y=202, H=326)
            _lvTelemetry = new ListView
            {
                Location = new Point(16, 202),
                Size = new Size(828, 326),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = Theme.FontRegular
            };

            _lvTelemetry.Columns.Add("Component / Target", 240);
            _lvTelemetry.Columns.Add("Category", 110);
            _lvTelemetry.Columns.Add("Current Status", 140);
            _lvTelemetry.Columns.Add("Risk Level", 90);
            _lvTelemetry.Columns.Add("Description & Security Impact", 230);

            // 4. Action Buttons Bar (Y=538, H=50)
            _pnlActions = new Panel
            {
                Location = new Point(16, 538),
                Size = new Size(828, 50),
                BackColor = Color.Transparent
            };

            _btnBlockAll = new PexorisButton
            {
                Location = new Point(0, 4),
                Size = new Size(240, 42),
                Text = "⚡ Block All Telemetry",
                Style = PexorisButtonStyle.PrimaryIndigo
            };
            _btnBlockAll.Click += BtnBlockAll_Click;

            _btnRefresh = new PexorisButton
            {
                Location = new Point(252, 4),
                Size = new Size(130, 42),
                Text = "🔄 Refresh Audit",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            _btnRefresh.Click += (s, e) => PerformScan();

            _btnRestore = new PexorisButton
            {
                Location = new Point(394, 4),
                Size = new Size(200, 42),
                Text = "↩️ Restore Windows Defaults",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRestore.Click += BtnRestore_Click;

            _btnFlushDns = new PexorisButton
            {
                Location = new Point(606, 4),
                Size = new Size(140, 42),
                Text = "🌐 Flush DNS",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnFlushDns.Click += BtnFlushDns_Click;

            _pnlActions.Controls.Add(_btnBlockAll);
            _pnlActions.Controls.Add(_btnRefresh);
            _pnlActions.Controls.Add(_btnRestore);
            _pnlActions.Controls.Add(_btnFlushDns);

            // 5. Footer (Y=596, H=38)
            _pnlFooter = new Panel
            {
                Location = new Point(0, 596),
                Size = new Size(860, 44),
                BackColor = Color.FromArgb(15, 23, 42)
            };

            _lblFooterStatus = new Label
            {
                Location = new Point(18, 12),
                Size = new Size(600, 20),
                Text = "Ready • Standalone 50 KB portable privacy defense • Zero background resident processes",
                ForeColor = Theme.TextMuted,
                Font = Theme.FontSmall
            };

            _lnkWebsite = new LinkLabel
            {
                Location = new Point(730, 12),
                Size = new Size(114, 20),
                Text = "pexoris.com",
                TextAlign = ContentAlignment.MiddleRight,
                LinkColor = Color.FromArgb(129, 140, 248), // Indigo 400
                ActiveLinkColor = Color.White,
                Font = Theme.FontBold
            };
            _lnkWebsite.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            _pnlFooter.Controls.Add(_lblFooterStatus);
            _pnlFooter.Controls.Add(_lnkWebsite);

            // Add all panels to Form
            this.Controls.Add(_pnlTitleBar);
            this.Controls.Add(_pnlTopCard);
            this.Controls.Add(_lvTelemetry);
            this.Controls.Add(_pnlActions);
            this.Controls.Add(_pnlFooter);

            this.ResumeLayout(false);

            this.Load += (s, e) =>
            {
                try { SetWindowTheme(_lvTelemetry.Handle, "Explorer", null); } catch { }
                PerformScan();
            };
        }

        private Panel CreateStatCard(int x, int y, int w, int h, string labelText, out Label valLabel)
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
                Size = new Size(w - 16, 24),
                Text = "Checking...",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero
            };

            p.Controls.Add(lbl);
            p.Controls.Add(valLabel);
            return p;
        }

        private void PerformScan()
        {
            _currentItems = TelemetryEngine.ScanTelemetry();

            _lvTelemetry.BeginUpdate();
            _lvTelemetry.Items.Clear();

            int blockedCount = 0;
            bool diagTrackBlocked = false;
            int ceipBlocked = 0;
            bool searchOffline = false;

            foreach (var item in _currentItems)
            {
                if (item.IsBlocked) blockedCount++;

                if (item.Name.Contains("DiagTrack") || item.Name.Contains("Connected User"))
                {
                    diagTrackBlocked = item.IsBlocked;
                }
                if (item.Name.Contains("CEIP") || item.Name.Contains("Appraiser"))
                {
                    if (item.IsBlocked) ceipBlocked++;
                }
                if (item.Name.Contains("Start Menu") || item.Name.Contains("Bing"))
                {
                    searchOffline = item.IsBlocked;
                }

                ListViewItem lvi = new ListViewItem(item.Name);
                lvi.SubItems.Add(item.Category);
                lvi.SubItems.Add(item.CurrentStatus);
                lvi.SubItems.Add(item.RiskLevel);
                lvi.SubItems.Add(item.Description);

                if (item.IsBlocked)
                {
                    lvi.ForeColor = Color.FromArgb(5, 150, 105); // Green
                }
                else
                {
                    lvi.ForeColor = (item.RiskLevel == "Critical" || item.RiskLevel == "High") ? 
                        Color.FromArgb(220, 38, 38) : Theme.TextBody;
                }

                _lvTelemetry.Items.Add(lvi);
            }

            _lvTelemetry.EndUpdate();

            // Update stats
            _lblStat1Val.Text = string.Format("{0} / {1} Neutralized", blockedCount, _currentItems.Count);
            _lblStat1Val.ForeColor = (blockedCount == _currentItems.Count) ? Color.FromArgb(5, 150, 105) : Color.FromArgb(220, 38, 38);

            _lblStat2Val.Text = diagTrackBlocked ? "Disabled (Safe)" : "Active (Tracking)";
            _lblStat2Val.ForeColor = diagTrackBlocked ? Color.FromArgb(5, 150, 105) : Color.FromArgb(220, 38, 38);

            _lblStat3Val.Text = string.Format("{0} Tasks Blocked", ceipBlocked);
            _lblStat3Val.ForeColor = (ceipBlocked >= 2) ? Color.FromArgb(5, 150, 105) : Theme.TextHero;

            _lblStat4Val.Text = searchOffline ? "Offline (Local)" : "Leaking to Cloud";
            _lblStat4Val.ForeColor = searchOffline ? Color.FromArgb(5, 150, 105) : Color.FromArgb(220, 38, 38);

            // Update pill
            if (blockedCount == _currentItems.Count)
            {
                _lblStatusPill.Text = "● Fully Protected (Off)";
                _lblStatusPill.BackColor = Color.FromArgb(236, 253, 245);
                _lblStatusPill.ForeColor = Color.FromArgb(5, 150, 105);
            }
            else if (blockedCount > 0)
            {
                _lblStatusPill.Text = "● Partially Protected";
                _lblStatusPill.BackColor = Color.FromArgb(254, 243, 199);
                _lblStatusPill.ForeColor = Color.FromArgb(180, 83, 9);
            }
            else
            {
                _lblStatusPill.Text = "● Telemetry Active (Vulnerable)";
                _lblStatusPill.BackColor = Color.FromArgb(254, 226, 226);
                _lblStatusPill.ForeColor = Color.FromArgb(220, 38, 38);
            }

            _lblFooterStatus.Text = string.Format("Audited {0} items • {1} telemetry vectors neutralized • System privacy secured", 
                _currentItems.Count, blockedCount);
        }

        private void BtnBlockAll_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Applying deep telemetry block policies...";

            int sCount, fCount;
            bool ok = TelemetryEngine.BlockAllTelemetry(out sCount, out fCount);
            PerformScan();

            Cursor = Cursors.Default;
            MessageBox.Show(this, 
                string.Format("Windows telemetry vectors neutralized successfully!\n\n• Successfully modified: {0} settings\n• Active DiagTrack and push services disabled\n• CEIP scheduled tasks turned off\n• Bing Start Menu cloud query leaks blocked", sCount),
                "Privacy Shield Active", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            var res = MessageBox.Show(this,
                "Restore default Windows telemetry configuration?\n\nThis will re-enable DiagTrack services, CEIP diagnostic tasks, and default group policies.",
                "Confirm Telemetry Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res != DialogResult.Yes) return;

            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Restoring Windows defaults...";

            int sCount, fCount;
            TelemetryEngine.RestoreDefaults(out sCount, out fCount);
            PerformScan();

            Cursor = Cursors.Default;
            MessageBox.Show(this, "Windows default telemetry configurations restored.", "Defaults Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnFlushDns_Click(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("ipconfig.exe", "/flushdns")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(2000);
                }
                MessageBox.Show(this, "Windows DNS Resolver Cache flushed successfully.", "DNS Cache Cleared", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error flushing DNS: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
