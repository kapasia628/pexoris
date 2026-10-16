using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisStartupTimer
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int CS_DROPSHADOW = 0x00020000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        // UI Controls
        private Panel _titleBar;
        private PictureBox _titleIcon;
        private Label _titleLabel;
        private Button _btnClose;
        private Button _btnMin;

        private Panel _topCard;
        private PictureBox _cardIcon;
        private Label _cardHeadline;
        private Label _cardSubtitle;

        // Metric Stat Tiles
        private Panel _tile1;
        private Panel _tile2;
        private Panel _tile3;
        private Panel _tile4;
        private Label _lblBootTime;
        private Label _lblKernelTime;
        private Label _lblPostTime;
        private Label _lblStartupCount;

        private ListView _listView;
        private Panel _actionBar;
        private PexorisButton _btnToggle;
        private PexorisButton _btnRefresh;
        private PexorisButton _btnBreakdown;
        private PexorisButton _btnOpenFolder;
        private PexorisButton _btnExport;

        private Panel _footerBar;
        private Label _lblStatus;
        private LinkLabel _lnkWebsite;

        private BootMetrics _currentMetrics;
        private List<StartupItem> _startupItems;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Window Configuration (860 x 640 standard)
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(860, 640);
            BackColor = Theme.CanvasBg;
            Text = "Pexoris StartupTimer";

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            // 1. Title Bar (44px)
            _titleBar = new Panel();
            _titleBar.Location = new Point(0, 0);
            _titleBar.Size = new Size(860, 44);
            _titleBar.BackColor = Theme.CardBg;
            _titleBar.MouseDown += TitleBar_MouseDown;

            _titleIcon = new PictureBox();
            _titleIcon.Location = new Point(16, 11);
            _titleIcon.Size = new Size(22, 22);
            _titleIcon.SizeMode = PictureBoxSizeMode.Zoom;
            if (Icon != null) _titleIcon.Image = Icon.ToBitmap();
            _titleIcon.MouseDown += TitleBar_MouseDown;

            _titleLabel = new Label();
            _titleLabel.Location = new Point(46, 13);
            _titleLabel.Size = new Size(500, 20);
            _titleLabel.Text = "Pexoris StartupTimer • Boot Duration & Startup Profiler";
            _titleLabel.Font = Theme.TitleFont;
            _titleLabel.ForeColor = Theme.TextHero;
            _titleLabel.MouseDown += TitleBar_MouseDown;

            _btnMin = new Button();
            _btnMin.Location = new Point(776, 0);
            _btnMin.Size = new Size(42, 44);
            _btnMin.FlatStyle = FlatStyle.Flat;
            _btnMin.FlatAppearance.BorderSize = 0;
            _btnMin.Text = "—";
            _btnMin.Font = new Font("Segoe UI", 9f);
            _btnMin.ForeColor = Theme.TextMuted;
            _btnMin.Cursor = Cursors.Hand;
            _btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;

            _btnClose = new Button();
            _btnClose.Location = new Point(818, 0);
            _btnClose.Size = new Size(42, 44);
            _btnClose.FlatStyle = FlatStyle.Flat;
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Text = "✕";
            _btnClose.Font = new Font("Segoe UI", 9f);
            _btnClose.ForeColor = Theme.TextMuted;
            _btnClose.Cursor = Cursors.Hand;
            _btnClose.MouseEnter += (s, e) => { _btnClose.BackColor = Theme.Destructive; _btnClose.ForeColor = Color.White; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.BackColor = Theme.CardBg; _btnClose.ForeColor = Theme.TextMuted; };
            _btnClose.Click += (s, e) => Application.Exit();

            _titleBar.Controls.Add(_titleIcon);
            _titleBar.Controls.Add(_titleLabel);
            _titleBar.Controls.Add(_btnMin);
            _titleBar.Controls.Add(_btnClose);

            // 2. Top Control Card (Y=52, H=128)
            _topCard = new Panel();
            _topCard.Location = new Point(14, 52);
            _topCard.Size = new Size(832, 128);
            _topCard.BackColor = Theme.CardBg;
            _topCard.Paint += TopCard_Paint;

            _cardIcon = new PictureBox();
            _cardIcon.Location = new Point(16, 16);
            _cardIcon.Size = new Size(44, 44);
            _cardIcon.SizeMode = PictureBoxSizeMode.Zoom;
            if (Icon != null) _cardIcon.Image = Icon.ToBitmap();

            _cardHeadline = new Label();
            _cardHeadline.Location = new Point(68, 16);
            _cardHeadline.Size = new Size(740, 22);
            _cardHeadline.Text = "Windows Boot Duration & Startup Impact Profiler";
            _cardHeadline.Font = Theme.HeaderFont;
            _cardHeadline.ForeColor = Theme.TextHero;

            _cardSubtitle = new Label();
            _cardSubtitle.Location = new Point(68, 38);
            _cardSubtitle.Size = new Size(740, 18);
            _cardSubtitle.Text = "Accurately measures BIOS & Event 100 boot times, detects delay culprits, and toggles startup applications.";
            _cardSubtitle.Font = Theme.SubtitleFont;
            _cardSubtitle.ForeColor = Theme.TextMuted;

            // 4 Stat Metric Tiles (Y=66, H=50)
            int tileW = 188;
            int gap = 12;

            _tile1 = CreateStatTile(16 + 0 * (tileW + gap), 64, tileW, 52, "TOTAL BOOT TIME", out _lblBootTime);
            _tile2 = CreateStatTile(16 + 1 * (tileW + gap), 64, tileW, 52, "MAIN KERNEL PATH", out _lblKernelTime);
            _tile3 = CreateStatTile(16 + 2 * (tileW + gap), 64, tileW, 52, "POST-BOOT IDLE", out _lblPostTime);
            _tile4 = CreateStatTile(16 + 3 * (tileW + gap), 64, tileW, 52, "STARTUP APPS", out _lblStartupCount);

            _topCard.Controls.Add(_cardIcon);
            _topCard.Controls.Add(_cardHeadline);
            _topCard.Controls.Add(_cardSubtitle);
            _topCard.Controls.Add(_tile1);
            _topCard.Controls.Add(_tile2);
            _topCard.Controls.Add(_tile3);
            _topCard.Controls.Add(_tile4);

            // 3. Explorer Table (Y=188, H=314)
            _listView = new ListView();
            _listView.Location = new Point(14, 188);
            _listView.Size = new Size(832, 314);
            _listView.View = View.Details;
            _listView.FullRowSelect = true;
            _listView.GridLines = false;
            _listView.BorderStyle = BorderStyle.FixedSingle;
            _listView.Font = Theme.BodyFont;
            _listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            _listView.Columns.Add("Application Name", 190);
            _listView.Columns.Add("Location", 120);
            _listView.Columns.Add("Status", 100);
            _listView.Columns.Add("Boot Impact", 120);
            _listView.Columns.Add("Target Binary Path", 280);

            // 4. Action Buttons Bar (Y=510, H=46)
            _actionBar = new Panel();
            _actionBar.Location = new Point(14, 510);
            _actionBar.Size = new Size(832, 46);

            _btnToggle = new PexorisButton();
            _btnToggle.Location = new Point(0, 5);
            _btnToggle.Size = new Size(180, 36);
            _btnToggle.Text = "⚡ Toggle Selected (On/Off)";
            _btnToggle.ButtonStyle = PexorisButtonStyle.WarningAmber;
            _btnToggle.Click += BtnToggle_Click;

            _btnRefresh = new PexorisButton();
            _btnRefresh.Location = new Point(190, 5);
            _btnRefresh.Size = new Size(155, 36);
            _btnRefresh.Text = "🔄 Diagnostic Scan";
            _btnRefresh.ButtonStyle = PexorisButtonStyle.PrimaryBlue;
            _btnRefresh.Click += (s, e) => LoadDiagnostics();

            _btnBreakdown = new PexorisButton();
            _btnBreakdown.Location = new Point(355, 5);
            _btnBreakdown.Size = new Size(150, 36);
            _btnBreakdown.Text = "📊 Event 100 Details";
            _btnBreakdown.ButtonStyle = PexorisButtonStyle.PrimaryBlue;
            _btnBreakdown.Click += BtnBreakdown_Click;

            _btnOpenFolder = new PexorisButton();
            _btnOpenFolder.Location = new Point(515, 5);
            _btnOpenFolder.Size = new Size(145, 36);
            _btnOpenFolder.Text = "📁 Startup Folder";
            _btnOpenFolder.ButtonStyle = PexorisButtonStyle.SecondaryOutline;
            _btnOpenFolder.Click += BtnOpenFolder_Click;

            _btnExport = new PexorisButton();
            _btnExport.Location = new Point(670, 5);
            _btnExport.Size = new Size(162, 36);
            _btnExport.Text = "📋 Export Report";
            _btnExport.ButtonStyle = PexorisButtonStyle.SecondaryOutline;
            _btnExport.Click += BtnExport_Click;

            _actionBar.Controls.Add(_btnToggle);
            _actionBar.Controls.Add(_btnRefresh);
            _actionBar.Controls.Add(_btnBreakdown);
            _actionBar.Controls.Add(_btnOpenFolder);
            _actionBar.Controls.Add(_btnExport);

            // 5. Footer (38px)
            _footerBar = new Panel();
            _footerBar.Location = new Point(0, 562);
            _footerBar.Size = new Size(860, 38);
            _footerBar.BackColor = Theme.CardBg;

            _lblStatus = new Label();
            _lblStatus.Location = new Point(18, 11);
            _lblStatus.Size = new Size(600, 18);
            _lblStatus.Text = "Ready • Click 'Diagnostic Scan' to audit Windows boot diagnostics";
            _lblStatus.Font = Theme.SmallFont;
            _lblStatus.ForeColor = Theme.TextMuted;

            _lnkWebsite = new LinkLabel();
            _lnkWebsite.Location = new Point(720, 11);
            _lnkWebsite.Size = new Size(125, 18);
            _lnkWebsite.Text = "pexoris.com";
            _lnkWebsite.TextAlign = ContentAlignment.TopRight;
            _lnkWebsite.Font = Theme.SmallFont;
            _lnkWebsite.LinkColor = Theme.Primary;
            _lnkWebsite.ActiveLinkColor = Theme.PrimaryHover;
            _lnkWebsite.LinkClicked += (s, e) => {
                try { Process.Start("https://pexoris.com/tools/startup-timer/"); } catch { }
            };

            _footerBar.Controls.Add(_lblStatus);
            _footerBar.Controls.Add(_lnkWebsite);

            Controls.Add(_titleBar);
            Controls.Add(_topCard);
            Controls.Add(_listView);
            Controls.Add(_actionBar);
            Controls.Add(_footerBar);
        }

        private Panel CreateStatTile(int x, int y, int w, int h, string caption, out Label valLabel)
        {
            Panel p = new Panel();
            p.Location = new Point(x, y);
            p.Size = new Size(w, h);
            p.BackColor = Theme.CanvasBg;
            p.Paint += (s, e) => {
                using (Pen pen = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, w - 1, h - 1);
                }
            };

            Label cap = new Label();
            cap.Location = new Point(8, 6);
            cap.Size = new Size(w - 16, 14);
            cap.Text = caption;
            cap.Font = new Font("Segoe UI", 7f, FontStyle.Bold);
            cap.ForeColor = Theme.TextMuted;

            valLabel = new Label();
            valLabel.Location = new Point(8, 22);
            valLabel.Size = new Size(w - 16, 24);
            valLabel.Text = "--";
            valLabel.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            valLabel.ForeColor = Theme.Primary;

            p.Controls.Add(cap);
            p.Controls.Add(valLabel);
            return p;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                SetWindowTheme(_listView.Handle, "Explorer", null);

                if (Icon != null)
                {
                    SendMessage(Handle, WM_SETICON, (IntPtr)ICON_SMALL, Icon.Handle);
                    SendMessage(Handle, WM_SETICON, (IntPtr)ICON_BIG, Icon.Handle);
                }
            }
            catch { }

            LoadDiagnostics();
        }

        private void TopCard_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Theme.BorderLight, 1f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, _topCard.Width - 1, _topCard.Height - 1);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void LoadDiagnostics()
        {
            Cursor = Cursors.WaitCursor;
            _lblStatus.Text = "Reading Event 100 diagnostics and startup entries...";

            try
            {
                _currentMetrics = StartupEngine.ReadBootMetrics();
                _startupItems = StartupEngine.ScanStartupItems();

                // Update Stat Tiles
                _lblBootTime.Text = _currentMetrics.TotalBootSeconds.ToString("0.0") + "s";
                _lblKernelTime.Text = _currentMetrics.MainPathSeconds.ToString("0.0") + "s";
                _lblPostTime.Text = _currentMetrics.PostBootSeconds.ToString("0.0") + "s";

                int enabledCount = 0;
                foreach (StartupItem it in _startupItems)
                {
                    if (it.IsEnabled) enabledCount++;
                }
                _lblStartupCount.Text = enabledCount + " of " + _startupItems.Count + " Active";

                // Populate Explorer ListView
                _listView.BeginUpdate();
                _listView.Items.Clear();

                foreach (StartupItem item in _startupItems)
                {
                    ListViewItem lvi = new ListViewItem(item.Name);
                    lvi.SubItems.Add(item.Location);
                    lvi.SubItems.Add(item.IsEnabled ? (item.FileExists ? "Active" : "Dead Target") : "Disabled");
                    lvi.SubItems.Add(item.ImpactRating);
                    lvi.SubItems.Add(item.TargetPath);
                    lvi.Tag = item;

                    if (!item.FileExists)
                    {
                        lvi.ForeColor = Theme.Destructive;
                        lvi.BackColor = Theme.DestructiveLight;
                    }
                    else if (!item.IsEnabled)
                    {
                        lvi.ForeColor = Theme.TextMuted;
                    }
                    else if (item.ImpactRating.StartsWith("High"))
                    {
                        lvi.ForeColor = Color.FromArgb(180, 83, 9);
                        lvi.BackColor = Theme.WarningLight;
                    }

                    _listView.Items.Add(lvi);
                }

                _listView.EndUpdate();

                string sourceTag = _currentMetrics.HasDiagnosticsLog ? "Diagnostics Event 100" : "Kernel UpTime Estimation";
                _lblStatus.Text = string.Format("Audited {0} startup applications • Boot Duration: {1}s ({2}) via {3}",
                    _startupItems.Count, _currentMetrics.TotalBootSeconds, _currentMetrics.SpeedRating, sourceTag);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Diagnostic Error: " + ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnToggle_Click(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select one or more startup items from the table to toggle.", "Pexoris StartupTimer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int changed = 0;
            foreach (ListViewItem lvi in _listView.SelectedItems)
            {
                StartupItem item = lvi.Tag as StartupItem;
                if (item != null)
                {
                    bool newState = !item.IsEnabled;
                    if (StartupEngine.ToggleStartupItemState(item, newState))
                    {
                        changed++;
                    }
                }
            }

            LoadDiagnostics();
            MessageBox.Show("Updated " + changed + " startup entries successfully.", "Startup Toggled", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnBreakdown_Click(object sender, EventArgs e)
        {
            if (_currentMetrics == null) return;

            string msg = string.Format(
                "=== Windows Boot Diagnostics Breakdown ===\n\n" +
                "• Total Boot Duration: {0} seconds\n" +
                "• Main Path (Kernel & Core Services): {1} seconds\n" +
                "• Post-Boot (Desktop to Idle): {2} seconds\n" +
                "• Speed Classification: {3}\n" +
                "• System Uptime: {4:N1} hours\n" +
                "• Last Boot Timestamp: {5}\n" +
                "• Diagnostics Log Source: {6}\n\n" +
                "Tip: Post-boot idle time is largely affected by auto-starting tray apps.\n" +
                "Disabling heavy startup apps can reduce boot latency by 40-70%.",
                _currentMetrics.TotalBootSeconds,
                _currentMetrics.MainPathSeconds,
                _currentMetrics.PostBootSeconds,
                _currentMetrics.SpeedRating,
                _currentMetrics.Uptime.TotalHours,
                _currentMetrics.LastBootTime.ToString("yyyy-MM-dd HH:mm:ss"),
                _currentMetrics.HasDiagnosticsLog ? "Event ID 100 Operational Log" : "Kernel UpTime Estimate");

            MessageBox.Show(msg, "Windows Boot Performance Breakdown", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open startup folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_startupItems == null || _currentMetrics == null) return;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV File (*.csv)|*.csv|Text File (*.txt)|*.txt";
                sfd.FileName = "Pexoris_Boot_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string csv = StartupEngine.ExportReportToCsv(_currentMetrics, _startupItems);
                        File.WriteAllText(sfd.FileName, csv);
                        MessageBox.Show("Boot report saved successfully to:\n" + sfd.FileName, "Report Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving report: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
