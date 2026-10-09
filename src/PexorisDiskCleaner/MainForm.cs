using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PexorisDiskCleaner
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

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
        private PexorisButton _btnPresetSafe;
        private PexorisButton _btnPresetAll;
        private PexorisButton _btnPresetNone;
        private Label _lblDriveInfo;

        private ListView _lvCategories;
        private Panel _pnlActionBar;
        private PexorisButton _btnClean;
        private PexorisButton _btnScan;
        private PexorisButton _btnEmptyBin;
        private PexorisButton _btnNativeClean;
        private PexorisButton _btnRefresh;

        private Panel _pnlFooter;
        private Label _lblStatus;
        private CheckBox _chkAutoClose;
        private LinkLabel _lnkSite;

        private List<JunkCategory> _categories;
        private bool _isBusy = false;

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

            _categories = DiskEngine.GetDefaultCategories();

            InitComponents();
            PopulateListView();
            PerformAsyncScan();
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
                Width = 380,
                Height = 20,
                Text = "Pexoris DiskCleaner • Portable Utility",
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
                Width = 260,
                Height = 24,
                Text = "Pexoris DiskCleaner",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            _lblCardSub = new Label
            {
                Left = 72,
                Top = 40,
                Width = 460,
                Height = 20,
                Text = "Deep disk cleanup, Windows upgrade leftovers, and temporary storage cleaner",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lblPillBadge = new Label
            {
                Left = 650,
                Top = 16,
                Width = 150,
                Height = 26,
                Text = "● Ready to Scan",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontSmall,
                ForeColor = Theme.Emerald,
                BackColor = Theme.EmeraldLight
            };
            _lblPillBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, _lblPillBadge.Width - 1, _lblPillBadge.Height - 1);
                using (Pen p = new Pen(Theme.EmeraldBorder, 1f))
                {
                    using (GraphicsPath gp = Theme.GetRoundedPath(rect, 13f))
                    {
                        e.Graphics.DrawPath(p, gp);
                    }
                }
            };

            // Preset Buttons & Drive Info
            _btnPresetSafe = new PexorisButton
            {
                Left = 16,
                Top = 76,
                Width = 140,
                Height = 34,
                Text = "⚡ Safe Defaults",
                Style = PexorisButtonStyle.PrimaryEmerald
            };
            _btnPresetSafe.Click += (s, e) => ApplyPreset(true, false);

            _btnPresetAll = new PexorisButton
            {
                Left = 164,
                Top = 76,
                Width = 110,
                Height = 34,
                Text = "Select All",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnPresetAll.Click += (s, e) => ApplyPreset(false, true);

            _btnPresetNone = new PexorisButton
            {
                Left = 282,
                Top = 76,
                Width = 110,
                Height = 34,
                Text = "Deselect All",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnPresetNone.Click += (s, e) => ApplyPreset(false, false);

            string sysDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            DriveInfo dInfo = new DriveInfo(sysDrive);
            string freeSpaceStr = DiskEngine.FormatBytes(dInfo.AvailableFreeSpace);

            _lblDriveInfo = new Label
            {
                Left = 410,
                Top = 82,
                Width = 390,
                Height = 24,
                Text = string.Format("System Drive: {0} ({1} Free Storage Available)", sysDrive, freeSpaceStr),
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            _pnlTopCard.Controls.AddRange(new Control[] {
                _pbCardIcon, _lblCardTitle, _lblCardSub, _lblPillBadge,
                _btnPresetSafe, _btnPresetAll, _btnPresetNone, _lblDriveInfo
            });
            Controls.Add(_pnlTopCard);

            // 3. Explorer Table ListView (Y=190, H=305, W=820, X=20)
            _lvCategories = new ListView
            {
                Left = 20,
                Top = 190,
                Width = 820,
                Height = 305,
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = Theme.FontRegular,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lvCategories.Columns.Add("Junk Category", 240);
            _lvCategories.Columns.Add("Target Description", 290);
            _lvCategories.Columns.Add("File Count", 90, HorizontalAlignment.Right);
            _lvCategories.Columns.Add("Size", 100, HorizontalAlignment.Right);
            _lvCategories.Columns.Add("Status", 95, HorizontalAlignment.Center);

            _lvCategories.ItemChecked += LvCategories_ItemChecked;
            Controls.Add(_lvCategories);

            // 4. Action Buttons Bar (Y=508, H=50, W=820, X=20)
            _pnlActionBar = new Panel
            {
                Left = 20,
                Top = 508,
                Width = 820,
                Height = 50,
                BackColor = Color.Transparent
            };

            _btnClean = new PexorisButton
            {
                Left = 0,
                Top = 4,
                Width = 210,
                Height = 42,
                Text = "⚡ Clean Selected Junk",
                Style = PexorisButtonStyle.PrimaryEmerald
            };
            _btnClean.Click += BtnClean_Click;

            _btnScan = new PexorisButton
            {
                Left = 218,
                Top = 4,
                Width = 190,
                Height = 42,
                Text = "🔍 Scan All Categories",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            _btnScan.Click += (s, e) => PerformAsyncScan();

            _btnEmptyBin = new PexorisButton
            {
                Left = 416,
                Top = 4,
                Width = 150,
                Height = 42,
                Text = "Empty Recycle Bin",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnEmptyBin.Click += BtnEmptyBin_Click;

            _btnNativeClean = new PexorisButton
            {
                Left = 574,
                Top = 4,
                Width = 146,
                Height = 42,
                Text = "Windows Cleanmgr",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnNativeClean.Click += (s, e) =>
            {
                try { Process.Start("cleanmgr.exe"); } catch { }
            };

            _btnRefresh = new PexorisButton
            {
                Left = 728,
                Top = 4,
                Width = 92,
                Height = 42,
                Text = "Refresh",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRefresh.Click += (s, e) => PerformAsyncScan();

            _pnlActionBar.Controls.AddRange(new Control[] {
                _btnClean, _btnScan, _btnEmptyBin, _btnNativeClean, _btnRefresh
            });
            Controls.Add(_pnlActionBar);

            // 5. Footer (38px, Y=602)
            _pnlFooter = new Panel
            {
                Left = 0,
                Top = 602,
                Width = 860,
                Height = 38,
                BackColor = Color.White
            };
            _pnlFooter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, 860, 0);
                }
            };

            _chkAutoClose = new CheckBox
            {
                Left = 20,
                Top = 9,
                Width = 160,
                Height = 20,
                Text = "Close after cleaning",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lblStatus = new Label
            {
                Left = 190,
                Top = 9,
                Width = 520,
                Height = 20,
                Text = "Ready • Calculating reclaimable storage...",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextBody,
                BackColor = Color.Transparent
            };

            _lnkSite = new LinkLabel
            {
                Left = 740,
                Top = 9,
                Width = 100,
                Height = 20,
                Text = "pexoris.com",
                Font = Theme.FontBold,
                LinkColor = Theme.Emerald,
                ActiveLinkColor = Theme.EmeraldDark,
                TextAlign = ContentAlignment.TopRight,
                BackColor = Color.Transparent
            };
            _lnkSite.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            _pnlFooter.Controls.AddRange(new Control[] { _chkAutoClose, _lblStatus, _lnkSite });
            Controls.Add(_pnlFooter);
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void PopulateListView()
        {
            _lvCategories.BeginUpdate();
            _lvCategories.Items.Clear();

            for (int i = 0; i < _categories.Count; i++)
            {
                JunkCategory cat = _categories[i];
                ListViewItem item = new ListViewItem(cat.Name);
                item.Tag = cat;
                item.Checked = cat.IsSelected;

                item.SubItems.Add(cat.Description);
                item.SubItems.Add(cat.FileCount > 0 ? cat.FileCount.ToString("N0") : "-");
                item.SubItems.Add(DiskEngine.FormatBytes(cat.TotalBytes));
                item.SubItems.Add(cat.Status);

                _lvCategories.Items.Add(item);
            }
            _lvCategories.EndUpdate();
            UpdateTotalStatus();
        }

        private void LvCategories_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            JunkCategory cat = e.Item.Tag as JunkCategory;
            if (cat != null)
            {
                cat.IsSelected = e.Item.Checked;
                UpdateTotalStatus();
            }
        }

        private void ApplyPreset(bool safeDefaults, bool all)
        {
            _lvCategories.BeginUpdate();
            for (int i = 0; i < _lvCategories.Items.Count; i++)
            {
                ListViewItem item = _lvCategories.Items[i];
                JunkCategory cat = item.Tag as JunkCategory;
                if (cat != null)
                {
                    if (all)
                    {
                        item.Checked = true;
                    }
                    else if (safeDefaults)
                    {
                        item.Checked = cat.IsRecommended;
                    }
                    else
                    {
                        item.Checked = false;
                    }
                }
            }
            _lvCategories.EndUpdate();
            UpdateTotalStatus();
        }

        private void UpdateTotalStatus()
        {
            long selectedBytes = 0;
            int selectedCount = 0;

            for (int i = 0; i < _categories.Count; i++)
            {
                if (_categories[i].IsSelected)
                {
                    selectedBytes += _categories[i].TotalBytes;
                    selectedCount += _categories[i].FileCount;
                }
            }

            _lblStatus.Text = string.Format("Selected: {0} files ({1}) • 10 Categories available",
                selectedCount.ToString("N0"),
                DiskEngine.FormatBytes(selectedBytes));

            if (selectedBytes > 0)
            {
                _lblPillBadge.Text = "● Junk Found (" + DiskEngine.FormatBytes(selectedBytes) + ")";
                _lblPillBadge.ForeColor = Theme.DestructiveRed;
                _lblPillBadge.BackColor = Color.FromArgb(254, 242, 242);
            }
            else
            {
                _lblPillBadge.Text = "● System Clean";
                _lblPillBadge.ForeColor = Theme.Emerald;
                _lblPillBadge.BackColor = Theme.EmeraldLight;
            }
        }

        private void PerformAsyncScan()
        {
            if (_isBusy) return;
            _isBusy = true;
            _btnScan.Text = "Scanning...";
            _lblPillBadge.Text = "● Scanning...";
            _lblPillBadge.ForeColor = Theme.PrimaryBlue;
            _lblPillBadge.BackColor = Color.FromArgb(239, 246, 255);

            Thread worker = new Thread(() =>
            {
                for (int i = 0; i < _categories.Count; i++)
                {
                    DiskEngine.ScanCategory(_categories[i]);
                }

                if (IsHandleCreated)
                {
                    Invoke(new Action(() =>
                    {
                        _isBusy = false;
                        _btnScan.Text = "🔍 Scan All Categories";
                        PopulateListView();
                    }));
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void BtnClean_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            long totalTargetBytes = 0;
            for (int i = 0; i < _categories.Count; i++)
            {
                if (_categories[i].IsSelected) totalTargetBytes += _categories[i].TotalBytes;
            }

            if (totalTargetBytes <= 0)
            {
                MessageBox.Show("No junk items selected or no reclaimable storage found.", "Pexoris DiskCleaner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                string.Format("Clean selected categories?\n\nEstimated space to reclaim: {0}\n\nIn-use system files will be safely skipped.", DiskEngine.FormatBytes(totalTargetBytes)),
                "Confirm Disk Cleanup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _isBusy = true;
            _btnClean.Text = "Cleaning...";
            _lblPillBadge.Text = "● Cleaning Storage...";
            _lblPillBadge.ForeColor = Theme.DestructiveRed;

            Thread worker = new Thread(() =>
            {
                long totalFreedBytes = 0;
                int totalFreedFiles = 0;

                for (int i = 0; i < _categories.Count; i++)
                {
                    if (_categories[i].IsSelected)
                    {
                        long cleaned;
                        totalFreedFiles += DiskEngine.CleanCategory(_categories[i], out cleaned);
                        totalFreedBytes += cleaned;
                    }
                }

                if (IsHandleCreated)
                {
                    Invoke(new Action(() =>
                    {
                        _isBusy = false;
                        _btnClean.Text = "⚡ Clean Selected Junk";
                        PopulateListView();

                        MessageBox.Show(
                            string.Format("Disk cleanup completed successfully!\n\nFreed: {0}\nFiles Purged: {1}",
                                DiskEngine.FormatBytes(totalFreedBytes),
                                totalFreedFiles.ToString("N0")),
                            "Cleanup Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        if (_chkAutoClose.Checked)
                        {
                            Application.Exit();
                        }
                    }));
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void BtnEmptyBin_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < _categories.Count; i++)
            {
                if (_categories[i].Id == "recyclebin")
                {
                    long freed;
                    DiskEngine.CleanCategory(_categories[i], out freed);
                    PopulateListView();
                    MessageBox.Show(string.Format("Recycle Bin emptied successfully!\nFreed: {0}", DiskEngine.FormatBytes(freed)), "Recycle Bin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                }
            }
        }
    }
}
