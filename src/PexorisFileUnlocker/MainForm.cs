using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using Pexoris.Core;

namespace Pexoris.Unlocker
{
    public class MainForm : Form
    {
        // Win32 API Imports for Dragging, Drop Shadow & Taskbar Icon
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_SETICON = 0x80;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        // UI Controls
        private Panel _titleBar;
        private PictureBox _picBrandIcon;
        private Label _lblTitle;
        private Label _lblClose;
        private Label _lblMin;

        private Panel _dropZoneCard;
        private Label _lblDropIcon;
        private Label _lblDropText;
        private Label _lblDropSubText;
        private TextBox _txtFilePath;
        private PexorisButton _btnBrowseFile;
        private PexorisButton _btnBrowseFolder;
        private Label _lblStatusBadge;

        private Panel _listContainer;
        private ListView _lvProcesses;
        private ColumnHeader _colPid;
        private ColumnHeader _colName;
        private ColumnHeader _colType;
        private ColumnHeader _colTitle;
        private ColumnHeader _colPath;

        private Panel _actionCard;
        private PexorisButton _btnKillSelected;
        private PexorisButton _btnUnlockDelete;
        private PexorisButton _btnUnlockRename;
        private PexorisButton _btnDeleteOnReboot;
        private PexorisButton _btnScanAgain;

        private Panel _footerBar;
        private CheckBox _chkContextMenu;
        private Label _lblFooterStatus;
        private LinkLabel _lnkWebsite;

