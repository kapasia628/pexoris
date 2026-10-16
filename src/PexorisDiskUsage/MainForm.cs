using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisDiskUsage
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

        private ComboBox _cboDrives;
        private PexorisButton _btnBrowse;
        private Panel _usageBar;
        private Label _lblDriveStats;

        private ListView _listView;
        private Panel _actionBar;
        private PexorisButton _btnRecycle;
        private PexorisButton _btnScan;
        private PexorisButton _btnFilterLarge;
        private PexorisButton _btnOpen;
        private PexorisButton _btnExport;

        private Panel _footerBar;
        private Label _lblStatus;
        private LinkLabel _lnkWebsite;

        private string _currentScanPath;
        private long _currentTotalBytes;
        private int _currentFileCount;
        private List<DiskItemInfo> _currentItems;
        private List<DriveInfo> _readyDrives;

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
            Text = "Pexoris DiskUsage";

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
            _titleLabel.Text = "Pexoris DiskUsage • Interactive Disk Treemap & Space Reclaim";
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
            _cardHeadline.Text = "Interactive Disk Treemap & Space Reclaim";
            _cardHeadline.Font = Theme.HeaderFont;
            _cardHeadline.ForeColor = Theme.TextHero;

            _cardSubtitle = new Label();
            _cardSubtitle.Location = new Point(68, 38);
            _cardSubtitle.Size = new Size(740, 18);
            _cardSubtitle.Text = "Rapidly analyze folder storage distribution, locate massive gigabyte files, and reclaim free space.";
            _cardSubtitle.Font = Theme.SubtitleFont;
            _cardSubtitle.ForeColor = Theme.TextMuted;

            // Drive Selector Row (Y=66)
            _cboDrives = new ComboBox();
            _cboDrives.Location = new Point(16, 68);
            _cboDrives.Size = new Size(220, 26);
            _cboDrives.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboDrives.Font = Theme.BodyBold;
            _cboDrives.SelectedIndexChanged += CboDrives_SelectedIndexChanged;

            _btnBrowse = new PexorisButton();
            _btnBrowse.Location = new Point(244, 66);
            _btnBrowse.Size = new Size(130, 28);
            _btnBrowse.Text = "📂 Select Folder";
            _btnBrowse.ButtonStyle = PexorisButtonStyle.SecondaryOutline;
            _btnBrowse.Click += BtnBrowse_Click;

            _lblDriveStats = new Label();
            _lblDriveStats.Location = new Point(386, 71);
            _lblDriveStats.Size = new Size(430, 20);
            _lblDriveStats.Text = "Drive Status: Ready";
            _lblDriveStats.Font = Theme.BodyFont;
            _lblDriveStats.ForeColor = Theme.TextMuted;
            _lblDriveStats.TextAlign = ContentAlignment.MiddleRight;

            // Usage Bar (Y=100, H=14)
            _usageBar = new Panel();
            _usageBar.Location = new Point(16, 102);
            _usageBar.Size = new Size(800, 14);
            _usageBar.BackColor = Color.FromArgb(226, 232, 240);
            _usageBar.Paint += UsageBar_Paint;

            _topCard.Controls.Add(_cardIcon);
            _topCard.Controls.Add(_cardHeadline);
            _topCard.Controls.Add(_cardSubtitle);
            _topCard.Controls.Add(_cboDrives);
            _topCard.Controls.Add(_btnBrowse);
            _topCard.Controls.Add(_lblDriveStats);
            _topCard.Controls.Add(_usageBar);

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
            _listView.DoubleClick += ListView_DoubleClick;

            _listView.Columns.Add("Folder / Item Name", 210);
            _listView.Columns.Add("Size", 110);
            _listView.Columns.Add("% of Scanned", 100);
            _listView.Columns.Add("Type", 70);
            _listView.Columns.Add("Files", 70);
            _listView.Columns.Add("Full Path", 270);

            // 4. Action Buttons Bar (Y=510, H=46)
            _actionBar = new Panel();
            _actionBar.Location = new Point(14, 510);
            _actionBar.Size = new Size(832, 46);

            _btnRecycle = new PexorisButton();
            _btnRecycle.Location = new Point(0, 5);
            _btnRecycle.Size = new Size(185, 36);
            _btnRecycle.Text = "⚡ Recycle Selected";
            _btnRecycle.ButtonStyle = PexorisButtonStyle.DestructiveRed;
            _btnRecycle.Click += BtnRecycle_Click;

            _btnScan = new PexorisButton();
            _btnScan.Location = new Point(195, 5);
            _btnScan.Size = new Size(150, 36);
            _btnScan.Text = "📊 Rescan Target";
            _btnScan.ButtonStyle = PexorisButtonStyle.PrimaryTeal;
            _btnScan.Click += (s, e) => StartScan(_currentScanPath);

            _btnFilterLarge = new PexorisButton();
            _btnFilterLarge.Location = new Point(355, 5);
            _btnFilterLarge.Size = new Size(150, 36);
            _btnFilterLarge.Text = "🔍 Files > 500 MB";
            _btnFilterLarge.ButtonStyle = PexorisButtonStyle.PrimaryTeal;
            _btnFilterLarge.Click += BtnFilterLarge_Click;

            _btnOpen = new PexorisButton();
            _btnOpen.Location = new Point(515, 5);
            _btnOpen.Size = new Size(145, 36);
            _btnOpen.Text = "📁 Open in Explorer";
            _btnOpen.ButtonStyle = PexorisButtonStyle.SecondaryOutline;
            _btnOpen.Click += BtnOpen_Click;

            _btnExport = new PexorisButton();
            _btnExport.Location = new Point(670, 5);
            _btnExport.Size = new Size(162, 36);
            _btnExport.Text = "📋 Export CSV Map";
            _btnExport.ButtonStyle = PexorisButtonStyle.SecondaryOutline;
            _btnExport.Click += BtnExport_Click;

            _actionBar.Controls.Add(_btnRecycle);
            _actionBar.Controls.Add(_btnScan);
            _actionBar.Controls.Add(_btnFilterLarge);
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
            _lblStatus.Text = "Ready • Double-click any folder to drill down into subdirectories";
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
                try { Process.Start("https://pexoris.com/tools/disk-usage/"); } catch { }
            };

            _footerBar.Controls.Add(_lblStatus);
            _footerBar.Controls.Add(_lnkWebsite);

            Controls.Add(_titleBar);
            Controls.Add(_topCard);
            Controls.Add(_listView);
            Controls.Add(_actionBar);
            Controls.Add(_footerBar);
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

            PopulateDrives();
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

        private void PopulateDrives()
        {
            _cboDrives.Items.Clear();
            _readyDrives = DiskEngine.GetFixedDrives();

            foreach (DriveInfo d in _readyDrives)
            {
                string label = string.IsNullOrEmpty(d.VolumeLabel) ? "Local Disk" : d.VolumeLabel;
                string totalStr = DiskItemInfo.FormatBytes(d.TotalSize);
                string freeStr = DiskItemInfo.FormatBytes(d.TotalFreeSpace);
                _cboDrives.Items.Add(string.Format("{0} ({1}) - {2} free of {3}", d.Name, label, freeStr, totalStr));
            }

            if (_cboDrives.Items.Count > 0)
            {
                _cboDrives.SelectedIndex = 0;
            }
        }

        private void CboDrives_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cboDrives.SelectedIndex >= 0 && _cboDrives.SelectedIndex < _readyDrives.Count)
            {
                DriveInfo d = _readyDrives[_cboDrives.SelectedIndex];
                _currentScanPath = d.RootDirectory.FullName;
                UpdateDriveStats(d);
                StartScan(_currentScanPath);
            }
        }

        private void UpdateDriveStats(DriveInfo d)
        {
            long used = d.TotalSize - d.TotalFreeSpace;
            double usedPct = Math.Round((used / (double)d.TotalSize) * 100.0, 1);

            _lblDriveStats.Text = string.Format("Capacity: {0} • Used: {1} ({2}%) • Free: {3}",
                DiskItemInfo.FormatBytes(d.TotalSize),
                DiskItemInfo.FormatBytes(used),
                usedPct,
                DiskItemInfo.FormatBytes(d.TotalFreeSpace));

            _usageBar.Invalidate();
        }

        private void UsageBar_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = _usageBar.Width;
            int h = _usageBar.Height;

            if (_cboDrives.SelectedIndex >= 0 && _cboDrives.SelectedIndex < _readyDrives.Count)
            {
                DriveInfo d = _readyDrives[_cboDrives.SelectedIndex];
                long used = d.TotalSize - d.TotalFreeSpace;
                float pct = (float)(used / (double)d.TotalSize);
                if (pct > 1.0f) pct = 1.0f;

                int fillW = (int)(w * pct);

                Color fillCol = pct > 0.9f ? Theme.Destructive : (pct > 0.75f ? Theme.Warning : Theme.Primary);

                using (SolidBrush b = new SolidBrush(fillCol))
                {
                    g.FillRectangle(b, 0, 0, fillW, h);
                }
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select a custom directory to analyze with Pexoris DiskUsage";
                fbd.ShowNewFolderButton = false;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _currentScanPath = fbd.SelectedPath;
                    _lblDriveStats.Text = "Custom Scan: " + _currentScanPath;
                    StartScan(_currentScanPath);
                }
            }
        }

        private void StartScan(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            Cursor = Cursors.WaitCursor;
            _lblStatus.Text = "Scanning directory contents for: " + path + "...";

            try
            {
                _currentItems = DiskEngine.ScanDirectory(path, out _currentTotalBytes, out _currentFileCount);

                _listView.BeginUpdate();
                _listView.Items.Clear();

                foreach (DiskItemInfo item in _currentItems)
                {
                    ListViewItem lvi = new ListViewItem(item.Name);
                    lvi.SubItems.Add(item.FormattedSize);
                    lvi.SubItems.Add(item.PercentageOfParent + "%");
                    lvi.SubItems.Add(item.IsDirectory ? "Folder" : "File");
                    lvi.SubItems.Add(item.FileCount.ToString("N0"));
                    lvi.SubItems.Add(item.FullPath);
                    lvi.Tag = item;

                    if (item.PercentageOfParent >= 25.0)
                    {
                        lvi.ForeColor = Color.FromArgb(180, 83, 9);
                        lvi.BackColor = Theme.WarningLight;
                    }

                    _listView.Items.Add(lvi);
                }

                _listView.EndUpdate();

                _lblStatus.Text = string.Format("Scanned {0} items ({1} files) totaling {2} in {3}",
                    _currentItems.Count, _currentFileCount, DiskItemInfo.FormatBytes(_currentTotalBytes), path);
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

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;
            DiskItemInfo item = _listView.SelectedItems[0].Tag as DiskItemInfo;
            if (item != null && item.IsDirectory)
            {
                _currentScanPath = item.FullPath;
                StartScan(_currentScanPath);
            }
        }

        private void BtnFilterLarge_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentScanPath)) return;

            Cursor = Cursors.WaitCursor;
            _lblStatus.Text = "Searching for files larger than 500 MB in " + _currentScanPath + "...";

            try
            {
                long threshold = 500L * 1024L * 1024L; // 500 MB
                List<DiskItemInfo> largeFiles = DiskEngine.FindLargeFiles(_currentScanPath, threshold, 100);

                _listView.BeginUpdate();
                _listView.Items.Clear();

                foreach (DiskItemInfo it in largeFiles)
                {
                    ListViewItem lvi = new ListViewItem(it.Name);
                    lvi.SubItems.Add(it.FormattedSize);
                    lvi.SubItems.Add("--");
                    lvi.SubItems.Add("Large File");
                    lvi.SubItems.Add("1");
                    lvi.SubItems.Add(it.FullPath);
                    lvi.Tag = it;

                    lvi.ForeColor = Theme.Destructive;
                    lvi.BackColor = Theme.DestructiveLight;

                    _listView.Items.Add(lvi);
                }

                _listView.EndUpdate();
                _lblStatus.Text = string.Format("Found {0} files exceeding 500 MB", largeFiles.Count);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Search error: " + ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnRecycle_Click(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select an item from the table to send to the Recycle Bin.", "Pexoris DiskUsage", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DiskItemInfo item = _listView.SelectedItems[0].Tag as DiskItemInfo;
            if (item == null) return;

            DialogResult dr = MessageBox.Show(
                "Are you sure you want to send this item to the Windows Recycle Bin?\n\n" +
                "Name: " + item.Name + "\n" +
                "Size: " + item.FormattedSize + "\n" +
                "Path: " + item.FullPath,
                "Confirm Recycle Bin Purge",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dr == DialogResult.Yes)
            {
                if (DiskEngine.SendToRecycleBin(item.FullPath))
                {
                    MessageBox.Show("Item safely sent to Recycle Bin:\n" + item.FullPath, "Recycle Bin Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    StartScan(_currentScanPath);
                }
                else
                {
                    MessageBox.Show("Failed to recycle item. Ensure you have file permissions and the file is not locked.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;
            DiskItemInfo item = _listView.SelectedItems[0].Tag as DiskItemInfo;
            if (item != null)
            {
                try
                {
                    if (item.IsDirectory)
                    {
                        Process.Start("explorer.exe", item.FullPath);
                    }
                    else
                    {
                        Process.Start("explorer.exe", "/select,\"" + item.FullPath + "\"");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open in Explorer: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_currentItems == null || _currentItems.Count == 0) return;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV File (*.csv)|*.csv|Text File (*.txt)|*.txt";
                sfd.FileName = "Pexoris_Disk_Audit_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string csv = DiskEngine.ExportToCsv(_currentScanPath, _currentTotalBytes, _currentFileCount, _currentItems);
                        File.WriteAllText(sfd.FileName, csv);
                        MessageBox.Show("Disk usage report saved successfully to:\n" + sfd.FileName, "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving CSV report: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
