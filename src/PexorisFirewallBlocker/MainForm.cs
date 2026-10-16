using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace PexorisFirewallBlocker
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

        private Label _lblDomain;
        private Label _lblPrivate;
        private Label _lblPublic;
        private Label _lblBlockCount;

        private TextBox _txtPath;
        private PexorisButton _btnBrowse;
        private CheckBox _chkOutbound;
        private CheckBox _chkInbound;
        private ComboBox _cmbFilter;

        private ListView _rulesList;
        private ContextMenuStrip _listContextMenu;

        private PexorisButton _btnBlockApp;
        private PexorisButton _btnUnblock;
        private PexorisButton _btnTelemetry;
        private PexorisButton _btnResetDefaults;
        private PexorisButton _btnRefresh;

        private Panel _footer;
        private Label _footerStatus;
        private LinkLabel _footerLink;

        private Icon _appIcon;

        public MainForm()
        {
            InitializeComponent();
            LoadApplicationIcon();
            RefreshFirewallData();
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
            this.Text = "Pexoris FirewallBlocker • Windows Defender Firewall Manager";
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

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
            _titleLabel.Text = "Pexoris FirewallBlocker v1.0";
            _titleLabel.Font = Theme.FontSection;
            _titleLabel.ForeColor = Theme.TextHero;
            _titleLabel.Location = new Point(42, 11);
            _titleLabel.AutoSize = true;
            _titleLabel.MouseDown += TitleBar_MouseDown;
            titleBar.Controls.Add(_titleLabel);

            _titleSubLabel = new Label();
            _titleSubLabel.Text = "• Windows Defender Firewall Manager";
            _titleSubLabel.Font = Theme.FontBody;
            _titleSubLabel.ForeColor = Theme.TextMuted;
            _titleSubLabel.Location = new Point(264, 13);
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
            _cardTitle.Text = "Windows Defender Firewall & Application Network Blocker";
            _cardTitle.Font = Theme.FontTitle;
            _cardTitle.ForeColor = Theme.TextHero;
            _cardTitle.Location = new Point(68, 14);
            _cardTitle.AutoSize = true;
            _topCard.Controls.Add(_cardTitle);

            _cardSub = new Label();
            _cardSub.Text = "1-Click inbound/outbound executable isolation, rule manager, and telemetry isolation shield.";
            _cardSub.Font = Theme.FontBody;
            _cardSub.ForeColor = Theme.TextMuted;
            _cardSub.Location = new Point(69, 36);
            _cardSub.AutoSize = true;
            _topCard.Controls.Add(_cardSub);

            _statusPill = new Label();
            _statusPill.Text = "● Active Protection";
            _statusPill.Font = Theme.FontSmallBold;
            _statusPill.ForeColor = Color.FromArgb(4, 120, 87);
            _statusPill.BackColor = Theme.GreenTint;
            _statusPill.TextAlign = ContentAlignment.MiddleCenter;
            _statusPill.Size = new Size(140, 26);
            _statusPill.Location = new Point(676, 16);
            _topCard.Controls.Add(_statusPill);

            // 4 Stat Tiles inside Top Card (Y=68)
            int tileW = 194;
            int tileH = 46;
            int tileY = 68;

            // Tile 1: Domain
            Panel tileDomain = CreateStatTile("Domain Profile", "Active (ON)", Theme.GreenSuccess, 16, tileY, tileW, tileH, out _lblDomain);
            _topCard.Controls.Add(tileDomain);

            // Tile 2: Private
            Panel tilePrivate = CreateStatTile("Private Profile", "Active (ON)", Theme.GreenSuccess, 218, tileY, tileW, tileH, out _lblPrivate);
            _topCard.Controls.Add(tilePrivate);

            // Tile 3: Public
            Panel tilePublic = CreateStatTile("Public Profile", "Active (ON)", Theme.GreenSuccess, 420, tileY, tileW, tileH, out _lblPublic);
            _topCard.Controls.Add(tilePublic);

            // Tile 4: Blocked Rules
            Panel tileBlock = CreateStatTile("Active Block Rules", "0 Rules", Theme.PrimaryRed, 622, tileY, tileW, tileH, out _lblBlockCount);
            _topCard.Controls.Add(tileBlock);

            this.Controls.Add(_topCard);

            // 3. TARGET EXECUTABLE & FILTER STRIP (Y=190, H=44)
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

            _txtPath = new TextBox();
            _txtPath.Location = new Point(12, 11);
            _txtPath.Size = new Size(390, 24);
            _txtPath.Font = Theme.FontBody;
            _txtPath.ForeColor = Theme.TextMuted;
            _txtPath.Text = "Drag and drop an .exe here or click Browse...";
            _txtPath.GotFocus += (s, e) =>
            {
                if (_txtPath.Text == "Drag and drop an .exe here or click Browse...")
                {
                    _txtPath.Text = "";
                    _txtPath.ForeColor = Theme.TextHero;
                }
            };
            strip.Controls.Add(_txtPath);

            _btnBrowse = new PexorisButton();
            _btnBrowse.Text = "📁 Browse...";
            _btnBrowse.Style = ButtonStyle.SecondaryOutline;
            _btnBrowse.Location = new Point(408, 8);
            _btnBrowse.Size = new Size(95, 28);
            _btnBrowse.Click += BtnBrowse_Click;
            strip.Controls.Add(_btnBrowse);

            _chkOutbound = new CheckBox();
            _chkOutbound.Text = "Outbound";
            _chkOutbound.Checked = true;
            _chkOutbound.Font = Theme.FontBodyBold;
            _chkOutbound.ForeColor = Theme.TextHero;
            _chkOutbound.Location = new Point(512, 12);
            _chkOutbound.AutoSize = true;
            strip.Controls.Add(_chkOutbound);

            _chkInbound = new CheckBox();
            _chkInbound.Text = "Inbound";
            _chkInbound.Checked = true;
            _chkInbound.Font = Theme.FontBodyBold;
            _chkInbound.ForeColor = Theme.TextHero;
            _chkInbound.Location = new Point(602, 12);
            _chkInbound.AutoSize = true;
            strip.Controls.Add(_chkInbound);

            _cmbFilter = new ComboBox();
            _cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbFilter.Items.AddRange(new object[] { "All Blocked Rules", "Pexoris Rules Only", "All Active Rules" });
            _cmbFilter.SelectedIndex = 0;
            _cmbFilter.Font = Theme.FontSmallBold;
            _cmbFilter.Location = new Point(682, 10);
            _cmbFilter.Size = new Size(140, 24);
            _cmbFilter.SelectedIndexChanged += (s, e) => RefreshRulesListView();
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
            _rulesList.AllowDrop = true;
            _rulesList.DragEnter += MainForm_DragEnter;
            _rulesList.DragDrop += MainForm_DragDrop;

            _rulesList.Columns.Add("Rule Name", 200);
            _rulesList.Columns.Add("Direction", 80);
            _rulesList.Columns.Add("Action", 75);
            _rulesList.Columns.Add("Status", 75);
            _rulesList.Columns.Add("Application Path", 285);
            _rulesList.Columns.Add("Profiles", 100);

            // Context Menu
            _listContextMenu = new ContextMenuStrip();
            ToolStripMenuItem menuUnblock = new ToolStripMenuItem("🔓 Unblock / Remove Rule");
            menuUnblock.Click += (s, e) => UnblockSelectedRule();
            ToolStripMenuItem menuToggle = new ToolStripMenuItem("⚡ Toggle Rule (Enable/Disable)");
            menuToggle.Click += (s, e) => ToggleSelectedRule();
            ToolStripMenuItem menuCopyPath = new ToolStripMenuItem("📋 Copy Application Path");
            menuCopyPath.Click += (s, e) => CopySelectedPath();
            ToolStripMenuItem menuOpenFolder = new ToolStripMenuItem("📁 Open Containing Folder");
            menuOpenFolder.Click += (s, e) => OpenSelectedFolder();

            _listContextMenu.Items.Add(menuUnblock);
            _listContextMenu.Items.Add(menuToggle);
            _listContextMenu.Items.Add(new ToolStripSeparator());
            _listContextMenu.Items.Add(menuCopyPath);
            _listContextMenu.Items.Add(menuOpenFolder);
            _rulesList.ContextMenuStrip = _listContextMenu;

            this.Controls.Add(_rulesList);

            // 5. ACTION BUTTONS BAR (Y=508, H=50)
            Panel actionBar = new Panel();
            actionBar.Location = new Point(14, 508);
            actionBar.Size = new Size(832, 50);
            actionBar.BackColor = Color.Transparent;

            _btnBlockApp = new PexorisButton();
            _btnBlockApp.Text = "⚡ Block Executable";
            _btnBlockApp.Style = ButtonStyle.DestructiveRed;
            _btnBlockApp.Location = new Point(0, 5);
            _btnBlockApp.Size = new Size(185, 40);
            _btnBlockApp.Click += BtnBlockApp_Click;
            actionBar.Controls.Add(_btnBlockApp);

            _btnUnblock = new PexorisButton();
            _btnUnblock.Text = "🔓 Unblock Selected";
            _btnUnblock.Style = ButtonStyle.PrimaryBlue;
            _btnUnblock.Location = new Point(195, 5);
            _btnUnblock.Size = new Size(160, 40);
            _btnUnblock.Click += (s, e) => UnblockSelectedRule();
            actionBar.Controls.Add(_btnUnblock);

            _btnTelemetry = new PexorisButton();
            _btnTelemetry.Text = "🛡️ Block Telemetry";
            _btnTelemetry.Style = ButtonStyle.PrimaryBlue;
            _btnTelemetry.Location = new Point(365, 5);
            _btnTelemetry.Size = new Size(160, 40);
            _btnTelemetry.Click += BtnTelemetry_Click;
            actionBar.Controls.Add(_btnTelemetry);

            _btnResetDefaults = new PexorisButton();
            _btnResetDefaults.Text = "⚙️ Restore Defaults";
            _btnResetDefaults.Style = ButtonStyle.SecondaryOutline;
            _btnResetDefaults.Location = new Point(535, 5);
            _btnResetDefaults.Size = new Size(155, 40);
            _btnResetDefaults.Click += BtnResetDefaults_Click;
            actionBar.Controls.Add(_btnResetDefaults);

            _btnRefresh = new PexorisButton();
            _btnRefresh.Text = "🔄 Refresh";
            _btnRefresh.Style = ButtonStyle.SecondaryOutline;
            _btnRefresh.Location = new Point(700, 5);
            _btnRefresh.Size = new Size(132, 40);
            _btnRefresh.Click += (s, e) => RefreshFirewallData();
            actionBar.Controls.Add(_btnRefresh);

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

            _footerStatus = new Label();
            _footerStatus.Text = "Ready • Drop any .exe to instantly block network access • Single 66 KB Portable";
            _footerStatus.Font = Theme.FontSmall;
            _footerStatus.ForeColor = Theme.TextMuted;
            _footerStatus.Location = new Point(16, 12);
            _footerStatus.AutoSize = true;
            _footer.Controls.Add(_footerStatus);

            _footerLink = new LinkLabel();
            _footerLink.Text = "pexoris.com/tools/firewall-blocker";
            _footerLink.Font = Theme.FontSmallBold;
            _footerLink.LinkColor = Theme.PrimaryBlue;
            _footerLink.ActiveLinkColor = Theme.PrimaryBluePress;
            _footerLink.Location = new Point(640, 12);
            _footerLink.AutoSize = true;
            _footerLink.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com/tools/firewall-blocker/"); } catch { }
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

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string target = files[0];
                    if (target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        _txtPath.Text = target;
                        _txtPath.ForeColor = Theme.TextHero;
                        PromptAndBlockApplication(target);
                    }
                    else
                    {
                        MessageBox.Show(this, "Please drop a valid Windows executable (.exe) file.", "Invalid File Type",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                ofd.Title = "Select Application to Block with Firewall";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    _txtPath.Text = ofd.FileName;
                    _txtPath.ForeColor = Theme.TextHero;
                }
            }
        }

        private void BtnBlockApp_Click(object sender, EventArgs e)
        {
            string path = _txtPath.Text.Trim();
            if (string.IsNullOrEmpty(path) || path == "Drag and drop an .exe here or click Browse...")
            {
                BtnBrowse_Click(sender, e);
                path = _txtPath.Text.Trim();
                if (string.IsNullOrEmpty(path) || path == "Drag and drop an .exe here or click Browse...")
                    return;
            }

            if (!File.Exists(path))
            {
                MessageBox.Show(this, "The specified file does not exist:\n" + path, "File Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            PromptAndBlockApplication(path);
        }

        private void PromptAndBlockApplication(string path)
        {
            bool blockOut = _chkOutbound.Checked;
            bool blockIn = _chkInbound.Checked;

            if (!blockOut && !blockIn)
            {
                MessageBox.Show(this, "Please select at least Outbound or Inbound rule creation.", "No Direction Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                FirewallEngine.BlockApplication(path, blockOut, blockIn);
                _footerStatus.Text = "Blocked " + Path.GetFileName(path) + " successfully.";
                RefreshFirewallData();
                MessageBox.Show(this, "Successfully created firewall block rule(s) for:\n\n" + path, "Application Blocked",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to block application: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UnblockSelectedRule()
        {
            if (_rulesList.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Please select a firewall rule from the list to unblock or remove.", "Select Rule",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ListViewItem item = _rulesList.SelectedItems[0];
            string ruleName = item.Text;
            string appPath = item.SubItems[4].Text;

            DialogResult res = MessageBox.Show(this,
                "Remove firewall rule \"" + ruleName + "\"?\n\nIf multiple rules block this application, click Yes to remove this rule, or you can delete all matching rules.",
                "Confirm Unblock", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                try
                {
                    FirewallEngine.UnblockRule(ruleName);
                    _footerStatus.Text = "Removed rule: " + ruleName;
                    RefreshFirewallData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Failed to unblock: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ToggleSelectedRule()
        {
            if (_rulesList.SelectedItems.Count == 0) return;
            string ruleName = _rulesList.SelectedItems[0].Text;
            try
            {
                FirewallEngine.ToggleRule(ruleName);
                RefreshFirewallData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to toggle rule: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopySelectedPath()
        {
            if (_rulesList.SelectedItems.Count == 0) return;
            string path = _rulesList.SelectedItems[0].SubItems[4].Text;
            if (!string.IsNullOrEmpty(path) && path != "(All Applications / Port)")
            {
                Clipboard.SetText(path);
                _footerStatus.Text = "Copied path to clipboard: " + path;
            }
        }

        private void OpenSelectedFolder()
        {
            if (_rulesList.SelectedItems.Count == 0) return;
            string path = _rulesList.SelectedItems[0].SubItems[4].Text;
            if (File.Exists(path))
            {
                try
                {
                    Process.Start("explorer.exe", "/select,\"" + path + "\"");
                }
                catch { }
            }
        }

        private void BtnTelemetry_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show(this,
                "Block Windows telemetry endpoints and diagnostic reporting tools (CompatTelRunner, DeviceCensus, EdgeUpdate, SearchApp) from connecting outbound?\n\nThis will prevent automatic background diagnostic data transmission.",
                "Block Windows Telemetry", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                try
                {
                    int blocked = FirewallEngine.BlockTelemetryPresets();
                    _footerStatus.Text = "Telemetry shield enabled: " + blocked + " endpoints isolated.";
                    RefreshFirewallData();
                    MessageBox.Show(this, "Telemetry outbound traffic blocked for " + blocked + " Windows components.",
                        "Telemetry Blocked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error setting telemetry rules: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnResetDefaults_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show(this,
                "Are you sure you want to restore Windows Defender Firewall to its factory default settings?\n\nAll custom inbound and outbound rules will be reset to Microsoft defaults.",
                "Confirm Firewall Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (res == DialogResult.Yes)
            {
                try
                {
                    FirewallEngine.ResetFirewallToDefaults();
                    _footerStatus.Text = "Firewall restored to factory default settings.";
                    RefreshFirewallData();
                    MessageBox.Show(this, "Windows Defender Firewall has been reset to factory defaults.",
                        "Firewall Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error resetting firewall: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void RefreshFirewallData()
        {
            try
            {
                FirewallProfileStatus status = FirewallEngine.GetProfileStatus();
                UpdateProfileLabels(status);
                RefreshRulesListView();
            }
            catch (Exception ex)
            {
                _footerStatus.Text = "Error auditing firewall: " + ex.Message;
            }
        }

        private void UpdateProfileLabels(FirewallProfileStatus status)
        {
            _lblDomain.Text = status.DomainEnabled ? "Protected (ON)" : "Disabled (OFF)";
            _lblDomain.ForeColor = status.DomainEnabled ? Theme.GreenSuccess : Theme.PrimaryRed;

            _lblPrivate.Text = status.PrivateEnabled ? "Protected (ON)" : "Disabled (OFF)";
            _lblPrivate.ForeColor = status.PrivateEnabled ? Theme.GreenSuccess : Theme.PrimaryRed;

            _lblPublic.Text = status.PublicEnabled ? "Protected (ON)" : "Disabled (OFF)";
            _lblPublic.ForeColor = status.PublicEnabled ? Theme.GreenSuccess : Theme.PrimaryRed;

            _lblBlockCount.Text = status.TotalBlockRules + " Rules (" + status.TotalPexorisRules + " Pexoris)";

            if (status.DomainEnabled && status.PrivateEnabled && status.PublicEnabled)
            {
                _statusPill.Text = "● Fully Protected";
                _statusPill.ForeColor = Color.FromArgb(4, 120, 87);
                _statusPill.BackColor = Theme.GreenTint;
            }
            else if (!status.DomainEnabled && !status.PrivateEnabled && !status.PublicEnabled)
            {
                _statusPill.Text = "● Firewall Disabled";
                _statusPill.ForeColor = Theme.PrimaryRed;
                _statusPill.BackColor = Theme.RedTint;
            }
            else
            {
                _statusPill.Text = "● Partial Protection";
                _statusPill.ForeColor = Theme.AmberWarning;
                _statusPill.BackColor = Theme.AmberTint;
            }
        }

        private void RefreshRulesListView()
        {
            _rulesList.BeginUpdate();
            _rulesList.Items.Clear();

            RuleFilter filter = RuleFilter.AllBlocked;
            if (_cmbFilter.SelectedIndex == 1) filter = RuleFilter.PexorisOnly;
            else if (_cmbFilter.SelectedIndex == 2) filter = RuleFilter.AllActive;

            List<FirewallRuleItem> rules = FirewallEngine.GetRules(filter);

            foreach (FirewallRuleItem r in rules)
            {
                ListViewItem item = new ListViewItem(r.Name);
                item.SubItems.Add(r.Direction);
                item.SubItems.Add(r.Action);
                item.SubItems.Add(r.Enabled ? "Enabled" : "Disabled");
                item.SubItems.Add(r.ApplicationName);
                item.SubItems.Add(r.Profiles);

                if (r.Action == "Block")
                {
                    item.ForeColor = Theme.PrimaryRed;
                    if (r.IsPexorisRule) item.BackColor = Theme.RedTint;
                }
                else
                {
                    item.ForeColor = Color.FromArgb(4, 120, 87);
                }

                if (!r.Enabled)
                {
                    item.ForeColor = Theme.TextMuted;
                }

                _rulesList.Items.Add(item);
            }

            _rulesList.EndUpdate();
            _footerStatus.Text = "Showing " + _rulesList.Items.Count + " firewall rules. Drag & drop any executable to block.";
        }
    }
}
