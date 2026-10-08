using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisExifStripper
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

        private ListView _lvImages;
        private Panel _actionBar;
        private PexorisButton _btnStrip;
        private PexorisButton _btnAddFiles;
        private PexorisButton _btnAddFolder;
        private PexorisButton _btnViewTags;
        private PexorisButton _btnClear;

        private Panel _footerBar;
        private CheckBox _chkBackup;
        private Label _lblFooterStatus;
        private LinkLabel _lnkWebsite;

        private List<ImageMetadataInfo> _items = new List<ImageMetadataInfo>();
        private BackgroundWorker _worker;
        private bool _isBusy = false;

        public MainForm()
        {
            InitializeComponent();
            ApplyTaskbarIcon();
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
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

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
                Text = "Pexoris ExifStripper — Image Privacy & GPS Metadata Cleaner",
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
                Text = "Pexoris ExifStripper v1.0",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                AutoSize = true,
                Location = new Point(74, 16)
            };

            _lblSubtitle = new Label
            {
                Text = "Sanitize photos before sharing online. Purges GPS coordinates, camera serials, timestamps & EXIF tags.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                AutoSize = true,
                Location = new Point(74, 40)
            };

            _lblStatusPill = new Label
            {
                Text = "[ Ready - Drag & Drop Photos or Folders ]",
                Font = Theme.FontSmall,
                ForeColor = Theme.VioletDark,
                BackColor = Theme.VioletLight,
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4),
                Location = new Point(74, 68)
            };

            _lblStats = new Label
            {
                Text = "Queued: 0  |  GPS Identified: 0  |  Clean: 0",
                Font = Theme.FontBold,
                ForeColor = Theme.TextBody,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
                Size = new Size(320, 24),
                Location = new Point(490, 70)
            };

            _cardHeader.Controls.Add(_pbCardIcon);
            _cardHeader.Controls.Add(_lblHeadline);
            _cardHeader.Controls.Add(_lblSubtitle);
            _cardHeader.Controls.Add(_lblStatusPill);
            _cardHeader.Controls.Add(_lblStats);

            // -------------------------------------------------------------
            // ListView (Images & Metadata Inspector)
            // -------------------------------------------------------------
            _lvImages = new ListView
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
            _lvImages.Columns.Add("Image File", 210);
            _lvImages.Columns.Add("Format", 65);
            _lvImages.Columns.Add("Size", 75);
            _lvImages.Columns.Add("GPS Location", 160);
            _lvImages.Columns.Add("Camera / Device", 135);
            _lvImages.Columns.Add("Date Taken", 95);
            _lvImages.Columns.Add("Status", 100);

            _lvImages.DoubleClick += (s, e) => ShowSelectedImageTags();
            _lvImages.SelectedIndexChanged += (s, e) =>
            {
                _btnViewTags.Enabled = _lvImages.SelectedIndices.Count > 0;
            };

            // -------------------------------------------------------------
            // Action Buttons Bar (Y=508, H=48)
            // -------------------------------------------------------------
            _actionBar = new Panel
            {
                Size = new Size(828, 48),
                Location = new Point(16, 506),
                BackColor = Color.Transparent
            };

            _btnStrip = new PexorisButton
            {
                Text = "⚡ Strip All Metadata & GPS",
                Style = PexorisButtonStyle.PrimaryViolet,
                Size = new Size(240, 44),
                Location = new Point(0, 2)
            };
            _btnStrip.Click += (s, e) => ExecuteStrip();

            _btnAddFiles = new PexorisButton
            {
                Text = "Add Images...",
                Style = PexorisButtonStyle.PrimaryBlue,
                Size = new Size(130, 44),
                Location = new Point(250, 2)
            };
            _btnAddFiles.Click += (s, e) => AddFilesDialog();

            _btnAddFolder = new PexorisButton
            {
                Text = "Add Folder...",
                Style = PexorisButtonStyle.SecondaryOutline,
                Size = new Size(130, 2 + 42),
                Location = new Point(390, 2)
            };
            _btnAddFolder.Click += (s, e) => AddFolderDialog();

            _btnViewTags = new PexorisButton
            {
                Text = "View Tags",
                Style = PexorisButtonStyle.SecondaryOutline,
                Size = new Size(120, 44),
                Location = new Point(530, 2),
                Enabled = false
            };
            _btnViewTags.Click += (s, e) => ShowSelectedImageTags();

            _btnClear = new PexorisButton
            {
                Text = "Clear List",
                Style = PexorisButtonStyle.SecondaryOutline,
                Size = new Size(110, 44),
                Location = new Point(660, 2)
            };
            _btnClear.Click += (s, e) => ClearList();

            _actionBar.Controls.Add(_btnStrip);
            _actionBar.Controls.Add(_btnAddFiles);
            _actionBar.Controls.Add(_btnAddFolder);
            _actionBar.Controls.Add(_btnViewTags);
            _actionBar.Controls.Add(_btnClear);

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

            _chkBackup = new CheckBox
            {
                Text = "Create .bak backups before stripping",
                Checked = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextBody,
                AutoSize = true,
                Location = new Point(16, 10)
            };

            _lblFooterStatus = new Label
            {
                Text = "0 images queued • Drag and drop any folder or photos to scan",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                AutoSize = true,
                Location = new Point(265, 11)
            };

            _lnkWebsite = new LinkLabel
            {
                Text = "pexoris.com",
                Font = Theme.FontSmall,
                LinkColor = Theme.VioletDark,
                ActiveLinkColor = Theme.Violet,
                AutoSize = true,
                Location = new Point(775, 11)
            };
            _lnkWebsite.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            _footerBar.Controls.Add(_chkBackup);
            _footerBar.Controls.Add(_lblFooterStatus);
            _footerBar.Controls.Add(_lnkWebsite);

            // Add all controls to Form
            this.Controls.Add(_titleBar);
            this.Controls.Add(_cardHeader);
            this.Controls.Add(_lvImages);
            this.Controls.Add(_actionBar);
            this.Controls.Add(_footerBar);

            this.ResumeLayout(false);
        }

        private void DragWindow()
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (_isBusy) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                AddPaths(files);
            }
        }

        private void AddFilesDialog()
        {
            if (_isBusy) return;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Photos to Sanitize";
                ofd.Filter = "Supported Images (*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.bmp)|*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.bmp|All Files (*.*)|*.*";
                ofd.Multiselect = true;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    AddPaths(ofd.FileNames);
                }
            }
        }

        private void AddFolderDialog()
        {
            if (_isBusy) return;
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select a folder containing photos to scan and strip:";
                fbd.ShowNewFolderButton = false;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    AddPaths(new string[] { fbd.SelectedPath });
                }
            }
        }

        private void AddPaths(string[] paths)
        {
            var filesToAdd = new List<string>();

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    if (ExifHelper.IsSupportedImage(path))
                        filesToAdd.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    try
                    {
                        var found = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                        foreach (var f in found)
                        {
                            if (ExifHelper.IsSupportedImage(f))
                                filesToAdd.Add(f);
                        }
                    }
                    catch { }
                }
            }

            if (filesToAdd.Count == 0) return;

            // Inspect and populate
            foreach (var file in filesToAdd)
            {
                // check if already in list
                if (_items.Exists(x => x.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var info = ExifHelper.InspectFile(file);
                _items.Add(info);

                var lvi = new ListViewItem(info.FileName);
                lvi.SubItems.Add(info.Extension);
                lvi.SubItems.Add(ExifHelper.FormatFileSize(info.FileSize));
                lvi.SubItems.Add(info.GpsCoordinates);
                lvi.SubItems.Add(info.CameraModel);
                lvi.SubItems.Add(info.DateTaken);
                lvi.SubItems.Add(info.Status);
                lvi.Tag = info;

                if (info.HasGps)
                {
                    lvi.ForeColor = Color.FromArgb(185, 28, 28); // Highlight GPS risks in dark red
                }

                _lvImages.Items.Add(lvi);
            }

            UpdateStats();
        }

        private void ClearList()
        {
            if (_isBusy) return;
            _items.Clear();
            _lvImages.Items.Clear();
            _btnViewTags.Enabled = false;
            UpdateStats();
        }

        private void UpdateStats()
        {
            int total = _items.Count;
            int gpsCount = _items.FindAll(x => x.HasGps).Count;
            int cleanCount = _items.FindAll(x => x.Status.StartsWith("Clean") || x.Status == "Stripped OK").Count;

            _lblStats.Text = string.Format("Queued: {0}  |  GPS Risk: {1}  |  Clean: {2}", total, gpsCount, cleanCount);
            _lblFooterStatus.Text = string.Format("{0} images queued • Ready to sanitize", total);

            if (gpsCount > 0)
            {
                _lblStatusPill.Text = string.Format("[ ALERT: {0} Images Contain Exact GPS Coordinates ]", gpsCount);
                _lblStatusPill.BackColor = Color.FromArgb(254, 242, 242);
                _lblStatusPill.ForeColor = Color.FromArgb(220, 38, 38);
            }
            else if (total > 0)
            {
                _lblStatusPill.Text = string.Format("[ {0} Images Loaded - Ready to Strip ]", total);
                _lblStatusPill.BackColor = Theme.VioletLight;
                _lblStatusPill.ForeColor = Theme.VioletDark;
            }
            else
            {
                _lblStatusPill.Text = "[ Ready - Drag & Drop Photos or Folders ]";
                _lblStatusPill.BackColor = Theme.VioletLight;
                _lblStatusPill.ForeColor = Theme.VioletDark;
            }
        }

        private void ShowSelectedImageTags()
        {
            if (_lvImages.SelectedItems.Count == 0) return;
            var info = _lvImages.SelectedItems[0].Tag as ImageMetadataInfo;
            if (info == null) return;

            string details;
            if (info.DetailedTags.Count == 0)
            {
                details = "No metadata or EXIF tags found in this image file.\nIt is already completely sanitized.";
            }
            else
            {
                details = string.Join("\n", info.DetailedTags.ToArray());
            }

            MessageBox.Show(this,
                string.Format("File: {0}\nPath: {1}\nDimensions: {2} x {3}\nSize: {4}\n\nDiscovered Metadata Tags ({5}):\n-----------------------------------------\n{6}",
                    info.FileName, info.FilePath, info.Width, info.Height, ExifHelper.FormatFileSize(info.FileSize), info.MetadataTagCount, details),
                "EXIF Metadata Details — " + info.FileName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ExecuteStrip()
        {
            if (_isBusy) return;
            if (_items.Count == 0)
            {
                MessageBox.Show(this, "Please add image files or drag and drop a folder first.", "No Images Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool backup = _chkBackup.Checked;
            var confirm = MessageBox.Show(this,
                string.Format("Are you sure you want to sanitize and strip all EXIF, GPS, camera, and author metadata from {0} photos?\n\nBackup (.bak): {1}",
                    _items.Count, backup ? "Enabled" : "Disabled"),
                "Confirm Metadata Sanitization",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            _isBusy = true;
            _btnStrip.Enabled = false;
            _btnAddFiles.Enabled = false;
            _btnAddFolder.Enabled = false;
            _btnClear.Enabled = false;
            _lblStatusPill.Text = "[ Sanitizing Photos... Please Wait ]";
            _lblStatusPill.BackColor = Color.FromArgb(254, 243, 199);
            _lblStatusPill.ForeColor = Color.FromArgb(180, 83, 9);

            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.DoWork += (s, e) =>
            {
                int stripped = 0;
                int errors = 0;
                for (int i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    string err;
                    bool ok = ExifHelper.StripMetadata(item.FilePath, backup, out err);
                    if (ok)
                    {
                        stripped++;
                        item.Status = "Stripped OK";
                        item.GpsCoordinates = "None (Removed)";
                        item.CameraModel = "None (Removed)";
                        item.DateTaken = "None (Removed)";
                        item.HasGps = false;
                        item.DetailedTags.Clear();
                        item.MetadataTagCount = 0;
                    }
                    else
                    {
                        errors++;
                        item.Status = "Error: " + err;
                    }
                    _worker.ReportProgress((int)(((i + 1.0) / _items.Count) * 100), i);
                }
                e.Result = new int[] { stripped, errors };
            };

            _worker.ProgressChanged += (s, pe) =>
            {
                int idx = (int)pe.UserState;
                if (idx < _lvImages.Items.Count)
                {
                    var lvi = _lvImages.Items[idx];
                    var item = _items[idx];
                    lvi.SubItems[3].Text = item.GpsCoordinates;
                    lvi.SubItems[4].Text = item.CameraModel;
                    lvi.SubItems[5].Text = item.DateTaken;
                    lvi.SubItems[6].Text = item.Status;
                    lvi.ForeColor = item.Status == "Stripped OK" ? Theme.SuccessGreen : Color.FromArgb(185, 28, 28);
                }
            };

            _worker.RunWorkerCompleted += (s, we) =>
            {
                _isBusy = false;
                _btnStrip.Enabled = true;
                _btnAddFiles.Enabled = true;
                _btnAddFolder.Enabled = true;
                _btnClear.Enabled = true;

                int[] res = (int[])we.Result;
                int stripped = res[0];
                int errors = res[1];

                UpdateStats();
                _lblStatusPill.Text = string.Format("[ Success: {0} Photos Sanitized ]", stripped);
                _lblStatusPill.BackColor = Color.FromArgb(236, 253, 245);
                _lblStatusPill.ForeColor = Theme.SuccessGreen;

                MessageBox.Show(this,
                    string.Format("Metadata sanitization complete!\n\n• Successfully cleaned: {0} photos\n• Errors: {1}\n\nAll GPS geolocations, camera serial numbers, and private timestamps have been removed.",
                        stripped, errors),
                    "Sanitization Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            _worker.RunWorkerAsync();
        }
    }
}
