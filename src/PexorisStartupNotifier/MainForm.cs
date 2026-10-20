using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisStartupNotifier
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
        private Label _pillStatus;

        // Stat boxes inside Top Card
        private Label _lblStatTotal;
        private Label _lblStatRegistry;
        private Label _lblStatFolders;
        private Label _lblStatInjections;

        private ListView _listView;
        private Panel _actionBar;
        private PexorisButton _btnPurge;
        private PexorisButton _btnToggleGuard;
        private PexorisButton _btnRefresh;
        private PexorisButton _btnOpen;
        private PexorisButton _btnExport;

        private Panel _footerBar;
        private Label _lblStatus;
        private LinkLabel _lnkWebsite;

        private List<StartupItem> _currentItems = new List<StartupItem>();
        private int _injectionsBlockedCount = 0;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(860, 640);
            BackColor = Theme.CanvasBg;
            Text = "Pexoris StartupNotifier";

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
            _titleLabel.Size = new Size(540, 20);
            _titleLabel.Text = "Pexoris StartupNotifier • Real-Time Startup Modification Monitor";
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
            _btnClose.Click += (s, e) => {
                StartupWatcherEngine.StopMonitoring();
                Application.Exit();
            };

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
            _cardHeadline.Location = new Point(68, 14);
            _cardHeadline.Size = new Size(480, 22);
            _cardHeadline.Text = "Real-Time Startup Modification Monitor";
            _cardHeadline.Font = Theme.HeaderFont;
            _cardHeadline.ForeColor = Theme.TextHero;

            _cardSubtitle = new Label();
            _cardSubtitle.Location = new Point(68, 36);
            _cardSubtitle.Size = new Size(540, 18);
            _cardSubtitle.Text = "Zero-footprint watcher alerting instantly when new software injects auto-start registry hooks.";
            _cardSubtitle.Font = Theme.SubtitleFont;
            _cardSubtitle.ForeColor = Theme.TextMuted;

            _pillStatus = new Label();
            _pillStatus.Location = new Point(610, 16);
            _pillStatus.Size = new Size(206, 26);
            _pillStatus.TextAlign = ContentAlignment.MiddleCenter;
            _pillStatus.Font = Theme.BodyBold;
            _pillStatus.BackColor = Theme.SuccessLight;
            _pillStatus.ForeColor = Theme.Success;
            _pillStatus.Text = "● Guard Active (7 Roots)";
            _pillStatus.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.Success, 1f))
                    e.Graphics.DrawRectangle(p, 0, 0, _pillStatus.Width - 1, _pillStatus.Height - 1);
            };

            // Stat tiles row (Y=66)
            _lblStatTotal = CreateStatTile(16, 66, 192, 48, "Total Startup Apps", "0");
            _lblStatRegistry = CreateStatTile(218, 66, 192, 48, "Registry Run Keys", "0");
            _lblStatFolders = CreateStatTile(420, 66, 192, 48, "Startup Folders", "0");
            _lblStatInjections = CreateStatTile(622, 66, 194, 48, "Injections Detected", "0");

            _topCard.Controls.Add(_cardIcon);
            _topCard.Controls.Add(_cardHeadline);
            _topCard.Controls.Add(_cardSubtitle);
            _topCard.Controls.Add(_pillStatus);
            _topCard.Controls.Add(_lblStatTotal);
            _topCard.Controls.Add(_lblStatRegistry);
            _topCard.Controls.Add(_lblStatFolders);
            _topCard.Controls.Add(_lblStatInjections);

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
            _listView.Columns.Add("Scope", 90);
            _listView.Columns.Add("Location", 140);
            _listView.Columns.Add("Target Command / Path", 325);
            _listView.Columns.Add("Target File", 85);

            // 4. Action Buttons Bar (Y=510, H=46)
            _actionBar = new Panel();
            _actionBar.Location = new Point(14, 510);
            _actionBar.Size = new Size(832, 46);

            _btnPurge = new PexorisButton();
            _btnPurge.Location = new Point(0, 5);
            _btnPurge.Size = new Size(185, 36);
            _btnPurge.Text = "⚡ Purge Selected Entry";
            _btnPurge.ButtonStyle = PexorisButtonStyle.DestructiveRed;
            _btnPurge.Click += BtnPurge_Click;

            _btnToggleGuard = new PexorisButton();
            _btnToggleGuard.Location = new Point(195, 5);
            _btnToggleGuard.Size = new Size(160, 36);
            _btnToggleGuard.Text = "🛡️ Pause Guard";
            _btnToggleGuard.ButtonStyle = PexorisButtonStyle.PrimaryIndigo;
            _btnToggleGuard.Click += BtnToggleGuard_Click;

            _btnRefresh = new PexorisButton();
            _btnRefresh.Location = new Point(365, 5);
            _btnRefresh.Size = new Size(140, 36);
            _btnRefresh.Text = "🔄 Rescan All";
            _btnRefresh.ButtonStyle = PexorisButtonStyle.PrimaryIndigo;
            _btnRefresh.Click += (s, e) => RefreshList();

            _btnOpen = new PexorisButton();
            _btnOpen.Location = new Point(515, 5);
            _btnOpen.Size = new Size(145, 36);
            _btnOpen.Text = "📁 Open Target";
            _btnOpen.ButtonStyle = PexorisButtonStyle.SecondaryOutline;
            _btnOpen.Click += BtnOpen_Click;

            _btnExport = new PexorisButton();
            _btnExport.Location = new Point(670, 5);
            _btnExport.Size = new Size(162, 36);
            _btnExport.Text = "📋 Export CSV Audit";
            _btnExport.ButtonStyle = PexorisButtonStyle.SecondaryOutline;
            _btnExport.Click += BtnExport_Click;

            _actionBar.Controls.Add(_btnPurge);
            _actionBar.Controls.Add(_btnToggleGuard);
            _actionBar.Controls.Add(_btnRefresh);
            _actionBar.Controls.Add(_btnOpen);
            _actionBar.Controls.Add(_btnExport);

            // 5. Footer (38px)
            _footerBar = new Panel();
            _footerBar.Location = new Point(0, 562);
            _footerBar.Size = new Size(860, 38);
            _footerBar.BackColor = Theme.CardBg;

            _lblStatus = new Label();
            _lblStatus.Location = new Point(18, 11);
            _lblStatus.Size = new Size(600, 18);
            _lblStatus.Text = "Ready • Watching HKCU/HKLM Run keys and Start Menu folders";
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
                try { Process.Start("https://pexoris.com/tools/startup-notifier/"); } catch { }
            };

            _footerBar.Controls.Add(_lblStatus);
            _footerBar.Controls.Add(_lnkWebsite);

            Controls.Add(_titleBar);
            Controls.Add(_topCard);
            Controls.Add(_listView);
            Controls.Add(_actionBar);
            Controls.Add(_footerBar);

            // Hook up engine events
            StartupWatcherEngine.OnNewStartupDetected += Engine_OnNewStartupDetected;
        }

        private Label CreateStatTile(int x, int y, int w, int h, string title, string initialVal)
        {
            Label lbl = new Label();
            lbl.Location = new Point(x, y);
            lbl.Size = new Size(w, h);
            lbl.BackColor = Color.FromArgb(248, 250, 252);
            lbl.Text = initialVal + "\n" + title;
            lbl.Font = Theme.HeaderFont;
            lbl.ForeColor = Theme.TextHero;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                    e.Graphics.DrawRectangle(p, 0, 0, w - 1, h - 1);
            };
            return lbl;
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

            RefreshList();
            StartupWatcherEngine.StartMonitoring();
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

        private void RefreshList()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                _currentItems = StartupWatcherEngine.ScanAllStartupItems();

                _listView.BeginUpdate();
                _listView.Items.Clear();

                int regCount = 0;
                int folderCount = 0;

                foreach (StartupItem item in _currentItems)
                {
                    ListViewItem lvi = new ListViewItem(item.Name);
                    lvi.SubItems.Add(item.Scope);
                    lvi.SubItems.Add(item.LocationDisplay);
                    lvi.SubItems.Add(item.Command);
                    lvi.SubItems.Add(item.TargetExists ? "Found" : "Missing");
                    lvi.Tag = item;

                    if (!item.TargetExists)
                    {
                        lvi.ForeColor = Color.FromArgb(180, 83, 9);
                        lvi.BackColor = Theme.WarningLight;
                    }

                    if (item.LocationType == StartupLocationType.FolderUserStartup ||
                        item.LocationType == StartupLocationType.FolderCommonStartup)
                    {
                        folderCount++;
                    }
                    else
                    {
                        regCount++;
                    }

                    _listView.Items.Add(lvi);
                }

                _listView.EndUpdate();

                _lblStatTotal.Text = _currentItems.Count + "\nTotal Startup Apps";
                _lblStatRegistry.Text = regCount + "\nRegistry Run Keys";
                _lblStatFolders.Text = folderCount + "\nStartup Folders";
                _lblStatInjections.Text = _injectionsBlockedCount + "\nInjections Detected";

                _lblStatus.Text = string.Format("Active • Monitored {0} startup entries across 7 system locations", _currentItems.Count);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Scan error: " + ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void Engine_OnNewStartupDetected(object sender, NewStartupEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, NewStartupEventArgs>(Engine_OnNewStartupDetected), sender, e);
                return;
            }

            _injectionsBlockedCount++;
            RefreshList();

            // Bring window to front
            WindowState = FormWindowState.Normal;
            Activate();

            StartupItem it = e.Item;
            DialogResult dr = MessageBox.Show(
                "⚡ NEW STARTUP ENTRY DETECTED!\n\n" +
                "Application: " + it.Name + "\n" +
                "Location: " + it.LocationDisplay + "\n" +
                "Command: " + it.Command + "\n\n" +
                "Do you want to BLOCK and PURGE this startup entry immediately?",
                "Pexoris StartupNotifier Alert",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dr == DialogResult.Yes)
            {
                if (StartupWatcherEngine.DeleteStartupItem(it))
                {
                    MessageBox.Show("Startup injection successfully purged:\n" + it.Name, "Pexoris Guard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RefreshList();
                }
            }
        }

        private void BtnToggleGuard_Click(object sender, EventArgs e)
        {
            if (StartupWatcherEngine.IsWatching)
            {
                StartupWatcherEngine.StopMonitoring();
                _btnToggleGuard.Text = "🛡️ Resume Guard";
                _pillStatus.Text = "○ Guard Paused";
                _pillStatus.BackColor = Theme.WarningLight;
                _pillStatus.ForeColor = Theme.Warning;
                _lblStatus.Text = "Real-time background monitor paused by user.";
            }
            else
            {
                StartupWatcherEngine.StartMonitoring();
                _btnToggleGuard.Text = "🛡️ Pause Guard";
                _pillStatus.Text = "● Guard Active (7 Roots)";
                _pillStatus.BackColor = Theme.SuccessLight;
                _pillStatus.ForeColor = Theme.Success;
                _lblStatus.Text = "Real-time monitor resumed • Watching all registry & startup folder hooks";
            }
            _pillStatus.Invalidate();
        }

        private void BtnPurge_Click(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a startup item from the list to purge.", "Pexoris StartupNotifier", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            StartupItem it = _listView.SelectedItems[0].Tag as StartupItem;
            if (it == null) return;

            DialogResult dr = MessageBox.Show(
                "Are you sure you want to remove this startup entry?\n\n" +
                "App: " + it.Name + "\n" +
                "Location: " + it.LocationDisplay + "\n" +
                "Command: " + it.Command,
                "Confirm Startup Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                if (StartupWatcherEngine.DeleteStartupItem(it))
                {
                    MessageBox.Show("Startup item deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RefreshList();
                }
                else
                {
                    MessageBox.Show("Failed to delete startup item. Verify administrative privileges.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;
            StartupItem it = _listView.SelectedItems[0].Tag as StartupItem;
            if (it == null) return;

            try
            {
                if (it.LocationType == StartupLocationType.FolderUserStartup ||
                    it.LocationType == StartupLocationType.FolderCommonStartup)
                {
                    Process.Start("explorer.exe", it.LocationPath);
                }
                else
                {
                    string target = it.Command.Trim('\"');
                    int sp = target.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                    if (sp > 0) target = target.Substring(0, sp + 4);

                    if (File.Exists(target))
                    {
                        Process.Start("explorer.exe", "/select,\"" + target + "\"");
                    }
                    else
                    {
                        Process.Start("regedit.exe");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open target location: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_currentItems == null || _currentItems.Count == 0) return;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV File (*.csv)|*.csv|Text File (*.txt)|*.txt";
                sfd.FileName = "Pexoris_Startup_Audit_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string csv = StartupWatcherEngine.ExportToCsv(_currentItems);
                        File.WriteAllText(sfd.FileName, csv);
                        MessageBox.Show("Startup audit report saved successfully to:\n" + sfd.FileName, "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
