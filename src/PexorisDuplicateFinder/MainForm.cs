using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PexorisDuplicateFinder
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

        // Title Bar Controls
        private Panel _pnlTitleBar;
        private PictureBox _pbTitleIcon;
        private Label _lblTitle;
        private Button _btnMin;
        private Button _btnClose;

        // Top Control Card Controls
        private Panel _pnlTopCard;
        private PictureBox _pbCardIcon;
        private Label _lblCardTitle;
        private Label _lblCardSub;
        private Label _lblPillBadge;

        private Label _lblFolder;
        private TextBox _txtPath;
        private Button _btnBrowse;
        private Button _btnPresetDownloads;
        private Button _btnPresetPictures;
        private Button _btnPresetDocs;

        private Label _lblFilterSize;
        private ComboBox _cboMinSize;
        private Label _lblFilterType;
        private ComboBox _cboFileType;

        // Middle ListView
        private ListView _lvDuplicates;

        // Action Bar Controls
        private Panel _pnlActionBar;
        private PexorisButton _btnDelete;
        private PexorisButton _btnAutoSelect;
        private PexorisButton _btnOpenFolder;
        private PexorisButton _btnExport;
        private PexorisButton _btnScan;

        // Footer Bar Controls
        private Panel _pnlFooter;
        private CheckBox _chkRecycleBin;
        private Label _lblStatus;
        private LinkLabel _lnkSite;

        // Scan state
        private bool _isScanning = false;
        private bool _cancelRequested = false;
        private Thread _scanThread = null;
        private List<DuplicateGroup> _duplicateGroups = new List<DuplicateGroup>();

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

            // Set default search folder to Downloads
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloads = Path.Combine(userProfile, "Downloads");
            if (Directory.Exists(downloads))
            {
                _txtPath.Text = downloads;
            }
            else
            {
                _txtPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
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
            if (Icon != null) _pbTitleIcon.Image = Icon.ToBitmap();
            _pbTitleIcon.MouseDown += TitleBar_MouseDown;

            _lblTitle = new Label
            {
                Left = 44,
                Top = 12,
                Width = 400,
                Height = 20,
                Text = "Pexoris DuplicateFinder • Portable Utility",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };
            _lblTitle.MouseDown += TitleBar_MouseDown;

            _btnMin = new Button
            {
                Left = 772,
                Top = 0,
                Width = 44,
                Height = 44,
                Text = "—",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand
            };
            _btnMin.FlatAppearance.BorderSize = 0;
            _btnMin.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            _btnMin.Click += delegate { WindowState = FormWindowState.Minimized; };

            _btnClose = new Button
            {
                Left = 816,
                Top = 0,
                Width = 44,
                Height = 44,
                Text = "✕",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSub,
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 68, 68);
            _btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 38, 38);
            _btnClose.MouseEnter += delegate { _btnClose.ForeColor = Color.White; };
            _btnClose.MouseLeave += delegate { _btnClose.ForeColor = Theme.TextSub; };
            _btnClose.Click += delegate { Close(); };

            _pnlTitleBar.Controls.Add(_pbTitleIcon);
            _pnlTitleBar.Controls.Add(_lblTitle);
            _pnlTitleBar.Controls.Add(_btnMin);
            _pnlTitleBar.Controls.Add(_btnClose);

            // 2. Top Control Card (H=130, Y=48)
            _pnlTopCard = new Panel
            {
                Left = 14,
                Top = 48,
                Width = 832,
                Height = 130,
                BackColor = Color.White
            };
            _pnlTopCard.Paint += delegate(object sender, PaintEventArgs pe)
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, _pnlTopCard.Width - 1, _pnlTopCard.Height - 1);
                using (GraphicsPath path = Theme.GetRoundedPath(rect, 8f))
                using (Pen pen = new Pen(Theme.BorderLight, 1f))
                {
                    pe.Graphics.DrawPath(pen, path);
                }
            };

            _pbCardIcon = new PictureBox
            {
                Left = 16,
                Top = 14,
                Width = 44,
                Height = 44,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            if (Icon != null) _pbCardIcon.Image = Icon.ToBitmap();

            _lblCardTitle = new Label
            {
                Left = 70,
                Top = 14,
                Width = 380,
                Height = 22,
                Text = "Duplicate File Finder & Space Reclaimer",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            _lblCardSub = new Label
            {
                Left = 70,
                Top = 38,
                Width = 530,
                Height = 18,
                Text = "Ultra-fast 3-tier hash matching (Size → 4KB Header → SHA-256) • Safe Recycle Bin Delete",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lblPillBadge = new Label
            {
                Left = 680,
                Top = 14,
                Width = 138,
                Height = 26,
                Text = "⚡ Standby",
                Font = Theme.FontBold,
                ForeColor = Theme.Indigo,
                BackColor = Theme.IndigoLight,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _lblPillBadge.Paint += delegate(object s, PaintEventArgs pe)
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath p = Theme.GetRoundedPath(new RectangleF(0, 0, _lblPillBadge.Width - 1, _lblPillBadge.Height - 1), 13f))
                using (Pen pen = new Pen(Theme.IndigoBorder, 1f))
                {
                    pe.Graphics.DrawPath(pen, p);
                }
            };

            // Path Row (Y=62)
            _lblFolder = new Label
            {
                Left = 16,
                Top = 66,
                Width = 52,
                Height = 20,
                Text = "Folder:",
                Font = Theme.FontBold,
                ForeColor = Theme.TextBody
            };

            _txtPath = new TextBox
            {
                Left = 70,
                Top = 63,
                Width = 440,
                Height = 24,
                Font = Theme.FontRegular,
                ForeColor = Theme.TextHero
            };

            _btnBrowse = new Button
            {
                Left = 516,
                Top = 62,
                Width = 72,
                Height = 26,
                Text = "Browse...",
                Font = Theme.FontSmall,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Theme.TextBody
            };
            _btnBrowse.FlatAppearance.BorderColor = Theme.BorderLight;
            _btnBrowse.Click += BtnBrowse_Click;

            _btnPresetDownloads = CreatePresetButton("Downloads", 594, 62, 70, delegate
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(p)) _txtPath.Text = p;
            });

            _btnPresetPictures = CreatePresetButton("Pictures", 668, 62, 66, delegate
            {
                string p = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (Directory.Exists(p)) _txtPath.Text = p;
            });

            _btnPresetDocs = CreatePresetButton("Documents", 738, 62, 80, delegate
            {
                string p = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (Directory.Exists(p)) _txtPath.Text = p;
            });

            // Filter Row (Y=96)
            _lblFilterSize = new Label
            {
                Left = 70,
                Top = 99,
                Width = 60,
                Height = 20,
                Text = "Min Size:",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            _cboMinSize = new ComboBox
            {
                Left = 132,
                Top = 96,
                Width = 110,
                Height = 24,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontSmall
            };
            _cboMinSize.Items.AddRange(new object[] { "All Sizes (> 0 B)", "> 100 KB", "> 1 MB (Default)", "> 10 MB", "> 100 MB" });
            _cboMinSize.SelectedIndex = 2; // Default > 1 MB

            _lblFilterType = new Label
            {
                Left = 260,
                Top = 99,
                Width = 64,
                Height = 20,
                Text = "File Type:",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            _cboFileType = new ComboBox
            {
                Left = 326,
                Top = 96,
                Width = 260,
                Height = 24,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontSmall
            };
            _cboFileType.Items.AddRange(new object[] {
                "All Files (*.*)",
                "Images (*.jpg;*.png;*.webp;*.gif;*.bmp)",
                "Videos (*.mp4;*.mkv;*.mov;*.avi;*.wmv)",
                "Documents (*.pdf;*.docx;*.xlsx;*.pptx;*.txt)",
                "Archives (*.zip;*.rar;*.7z;*.tar;*.gz)"
            });
            _cboFileType.SelectedIndex = 0;

            _pnlTopCard.Controls.Add(_pbCardIcon);
            _pnlTopCard.Controls.Add(_lblCardTitle);
            _pnlTopCard.Controls.Add(_lblCardSub);
            _pnlTopCard.Controls.Add(_lblPillBadge);
            _pnlTopCard.Controls.Add(_lblFolder);
            _pnlTopCard.Controls.Add(_txtPath);
            _pnlTopCard.Controls.Add(_btnBrowse);
            _pnlTopCard.Controls.Add(_btnPresetDownloads);
            _pnlTopCard.Controls.Add(_btnPresetPictures);
            _pnlTopCard.Controls.Add(_btnPresetDocs);
            _pnlTopCard.Controls.Add(_lblFilterSize);
            _pnlTopCard.Controls.Add(_cboMinSize);
            _pnlTopCard.Controls.Add(_lblFilterType);
            _pnlTopCard.Controls.Add(_cboFileType);

            // 3. Middle ListView Explorer (Y=184, H=318)
            _lvDuplicates = new ListView
            {
                Left = 14,
                Top = 184,
                Width = 832,
                Height = 318,
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true,
                GridLines = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = Theme.FontRegular
            };

            _lvDuplicates.Columns.Add("File Name", 220);
            _lvDuplicates.Columns.Add("Group", 70);
            _lvDuplicates.Columns.Add("Role", 85);
            _lvDuplicates.Columns.Add("Size", 85);
            _lvDuplicates.Columns.Add("Modified Date", 125);
            _lvDuplicates.Columns.Add("Folder Path", 240);

            _lvDuplicates.ItemChecked += delegate { UpdateStatusSummary(); };

            // 4. Action Buttons Bar (Y=508, H=50)
            _pnlActionBar = new Panel { Left = 14, Top = 508, Width = 832, Height = 50, BackColor = Color.Transparent };

            _btnDelete = new PexorisButton
            {
                Left = 0,
                Top = 6,
                Width = 210,
                Height = 40,
                Text = "🗑️ Delete Selected Files",
                Style = PexorisButtonStyle.DestructiveRed
            };
            _btnDelete.Click += BtnDelete_Click;

            _btnAutoSelect = new PexorisButton
            {
                Left = 218,
                Top = 6,
                Width = 190,
                Height = 40,
                Text = "⚡ Smart Auto-Select",
                Style = PexorisButtonStyle.PrimaryIndigo
            };
            _btnAutoSelect.Click += BtnAutoSelect_Click;

            _btnOpenFolder = new PexorisButton
            {
                Left = 416,
                Top = 6,
                Width = 140,
                Height = 40,
                Text = "📂 Open Folder",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnOpenFolder.Click += BtnOpenFolder_Click;

            _btnExport = new PexorisButton
            {
                Left = 564,
                Top = 6,
                Width = 130,
                Height = 40,
                Text = "📋 Export CSV",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnExport.Click += BtnExport_Click;

            _btnScan = new PexorisButton
            {
                Left = 702,
                Top = 6,
                Width = 130,
                Height = 40,
                Text = "🔍 Start Scan",
                Style = PexorisButtonStyle.SuccessGreen
            };
            _btnScan.Click += BtnScan_Click;

            _pnlActionBar.Controls.Add(_btnDelete);
            _pnlActionBar.Controls.Add(_btnAutoSelect);
            _pnlActionBar.Controls.Add(_btnOpenFolder);
            _pnlActionBar.Controls.Add(_btnExport);
            _pnlActionBar.Controls.Add(_btnScan);

            // 5. Footer Bar (38px, Y=564)
            _pnlFooter = new Panel { Left = 0, Top = 564, Width = 860, Height = 76, BackColor = Color.White };
            _pnlFooter.Paint += delegate(object s, PaintEventArgs pe)
            {
                pe.Graphics.DrawLine(new Pen(Theme.BorderLight), 0, 0, _pnlFooter.Width, 0);
            };

            _chkRecycleBin = new CheckBox
            {
                Left = 16,
                Top = 8,
                Width = 310,
                Height = 22,
                Text = "Send deleted files to Recycle Bin (Safe Undo)",
                Checked = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextBody,
                BackColor = Color.Transparent
            };

            _lblStatus = new Label
            {
                Left = 16,
                Top = 36,
                Width = 650,
                Height = 20,
                Text = "Ready • Choose a target folder and click 'Start Scan'",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lnkSite = new LinkLabel
            {
                Left = 716,
                Top = 36,
                Width = 130,
                Height = 20,
                Text = "pexoris.com",
                Font = Theme.FontBold,
                LinkColor = Theme.Indigo,
                ActiveLinkColor = Theme.IndigoHover,
                TextAlign = ContentAlignment.TopRight,
                BackColor = Color.Transparent
            };
            _lnkSite.LinkClicked += delegate { Process.Start("https://pexoris.com/tools/duplicate-finder/"); };

            _pnlFooter.Controls.Add(_chkRecycleBin);
            _pnlFooter.Controls.Add(_lblStatus);
            _pnlFooter.Controls.Add(_lnkSite);

            // Add all main panels
            Controls.Add(_pnlTitleBar);
            Controls.Add(_pnlTopCard);
            Controls.Add(_lvDuplicates);
            Controls.Add(_pnlActionBar);
            Controls.Add(_pnlFooter);
        }

        private Button CreatePresetButton(string text, int left, int top, int width, EventHandler onClick)
        {
            Button btn = new Button
            {
                Left = left,
                Top = top,
                Width = width,
                Height = 26,
                Text = text,
                Font = Theme.FontSmall,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Theme.TextBody
            };
            btn.FlatAppearance.BorderColor = Theme.BorderLight;
            btn.Click += onClick;
            return btn;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select target folder to scan for duplicate files:";
                if (!string.IsNullOrEmpty(_txtPath.Text) && Directory.Exists(_txtPath.Text))
                {
                    fbd.SelectedPath = _txtPath.Text;
                }
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _txtPath.Text = fbd.SelectedPath;
                }
            }
        }

        private long GetSelectedMinSizeBytes()
        {
            switch (_cboMinSize.SelectedIndex)
            {
                case 1: return 100 * 1024; // 100 KB
                case 2: return 1024 * 1024; // 1 MB
                case 3: return 10 * 1024 * 1024; // 10 MB
                case 4: return 100 * 1024 * 1024; // 100 MB
                default: return 0;
            }
        }

        private string GetSelectedSearchPattern()
        {
            switch (_cboFileType.SelectedIndex)
            {
                case 1: return "*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp;*.tiff";
                case 2: return "*.mp4;*.mkv;*.mov;*.avi;*.wmv;*.flv;*.webm";
                case 3: return "*.pdf;*.docx;*.doc;*.xlsx;*.xls;*.pptx;*.ppt;*.txt;*.rtf";
                case 4: return "*.zip;*.rar;*.7z;*.tar;*.gz;*.bz2;*.iso";
                default: return "*.*";
            }
        }

        private void BtnScan_Click(object sender, EventArgs e)
        {
            if (_isScanning)
            {
                _cancelRequested = true;
                _btnScan.Text = "Stopping...";
                _btnScan.Enabled = false;
                return;
            }

            string targetDir = _txtPath.Text.Trim();
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
            {
                MessageBox.Show(
                    "Please select a valid directory path to scan.",
                    "Invalid Folder Path",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _isScanning = true;
            _cancelRequested = false;
            _btnScan.Text = "🛑 Stop Scan";
            _btnScan.Style = PexorisButtonStyle.DestructiveRed;
            _btnScan.Invalidate();

            _lblPillBadge.Text = "🔍 Scanning...";
            _lblPillBadge.ForeColor = Theme.PrimaryBlue;
            _lblPillBadge.BackColor = Color.FromArgb(239, 246, 255);
            _lblPillBadge.Invalidate();

            _lvDuplicates.Items.Clear();
            _duplicateGroups.Clear();

            long minSize = GetSelectedMinSizeBytes();
            string pattern = GetSelectedSearchPattern();

            _scanThread = new Thread(delegate ()
            {
                List<DuplicateGroup> results = DuplicateEngine.ScanDirectory(
                    targetDir,
                    minSize,
                    pattern,
                    delegate (string currentFolder, int count)
                    {
                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(delegate ()
                            {
                                _lblStatus.Text = string.Format("Scanning {0} files found... {1}", count, TruncatePath(currentFolder, 60));
                            }));
                        }
                    },
                    delegate ()
                    {
                        return _cancelRequested;
                    });

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(delegate ()
                    {
                        OnScanCompleted(results);
                    }));
                }
            });
            _scanThread.IsBackground = true;
            _scanThread.Start();
        }

        private void OnScanCompleted(List<DuplicateGroup> groups)
        {
            _isScanning = false;
            _btnScan.Text = "🔍 Start Scan";
            _btnScan.Style = PexorisButtonStyle.SuccessGreen;
            _btnScan.Enabled = true;
            _btnScan.Invalidate();

            _duplicateGroups = groups;
            PopulateListView(groups);

            int totalGroups = groups.Count;
            int totalDuplicates = 0;
            long totalReclaimable = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                totalDuplicates += (groups[i].Items.Count - 1);
                totalReclaimable += (groups[i].FileSize * (groups[i].Items.Count - 1));
            }

            if (totalGroups > 0)
            {
                _lblPillBadge.Text = string.Format("✅ {0} Groups Found", totalGroups);
                _lblPillBadge.ForeColor = Theme.SuccessGreen;
                _lblPillBadge.BackColor = Color.FromArgb(236, 253, 245);
            }
            else
            {
                _lblPillBadge.Text = "✅ Clean (0 Clones)";
                _lblPillBadge.ForeColor = Theme.TextSub;
                _lblPillBadge.BackColor = Theme.BgCanvas;
            }
            _lblPillBadge.Invalidate();

            UpdateStatusSummary();
        }

        private void PopulateListView(List<DuplicateGroup> groups)
        {
            _lvDuplicates.BeginUpdate();
            _lvDuplicates.Items.Clear();

            Color evenGroupColor = Color.FromArgb(245, 247, 255); // Soft Indigo Tint
            Color oddGroupColor = Color.White;

            for (int g = 0; g < groups.Count; g++)
            {
                DuplicateGroup grp = groups[g];
                Color rowBg = (g % 2 == 0) ? evenGroupColor : oddGroupColor;

                for (int i = 0; i < grp.Items.Count; i++)
                {
                    DuplicateFileItem item = grp.Items[i];
                    ListViewItem lvi = new ListViewItem(item.FileName);
                    lvi.Tag = item;
                    lvi.SubItems.Add("#" + item.GroupId);

                    if (item.IsOriginal)
                    {
                        lvi.SubItems.Add("Original ★");
                        lvi.ForeColor = Color.FromArgb(15, 23, 42);
                        lvi.Checked = false; // Never auto-check original
                    }
                    else
                    {
                        lvi.SubItems.Add("Duplicate");
                        lvi.ForeColor = Color.FromArgb(220, 38, 38);
                        lvi.Checked = true; // Auto-check duplicate copy for convenience
                    }

                    lvi.SubItems.Add(item.FormattedSize);
                    lvi.SubItems.Add(item.ModifiedDate.ToString("yyyy-MM-dd HH:mm"));
                    lvi.SubItems.Add(item.DirectoryName);
                    lvi.BackColor = rowBg;

                    _lvDuplicates.Items.Add(lvi);
                }
            }

            _lvDuplicates.EndUpdate();
        }

        private void BtnAutoSelect_Click(object sender, EventArgs e)
        {
            if (_lvDuplicates.Items.Count == 0) return;

            _lvDuplicates.BeginUpdate();
            for (int i = 0; i < _lvDuplicates.Items.Count; i++)
            {
                ListViewItem lvi = _lvDuplicates.Items[i];
                DuplicateFileItem itm = lvi.Tag as DuplicateFileItem;
                if (itm != null)
                {
                    // Uncheck original, check duplicates
                    lvi.Checked = !itm.IsOriginal;
                }
            }
            _lvDuplicates.EndUpdate();
            UpdateStatusSummary();
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            if (_lvDuplicates.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a file row in the list first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ListViewItem lvi = _lvDuplicates.SelectedItems[0];
            DuplicateFileItem itm = lvi.Tag as DuplicateFileItem;
            if (itm != null && File.Exists(itm.FullPath))
            {
                try
                {
                    Process.Start("explorer.exe", string.Format("/select,\"{0}\"", itm.FullPath));
                }
                catch { }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            List<ListViewItem> checkedItems = new List<ListViewItem>();
            long totalReclaimBytes = 0;

            for (int i = 0; i < _lvDuplicates.Items.Count; i++)
            {
                ListViewItem lvi = _lvDuplicates.Items[i];
                if (lvi.Checked)
                {
                    checkedItems.Add(lvi);
                    DuplicateFileItem itm = lvi.Tag as DuplicateFileItem;
                    if (itm != null) totalReclaimBytes += itm.SizeBytes;
                }
            }

            if (checkedItems.Count == 0)
            {
                MessageBox.Show(
                    "No duplicate files are checked for deletion.\n\nUse 'Smart Auto-Select' or check the files you wish to remove.",
                    "No Files Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string recycleNote = _chkRecycleBin.Checked
                ? "Files will be safely moved to the Windows Recycle Bin (can be restored)."
                : "WARNING: Files will be PERMANENTLY deleted from disk!";

            string confirmMsg = string.Format(
                "Are you sure you want to delete {0} selected duplicate files?\n\nTotal reclaimable space: {1}\n\n{2}",
                checkedItems.Count,
                DuplicateFileItem.FormatBytes(totalReclaimBytes),
                recycleNote);

            DialogResult res = MessageBox.Show(
                confirmMsg,
                "Confirm Duplicate Deletion",
                MessageBoxButtons.YesNo,
                _chkRecycleBin.Checked ? MessageBoxIcon.Question : MessageBoxIcon.Warning);

            if (res != DialogResult.Yes) return;

            int deletedCount = 0;
            int failedCount = 0;
            bool sendToRecycle = _chkRecycleBin.Checked;

            _lvDuplicates.BeginUpdate();
            for (int j = 0; j < checkedItems.Count; j++)
            {
                ListViewItem lvi = checkedItems[j];
                DuplicateFileItem itm = lvi.Tag as DuplicateFileItem;
                if (itm != null)
                {
                    if (DuplicateEngine.DeleteFileSafe(itm.FullPath, sendToRecycle))
                    {
                        deletedCount++;
                        _lvDuplicates.Items.Remove(lvi);
                    }
                    else
                    {
                        failedCount++;
                        lvi.ForeColor = Color.DarkGray;
                        lvi.Checked = false;
                    }
                }
            }
            _lvDuplicates.EndUpdate();

            string resultMsg = string.Format("Successfully deleted {0} duplicate files and reclaimed {1}.",
                deletedCount, DuplicateFileItem.FormatBytes(totalReclaimBytes));
            if (failedCount > 0)
            {
                resultMsg += string.Format("\n\nNote: {0} files were in use or could not be accessed.", failedCount);
            }

            MessageBox.Show(resultMsg, "Cleanup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            UpdateStatusSummary();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_lvDuplicates.Items.Count == 0)
            {
                MessageBox.Show("There are no duplicate scan results to export.", "Empty List", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Report (*.csv)|*.csv|Text File (*.txt)|*.txt";
                sfd.FileName = string.Format("Pexoris_Duplicate_Report_{0:yyyyMMdd_HHmmss}.csv", DateTime.Now);
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Group,Role,File Name,Size (Bytes),Formatted Size,Modified Date,SHA-256 Hash,Folder Path,Full Path");

                        for (int i = 0; i < _lvDuplicates.Items.Count; i++)
                        {
                            ListViewItem lvi = _lvDuplicates.Items[i];
                            DuplicateFileItem itm = lvi.Tag as DuplicateFileItem;
                            if (itm != null)
                            {
                                sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\"",
                                    itm.GroupId,
                                    itm.IsOriginal ? "Original" : "Duplicate",
                                    itm.FileName.Replace("\"", "\"\""),
                                    itm.SizeBytes,
                                    itm.FormattedSize,
                                    itm.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                                    itm.Sha256Hash,
                                    itm.DirectoryName.Replace("\"", "\"\""),
                                    itm.FullPath.Replace("\"", "\"\"")));
                            }
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("Report exported successfully to:\n" + sfd.FileName, "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to save report: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void UpdateStatusSummary()
        {
            int checkedCount = 0;
            long checkedBytes = 0;

            for (int i = 0; i < _lvDuplicates.Items.Count; i++)
            {
                ListViewItem lvi = _lvDuplicates.Items[i];
                if (lvi.Checked)
                {
                    checkedCount++;
                    DuplicateFileItem itm = lvi.Tag as DuplicateFileItem;
                    if (itm != null) checkedBytes += itm.SizeBytes;
                }
            }

            _lblStatus.Text = string.Format(
                "{0} duplicate groups found • {1} files selected • {2} space reclaimable",
                _duplicateGroups.Count,
                checkedCount,
                DuplicateFileItem.FormatBytes(checkedBytes));
        }

        private static string TruncatePath(string path, int maxChars)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (path.Length <= maxChars) return path;
            return "..." + path.Substring(path.Length - maxChars);
        }
    }
}
