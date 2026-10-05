using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pexoris.PortKiller
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

        // UI Controls - Matched 1:1 with Pexoris Ecosystem Standard
        private Panel _titleBar;
        private PictureBox _picBrandIcon;
        private Label _lblTitle;
        private Label _lblClose;
        private Label _lblMin;

        private Panel _controlCard;
        private Label _lblCardIcon;
        private Label _lblCardText;
        private Label _lblCardSubText;
        private Label _lblStatusBadge;
        private TextBox _txtSearch;
        private PexorisButton _btnQuick80;
        private PexorisButton _btnQuick3000;
        private PexorisButton _btnQuick8080;
        private PexorisButton _btnQuick3306;

        private Panel _listContainer;
        private ListView _lvSockets;
        private ColumnHeader _colPort;
        private ColumnHeader _colProto;
        private ColumnHeader _colState;
        private ColumnHeader _colProcess;
        private ColumnHeader _colPid;
        private ColumnHeader _colTitle;
        private ColumnHeader _colPath;

        private Panel _actionCard;
        private PexorisButton _btnKillSelected;
        private PexorisButton _btnFreePort80;
        private PexorisButton _btnFreePort3000;
        private PexorisButton _btnStopIis;
        private PexorisButton _btnScanAgain;

        private Panel _footerBar;
        private CheckBox _chkAutoRefresh;
        private Label _lblFooterStatus;

        private List<PortItem> _cachedPorts = new List<PortItem>();
        private Timer _autoRefreshTimer;
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

        public MainForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            SetupAppIcon();
            LoadActiveSockets();
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
                if (_lvSockets != null && _lvSockets.IsHandleCreated)
                {
                    SetWindowTheme(_lvSockets.Handle, "Explorer", null);
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "Pexoris PortKiller";
            this.Size = new Size(860, 640);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = PexorisTheme.AppleCanvas;
            this.ForeColor = PexorisTheme.TextPrimary;
            this.FormBorderStyle = FormBorderStyle.None;
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

            _picBrandIcon = new PictureBox
            {
                Size = new Size(24, 24),
                Location = new Point(16, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = PexorisTheme.GetPortKillerLogoBitmap(24),
                BackColor = Color.Transparent
            };
            _picBrandIcon.MouseDown += TitleBar_MouseDown;

            _lblTitle = new Label
            {
                Text = "Pexoris PortKiller  •  Portable Utility",
                Font = PexorisTheme.FontHeader(10f),
                ForeColor = PexorisTheme.TextPrimary,
                AutoSize = false,
                Location = new Point(48, 0),
                Size = new Size(420, 44),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _lblTitle.MouseDown += TitleBar_MouseDown;

            // Standard Windows Window Controls (Matching FileUnlocker)
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

            // ================= 2. TOP CONTROL CARD (MATCHING FILEUNLOCKER) =================
            _controlCard = new Panel
            {
                Location = new Point(18, 58),
                Size = new Size(824, 120),
                BackColor = PexorisTheme.AppleWhite,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _controlCard.Paint += ControlCard_Paint;

            _lblCardIcon = new Label
            {
                Text = "⚡",
                Font = new Font("Segoe UI Emoji", 24f),
                ForeColor = PexorisTheme.AppleAmber,
                Location = new Point(18, 12),
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblCardText = new Label
            {
                Text = "Free \"Port 80/3000/8080 Already in Use\" Errors",
                Font = PexorisTheme.FontBold(10.5f),
                ForeColor = PexorisTheme.TextPrimary,
                Location = new Point(68, 13),
                Size = new Size(450, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblCardSubText = new Label
            {
                Text = "Inspects active TCP/UDP sockets via Windows IP Helper & terminates locking PID",
                Font = PexorisTheme.FontBody(8.5f),
                ForeColor = PexorisTheme.TextSecondary,
                Location = new Point(68, 35),
                Size = new Size(450, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };

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

            _txtSearch = new TextBox
            {
                Location = new Point(18, 70),
                Size = new Size(290, 32),
                BackColor = PexorisTheme.AppleWhite,
                ForeColor = PexorisTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = PexorisTheme.FontCode(9.5f)
            };
            _txtSearch.TextChanged += (s, e) => FilterSockets();

            // Preset Buttons in the Top Card
            _btnQuick80 = new PexorisButton
            {
                Text = "Port 80",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(318, 68),
                Size = new Size(118, 34)
            };
            _btnQuick80.Click += (s, e) => { _txtSearch.Text = "80"; FilterSockets(); };

            _btnQuick3000 = new PexorisButton
            {
                Text = "Port 3000",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(444, 68),
                Size = new Size(118, 34)
            };
            _btnQuick3000.Click += (s, e) => { _txtSearch.Text = "3000"; FilterSockets(); };

            _btnQuick8080 = new PexorisButton
            {
                Text = "Port 8080",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(570, 68),
                Size = new Size(118, 34)
            };
            _btnQuick8080.Click += (s, e) => { _txtSearch.Text = "8080"; FilterSockets(); };

            _btnQuick3306 = new PexorisButton
            {
                Text = "Port 3306",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(696, 68),
                Size = new Size(110, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnQuick3306.Click += (s, e) => { _txtSearch.Text = "3306"; FilterSockets(); };

            _controlCard.Controls.Add(_lblCardIcon);
            _controlCard.Controls.Add(_lblCardText);
            _controlCard.Controls.Add(_lblCardSubText);
            _controlCard.Controls.Add(_lblStatusBadge);
            _controlCard.Controls.Add(_txtSearch);
            _controlCard.Controls.Add(_btnQuick80);
            _controlCard.Controls.Add(_btnQuick3000);
            _controlCard.Controls.Add(_btnQuick8080);
            _controlCard.Controls.Add(_btnQuick3306);

            // ================= 3. SOCKETS LISTVIEW (MATCHING FILEUNLOCKER) =================
            Label lblListHeader = new Label
            {
                Text = "Processes Holding Network Sockets / Ports:",
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
                Rectangle r = new Rectangle(0, 0, _listContainer.Width - 1, _listContainer.Height - 1);
                using (Pen p = new Pen(PexorisTheme.BorderLight, 1f))
                {
                    e.Graphics.DrawRectangle(p, r);
                }
            };

            _lvSockets = new ListView
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

            _colPort = new ColumnHeader { Text = "Port", Width = 80 };
            _colProto = new ColumnHeader { Text = "Protocol", Width = 80 };
            _colState = new ColumnHeader { Text = "State", Width = 110 };
            _colProcess = new ColumnHeader { Text = "Process Name", Width = 170 };
            _colPid = new ColumnHeader { Text = "PID", Width = 75 };
            _colTitle = new ColumnHeader { Text = "Window / Title", Width = 160 };
            _colPath = new ColumnHeader { Text = "Executable Path", Width = 310 };

            _lvSockets.Columns.AddRange(new ColumnHeader[] { _colPort, _colProto, _colState, _colProcess, _colPid, _colTitle, _colPath });
            _listContainer.Controls.Add(_lvSockets);

            _lvSockets.SelectedIndexChanged += (s, e) =>
            {
                // Row selection event
            };

            // Context Menu for Process Items
            _listContextMenu = new ContextMenuStrip();
            ToolStripMenuItem menuKill = new ToolStripMenuItem("⚡ Kill Process & Free Port");
            menuKill.Click += (s, e) => TerminateSelectedProcess();
            ToolStripMenuItem menuOpenFolder = new ToolStripMenuItem("📂 Open Process File Location");
            menuOpenFolder.Click += (s, e) => OpenSelectedProcessLocation();
            _listContextMenu.Items.Add(menuKill);
            _listContextMenu.Items.Add(menuOpenFolder);
            _lvSockets.ContextMenuStrip = _listContextMenu;

            // ================= 4. ACTION BAR (MATCHING FILEUNLOCKER 1:1) =================
            _actionCard = new Panel
            {
                Location = new Point(18, 508),
                Size = new Size(824, 50),
                BackColor = PexorisTheme.AppleCanvas,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // BUTTON 1: KILL PROCESS & FREE PORT (Red Destructive - Prominent & Always Active)
            _btnKillSelected = new PexorisButton
            {
                Text = "⚡ Kill Process & Free Port",
                StyleType = PexorisButton.ButtonStyleType.DestructiveRed,
                Location = new Point(0, 6),
                Size = new Size(196, 38),
                Enabled = true
            };
            _btnKillSelected.Click += (s, e) => TerminateSelectedProcess();

            // BUTTON 2: 1-Click Free Port 80 (Apache)
            _btnFreePort80 = new PexorisButton
            {
                Text = "⚡ Free Port 80",
                StyleType = PexorisButton.ButtonStyleType.PrimaryBlue,
                Location = new Point(206, 6),
                Size = new Size(142, 38)
            };
            _btnFreePort80.Click += (s, e) => FreeSpecificPort(80);

            // BUTTON 3: 1-Click Free Port 3000 (Node)
            _btnFreePort3000 = new PexorisButton
            {
                Text = "⚡ Free Port 3000",
                StyleType = PexorisButton.ButtonStyleType.PrimaryBlue,
                Location = new Point(358, 6),
                Size = new Size(142, 38)
            };
            _btnFreePort3000.Click += (s, e) => FreeSpecificPort(3000);

            // BUTTON 4: Stop IIS / W3SVC
            _btnStopIis = new PexorisButton
            {
                Text = "🛑 Stop IIS (W3SVC)",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(510, 6),
                Size = new Size(166, 38)
            };
            _btnStopIis.Click += (s, e) => StopIisService();

            // BUTTON 5: Scan / Refresh
            _btnScanAgain = new PexorisButton
            {
                Text = "🔄 Scan",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(686, 6),
                Size = new Size(138, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnScanAgain.Click += (s, e) => LoadActiveSockets();

            _actionCard.Controls.Add(_btnKillSelected);
            _actionCard.Controls.Add(_btnFreePort80);
            _actionCard.Controls.Add(_btnFreePort3000);
            _actionCard.Controls.Add(_btnStopIis);
            _actionCard.Controls.Add(_btnScanAgain);

            // ================= 5. FOOTER STATUS BAR (MATCHING FILEUNLOCKER 1:1) =================
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

            _chkAutoRefresh = new CheckBox
            {
                Text = "Auto-refresh active sockets every 5 seconds",
                Font = PexorisTheme.FontBody(8.5f),
                ForeColor = PexorisTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(18, 9),
                Checked = true,
                Cursor = Cursors.Hand
            };
            _chkAutoRefresh.CheckedChanged += (s, e) => {
                _autoRefreshTimer.Enabled = _chkAutoRefresh.Checked;
            };

            _lblFooterStatus = new Label
            {
                Text = "Ready. 100% Standalone & Portable.",
                Font = PexorisTheme.FontBody(8.5f),
                ForeColor = PexorisTheme.TextMuted,
                Location = new Point(360, 9),
                Size = new Size(360, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            LinkLabel lnkWebsite = new LinkLabel
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
            lnkWebsite.LinkClicked += (s, e) => {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            _footerBar.Controls.Add(_chkAutoRefresh);
            _footerBar.Controls.Add(_lblFooterStatus);
            _footerBar.Controls.Add(lnkWebsite);

            // Add all components in proper Z-Order
            this.Controls.Add(_listContainer);
            this.Controls.Add(lblListHeader);
            this.Controls.Add(_actionCard);
            this.Controls.Add(_controlCard);
            this.Controls.Add(_titleBar);
            this.Controls.Add(_footerBar);

            // Auto-refresh timer
            _autoRefreshTimer = new Timer { Interval = 5000 };
            _autoRefreshTimer.Tick += (s, e) =>
            {
                if (!_txtSearch.Focused) LoadActiveSockets(silent: true);
            };
            _autoRefreshTimer.Start();
        }

        private void ControlCard_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF r = new RectangleF(0.5f, 0.5f, _controlCard.Width - 1f, _controlCard.Height - 1f);
            using (GraphicsPath p = PexorisTheme.GetRoundedPath(r, 10f))
            {
                using (Pen pen = new Pen(PexorisTheme.BorderMedium, 1f))
                {
                    e.Graphics.DrawPath(pen, p);
                }
            }
        }

        private void StatusBadge_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF r = new RectangleF(0.5f, 0.5f, _lblStatusBadge.Width - 1f, _lblStatusBadge.Height - 1f);
            using (GraphicsPath p = PexorisTheme.GetRoundedPath(r, r.Height / 2f))
            {
                using (Pen pen = new Pen(PexorisTheme.BorderMedium, 1f))
                {
                    e.Graphics.DrawPath(pen, p);
                }
            }
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void LoadActiveSockets(bool silent = false)
        {
            try
            {
                _cachedPorts = SocketHelper.GetAllActivePorts();
                FilterSockets();
                int listening = CountListening();
                if (!silent)
                {
                    _lblFooterStatus.Text = string.Format("Found {0} sockets ({1} listening ports)", _cachedPorts.Count, listening);
                }

                // Check critical dev ports
                bool port80Busy = IsPortOccupied(80);
                bool port3000Busy = IsPortOccupied(3000);

                if (port80Busy || port3000Busy)
                {
                    _lblStatusBadge.Text = string.Format("OCCUPIED ({0})", port80Busy && port3000Busy ? "PORT 80 & 3000" : (port80Busy ? "PORT 80" : "PORT 3000"));
                    _lblStatusBadge.BackColor = PexorisTheme.AppleRedLight;
                    _lblStatusBadge.ForeColor = PexorisTheme.AppleRed;
                }
                else
                {
                    _lblStatusBadge.Text = "READY";
                    _lblStatusBadge.BackColor = Color.FromArgb(243, 244, 246);
                    _lblStatusBadge.ForeColor = Color.FromArgb(75, 85, 99);
                }
                _lblStatusBadge.Invalidate();
            }
            catch (Exception ex)
            {
                _lblFooterStatus.Text = "Scan error: " + ex.Message;
            }
        }

        private bool IsPortOccupied(int port)
        {
            foreach (var p in _cachedPorts)
            {
                if (p.LocalPort == port && p.State == "LISTENING") return true;
            }
            return false;
        }

        private int CountListening()
        {
            int c = 0;
            foreach (var p in _cachedPorts) if (p.State == "LISTENING") c++;
            return c;
        }

        private void FilterSockets()
        {
            string q = _txtSearch.Text.Trim().ToLowerInvariant();
            _lvSockets.BeginUpdate();
            _lvSockets.Items.Clear();

            foreach (var item in _cachedPorts)
            {
                if (!string.IsNullOrEmpty(q))
                {
                    bool match = item.LocalPort.ToString().Contains(q) ||
                                 item.ProcessName.ToLowerInvariant().Contains(q) ||
                                 item.Protocol.ToLowerInvariant().Contains(q) ||
                                 item.State.ToLowerInvariant().Contains(q) ||
                                 item.Pid.ToString().Contains(q);
                    if (!match) continue;
                }

                ListViewItem lvi = new ListViewItem(item.LocalPort.ToString());
                lvi.SubItems.Add(item.Protocol);
                lvi.SubItems.Add(item.State);
                lvi.SubItems.Add(item.ProcessName);
                lvi.SubItems.Add(item.Pid.ToString());
                lvi.SubItems.Add(item.WindowTitle);
                lvi.SubItems.Add(item.ExecutablePath);

                lvi.Tag = item;

                if (item.LocalPort == 80 || item.LocalPort == 3000 || item.LocalPort == 8080 || item.LocalPort == 3306)
                {
                    lvi.BackColor = Color.FromArgb(254, 242, 242);
                    lvi.ForeColor = Color.FromArgb(185, 28, 28);
                }
                else if (item.State == "LISTENING")
                {
                    lvi.BackColor = Color.FromArgb(240, 253, 244);
                }

                _lvSockets.Items.Add(lvi);
            }

            _lvSockets.EndUpdate();
        }

        private void TerminateSelectedProcess()
        {
            if (_lvSockets.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    "Please select an active port or process row from the list above to kill it, or use the 1-Click Free buttons ('⚡ Free Port 80', '⚡ Free Port 3000') below.",
                    "Pexoris PortKiller",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
            PortItem item = _lvSockets.SelectedItems[0].Tag as PortItem;
            if (item == null) return;

            KillPortItem(item);
        }

        private void FreeSpecificPort(int port)
        {
            PortItem target = null;
            foreach (var p in _cachedPorts)
            {
                if (p.LocalPort == port && p.State == "LISTENING")
                {
                    target = p;
                    break;
                }
            }

            if (target == null)
            {
                MessageBox.Show(string.Format("Port {0} is already completely FREE! No process is listening on it.", port), "Port Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            KillPortItem(target);
        }

        private void KillPortItem(PortItem item)
        {
            if (item.Pid <= 4)
            {
                MessageBox.Show(
                    string.Format("PID {0} is {1}.\n\nWhen Port {2} is held by System (PID 4), it is typically IIS / W3SVC (World Wide Web Publishing Service).\n\nClick 'Stop IIS (W3SVC)' button to release Port {2}.", item.Pid, item.ProcessName, item.LocalPort),
                    "System Protected Process",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult dr = MessageBox.Show(
                string.Format("Are you sure you want to terminate process '{0}' (PID: {1}) occupying Port {2}?", item.ProcessName, item.Pid, item.LocalPort),
                "Terminate Process & Free Port",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                string err;
                if (SocketHelper.TerminateProcess(item.Pid, out err))
                {
                    _lblFooterStatus.Text = string.Format("✓ Successfully killed {0} (PID {1}). Port {2} is now FREE!", item.ProcessName, item.Pid, item.LocalPort);
                    LoadActiveSockets();
                }
                else
                {
                    MessageBox.Show("Failed to terminate process: " + err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OpenSelectedProcessLocation()
        {
            if (_lvSockets.SelectedItems.Count == 0) return;
            PortItem item = _lvSockets.SelectedItems[0].Tag as PortItem;
            if (item == null || string.IsNullOrEmpty(item.ExecutablePath) || !File.Exists(item.ExecutablePath))
            {
                MessageBox.Show("Executable path not accessible.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Process.Start("explorer.exe", string.Format("/select,\"{0}\"", item.ExecutablePath));
            }
            catch { }
        }

        private void StopIisService()
        {
            DialogResult dr = MessageBox.Show(
                "This will run 'net stop was /y' and 'net stop w3svc' to stop the Windows IIS World Wide Web Publishing Service and free Port 80 for Apache / XAMPP.\n\nProceed?",
                "Stop IIS Service",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c net stop was /y & net stop w3svc")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        Verb = "runas"
                    };
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit(5000);
                    }
                    _lblFooterStatus.Text = "✓ Sent command to stop IIS services. Refreshing sockets...";
                    LoadActiveSockets();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error stopping IIS: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
