using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace PexorisShortcutDoctor
{
    public class MainForm : Form
    {
        // UI Controls
        private PictureBox _titleIcon;
        private Label _titleLabel;
        private Label _titleSubLabel;
        private Button _btnMin;
        private Button _btnClose;

        private Panel _topCard;
        private PictureBox _cardIcon;
        private Label _cardTitle;
        private Label _cardSub;
        private Label _statusPill;

        private Label _lblScanned;
        private Label _lblBroken;
        private Label _lblRepairable;
        private Label _lblHealthy;

        private CheckBox _chkDesktop;
        private CheckBox _chkStartMenu;
        private CheckBox _chkQuickLaunch;
        private CheckBox _chkAppData;
        private ComboBox _cmbFilter;

        private ListView _rulesList;
        private ContextMenuStrip _listContextMenu;

        private PexorisButton _btnPurgeSelected;
        private PexorisButton _btnAutoRepair;
        private PexorisButton _btnPurgeAllBroken;
        private PexorisButton _btnExportReport;
        private PexorisButton _btnRescan;

        private Panel _footer;
        private CheckBox _chkRecycleBin;
        private Label _footerStatus;
        private LinkLabel _footerLink;

        private Icon _appIcon;
        private List<ShortcutItem> _cachedItems = new List<ShortcutItem>();

        public MainForm()
        {
            InitializeComponent();
            LoadApplicationIcon();
            PerformSystemScan();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= Theme.CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                IntPtr hIcon = _appIcon != null ? _appIcon.Handle : Icon.ExtractAssociatedIcon(Application.ExecutablePath).Handle;
                Theme.SendMessage(this.Handle, Theme.WM_SETICON, (IntPtr)Theme.ICON_SMALL, hIcon);
                Theme.SendMessage(this.Handle, Theme.WM_SETICON, (IntPtr)Theme.ICON_BIG, hIcon);
            }
            catch { }
        }

        private void LoadApplicationIcon()
        {
            try
            {
                _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                _appIcon = null;
            }

            if (_appIcon != null)
            {
                Bitmap bmp = _appIcon.ToBitmap();
                _titleIcon.Image = bmp;
                _cardIcon.Image = bmp;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.ClientSize = new Size(860, 640);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgCanvas;
            this.Font = Theme.FontBody;
            this.Text = "Pexoris ShortcutDoctor • Windows Shortcut & .LNK Repair Utility";

            // 1. TITLE BAR (44px)
            Panel titleBar = new Panel();
            titleBar.Location = new Point(0, 0);
            titleBar.Size = new Size(860, 44);
            titleBar.BackColor = Theme.CardWhite;
            titleBar.MouseDown += TitleBar_MouseDown;

            _titleIcon = new PictureBox();
            _titleIcon.Location = new Point(14, 11);
            _titleIcon.Size = new Size(22, 22);
            _titleIcon.SizeMode = PictureBoxSizeMode.Zoom;
            _titleIcon.MouseDown += TitleBar_MouseDown;
            titleBar.Controls.Add(_titleIcon);

            _titleLabel = new Label();
            _titleLabel.Text = "Pexoris ShortcutDoctor v1.0";
            _titleLabel.Font = Theme.FontSection;
            _titleLabel.ForeColor = Theme.TextHero;
            _titleLabel.Location = new Point(42, 11);
            _titleLabel.AutoSize = true;
            _titleLabel.MouseDown += TitleBar_MouseDown;
            titleBar.Controls.Add(_titleLabel);

            _titleSubLabel = new Label();
            _titleSubLabel.Text = "• Broken .LNK Shortcut Repair & Cleaner";
            _titleSubLabel.Font = Theme.FontBody;
            _titleSubLabel.ForeColor = Theme.TextMuted;
            _titleSubLabel.Location = new Point(265, 13);
            _titleSubLabel.AutoSize = true;
            _titleSubLabel.MouseDown += TitleBar_MouseDown;
            titleBar.Controls.Add(_titleSubLabel);

            _btnMin = new Button();
            _btnMin.Text = "—";
            _btnMin.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _btnMin.Size = new Size(40, 30);
            _btnMin.Location = new Point(772, 7);
            _btnMin.FlatStyle = FlatStyle.Flat;
            _btnMin.FlatAppearance.BorderSize = 0;
            _btnMin.BackColor = Color.Transparent;
            _btnMin.ForeColor = Theme.TextMuted;
            _btnMin.Cursor = Cursors.Hand;
            _btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            titleBar.Controls.Add(_btnMin);

            _btnClose = new Button();
            _btnClose.Text = "✕";
            _btnClose.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _btnClose.Size = new Size(40, 30);
            _btnClose.Location = new Point(812, 7);
            _btnClose.FlatStyle = FlatStyle.Flat;
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.BackColor = Color.Transparent;
            _btnClose.ForeColor = Theme.TextMuted;
            _btnClose.Cursor = Cursors.Hand;
            _btnClose.MouseEnter += (s, e) => { _btnClose.BackColor = Theme.PrimaryRed; _btnClose.ForeColor = Color.White; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.BackColor = Color.Transparent; _btnClose.ForeColor = Theme.TextMuted; };
            _btnClose.Click += (s, e) => Application.Exit();
            titleBar.Controls.Add(_btnClose);

            this.Controls.Add(titleBar);

            // 2. TOP CONTROL CARD (Y=54, H=128)
            _topCard = new Panel();
            _topCard.Location = new Point(14, 54);
            _topCard.Size = new Size(832, 128);
            _topCard.BackColor = Theme.CardWhite;
            _topCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, _topCard.Width - 1, _topCard.Height - 1);
                using (GraphicsPath p = Theme.GetRoundedRect(r, 8f))
                {
                    using (Pen pen = new Pen(Theme.BorderLight, 1f))
                    {
                        e.Graphics.DrawPath(pen, p);
                    }
                }
            };

            _cardIcon = new PictureBox();
            _cardIcon.Location = new Point(16, 14);
            _cardIcon.Size = new Size(44, 44);
            _cardIcon.SizeMode = PictureBoxSizeMode.Zoom;
            _topCard.Controls.Add(_cardIcon);

            _cardTitle = new Label();
            _cardTitle.Text = "Windows Shortcut Doctor & Broken .LNK Target Fixer";
            _cardTitle.Font = Theme.FontTitle;
            _cardTitle.ForeColor = Theme.TextHero;
            _cardTitle.Location = new Point(68, 14);
            _cardTitle.AutoSize = true;
            _topCard.Controls.Add(_cardTitle);

            _cardSub = new Label();
            _cardSub.Text = "Scan Desktop, Start Menu & AppData for orphaned targets, resolve moved executables, and purge dead links.";
            _cardSub.Font = Theme.FontBody;
            _cardSub.ForeColor = Theme.TextMuted;
            _cardSub.Location = new Point(69, 36);
            _cardSub.AutoSize = true;
            _topCard.Controls.Add(_cardSub);

            _statusPill = new Label();
            _statusPill.Text = "● Ready to Scan";
            _statusPill.Font = Theme.FontSmallBold;
            _statusPill.ForeColor = Color.FromArgb(109, 40, 217);
            _statusPill.BackColor = Theme.VioletTint;
            _statusPill.TextAlign = ContentAlignment.MiddleCenter;
            _statusPill.Size = new Size(130, 26);
            _statusPill.Location = new Point(686, 16);
            _topCard.Controls.Add(_statusPill);

            // 4 Stat Tiles (Y=68)
            int tileW = 194;
            int tileH = 46;
            int tileY = 68;

            Panel tile1 = CreateStatTile("Shortcuts Scanned", "0 Links", Theme.TextHero, 16, tileY, tileW, tileH, out _lblScanned);
            _topCard.Controls.Add(tile1);

            Panel tile2 = CreateStatTile("Broken Targets", "0 Broken", Theme.PrimaryRed, 218, tileY, tileW, tileH, out _lblBroken);
            _topCard.Controls.Add(tile2);

            Panel tile3 = CreateStatTile("Auto-Repairable", "0 Matched", Theme.AmberWarning, 420, tileY, tileW, tileH, out _lblRepairable);
            _topCard.Controls.Add(tile3);

            Panel tile4 = CreateStatTile("Healthy Links", "0 Healthy", Theme.GreenSuccess, 622, tileY, tileW, tileH, out _lblHealthy);
            _topCard.Controls.Add(tile4);

            this.Controls.Add(_topCard);

            // 3. SCOPE STRIP (Y=190, H=44)
            Panel strip = new Panel();
            strip.Location = new Point(14, 190);
            strip.Size = new Size(832, 44);
            strip.BackColor = Theme.CardWhite;
            strip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, strip.Width - 1, strip.Height - 1);
                using (GraphicsPath p = Theme.GetRoundedRect(r, 7f))
                {
                    using (Pen pen = new Pen(Theme.BorderLight, 1f))
                    {
                        e.Graphics.DrawPath(pen, p);
                    }
                }
            };

            Label lblScope = new Label();
            lblScope.Text = "Scan Scope:";
            lblScope.Font = Theme.FontBodyBold;
            lblScope.ForeColor = Theme.TextMuted;
            lblScope.Location = new Point(12, 12);
            lblScope.AutoSize = true;
            strip.Controls.Add(lblScope);

            _chkDesktop = new CheckBox();
            _chkDesktop.Text = "Desktop";
            _chkDesktop.Checked = true;
            _chkDesktop.Font = Theme.FontBodyBold;
            _chkDesktop.ForeColor = Theme.TextHero;
            _chkDesktop.Location = new Point(105, 12);
            _chkDesktop.AutoSize = true;
            strip.Controls.Add(_chkDesktop);

            _chkStartMenu = new CheckBox();
            _chkStartMenu.Text = "Start Menu";
            _chkStartMenu.Checked = true;
            _chkStartMenu.Font = Theme.FontBodyBold;
            _chkStartMenu.ForeColor = Theme.TextHero;
            _chkStartMenu.Location = new Point(190, 12);
            _chkStartMenu.AutoSize = true;
            strip.Controls.Add(_chkStartMenu);

            _chkQuickLaunch = new CheckBox();
            _chkQuickLaunch.Text = "Quick Launch";
            _chkQuickLaunch.Checked = true;
            _chkQuickLaunch.Font = Theme.FontBodyBold;
            _chkQuickLaunch.ForeColor = Theme.TextHero;
            _chkQuickLaunch.Location = new Point(292, 12);
            _chkQuickLaunch.AutoSize = true;
            strip.Controls.Add(_chkQuickLaunch);

            _chkAppData = new CheckBox();
            _chkAppData.Text = "AppData Programs";
            _chkAppData.Checked = true;
            _chkAppData.Font = Theme.FontBodyBold;
            _chkAppData.ForeColor = Theme.TextHero;
            _chkAppData.Location = new Point(410, 12);
            _chkAppData.AutoSize = true;
            strip.Controls.Add(_chkAppData);

            Label lblFilter = new Label();
            lblFilter.Text = "Filter:";
            lblFilter.Font = Theme.FontBodyBold;
            lblFilter.ForeColor = Theme.TextMuted;
            lblFilter.Location = new Point(570, 12);
            lblFilter.AutoSize = true;
            strip.Controls.Add(lblFilter);

            _cmbFilter = new ComboBox();
            _cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbFilter.Items.AddRange(new object[] { "Broken Shortcuts Only", "Auto-Repairable Only", "All Scanned (.lnk)" });
            _cmbFilter.SelectedIndex = 0;
            _cmbFilter.Font = Theme.FontSmallBold;
            _cmbFilter.Location = new Point(622, 10);
            _cmbFilter.Size = new Size(198, 24);
            _cmbFilter.SelectedIndexChanged += (s, e) => RefreshListDisplay();
            strip.Controls.Add(_cmbFilter);

            this.Controls.Add(strip);

            // 4. EXPLORER LISTVIEW (Y=242, H=258)
            _rulesList = new ListView();
            _rulesList.Location = new Point(14, 242);
            _rulesList.Size = new Size(832, 258);
            _rulesList.View = View.Details;
            _rulesList.FullRowSelect = true;
            _rulesList.GridLines = false;
            _rulesList.BorderStyle = BorderStyle.FixedSingle;
            _rulesList.BackColor = Theme.CardWhite;
            _rulesList.Font = Theme.FontBody;
            _rulesList.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            _rulesList.Columns.Add("Shortcut Name", 190);
            _rulesList.Columns.Add("Status", 90);
            _rulesList.Columns.Add("Target Path", 260);
            _rulesList.Columns.Add("Suggested Fix / Target", 180);
            _rulesList.Columns.Add("Shortcut Location", 100);

            // Context Menu
            _listContextMenu = new ContextMenuStrip();
            ToolStripMenuItem menuPurge = new ToolStripMenuItem("⚡ Delete / Purge Dead Shortcut");
            menuPurge.Click += (s, e) => PurgeSelectedShortcut();
            ToolStripMenuItem menuRepair = new ToolStripMenuItem("🔧 Apply Auto-Repair Target");
            menuRepair.Click += (s, e) => RepairSelectedShortcut();
            ToolStripMenuItem menuOpenFolder = new ToolStripMenuItem("📁 Open Containing Folder");
            menuOpenFolder.Click += (s, e) => OpenContainingFolder();
            ToolStripMenuItem menuCopyTarget = new ToolStripMenuItem("📋 Copy Target Path");
            menuCopyTarget.Click += (s, e) => CopyTargetPath();

            _listContextMenu.Items.Add(menuPurge);
            _listContextMenu.Items.Add(menuRepair);
            _listContextMenu.Items.Add(new ToolStripSeparator());
            _listContextMenu.Items.Add(menuOpenFolder);
            _listContextMenu.Items.Add(menuCopyTarget);
            _rulesList.ContextMenuStrip = _listContextMenu;

            this.Controls.Add(_rulesList);

            // 5. ACTION BUTTONS BAR (Y=508, H=50)
            Panel actionBar = new Panel();
            actionBar.Location = new Point(14, 508);
            actionBar.Size = new Size(832, 50);
            actionBar.BackColor = Color.Transparent;

            _btnPurgeSelected = new PexorisButton();
            _btnPurgeSelected.Text = "⚡ Purge Selected";
            _btnPurgeSelected.Style = ButtonStyle.DestructiveRed;
            _btnPurgeSelected.Location = new Point(0, 5);
            _btnPurgeSelected.Size = new Size(170, 40);
            _btnPurgeSelected.Click += (s, e) => PurgeSelectedShortcut();
            actionBar.Controls.Add(_btnPurgeSelected);

            _btnAutoRepair = new PexorisButton();
            _btnAutoRepair.Text = "🔧 Auto-Repair Matched";
            _btnAutoRepair.Style = ButtonStyle.PrimaryViolet;
            _btnAutoRepair.Location = new Point(180, 5);
            _btnAutoRepair.Size = new Size(185, 40);
            _btnAutoRepair.Click += BtnAutoRepair_Click;
            actionBar.Controls.Add(_btnAutoRepair);

            _btnPurgeAllBroken = new PexorisButton();
            _btnPurgeAllBroken.Text = "🗑️ Purge All Broken";
            _btnPurgeAllBroken.Style = ButtonStyle.DestructiveRed;
            _btnPurgeAllBroken.Location = new Point(375, 5);
            _btnPurgeAllBroken.Size = new Size(165, 40);
            _btnPurgeAllBroken.Click += BtnPurgeAllBroken_Click;
            actionBar.Controls.Add(_btnPurgeAllBroken);

            _btnExportReport = new PexorisButton();
            _btnExportReport.Text = "📋 Export Report";
            _btnExportReport.Style = ButtonStyle.SecondaryOutline;
            _btnExportReport.Location = new Point(550, 5);
            _btnExportReport.Size = new Size(140, 40);
            _btnExportReport.Click += BtnExportReport_Click;
            actionBar.Controls.Add(_btnExportReport);

            _btnRescan = new PexorisButton();
            _btnRescan.Text = "🔄 Rescan System";
            _btnRescan.Style = ButtonStyle.SecondaryOutline;
            _btnRescan.Location = new Point(700, 5);
            _btnRescan.Size = new Size(132, 40);
            _btnRescan.Click += (s, e) => PerformSystemScan();
            actionBar.Controls.Add(_btnRescan);

            this.Controls.Add(actionBar);

            // 6. FOOTER (Y=598, H=42)
            _footer = new Panel();
            _footer.Location = new Point(0, 598);
            _footer.Size = new Size(860, 42);
            _footer.BackColor = Theme.CardWhite;
            _footer.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, _footer.Width, 0);
                }
            };

            _chkRecycleBin = new CheckBox();
            _chkRecycleBin.Text = "Send deleted shortcuts to Recycle Bin (Safe delete)";
            _chkRecycleBin.Checked = true;
            _chkRecycleBin.Font = Theme.FontSmallBold;
            _chkRecycleBin.ForeColor = Theme.TextBody;
            _chkRecycleBin.Location = new Point(16, 12);
            _chkRecycleBin.AutoSize = true;
            _footer.Controls.Add(_chkRecycleBin);

            _footerStatus = new Label();
            _footerStatus.Text = "Ready • Standalone Portable • Zero background processes";
            _footerStatus.Font = Theme.FontSmall;
            _footerStatus.ForeColor = Theme.TextMuted;
            _footerStatus.Location = new Point(340, 13);
            _footerStatus.AutoSize = true;
            _footer.Controls.Add(_footerStatus);

            _footerLink = new LinkLabel();
            _footerLink.Text = "pexoris.com/tools/shortcut-doctor";
            _footerLink.Font = Theme.FontSmallBold;
            _footerLink.LinkColor = Theme.PrimaryViolet;
            _footerLink.ActiveLinkColor = Theme.PrimaryVioletPress;
            _footerLink.Location = new Point(640, 12);
            _footerLink.AutoSize = true;
            _footerLink.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com/tools/shortcut-doctor/"); } catch { }
            };
            _footer.Controls.Add(_footerLink);

            this.Controls.Add(_footer);

            this.ResumeLayout(false);
        }

        private Panel CreateStatTile(string label, string initVal, Color valColor, int x, int y, int w, int h, out Label valLabel)
        {
            Panel p = new Panel();
            p.Location = new Point(x, y);
            p.Size = new Size(w, h);
            p.BackColor = Theme.BgCanvas;
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                using (GraphicsPath gp = Theme.GetRoundedRect(r, 6f))
                {
                    using (Pen pen = new Pen(Theme.BorderLight, 1f))
                    {
                        e.Graphics.DrawPath(pen, gp);
                    }
                }
            };

            Label lbl = new Label();
            lbl.Text = label.ToUpperInvariant();
            lbl.Font = Theme.FontSmall;
            lbl.ForeColor = Theme.TextMuted;
            lbl.Location = new Point(8, 5);
            lbl.AutoSize = true;
            p.Controls.Add(lbl);

            valLabel = new Label();
            valLabel.Text = initVal;
            valLabel.Font = Theme.FontBodyBold;
            valLabel.ForeColor = valColor;
            valLabel.Location = new Point(8, 22);
            valLabel.AutoSize = true;
            p.Controls.Add(valLabel);

            return p;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Theme.ReleaseCapture();
                Theme.SendMessage(this.Handle, Theme.WM_NCLBUTTONDOWN, Theme.HT_CAPTION, 0);
            }
        }

        private void PerformSystemScan()
        {
            _statusPill.Text = "● Scanning...";
            _statusPill.ForeColor = Theme.AmberWarning;
            _statusPill.BackColor = Theme.AmberTint;
            Application.DoEvents();

            try
            {
                bool desktop = _chkDesktop.Checked;
                bool startMenu = _chkStartMenu.Checked;
                bool quickLaunch = _chkQuickLaunch.Checked;
                bool appData = _chkAppData.Checked;

                _cachedItems = ShortcutEngine.ScanShortcuts(desktop, startMenu, quickLaunch, appData, null);

                int total = _cachedItems.Count;
                int broken = 0;
                int repairable = 0;
                int healthy = 0;

                foreach (ShortcutItem it in _cachedItems)
                {
                    if (it.Status == ShortcutStatus.BrokenTarget) broken++;
                    else if (it.Status == ShortcutStatus.AutoRepairable) { broken++; repairable++; }
                    else if (it.Status == ShortcutStatus.Healthy) healthy++;
                }

                _lblScanned.Text = total + " Links";
                _lblBroken.Text = broken + " Broken";
                _lblRepairable.Text = repairable + " Matched";
                _lblHealthy.Text = healthy + " Healthy";

                if (broken == 0)
                {
                    _statusPill.Text = "● All Healthy";
                    _statusPill.ForeColor = Color.FromArgb(4, 120, 87);
                    _statusPill.BackColor = Theme.GreenTint;
                }
                else
                {
                    _statusPill.Text = "● " + broken + " Issues Found";
                    _statusPill.ForeColor = Theme.PrimaryRed;
                    _statusPill.BackColor = Theme.RedTint;
                }

                RefreshListDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error during shortcut scan: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshListDisplay()
        {
            _rulesList.BeginUpdate();
            _rulesList.Items.Clear();

            int filterIndex = _cmbFilter.SelectedIndex;

            foreach (ShortcutItem it in _cachedItems)
            {
                bool show = false;
                if (filterIndex == 0) // Broken Only
                {
                    show = (it.Status == ShortcutStatus.BrokenTarget || it.Status == ShortcutStatus.AutoRepairable);
                }
                else if (filterIndex == 1) // Auto-Repairable Only
                {
                    show = (it.Status == ShortcutStatus.AutoRepairable);
                }
                else // All Scanned
                {
                    show = true;
                }

                if (!show) continue;

                ListViewItem item = new ListViewItem(it.Name);
                item.Tag = it;

                string statusText;
                Color statusColor;

                switch (it.Status)
                {
                    case ShortcutStatus.AutoRepairable:
                        statusText = "Repairable";
                        statusColor = Theme.AmberWarning;
                        break;
                    case ShortcutStatus.BrokenTarget:
                        statusText = "Broken";
                        statusColor = Theme.PrimaryRed;
                        break;
                    default:
                        statusText = "Healthy";
                        statusColor = Color.FromArgb(4, 120, 87);
                        break;
                }

                item.SubItems.Add(statusText);
                item.SubItems.Add(string.IsNullOrEmpty(it.TargetPath) ? "(No Target Specified)" : it.TargetPath);
                item.SubItems.Add(string.IsNullOrEmpty(it.SuggestedPath) ? "—" : it.SuggestedPath);
                item.SubItems.Add(Path.GetFileName(it.Folder));

                item.ForeColor = statusColor;
                if (it.Status == ShortcutStatus.BrokenTarget)
                {
                    item.BackColor = Theme.RedTint;
                }

                _rulesList.Items.Add(item);
            }

            _rulesList.EndUpdate();
            _footerStatus.Text = "Showing " + _rulesList.Items.Count + " items in current view.";
        }

        private void PurgeSelectedShortcut()
        {
            if (_rulesList.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Please select at least one shortcut to purge.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ListViewItem sel = _rulesList.SelectedItems[0];
            ShortcutItem it = sel.Tag as ShortcutItem;
            if (it == null) return;

            DialogResult res = MessageBox.Show(this,
                "Delete shortcut \"" + it.Name + "\"?\n\nLocation: " + it.LnkPath,
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                ShortcutEngine.DeleteShortcut(it.LnkPath, _chkRecycleBin.Checked);
                _cachedItems.Remove(it);
                RefreshListDisplay();
                _footerStatus.Text = "Deleted shortcut: " + it.Name;
            }
        }

        private void RepairSelectedShortcut()
        {
            if (_rulesList.SelectedItems.Count == 0) return;
            ListViewItem sel = _rulesList.SelectedItems[0];
            ShortcutItem it = sel.Tag as ShortcutItem;
            if (it == null) return;

            if (string.IsNullOrEmpty(it.SuggestedPath))
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Select New Target Executable for " + it.Name;
                    ofd.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        it.SuggestedPath = ofd.FileName;
                    }
                    else return;
                }
            }

            if (ShortcutEngine.RepairShortcut(it.LnkPath, it.SuggestedPath))
            {
                it.TargetPath = it.SuggestedPath;
                it.Status = ShortcutStatus.Healthy;
                it.SuggestedPath = string.Empty;
                RefreshListDisplay();
                MessageBox.Show(this, "Shortcut target successfully repaired:\n\n" + it.TargetPath, "Shortcut Repaired",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "Failed to update shortcut target.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAutoRepair_Click(object sender, EventArgs e)
        {
            List<ShortcutItem> repairable = new List<ShortcutItem>();
            foreach (ShortcutItem it in _cachedItems)
            {
                if (it.Status == ShortcutStatus.AutoRepairable && !string.IsNullOrEmpty(it.SuggestedPath))
                {
                    repairable.Add(it);
                }
            }

            if (repairable.Count == 0)
            {
                MessageBox.Show(this, "No auto-repairable shortcuts found with matching relocated executables.", "None Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult res = MessageBox.Show(this,
                "Auto-repair " + repairable.Count + " shortcuts with detected alternate target paths?",
                "Confirm Auto-Repair", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                int successCount = 0;
                foreach (ShortcutItem it in repairable)
                {
                    if (ShortcutEngine.RepairShortcut(it.LnkPath, it.SuggestedPath))
                    {
                        it.TargetPath = it.SuggestedPath;
                        it.Status = ShortcutStatus.Healthy;
                        it.SuggestedPath = string.Empty;
                        successCount++;
                    }
                }

                RefreshListDisplay();
                MessageBox.Show(this, "Successfully repaired " + successCount + " shortcuts.", "Auto-Repair Completed",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnPurgeAllBroken_Click(object sender, EventArgs e)
        {
            List<ShortcutItem> brokenList = new List<ShortcutItem>();
            foreach (ShortcutItem it in _cachedItems)
            {
                if (it.Status == ShortcutStatus.BrokenTarget || it.Status == ShortcutStatus.AutoRepairable)
                {
                    brokenList.Add(it);
                }
            }

            if (brokenList.Count == 0)
            {
                MessageBox.Show(this, "No broken shortcuts found in current scan.", "Clean System",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string dest = _chkRecycleBin.Checked ? "Recycle Bin" : "permanent deletion";
            DialogResult res = MessageBox.Show(this,
                "Are you sure you want to purge all " + brokenList.Count + " broken shortcuts to " + dest + "?",
                "Confirm Purge All", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (res == DialogResult.Yes)
            {
                int deleted = 0;
                foreach (ShortcutItem it in brokenList)
                {
                    ShortcutEngine.DeleteShortcut(it.LnkPath, _chkRecycleBin.Checked);
                    _cachedItems.Remove(it);
                    deleted++;
                }

                RefreshListDisplay();
                MessageBox.Show(this, "Purged " + deleted + " broken shortcuts.", "Purge Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnExportReport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt";
                sfd.FileName = "ShortcutReport_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        ShortcutEngine.ExportReport(sfd.FileName, _cachedItems);
                        MessageBox.Show(this, "Report exported successfully to:\n" + sfd.FileName, "Export Successful",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Failed to export report: " + ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OpenContainingFolder()
        {
            if (_rulesList.SelectedItems.Count == 0) return;
            ShortcutItem it = _rulesList.SelectedItems[0].Tag as ShortcutItem;
            if (it != null && File.Exists(it.LnkPath))
            {
                try { Process.Start("explorer.exe", "/select,\"" + it.LnkPath + "\""); } catch { }
            }
        }

        private void CopyTargetPath()
        {
            if (_rulesList.SelectedItems.Count == 0) return;
            ShortcutItem it = _rulesList.SelectedItems[0].Tag as ShortcutItem;
            if (it != null && !string.IsNullOrEmpty(it.TargetPath))
            {
                Clipboard.SetText(it.TargetPath);
                _footerStatus.Text = "Copied target path to clipboard.";
            }
        }
    }
}
