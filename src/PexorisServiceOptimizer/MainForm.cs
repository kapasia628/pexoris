using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisServiceOptimizer
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

        private PexorisButton btnPresetSafe;
        private PexorisButton btnPresetGaming;
        private PexorisButton btnPresetRestore;

        private Label lblSection;
        private TextBox txtSearch;
        private PexorisButton btnFilterAll;
        private PexorisButton btnFilterSafe;
        private PexorisButton btnFilterDisabled;

        private Panel pnlListWrapper;
        private ListView lvServices;

        private Panel pnlActions;
        private PexorisButton btnApplySafe;
        private PexorisButton btnApplyGaming;
        private PexorisButton btnToggleSelected;
        private PexorisButton btnStartStop;
        private PexorisButton btnRefresh;

        private Panel pnlFooter;
        private CheckBox chkAutoBackup;
        private Label lblFooterStatus;
        private LinkLabel lnkBrand;

        private List<ServiceItem> _allServices = new List<ServiceItem>();
        private string _activeFilter = "ALL";

        public MainForm()
        {
            InitializeComponent();
            ApplyCustomDropShadow();
            LoadAppIcon();
            RefreshServicesList();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Pexoris Service Optimizer";
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
                Text = "Pexoris Service Optimizer",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };
            lblTitleText.MouseDown += TitleBar_MouseDown;

            lblTitleBadge = new Label
            {
                Location = new Point(230, 14),
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
                Text = "Windows Background Service Optimizer",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            lblCardSubtitle = new Label
            {
                Location = new Point(70, 40),
                Size = new Size(490, 32),
                Text = "Safely disable background telemetry, error dumps and non-essential bloatware services to reduce RAM usage and input latency.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            pillStatus = new Label
            {
                Location = new Point(600, 16),
                Size = new Size(212, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontBold,
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Theme.PrimaryBlue
            };
            pillStatus.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(191, 219, 254)))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath p = Theme.GetRoundedPath(new RectangleF(0.5f, 0.5f, pillStatus.Width - 1f, pillStatus.Height - 1f), 12f))
                    {
                        e.Graphics.DrawPath(pen, p);
                    }
                }
            };

            // Preset Buttons inside Top Card
            btnPresetSafe = new PexorisButton
            {
                Location = new Point(70, 76),
                Size = new Size(165, 32),
                Text = "⚡ Safe Profile",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            btnPresetSafe.Click += (s, e) => ApplySafeProfileClick();

            btnPresetGaming = new PexorisButton
            {
                Location = new Point(243, 76),
                Size = new Size(165, 32),
                Text = "🎮 Gaming Profile",
                Style = PexorisButtonStyle.GamerPurple
            };
            btnPresetGaming.Click += (s, e) => ApplyGamingProfileClick();

            btnPresetRestore = new PexorisButton
            {
                Location = new Point(416, 76),
                Size = new Size(175, 32),
                Text = "↩ Restore Defaults",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnPresetRestore.Click += (s, e) => RestoreDefaultsClick();

            cardTop.Controls.Add(picCardIcon);
            cardTop.Controls.Add(lblCardTitle);
            cardTop.Controls.Add(lblCardSubtitle);
            cardTop.Controls.Add(pillStatus);
            cardTop.Controls.Add(btnPresetSafe);
            cardTop.Controls.Add(btnPresetGaming);
            cardTop.Controls.Add(btnPresetRestore);

            // 3. Middle Explorer Section
            lblSection = new Label
            {
                Location = new Point(16, 186),
                AutoSize = true,
                Text = "SYSTEM SERVICES EXPLORER",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                ForeColor = Theme.TextMuted
            };

            btnFilterAll = new PexorisButton
            {
                Location = new Point(200, 182),
                Size = new Size(70, 24),
                Text = "All",
                Font = Theme.FontSmall,
                Style = PexorisButtonStyle.PrimaryBlue
            };
            btnFilterAll.Click += (s, e) => { SetFilter("ALL", btnFilterAll); };

            btnFilterSafe = new PexorisButton
            {
                Location = new Point(276, 182),
                Size = new Size(110, 24),
                Text = "Safe to Tweak",
                Font = Theme.FontSmall,
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnFilterSafe.Click += (s, e) => { SetFilter("SAFE", btnFilterSafe); };

            btnFilterDisabled = new PexorisButton
            {
                Location = new Point(392, 182),
                Size = new Size(95, 24),
                Text = "Disabled",
                Font = Theme.FontSmall,
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnFilterDisabled.Click += (s, e) => { SetFilter("DISABLED", btnFilterDisabled); };

            txtSearch = new TextBox
            {
                Location = new Point(594, 182),
                Size = new Size(250, 24),
                Font = Theme.FontRegular,
                ForeColor = Theme.TextSub,
                Text = "Search services..."
            };
            txtSearch.GotFocus += (s, e) => { if (txtSearch.Text == "Search services...") txtSearch.Text = ""; };
            txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Text = "Search services..."; };
            txtSearch.TextChanged += (s, e) => ApplyFilterAndSearch();

            // ListView Wrapper Panel
            pnlListWrapper = new Panel
            {
                Location = new Point(16, 212),
                Size = new Size(828, 286),
                BackColor = Theme.CardBg,
                Padding = new Padding(1)
            };
            pnlListWrapper.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Theme.BorderLight))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlListWrapper.Width - 1, pnlListWrapper.Height - 1);
                }
            };

            lvServices = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BorderStyle = BorderStyle.None,
                Font = Theme.FontRegular,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvServices.Columns.Add("Service Display Name", 250);
            lvServices.Columns.Add("System Name", 130);
            lvServices.Columns.Add("Startup Type", 100);
            lvServices.Columns.Add("State", 85);
            lvServices.Columns.Add("Safety Level", 110);
            lvServices.Columns.Add("Description", 145);
            lvServices.SelectedIndexChanged += LvServices_SelectedIndexChanged;
            lvServices.DoubleClick += (s, e) => ToggleSelectedService();

            pnlListWrapper.Controls.Add(lvServices);

            // 4. Action Buttons Bar (Y=508, H=50)
            pnlActions = new Panel
            {
                Location = new Point(16, 508),
                Size = new Size(828, 50),
                BackColor = Color.Transparent
            };

            btnApplySafe = new PexorisButton
            {
                Location = new Point(0, 4),
                Size = new Size(180, 40),
                Text = "⚡ Apply Safe Profile",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            btnApplySafe.Click += (s, e) => ApplySafeProfileClick();

            btnApplyGaming = new PexorisButton
            {
                Location = new Point(190, 4),
                Size = new Size(175, 40),
                Text = "🎮 Apply Gaming Profile",
                Style = PexorisButtonStyle.GamerPurple
            };
            btnApplyGaming.Click += (s, e) => ApplyGamingProfileClick();

            btnToggleSelected = new PexorisButton
            {
                Location = new Point(375, 4),
                Size = new Size(145, 40),
                Text = "⏻ Toggle Startup",
                Style = PexorisButtonStyle.SuccessGreen,
                Enabled = false
            };
            btnToggleSelected.Click += (s, e) => ToggleSelectedService();

            btnStartStop = new PexorisButton
            {
                Location = new Point(530, 4),
                Size = new Size(145, 40),
                Text = "▶ Start / ⏹ Stop",
                Style = PexorisButtonStyle.SecondaryOutline,
                Enabled = false
            };
            btnStartStop.Click += (s, e) => StartStopSelectedService();

            btnRefresh = new PexorisButton
            {
                Location = new Point(685, 4),
                Size = new Size(143, 40),
                Text = "⟳ Refresh",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnRefresh.Click += (s, e) => RefreshServicesList();

            pnlActions.Controls.Add(btnApplySafe);
            pnlActions.Controls.Add(btnApplyGaming);
            pnlActions.Controls.Add(btnToggleSelected);
            pnlActions.Controls.Add(btnStartStop);
            pnlActions.Controls.Add(btnRefresh);

            // 5. Footer (38px)
            pnlFooter = new Panel
            {
                Location = new Point(0, 602),
                Size = new Size(860, 38),
                BackColor = Theme.CardBg,
                Dock = DockStyle.Bottom
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Theme.BorderLight))
                {
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
                }
            };

            chkAutoBackup = new CheckBox
            {
                Location = new Point(16, 9),
                AutoSize = true,
                Text = "Auto-backup registry before applying tweaks",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                Checked = true
            };

            lblFooterStatus = new Label
            {
                Location = new Point(310, 10),
                Size = new Size(390, 20),
                Text = "Ready • Running as Administrator (64-bit)",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            lnkBrand = new LinkLabel
            {
                Location = new Point(730, 10),
                Size = new Size(116, 20),
                Text = "pexoris.com",
                TextAlign = ContentAlignment.MiddleRight,
                Font = Theme.FontBold,
                LinkColor = Theme.PrimaryBlue,
                ActiveLinkColor = Theme.PrimaryHover,
                VisitedLinkColor = Theme.PrimaryBlue
            };
            lnkBrand.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://pexoris.com") { UseShellExecute = true }); } catch { }
            };

            pnlFooter.Controls.Add(chkAutoBackup);
            pnlFooter.Controls.Add(lblFooterStatus);
            pnlFooter.Controls.Add(lnkBrand);

            // Add all root controls
            Controls.Add(pnlFooter);
            Controls.Add(pnlActions);
            Controls.Add(pnlListWrapper);
            Controls.Add(txtSearch);
            Controls.Add(btnFilterDisabled);
            Controls.Add(btnFilterSafe);
            Controls.Add(btnFilterAll);
            Controls.Add(lblSection);
            Controls.Add(cardTop);
            Controls.Add(pnlTitleBar);

            ResumeLayout(false);
            PerformLayout();
        }

        private void SetFilter(string filter, PexorisButton activeBtn)
        {
            _activeFilter = filter;
            btnFilterAll.Style = PexorisButtonStyle.SecondaryOutline;
            btnFilterSafe.Style = PexorisButtonStyle.SecondaryOutline;
            btnFilterDisabled.Style = PexorisButtonStyle.SecondaryOutline;

            activeBtn.Style = PexorisButtonStyle.PrimaryBlue;
            btnFilterAll.Invalidate();
            btnFilterSafe.Invalidate();
            btnFilterDisabled.Invalidate();

            ApplyFilterAndSearch();
        }

        private void ApplyCustomDropShadow()
        {
            // Handled via CreateParams CS_DROPSHADOW
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
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

        private void RefreshServicesList()
        {
            lblFooterStatus.Text = "Scanning Windows services registry...";
            Application.DoEvents();

            _allServices = ServiceHelper.ScanServices();
            ApplyFilterAndSearch();

            // Count stats
            int disabledCount = 0;
            foreach (var item in _allServices)
            {
                if (item.CurrentStartup == ServiceStartupMode.Disabled)
                    disabledCount++;
            }

            pillStatus.Text = string.Format("● {0} SERVICES ({1} DISABLED)", _allServices.Count, disabledCount);
            if (disabledCount > 6)
            {
                pillStatus.BackColor = Color.FromArgb(236, 253, 245);
                pillStatus.ForeColor = Theme.SuccessGreen;
            }
            else
            {
                pillStatus.BackColor = Color.FromArgb(239, 246, 255);
                pillStatus.ForeColor = Theme.PrimaryBlue;
            }
            pillStatus.Invalidate();

            lblFooterStatus.Text = string.Format("Ready • {0} monitored services loaded.", _allServices.Count);
        }

        private void ApplyFilterAndSearch()
        {
            lvServices.BeginUpdate();
            lvServices.Items.Clear();

            string query = (txtSearch.Text != "Search services...") ? txtSearch.Text.Trim().ToLowerInvariant() : "";

            foreach (var s in _allServices)
            {
                // Filter check
                if (_activeFilter == "SAFE" && s.Safety != ServiceSafetyLevel.Safe) continue;
                if (_activeFilter == "DISABLED" && s.CurrentStartup != ServiceStartupMode.Disabled) continue;

                // Search query check
                if (!string.IsNullOrEmpty(query))
                {
                    bool match = (s.DisplayName != null && s.DisplayName.ToLowerInvariant().Contains(query)) ||
                                 (s.ServiceName != null && s.ServiceName.ToLowerInvariant().Contains(query)) ||
                                 (s.Description != null && s.Description.ToLowerInvariant().Contains(query));
                    if (!match) continue;
                }

                ListViewItem lvi = new ListViewItem(s.DisplayName ?? s.ServiceName);
                lvi.SubItems.Add(s.ServiceName);
                lvi.SubItems.Add(s.CurrentStartup.ToString());
                lvi.SubItems.Add(s.IsRunning ? "Running" : "Stopped");

                string safetyLabel = s.Safety.ToString();
                if (s.Safety == ServiceSafetyLevel.Safe) safetyLabel = "Safe to Disable";
                else if (s.Safety == ServiceSafetyLevel.Gaming) safetyLabel = "Gaming Tweak";
                else if (s.Safety == ServiceSafetyLevel.Critical) safetyLabel = "Protected";
                lvi.SubItems.Add(safetyLabel);

                lvi.SubItems.Add(s.Description);

                // Styling
                if (s.CurrentStartup == ServiceStartupMode.Disabled)
                {
                    lvi.ForeColor = Color.FromArgb(16, 185, 129); // Green disabled (optimized)
                }
                else if (s.Safety == ServiceSafetyLevel.Critical)
                {
                    lvi.ForeColor = Color.FromArgb(100, 116, 139); // Muted protected
                }
                else if (s.IsRunning)
                {
                    lvi.ForeColor = Theme.TextHero;
                }

                lvi.Tag = s;
                lvServices.Items.Add(lvi);
            }

            lvServices.EndUpdate();
            UpdateActionButtonsState();
        }

        private void LvServices_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionButtonsState();
        }

        private void UpdateActionButtonsState()
        {
            if (lvServices.SelectedItems.Count > 0)
            {
                ServiceItem s = lvServices.SelectedItems[0].Tag as ServiceItem;
                if (s != null)
                {
                    btnToggleSelected.Enabled = (s.Safety != ServiceSafetyLevel.Critical);
                    btnStartStop.Enabled = (s.Safety != ServiceSafetyLevel.Critical);

                    if (s.CurrentStartup == ServiceStartupMode.Disabled)
                    {
                        btnToggleSelected.Text = "⏻ Enable Service";
                        btnToggleSelected.Style = PexorisButtonStyle.PrimaryBlue;
                    }
                    else
                    {
                        btnToggleSelected.Text = "⏻ Disable Service";
                        btnToggleSelected.Style = PexorisButtonStyle.DestructiveRed;
                    }

                    if (s.IsRunning)
                    {
                        btnStartStop.Text = "⏹ Stop Service";
                    }
                    else
                    {
                        btnStartStop.Text = "▶ Start Service";
                    }
                    btnToggleSelected.Invalidate();
                    btnStartStop.Invalidate();
                    return;
                }
            }

            btnToggleSelected.Enabled = false;
            btnStartStop.Enabled = false;
            btnToggleSelected.Text = "⏻ Toggle Startup";
            btnStartStop.Text = "▶ Start / ⏹ Stop";
            btnToggleSelected.Invalidate();
            btnStartStop.Invalidate();
        }

        private void ToggleSelectedService()
        {
            if (lvServices.SelectedItems.Count == 0) return;
            ServiceItem s = lvServices.SelectedItems[0].Tag as ServiceItem;
            if (s == null) return;

            if (s.Safety == ServiceSafetyLevel.Critical)
            {
                MessageBox.Show(
                    string.Format("'{0}' is a critical Windows system service. Disabling it is blocked by Pexoris to prevent Windows instability.", s.DisplayName),
                    "Protected System Service",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            PerformBackupIfEnabled();

            ServiceStartupMode target = (s.CurrentStartup == ServiceStartupMode.Disabled) ? s.DefaultStartup : ServiceStartupMode.Disabled;
            if (target == ServiceStartupMode.Disabled)
            {
                ServiceHelper.SetStartupMode(s.ServiceName, ServiceStartupMode.Disabled);
                ServiceHelper.StopServiceSafe(s.ServiceName);
                lblFooterStatus.Text = string.Format("Disabled and stopped '{0}'.", s.DisplayName);
            }
            else
            {
                ServiceHelper.SetStartupMode(s.ServiceName, target);
                lblFooterStatus.Text = string.Format("Restored '{0}' to {1}.", s.DisplayName, target);
            }

            RefreshServicesList();
        }

        private void StartStopSelectedService()
        {
            if (lvServices.SelectedItems.Count == 0) return;
            ServiceItem s = lvServices.SelectedItems[0].Tag as ServiceItem;
            if (s == null) return;

            if (s.Safety == ServiceSafetyLevel.Critical)
            {
                MessageBox.Show("Modifying critical services directly is not permitted.", "Protected Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (s.IsRunning)
            {
                lblFooterStatus.Text = string.Format("Stopping '{0}'...", s.DisplayName);
                Application.DoEvents();
                ServiceHelper.StopServiceSafe(s.ServiceName);
                lblFooterStatus.Text = string.Format("Service '{0}' stopped.", s.DisplayName);
            }
            else
            {
                lblFooterStatus.Text = string.Format("Starting '{0}'...", s.DisplayName);
                Application.DoEvents();
                ServiceHelper.StartServiceSafe(s.ServiceName);
                lblFooterStatus.Text = string.Format("Service '{0}' started.", s.DisplayName);
            }

            RefreshServicesList();
        }

        private void PerformBackupIfEnabled()
        {
            if (chkAutoBackup.Checked)
            {
                string backup = ServiceHelper.CreateBackupRegistryFile();
                if (backup != null)
                {
                    lblFooterStatus.Text = "Registry backup saved: " + Path.GetFileName(backup);
                }
            }
        }

        private void ApplySafeProfileClick()
        {
            DialogResult dr = MessageBox.Show(
                "This will safely disable telemetry, diagnostic tracking, error reporting, and legacy bloatware services (DiagTrack, dmwappushservice, RetailDemo, etc.).\n\nAudio, printing, networking, and Windows Update will remain fully functional.\n\nApply Safe Profile now?",
                "Apply Safe Profile",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            PerformBackupIfEnabled();
            lblFooterStatus.Text = "Applying Safe Profile tweaks...";
            Application.DoEvents();

            int count = ServiceHelper.ApplySafeProfile();
            RefreshServicesList();

            MessageBox.Show(
                string.Format("Safe Profile applied successfully!\n\n{0} telemetry and bloatware services were safely disabled.", count),
                "Optimization Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ApplyGamingProfileClick()
        {
            DialogResult dr = MessageBox.Show(
                "This will apply Safe Profile tweaks PLUS disable non-essential Xbox background sync, sensor monitors, and adjust Windows Search indexing to minimize in-game stutter and latency.\n\nApply Gaming Profile now?",
                "Apply Gaming Profile",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            PerformBackupIfEnabled();
            lblFooterStatus.Text = "Applying Gaming Profile tweaks...";
            Application.DoEvents();

            int count = ServiceHelper.ApplyGamingProfile();
            RefreshServicesList();

            MessageBox.Show(
                string.Format("Gaming Profile applied successfully!\n\n{0} background services were optimized for gaming.", count),
                "Gaming Profile Applied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void RestoreDefaultsClick()
        {
            DialogResult dr = MessageBox.Show(
                "This will restore all monitored Windows services back to their native factory startup configurations.\n\nProceed with full restore?",
                "Restore Windows Service Defaults",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            PerformBackupIfEnabled();
            lblFooterStatus.Text = "Restoring Windows factory defaults...";
            Application.DoEvents();

            int count = ServiceHelper.RestoreDefaults();
            RefreshServicesList();

            MessageBox.Show(
                string.Format("Windows defaults restored successfully!\n\n{0} services reset to factory configuration.", count),
                "Restore Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
