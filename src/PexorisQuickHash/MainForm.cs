using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisQuickHash
{
    public class MainForm : Form
    {
        // P/Invoke for Dragging, Drop Shadow, and Taskbar Icon
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

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

        // UI Controls
        private Panel _titleBar;
        private PictureBox _titleIcon;
        private Label _titleLabel;
        private Button _btnMin;
        private Button _btnClose;

        // Top Control Card
        private Panel _topCard;
        private PictureBox _cardIcon;
        private Label _lblHeadline;
        private Label _lblSubtitle;
        private Label _lblStatusPill;

        private Label _lblFile;
        private TextBox _txtFilePath;
        private PexorisButton _btnBrowse;

        private Label _lblCompare;
        private TextBox _txtCompare;
        private PexorisButton _btnPasteCompare;

        // ListView
        private ListView _lvHashes;
        private ColumnHeader _colAlgo;
        private ColumnHeader _colHash;
        private ColumnHeader _colBits;
        private ColumnHeader _colMatch;

        // Action Buttons Bar
        private Panel _actionBar;
        private PexorisButton _btnCalculate;
        private PexorisButton _btnCopySha256;
        private PexorisButton _btnBatch;
        private PexorisButton _btnExport;
        private PexorisButton _btnClear;

        // Footer
        private Panel _footer;
        private CheckBox _chkLowercase;
        private CheckBox _chkAutoCompare;
        private Label _lblFooterStatus;
        private LinkLabel _lnkBrand;

        // Background calculation
        private BackgroundWorker _bgWorker;
        private HashResult _currentResult;
        private ContextMenuStrip _contextMenu;

        public MainForm(string initialFilePath)
        {
            InitializeComponent();
            ApplyDoubleBuffering();

            if (!string.IsNullOrEmpty(initialFilePath) && File.Exists(initialFilePath))
            {
                _txtFilePath.Text = initialFilePath;
                StartHashCalculation();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    Icon = appIcon;
                    SendMessage(Handle, WM_SETICON, ICON_SMALL, appIcon.Handle.ToInt32());
                    SendMessage(Handle, WM_SETICON, ICON_BIG, appIcon.Handle.ToInt32());
                    if (_titleIcon != null) _titleIcon.Image = appIcon.ToBitmap();
                    if (_cardIcon != null) _cardIcon.Image = appIcon.ToBitmap();
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            ClientSize = new Size(860, 640);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            AllowDrop = true;
            Text = "Pexoris QuickHash";

            // Title Bar
            _titleBar = new Panel();
            _titleBar.Size = new Size(860, 44);
            _titleBar.Location = new Point(0, 0);
            _titleBar.BackColor = Theme.Surface;
            _titleBar.MouseDown += TitleBar_MouseDown;

            _titleIcon = new PictureBox();
            _titleIcon.Size = new Size(22, 22);
            _titleIcon.Location = new Point(14, 11);
            _titleIcon.SizeMode = PictureBoxSizeMode.Zoom;
            _titleIcon.MouseDown += TitleBar_MouseDown;

            _titleLabel = new Label();
            _titleLabel.Text = "Pexoris QuickHash • Portable Checksum & Integrity Verifier";
            _titleLabel.Font = Theme.BoldFont;
            _titleLabel.ForeColor = Theme.TextHero;
            _titleLabel.Location = new Point(44, 12);
            _titleLabel.AutoSize = true;
            _titleLabel.MouseDown += TitleBar_MouseDown;

            _btnMin = CreateCaptionButton("—", 780);
            _btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;

            _btnClose = CreateCaptionButton("✕", 820);
            _btnClose.Click += (s, e) => Close();

            _titleBar.Controls.Add(_titleIcon);
            _titleBar.Controls.Add(_titleLabel);
            _titleBar.Controls.Add(_btnMin);
            _titleBar.Controls.Add(_btnClose);

            // Top Control Card (H=124)
            _topCard = new Panel();
            _topCard.Location = new Point(16, 52);
            _topCard.Size = new Size(828, 126);
            _topCard.BackColor = Theme.Surface;
            _topCard.Paint += TopCard_Paint;

            _cardIcon = new PictureBox();
            _cardIcon.Size = new Size(42, 42);
            _cardIcon.Location = new Point(16, 14);
            _cardIcon.SizeMode = PictureBoxSizeMode.Zoom;

            _lblHeadline = new Label();
            _lblHeadline.Text = "Pexoris QuickHash";
            _lblHeadline.Font = Theme.TitleFont;
            _lblHeadline.ForeColor = Theme.TextHero;
            _lblHeadline.Location = new Point(68, 12);
            _lblHeadline.AutoSize = true;

            _lblSubtitle = new Label();
            _lblSubtitle.Text = "High-speed multi-algorithm checksum calculation and integrity comparison";
            _lblSubtitle.Font = Theme.SubtitleFont;
            _lblSubtitle.ForeColor = Theme.TextMuted;
            _lblSubtitle.Location = new Point(70, 36);
            _lblSubtitle.AutoSize = true;

            _lblStatusPill = new Label();
            _lblStatusPill.Text = "● Ready to Hash";
            _lblStatusPill.Font = Theme.BoldFont;
            _lblStatusPill.ForeColor = Theme.TextMuted;
            _lblStatusPill.BackColor = Color.FromArgb(241, 245, 249);
            _lblStatusPill.TextAlign = ContentAlignment.MiddleCenter;
            _lblStatusPill.Size = new Size(180, 26);
            _lblStatusPill.Location = new Point(634, 16);
            _lblStatusPill.Paint += StatusPill_Paint;

            // Target File Row
            _lblFile = new Label();
            _lblFile.Text = "Target File:";
            _lblFile.Font = Theme.BoldFont;
            _lblFile.ForeColor = Theme.TextHero;
            _lblFile.Location = new Point(16, 64);
            _lblFile.Size = new Size(85, 22);

            _txtFilePath = new TextBox();
            _txtFilePath.Font = Theme.BodyFont;
            _txtFilePath.Location = new Point(105, 62);
            _txtFilePath.Size = new Size(615, 24);
            _txtFilePath.AllowDrop = true;
            _txtFilePath.DragEnter += Form_DragEnter;
            _txtFilePath.DragDrop += Form_DragDrop;
            _txtFilePath.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) StartHashCalculation(); };

            _btnBrowse = new PexorisButton();
            _btnBrowse.Text = "Browse...";
            _btnBrowse.Style = ButtonStyle.SecondaryOutline;
            _btnBrowse.Location = new Point(728, 61);
            _btnBrowse.Size = new Size(86, 26);
            _btnBrowse.Click += BtnBrowse_Click;

            // Compare Row
            _lblCompare = new Label();
            _lblCompare.Text = "Compare:";
            _lblCompare.Font = Theme.BoldFont;
            _lblCompare.ForeColor = Theme.TextHero;
            _lblCompare.Location = new Point(16, 94);
            _lblCompare.Size = new Size(85, 22);

            _txtCompare = new TextBox();
            _txtCompare.Font = Theme.CodeFont;
            _txtCompare.Location = new Point(105, 92);
            _txtCompare.Size = new Size(615, 24);
            _txtCompare.TextChanged += TxtCompare_TextChanged;

            _btnPasteCompare = new PexorisButton();
            _btnPasteCompare.Text = "Paste";
            _btnPasteCompare.Style = ButtonStyle.SecondaryOutline;
            _btnPasteCompare.Location = new Point(728, 91);
            _btnPasteCompare.Size = new Size(86, 26);
            _btnPasteCompare.Click += (s, e) =>
            {
                if (Clipboard.ContainsText())
                {
                    _txtCompare.Text = Clipboard.GetText().Trim();
                }
            };

            _topCard.Controls.Add(_cardIcon);
            _topCard.Controls.Add(_lblHeadline);
            _topCard.Controls.Add(_lblSubtitle);
            _topCard.Controls.Add(_lblStatusPill);
            _topCard.Controls.Add(_lblFile);
            _topCard.Controls.Add(_txtFilePath);
            _topCard.Controls.Add(_btnBrowse);
            _topCard.Controls.Add(_lblCompare);
            _topCard.Controls.Add(_txtCompare);
            _topCard.Controls.Add(_btnPasteCompare);

            // Explorer ListView
            _lvHashes = new ListView();
            _lvHashes.Location = new Point(16, 186);
            _lvHashes.Size = new Size(828, 312);
            _lvHashes.View = View.Details;
            _lvHashes.FullRowSelect = true;
            _lvHashes.GridLines = true;
            _lvHashes.Font = Theme.BodyFont;
            _lvHashes.BorderStyle = BorderStyle.FixedSingle;
            _lvHashes.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _lvHashes.DoubleClick += LvHashes_DoubleClick;

            _colAlgo = new ColumnHeader();
            _colAlgo.Text = "Algorithm";
            _colAlgo.Width = 100;

            _colHash = new ColumnHeader();
            _colHash.Text = "Calculated Checksum / Hash";
            _colHash.Width = 490;

            _colBits = new ColumnHeader();
            _colBits.Text = "Bits";
            _colBits.Width = 75;

            _colMatch = new ColumnHeader();
            _colMatch.Text = "Verification Match";
            _colMatch.Width = 155;

            _lvHashes.Columns.AddRange(new ColumnHeader[] { _colAlgo, _colHash, _colBits, _colMatch });

            // Initialize placeholder items
            AddEmptyHashRow("CRC32", "32-bit");
            AddEmptyHashRow("MD5", "128-bit");
            AddEmptyHashRow("SHA-1", "160-bit");
            AddEmptyHashRow("SHA-256", "256-bit");
            AddEmptyHashRow("SHA-384", "384-bit");
            AddEmptyHashRow("SHA-512", "512-bit");

            // Context Menu
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("Copy Checksum (Hash)", null, (s, e) => CopySelectedHash());
            _contextMenu.Items.Add("Copy Full Row", null, (s, e) => CopySelectedRow());
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("Lookup SHA-256 on VirusTotal", null, (s, e) => LookupVirusTotal());
            _lvHashes.ContextMenuStrip = _contextMenu;

            // Action Buttons Bar (Y=508, H=50)
            _actionBar = new Panel();
            _actionBar.Location = new Point(16, 506);
            _actionBar.Size = new Size(828, 50);
            _actionBar.BackColor = Color.Transparent;

            _btnCalculate = new PexorisButton();
            _btnCalculate.Text = "⚡ Calculate All Hashes";
            _btnCalculate.Style = ButtonStyle.DestructiveRed; // Rose Fuchsia
            _btnCalculate.Location = new Point(0, 8);
            _btnCalculate.Size = new Size(185, 36);
            _btnCalculate.Click += (s, e) => StartHashCalculation();

            _btnCopySha256 = new PexorisButton();
            _btnCopySha256.Text = "📋 Copy SHA-256";
            _btnCopySha256.Style = ButtonStyle.PrimaryBlue;
            _btnCopySha256.Location = new Point(195, 8);
            _btnCopySha256.Size = new Size(145, 36);
            _btnCopySha256.Click += (s, e) =>
            {
                if (_currentResult != null && !string.IsNullOrEmpty(_currentResult.SHA256))
                {
                    Clipboard.SetText(_currentResult.SHA256);
                    SetStatus("SHA-256 copied to clipboard!", false);
                }
            };

            _btnBatch = new PexorisButton();
            _btnBatch.Text = "📁 Batch Folder Hash";
            _btnBatch.Style = ButtonStyle.PrimaryBlue;
            _btnBatch.Location = new Point(350, 8);
            _btnBatch.Size = new Size(165, 36);
            _btnBatch.Click += BtnBatch_Click;

            _btnExport = new PexorisButton();
            _btnExport.Text = "💾 Export Report";
            _btnExport.Style = ButtonStyle.SecondaryOutline;
            _btnExport.Location = new Point(525, 8);
            _btnExport.Size = new Size(145, 36);
            _btnExport.Click += BtnExport_Click;

            _btnClear = new PexorisButton();
            _btnClear.Text = "🔄 Clear All";
            _btnClear.Style = ButtonStyle.SecondaryOutline;
            _btnClear.Location = new Point(680, 8);
            _btnClear.Size = new Size(148, 36);
            _btnClear.Click += (s, e) => ClearAll();

            _actionBar.Controls.Add(_btnCalculate);
            _actionBar.Controls.Add(_btnCopySha256);
            _actionBar.Controls.Add(_btnBatch);
            _actionBar.Controls.Add(_btnExport);
            _actionBar.Controls.Add(_btnClear);

            // Footer (Y=566, H=38)
            _footer = new Panel();
            _footer.Location = new Point(0, 564);
            _footer.Size = new Size(860, 76);
            _footer.BackColor = Theme.Surface;
            _footer.Paint += Footer_Paint;

            _chkLowercase = new CheckBox();
            _chkLowercase.Text = "Lowercase Hex";
            _chkLowercase.Font = Theme.SubtitleFont;
            _chkLowercase.ForeColor = Theme.TextHero;
            _chkLowercase.Location = new Point(20, 10);
            _chkLowercase.AutoSize = true;
            _chkLowercase.CheckedChanged += (s, e) =>
            {
                if (_currentResult != null) StartHashCalculation();
            };

            _chkAutoCompare = new CheckBox();
            _chkAutoCompare.Text = "Auto-Check Clipboard";
            _chkAutoCompare.Font = Theme.SubtitleFont;
            _chkAutoCompare.ForeColor = Theme.TextHero;
            _chkAutoCompare.Location = new Point(135, 10);
            _chkAutoCompare.AutoSize = true;
            _chkAutoCompare.Checked = true;

            _lblFooterStatus = new Label();
            _lblFooterStatus.Text = "Drag & drop any file above to instantly calculate hashes • Standalone portable tool";
            _lblFooterStatus.Font = Theme.SubtitleFont;
            _lblFooterStatus.ForeColor = Theme.TextMuted;
            _lblFooterStatus.Location = new Point(285, 12);
            _lblFooterStatus.Size = new Size(460, 20);

            _lnkBrand = new LinkLabel();
            _lnkBrand.Text = "pexoris.com";
            _lnkBrand.Font = Theme.BoldFont;
            _lnkBrand.LinkColor = Theme.PrimaryRose;
            _lnkBrand.Location = new Point(765, 11);
            _lnkBrand.AutoSize = true;
            _lnkBrand.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com/tools/quickhash/"); } catch { }
            };

            _footer.Controls.Add(_chkLowercase);
            _footer.Controls.Add(_chkAutoCompare);
            _footer.Controls.Add(_lblFooterStatus);
            _footer.Controls.Add(_lnkBrand);

            // Background Worker
            _bgWorker = new BackgroundWorker();
            _bgWorker.WorkerReportsProgress = true;
            _bgWorker.WorkerSupportsCancellation = true;
            _bgWorker.DoWork += BgWorker_DoWork;
            _bgWorker.ProgressChanged += BgWorker_ProgressChanged;
            _bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;

            // Form Drag and Drop
            DragEnter += Form_DragEnter;
            DragDrop += Form_DragDrop;

            // Add all controls to form
            Controls.Add(_titleBar);
            Controls.Add(_topCard);
            Controls.Add(_lvHashes);
            Controls.Add(_actionBar);
            Controls.Add(_footer);

            ResumeLayout(false);
        }

        private void AddEmptyHashRow(string algo, string bits)
        {
            ListViewItem item = new ListViewItem(algo);
            item.SubItems.Add("—");
            item.SubItems.Add(bits);
            item.SubItems.Add("—");
            item.Tag = algo;
            _lvHashes.Items.Add(item);
        }

        private Button CreateCaptionButton(string text, int x)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            btn.Size = new Size(36, 28);
            btn.Location = new Point(x, 8);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Theme.TextMuted;
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(241, 245, 249); btn.ForeColor = Theme.TextHero; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; btn.ForeColor = Theme.TextMuted; };
            return btn;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void TopCard_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(0, 0, _topCard.Width - 1, _topCard.Height - 1);
            using (GraphicsPath path = Theme.GetRoundedRectangle(rect, 10f))
            {
                using (Pen pen = new Pen(Theme.Border, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private void StatusPill_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(0, 0, _lblStatusPill.Width - 1, _lblStatusPill.Height - 1);
            using (GraphicsPath path = Theme.GetRoundedRectangle(rect, 6f))
            {
                using (Pen pen = new Pen(Theme.Border, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private void Footer_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Pen p = new Pen(Theme.Border, 1f))
            {
                g.DrawLine(p, 0, 0, _footer.Width, 0);
            }
        }

        private void Form_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Form_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                _txtFilePath.Text = files[0];
                StartHashCalculation();
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select File to Verify & Calculate Hashes";
                ofd.Filter = "All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _txtFilePath.Text = ofd.FileName;
                    StartHashCalculation();
                }
            }
        }

        private void StartHashCalculation()
        {
            string path = _txtFilePath.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please choose a file or drag-and-drop a file to calculate hashes.", "Pexoris QuickHash", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!File.Exists(path))
            {
                MessageBox.Show("The specified file does not exist: " + path, "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_chkAutoCompare.Checked && string.IsNullOrEmpty(_txtCompare.Text) && Clipboard.ContainsText())
            {
                string clip = Clipboard.GetText().Trim();
                if (HashEngine.DetectAlgorithm(clip) != string.Empty)
                {
                    _txtCompare.Text = clip;
                }
            }

            _btnCalculate.Enabled = false;
            _btnBrowse.Enabled = false;
            SetStatusPill("⚡ Computing...", Theme.PrimaryRose, Theme.PrimaryRoseLight);
            SetStatus("Calculating streaming hashes for: " + Path.GetFileName(path), false);

            bool lowercase = _chkLowercase.Checked;
            _bgWorker.RunWorkerAsync(new object[] { path, lowercase });
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] args = (object[])e.Argument;
            string file = (string)args[0];
            bool lowercase = (bool)args[1];

            HashResult result = HashEngine.ComputeAllHashes(file, (pct) =>
            {
                _bgWorker.ReportProgress(pct);
            }, lowercase);

            e.Result = result;
        }

        private void BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            SetStatusPill(string.Format("⚡ Computing ({0}%)...", e.ProgressPercentage), Theme.PrimaryRose, Theme.PrimaryRoseLight);
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _btnCalculate.Enabled = true;
            _btnBrowse.Enabled = true;

            if (e.Error != null)
            {
                SetStatusPill("✕ Read Error", Theme.DestructiveRed, Theme.DestructiveLight);
                SetStatus("Error calculating hash: " + e.Error.Message, true);
                MessageBox.Show("Failed to calculate hashes: " + e.Error.Message, "Calculation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _currentResult = (HashResult)e.Result;
            PopulateHashListView(_currentResult);
            RunVerificationComparison();

            SetStatus(string.Format("Completed in {0} ms • Size: {1}", _currentResult.ElapsedMs, _currentResult.FormattedSize), false);
        }

        private void PopulateHashListView(HashResult r)
        {
            _lvHashes.BeginUpdate();
            _lvHashes.Items.Clear();

            AddHashItem("CRC32", r.CRC32, "32-bit");
            AddHashItem("MD5", r.MD5, "128-bit");
            AddHashItem("SHA-1", r.SHA1, "160-bit");
            AddHashItem("SHA-256", r.SHA256, "256-bit");
            AddHashItem("SHA-384", r.SHA384, "384-bit");
            AddHashItem("SHA-512", r.SHA512, "512-bit");

            _lvHashes.EndUpdate();
        }

        private void AddHashItem(string algo, string hash, string bits)
        {
            ListViewItem item = new ListViewItem(algo);
            item.SubItems.Add(hash);
            item.SubItems.Add(bits);
            item.SubItems.Add("Ready");
            item.Tag = algo;
            _lvHashes.Items.Add(item);
        }

        private void TxtCompare_TextChanged(object sender, EventArgs e)
        {
            RunVerificationComparison();
        }

        private void RunVerificationComparison()
        {
            string expected = _txtCompare.Text.Trim();
            if (_currentResult == null)
            {
                if (!string.IsNullOrEmpty(expected))
                {
                    string algo = HashEngine.DetectAlgorithm(expected);
                    if (!string.IsNullOrEmpty(algo))
                        SetStatusPill("Input: " + algo, Theme.PrimaryBlue, Theme.BlueLight);
                }
                return;
            }

            if (string.IsNullOrEmpty(expected))
            {
                SetStatusPill("● Ready", Theme.TextHero, Theme.Background);
                foreach (ListViewItem item in _lvHashes.Items)
                {
                    item.SubItems[3].Text = "No test hash";
                    item.BackColor = Color.White;
                }
                return;
            }

            string matchedAlgo;
            bool isMatch = HashEngine.CompareHash(expected, _currentResult, out matchedAlgo);

            if (isMatch)
            {
                SetStatusPill("✓ Match: " + matchedAlgo, Theme.SuccessGreen, Theme.SuccessLight);
                SetStatus("VERIFIED MATCH: Expected hash matches calculated " + matchedAlgo + "!", false);

                foreach (ListViewItem item in _lvHashes.Items)
                {
                    if (item.Text.Equals(matchedAlgo, StringComparison.OrdinalIgnoreCase))
                    {
                        item.SubItems[3].Text = "✓ MATCH VERIFIED";
                        item.BackColor = Color.FromArgb(236, 253, 245); // Light green
                    }
                    else
                    {
                        item.SubItems[3].Text = "Different Algo";
                        item.BackColor = Color.White;
                    }
                }
            }
            else
            {
                string expectedAlgo = HashEngine.DetectAlgorithm(expected);
                string msg = !string.IsNullOrEmpty(expectedAlgo) ? ("✕ MISMATCH (" + expectedAlgo + ")") : "✕ MISMATCH";
                SetStatusPill(msg, Theme.DestructiveRed, Theme.DestructiveLight);
                SetStatus("MISMATCH: The expected hash does NOT match any calculated hash for this file!", true);

                foreach (ListViewItem item in _lvHashes.Items)
                {
                    if (!string.IsNullOrEmpty(expectedAlgo) && item.Text.Equals(expectedAlgo, StringComparison.OrdinalIgnoreCase))
                    {
                        item.SubItems[3].Text = "✕ MISMATCH";
                        item.BackColor = Color.FromArgb(254, 242, 242); // Light red
                    }
                    else
                    {
                        item.SubItems[3].Text = "Mismatch";
                        item.BackColor = Color.White;
                    }
                }
            }
        }

        private void SetStatusPill(string text, Color textColor, Color bgColor)
        {
            _lblStatusPill.Text = text;
            _lblStatusPill.ForeColor = textColor;
            _lblStatusPill.BackColor = bgColor;
            _lblStatusPill.Invalidate();
        }

        private void SetStatus(string text, bool isError)
        {
            _lblFooterStatus.Text = text;
            _lblFooterStatus.ForeColor = isError ? Theme.DestructiveRed : Theme.TextHero;
        }

        private void ClearAll()
        {
            _txtFilePath.Clear();
            _txtCompare.Clear();
            _currentResult = null;
            _lvHashes.Items.Clear();
            AddEmptyHashRow("CRC32", "32-bit");
            AddEmptyHashRow("MD5", "128-bit");
            AddEmptyHashRow("SHA-1", "160-bit");
            AddEmptyHashRow("SHA-256", "256-bit");
            AddEmptyHashRow("SHA-384", "384-bit");
            AddEmptyHashRow("SHA-512", "512-bit");
            SetStatusPill("● Ready to Hash", Theme.TextMuted, Color.FromArgb(241, 245, 249));
            SetStatus("Cleared all inputs and results.", false);
        }

        private void LvHashes_DoubleClick(object sender, EventArgs e)
        {
            CopySelectedHash();
        }

        private void CopySelectedHash()
        {
            if (_lvHashes.SelectedItems.Count > 0)
            {
                string hash = _lvHashes.SelectedItems[0].SubItems[1].Text;
                if (!string.IsNullOrEmpty(hash) && hash != "—")
                {
                    Clipboard.SetText(hash);
                    SetStatus(string.Format("{0} hash copied to clipboard!", _lvHashes.SelectedItems[0].Text), false);
                }
            }
        }

        private void CopySelectedRow()
        {
            if (_lvHashes.SelectedItems.Count > 0)
            {
                ListViewItem item = _lvHashes.SelectedItems[0];
                string row = string.Format("{0}: {1} ({2}) [{3}]", item.SubItems[0].Text, item.SubItems[1].Text, item.SubItems[2].Text, item.SubItems[3].Text);
                Clipboard.SetText(row);
                SetStatus("Row data copied to clipboard!", false);
            }
        }

        private void LookupVirusTotal()
        {
            if (_currentResult != null && !string.IsNullOrEmpty(_currentResult.SHA256))
            {
                try
                {
                    string url = "https://www.virustotal.com/gui/file/" + _currentResult.SHA256;
                    Process.Start(url);
                }
                catch { }
            }
            else
            {
                MessageBox.Show("Please calculate a file's SHA-256 hash first.", "VirusTotal Lookup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnBatch_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select a folder to batch calculate checksums for all files";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string[] files = Directory.GetFiles(fbd.SelectedPath, "*.*", SearchOption.TopDirectoryOnly);
                    if (files.Length == 0)
                    {
                        MessageBox.Show("No files found in selected folder.", "Batch Hash", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    if (MessageBox.Show(string.Format("Found {0} files. Compute hashes and export to CSV?", files.Length), "Batch Folder Hash", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        List<HashResult> batchResults = new List<HashResult>();
                        bool lowercase = _chkLowercase.Checked;

                        using (SaveFileDialog sfd = new SaveFileDialog())
                        {
                            sfd.Title = "Save Batch Hash CSV Report";
                            sfd.Filter = "CSV File (*.csv)|*.csv";
                            sfd.FileName = "Pexoris_QuickHash_Batch_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                            if (sfd.ShowDialog() == DialogResult.OK)
                            {
                                int count = 0;
                                foreach (string f in files)
                                {
                                    try
                                    {
                                        HashResult r = HashEngine.ComputeAllHashes(f, null, lowercase);
                                        batchResults.Add(r);
                                        count++;
                                    }
                                    catch { }
                                }

                                string csv = HashEngine.ExportToCsv(batchResults);
                                File.WriteAllText(sfd.FileName, csv);
                                SetStatus(string.Format("Batch complete! {0} files hashed and exported to CSV.", count), false);
                                MessageBox.Show(string.Format("Successfully hashed {0} files and saved to:\n{1}", count, sfd.FileName), "Batch Hash Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_currentResult == null)
            {
                MessageBox.Show("Please calculate hashes for a file first before exporting.", "Pexoris QuickHash", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Export Hash Integrity Report";
                sfd.Filter = "Text Report (*.txt)|*.txt|CSV File (*.csv)|*.csv";
                sfd.FileName = "QuickHash_" + Path.GetFileNameWithoutExtension(_currentResult.FileName) + "_" + DateTime.Now.ToString("yyyyMMdd");

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (sfd.FilterIndex == 1)
                    {
                        string report = HashEngine.ExportToTextReport(_currentResult);
                        File.WriteAllText(sfd.FileName, report);
                    }
                    else
                    {
                        List<HashResult> list = new List<HashResult>();
                        list.Add(_currentResult);
                        string csv = HashEngine.ExportToCsv(list);
                        File.WriteAllText(sfd.FileName, csv);
                    }
                    SetStatus("Report exported to: " + Path.GetFileName(sfd.FileName), false);
                }
            }
        }

        private void ApplyDoubleBuffering()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
        }
    }
}
