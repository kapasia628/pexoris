using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pexoris.DoHSwitcher
{
    public class MainForm : Form
    {
        // Win32 API Imports for Dragging, Drop Shadow & Explorer Theme
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
        private ComboBox _cmbAdapters;
        private PexorisButton _btnQuickCloudflare;
        private PexorisButton _btnQuickAdGuard;
        private PexorisButton _btnQuickGoogle;
        private PexorisButton _btnQuickQuad9;

        private Panel _listContainer;
        private ListView _lvProviders;
        private ColumnHeader _colName;
        private ColumnHeader _colCategory;
        private ColumnHeader _colPrimary;
        private ColumnHeader _colSecondary;
        private ColumnHeader _colDoh;
        private ColumnHeader _colPing;

        private Panel _actionCard;
        private PexorisButton _btnApplySelected;
        private PexorisButton _btnApplyAdGuard;
        private PexorisButton _btnBenchmark;
        private PexorisButton _btnRestoreDhcp;
        private PexorisButton _btnFlushDns;

        private Panel _footerBar;
        private CheckBox _chkAutoFlush;
        private Label _lblFooterStatus;
        private LinkLabel _lnkWebsite;

        private List<DnsProvider> _providers;
        private List<NetworkAdapterInfo> _adapters;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x00020000; // WS_MINIMIZEBOX
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW (Smooth Apple-style window shadow)
                return cp;
            }
        }

        public MainForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            SetupAppIcon();
            LoadAdaptersAndProviders();
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
                if (_lvProviders != null && _lvProviders.IsHandleCreated)
                {
                    SetWindowTheme(_lvProviders.Handle, "Explorer", null);
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "Pexoris DoH Switcher";
            this.Size = new Size(860, 640);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = PexorisTheme.AppleCanvas;
            this.ForeColor = PexorisTheme.TextPrimary;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = true;

            // ================= 1. WINDOWS 11 CLEAN TITLE BAR =================
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
                Image = PexorisTheme.GetDoHSwitcherLogoBitmap(24),
                BackColor = Color.Transparent
            };
            _picBrandIcon.MouseDown += TitleBar_MouseDown;

            _lblTitle = new Label
            {
                Text = "Pexoris DoH Switcher  •  Portable Utility",
                Font = PexorisTheme.FontHeader(10f),
                ForeColor = PexorisTheme.TextPrimary,
                AutoSize = false,
                Location = new Point(48, 0),
                Size = new Size(420, 44),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _lblTitle.MouseDown += TitleBar_MouseDown;

            // Standard Windows Controls (— and ✕)
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

            // ================= 2. TOP CONTROL CARD (120px) =================
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
                Text = "🔒",
                Font = new Font("Segoe UI Emoji", 24f),
                ForeColor = PexorisTheme.AppleBlue,
                Location = new Point(18, 12),
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblCardText = new Label
            {
                Text = "Encrypt DNS & Bypass ISP Website Blocks (DoH)",
                Font = PexorisTheme.FontBold(10.5f),
                ForeColor = PexorisTheme.TextPrimary,
                Location = new Point(68, 13),
                Size = new Size(450, 22),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };

            _lblCardSubText = new Label
            {
                Text = "Configures native Windows 11 DNS-over-HTTPS to block ads & protect privacy",
                Font = PexorisTheme.FontBody(8.5f),
                ForeColor = PexorisTheme.TextSecondary,
                Location = new Point(68, 35),
                Size = new Size(450, 18),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
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

            // Network Adapter Dropdown
            _cmbAdapters = new ComboBox
            {
                Location = new Point(18, 70),
                Size = new Size(290, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = PexorisTheme.FontBody(9.5f),
                BackColor = Color.White
            };
            _cmbAdapters.SelectedIndexChanged += (s, e) => RefreshCurrentDnsStatus();

            // Preset Buttons inside Card
            _btnQuickCloudflare = new PexorisButton
            {
                Text = "Cloudflare 1.1",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(318, 68),
                Size = new Size(118, 34)
            };
            _btnQuickCloudflare.Click += (s, e) => SelectAndApplyProvider("1.1.1.1");

            _btnQuickAdGuard = new PexorisButton
            {
                Text = "AdGuard (Ads)",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(444, 68),
                Size = new Size(118, 34)
            };
            _btnQuickAdGuard.Click += (s, e) => SelectAndApplyProvider("94.140.14.14");

            _btnQuickGoogle = new PexorisButton
            {
                Text = "Google 8.8",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(570, 68),
                Size = new Size(118, 34)
            };
            _btnQuickGoogle.Click += (s, e) => SelectAndApplyProvider("8.8.8.8");

            _btnQuickQuad9 = new PexorisButton
            {
                Text = "Quad9 (Safe)",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(696, 68),
                Size = new Size(110, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnQuickQuad9.Click += (s, e) => SelectAndApplyProvider("9.9.9.9");

            _controlCard.Controls.Add(_lblCardIcon);
            _controlCard.Controls.Add(_lblCardText);
            _controlCard.Controls.Add(_lblCardSubText);
            _controlCard.Controls.Add(_lblStatusBadge);
            _controlCard.Controls.Add(_cmbAdapters);
            _controlCard.Controls.Add(_btnQuickCloudflare);
            _controlCard.Controls.Add(_btnQuickAdGuard);
            _controlCard.Controls.Add(_btnQuickGoogle);
            _controlCard.Controls.Add(_btnQuickQuad9);

            // ================= 3. PROVIDERS LISTVIEW =================
            Label lblListHeader = new Label
            {
                Text = "Secure Encrypted DNS Providers & Latency Benchmark:",
                Font = PexorisTheme.FontBold(9.5f),
                ForeColor = PexorisTheme.TextPrimary,
                Location = new Point(18, 190),
                Size = new Size(450, 20)
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
                    e.Graphics.DrawRectangle(p, r);
            };

            _lvProviders = new ListView
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

            _colName = new ColumnHeader { Text = "Provider Name", Width = 190 };
            _colCategory = new ColumnHeader { Text = "Category", Width = 130 };
            _colPrimary = new ColumnHeader { Text = "Primary DNS (IPv4)", Width = 135 };
            _colSecondary = new ColumnHeader { Text = "Secondary DNS", Width = 135 };
            _colDoh = new ColumnHeader { Text = "DoH Encryption", Width = 120 };
            _colPing = new ColumnHeader { Text = "Ping (Latency)", Width = 100 };

            _lvProviders.Columns.AddRange(new ColumnHeader[] {
                _colName, _colCategory, _colPrimary, _colSecondary, _colDoh, _colPing
            });
            _listContainer.Controls.Add(_lvProviders);

            _lvProviders.DoubleClick += (s, e) => ApplySelectedProvider();

            // ================= 4. ACTION BAR (Y=508, H=50) =================
            _actionCard = new Panel
            {
                Location = new Point(18, 508),
                Size = new Size(824, 50),
                BackColor = PexorisTheme.AppleCanvas,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // BUTTON 1: Apply Selected DNS (Red Destructive Style for primary punch)
            _btnApplySelected = new PexorisButton
            {
                Text = "⚡ Apply Selected DNS",
                StyleType = PexorisButton.ButtonStyleType.DestructiveRed,
                Location = new Point(0, 6),
                Size = new Size(196, 38),
                Enabled = true
            };
            _btnApplySelected.Click += (s, e) => ApplySelectedProvider();

            // BUTTON 2: 1-Click AdGuard (Block Ads)
            _btnApplyAdGuard = new PexorisButton
            {
                Text = "🛡️ AdGuard (Block Ads)",
                StyleType = PexorisButton.ButtonStyleType.PrimaryBlue,
                Location = new Point(206, 6),
                Size = new Size(160, 38)
            };
            _btnApplyAdGuard.Click += (s, e) => SelectAndApplyProvider("94.140.14.14");

            // BUTTON 3: Benchmark Latency
            _btnBenchmark = new PexorisButton
            {
                Text = "⚡ Fastest DNS Test",
                StyleType = PexorisButton.ButtonStyleType.PrimaryBlue,
                Location = new Point(376, 6),
                Size = new Size(150, 38)
            };
            _btnBenchmark.Click += (s, e) => BenchmarkAllDns();

            // BUTTON 4: Restore DHCP Default
            _btnRestoreDhcp = new PexorisButton
            {
                Text = "🔄 Restore Default (DHCP)",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(536, 6),
                Size = new Size(160, 38)
            };
            _btnRestoreDhcp.Click += (s, e) => RestoreDefaultDhcp();

            // BUTTON 5: Flush DNS Cache
            _btnFlushDns = new PexorisButton
            {
                Text = "🔄 Flush DNS",
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                Location = new Point(706, 6),
                Size = new Size(118, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnFlushDns.Click += (s, e) =>
            {
                DnsHelper.FlushDnsCache();
                _lblFooterStatus.Text = "✓ Windows DNS cache flushed successfully.";
            };

            _actionCard.Controls.Add(_btnApplySelected);
            _actionCard.Controls.Add(_btnApplyAdGuard);
            _actionCard.Controls.Add(_btnBenchmark);
            _actionCard.Controls.Add(_btnRestoreDhcp);
            _actionCard.Controls.Add(_btnFlushDns);

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

            _chkAutoFlush = new CheckBox
            {
                Text = "Automatically flush Windows DNS cache on apply",
                Font = PexorisTheme.FontBody(8.5f),
                ForeColor = PexorisTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(18, 9),
                Checked = true,
                Cursor = Cursors.Hand
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

            _footerBar.Controls.Add(_chkAutoFlush);
            _footerBar.Controls.Add(_lblFooterStatus);
            _footerBar.Controls.Add(_lnkWebsite);

            // Add all components in proper Z-Order
            this.Controls.Add(lblListHeader);
            this.Controls.Add(_listContainer);
            this.Controls.Add(_actionCard);
            this.Controls.Add(_controlCard);
            this.Controls.Add(_titleBar);
            this.Controls.Add(_footerBar);
        }

        private void ControlCard_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF r = new RectangleF(0.5f, 0.5f, _controlCard.Width - 1f, _controlCard.Height - 1f);
            using (GraphicsPath p = PexorisTheme.GetRoundedPath(r, 10f))
            using (Pen pen = new Pen(PexorisTheme.BorderMedium, 1f))
            {
                e.Graphics.DrawPath(pen, p);
            }
        }

        private void StatusBadge_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF r = new RectangleF(0.5f, 0.5f, _lblStatusBadge.Width - 1f, _lblStatusBadge.Height - 1f);
            using (GraphicsPath p = PexorisTheme.GetRoundedPath(r, r.Height / 2f))
            using (Pen pen = new Pen(PexorisTheme.BorderMedium, 1f))
            {
                e.Graphics.DrawPath(pen, p);
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

        private void LoadAdaptersAndProviders()
        {
            try
            {
                _adapters = DnsHelper.GetActiveAdapters();
                _cmbAdapters.Items.Clear();

                foreach (var a in _adapters)
                {
                    _cmbAdapters.Items.Add(a);
                }

                if (_cmbAdapters.Items.Count > 0)
                {
                    _cmbAdapters.SelectedIndex = 0;
                }

                _providers = DnsHelper.GetCuratedProviders();
                PopulateProvidersList();
                RefreshCurrentDnsStatus();
            }
            catch (Exception ex)
            {
                _lblFooterStatus.Text = "Load error: " + ex.Message;
            }
        }

        private void PopulateProvidersList()
        {
            _lvProviders.BeginUpdate();
            _lvProviders.Items.Clear();

            foreach (var p in _providers)
            {
                ListViewItem item = new ListViewItem(p.Name);
                item.SubItems.Add(p.Category);
                item.SubItems.Add(p.PrimaryIp);
                item.SubItems.Add(p.SecondaryIp);
                item.SubItems.Add(!string.IsNullOrEmpty(p.DohTemplate) ? "✓ HTTPS (DoH)" : "Standard");
                item.SubItems.Add(p.LatencyMs >= 0 ? string.Format("{0} ms", p.LatencyMs) : "—");
                item.Tag = p;

                _lvProviders.Items.Add(item);
            }

            _lvProviders.EndUpdate();
        }

        private void RefreshCurrentDnsStatus()
        {
            NetworkAdapterInfo activeAdapter = _cmbAdapters.SelectedItem as NetworkAdapterInfo;
            if (activeAdapter == null) return;

            // Re-read adapter properties
            var refreshedAdapters = DnsHelper.GetActiveAdapters();
            foreach (var a in refreshedAdapters)
            {
                if (a.Id == activeAdapter.Id)
                {
                    activeAdapter = a;
                    break;
                }
            }

            string currentDns = activeAdapter.DnsServers.Count > 0 ? activeAdapter.DnsServers[0] : "DHCP Default";
            bool isDoh = false;

            // Highlight in ListView
            foreach (ListViewItem item in _lvProviders.Items)
            {
                DnsProvider p = item.Tag as DnsProvider;
                if (p != null && (p.PrimaryIp == currentDns || (activeAdapter.DnsServers.Contains(p.PrimaryIp))))
                {
                    item.BackColor = Color.FromArgb(240, 253, 244); // Soft Green
                    item.Selected = true;
                    isDoh = !string.IsNullOrEmpty(p.DohTemplate);
                }
                else
                {
                    item.BackColor = Color.White;
                }
            }

            if (activeAdapter.IsDhcp && activeAdapter.DnsServers.Count == 0)
            {
                _lblStatusBadge.Text = "DHCP (ISP DEFAULT)";
                _lblStatusBadge.BackColor = Color.FromArgb(243, 244, 246);
                _lblStatusBadge.ForeColor = Color.FromArgb(75, 85, 99);
                _lblFooterStatus.Text = string.Format("Active: {0} • Using ISP Default DNS (Unencrypted)", activeAdapter.Name);
            }
            else
            {
                _lblStatusBadge.Text = isDoh ? "ENCRYPTED DOH ACTIVE" : "CUSTOM DNS ACTIVE";
                _lblStatusBadge.BackColor = isDoh ? PexorisTheme.AppleGreenLight : PexorisTheme.AppleBlueLight;
                _lblStatusBadge.ForeColor = isDoh ? PexorisTheme.AppleGreen : PexorisTheme.AppleBlue;
                _lblFooterStatus.Text = string.Format("Active: {0} • DNS: {1}", activeAdapter.Name, string.Join(", ", activeAdapter.DnsServers.ToArray()));
            }

            _lblStatusBadge.Invalidate();
        }

        private void ApplySelectedProvider()
        {
            if (_lvProviders.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    "Please select a DNS provider from the list to apply, or use the 1-click preset buttons above.",
                    "Pexoris DoH Switcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            DnsProvider p = _lvProviders.SelectedItems[0].Tag as DnsProvider;
            if (p != null)
            {
                ApplyProvider(p);
            }
        }

        private void SelectAndApplyProvider(string primaryIp)
        {
            foreach (var p in _providers)
            {
                if (p.PrimaryIp == primaryIp)
                {
                    ApplyProvider(p);
                    return;
                }
            }
        }

        private void ApplyProvider(DnsProvider p)
        {
            NetworkAdapterInfo adapter = _cmbAdapters.SelectedItem as NetworkAdapterInfo;
            if (adapter == null)
            {
                MessageBox.Show("Please select an active network adapter first.", "No Adapter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string err;
            if (DnsHelper.ApplyDns(adapter.Name, p, true, out err))
            {
                if (_chkAutoFlush.Checked) DnsHelper.FlushDnsCache();
                _lblFooterStatus.Text = string.Format("✓ Successfully applied {0} ({1}). DoH Encrypted!", p.Name, p.PrimaryIp);
                RefreshCurrentDnsStatus();
            }
            else
            {
                MessageBox.Show("Failed to apply DNS: " + err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestoreDefaultDhcp()
        {
            NetworkAdapterInfo adapter = _cmbAdapters.SelectedItem as NetworkAdapterInfo;
            if (adapter == null) return;

            string err;
            if (DnsHelper.RestoreDhcp(adapter.Name, out err))
            {
                if (_chkAutoFlush.Checked) DnsHelper.FlushDnsCache();
                _lblFooterStatus.Text = string.Format("✓ Restored {0} to Automatic DHCP DNS.", adapter.Name);
                RefreshCurrentDnsStatus();
            }
            else
            {
                MessageBox.Show("Failed to restore DHCP: " + err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BenchmarkAllDns()
        {
            _btnBenchmark.Enabled = false;
            _lblFooterStatus.Text = "Benchmarking DNS latency across global servers...";

            Task.Factory.StartNew(() =>
            {
                DnsProvider fastest = null;
                long bestPing = 9999;

                foreach (var p in _providers)
                {
                    p.LatencyMs = DnsHelper.PingServer(p.PrimaryIp);
                    if (p.LatencyMs >= 0 && p.LatencyMs < bestPing)
                    {
                        bestPing = p.LatencyMs;
                        fastest = p;
                    }
                }

                // Update UI on main thread
                this.BeginInvoke(new Action(() =>
                {
                    PopulateProvidersList();
                    RefreshCurrentDnsStatus();
                    _btnBenchmark.Enabled = true;

                    if (fastest != null)
                    {
                        _lblFooterStatus.Text = string.Format("✓ Benchmark complete! Fastest DNS: {0} ({1} ms)", fastest.Name, fastest.LatencyMs);
                        DialogResult dr = MessageBox.Show(
                            string.Format("Fastest DNS found:\n\n{0}\nPrimary: {1}\nLatency: {2} ms\n\nWould you like to apply {0} right now?", fastest.Name, fastest.PrimaryIp, fastest.LatencyMs),
                            "Fastest DNS Benchmark",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (dr == DialogResult.Yes)
                        {
                            ApplyProvider(fastest);
                        }
                    }
                }));
            });
        }
    }
}
