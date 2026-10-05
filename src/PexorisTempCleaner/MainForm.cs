using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PexorisTempCleaner
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

        private PexorisButton btnPresetDeepClean;
        private PexorisButton btnPresetQuickScan;
        private PexorisButton btnPresetRecommended;

        private Panel pnlListWrapper;
        private ListView lvTargets;

        private Panel pnlActions;
        private PexorisButton btnCleanSelected;
        private PexorisButton btnScan;
        private PexorisButton btnSelectAll;
        private PexorisButton btnDeselectAll;
        private PexorisButton btnOpenFolder;

        private Panel pnlFooter;
        private CheckBox chkSkipLocked;
        private Label lblFooterStatus;
        private LinkLabel lnkBrand;

        private List<CleanTarget> _targets = new List<CleanTarget>();
        private bool _isBusy = false;
        private bool _hasScanned = false;
        private bool _updatingChecks = false;

        public MainForm()
        {
            InitializeComponent();
            LoadAppIcon();
            InitTargets();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Pexoris Temp Cleaner";
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
            pnlTitleBar.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Theme.BorderLight))
                {
                    e.Graphics.DrawLine(pen, 0, pnlTitleBar.Height - 1, pnlTitleBar.Width, pnlTitleBar.Height - 1);
                }
            };

            picTitleIcon = new PictureBox
            {
                Location = new Point(14, 10),
                Size = new Size(24, 24),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            picTitleIcon.MouseDown += TitleBar_MouseDown;

            lblTitleText = new Label
            {
                Location = new Point(46, 12),
                AutoSize = true,
                Text = "Pexoris Temp Cleaner",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };
            lblTitleText.MouseDown += TitleBar_MouseDown;

            lblTitleBadge = new Label
            {
                Location = new Point(220, 14),
                AutoSize = true,
                Text = "v1.0 • Portable",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent
            };
            lblTitleBadge.MouseDown += TitleBar_MouseDown;

            btnMinimize = new Label
            {
                Location = new Point(772, 0),
                Size = new Size(44, 44),
                Text = "—",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Theme.TextSub,
                Cursor = Cursors.Hand
            };
            btnMinimize.MouseEnter += (s, e) => { btnMinimize.BackColor = Color.FromArgb(241, 245, 249); };
            btnMinimize.MouseLeave += (s, e) => { btnMinimize.BackColor = Color.Transparent; };
            btnMinimize.Click += (s, e) => { WindowState = FormWindowState.Minimized; };

            btnClose = new Label
            {
                Location = new Point(816, 0),
                Size = new Size(44, 44),
                Text = "✕",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Theme.TextSub,
                Cursor = Cursors.Hand
            };
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Theme.DestructiveRed; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = Theme.TextSub; };
            btnClose.Click += (s, e) => { Close(); };

            pnlTitleBar.Controls.Add(picTitleIcon);
            pnlTitleBar.Controls.Add(lblTitleText);
            pnlTitleBar.Controls.Add(lblTitleBadge);
            pnlTitleBar.Controls.Add(btnMinimize);
            pnlTitleBar.Controls.Add(btnClose);

            // 2. Top Control Card (120px)
            cardTop = new Panel
            {
                Location = new Point(16, 56),
                Size = new Size(828, 120),
                BackColor = Theme.CardBg
            };
            cardTop.Paint += CardTop_Paint;

            picCardIcon = new PictureBox
            {
                Location = new Point(16, 16),
                Size = new Size(44, 44),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            lblCardTitle = new Label
            {
                Location = new Point(70, 16),
                AutoSize = true,
                Text = "Windows System & User Temp Cleaner",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            lblCardSubtitle = new Label
            {
                Location = new Point(70, 40),
                Size = new Size(500, 32),
                Text = "Deeply scan and purge accumulated temporary files, Windows Update cache, crash dumps, and prefetch safely without breaking running tasks.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            pillStatus = new Label
            {
                Location = new Point(590, 16),
                Size = new Size(222, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontBold,
                BackColor = Color.FromArgb(238, 242, 255),
                ForeColor = Theme.PrimaryBlue
            };
            pillStatus.Text = "● 0 B TO CLEAN (READY)";
            pillStatus.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(199, 210, 254)))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath p = Theme.GetRoundedPath(new RectangleF(0.5f, 0.5f, pillStatus.Width - 1f, pillStatus.Height - 1f), 12f))
                    {
                        e.Graphics.DrawPath(pen, p);
                    }
                }
            };

            // Preset Buttons inside Top Card
            btnPresetDeepClean = new PexorisButton
            {
                Location = new Point(70, 76),
                Size = new Size(175, 32),
                Text = "⚡ Deep Junk Clean",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            btnPresetDeepClean.Click += (s, e) => PresetDeepClean();

            btnPresetQuickScan = new PexorisButton
            {
                Location = new Point(253, 76),
                Size = new Size(155, 32),
                Text = "🔍 Quick Scan",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnPresetQuickScan.Click += (s, e) => StartScan(false);

            btnPresetRecommended = new PexorisButton
            {
                Location = new Point(416, 76),
                Size = new Size(185, 32),
                Text = "⚙️ Select Recommended",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnPresetRecommended.Click += (s, e) => SelectRecommended();

            cardTop.Controls.Add(picCardIcon);
            cardTop.Controls.Add(lblCardTitle);
            cardTop.Controls.Add(lblCardSubtitle);
            cardTop.Controls.Add(pillStatus);
            cardTop.Controls.Add(btnPresetDeepClean);
            cardTop.Controls.Add(btnPresetQuickScan);
            cardTop.Controls.Add(btnPresetRecommended);

            // 3. ListView Wrapper Panel (314px)
            pnlListWrapper = new Panel
            {
                Location = new Point(16, 184),
                Size = new Size(828, 314),
                BackColor = Theme.CardBg
            };
            pnlListWrapper.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Theme.BorderLight))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlListWrapper.Width - 1, pnlListWrapper.Height - 1);
                }
            };

            lvTargets = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Font = Theme.FontRegular
            };

            lvTargets.Columns.Add("Junk Category", 225);
            lvTargets.Columns.Add("Size", 100);
            lvTargets.Columns.Add("Files", 80);
            lvTargets.Columns.Add("Safety", 100);
            lvTargets.Columns.Add("Folder Path / Description", 310);

            lvTargets.ItemChecked += LvTargets_ItemChecked;

            pnlListWrapper.Controls.Add(lvTargets);

            // 4. Action Buttons Bar (Y=508, H=50)
            pnlActions = new Panel
            {
                Location = new Point(16, 508),
                Size = new Size(828, 50),
                BackColor = Color.Transparent
            };

            btnCleanSelected = new PexorisButton
            {
                Location = new Point(0, 6),
                Size = new Size(205, 38),
                Text = "⚡ Clean Selected Junk",
                Style = PexorisButtonStyle.DestructiveRed
            };
            btnCleanSelected.Click += (s, e) => StartClean();

            btnScan = new PexorisButton
            {
                Location = new Point(215, 6),
                Size = new Size(150, 38),
                Text = "🔍 Scan Disk Junk",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnScan.Click += (s, e) => StartScan(false);

            btnSelectAll = new PexorisButton
            {
                Location = new Point(375, 6),
                Size = new Size(115, 38),
                Text = "✓ Select All",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnSelectAll.Click += (s, e) => SetAllSelection(true);

            btnDeselectAll = new PexorisButton
            {
                Location = new Point(498, 6),
                Size = new Size(125, 38),
                Text = "✕ Deselect All",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnDeselectAll.Click += (s, e) => SetAllSelection(false);

            btnOpenFolder = new PexorisButton
            {
                Location = new Point(631, 6),
                Size = new Size(197, 38),
                Text = "📂 Open Temp Folder",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnOpenFolder.Click += (s, e) => OpenSelectedOrUserTemp();

            pnlActions.Controls.Add(btnCleanSelected);
            pnlActions.Controls.Add(btnScan);
            pnlActions.Controls.Add(btnSelectAll);
            pnlActions.Controls.Add(btnDeselectAll);
            pnlActions.Controls.Add(btnOpenFolder);

            // 5. Footer (Y=566, H=38)
            pnlFooter = new Panel
            {
                Location = new Point(16, 566),
                Size = new Size(828, 38),
                BackColor = Theme.CardBg
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Theme.BorderLight))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlFooter.Width - 1, pnlFooter.Height - 1);
                }
            };

            chkSkipLocked = new CheckBox
            {
                Location = new Point(12, 10),
                AutoSize = true,
                Text = "Safely skip locked and in-use files",
                Checked = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            lblFooterStatus = new Label
            {
                Location = new Point(255, 11),
                Size = new Size(460, 18),
                Text = "Ready • Click 'Scan Disk Junk' to analyze recoverable storage space.",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            lnkBrand = new LinkLabel
            {
                Location = new Point(720, 11),
                Size = new Size(95, 18),
                Text = "pexoris.com",
                Font = Theme.FontSmall,
                ForeColor = Theme.PrimaryBlue,
                LinkColor = Theme.PrimaryBlue,
                ActiveLinkColor = Color.FromArgb(29, 78, 216),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            lnkBrand.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            pnlFooter.Controls.Add(chkSkipLocked);
            pnlFooter.Controls.Add(lblFooterStatus);
            pnlFooter.Controls.Add(lnkBrand);

            // Add all main components
            Controls.Add(pnlFooter);
            Controls.Add(pnlActions);
            Controls.Add(pnlListWrapper);
            Controls.Add(cardTop);
            Controls.Add(pnlTitleBar);

            ResumeLayout(false);
        }

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
                    SendMessage(Handle, WM_SETICON, ICON_SMALL, appIcon.Handle.ToInt32());
                    SendMessage(Handle, WM_SETICON, ICON_BIG, appIcon.Handle.ToInt32());

                    Bitmap bmp = appIcon.ToBitmap();
                    picTitleIcon.Image = bmp;
                    picCardIcon.Image = bmp;
                }
            }
            catch { }
        }

        private void CardTop_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle r = cardTop.ClientRectangle;
            RectangleF rf = new RectangleF(0.5f, 0.5f, r.Width - 1f, r.Height - 1f);

            using (GraphicsPath p = Theme.GetRoundedPath(rf, 8f))
            {
                using (SolidBrush b = new SolidBrush(Theme.CardBg))
                {
                    g.FillPath(b, p);
                }
                using (Pen pen = new Pen(Theme.BorderLight))
                {
                    g.DrawPath(pen, p);
                }
            }
        }

        private void InitTargets()
        {
            _targets = CleanerHelper.GetDefaultTargets();
            PopulateListView();
            UpdateStats();
            // Automatically kick off background scan on launch
            StartScan(false);
        }

        private void PopulateListView()
        {
            _updatingChecks = true;
            lvTargets.BeginUpdate();
            lvTargets.Items.Clear();

            foreach (var target in _targets)
            {
                ListViewItem lvi = new ListViewItem(target.Name);
                lvi.Checked = target.IsSelected;
                lvi.SubItems.Add(target.SizeBytes > 0 ? CleanerHelper.FormatBytes(target.SizeBytes) : (_hasScanned ? "0 B" : "Unscanned"));
                lvi.SubItems.Add(target.FileCount > 0 ? string.Format("{0:N0}", target.FileCount) : (_hasScanned ? "0" : "-"));
                lvi.SubItems.Add(target.Safety);
                lvi.SubItems.Add(target.DirectoryPath);
                lvi.Tag = target;

                if (target.Safety.Contains("100%"))
                {
                    lvi.ForeColor = Color.FromArgb(15, 23, 42);
                }
                else if (target.Safety.Contains("Recommended"))
                {
                    lvi.ForeColor = Color.FromArgb(30, 64, 175);
                }

                lvTargets.Items.Add(lvi);
            }

            lvTargets.EndUpdate();
            _updatingChecks = false;
        }

        private void LvTargets_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_updatingChecks) return;

            CleanTarget target = e.Item.Tag as CleanTarget;
            if (target != null)
            {
                target.IsSelected = e.Item.Checked;
                UpdateStats();
            }
        }

        private void UpdateStats()
        {
            long totalSelectedBytes = 0;
            int totalSelectedFiles = 0;
            int selectedCount = 0;

            foreach (var t in _targets)
            {
                if (t.IsSelected)
                {
                    selectedCount++;
                    totalSelectedBytes += t.SizeBytes;
                    totalSelectedFiles += t.FileCount;
                }
            }

            if (!_hasScanned)
            {
                pillStatus.Text = string.Format("● {0} TARGETS SELECTED", selectedCount);
                pillStatus.BackColor = Color.FromArgb(238, 242, 255);
                pillStatus.ForeColor = Theme.PrimaryBlue;
            }
            else if (totalSelectedBytes > 0)
            {
                pillStatus.Text = string.Format("● {0} JUNK DETECTED", CleanerHelper.FormatBytes(totalSelectedBytes));
                pillStatus.BackColor = Color.FromArgb(254, 242, 242);
                pillStatus.ForeColor = Theme.DestructiveRed;
            }
            else
            {
                pillStatus.Text = "● 0 B TO CLEAN (CLEAN)";
                pillStatus.BackColor = Color.FromArgb(236, 253, 245);
                pillStatus.ForeColor = Theme.SuccessGreen;
            }
            pillStatus.Invalidate();

            lblFooterStatus.Text = string.Format("Selected: {0} of {1} categories • {2} recoverable ({3} files).",
                selectedCount, _targets.Count, CleanerHelper.FormatBytes(totalSelectedBytes), totalSelectedFiles);
        }

        private void SetAllSelection(bool check)
        {
            _updatingChecks = true;
            lvTargets.BeginUpdate();
            foreach (ListViewItem item in lvTargets.Items)
            {
                item.Checked = check;
                CleanTarget t = item.Tag as CleanTarget;
                if (t != null) t.IsSelected = check;
            }
            lvTargets.EndUpdate();
            _updatingChecks = false;
            UpdateStats();
        }

        private void SelectRecommended()
        {
            _updatingChecks = true;
            lvTargets.BeginUpdate();
            foreach (ListViewItem item in lvTargets.Items)
            {
                CleanTarget t = item.Tag as CleanTarget;
                if (t != null)
                {
                    // Select all except CBS logs
                    bool shouldCheck = (t.Id != "CbsLogs");
                    item.Checked = shouldCheck;
                    t.IsSelected = shouldCheck;
                }
            }
            lvTargets.EndUpdate();
            _updatingChecks = false;
            UpdateStats();
        }

        private void StartScan(bool autoCleanAfter)
        {
            if (_isBusy) return;
            _isBusy = true;

            btnScan.Enabled = false;
            btnCleanSelected.Enabled = false;
            btnPresetDeepClean.Enabled = false;
            btnPresetQuickScan.Enabled = false;
            lblFooterStatus.Text = "Scanning junk locations across system...";

            ThreadPool.QueueUserWorkItem(delegate
            {
                foreach (var t in _targets)
                {
                    CleanerHelper.ScanTarget(t);
                }

                BeginInvoke(new Action(delegate
                {
                    _isBusy = false;
                    _hasScanned = true;
                    btnScan.Enabled = true;
                    btnCleanSelected.Enabled = true;
                    btnPresetDeepClean.Enabled = true;
                    btnPresetQuickScan.Enabled = true;

                    // Refresh item views
                    _updatingChecks = true;
                    lvTargets.BeginUpdate();
                    for (int i = 0; i < lvTargets.Items.Count; i++)
                    {
                        CleanTarget target = lvTargets.Items[i].Tag as CleanTarget;
                        if (target != null)
                        {
                            lvTargets.Items[i].SubItems[1].Text = CleanerHelper.FormatBytes(target.SizeBytes);
                            lvTargets.Items[i].SubItems[2].Text = string.Format("{0:N0}", target.FileCount);
                        }
                    }
                    lvTargets.EndUpdate();
                    _updatingChecks = false;

                    UpdateStats();

                    if (autoCleanAfter)
                    {
                        StartClean();
                    }
                }));
            });
        }

        private void PresetDeepClean()
        {
            if (_isBusy) return;
            SelectRecommended();
            if (!_hasScanned)
            {
                StartScan(true);
            }
            else
            {
                StartClean();
            }
        }

        private void StartClean()
        {
            if (_isBusy) return;

            long totalBytesToClean = 0;
            int countSelected = 0;
            foreach (var t in _targets)
            {
                if (t.IsSelected)
                {
                    countSelected++;
                    totalBytesToClean += t.SizeBytes;
                }
            }

            if (countSelected == 0)
            {
                MessageBox.Show("Please select at least one junk category to clean.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                string.Format("Are you sure you want to permanently delete selected temporary files?\n\nCategories selected: {0}\nEstimated size: {1}\n\nFiles actively locked by running programs will be safely preserved.",
                    countSelected, CleanerHelper.FormatBytes(totalBytesToClean)),
                "Confirm Junk Clean",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _isBusy = true;
            btnCleanSelected.Enabled = false;
            btnScan.Enabled = false;
            btnPresetDeepClean.Enabled = false;
            lblFooterStatus.Text = "Purging temporary files...";

            ThreadPool.QueueUserWorkItem(delegate
            {
                long totalFreed = 0;
                int totalDeleted = 0;
                int totalSkipped = 0;

                foreach (var t in _targets)
                {
                    if (t.IsSelected)
                    {
                        CleanerHelper.CleanTargetFiles(t);
                        totalFreed += t.FreedBytes;
                        totalDeleted += t.DeletedFiles;
                        totalSkipped += t.SkippedFiles;

                        // Re-scan remaining in this target
                        CleanerHelper.ScanTarget(t);
                    }
                }

                BeginInvoke(new Action(delegate
                {
                    _isBusy = false;
                    btnCleanSelected.Enabled = true;
                    btnScan.Enabled = true;
                    btnPresetDeepClean.Enabled = true;

                    // Refresh item views
                    _updatingChecks = true;
                    lvTargets.BeginUpdate();
                    for (int i = 0; i < lvTargets.Items.Count; i++)
                    {
                        CleanTarget target = lvTargets.Items[i].Tag as CleanTarget;
                        if (target != null)
                        {
                            lvTargets.Items[i].SubItems[1].Text = CleanerHelper.FormatBytes(target.SizeBytes);
                            lvTargets.Items[i].SubItems[2].Text = string.Format("{0:N0}", target.FileCount);
                        }
                    }
                    lvTargets.EndUpdate();
                    _updatingChecks = false;

                    UpdateStats();

                    pillStatus.Text = string.Format("● {0} PURGED SAFELY", CleanerHelper.FormatBytes(totalFreed));
                    pillStatus.BackColor = Color.FromArgb(236, 253, 245);
                    pillStatus.ForeColor = Theme.SuccessGreen;
                    pillStatus.Invalidate();

                    string summary = string.Format(
                        "Cleaning Completed Successfully!\n\n• Storage Space Freed: {0}\n• Files Deleted: {1:N0}\n• Active Locked Files Preserved: {2:N0}",
                        CleanerHelper.FormatBytes(totalFreed), totalDeleted, totalSkipped);

                    lblFooterStatus.Text = string.Format("Cleanup finished: Freed {0} ({1} files). {2} active locked files preserved.",
                        CleanerHelper.FormatBytes(totalFreed), totalDeleted, totalSkipped);

                    MessageBox.Show(summary, "Pexoris Temp Cleaner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            });
        }

        private void OpenSelectedOrUserTemp()
        {
            string targetFolder = Path.GetTempPath();

            if (lvTargets.SelectedItems.Count > 0)
            {
                CleanTarget t = lvTargets.SelectedItems[0].Tag as CleanTarget;
                if (t != null && !string.IsNullOrEmpty(t.DirectoryPath) && Directory.Exists(t.DirectoryPath))
                {
                    targetFolder = t.DirectoryPath;
                }
            }

            try
            {
                Process.Start("explorer.exe", targetFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
