using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisBatteryOptimizer
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
        private static readonly IntPtr ICON_SMALL = new IntPtr(0);
        private static readonly IntPtr ICON_BIG = new IntPtr(1);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        // Controls
        private Panel _pnlTitleBar;
        private PictureBox _pbTitleIcon;
        private Label _lblTitle;
        private Button _btnMin;
        private Button _btnClose;

        private Panel _pnlTopCard;
        private PictureBox _pbCardIcon;
        private Label _lblCardTitle;
        private Label _lblCardSub;
        private Label _lblPillBadge;
        private TextBox _txtSearch;

        private ListView _lvMetrics;
        private Panel _pnlActionBar;
        private PexorisButton _btnUltraSaver;
        private PexorisButton _btnOptimizeStandby;
        private PexorisButton _btnReport;
        private PexorisButton _btnBalanced;
        private PexorisButton _btnRefresh;

        private Panel _pnlFooter;
        private Label _lblStatus;
        private CheckBox _chkAutoPoll;
        private LinkLabel _lnkSite;

        private System.Windows.Forms.Timer _pollTimer;
        private List<BatteryMetricItem> _cachedMetrics = new List<BatteryMetricItem>();

        public MainForm()
        {
            Width = 860;
            Height = 640;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.BgCanvas;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            InitComponents();

            _pollTimer = new System.Windows.Forms.Timer();
            _pollTimer.Interval = 5000; // 5 seconds
            _pollTimer.Tick += (s, e) => { if (_chkAutoPoll.Checked) RefreshBatteryMetrics(false); };
            _pollTimer.Start();

            RefreshBatteryMetrics(true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                if (Icon != null)
                {
                    SendMessage(Handle, WM_SETICON, ICON_SMALL, Icon.Handle);
                    SendMessage(Handle, WM_SETICON, ICON_BIG, Icon.Handle);
                }
            }
            catch { }
        }

        private void InitComponents()
        {
            // 1. Title Bar (44px)
            _pnlTitleBar = new Panel { Left = 0, Top = 0, Width = 860, Height = 44, BackColor = Color.White };
            _pnlTitleBar.MouseDown += TitleBar_MouseDown;

            _pbTitleIcon = new PictureBox
            {
                Left = 14,
                Top = 11,
                Width = 22,
                Height = 22,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try { if (Icon != null) _pbTitleIcon.Image = Icon.ToBitmap(); } catch { }
            _pbTitleIcon.MouseDown += TitleBar_MouseDown;

            _lblTitle = new Label
            {
                Left = 44,
                Top = 12,
                Width = 480,
                Height = 20,
                Text = "Pexoris BatteryOptimizer • Laptop Power & Battery Health",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };
            _lblTitle.MouseDown += TitleBar_MouseDown;

            _btnMin = new Button
            {
                Left = 770,
                Top = 0,
                Width = 45,
                Height = 44,
                Text = "—",
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontRegular,
                ForeColor = Theme.TextSub,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnMin.FlatAppearance.BorderSize = 0;
            _btnMin.MouseEnter += (s, e) => _btnMin.BackColor = Color.FromArgb(241, 245, 249);
            _btnMin.MouseLeave += (s, e) => _btnMin.BackColor = Color.White;
            _btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;

            _btnClose = new Button
            {
                Left = 815,
                Top = 0,
                Width = 45,
                Height = 44,
                Text = "✕",
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontRegular,
                ForeColor = Theme.TextSub,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.MouseEnter += (s, e) => { _btnClose.BackColor = Color.FromArgb(239, 68, 68); _btnClose.ForeColor = Color.White; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.BackColor = Color.White; _btnClose.ForeColor = Theme.TextSub; };
            _btnClose.Click += (s, e) => Application.Exit();

            _pnlTitleBar.Controls.AddRange(new Control[] { _pbTitleIcon, _lblTitle, _btnMin, _btnClose });
            Controls.Add(_pnlTitleBar);

            // 2. Top Control Card (Y=54, H=124, W=820, X=20)
            _pnlTopCard = new Panel
            {
                Left = 20,
                Top = 54,
                Width = 820,
                Height = 124,
                BackColor = Theme.CardBg
            };
            _pnlTopCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, _pnlTopCard.Width - 1, _pnlTopCard.Height - 1);
                using (Pen pen = new Pen(Theme.BorderLight, 1f))
                {
                    using (GraphicsPath path = Theme.GetRoundedPath(rect, 10f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            _pbCardIcon = new PictureBox
            {
                Left = 16,
                Top = 18,
                Width = 44,
                Height = 44,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try { if (Icon != null) _pbCardIcon.Image = Icon.ToBitmap(); } catch { }

            _lblCardTitle = new Label
            {
                Left = 72,
                Top = 16,
                Width = 360,
                Height = 24,
                Text = "Battery Health & Modern Standby Governor",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            _lblCardSub = new Label
            {
                Left = 72,
                Top = 40,
                Width = 530,
                Height = 20,
                Text = "Maximize laptop runtime, audit battery wear degradation, and eliminate sleep power drains",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lblPillBadge = new Label
            {
                Left = 610,
                Top = 16,
                Width = 192,
                Height = 26,
                Text = "🔋 ON BATTERY (85%)",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontSmall,
                ForeColor = Color.FromArgb(4, 120, 87),
                BackColor = Color.FromArgb(236, 253, 245)
            };
            _lblPillBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, _lblPillBadge.Width - 1, _lblPillBadge.Height - 1);
                using (Pen pen = new Pen(Color.FromArgb(167, 243, 208), 1f))
                {
                    using (GraphicsPath path = Theme.GetRoundedPath(rect, 13f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            // Search filter row inside card
            Label lblSearchIcon = new Label
            {
                Left = 18,
                Top = 80,
                Width = 24,
                Height = 26,
                Text = "🔍",
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.Transparent
            };

            _txtSearch = new TextBox
            {
                Left = 44,
                Top = 79,
                Width = 756,
                Height = 26,
                Font = Theme.FontRegular,
                ForeColor = Theme.TextHero,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.TextChanged += (s, e) => ApplySearchFilter();

            _pnlTopCard.Controls.AddRange(new Control[]
            {
                _pbCardIcon, _lblCardTitle, _lblCardSub, _lblPillBadge,
                lblSearchIcon, _txtSearch
            });
            Controls.Add(_pnlTopCard);

            // 3. Explorer Details ListView (Y=190, H=306, W=820, X=20)
            _lvMetrics = new ListView
            {
                Left = 20,
                Top = 190,
                Width = 820,
                Height = 306,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.FontRegular,
                BackColor = Color.White,
                ForeColor = Theme.TextBody,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            try { SetWindowTheme(_lvMetrics.Handle, "Explorer", null); } catch { }

            _lvMetrics.Columns.Add("Parameter / Hardware Metric", 210);
            _lvMetrics.Columns.Add("Current Reading", 160);
            _lvMetrics.Columns.Add("Target / Baseline", 140);
            _lvMetrics.Columns.Add("Health / State", 130);
            _lvMetrics.Columns.Add("Diagnostic Recommendation", 170);

            Controls.Add(_lvMetrics);

            // 4. Action Buttons Bar (Y=508, H=46, W=820, X=20)
            _pnlActionBar = new Panel { Left = 20, Top = 508, Width = 820, Height = 46, BackColor = Color.Transparent };

            _btnUltraSaver = new PexorisButton
            {
                Left = 0,
                Top = 0,
                Width = 220,
                Height = 44,
                Text = "⚡ Ultra Battery Saver",
                Style = PexorisButtonStyle.DestructiveRed
            };
            _btnUltraSaver.Click += BtnUltraSaver_Click;

            _btnOptimizeStandby = new PexorisButton
            {
                Left = 230,
                Top = 0,
                Width = 190,
                Height = 44,
                Text = "🛡️ Optimize Standby (S0)",
                Style = PexorisButtonStyle.PrimaryGreen
            };
            _btnOptimizeStandby.Click += BtnOptimizeStandby_Click;

            _btnReport = new PexorisButton
            {
                Left = 430,
                Top = 0,
                Width = 180,
                Height = 44,
                Text = "📄 Battery Report (HTML)",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            _btnReport.Click += BtnReport_Click;

            _btnBalanced = new PexorisButton
            {
                Left = 620,
                Top = 0,
                Width = 100,
                Height = 44,
                Text = "⚖️ Balanced",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnBalanced.Click += BtnBalanced_Click;

            _btnRefresh = new PexorisButton
            {
                Left = 728,
                Top = 0,
                Width = 92,
                Height = 44,
                Text = "🔄 Refresh",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRefresh.Click += (s, e) => RefreshBatteryMetrics(true);

            _pnlActionBar.Controls.AddRange(new Control[]
            {
                _btnUltraSaver, _btnOptimizeStandby, _btnReport,
                _btnBalanced, _btnRefresh
            });
            Controls.Add(_pnlActionBar);

            // 5. Footer (Y=566, H=60, W=820, X=20)
            _pnlFooter = new Panel { Left = 20, Top = 566, Width = 820, Height = 60, BackColor = Color.Transparent };

            _chkAutoPoll = new CheckBox
            {
                Left = 0,
                Top = 6,
                Width = 460,
                Height = 22,
                Text = "Continuously poll battery discharge rate and power telemetry every 5 seconds",
                Font = Theme.FontSub,
                ForeColor = Theme.TextBody,
                Checked = true
            };

            _lblStatus = new Label
            {
                Left = 0,
                Top = 32,
                Width = 560,
                Height = 20,
                Text = "Ready • Audited battery hardware health metrics",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            _lnkSite = new LinkLabel
            {
                Left = 660,
                Top = 32,
                Width = 160,
                Height = 20,
                Text = "pexoris.com/tools",
                Font = Theme.FontBold,
                LinkColor = Theme.Emerald,
                ActiveLinkColor = Theme.EmeraldHover,
                TextAlign = ContentAlignment.TopRight
            };
            _lnkSite.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://pexoris.com/tools/battery-optimizer/") { UseShellExecute = true }); } catch { }
            };

            _pnlFooter.Controls.AddRange(new Control[] { _chkAutoPoll, _lblStatus, _lnkSite });
            Controls.Add(_pnlFooter);
        }

        private void RefreshBatteryMetrics(bool updateStatusLabel)
        {
            try
            {
                string pillText;
                string summaryText;
                _cachedMetrics = BatteryEngine.GetBatteryMetrics(out pillText, out summaryText);

                _lblPillBadge.Text = pillText;
                if (pillText.Contains("BATTERY"))
                {
                    _lblPillBadge.ForeColor = Color.FromArgb(4, 120, 87);
                    _lblPillBadge.BackColor = Color.FromArgb(236, 253, 245);
                }
                else
                {
                    _lblPillBadge.ForeColor = Color.FromArgb(30, 64, 175);
                    _lblPillBadge.BackColor = Color.FromArgb(239, 246, 255);
                }

                ApplySearchFilter();

                if (updateStatusLabel)
                {
                    _lblStatus.Text = summaryText + " • Ready";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Telemetry error: " + ex.Message;
            }
        }

        private void ApplySearchFilter()
        {
            _lvMetrics.BeginUpdate();
            _lvMetrics.Items.Clear();

            string filter = (_txtSearch.Text ?? "").Trim().ToLowerInvariant();

            foreach (BatteryMetricItem item in _cachedMetrics)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    bool match = (item.Parameter != null && item.Parameter.ToLowerInvariant().Contains(filter)) ||
                                 (item.Value != null && item.Value.ToLowerInvariant().Contains(filter)) ||
                                 (item.Status != null && item.Status.ToLowerInvariant().Contains(filter));
                    if (!match) continue;
                }

                ListViewItem lvi = new ListViewItem(item.Parameter);
                lvi.SubItems.Add(item.Value);
                lvi.SubItems.Add(item.Ideal);
                lvi.SubItems.Add(item.Status);
                lvi.SubItems.Add(item.Action);

                if (item.Status != null && (item.Status.Contains("Degraded") || item.Status.Contains("Low Battery")))
                {
                    lvi.BackColor = Color.FromArgb(254, 242, 242);
                    lvi.ForeColor = Color.FromArgb(185, 28, 28);
                }
                else if (item.Status != null && item.Status.Contains("Excellent"))
                {
                    lvi.BackColor = Color.FromArgb(236, 253, 245);
                    lvi.ForeColor = Color.FromArgb(4, 120, 87);
                }

                _lvMetrics.Items.Add(lvi);
            }

            _lvMetrics.EndUpdate();
        }

        private void BtnUltraSaver_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                "Enable Ultra Battery Saver mode?\n\n" +
                "• Applies Power Saver power plan\n" +
                "• Lowers display idle timeout to 2 minutes\n" +
                "• Throttles maximum CPU processor frequency on battery to 75%\n" +
                "• Significantly extends remaining laptop battery hours\n\n" +
                "Click OK to apply.",
                "Pexoris BatteryOptimizer",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (dr != DialogResult.OK) return;

            string msg;
            bool ok = BatteryEngine.EnableUltraBatterySaver(out msg);
            RefreshBatteryMetrics(true);
            MessageBox.Show(msg, "Pexoris BatteryOptimizer", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnOptimizeStandby_Click(object sender, EventArgs e)
        {
            string msg;
            bool ok = BatteryEngine.OptimizeModernStandby(out msg);
            RefreshBatteryMetrics(true);
            MessageBox.Show(msg, "Pexoris BatteryOptimizer", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnReport_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _lblStatus.Text = "Generating Windows battery health report...";
            Application.DoEvents();

            string reportPath;
            bool ok = BatteryEngine.GenerateBatteryReport(out reportPath);

            Cursor = Cursors.Default;
            _lblStatus.Text = ok ? "Battery report generated: " + reportPath : "Report generation failed";

            if (!ok)
            {
                MessageBox.Show("Could not generate battery report. Ensure powercfg is accessible.", "Pexoris BatteryOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnBalanced_Click(object sender, EventArgs e)
        {
            string msg;
            bool ok = BatteryEngine.RestoreBalancedPlan(out msg);
            RefreshBatteryMetrics(true);
            MessageBox.Show(msg, "Pexoris BatteryOptimizer", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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
