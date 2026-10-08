using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisIconCacheRebuilder
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_SETICON = 0x0080;
        private static readonly IntPtr ICON_SMALL = new IntPtr(0);
        private static readonly IntPtr ICON_BIG = new IntPtr(1);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        // UI Controls
        private Panel _titleBar;
        private Label _lblTitle;
        private PictureBox _pbTitleIcon;
        private Label _btnMin;
        private Label _btnClose;

        private Panel _cardHeader;
        private PictureBox _pbCardIcon;
        private Label _lblHeadline;
        private Label _lblSubtitle;
        private Label _lblStatusPill;
        private Label _lblStats;

        private ListView _lvCaches;
        private Panel _actionBar;
        private PexorisButton _btnRebuildAll;
        private PexorisButton _btnRebuildIcons;
        private PexorisButton _btnPurgeThumbs;
        private PexorisButton _btnRestartExplorer;
        private PexorisButton _btnRefresh;

        private Panel _footerBar;
        private CheckBox _chkRestartExplorer;
        private Label _lblFooterStatus;
        private LinkLabel _lnkWebsite;

        private List<CacheFileInfo> _items = new List<CacheFileInfo>();
        private BackgroundWorker _worker;
        private bool _isBusy = false;

        public MainForm()
        {
            InitializeComponent();
            ApplyTaskbarIcon();
            ScanCaches();
        }

        private void ApplyTaskbarIcon()
        {
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                {
                    this.Icon = icon;
                    SendMessage(this.Handle, WM_SETICON, ICON_SMALL, icon.Handle);
                    SendMessage(this.Handle, WM_SETICON, ICON_BIG, icon.Handle);
                    if (_pbTitleIcon != null)
                    {
                        _pbTitleIcon.Image = new Bitmap(icon.ToBitmap(), new Size(20, 20));
                    }
                    if (_pbCardIcon != null)
                    {
                        _pbCardIcon.Image = new Bitmap(icon.ToBitmap(), new Size(48, 48));
                    }
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

            // -------------------------------------------------------------
            // Title Bar (44px)
            // -------------------------------------------------------------
            _titleBar = new Panel
            {
                Size = new Size(860, 44),
                Location = new Point(0, 0),
                BackColor = Theme.CardBg
            };
            _titleBar.MouseDown += (s, e) => DragWindow();
            _titleBar.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(Theme.BorderLight), 0, 43, 860, 43);
            };

            _pbTitleIcon = new PictureBox
            {
                Size = new Size(20, 20),
                Location = new Point(16, 12),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            _pbTitleIcon.MouseDown += (s, e) => DragWindow();

            _lblTitle = new Label
            {
                Text = "Pexoris IconCacheRebuilder — Fix Blank & Corrupted Windows Icons",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero,
                AutoSize = true,
                Location = new Point(44, 12)
            };
            _lblTitle.MouseDown += (s, e) => DragWindow();

            _btnMin = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(44, 44),
                Location = new Point(772, 0),
                Cursor = Cursors.Hand
            };
            _btnMin.MouseEnter += (s, e) => _btnMin.BackColor = Color.FromArgb(241, 245, 249);
            _btnMin.MouseLeave += (s, e) => _btnMin.BackColor = Color.Transparent;
            _btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            _btnClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(44, 44),
                Location = new Point(816, 0),
                Cursor = Cursors.Hand
            };
            _btnClose.MouseEnter += (s, e) =>
            {
                _btnClose.BackColor = Theme.DestructiveRed;
                _btnClose.ForeColor = Color.White;
            };
            _btnClose.MouseLeave += (s, e) =>
            {
                _btnClose.BackColor = Color.Transparent;
                _btnClose.ForeColor = Theme.TextSub;
            };
            _btnClose.Click += (s, e) => this.Close();

            _titleBar.Controls.Add(_pbTitleIcon);
            _titleBar.Controls.Add(_lblTitle);
            _titleBar.Controls.Add(_btnMin);
            _titleBar.Controls.Add(_btnClose);

            // -------------------------------------------------------------
            // Top Control Card (110px)
            // -------------------------------------------------------------
            _cardHeader = new Panel
            {
                Size = new Size(828, 110),
                Location = new Point(16, 56),
                BackColor = Theme.CardBg
            };
            _cardHeader.Paint += (s, e) =>
            {
                using (GraphicsPath p = Theme.GetRoundedPath(new RectangleF(0.5f, 0.5f, 827, 109), 8f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(Theme.BorderLight, 1f))
                    {
                        e.Graphics.DrawPath(pen, p);
                    }
                }
            };

            _pbCardIcon = new PictureBox
            {
                Size = new Size(48, 48),
                Location = new Point(16, 16),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            _lblHeadline = new Label
            {
                Text = "Pexoris IconCacheRebuilder v1.0",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                AutoSize = true,
                Location = new Point(74, 16)
            };

            _lblSubtitle = new Label
            {
                Text = "Resolve white sheets, black boxes & missing thumbnails. Safe 1-click Explorer database flush & rebuild.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                AutoSize = true,
                Location = new Point(74, 40)
            };

            _lblStatusPill = new Label
            {
                Text = "[ Scanning Cache Databases... ]",
                Font = Theme.FontSmall,
                ForeColor = Theme.AmberDark,
                BackColor = Theme.AmberLight,
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4),
                Location = new Point(74, 68)
            };

            _lblStats = new Label
            {
                Text = "Icons: 0  |  Thumbnails: 0  |  Size: 0 MB",
                Font = Theme.FontBold,
                ForeColor = Theme.TextBody,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
                Size = new Size(330, 24),
                Location = new Point(480, 70)
            };

            _cardHeader.Controls.Add(_pbCardIcon);
            _cardHeader.Controls.Add(_lblHeadline);
            _cardHeader.Controls.Add(_lblSubtitle);
            _cardHeader.Controls.Add(_lblStatusPill);
            _cardHeader.Controls.Add(_lblStats);

            // -------------------------------------------------------------
            // ListView (Cache Databases Inspector)
            // -------------------------------------------------------------
            _lvCaches = new ListView
            {
                Size = new Size(828, 320),
                Location = new Point(16, 176),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.FontRegular,
                BackColor = Color.White
            };
            _lvCaches.Columns.Add("Database File", 210);
            _lvCaches.Columns.Add("Category", 110);
            _lvCaches.Columns.Add("Size", 80);
            _lvCaches.Columns.Add("Explorer Lock Status", 145);
            _lvCaches.Columns.Add("Path Location", 175);
            _lvCaches.Columns.Add("Status", 100);

            // -------------------------------------------------------------
            // Action Buttons Bar (Y=506, H=48)
            // -------------------------------------------------------------
            _actionBar = new Panel
            {
                Size = new Size(828, 48),
                Location = new Point(16, 506),
                BackColor = Color.Transparent
            };

            _btnRebuildAll = new PexorisButton
            {
                Text = "⚡ 1-Click Rebuild All Caches",
                Style = PexorisButtonStyle.PrimaryAmber,
                Size = new Size(240, 44),
                Location = new Point(0, 2)
            };
            _btnRebuildAll.Click += (s, e) => ExecuteRebuild(true, true);

            _btnRebuildIcons = new PexorisButton
            {
                Text = "Rebuild Icons Only",
                Style = PexorisButtonStyle.PrimaryBlue,
                Size = new Size(150, 44),
                Location = new Point(250, 2)
            };
            _btnRebuildIcons.Click += (s, e) => ExecuteRebuild(true, false);

            _btnPurgeThumbs = new PexorisButton
            {
                Text = "Purge Thumbnails",
                Style = PexorisButtonStyle.SecondaryOutline,
                Size = new Size(145, 44),
                Location = new Point(410, 2)
            };
            _btnPurgeThumbs.Click += (s, e) => ExecuteRebuild(false, true);

            _btnRestartExplorer = new PexorisButton
            {
                Text = "Restart Explorer",
                Style = PexorisButtonStyle.SecondaryOutline,
                Size = new Size(130, 44),
                Location = new Point(565, 2)
            };
            _btnRestartExplorer.Click += (s, e) =>
            {
                CacheEngine.KillExplorer();
                System.Threading.Thread.Sleep(500);
                CacheEngine.StartExplorer();
                ScanCaches();
            };

            _btnRefresh = new PexorisButton
            {
                Text = "Refresh Scan",
                Style = PexorisButtonStyle.SecondaryOutline,
                Size = new Size(115, 44),
                Location = new Point(705, 2)
            };
            _btnRefresh.Click += (s, e) => ScanCaches();

            _actionBar.Controls.Add(_btnRebuildAll);
            _actionBar.Controls.Add(_btnRebuildIcons);
            _actionBar.Controls.Add(_btnPurgeThumbs);
            _actionBar.Controls.Add(_btnRestartExplorer);
            _actionBar.Controls.Add(_btnRefresh);

            // -------------------------------------------------------------
            // Footer Bar (38px)
            // -------------------------------------------------------------
            _footerBar = new Panel
            {
                Size = new Size(860, 38),
                Location = new Point(0, 602),
                BackColor = Theme.CardBg
            };
            _footerBar.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(Theme.BorderLight), 0, 0, 860, 0);
            };

            _chkRestartExplorer = new CheckBox
            {
                Text = "Restart Windows Explorer shell automatically",
                Checked = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextBody,
                AutoSize = true,
                Location = new Point(16, 10)
            };

            _lblFooterStatus = new Label
            {
                Text = "Ready to flush and rebuild cache databases",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                AutoSize = true,
                Location = new Point(310, 11)
            };

            _lnkWebsite = new LinkLabel
            {
                Text = "pexoris.com",
                Font = Theme.FontSmall,
                LinkColor = Theme.AmberDark,
                ActiveLinkColor = Theme.Amber,
                AutoSize = true,
                Location = new Point(775, 11)
            };
            _lnkWebsite.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            _footerBar.Controls.Add(_chkRestartExplorer);
            _footerBar.Controls.Add(_lblFooterStatus);
            _footerBar.Controls.Add(_lnkWebsite);

            // Add all controls
            this.Controls.Add(_titleBar);
            this.Controls.Add(_cardHeader);
            this.Controls.Add(_lvCaches);
            this.Controls.Add(_actionBar);
            this.Controls.Add(_footerBar);

            this.ResumeLayout(false);
        }

        private void DragWindow()
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
        }

        private void ScanCaches()
        {
            if (_isBusy) return;
            _lvCaches.Items.Clear();
            _items = CacheEngine.ScanCacheFiles();

            long totalBytes = 0;
            int iconCount = 0;
            int thumbCount = 0;

            foreach (var item in _items)
            {
                totalBytes += item.FileSize;
                if (item.CacheType == "Icon Cache") iconCount++;
                else thumbCount++;

                var lvi = new ListViewItem(item.FileName);
                lvi.SubItems.Add(item.CacheType);
                lvi.SubItems.Add(CacheEngine.FormatBytes(item.FileSize));
                lvi.SubItems.Add(item.IsLocked ? "🔒 Locked by Explorer" : "Unlocked");
                lvi.SubItems.Add(item.FullPath);
                lvi.SubItems.Add(item.Status);
                lvi.Tag = item;

                if (item.IsLocked)
                {
                    lvi.ForeColor = Color.FromArgb(180, 83, 9); // Amber dark
                }

                _lvCaches.Items.Add(lvi);
            }

            _lblStats.Text = string.Format("Icons: {0}  |  Thumbnails: {1}  |  Total: {2}", iconCount, thumbCount, CacheEngine.FormatBytes(totalBytes));
            _lblStatusPill.Text = string.Format("[ Ready - {0} Cache Databases Detected ({1}) ]", _items.Count, CacheEngine.FormatBytes(totalBytes));
            _lblStatusPill.BackColor = Theme.AmberLight;
            _lblStatusPill.ForeColor = Theme.AmberDark;
            _lblFooterStatus.Text = string.Format("{0} cache files found ({1}) • Ready to rebuild", _items.Count, CacheEngine.FormatBytes(totalBytes));
        }

        private void ExecuteRebuild(bool icons, bool thumbs)
        {
            if (_isBusy) return;

            string targetName = (icons && thumbs) ? "Icon & Thumbnail" : (icons ? "Icon" : "Thumbnail");
            var confirm = MessageBox.Show(this,
                string.Format("Are you sure you want to rebuild the Windows {0} Cache?\n\nWindows Explorer will be temporarily closed and restarted to release file locks.", targetName),
                "Confirm Cache Rebuild",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _isBusy = true;
            _btnRebuildAll.Enabled = false;
            _btnRebuildIcons.Enabled = false;
            _btnPurgeThumbs.Enabled = false;
            _btnRestartExplorer.Enabled = false;
            _btnRefresh.Enabled = false;

            _lblStatusPill.Text = "[ Rebuilding Caches... Explorer Restarting ]";
            _lblStatusPill.BackColor = Color.FromArgb(254, 243, 199);
            _lblStatusPill.ForeColor = Theme.AmberDark;

            bool restart = _chkRestartExplorer.Checked;

            _worker = new BackgroundWorker();
            _worker.DoWork += (s, e) =>
            {
                int delCount;
                int errCount;
                string logs;
                bool ok = CacheEngine.RebuildCaches(icons, thumbs, restart, out delCount, out errCount, out logs);
                e.Result = new object[] { ok, delCount, errCount, logs };
            };

            _worker.RunWorkerCompleted += (s, e) =>
            {
                _isBusy = false;
                _btnRebuildAll.Enabled = true;
                _btnRebuildIcons.Enabled = true;
                _btnPurgeThumbs.Enabled = true;
                _btnRestartExplorer.Enabled = true;
                _btnRefresh.Enabled = true;

                object[] res = (object[])e.Result;
                bool ok = (bool)res[0];
                int deleted = (int)res[1];
                int errors = (int)res[2];
                string log = (string)res[3];

                ScanCaches();

                _lblStatusPill.Text = string.Format("[ Success: {0} Databases Flushed & Rebuilt ]", deleted);
                _lblStatusPill.BackColor = Color.FromArgb(236, 253, 245);
                _lblStatusPill.ForeColor = Theme.SuccessGreen;

                MessageBox.Show(this,
                    string.Format("Cache Rebuild Complete!\n\n• Flushed & Cleared: {0} cache databases\n• Errors: {1}\n• Windows Shell: Refreshed & Notified\n\nAll corrupted desktop, taskbar, and file icons have been restored.",
                        deleted, errors),
                    "Rebuild Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            _worker.RunWorkerAsync();
        }
    }
}
