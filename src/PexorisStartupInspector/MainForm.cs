using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisStartupInspector
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

        private PexorisButton btnQuickClean;
        private PexorisButton btnOpenStartupFolder;
        private PexorisButton btnOpenRegedit;

        private Label lblSection;
        private TextBox txtSearch;
        private PexorisButton btnFilterAll;
        private PexorisButton btnFilterSafe;
        private PexorisButton btnFilterRegistry;
        private PexorisButton btnFilterFolder;
        private PexorisButton btnFilterDisabled;

        private Panel pnlListWrapper;
        private ListView lvItems;

        private Panel pnlActions;
        private PexorisButton btnDisableSelected;
        private PexorisButton btnEnableSelected;
        private PexorisButton btnDeleteSelected;
        private PexorisButton btnOpenFileLocation;
        private PexorisButton btnRefresh;

        private Panel pnlFooter;
        private CheckBox chkAutoBackup;
        private Label lblFooterStatus;
        private LinkLabel lnkBrand;

        private ContextMenuStrip ctxMenu;

        private List<StartupItem> _allItems = new List<StartupItem>();
        private string _activeFilter = "ALL";

        public MainForm()
        {
            InitializeComponent();
            ApplyCustomDropShadow();
            LoadAppIcon();
            RefreshStartupList();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Pexoris Startup Inspector";
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
                Text = "Pexoris Startup Inspector",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero,
                Location = new Point(44, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            lblTitleText.MouseDown += TitleBar_MouseDown;

            lblTitleBadge = new Label
            {
                Text = "v1.0 • Portable",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                BackColor = Color.FromArgb(241, 245, 249),
                Location = new Point(220, 12),
                Padding = new Padding(6, 2, 6, 2),
                AutoSize = true
            };
            lblTitleBadge.MouseDown += TitleBar_MouseDown;

            btnMinimize = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Theme.TextSub,
                Size = new Size(40, 44),
                Location = new Point(772, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnMinimize.MouseEnter += (s, e) => { btnMinimize.BackColor = Color.FromArgb(241, 245, 249); btnMinimize.ForeColor = Theme.TextHero; };
            btnMinimize.MouseLeave += (s, e) => { btnMinimize.BackColor = Color.Transparent; btnMinimize.ForeColor = Theme.TextSub; };
            btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;

            btnClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Theme.TextSub,
                Size = new Size(44, 44),
                Location = new Point(816, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Theme.DestructiveRed; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = Theme.TextSub; };
            btnClose.Click += (s, e) => Close();

            pnlTitleBar.Controls.Add(picTitleIcon);
            pnlTitleBar.Controls.Add(lblTitleText);
            pnlTitleBar.Controls.Add(lblTitleBadge);
            pnlTitleBar.Controls.Add(btnMinimize);
            pnlTitleBar.Controls.Add(btnClose);

            // 2. Top Control Card (120px)
            cardTop = new Panel
            {
                Location = new Point(16, 54),
                Size = new Size(828, 114),
                BackColor = Theme.CardBg
            };
            cardTop.Paint += CardTop_Paint;

            picCardIcon = new PictureBox
            {
                Location = new Point(16, 16),
                Size = new Size(46, 46),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            lblCardTitle = new Label
            {
                Text = "Windows Startup & Autorun Inspector",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                Location = new Point(72, 16),
                AutoSize = true
            };

            lblCardSubtitle = new Label
            {
                Text = "Inspect hidden boot delays, background telemetry updaters & startup bloatware.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                Location = new Point(74, 42),
                AutoSize = true
            };

            pillStatus = new Label
            {
                Text = "● Ready • 0 Items",
                Font = Theme.FontSmall,
                ForeColor = Color.FromArgb(79, 70, 229),
                BackColor = Color.FromArgb(238, 242, 255),
                Location = new Point(660, 16),
                Size = new Size(150, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pillStatus.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(199, 210, 254)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pillStatus.Width - 1, pillStatus.Height - 1);
                }
            };

            btnQuickClean = new PexorisButton
            {
                Text = "⚡ Disable Safe Bloatware",
                Style = PexorisButtonStyle.PrimaryBlue,
                Location = new Point(74, 72),
                Size = new Size(185, 30),
                CornerRadius = 6f
            };
            btnQuickClean.Click += BtnQuickClean_Click;

            btnOpenStartupFolder = new PexorisButton
            {
                Text = "📁 Open Startup Folder",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(267, 72),
                Size = new Size(160, 30),
                CornerRadius = 6f
            };
            btnOpenStartupFolder.Click += BtnOpenStartupFolder_Click;

            btnOpenRegedit = new PexorisButton
            {
                Text = "🛠 Open Registry Run",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(435, 72),
                Size = new Size(150, 30),
                CornerRadius = 6f
            };
            btnOpenRegedit.Click += BtnOpenRegedit_Click;

            cardTop.Controls.Add(picCardIcon);
            cardTop.Controls.Add(lblCardTitle);
            cardTop.Controls.Add(lblCardSubtitle);
            cardTop.Controls.Add(pillStatus);
            cardTop.Controls.Add(btnQuickClean);
            cardTop.Controls.Add(btnOpenStartupFolder);
            cardTop.Controls.Add(btnOpenRegedit);

            // 3. Filter Row + Search (Y=176, H=34)
            lblSection = new Label
            {
                Text = "STARTUP LOCATIONS & AUTORUN ENTRIES",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Location = new Point(16, 178),
                AutoSize = true
            };

            btnFilterAll = new PexorisButton
            {
                Text = "All",
                Style = PexorisButtonStyle.PrimaryBlue,
                Location = new Point(245, 172),
                Size = new Size(65, 28),
                CornerRadius = 5f
            };
            btnFilterAll.Click += (s, e) => SetFilter("ALL", btnFilterAll);

            btnFilterSafe = new PexorisButton
            {
                Text = "⚡ Safe to Disable",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(316, 172),
                Size = new Size(115, 28),
                CornerRadius = 5f
            };
            btnFilterSafe.Click += (s, e) => SetFilter("SAFE", btnFilterSafe);

            btnFilterRegistry = new PexorisButton
            {
                Text = "Registry",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(437, 172),
                Size = new Size(75, 28),
                CornerRadius = 5f
            };
            btnFilterRegistry.Click += (s, e) => SetFilter("REGISTRY", btnFilterRegistry);

            btnFilterFolder = new PexorisButton
            {
                Text = "Folder",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(518, 172),
                Size = new Size(70, 28),
                CornerRadius = 5f
            };
            btnFilterFolder.Click += (s, e) => SetFilter("FOLDER", btnFilterFolder);

            btnFilterDisabled = new PexorisButton
            {
                Text = "Disabled",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(594, 172),
                Size = new Size(75, 28),
                CornerRadius = 5f
            };
            btnFilterDisabled.Click += (s, e) => SetFilter("DISABLED", btnFilterDisabled);

            txtSearch = new TextBox
            {
                Location = new Point(675, 174),
                Size = new Size(169, 24),
                Font = Theme.FontRegular,
                ForeColor = Theme.TextSub,
                Text = "Search items..."
            };
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "Search items...") { txtSearch.Text = ""; txtSearch.ForeColor = Theme.TextHero; } };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Search items..."; txtSearch.ForeColor = Theme.TextSub; } };
            txtSearch.TextChanged += (s, e) => PopulateListView();

            // 4. ListView Explorer (Y=206, H=294)
            pnlListWrapper = new Panel
            {
                Location = new Point(16, 206),
                Size = new Size(828, 294),
                BackColor = Theme.CardBg
            };
            pnlListWrapper.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Theme.BorderLight))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlListWrapper.Width - 1, pnlListWrapper.Height - 1);
                }
            };

            lvItems = new ListView
            {
                Location = new Point(1, 1),
                Size = new Size(826, 292),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = true,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.CardBg,
                Font = Theme.FontRegular
            };

            lvItems.Columns.Add("Status", 85);
            lvItems.Columns.Add("Application Name", 170);
            lvItems.Columns.Add("Publisher", 130);
            lvItems.Columns.Add("Location", 140);
            lvItems.Columns.Add("Impact", 100);
            lvItems.Columns.Add("Command / File Path", 320);

            lvItems.DoubleClick += LvItems_DoubleClick;
            lvItems.SelectedIndexChanged += LvItems_SelectedIndexChanged;

            // Context Menu
            ctxMenu = new ContextMenuStrip();
            ToolStripMenuItem miToggle = new ToolStripMenuItem("Toggle Enable / Disable", null, (s, e) => ToggleSelectedItems());
            ToolStripMenuItem miOpenLoc = new ToolStripMenuItem("Open File Location", null, (s, e) => OpenSelectedFileLocation());
            ToolStripMenuItem miCopy = new ToolStripMenuItem("Copy Command Path", null, (s, e) => CopyCommandPath());
            ToolStripMenuItem miGoogle = new ToolStripMenuItem("Search Process on Google", null, (s, e) => SearchOnline());
            ToolStripSeparator miSep = new ToolStripSeparator();
            ToolStripMenuItem miDelete = new ToolStripMenuItem("Delete Permanently", null, (s, e) => DeleteSelectedPermanently());

            ctxMenu.Items.AddRange(new ToolStripItem[] { miToggle, miOpenLoc, miCopy, miGoogle, miSep, miDelete });
            lvItems.ContextMenuStrip = ctxMenu;

            pnlListWrapper.Controls.Add(lvItems);

            // 5. Action Buttons Bar (Y=508, H=50)
            pnlActions = new Panel
            {
                Location = new Point(16, 508),
                Size = new Size(828, 48),
                BackColor = Color.Transparent
            };

            btnDisableSelected = new PexorisButton
            {
                Text = "⚡ Disable Selected",
                Style = PexorisButtonStyle.DestructiveRed,
                Location = new Point(0, 4),
                Size = new Size(160, 40),
                CornerRadius = 7f,
                Enabled = false
            };
            btnDisableSelected.Click += (s, e) => DisableSelectedItems();

            btnEnableSelected = new PexorisButton
            {
                Text = "✓ Enable Selected",
                Style = PexorisButtonStyle.SuccessGreen,
                Location = new Point(170, 4),
                Size = new Size(150, 40),
                CornerRadius = 7f,
                Enabled = false
            };
            btnEnableSelected.Click += (s, e) => EnableSelectedItems();

            btnOpenFileLocation = new PexorisButton
            {
                Text = "📁 Open Location",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(330, 4),
                Size = new Size(140, 40),
                CornerRadius = 7f,
                Enabled = false
            };
            btnOpenFileLocation.Click += (s, e) => OpenSelectedFileLocation();

            btnDeleteSelected = new PexorisButton
            {
                Text = "🗑 Delete Permanently",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(480, 4),
                Size = new Size(160, 40),
                CornerRadius = 7f,
                Enabled = false
            };
            btnDeleteSelected.Click += (s, e) => DeleteSelectedPermanently();

            btnRefresh = new PexorisButton
            {
                Text = "🔄 Refresh Scan",
                Style = PexorisButtonStyle.PrimaryBlue,
                Location = new Point(650, 4),
                Size = new Size(178, 40),
                CornerRadius = 7f
            };
            btnRefresh.Click += (s, e) => RefreshStartupList();

            pnlActions.Controls.Add(btnDisableSelected);
            pnlActions.Controls.Add(btnEnableSelected);
            pnlActions.Controls.Add(btnOpenFileLocation);
            pnlActions.Controls.Add(btnDeleteSelected);
            pnlActions.Controls.Add(btnRefresh);

            // 6. Footer (38px)
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
                Text = "Auto-Backup to Registry before changes",
                Checked = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                Location = new Point(16, 9),
                AutoSize = true
            };

            lblFooterStatus = new Label
            {
                Text = "Ready • All locations indexed",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Location = new Point(270, 11),
                AutoSize = true
            };

            lnkBrand = new LinkLabel
            {
                Text = "pexoris.com",
                Font = Theme.FontSmall,
                LinkColor = Theme.PrimaryBlue,
                ActiveLinkColor = Theme.PrimaryHover,
                Location = new Point(770, 11),
                AutoSize = true
            };
            lnkBrand.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            pnlFooter.Controls.Add(chkAutoBackup);
            pnlFooter.Controls.Add(lblFooterStatus);
            pnlFooter.Controls.Add(lnkBrand);

            // Add all controls to Form
            Controls.Add(pnlListWrapper);
            Controls.Add(lblSection);
            Controls.Add(btnFilterAll);
            Controls.Add(btnFilterSafe);
            Controls.Add(btnFilterRegistry);
            Controls.Add(btnFilterFolder);
            Controls.Add(btnFilterDisabled);
            Controls.Add(txtSearch);
            Controls.Add(pnlActions);
            Controls.Add(cardTop);
            Controls.Add(pnlTitleBar);
            Controls.Add(pnlFooter);

            ResumeLayout(false);
            PerformLayout();
        }

        private void CardTop_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, cardTop.Width - 1, cardTop.Height - 1);
            using (GraphicsPath path = Theme.GetRoundedPath(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height), 8f))
            {
                using (SolidBrush brush = new SolidBrush(Theme.CardBg))
                {
                    g.FillPath(brush, path);
                }
                using (Pen pen = new Pen(Theme.BorderLight))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private void ApplyCustomDropShadow()
        {
            // Drop shadow is provided through CreateParams CS_DROPSHADOW
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
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
                    picTitleIcon.Image = appIcon.ToBitmap();
                    picCardIcon.Image = appIcon.ToBitmap();
                    SendMessage(Handle, WM_SETICON, ICON_SMALL, appIcon.Handle.ToInt32());
                    SendMessage(Handle, WM_SETICON, ICON_BIG, appIcon.Handle.ToInt32());
                }
            }
            catch { }
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void SetFilter(string filter, PexorisButton activeBtn)
        {
            _activeFilter = filter;
            btnFilterAll.Style = PexorisButtonStyle.SecondaryOutline;
            btnFilterSafe.Style = PexorisButtonStyle.SecondaryOutline;
            btnFilterRegistry.Style = PexorisButtonStyle.SecondaryOutline;
            btnFilterFolder.Style = PexorisButtonStyle.SecondaryOutline;
            btnFilterDisabled.Style = PexorisButtonStyle.SecondaryOutline;

            activeBtn.Style = PexorisButtonStyle.PrimaryBlue;

            btnFilterAll.Invalidate();
            btnFilterSafe.Invalidate();
            btnFilterRegistry.Invalidate();
            btnFilterFolder.Invalidate();
            btnFilterDisabled.Invalidate();

            PopulateListView();
        }

        private void RefreshStartupList()
        {
            Cursor = Cursors.WaitCursor;
            lblFooterStatus.Text = "Scanning autorun registries and startup directories...";

            try
            {
                _allItems = StartupHelper.ScanAllStartupItems();
                PopulateListView();

                int total = _allItems.Count;
                int safe = _allItems.FindAll(i => i.IsSafeToDisable && i.IsEnabled).Count;
                int disabled = _allItems.FindAll(i => !i.IsEnabled).Count;

                pillStatus.Text = "● " + total + " Items (" + safe + " Safe to Disable)";
                lblFooterStatus.Text = "Scan complete: " + total + " items found. " + disabled + " currently disabled.";
            }
            catch (Exception ex)
            {
                lblFooterStatus.Text = "Error during scan: " + ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void PopulateListView()
        {
            lvItems.BeginUpdate();
            lvItems.Items.Clear();

            string search = txtSearch.Text == "Search items..." ? "" : txtSearch.Text.Trim().ToLowerInvariant();

            foreach (StartupItem item in _allItems)
            {
                // Filter tabs
                if (_activeFilter == "SAFE" && (!item.IsSafeToDisable || !item.IsEnabled)) continue;
                if (_activeFilter == "REGISTRY" && (item.LocationType == StartupLocationType.Folder_User_Startup || item.LocationType == StartupLocationType.Folder_Common_Startup)) continue;
                if (_activeFilter == "FOLDER" && item.LocationType != StartupLocationType.Folder_User_Startup && item.LocationType != StartupLocationType.Folder_Common_Startup) continue;
                if (_activeFilter == "DISABLED" && item.IsEnabled) continue;

                // Search string filter
                if (!string.IsNullOrEmpty(search))
                {
                    bool match = (item.Name != null && item.Name.ToLowerInvariant().Contains(search)) ||
                                 (item.Publisher != null && item.Publisher.ToLowerInvariant().Contains(search)) ||
                                 (item.Command != null && item.Command.ToLowerInvariant().Contains(search)) ||
                                 (item.LocationDisplay != null && item.LocationDisplay.ToLowerInvariant().Contains(search));
                    if (!match) continue;
                }

                string statusText = item.IsEnabled ? "● Enabled" : "○ Disabled";
                ListViewItem lvi = new ListViewItem(statusText);

                lvi.SubItems.Add(item.Name);
                lvi.SubItems.Add(item.Publisher);
                lvi.SubItems.Add(item.LocationDisplay);

                string impactText = item.Impact.ToString();
                if (item.Impact == StartupImpactLevel.High) impactText = "⚡ High Delay";
                else if (item.Impact == StartupImpactLevel.Crucial) impactText = "🔒 Crucial / Driver";
                else if (item.Impact == StartupImpactLevel.Orphaned) impactText = "⚠️ Missing File";
                lvi.SubItems.Add(impactText);

                lvi.SubItems.Add(item.Command);
                lvi.Tag = item;

                if (!item.IsEnabled)
                {
                    lvi.ForeColor = Theme.TextMuted;
                }
                else if (item.Impact == StartupImpactLevel.High && item.IsSafeToDisable)
                {
                    lvi.ForeColor = Color.FromArgb(185, 28, 28); // Highlight high impact bloatware in red-amber
                }
                else if (item.Impact == StartupImpactLevel.Crucial)
                {
                    lvi.ForeColor = Color.FromArgb(71, 85, 105);
                }

                lvItems.Items.Add(lvi);
            }

            lvItems.EndUpdate();
            UpdateActionButtonStates();
        }

        private void LvItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionButtonStates();
        }

        private void UpdateActionButtonStates()
        {
            bool hasSelection = lvItems.SelectedItems.Count > 0;
            btnOpenFileLocation.Enabled = hasSelection;
            btnDeleteSelected.Enabled = hasSelection;

            bool canDisable = false;
            bool canEnable = false;

            foreach (ListViewItem lvi in lvItems.SelectedItems)
            {
                StartupItem item = lvi.Tag as StartupItem;
                if (item != null)
                {
                    if (item.IsEnabled) canDisable = true;
                    else canEnable = true;
                }
            }

            btnDisableSelected.Enabled = canDisable;
            btnEnableSelected.Enabled = canEnable;
        }

        private void LvItems_DoubleClick(object sender, EventArgs e)
        {
            ToggleSelectedItems();
        }

        private void ToggleSelectedItems()
        {
            if (lvItems.SelectedItems.Count == 0) return;

            foreach (ListViewItem lvi in lvItems.SelectedItems)
            {
                StartupItem item = lvi.Tag as StartupItem;
                if (item == null) continue;

                if (item.IsEnabled)
                {
                    StartupHelper.DisableItem(item);
                }
                else
                {
                    StartupHelper.EnableItem(item);
                }
            }
            RefreshStartupList();
        }

        private void DisableSelectedItems()
        {
            if (lvItems.SelectedItems.Count == 0) return;

            int count = 0;
            foreach (ListViewItem lvi in lvItems.SelectedItems)
            {
                StartupItem item = lvi.Tag as StartupItem;
                if (item != null && item.IsEnabled)
                {
                    if (StartupHelper.DisableItem(item)) count++;
                }
            }

            lblFooterStatus.Text = count + " startup items disabled safely.";
            RefreshStartupList();
        }

        private void EnableSelectedItems()
        {
            if (lvItems.SelectedItems.Count == 0) return;

            int count = 0;
            foreach (ListViewItem lvi in lvItems.SelectedItems)
            {
                StartupItem item = lvi.Tag as StartupItem;
                if (item != null && !item.IsEnabled)
                {
                    if (StartupHelper.EnableItem(item)) count++;
                }
            }

            lblFooterStatus.Text = count + " startup items re-enabled.";
            RefreshStartupList();
        }

        private void DeleteSelectedPermanently()
        {
            if (lvItems.SelectedItems.Count == 0) return;

            DialogResult res = MessageBox.Show(
                "Are you sure you want to permanently delete the selected startup entry/shortcut?\nThis action cannot be undone.",
                "Confirm Permanent Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (res != DialogResult.Yes) return;

            int count = 0;
            foreach (ListViewItem lvi in lvItems.SelectedItems)
            {
                StartupItem item = lvi.Tag as StartupItem;
                if (item != null)
                {
                    if (StartupHelper.DeleteItemPermanently(item)) count++;
                }
            }

            lblFooterStatus.Text = count + " startup entries deleted permanently.";
            RefreshStartupList();
        }

        private void OpenSelectedFileLocation()
        {
            if (lvItems.SelectedItems.Count == 0) return;

            StartupItem item = lvItems.SelectedItems[0].Tag as StartupItem;
            if (item == null) return;

            string target = item.CleanExecutablePath;
            if (string.IsNullOrEmpty(target) && !string.IsNullOrEmpty(item.FileSystemPath))
            {
                target = item.FileSystemPath;
            }

            if (!string.IsNullOrEmpty(target) && File.Exists(target))
            {
                try
                {
                    Process.Start("explorer.exe", "/select,\"" + target + "\"");
                }
                catch { }
            }
            else
            {
                MessageBox.Show("Target executable or shortcut file does not exist on disk.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CopyCommandPath()
        {
            if (lvItems.SelectedItems.Count == 0) return;
            StartupItem item = lvItems.SelectedItems[0].Tag as StartupItem;
            if (item != null && !string.IsNullOrEmpty(item.Command))
            {
                Clipboard.SetText(item.Command);
                lblFooterStatus.Text = "Command copied to clipboard.";
            }
        }

        private void SearchOnline()
        {
            if (lvItems.SelectedItems.Count == 0) return;
            StartupItem item = lvItems.SelectedItems[0].Tag as StartupItem;
            if (item != null)
            {
                string query = "what is startup " + item.Name;
                try
                {
                    Process.Start("https://www.google.com/search?q=" + Uri.EscapeDataString(query));
                }
                catch { }
            }
        }

        private void BtnQuickClean_Click(object sender, EventArgs e)
        {
            List<StartupItem> safeItems = _allItems.FindAll(i => i.IsSafeToDisable && i.IsEnabled);

            if (safeItems.Count == 0)
            {
                MessageBox.Show("No active high-delay startup bloatware found on your system! Your boot sequence is already optimized.",
                    "Boot Optimization", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult res = MessageBox.Show(
                "Pexoris will safely disable " + safeItems.Count + " high-delay startup updaters & bloatware apps.\n\n" +
                "Crucial drivers (Realtek audio, NVIDIA/AMD graphics, Windows Security) will NOT be touched.\n" +
                "You can re-enable any item at any time.\n\nProceed with 1-Click Clean?",
                "Optimize Startup Sequence", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res != DialogResult.Yes) return;

            int disabledCount = 0;
            foreach (StartupItem item in safeItems)
            {
                if (StartupHelper.DisableItem(item))
                {
                    disabledCount++;
                }
            }

            lblFooterStatus.Text = "Optimized! " + disabledCount + " startup bloatware items safely disabled.";
            RefreshStartupList();
        }

        private void BtnOpenStartupFolder_Click(object sender, EventArgs e)
        {
            string userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            try
            {
                Process.Start("explorer.exe", userStartup);
            }
            catch { }
        }

        private void BtnOpenRegedit_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("regedit.exe");
            }
            catch { }
        }
    }
}