        private string _currentTargetPath = "";
        private ContextMenuStrip _listContextMenu;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x00020000; // WS_MINIMIZEBOX (Allows minimize from taskbar)
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW (Smooth Apple-style window shadow)
                return cp;
            }
        }

        public MainForm(string initialPath = null)
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            SetupAppIcon();
            CheckContextMenuStatus();

            if (!string.IsNullOrEmpty(initialPath) && (File.Exists(initialPath) || Directory.Exists(initialPath)))
            {
                SetTargetFile(initialPath);
            }
        }

        private void SetupAppIcon()
        {
            try
            {
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                try
                {
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                    if (!File.Exists(iconPath))
                        iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
                    if (File.Exists(iconPath))
                        this.Icon = new Icon(iconPath);
                }
                catch { }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (this.Icon != null)
            {
                SendMessage(this.Handle, WM_SETICON, (IntPtr)ICON_BIG, this.Icon.Handle);
                SendMessage(this.Handle, WM_SETICON, (IntPtr)ICON_SMALL, this.Icon.Handle);
            }

            try
            {
                if (_lvProcesses != null && _lvProcesses.IsHandleCreated)
                {
                    SetWindowTheme(_lvProcesses.Handle, "Explorer", null);
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "Pexoris FileUnlocker";
            this.Size = new Size(860, 640);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = PexorisTheme.AppleCanvas;
            this.ForeColor = PexorisTheme.TextPrimary;
            this.FormBorderStyle = FormBorderStyle.None;
            this.AllowDrop = true;
            this.ShowInTaskbar = true;

            // ================= 1. macOS STYLE CLEAN TITLE BAR =================
            _titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = PexorisTheme.AppleWhite
            };
            _titleBar.Paint += (s, e) => {
                using (Pen p = new Pen(PexorisTheme.BorderLight, 1f))
                    e.Graphics.DrawLine(p, 0, _titleBar.Height - 1, _titleBar.Width, _titleBar.Height - 1);
            };
            _titleBar.MouseDown += TitleBar_MouseDown;

            // REAL BRAND LOGO ICON (Not an emoji, but our actual high-res Pexoris Brand Logo!)
            _picBrandIcon = new PictureBox
            {
                Size = new Size(24, 24),
                Location = new Point(16, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = PexorisTheme.GetBrandLogoBitmap(24),
                BackColor = Color.Transparent
            };
            _picBrandIcon.MouseDown += TitleBar_MouseDown;

            // Clean Title Text
            _lblTitle = new Label
            {
                Text = "Pexoris FileUnlocker  •  Portable Utility",
                Font = PexorisTheme.FontHeader(10f),
                ForeColor = PexorisTheme.TextPrimary,
                AutoSize = false,
                Location = new Point(48, 0),
                Size = new Size(420, 44),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _lblTitle.MouseDown += TitleBar_MouseDown;

            // Apple Window Controls
            _lblClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                ForeColor = PexorisTheme.TextSecondary,
                Size = new Size(46, 44),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _lblClose.MouseEnter += (s, e) => { _lblClose.BackColor = PexorisTheme.AppleRed; _lblClose.ForeColor = Color.White; };
            _lblClose.MouseLeave += (s, e) => { _lblClose.BackColor = Color.Transparent; _lblClose.ForeColor = PexorisTheme.TextSecondary; };
            _lblClose.Click += (s, e) => Close();

            _lblMin = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = PexorisTheme.TextSecondary,
                Size = new Size(46, 44),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _lblMin.MouseEnter += (s, e) => { _lblMin.BackColor = Color.FromArgb(243, 244, 246); _lblMin.ForeColor = PexorisTheme.TextPrimary; };
            _lblMin.MouseLeave += (s, e) => { _lblMin.BackColor = Color.Transparent; _lblMin.ForeColor = PexorisTheme.TextSecondary; };
            _lblMin.Click += (s, e) => WindowState = FormWindowState.Minimized;

            _titleBar.Controls.Add(_picBrandIcon);
            _titleBar.Controls.Add(_lblTitle);
            _titleBar.Controls.Add(_lblMin);
            _titleBar.Controls.Add(_lblClose);

            // ================= 2. APPLE STYLE DROP ZONE CARD =================
            _dropZoneCard = new Panel
            {
                Location = new Point(18, 58),
                Size = new Size(824, 120),
                BackColor = PexorisTheme.AppleWhite,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AllowDrop = true
            };
            _dropZoneCard.Paint += DropZoneCard_Paint;
            _dropZoneCard.DragEnter += Control_DragEnter;
            _dropZoneCard.DragDrop += Control_DragDrop;
            this.DragEnter += Control_DragEnter;
            this.DragDrop += Control_DragDrop;

            _lblDropIcon = new Label
            {
                Text = "📂",
                Font = new Font("Segoe UI Emoji", 24f),
                ForeColor = PexorisTheme.AppleBlue,
                Location = new Point(18, 12),
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblDropText = new Label
            {
                Text = "Drag & Drop any locked file or folder here",
                Font = PexorisTheme.FontBold(10.5f),
                ForeColor = PexorisTheme.TextPrimary,
                Location = new Point(68, 13),
                Size = new Size(450, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblDropSubText = new Label
            {
                Text = "Inspects locking process handles via native Windows Restart Manager",
                Font = PexorisTheme.FontBody(8.5f),
                ForeColor = PexorisTheme.TextSecondary,
                Location = new Point(68, 35),
                Size = new Size(450, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Apple Capsule Pill Status Badge
            _lblStatusBadge = new Label
            {
                Text = "READY",
                Font = PexorisTheme.FontBold(8.5f),
                ForeColor = Color.FromArgb(75, 85, 99),
                BackColor = Color.FromArgb(243, 244, 246),
                Location = new Point(640, 16),
                Size = new Size(166, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _lblStatusBadge.Paint += StatusBadge_Paint;

            _txtFilePath = new TextBox
            {
                Location = new Point(18, 70),
                Size = new Size(576, 32),
                BackColor = PexorisTheme.AppleWhite,
                ForeColor = PexorisTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = PexorisTheme.FontCode(9.5f),
                ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _btnBrowseFile = new PexorisButton
            {
                Text = "Browse File",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(602, 68),
                Size = new Size(100, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnBrowseFile.Click += BtnBrowseFile_Click;

            _btnBrowseFolder = new PexorisButton
            {
                Text = "Folder...",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(708, 68),
                Size = new Size(98, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnBrowseFolder.Click += BtnBrowseFolder_Click;

            _dropZoneCard.Controls.Add(_lblDropIcon);
            _dropZoneCard.Controls.Add(_lblDropText);
            _dropZoneCard.Controls.Add(_lblDropSubText);
            _dropZoneCard.Controls.Add(_lblStatusBadge);
            _dropZoneCard.Controls.Add(_txtFilePath);
            _dropZoneCard.Controls.Add(_btnBrowseFile);
            _dropZoneCard.Controls.Add(_btnBrowseFolder);

            // ================= 3. PROCESS LISTVIEW (macOS FINDER STYLE) =================
            Label lblListHeader = new Label
            {
                Text = "Processes Holding Locks on this Resource:",
                Font = PexorisTheme.FontBold(9.5f),
                ForeColor = PexorisTheme.TextPrimary,
                Location = new Point(18, 190),
                Size = new Size(400, 20)
            };

            _listContainer = new Panel
            {
                Location = new Point(18, 214),
                Size = new Size(824, 280),
                BackColor = PexorisTheme.AppleWhite,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _listContainer.Paint += (s, e) => {
                // Crisp 1px Apple border around list container
                Rectangle r = new Rectangle(0, 0, _listContainer.Width - 1, _listContainer.Height - 1);
                using (Pen p = new Pen(PexorisTheme.BorderLight, 1f))
                {
                    e.Graphics.DrawRectangle(p, r);
                }
            };

            _lvProcesses = new ListView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = PexorisTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                Font = PexorisTheme.FontBody(9.5f)
            };

            _colPid = new ColumnHeader { Text = "PID", Width = 90 };
            _colName = new ColumnHeader { Text = "Process Name", Width = 190 };
            _colType = new ColumnHeader { Text = "Application Type", Width = 130 };
            _colTitle = new ColumnHeader { Text = "Window Title", Width = 210 };
            _colPath = new ColumnHeader { Text = "Executable Path", Width = 320 };

            _lvProcesses.Columns.AddRange(new ColumnHeader[] { _colPid, _colName, _colType, _colTitle, _colPath });
            _listContainer.Controls.Add(_lvProcesses);

            // Context Menu for Process Items
            _listContextMenu = new ContextMenuStrip();
            ToolStripMenuItem menuKill = new ToolStripMenuItem("⚡ Kill This Process");
            menuKill.Click += (s, e) => TerminateSelectedProcess();
            ToolStripMenuItem menuOpenFolder = new ToolStripMenuItem("📂 Open Process File Location");
            menuOpenFolder.Click += (s, e) => OpenSelectedProcessLocation();
            _listContextMenu.Items.Add(menuKill);
            _listContextMenu.Items.Add(menuOpenFolder);
            _lvProcesses.ContextMenuStrip = _listContextMenu;

            // ================= 4. ACTION BAR (CLEAN APPLE BUTTONS) =================
            _actionCard = new Panel
            {
                Location = new Point(18, 508),
                Size = new Size(824, 50),
                BackColor = PexorisTheme.AppleCanvas,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _btnKillSelected = new PexorisButton
            {
                Text = "⚡ Kill Process & Free",
                StyleType = PexorisButton.ButtonStyleType.DestructiveRed,
                Location = new Point(0, 6),
                Size = new Size(174, 38)
            };
            _btnKillSelected.Click += (s, e) => TerminateSelectedProcess();

            _btnUnlockDelete = new PexorisButton
            {
                Text = "🗑️ Force Unlock & Delete",
                StyleType = PexorisButton.ButtonStyleType.PrimaryBlue,
                Location = new Point(184, 6),
                Size = new Size(196, 38)
            };
            _btnUnlockDelete.Click += BtnUnlockDelete_Click;

            _btnUnlockRename = new PexorisButton
            {
                Text = "✏️ Rename / Move",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(390, 6),
                Size = new Size(140, 38)
            };
            _btnUnlockRename.Click += BtnUnlockRename_Click;

            _btnDeleteOnReboot = new PexorisButton
            {
                Text = "⏳ Delete on Reboot",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(538, 6),
                Size = new Size(154, 38)
            };
            _btnDeleteOnReboot.Click += BtnDeleteOnReboot_Click;

            _btnScanAgain = new PexorisButton
            {
                Text = "🔄 Scan",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(702, 6),
                Size = new Size(122, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnScanAgain.Click += (s, e) => ScanCurrentResource();

            _actionCard.Controls.Add(_btnKillSelected);
            _actionCard.Controls.Add(_btnUnlockDelete);
            _actionCard.Controls.Add(_btnUnlockRename);
            _actionCard.Controls.Add(_btnDeleteOnReboot);
            _actionCard.Controls.Add(_btnScanAgain);

            // ================= 5. FOOTER STATUS BAR =================
            _footerBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                BackColor = PexorisTheme.AppleWhite
            };
            _footerBar.Paint += (s, e) => {
                using (Pen p = new Pen(PexorisTheme.BorderLight, 1f))
                    e.Graphics.DrawLine(p, 0, 0, _footerBar.Width, 0);
            };

            _chkContextMenu = new CheckBox
            {
                Text = "Add 'Unlock with Pexoris' to Windows Right-Click Menu",
                Font = PexorisTheme.FontBody(8.5f),
                ForeColor = PexorisTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(18, 9),
                Cursor = Cursors.Hand
            };
            _chkContextMenu.CheckedChanged += ChkContextMenu_CheckedChanged;

            _lblFooterStatus = new Label
            {
                Text = "Ready. 100% Standalone & Portable.",
                Font = PexorisTheme.FontBody(8.5f),
                ForeColor = PexorisTheme.TextMuted,
                Location = new Point(380, 9),
                Size = new Size(310, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _lnkWebsite = new LinkLabel
            {
                Text = "pexoris.com",
                Font = PexorisTheme.FontBold(8.5f),
                LinkColor = PexorisTheme.AppleBlue,
                ActiveLinkColor = PexorisTheme.AppleBlueHover,
                VisitedLinkColor = PexorisTheme.AppleBlue,
                AutoSize = true,
                Location = new Point(740, 9),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _lnkWebsite.LinkClicked += (s, e) => {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            _footerBar.Controls.Add(_chkContextMenu);
            _footerBar.Controls.Add(_lblFooterStatus);
            _footerBar.Controls.Add(_lnkWebsite);

            // Add all controls into Form
            this.Controls.Add(lblListHeader);
            this.Controls.Add(_listContainer);
            this.Controls.Add(_actionCard);
            this.Controls.Add(_dropZoneCard);
            this.Controls.Add(_titleBar);
            this.Controls.Add(_footerBar);
        }

        private void DropZoneCard_Paint(object sender, PaintEventArgs e)
        {
            Rectangle rect = new Rectangle(0, 0, _dropZoneCard.Width - 1, _dropZoneCard.Height - 1);
            using (GraphicsPath path = PexorisTheme.GetRoundedPath(rect, 8f))
            {
                // Crisp 1px Apple border
                using (Pen pen = new Pen(PexorisTheme.BorderMedium, 1f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        private void StatusBadge_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Clear parent background first to eliminate black corners
            using (SolidBrush parentBrush = new SolidBrush(_dropZoneCard.BackColor))
            {
                e.Graphics.FillRectangle(parentBrush, _lblStatusBadge.ClientRectangle);
            }

            RectangleF rect = new RectangleF(0.5f, 0.5f, _lblStatusBadge.Width - 1f, _lblStatusBadge.Height - 1f);
            using (GraphicsPath path = PexorisTheme.GetRoundedPath(rect, 13f)) // Apple Pill Capsule
            {
                using (SolidBrush b = new SolidBrush(_lblStatusBadge.BackColor))
                {
                    e.Graphics.FillPath(b, path);
                }
                using (Pen p = new Pen(_lblStatusBadge.ForeColor, 1f))
                {
                    e.Graphics.DrawPath(p, path);
                }
            }
            TextRenderer.DrawText(e.Graphics, _lblStatusBadge.Text, _lblStatusBadge.Font, _lblStatusBadge.ClientRectangle, _lblStatusBadge.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void Control_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Control_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                SetTargetFile(files[0]);
            }
        }

        private void BtnBrowseFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select locked file to inspect";
                ofd.Filter = "All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    SetTargetFile(ofd.FileName);
                }
            }
        }

        private void BtnBrowseFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select locked folder to inspect";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    SetTargetFile(fbd.SelectedPath);
                }
            }
        }

        private void SetTargetFile(string path)
        {
            _currentTargetPath = path;
            _txtFilePath.Text = path;
            ScanCurrentResource();
        }

        private void ScanCurrentResource()
        {
            if (string.IsNullOrEmpty(_currentTargetPath) || (!File.Exists(_currentTargetPath) && !Directory.Exists(_currentTargetPath)))
            {
                _lblStatusBadge.Text = "NOT FOUND";
                _lblStatusBadge.ForeColor = PexorisTheme.AppleRed;
                _lblStatusBadge.BackColor = PexorisTheme.AppleRedLight;
                _lblStatusBadge.Invalidate();
                _lvProcesses.Items.Clear();
                return;
            }

            _lvProcesses.Items.Clear();
            _lblFooterStatus.Text = "Scanning resource locks...";
            Application.DoEvents();

            List<LockedProcessInfo> lockers = RestartManagerHelper.FindLockingProcesses(_currentTargetPath);
            bool isFileLockedDirectly = File.Exists(_currentTargetPath) && RestartManagerHelper.IsFileLocked(_currentTargetPath);

            if (lockers.Count > 0)
            {
                _lblStatusBadge.Text = "LOCKED (" + lockers.Count + " PROCESS)";
                _lblStatusBadge.ForeColor = PexorisTheme.AppleRed;
                _lblStatusBadge.BackColor = PexorisTheme.AppleRedLight;
                _lblStatusBadge.Invalidate();

                foreach (var info in lockers)
                {
                    ListViewItem item = new ListViewItem(info.ProcessId.ToString());
                    item.SubItems.Add(info.ProcessName);
                    item.SubItems.Add(info.AppType);
                    item.SubItems.Add(string.IsNullOrEmpty(info.WindowTitle) ? "-" : info.WindowTitle);
                    item.SubItems.Add(info.ExecutablePath);
                    item.Tag = info;
                    _lvProcesses.Items.Add(item);
                }

                _lblFooterStatus.Text = string.Format("Found {0} locking process(es). Select an action below.", lockers.Count);
            }
            else
            {
                if (isFileLockedDirectly)
                {
                    _lblStatusBadge.Text = "EXCLUSIVE LOCK";
                    _lblStatusBadge.ForeColor = PexorisTheme.AppleYellow;
                    _lblStatusBadge.BackColor = Color.FromArgb(254, 249, 195);
                    _lblStatusBadge.Invalidate();
                    _lblFooterStatus.Text = "File has exclusive lock (system or driver handle). 'Delete on Reboot' is recommended.";
                }
                else
                {
                    _lblStatusBadge.Text = "FREE (ACCESSIBLE)";
                    _lblStatusBadge.ForeColor = Color.FromArgb(22, 101, 52);
                    _lblStatusBadge.BackColor = PexorisTheme.AppleGreenLight;
                    _lblStatusBadge.Invalidate();
                    _lblFooterStatus.Text = "Resource is completely accessible and can be moved/deleted normally.";
                }
            }
        }

        private void TerminateSelectedProcess()
        {
            if (_lvProcesses.SelectedItems.Count == 0)
            {
                if (_lvProcesses.Items.Count > 0)
                {
                    DialogResult dr = MessageBox.Show(
                        "No specific process selected. Do you want to terminate ALL " + _lvProcesses.Items.Count + " locking processes?",
                        "Pexoris Unlocker - Terminate All", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dr == DialogResult.Yes)
                    {
                        foreach (ListViewItem item in _lvProcesses.Items)
                        {
                            LockedProcessInfo info = item.Tag as LockedProcessInfo;
                            if (info != null)
                                RestartManagerHelper.TerminateProcessById(info.ProcessId);
                        }
                        ScanCurrentResource();
                    }
                }
                else
                {
                    MessageBox.Show("No active locking process to terminate.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            foreach (ListViewItem item in _lvProcesses.SelectedItems)
            {
                LockedProcessInfo info = item.Tag as LockedProcessInfo;
                if (info != null)
                {
                    bool success = RestartManagerHelper.TerminateProcessById(info.ProcessId);
                    if (!success)
                    {
                        MessageBox.Show("Could not terminate PID " + info.ProcessId + " (" + info.ProcessName + "). Run as Administrator.",
                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            System.Threading.Thread.Sleep(500);
            ScanCurrentResource();
        }

        private void BtnUnlockDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTargetPath)) return;

            DialogResult confirm = MessageBox.Show(
                "Are you sure you want to FORCE UNLOCK and PERMANENTLY DELETE:\n\n" + _currentTargetPath + "\n\nThis will terminate any processes holding it!",
                "Pexoris Force Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            foreach (ListViewItem item in _lvProcesses.Items)
            {
                LockedProcessInfo info = item.Tag as LockedProcessInfo;
                if (info != null)
                    RestartManagerHelper.TerminateProcessById(info.ProcessId);
            }

            System.Threading.Thread.Sleep(300);

            try
            {
                if (File.Exists(_currentTargetPath))
                {
                    File.SetAttributes(_currentTargetPath, FileAttributes.Normal);
                    File.Delete(_currentTargetPath);
                }
                else if (Directory.Exists(_currentTargetPath))
                {
                    Directory.Delete(_currentTargetPath, true);
                }

                MessageBox.Show("File/Folder deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _currentTargetPath = "";
                _txtFilePath.Text = "";
                _lvProcesses.Items.Clear();
                _lblStatusBadge.Text = "DELETED";
                _lblStatusBadge.ForeColor = Color.FromArgb(22, 101, 52);
                _lblStatusBadge.BackColor = PexorisTheme.AppleGreenLight;
                _lblStatusBadge.Invalidate();
            }
            catch (Exception ex)
            {
                DialogResult sched = MessageBox.Show(
                    "Direct deletion failed (" + ex.Message + ").\n\nWould you like to schedule it for DELETION ON NEXT REBOOT?",
                    "Schedule Delete on Reboot", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (sched == DialogResult.Yes)
                {
                    BtnDeleteOnReboot_Click(sender, e);
                }
            }
        }

        private void BtnUnlockRename_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTargetPath) || (!File.Exists(_currentTargetPath) && !Directory.Exists(_currentTargetPath)))
            {
                MessageBox.Show("Please select an existing file or folder first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string currentName = Path.GetFileName(_currentTargetPath);
            string prompt = PexorisDialogs.ShowInput("Enter new name for the file or folder:", "Pexoris Rename", currentName);
            if (string.IsNullOrEmpty(prompt) || prompt == currentName) return;

            string parent = Path.GetDirectoryName(_currentTargetPath);
            string newPath = Path.Combine(parent, prompt);

            foreach (ListViewItem item in _lvProcesses.Items)
            {
                LockedProcessInfo info = item.Tag as LockedProcessInfo;
                if (info != null)
                    RestartManagerHelper.TerminateProcessById(info.ProcessId);
            }

            System.Threading.Thread.Sleep(300);

            try
            {
                if (File.Exists(_currentTargetPath))
                    File.Move(_currentTargetPath, newPath);
                else if (Directory.Exists(_currentTargetPath))
                    Directory.Move(_currentTargetPath, newPath);

                MessageBox.Show("Resource renamed successfully to:\n" + prompt, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetTargetFile(newPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to rename: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteOnReboot_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTargetPath)) return;

            bool ok = RestartManagerHelper.ScheduleDeleteOnReboot(_currentTargetPath);
            if (ok)
            {
                MessageBox.Show(
                    "Scheduled successfully!\n\nWindows will automatically delete this file before other programs start on your next reboot.",
                    "Pexoris Reboot Deletion", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _lblFooterStatus.Text = "Scheduled for deletion on next Windows boot.";
            }
            else
            {
                MessageBox.Show("Failed to register reboot deletion. Make sure to run as Administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSelectedProcessLocation()
        {
            if (_lvProcesses.SelectedItems.Count == 0) return;
            LockedProcessInfo info = _lvProcesses.SelectedItems[0].Tag as LockedProcessInfo;
            if (info != null && File.Exists(info.ExecutablePath))
            {
                try
                {
                    Process.Start("explorer.exe", "/select,\"" + info.ExecutablePath + "\"");
                }
                catch { }
            }
        }

        private void CheckContextMenuStatus()
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"*\shell\PexorisUnlocker"))
                {
                    _chkContextMenu.CheckedChanged -= ChkContextMenu_CheckedChanged;
                    _chkContextMenu.Checked = (key != null);
                    _chkContextMenu.CheckedChanged += ChkContextMenu_CheckedChanged;
                }
            }
            catch { }
        }

        private void ChkContextMenu_CheckedChanged(object sender, EventArgs e)
        {
            string exePath = Application.ExecutablePath;

            try
            {
                if (_chkContextMenu.Checked)
                {
                    using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"*\shell\PexorisUnlocker"))
                    {
                        if (key != null)
                        {
                            key.SetValue("", "Unlock with Pexoris");
                            key.SetValue("Icon", "\"" + exePath + "\"");
                            using (RegistryKey cmdKey = key.CreateSubKey("command"))
                            {
                                cmdKey.SetValue("", "\"" + exePath + "\" \"%1\"");
                            }
                        }
                    }

                    using (RegistryKey dirKey = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\PexorisUnlocker"))
                    {
                        if (dirKey != null)
                        {
                            dirKey.SetValue("", "Unlock with Pexoris");
                            dirKey.SetValue("Icon", "\"" + exePath + "\"");
                            using (RegistryKey cmdKey = dirKey.CreateSubKey("command"))
                            {
                                cmdKey.SetValue("", "\"" + exePath + "\" \"%1\"");
                            }
                        }
                    }

                    MessageBox.Show("'Unlock with Pexoris' added to Windows right-click context menu!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    try { Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\PexorisUnlocker"); } catch { }
                    try { Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\shell\PexorisUnlocker"); } catch { }

                    MessageBox.Show("Removed from Windows context menu.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Administrator privileges required to modify context menu: " + ex.Message, "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _chkContextMenu.CheckedChanged -= ChkContextMenu_CheckedChanged;
                _chkContextMenu.Checked = !_chkContextMenu.Checked;
                _chkContextMenu.CheckedChanged += ChkContextMenu_CheckedChanged;
            }
        }
    }
}
