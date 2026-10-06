using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisDefenderToggle
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

        private PexorisButton btnTopToggle;
        private PexorisButton btnTopAddDevFolder;
        private PexorisButton btnTopWhitelistCompilers;

        private Label lblSection;
        private TextBox txtSearch;
        private PexorisButton btnFilterAll;
        private PexorisButton btnFilterFolders;
        private PexorisButton btnFilterProcs;

        private Panel pnlListWrapper;
        private ListView lvExclusions;

        private Panel pnlActions;
        private PexorisButton btnToggleRealtime;
        private PexorisButton btnAddFolder;
        private PexorisButton btnAddProcess;
        private PexorisButton btnRemoveSelected;
        private PexorisButton btnRefresh;

        private Panel pnlFooter;
        private CheckBox chkAutoRestoreOnExit;
        private Label lblFooterStatus;
        private LinkLabel lnkBrand;

        private ContextMenuStrip ctxMenu;

        private List<DefenderExclusionItem> _allExclusions = new List<DefenderExclusionItem>();
        private DefenderStatusInfo _currentStatus = new DefenderStatusInfo();
        private string _activeFilter = "ALL";

        public MainForm()
        {
            InitializeComponent();
            ApplyCustomDropShadow();
            LoadAppIcon();
            RefreshAll();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Pexoris Defender Toggle";
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
                Text = "Pexoris Defender Toggle",
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
                Location = new Point(210, 12),
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
            btnClose.Click += (s, e) =>
            {
                if (chkAutoRestoreOnExit.Checked && !_currentStatus.IsRealtimeProtectionEnabled)
                {
                    DefenderHelper.SetRealtimeProtection(true);
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
                Text = "Windows Defender & Dev Shield Toggle",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                Location = new Point(72, 16),
                AutoSize = true
            };

            lblCardSubtitle = new Label
            {
                Text = "Pause real-time compilation locks & manage developer folder exclusions without breaking definitions.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                Location = new Point(74, 42),
                AutoSize = true
            };

            pillStatus = new Label
            {
                Text = "● Checking...",
                Font = Theme.FontSmall,
                ForeColor = Theme.SuccessGreen,
                BackColor = Color.FromArgb(236, 253, 245),
                Location = new Point(650, 16),
                Size = new Size(160, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pillStatus.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(167, 243, 208)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pillStatus.Width - 1, pillStatus.Height - 1);
                }
            };

            btnTopToggle = new PexorisButton
            {
                Text = "⚡ Pause Real-Time (Dev Mode)",
                Style = PexorisButtonStyle.WarningAmber,
                Location = new Point(74, 72),
                Size = new Size(210, 30),
                CornerRadius = 6f
            };
            btnTopToggle.Click += BtnToggleRealtime_Click;

            btnTopAddDevFolder = new PexorisButton
            {
                Text = "📁 Whitelist Dev Folder",
                Style = PexorisButtonStyle.PrimaryBlue,
                Location = new Point(292, 72),
                Size = new Size(170, 30),
                CornerRadius = 6f
            };
            btnTopAddDevFolder.Click += BtnAddFolder_Click;

            btnTopWhitelistCompilers = new PexorisButton
            {
                Text = "⚡ Whitelist Compilers",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(470, 72),
                Size = new Size(165, 30),
                CornerRadius = 6f
            };
            btnTopWhitelistCompilers.Click += BtnWhitelistCompilers_Click;

            cardTop.Controls.Add(picCardIcon);
            cardTop.Controls.Add(lblCardTitle);
            cardTop.Controls.Add(lblCardSubtitle);
            cardTop.Controls.Add(pillStatus);
            cardTop.Controls.Add(btnTopToggle);
            cardTop.Controls.Add(btnTopAddDevFolder);
            cardTop.Controls.Add(btnTopWhitelistCompilers);

            // 3. Filter Row + Search (Y=176, H=34)
            lblSection = new Label
            {
                Text = "ACTIVE DEFENDER EXCLUSIONS & WHITELISTED TARGETS",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Location = new Point(16, 178),
                AutoSize = true
            };

            btnFilterAll = new PexorisButton
            {
                Text = "All",
                Style = PexorisButtonStyle.PrimaryBlue,
                Location = new Point(360, 172),
                Size = new Size(60, 28),
                CornerRadius = 5f
            };
            btnFilterAll.Click += (s, e) => SetFilter("ALL", btnFilterAll);

            btnFilterFolders = new PexorisButton
            {
                Text = "Folders",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(426, 172),
                Size = new Size(75, 28),
                CornerRadius = 5f
            };
            btnFilterFolders.Click += (s, e) => SetFilter("FOLDERS", btnFilterFolders);

            btnFilterProcs = new PexorisButton
            {
                Text = "Processes",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(507, 172),
                Size = new Size(85, 28),
                CornerRadius = 5f
            };
            btnFilterProcs.Click += (s, e) => SetFilter("PROCS", btnFilterProcs);

            txtSearch = new TextBox
            {
                Location = new Point(600, 174),
                Size = new Size(244, 24),
                Font = Theme.FontRegular,
                ForeColor = Theme.TextSub,
                Text = "Search exclusions..."
            };
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "Search exclusions...") { txtSearch.Text = ""; txtSearch.ForeColor = Theme.TextHero; } };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Search exclusions..."; txtSearch.ForeColor = Theme.TextSub; } };
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

            lvExclusions = new ListView
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

            lvExclusions.Columns.Add("Type", 120);
            lvExclusions.Columns.Add("Target Exclusion Path / Process Name", 450);
            lvExclusions.Columns.Add("Disk State", 120);
            lvExclusions.Columns.Add("Protection State", 120);

            lvExclusions.DoubleClick += LvExclusions_DoubleClick;
            lvExclusions.SelectedIndexChanged += LvExclusions_SelectedIndexChanged;

            // Context Menu
            ctxMenu = new ContextMenuStrip();
            ToolStripMenuItem miOpen = new ToolStripMenuItem("Open Folder in Explorer", null, (s, e) => OpenSelectedInExplorer());
            ToolStripMenuItem miCopy = new ToolStripMenuItem("Copy Path to Clipboard", null, (s, e) => CopySelectedPath());
            ToolStripSeparator miSep = new ToolStripSeparator();
            ToolStripMenuItem miRemove = new ToolStripMenuItem("Remove Exclusion", null, (s, e) => RemoveSelectedExclusions());
            ctxMenu.Items.AddRange(new ToolStripItem[] { miOpen, miCopy, miSep, miRemove });
            lvExclusions.ContextMenuStrip = ctxMenu;

            pnlListWrapper.Controls.Add(lvExclusions);

            // 5. Action Buttons Bar (Y=508, H=48)
            pnlActions = new Panel
            {
                Location = new Point(16, 508),
                Size = new Size(828, 48),
                BackColor = Color.Transparent
            };

            btnToggleRealtime = new PexorisButton
            {
                Text = "⚡ Pause Real-Time",
                Style = PexorisButtonStyle.WarningAmber,
                Location = new Point(0, 4),
                Size = new Size(170, 40),
                CornerRadius = 7f
            };
            btnToggleRealtime.Click += BtnToggleRealtime_Click;

            btnAddFolder = new PexorisButton
            {
                Text = "📁 Whitelist Folder",
                Style = PexorisButtonStyle.PrimaryBlue,
                Location = new Point(180, 4),
                Size = new Size(150, 40),
                CornerRadius = 7f
            };
            btnAddFolder.Click += BtnAddFolder_Click;

            btnAddProcess = new PexorisButton
            {
                Text = "+ Whitelist Process",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(340, 4),
                Size = new Size(150, 40),
                CornerRadius = 7f
            };
            btnAddProcess.Click += BtnAddProcess_Click;

            btnRemoveSelected = new PexorisButton
            {
                Text = "🗑 Remove Selected",
                Style = PexorisButtonStyle.DestructiveRed,
                Location = new Point(500, 4),
                Size = new Size(160, 40),
                CornerRadius = 7f,
                Enabled = false
            };
            btnRemoveSelected.Click += (s, e) => RemoveSelectedExclusions();

            btnRefresh = new PexorisButton
            {
                Text = "🔄 Refresh",
                Style = PexorisButtonStyle.SecondaryOutline,
                Location = new Point(670, 4),
                Size = new Size(158, 40),
                CornerRadius = 7f
            };
            btnRefresh.Click += (s, e) => RefreshAll();

            pnlActions.Controls.Add(btnToggleRealtime);
            pnlActions.Controls.Add(btnAddFolder);
            pnlActions.Controls.Add(btnAddProcess);
            pnlActions.Controls.Add(btnRemoveSelected);
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

            chkAutoRestoreOnExit = new CheckBox
            {
                Text = "Auto-re-enable Real-Time Protection on exit",
                Checked = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                Location = new Point(16, 9),
                AutoSize = true
            };

            lblFooterStatus = new Label
            {
                Text = "Ready • Windows Defender status indexed",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Location = new Point(295, 11),
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

            pnlFooter.Controls.Add(chkAutoRestoreOnExit);
            pnlFooter.Controls.Add(lblFooterStatus);
            pnlFooter.Controls.Add(lnkBrand);

            // Add controls to form
            Controls.Add(pnlListWrapper);
            Controls.Add(lblSection);
            Controls.Add(btnFilterAll);
            Controls.Add(btnFilterFolders);
            Controls.Add(btnFilterProcs);
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

        private void ApplyCustomDropShadow() { }

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
            btnFilterFolders.Style = PexorisButtonStyle.SecondaryOutline;
            btnFilterProcs.Style = PexorisButtonStyle.SecondaryOutline;

            activeBtn.Style = PexorisButtonStyle.PrimaryBlue;

            btnFilterAll.Invalidate();
            btnFilterFolders.Invalidate();
            btnFilterProcs.Invalidate();

            PopulateListView();
        }

        private void RefreshAll()
        {
            Cursor = Cursors.WaitCursor;
            lblFooterStatus.Text = "Querying Windows Defender status & exclusions...";

            try
            {
                _currentStatus = DefenderHelper.GetDefenderStatus();
                UpdateStatusUI();

                _allExclusions = DefenderHelper.GetAllExclusions();
                PopulateListView();

                lblFooterStatus.Text = string.Format("Defender is {0}. {1} active exclusions registered.",
                    _currentStatus.IsRealtimeProtectionEnabled ? "Active" : "Paused", _allExclusions.Count);
            }
            catch (Exception ex)
            {
                lblFooterStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void UpdateStatusUI()
        {
            if (_currentStatus.IsRealtimeProtectionEnabled)
            {
                pillStatus.Text = "● Active Protection";
                pillStatus.ForeColor = Theme.SuccessGreen;
                pillStatus.BackColor = Color.FromArgb(236, 253, 245);

                btnTopToggle.Text = "⚡ Pause Real-Time (Dev Mode)";
                btnTopToggle.Style = PexorisButtonStyle.WarningAmber;

                btnToggleRealtime.Text = "⚡ Pause Real-Time";
                btnToggleRealtime.Style = PexorisButtonStyle.WarningAmber;
            }
            else
            {
                pillStatus.Text = "⏸ Paused (Dev Mode)";
                pillStatus.ForeColor = Theme.DestructiveRed;
                pillStatus.BackColor = Color.FromArgb(254, 242, 242);

                btnTopToggle.Text = "✓ Enable Full Protection";
                btnTopToggle.Style = PexorisButtonStyle.SuccessGreen;

                btnToggleRealtime.Text = "✓ Enable Protection";
                btnToggleRealtime.Style = PexorisButtonStyle.SuccessGreen;
            }

            pillStatus.Invalidate();
            btnTopToggle.Invalidate();
            btnToggleRealtime.Invalidate();
        }

        private void PopulateListView()
        {
            lvExclusions.BeginUpdate();
            lvExclusions.Items.Clear();

            string search = txtSearch.Text == "Search exclusions..." ? "" : txtSearch.Text.Trim().ToLowerInvariant();

            foreach (DefenderExclusionItem item in _allExclusions)
            {
                if (_activeFilter == "FOLDERS" && item.Type != ExclusionType.FolderPath) continue;
                if (_activeFilter == "PROCS" && item.Type != ExclusionType.ProcessName) continue;

                if (!string.IsNullOrEmpty(search))
                {
                    if (item.Target != null && !item.Target.ToLowerInvariant().Contains(search)) continue;
                }

                ListViewItem lvi = new ListViewItem(item.TypeDisplay);
                lvi.SubItems.Add(item.Target);

                string diskState = item.Type == ExclusionType.FolderPath ? (item.ExistsOnDisk ? "✓ Found on Disk" : "⚠️ Missing Path") : "Global Process";
                lvi.SubItems.Add(diskState);
                lvi.SubItems.Add("Active Exclusion");

                lvi.Tag = item;

                if (item.Type == ExclusionType.FolderPath && !item.ExistsOnDisk)
                {
                    lvi.ForeColor = Color.FromArgb(185, 28, 28);
                }
                else
                {
                    lvi.ForeColor = Theme.TextHero;
                }

                lvExclusions.Items.Add(lvi);
            }

            lvExclusions.EndUpdate();
            UpdateActionButtonStates();
        }

        private void LvExclusions_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionButtonStates();
        }

        private void UpdateActionButtonStates()
        {
            btnRemoveSelected.Enabled = lvExclusions.SelectedItems.Count > 0;
        }

        private void BtnToggleRealtime_Click(object sender, EventArgs e)
        {
            bool newTargetState = !_currentStatus.IsRealtimeProtectionEnabled;

            Cursor = Cursors.WaitCursor;
            lblFooterStatus.Text = (newTargetState ? "Restoring" : "Pausing") + " Defender Real-Time Protection...";

            DefenderHelper.SetRealtimeProtection(newTargetState);

            // Allow 1 second for Windows Security backend to acknowledge
            System.Threading.Thread.Sleep(1000);
            RefreshAll();
        }

        private void BtnAddFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select Developer Project or Compilation Folder to Whitelist from Defender Scans:";
                if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(fbd.SelectedPath))
                {
                    Cursor = Cursors.WaitCursor;
                    if (DefenderHelper.AddFolderExclusion(fbd.SelectedPath))
                    {
                        lblFooterStatus.Text = "Whitelisted folder: " + fbd.SelectedPath;
                    }
                    RefreshAll();
                }
            }
        }

        private void BtnAddProcess_Click(object sender, EventArgs e)
        {
            string procName = ShowTextInputDialog("Whitelist Process Exclusion",
                "Enter process name or compiler executable to whitelist (e.g., rustc.exe, node.exe, cl.exe):",
                "rustc.exe");

            if (!string.IsNullOrEmpty(procName))
            {
                Cursor = Cursors.WaitCursor;
                if (DefenderHelper.AddProcessExclusion(procName.Trim()))
                {
                    lblFooterStatus.Text = "Whitelisted process: " + procName.Trim();
                }
                RefreshAll();
            }
        }

        private static string ShowTextInputDialog(string title, string prompt, string defaultValue)
        {
            using (Form promptForm = new Form())
            {
                promptForm.Width = 440;
                promptForm.Height = 180;
                promptForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                promptForm.Text = title;
                promptForm.StartPosition = FormStartPosition.CenterScreen;
                promptForm.MaximizeBox = false;
                promptForm.MinimizeBox = false;
                promptForm.BackColor = Theme.CardBg;

                Label textLabel = new Label() { Left = 20, Top = 20, Width = 380, Height = 36, Text = prompt, Font = Theme.FontRegular };
                TextBox textBox = new TextBox() { Left = 20, Top = 62, Width = 380, Text = defaultValue, Font = Theme.FontRegular };
                Button confirmation = new Button() { Text = "Whitelist", Left = 280, Width = 120, Top = 96, DialogResult = DialogResult.OK, Height = 30, BackColor = Theme.PrimaryBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                confirmation.FlatAppearance.BorderSize = 0;

                promptForm.Controls.Add(textLabel);
                promptForm.Controls.Add(textBox);
                promptForm.Controls.Add(confirmation);
                promptForm.AcceptButton = confirmation;

                return promptForm.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : string.Empty;
            }
        }

        private void BtnWhitelistCompilers_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show(
                "Pexoris will whitelist standard high-overhead compilation executables:\n\n" +
                "• rustc.exe & cargo.exe (Rust)\n" +
                "• csc.exe & cl.exe (.NET & C++ MSVC)\n" +
                "• gcc.exe & go.exe (MinGW & Golang)\n" +
                "• node.exe (Node.js & npm scripts)\n\n" +
                "This dramatically cuts build delays caused by MsMpEng.exe.\n\nProceed?",
                "Whitelist Compiler Binaries", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                Cursor = Cursors.WaitCursor;
                DefenderHelper.WhitelistDevCompilersPreset();
                lblFooterStatus.Text = "Compiler executables whitelisted successfully.";
                RefreshAll();
            }
        }

        private void RemoveSelectedExclusions()
        {
            if (lvExclusions.SelectedItems.Count == 0) return;

            DialogResult res = MessageBox.Show(
                "Are you sure you want to remove the selected exclusions from Windows Defender?",
                "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res != DialogResult.Yes) return;

            Cursor = Cursors.WaitCursor;
            foreach (ListViewItem lvi in lvExclusions.SelectedItems)
            {
                DefenderExclusionItem item = lvi.Tag as DefenderExclusionItem;
                if (item != null)
                {
                    if (item.Type == ExclusionType.FolderPath)
                    {
                        DefenderHelper.RemoveFolderExclusion(item.Target);
                    }
                    else if (item.Type == ExclusionType.ProcessName)
                    {
                        DefenderHelper.RemoveProcessExclusion(item.Target);
                    }
                }
            }

            lblFooterStatus.Text = "Selected exclusions removed.";
            RefreshAll();
        }

        private void LvExclusions_DoubleClick(object sender, EventArgs e)
        {
            OpenSelectedInExplorer();
        }

        private void OpenSelectedInExplorer()
        {
            if (lvExclusions.SelectedItems.Count == 0) return;
            DefenderExclusionItem item = lvExclusions.SelectedItems[0].Tag as DefenderExclusionItem;
            if (item != null && item.Type == ExclusionType.FolderPath && Directory.Exists(item.Target))
            {
                try { Process.Start("explorer.exe", "\"" + item.Target + "\""); } catch { }
            }
        }

        private void CopySelectedPath()
        {
            if (lvExclusions.SelectedItems.Count == 0) return;
            DefenderExclusionItem item = lvExclusions.SelectedItems[0].Tag as DefenderExclusionItem;
            if (item != null && !string.IsNullOrEmpty(item.Target))
            {
                Clipboard.SetText(item.Target);
                lblFooterStatus.Text = "Target copied to clipboard.";
            }
        }
    }
}
