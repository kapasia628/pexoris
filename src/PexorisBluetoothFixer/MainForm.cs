using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisBluetoothFixer
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
        private ListView _lvBluetooth;

        // Action Buttons Bar
        private Panel _pnlActions;
        private PexorisButton _btnFullReset;
        private PexorisButton _btnRestartServices;
        private PexorisButton _btnPurgeLocks;
        private PexorisButton _btnDisablePowerSave;
        private PexorisButton _btnRefresh;

        // Footer
        private Panel _pnlFooter;
        private CheckBox _chkDisablePowerSave;
        private Label _lblFooterStatus;
        private LinkLabel _lnkWebsite;

        private BluetoothAuditResult _auditResult;

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
            this.Text = "Pexoris BluetoothFixer";

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
                Text = "Pexoris BluetoothFixer • Portable Utility",
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
                Text = "Pexoris BluetoothFixer • Radio & Service Restoration",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero
            };

            _lblCardSub = new Label
            {
                Location = new Point(68, 37),
                Size = new Size(520, 18),
                Text = "1-Click fix for missing Bluetooth toggle, hung bthserv, paired cache locks & audio stutter.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub
            };

            _lblStatusPill = new Label
            {
                Location = new Point(620, 14),
                Size = new Size(184, 26),
                Text = "● Bluetooth: Checking...",
                Font = Theme.FontBold,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.PrimaryBlueLight,
                ForeColor = Theme.PrimaryBlue
            };

            // 4 Stat boxes (Y=60, H=46, W=191 each)
            int statW = 191;
            int statGap = 8;
            int startX = 16;
            int statY = 60;

            _pnlStat1 = CreateStatBox(startX, statY, statW, 46, "BLUETOOTH RADIO", out _lblStat1Val);
            _pnlStat2 = CreateStatBox(startX + (statW + statGap), statY, statW, 46, "BTHSERV STATUS", out _lblStat2Val);
            _pnlStat3 = CreateStatBox(startX + (statW + statGap) * 2, statY, statW, 46, "AUDIO ENGINE", out _lblStat3Val);
            _pnlStat4 = CreateStatBox(startX + (statW + statGap) * 3, statY, statW, 46, "PAIRED DEVICES", out _lblStat4Val);

            _pnlTopCard.Controls.Add(_pbCardIcon);
            _pnlTopCard.Controls.Add(_lblCardHeadline);
            _pnlTopCard.Controls.Add(_lblCardSub);
            _pnlTopCard.Controls.Add(_lblStatusPill);
            _pnlTopCard.Controls.Add(_pnlStat1);
            _pnlTopCard.Controls.Add(_pnlStat2);
            _pnlTopCard.Controls.Add(_pnlStat3);
            _pnlTopCard.Controls.Add(_pnlStat4);

            // 3. Explorer ListView (Y=178, H=320, W=820)
            _lvBluetooth = new ListView
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

            _lvBluetooth.Columns.Add("Component / Device", 250);
            _lvBluetooth.Columns.Add("Service / Hardware Class", 170);
            _lvBluetooth.Columns.Add("Details / Device ID", 180);
            _lvBluetooth.Columns.Add("State", 100);
            _lvBluetooth.Columns.Add("Recommended Action", 110);

            try { SetWindowTheme(_lvBluetooth.Handle, "Explorer", null); } catch { }

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
                Text = "⚡ Full Bluetooth Stack Reset",
                Style = PexorisButtonStyle.DestructiveRed
            };
            _btnFullReset.Click += BtnFullReset_Click;

            _btnRestartServices = new PexorisButton
            {
                Location = new Point(238, 4),
                Size = new Size(160, 40),
                Text = "🔄 Restart bthserv",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            _btnRestartServices.Click += BtnRestartServices_Click;

            _btnPurgeLocks = new PexorisButton
            {
                Location = new Point(406, 4),
                Size = new Size(160, 40),
                Text = "🧹 Purge Pairing Locks",
                Style = PexorisButtonStyle.SecondarySky
            };
            _btnPurgeLocks.Click += BtnPurgeLocks_Click;

            _btnDisablePowerSave = new PexorisButton
            {
                Location = new Point(574, 4),
                Size = new Size(140, 40),
                Text = "🔋 Disable Sleep",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnDisablePowerSave.Click += BtnDisablePowerSave_Click;

            _btnRefresh = new PexorisButton
            {
                Location = new Point(722, 4),
                Size = new Size(98, 40),
                Text = "🔍 Refresh",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRefresh.Click += (s, e) => PerformAudit();

            _pnlActions.Controls.Add(_btnFullReset);
            _pnlActions.Controls.Add(_btnRestartServices);
            _pnlActions.Controls.Add(_btnPurgeLocks);
            _pnlActions.Controls.Add(_btnDisablePowerSave);
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

            _chkDisablePowerSave = new CheckBox
            {
                Location = new Point(22, 10),
                Size = new Size(250, 20),
                Text = "Keep Bluetooth Radio Awake (Prevent Sleep)",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextBody,
                Checked = true
            };

            _lblFooterStatus = new Label
            {
                Location = new Point(280, 11),
                Size = new Size(450, 18),
                Text = "Auditing Bluetooth drivers, services & radios...",
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

            _pnlFooter.Controls.Add(_chkDisablePowerSave);
            _pnlFooter.Controls.Add(_lblFooterStatus);
            _pnlFooter.Controls.Add(_lnkWebsite);

            this.Controls.Add(_pnlTitleBar);
            this.Controls.Add(_pnlTopCard);
            this.Controls.Add(_lvBluetooth);
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
            _lblFooterStatus.Text = "Scanning Bluetooth radios, bthserv daemon & paired devices...";

            _auditResult = BluetoothEngine.PerformAudit();

            _lvBluetooth.BeginUpdate();
            _lvBluetooth.Items.Clear();

            foreach (var item in _auditResult.Components)
            {
                ListViewItem lvi = new ListViewItem(item.Component);
                lvi.SubItems.Add(item.TypeOrClass);
                lvi.SubItems.Add(item.Details);
                lvi.SubItems.Add(item.State);
                lvi.SubItems.Add(item.RecommendedAction);

                if (item.IsDegraded)
                {
                    lvi.ForeColor = Color.FromArgb(220, 38, 38); // Red
                    lvi.BackColor = Color.FromArgb(254, 242, 242);
                }
                else if (item.State.Contains("Running") || item.State.Contains("Registered") || item.State.Contains("Present") || item.State.Contains("Paired"))
                {
                    lvi.ForeColor = Color.FromArgb(15, 23, 42);
                }
                else
                {
                    lvi.ForeColor = Theme.TextSub;
                }

                _lvBluetooth.Items.Add(lvi);
            }

            _lvBluetooth.EndUpdate();

            // Update stats
            _lblStat1Val.Text = _auditResult.RadioAdapterName;
            _lblStat1Val.ForeColor = _auditResult.IsRadioDetected ? Color.FromArgb(5, 150, 105) : Color.FromArgb(220, 38, 38);

            _lblStat2Val.Text = _auditResult.BthServStatus;
            _lblStat2Val.ForeColor = _auditResult.BthServStatus.Contains("Running") ? Color.FromArgb(5, 150, 105) : Color.FromArgb(220, 38, 38);

            _lblStat3Val.Text = _auditResult.AudioStackStatus;
            _lblStat3Val.ForeColor = _auditResult.AudioStackStatus.Contains("Running") ? Color.FromArgb(5, 150, 105) : Color.FromArgb(220, 38, 38);

            _lblStat4Val.Text = string.Format("{0} Paired Devices", _auditResult.PairedDevicesCount);
            _lblStat4Val.ForeColor = Color.FromArgb(15, 23, 42);

            // Update status capsule pill
            if (_auditResult.IsAnyIssueDetected || !_auditResult.IsRadioDetected)
            {
                _lblStatusPill.Text = "● Bluetooth: Radio / Service Issue";
                _lblStatusPill.BackColor = Color.FromArgb(254, 226, 226);
                _lblStatusPill.ForeColor = Color.FromArgb(220, 38, 38);
            }
            else
            {
                _lblStatusPill.Text = "● Bluetooth: Active & Ready";
                _lblStatusPill.BackColor = Color.FromArgb(236, 253, 245);
                _lblStatusPill.ForeColor = Color.FromArgb(5, 150, 105);
            }

            _lblFooterStatus.Text = string.Format("Audited {0} items • Radio: {1} • bthserv: {2} • Ready",
                _auditResult.Components.Count, _auditResult.IsRadioDetected ? "Active" : "Missing", _auditResult.BthServStatus);

            Cursor = Cursors.Default;
        }

        private void BtnFullReset_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(this,
                "Execute Full Bluetooth Stack Reset?\n\nThis will:\n• Restart and reconfigure bthserv to Automatic\n• Re-initialize Bluetooth Audio Gateway (BthHFSrv)\n• Restart Device Association Service & Windows Audio\n• Re-enumerate PnP hardware device tree" +
                (_chkDisablePowerSave.Checked ? "\n• Disable Bluetooth USB selective suspend power saving" : "") +
                "\n\nConnected Bluetooth devices will briefly disconnect. Continue?",
                "Confirm Full Bluetooth Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (dr != DialogResult.Yes) return;

            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Executing full Bluetooth stack reset & service restoration...";

            string msg = BluetoothEngine.FullBluetoothReset(_chkDisablePowerSave.Checked);
            PerformAudit();

            Cursor = Cursors.Default;
            MessageBox.Show(this, msg, "Bluetooth Rebuilt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRestartServices_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Restarting bthserv and Windows Audio...";

            string msg = BluetoothEngine.RestartServicesOnly();
            PerformAudit();

            Cursor = Cursors.Default;
            MessageBox.Show(this, msg, "Services Restarted", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnPurgeLocks_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Purging pairing discovery locks...";

            string msg = BluetoothEngine.PurgePairingLocks();
            PerformAudit();

            Cursor = Cursors.Default;
            MessageBox.Show(this, msg, "Locks Purged", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDisablePowerSave_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Disabling Bluetooth selective suspend power saving...";

            string msg = BluetoothEngine.DisableBluetoothPowerSaving();
            PerformAudit();

            Cursor = Cursors.Default;
            MessageBox.Show(this, msg, "Power Saving Disabled", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
