using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisAudioFixer
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
        private ListView _lvAudio;

        // Action Buttons Bar
        private Panel _pnlActions;
        private PexorisButton _btnFullReset;
        private PexorisButton _btnRestartAudiosrv;
        private PexorisButton _btnFlushExclusive;
        private PexorisButton _btnTestChime;
        private PexorisButton _btnRefresh;

        // Footer
        private Panel _pnlFooter;
        private CheckBox _chkAutoVerify;
        private Label _lblFooterStatus;
        private LinkLabel _lnkWebsite;

        private AudioAuditResult _auditResult;

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
            this.Text = "Pexoris AudioFixer";

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
                Text = "Pexoris AudioFixer • Portable Utility",
                Font = Theme.FontBold,
                ForeColor = Color.FromArgb(248, 250, 252),
                BackColor = Color.Transparent
            };
            _lblTitle.MouseDown += TitleBar_MouseDown;

            _btnMinimize = new Label
            {
                Location = new Point(780, 0),
                Size = new Size(40, 44),
                Text = "—",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnMinimize.MouseEnter += (s, e) => { _btnMinimize.BackColor = Color.FromArgb(30, 41, 59); _btnMinimize.ForeColor = Color.White; };
            _btnMinimize.MouseLeave += (s, e) => { _btnMinimize.BackColor = Color.Transparent; _btnMinimize.ForeColor = Color.FromArgb(148, 163, 184); };
            _btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            _btnClose = new Label
            {
                Location = new Point(820, 0),
                Size = new Size(40, 44),
                Text = "✕",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnClose.MouseEnter += (s, e) => { _btnClose.BackColor = Color.FromArgb(220, 38, 38); _btnClose.ForeColor = Color.White; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.BackColor = Color.Transparent; _btnClose.ForeColor = Color.FromArgb(148, 163, 184); };
            _btnClose.Click += (s, e) => this.Close();

            _pnlTitleBar.Controls.AddRange(new Control[] { _pbTitleIcon, _lblTitle, _btnMinimize, _btnClose });

            // 2. Top Control Card (Y=54, H=135, W=824, X=18)
            _pnlTopCard = new Panel
            {
                Location = new Point(18, 54),
                Size = new Size(824, 135),
                BackColor = Theme.CardBg
            };
            _pnlTopCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, _pnlTopCard.Width - 1, _pnlTopCard.Height - 1);
                }
            };

            _pbCardIcon = new PictureBox
            {
                Location = new Point(14, 14),
                Size = new Size(44, 44),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            _lblCardHeadline = new Label
            {
                Location = new Point(68, 14),
                Size = new Size(540, 22),
                Text = "Windows Audio Subsystem & Endpoint Graph Diagnostic",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero
            };

            _lblCardSub = new Label
            {
                Location = new Point(68, 38),
                Size = new Size(550, 18),
                Text = "Audits Audiosrv, AudioEndpointBuilder, audiodg.exe, and WASAPI render endpoints.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub
            };

            _lblStatusPill = new Label
            {
                Location = new Point(640, 16),
                Size = new Size(168, 26),
                Text = "Audio Subsystem: Healthy",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontSmall,
                BackColor = Theme.SuccessGreenLight,
                ForeColor = Theme.SuccessGreen
            };

            // 4 Stats Panels (Y=66, H=56)
            int statW = 192;
            int statH = 56;
            int statY = 66;

            _pnlStat1 = CreateStatBox(14, statY, statW, statH, "AUDIO SERVICE", "Active / Auto", Theme.PrimaryIndigo, out _lblStat1Val);
            _pnlStat2 = CreateStatBox(14 + statW + 8, statY, statW, statH, "ENDPOINT BUILDER", "Running", Theme.PrimaryIndigo, out _lblStat2Val);
            _pnlStat3 = CreateStatBox(14 + (statW + 8) * 2, statY, statW, statH, "ACTIVE ENDPOINTS", "0 Ready", Theme.Cyan, out _lblStat3Val);
            _pnlStat4 = CreateStatBox(14 + (statW + 8) * 3, statY, statW, statH, "AUDIO GRAPH (DG)", "Isolated", Theme.SuccessGreen, out _lblStat4Val);

            _pnlTopCard.Controls.AddRange(new Control[] {
                _pbCardIcon, _lblCardHeadline, _lblCardSub, _lblStatusPill,
                _pnlStat1, _pnlStat2, _pnlStat3, _pnlStat4
            });

            // 3. Section Label (Y=196)
            Label lblSection = new Label
            {
                Location = new Point(20, 196),
                Size = new Size(600, 20),
                Text = "Audio Render Devices, Win32 Services & Signal Processor Engine:",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero
            };

            // 4. Explorer ListView Container (Y=220, H=280, W=824)
            Panel pnlListContainer = new Panel
            {
                Location = new Point(18, 220),
                Size = new Size(824, 280),
                BackColor = Theme.CardBg,
                Padding = new Padding(1)
            };
            pnlListContainer.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderMedium, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlListContainer.Width - 1, pnlListContainer.Height - 1);
                }
            };

            _lvAudio = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                Font = Theme.FontRegular,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _lvAudio.Columns.Add("Component / Device", 195);
            _lvAudio.Columns.Add("Type / Class", 170);
            _lvAudio.Columns.Add("Diagnostic Details", 230);
            _lvAudio.Columns.Add("State", 100);
            _lvAudio.Columns.Add("Recommended Action", 125);

            pnlListContainer.Controls.Add(_lvAudio);

            // 5. Action Buttons Bar (Y=508, H=50, W=824)
            _pnlActions = new Panel
            {
                Location = new Point(18, 508),
                Size = new Size(824, 50),
                BackColor = Color.Transparent
            };

            _btnFullReset = new PexorisButton
            {
                Location = new Point(0, 7),
                Size = new Size(230, 36),
                Text = "⚡ 1-Click Audio Graph Reset",
                Style = PexorisButtonStyle.DestructiveRed
            };
            _btnFullReset.Click += BtnFullReset_Click;

            _btnRestartAudiosrv = new PexorisButton
            {
                Location = new Point(238, 7),
                Size = new Size(160, 36),
                Text = "Restart Audiosrv",
                Style = PexorisButtonStyle.PrimaryIndigo
            };
            _btnRestartAudiosrv.Click += BtnRestartAudiosrv_Click;

            _btnFlushExclusive = new PexorisButton
            {
                Location = new Point(406, 7),
                Size = new Size(165, 36),
                Text = "Flush Exclusive Mode",
                Style = PexorisButtonStyle.SecondaryCyan
            };
            _btnFlushExclusive.Click += BtnFlushExclusive_Click;

            _btnTestChime = new PexorisButton
            {
                Location = new Point(579, 7),
                Size = new Size(150, 36),
                Text = "Test Audio Chime",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnTestChime.Click += (s, e) => AudioEngine.PlayTestChime();

            _btnRefresh = new PexorisButton
            {
                Location = new Point(737, 7),
                Size = new Size(87, 36),
                Text = "Refresh",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRefresh.Click += (s, e) => RefreshAudit();

            _pnlActions.Controls.AddRange(new Control[] {
                _btnFullReset, _btnRestartAudiosrv, _btnFlushExclusive, _btnTestChime, _btnRefresh
            });

            // 6. Footer (Y=602, H=38)
            _pnlFooter = new Panel
            {
                Location = new Point(0, 602),
                Size = new Size(860, 38),
                BackColor = Color.FromArgb(9, 14, 23)
            };

            _chkAutoVerify = new CheckBox
            {
                Location = new Point(18, 9),
                Size = new Size(330, 20),
                Text = "Play verification audio chime after stack reset",
                Checked = true,
                Font = Theme.FontSmall,
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.Transparent
            };

            _lblFooterStatus = new Label
            {
                Location = new Point(360, 10),
                Size = new Size(360, 20),
                Text = "Ready • Standalone Zero-Bloat Portable Utility",
                Font = Theme.FontSmall,
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent
            };

            _lnkWebsite = new LinkLabel
            {
                Location = new Point(745, 10),
                Size = new Size(95, 20),
                Text = "pexoris.com",
                Font = Theme.FontSmall,
                LinkColor = Color.FromArgb(129, 140, 248),
                ActiveLinkColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopRight
            };
            _lnkWebsite.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://pexoris.com/tools/audio-fixer/") { UseShellExecute = true }); } catch { }
            };

            _pnlFooter.Controls.AddRange(new Control[] { _chkAutoVerify, _lblFooterStatus, _lnkWebsite });

            // Assemble Form
            this.Controls.AddRange(new Control[] {
                _pnlTitleBar,
                _pnlTopCard,
                lblSection,
                pnlListContainer,
                _pnlActions,
                _pnlFooter
            });

            this.ResumeLayout(false);

            try { SetWindowTheme(_lvAudio.Handle, "Explorer", null); } catch { }

            this.Load += (s, e) => RefreshAudit();
        }

        private Panel CreateStatBox(int x, int y, int w, int h, string labelText, string initialVal, Color valColor, out Label outValLabel)
        {
            Panel p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Theme.BgCanvas
            };
            p.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, w - 1, h - 1);
                }
            };

            Label lbl = new Label
            {
                Location = new Point(8, 6),
                Size = new Size(w - 16, 14),
                Text = labelText,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.TextSub
            };

            Label val = new Label
            {
                Location = new Point(8, 22),
                Size = new Size(w - 16, 24),
                Text = initialVal,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = valColor
            };

            p.Controls.Add(lbl);
            p.Controls.Add(val);

            outValLabel = val;
            return p;
        }

        private void RefreshAudit()
        {
            Cursor.Current = Cursors.WaitCursor;
            _auditResult = AudioEngine.RunAudit();

            _lvAudio.BeginUpdate();
            _lvAudio.Items.Clear();

            bool anyDegraded = false;
            foreach (var item in _auditResult.Components)
            {
                ListViewItem lvi = new ListViewItem(item.DeviceName);
                lvi.SubItems.Add(item.TypeOrEndpoint);
                lvi.SubItems.Add(item.Details);
                lvi.SubItems.Add(item.State);
                lvi.SubItems.Add(item.RecommendedAction);

                if (item.IsDegraded)
                {
                    lvi.BackColor = Color.FromArgb(254, 242, 242);
                    lvi.ForeColor = Color.FromArgb(185, 28, 28);
                    anyDegraded = true;
                }
                _lvAudio.Items.Add(lvi);
            }
            _lvAudio.EndUpdate();

            // Update stats
            _lblStat1Val.Text = _auditResult.AudiosrvRunning ? "Active / Auto" : "Stopped";
            _lblStat1Val.ForeColor = _auditResult.AudiosrvRunning ? Theme.PrimaryIndigo : Theme.DestructiveRed;

            _lblStat2Val.Text = _auditResult.EndpointBuilderRunning ? "Running" : "Stopped";
            _lblStat2Val.ForeColor = _auditResult.EndpointBuilderRunning ? Theme.PrimaryIndigo : Theme.DestructiveRed;

            _lblStat3Val.Text = _auditResult.ActiveEndpointsCount + " Ready";
            _lblStat4Val.Text = _auditResult.AudioDgProcessCount > 0 ? "Isolated (OK)" : "On-Demand";

            if (anyDegraded || !_auditResult.AudiosrvRunning || !_auditResult.EndpointBuilderRunning)
            {
                _lblStatusPill.Text = "Audio Stalled / Degraded";
                _lblStatusPill.BackColor = Theme.DestructiveRedLight;
                _lblStatusPill.ForeColor = Theme.DestructiveRed;
            }
            else
            {
                _lblStatusPill.Text = "Audio Subsystem: Healthy";
                _lblStatusPill.BackColor = Theme.SuccessGreenLight;
                _lblStatusPill.ForeColor = Theme.SuccessGreen;
            }

            _lblFooterStatus.Text = string.Format("Audited {0} components. Ready.", _auditResult.Components.Count);
            Cursor.Current = Cursors.Default;
        }

        private void BtnFullReset_Click(object sender, EventArgs e)
        {
            var dr = MessageBox.Show(
                "This will restart Windows Audio services (Audiosrv, AudioEndpointBuilder),\n" +
                "terminate hung audiodg.exe instances, and rescan audio endpoints.\n\n" +
                "Active playback streams may pause briefly.\n\nProceed with 1-Click Audio Reset?",
                "Pexoris AudioFixer", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            Cursor.Current = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Executing 1-Click Audio Graph Reset...";
            Application.DoEvents();

            string log;
            bool ok = AudioEngine.ResetAudioStack(out log);

            Cursor.Current = Cursors.Default;
            RefreshAudit();

            MessageBox.Show(
                (ok ? "Audio stack reset completed successfully!\n\n" : "Audio stack reset finished with warnings:\n\n") + log,
                "Pexoris AudioFixer Result", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnRestartAudiosrv_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Restarting Audiosrv...";
            Application.DoEvents();

            string log;
            AudioEngine.RestartAudiosrvOnly(out log);

            Cursor.Current = Cursors.Default;
            RefreshAudit();
            MessageBox.Show(log, "Pexoris AudioFixer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnFlushExclusive_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            _lblFooterStatus.Text = "Flushing Exclusive Mode Locks...";
            Application.DoEvents();

            string log;
            AudioEngine.FlushExclusiveMode(out log);

            Cursor.Current = Cursors.Default;
            RefreshAudit();
            MessageBox.Show(log, "Pexoris AudioFixer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }
    }
}
