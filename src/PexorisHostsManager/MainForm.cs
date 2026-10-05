using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisHostsManager
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

        private PexorisButton btnPresetBlockAds;
        private PexorisButton btnPresetDevLocal;
        private PexorisButton btnPresetRestoreDefault;

        // Quick Add Row
        private TextBox txtNewIp;
        private TextBox txtNewHost;
        private TextBox txtNewComment;
        private PexorisButton btnAddHost;

        private Panel pnlListWrapper;
        private ListView lvHosts;

        private Panel pnlActions;
        private PexorisButton btnSaveFlush;
        private PexorisButton btnToggleSelected;
        private PexorisButton btnDeleteSelected;
        private PexorisButton btnReload;
        private PexorisButton btnOpenFolder;

        private Panel pnlFooter;
        private CheckBox chkAutoFlush;
        private Label lblFooterStatus;
        private LinkLabel lnkBrand;

        private List<HostEntry> _entries = new List<HostEntry>();
        private bool _isDirty = false;

        public MainForm()
        {
            InitializeComponent();
            ApplyCustomDropShadow();
            LoadAppIcon();
            ReloadHostsFromFile();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Pexoris Hosts Manager";
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
                Text = "Pexoris Hosts Manager",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };
            lblTitleText.MouseDown += TitleBar_MouseDown;

            lblTitleBadge = new Label
            {
                Location = new Point(214, 14),
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
            btnClose.Click += (s, e) =>
            {
                if (_isDirty)
                {
                    DialogResult dr = MessageBox.Show("You have unsaved changes to your hosts file. Do you want to save before closing?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (dr == DialogResult.Yes) SaveAndFlush();
                    else if (dr == DialogResult.Cancel) return;
                }
                Close();
            };

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
                Text = "Windows Hosts File Manager & DNS Filter",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            lblCardSubtitle = new Label
            {
                Location = new Point(70, 40),
                Size = new Size(490, 32),
                Text = "Directly view, edit, toggle, and secure your Windows hosts file. Automatically bypasses permission denied errors and flushes DNS cache.",
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
                BackColor = Color.FromArgb(238, 242, 255),
                ForeColor = Theme.PrimaryBlue
            };
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
            btnPresetBlockAds = new PexorisButton
            {
                Location = new Point(70, 76),
                Size = new Size(185, 32),
                Text = "🛡️ Block Ads & Telemetry",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            btnPresetBlockAds.Click += (s, e) => ApplyBlockAdsPreset();

            btnPresetDevLocal = new PexorisButton
            {
                Location = new Point(263, 76),
                Size = new Size(170, 32),
                Text = "💻 Local Dev Presets",
                Style = PexorisButtonStyle.GamerPurple
            };
            btnPresetDevLocal.Click += (s, e) => ApplyDevLocalPreset();

            btnPresetRestoreDefault = new PexorisButton
            {
                Location = new Point(441, 76),
                Size = new Size(175, 32),
                Text = "↩ Restore Defaults",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnPresetRestoreDefault.Click += (s, e) => RestoreDefaultHosts();

            cardTop.Controls.Add(picCardIcon);
            cardTop.Controls.Add(lblCardTitle);
            cardTop.Controls.Add(lblCardSubtitle);
            cardTop.Controls.Add(pillStatus);
            cardTop.Controls.Add(btnPresetBlockAds);
            cardTop.Controls.Add(btnPresetDevLocal);
            cardTop.Controls.Add(btnPresetRestoreDefault);

            // 3. Quick Add Row (Y=182, H=28)
            txtNewIp = new TextBox
            {
                Location = new Point(16, 184),
                Size = new Size(140, 24),
                Font = Theme.FontMono,
                ForeColor = Theme.TextSub,
                Text = "127.0.0.1"
            };
            txtNewIp.GotFocus += (s, e) => { if (txtNewIp.Text == "127.0.0.1") txtNewIp.SelectAll(); };

            txtNewHost = new TextBox
            {
                Location = new Point(164, 184),
                Size = new Size(260, 24),
                Font = Theme.FontRegular,
                ForeColor = Theme.TextSub,
                Text = "example.local"
            };
            txtNewHost.GotFocus += (s, e) => { if (txtNewHost.Text == "example.local") txtNewHost.Text = ""; };
            txtNewHost.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtNewHost.Text)) txtNewHost.Text = "example.local"; };

            txtNewComment = new TextBox
            {
                Location = new Point(432, 184),
                Size = new Size(270, 24),
                Font = Theme.FontRegular,
                ForeColor = Theme.TextSub,
                Text = "Custom Host"
            };
            txtNewComment.GotFocus += (s, e) => { if (txtNewComment.Text == "Custom Host") txtNewComment.Text = ""; };
            txtNewComment.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtNewComment.Text)) txtNewComment.Text = "Custom Host"; };

            btnAddHost = new PexorisButton
            {
                Location = new Point(710, 182),
                Size = new Size(134, 28),
                Text = "➕ Add Entry",
                Style = PexorisButtonStyle.SuccessGreen
            };
            btnAddHost.Click += (s, e) => AddHostEntryFromInputs();

            // 4. ListView Explorer Section
            pnlListWrapper = new Panel
            {
                Location = new Point(16, 218),
                Size = new Size(828, 280),
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

            lvHosts = new ListView
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
            lvHosts.Columns.Add("Status", 80);
            lvHosts.Columns.Add("IP Address", 140);
            lvHosts.Columns.Add("Hostname / Domain", 280);
            lvHosts.Columns.Add("Comment", 210);
            lvHosts.Columns.Add("Type", 95);
            lvHosts.SelectedIndexChanged += (s, e) => UpdateActionButtons();
            lvHosts.DoubleClick += (s, e) => ToggleSelectedEntry();

            pnlListWrapper.Controls.Add(lvHosts);

            // 5. Action Buttons Bar (Y=508, H=50)
            pnlActions = new Panel
            {
                Location = new Point(16, 508),
                Size = new Size(828, 50),
                BackColor = Color.Transparent
            };

            btnSaveFlush = new PexorisButton
            {
                Location = new Point(0, 4),
                Size = new Size(185, 40),
                Text = "💾 Save & Flush DNS",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            btnSaveFlush.Click += (s, e) => SaveAndFlush();

            btnToggleSelected = new PexorisButton
            {
                Location = new Point(195, 4),
                Size = new Size(150, 40),
                Text = "⏻ Toggle State",
                Style = PexorisButtonStyle.SecondaryOutline,
                Enabled = false
            };
            btnToggleSelected.Click += (s, e) => ToggleSelectedEntry();

            btnDeleteSelected = new PexorisButton
            {
                Location = new Point(353, 4),
                Size = new Size(145, 40),
                Text = "🗑️ Delete Host",
                Style = PexorisButtonStyle.DestructiveRed,
                Enabled = false
            };
            btnDeleteSelected.Click += (s, e) => DeleteSelectedEntry();

            btnReload = new PexorisButton
            {
                Location = new Point(506, 4),
                Size = new Size(155, 40),
                Text = "⟳ Reload from Disk",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnReload.Click += (s, e) => ReloadHostsFromFile();

            btnOpenFolder = new PexorisButton
            {
                Location = new Point(669, 4),
                Size = new Size(159, 40),
                Text = "📂 Open Directory",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnOpenFolder.Click += (s, e) =>
            {
                try
                {
                    string dir = Path.GetDirectoryName(HostsHelper.GetHostsPath());
                    Process.Start("explorer.exe", dir);
                }
                catch { }
            };

            pnlActions.Controls.Add(btnSaveFlush);
            pnlActions.Controls.Add(btnToggleSelected);
            pnlActions.Controls.Add(btnDeleteSelected);
            pnlActions.Controls.Add(btnReload);
            pnlActions.Controls.Add(btnOpenFolder);

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

            chkAutoFlush = new CheckBox
            {
                Location = new Point(16, 9),
                AutoSize = true,
                Text = "Auto-flush Windows DNS cache on save",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                Checked = true
            };

            lblFooterStatus = new Label
            {
                Location = new Point(290, 10),
                Size = new Size(420, 20),
                Text = "Ready • Target: C:\\Windows\\System32\\drivers\\etc\\hosts",
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

            pnlFooter.Controls.Add(chkAutoFlush);
            pnlFooter.Controls.Add(lblFooterStatus);
            pnlFooter.Controls.Add(lnkBrand);

            // Add all controls
            Controls.Add(pnlFooter);
            Controls.Add(pnlActions);
            Controls.Add(pnlListWrapper);
            Controls.Add(btnAddHost);
            Controls.Add(txtNewComment);
            Controls.Add(txtNewHost);
            Controls.Add(txtNewIp);
            Controls.Add(cardTop);
            Controls.Add(pnlTitleBar);

            ResumeLayout(false);
            PerformLayout();
        }

        private void ApplyCustomDropShadow()
        {
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

        private void ReloadHostsFromFile()
        {
            lblFooterStatus.Text = "Reading C:\\Windows\\System32\\drivers\\etc\\hosts...";
            Application.DoEvents();

            _entries = HostsHelper.LoadHosts();
            _isDirty = false;
            RefreshListView();

            lblFooterStatus.Text = string.Format("Ready • Loaded {0} entries successfully.", GetActionableCount());
        }

        private int GetActionableCount()
        {
            int c = 0;
            foreach (var e in _entries) if (!e.IsHeader) c++;
            return c;
        }

        private void RefreshListView()
        {
            lvHosts.BeginUpdate();
            lvHosts.Items.Clear();

            int enabledCount = 0;
            int total = 0;

            foreach (var item in _entries)
            {
                if (item.IsHeader) continue;
                total++;
                if (item.IsEnabled) enabledCount++;

                ListViewItem lvi = new ListViewItem(item.IsEnabled ? "● Active" : "○ Disabled");
                lvi.SubItems.Add(item.IpAddress);
                lvi.SubItems.Add(item.Hostname);
                lvi.SubItems.Add(item.Comment ?? "");

                string type = "Custom";
                if (item.IpAddress == "0.0.0.0") type = "Blocklist";
                else if (item.IpAddress == "127.0.0.1") type = "Loopback";
                lvi.SubItems.Add(type);

                if (item.IsEnabled)
                {
                    if (item.IpAddress == "0.0.0.0") lvi.ForeColor = Color.FromArgb(220, 38, 38);
                    else lvi.ForeColor = Theme.TextHero;
                }
                else
                {
                    lvi.ForeColor = Theme.TextMuted;
                }

                lvi.Tag = item;
                lvHosts.Items.Add(lvi);
            }

            lvHosts.EndUpdate();

            pillStatus.Text = string.Format("● {0} ENTRIES ({1} ACTIVE)", total, enabledCount);
            if (enabledCount > 0)
            {
                pillStatus.BackColor = Color.FromArgb(238, 242, 255);
                pillStatus.ForeColor = Theme.PrimaryBlue;
            }
            else
            {
                pillStatus.BackColor = Color.FromArgb(241, 245, 249);
                pillStatus.ForeColor = Theme.TextMuted;
            }
            pillStatus.Invalidate();

            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            bool hasSelection = lvHosts.SelectedItems.Count > 0;
            btnToggleSelected.Enabled = hasSelection;
            btnDeleteSelected.Enabled = hasSelection;

            if (hasSelection)
            {
                HostEntry e = lvHosts.SelectedItems[0].Tag as HostEntry;
                if (e != null)
                {
                    btnToggleSelected.Text = e.IsEnabled ? "⏻ Disable Host" : "⏻ Enable Host";
                    btnToggleSelected.Style = e.IsEnabled ? PexorisButtonStyle.SecondaryOutline : PexorisButtonStyle.SuccessGreen;
                    btnToggleSelected.Invalidate();
                }
            }
            else
            {
                btnToggleSelected.Text = "⏻ Toggle State";
                btnToggleSelected.Style = PexorisButtonStyle.SecondaryOutline;
                btnToggleSelected.Invalidate();
            }
        }

        private void AddHostEntryFromInputs()
        {
            string ip = txtNewIp.Text.Trim();
            string host = txtNewHost.Text.Trim();
            string comment = txtNewComment.Text.Trim();

            if (string.IsNullOrEmpty(ip) || !HostsHelper.IsValidIp(ip))
            {
                MessageBox.Show("Please enter a valid IPv4 or IPv6 address (e.g. 127.0.0.1 or 0.0.0.0).", "Invalid IP Address", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewIp.Focus();
                return;
            }

            if (string.IsNullOrEmpty(host) || host == "example.local")
            {
                MessageBox.Show("Please enter a valid hostname or domain name.", "Invalid Hostname", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewHost.Focus();
                return;
            }

            // Check duplicate
            foreach (var e in _entries)
            {
                if (!e.IsHeader && string.Equals(e.Hostname, host, StringComparison.OrdinalIgnoreCase))
                {
                    DialogResult dr = MessageBox.Show(string.Format("Hostname '{0}' already exists pointing to {1}. Update to {2}?", host, e.IpAddress, ip), "Duplicate Host", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr == DialogResult.Yes)
                    {
                        e.IpAddress = ip;
                        e.Comment = comment;
                        e.IsEnabled = true;
                        _isDirty = true;
                        RefreshListView();
                        lblFooterStatus.Text = string.Format("Updated '{0}' -> {1}. Remember to click Save.", host, ip);
                    }
                    return;
                }
            }

            _entries.Add(new HostEntry
            {
                IsEnabled = true,
                IpAddress = ip,
                Hostname = host,
                Comment = (comment == "Custom Host") ? "" : comment,
                IsHeader = false
            });

            _isDirty = true;
            RefreshListView();

            txtNewHost.Text = "example.local";
            lblFooterStatus.Text = string.Format("Added '{0}' -> {1}. Remember to click 'Save & Flush DNS'.", host, ip);
        }

        private void ToggleSelectedEntry()
        {
            if (lvHosts.SelectedItems.Count == 0) return;
            HostEntry e = lvHosts.SelectedItems[0].Tag as HostEntry;
            if (e == null) return;

            e.IsEnabled = !e.IsEnabled;
            _isDirty = true;
            RefreshListView();
            lblFooterStatus.Text = string.Format("{0} '{1}'. Click 'Save & Flush DNS' to apply.", e.IsEnabled ? "Enabled" : "Disabled", e.Hostname);
        }

        private void DeleteSelectedEntry()
        {
            if (lvHosts.SelectedItems.Count == 0) return;
            HostEntry e = lvHosts.SelectedItems[0].Tag as HostEntry;
            if (e == null) return;

            DialogResult dr = MessageBox.Show(string.Format("Are you sure you want to delete '{0}' ({1}) from your hosts file?", e.Hostname, e.IpAddress), "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                _entries.Remove(e);
                _isDirty = true;
                RefreshListView();
                lblFooterStatus.Text = string.Format("Deleted '{0}'. Click 'Save & Flush DNS' to apply.", e.Hostname);
            }
        }

        private void SaveAndFlush()
        {
            lblFooterStatus.Text = "Saving hosts file...";
            Application.DoEvents();

            // Backup first
            string backup = HostsHelper.CreateBackup();

            bool ok = HostsHelper.SaveHosts(_entries);
            if (ok)
            {
                _isDirty = false;
                if (chkAutoFlush.Checked) HostsHelper.FlushDns();

                lblFooterStatus.Text = string.Format("Success • Saved & DNS flushed. Backup: {0}", Path.GetFileName(backup ?? "none"));
                MessageBox.Show("Windows hosts file saved successfully and DNS resolver cache flushed!", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to save hosts file! Please ensure Pexoris is running with Administrator privileges and no anti-virus has locked the file.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblFooterStatus.Text = "Error saving hosts file. Check permissions.";
            }
        }

        private void ApplyBlockAdsPreset()
        {
            DialogResult dr = MessageBox.Show(
                "This will add 13 known Microsoft telemetry endpoints and aggressive tracking domains mapped to 0.0.0.0.\n\nProceed with adding telemetry blocklist?",
                "Block Ads & Telemetry",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            var preset = HostsHelper.GetTelemetryAdblockPreset();
            int added = 0;

            foreach (var p in preset)
            {
                bool exists = false;
                foreach (var e in _entries)
                {
                    if (!e.IsHeader && string.Equals(e.Hostname, p.Hostname, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        e.IpAddress = "0.0.0.0";
                        e.IsEnabled = true;
                        break;
                    }
                }
                if (!exists)
                {
                    _entries.Add(p);
                    added++;
                }
            }

            _isDirty = true;
            RefreshListView();
            lblFooterStatus.Text = string.Format("Added {0} telemetry adblock domains. Click 'Save & Flush DNS' to apply.", added);
        }

        private void ApplyDevLocalPreset()
        {
            var preset = HostsHelper.GetDevLocalhostPreset();
            int added = 0;

            foreach (var p in preset)
            {
                bool exists = false;
                foreach (var e in _entries)
                {
                    if (!e.IsHeader && string.Equals(e.Hostname, p.Hostname, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    _entries.Add(p);
                    added++;
                }
            }

            _isDirty = true;
            RefreshListView();
            lblFooterStatus.Text = string.Format("Added {0} local development presets (app.local, api.local). Click 'Save & Flush DNS'.", added);
        }

        private void RestoreDefaultHosts()
        {
            DialogResult dr = MessageBox.Show(
                "This will reset your hosts file back to standard Microsoft Windows default (empty template with localhost comment).\n\nAre you sure you want to proceed?",
                "Restore Windows Defaults",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dr != DialogResult.Yes) return;

            HostsHelper.CreateBackup();

            _entries.Clear();
            _entries.Add(new HostEntry { IsHeader = true, RawText = HostsHelper.GetDefaultWindowsHostsContent() });
            _isDirty = true;
            RefreshListView();

            lblFooterStatus.Text = "Hosts file reset to Windows default template. Click 'Save & Flush DNS' to apply.";
        }
    }
}
