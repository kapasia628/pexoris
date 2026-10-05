using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pexoris.AIShield
{
    public class MainForm : Form
    {
        // Native Window Dragging & Icon setting
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
        private Label btnMinimize;
        private Label btnClose;

        private Panel cardTop;
        private PictureBox picCardIcon;
        private Label lblCardTitle;
        private Label lblCardSubtitle;
        private Label pillStatus;

        private PexorisButton btnPresetMax;
        private PexorisButton btnPresetAIOnly;
        private PexorisButton btnPresetDefaults;

        private Label lblSection;
        private Panel pnlListWrapper;
        private ListView lvPolicies;

        private Panel pnlActions;
        private PexorisButton btnBlockAll;
        private PexorisButton btnBlockRecall;
        private PexorisButton btnBlockCopilot;
        private PexorisButton btnRestoreDefaults;
        private PexorisButton btnRefresh;

        private Panel pnlFooter;
        private CheckBox chkAutoRestartExplorer;
        private Label lblFooterStatus;
        private LinkLabel lnkPexoris;

        private List<ShieldItem> currentItems = new List<ShieldItem>();

        public MainForm()
        {
            SetupAppIcon();
            InitializeComponent();
            ApplyCustomStyles();
            ScanPolicies();
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
                    string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\app.ico");
                    if (File.Exists(icoPath))
                    {
                        this.Icon = new Icon(icoPath);
                    }
                }
                catch { }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (this.Icon != null)
            {
                SendMessage(this.Handle, WM_SETICON, ICON_BIG, (int)this.Icon.Handle);
                SendMessage(this.Handle, WM_SETICON, ICON_SMALL, (int)this.Icon.Handle);
            }
            try
            {
                if (lvPolicies != null && lvPolicies.IsHandleCreated)
                {
                    PexorisTheme.ApplyExplorerTheme(lvPolicies.Handle);
                }
            }
            catch { }
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

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.ClientSize = new Size(860, 640);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = PexorisTheme.AppleCanvas;
            this.Text = "Pexoris AIShield";
            this.DoubleBuffered = true;

            // ==========================================
            // 1. TITLE BAR (H = 44px)
            // ==========================================
            pnlTitleBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(860, 44),
                BackColor = PexorisTheme.AppleWhite
            };
            pnlTitleBar.MouseDown += TitleBar_MouseDown;

            picTitleIcon = new PictureBox
            {
                Location = new Point(14, 11),
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            if (this.Icon != null)
            {
                picTitleIcon.Image = this.Icon.ToBitmap();
            }
            picTitleIcon.MouseDown += TitleBar_MouseDown;

            lblTitleText = new Label
            {
                Text = "Pexoris AIShield • Windows 11 AI & Privacy Defense",
                Font = PexorisTheme.FontBold(10.5f),
                ForeColor = PexorisTheme.TextPrimary,
                Location = new Point(42, 12),
                AutoSize = true,
                UseMnemonic = false
            };
            lblTitleText.MouseDown += TitleBar_MouseDown;

            btnMinimize = new Label
            {
                Text = "—",
                Font = PexorisTheme.FontBody(11f),
                ForeColor = PexorisTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(44, 44),
                Location = new Point(860 - 88, 0),
                Cursor = Cursors.Hand
            };
            btnMinimize.MouseEnter += (s, e) => btnMinimize.BackColor = PexorisTheme.AppleCardHover;
            btnMinimize.MouseLeave += (s, e) => btnMinimize.BackColor = Color.Transparent;
            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            btnClose = new Label
            {
                Text = "✕",
                Font = PexorisTheme.FontBody(10.5f),
                ForeColor = PexorisTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(44, 44),
                Location = new Point(860 - 44, 0),
                Cursor = Cursors.Hand
            };
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = PexorisTheme.AppleRed; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = PexorisTheme.TextSecondary; };
            btnClose.Click += (s, e) => this.Close();

            pnlTitleBar.Controls.Add(picTitleIcon);
            pnlTitleBar.Controls.Add(lblTitleText);
            pnlTitleBar.Controls.Add(btnMinimize);
            pnlTitleBar.Controls.Add(btnClose);

            // ==========================================
            // 2. TOP CONTROL CARD (Y = 56, H = 124px)
            // ==========================================
            cardTop = new Panel
            {
                Location = new Point(20, 56),
                Size = new Size(820, 124),
                BackColor = PexorisTheme.AppleWhite
            };
            cardTop.Paint += CardTop_Paint;

            picCardIcon = new PictureBox
            {
                Location = new Point(18, 14),
                Size = new Size(46, 46),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            if (this.Icon != null)
            {
                picCardIcon.Image = this.Icon.ToBitmap();
            }

            lblCardTitle = new Label
            {
                Text = "Windows 11 AI, Recall & Telemetry Shield",
                Font = PexorisTheme.FontHeader(14.5f),
                ForeColor = PexorisTheme.TextPrimary,
                Location = new Point(74, 16),
                AutoSize = true,
                UseMnemonic = false
            };

            lblCardSubtitle = new Label
            {
                Text = "1-Click disable invasive background screenshots, Copilot AI, and Bing search telemetry.",
                Font = PexorisTheme.FontBody(10f),
                ForeColor = PexorisTheme.TextSecondary,
                Location = new Point(76, 42),
                AutoSize = true,
                UseMnemonic = false
            };

            pillStatus = new Label
            {
                Text = "SCANNING SYSTEM...",
                Font = PexorisTheme.FontBold(9f),
                ForeColor = PexorisTheme.AppleAmber,
                BackColor = PexorisTheme.AppleAmberLight,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(180, 28),
                Location = new Point(820 - 200, 16)
            };

            // Quick Presets Row inside Top Card
            btnPresetMax = CreatePillButton("⚡ Maximum Shield (8/8)", new Point(76, 74), 180, true);
            btnPresetMax.Click += (s, e) => BlockAllAction();

            btnPresetAIOnly = CreatePillButton("🤖 AI & Recall Only (2/8)", new Point(266, 74), 170, true);
            btnPresetAIOnly.Click += (s, e) => BlockAIOnlyAction();

            btnPresetDefaults = CreatePillButton("↺ Windows Defaults", new Point(446, 74), 160, false);
            btnPresetDefaults.Click += (s, e) => RestoreDefaultsAction();

            cardTop.Controls.Add(picCardIcon);
            cardTop.Controls.Add(lblCardTitle);
            cardTop.Controls.Add(lblCardSubtitle);
            cardTop.Controls.Add(pillStatus);
            cardTop.Controls.Add(btnPresetMax);
            cardTop.Controls.Add(btnPresetAIOnly);
            cardTop.Controls.Add(btnPresetDefaults);

            // ==========================================
            // 3. EXPLORER LISTVIEW TABLE (Y = 194, H = 298px)
            // ==========================================
            lblSection = new Label
            {
                Text = "PROTECTED COMPONENTS & WINDOWS 11 TELEMETRY STATUS",
                Font = PexorisTheme.FontBold(9.5f),
                ForeColor = PexorisTheme.TextSecondary,
                Location = new Point(24, 194),
                AutoSize = true,
                UseMnemonic = false
            };

            pnlListWrapper = new Panel
            {
                Location = new Point(20, 218),
                Size = new Size(820, 274),
                BackColor = PexorisTheme.AppleWhite
            };
            pnlListWrapper.Paint += (s, e) =>
            {
                using (Pen p = new Pen(PexorisTheme.BorderLight, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlListWrapper.Width - 1, pnlListWrapper.Height - 1);
                }
            };

            lvPolicies = new ListView
            {
                Location = new Point(1, 1),
                Size = new Size(818, 272),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = PexorisTheme.FontBody(9.5f),
                BackColor = PexorisTheme.AppleWhite
            };

            lvPolicies.Columns.Add("Protected Component / Telemetry Feature", 270);
            lvPolicies.Columns.Add("Category", 130);
            lvPolicies.Columns.Add("Status", 130);
            lvPolicies.Columns.Add("Privacy Impact", 120);
            lvPolicies.Columns.Add("Registry Policy Key", 160);

            PexorisTheme.ApplyExplorerTheme(lvPolicies.Handle);
            pnlListWrapper.Controls.Add(lvPolicies);

            // ==========================================
            // 4. ACTION BAR (Y = 502, H = 50px)
            // ==========================================
            pnlActions = new Panel
            {
                Location = new Point(20, 502),
                Size = new Size(820, 50),
                BackColor = Color.Transparent
            };

            // Slot 1: Primary Action (Red Button)
            btnBlockAll = new PexorisButton
            {
                Text = "⚡ Block All AI & Telemetry",
                Font = PexorisTheme.FontBold(10f),
                Size = new Size(210, 42),
                Location = new Point(0, 4),
                StyleType = PexorisButton.ButtonStyleType.DestructiveRed,
                CornerRadius = 8f
            };
            btnBlockAll.Click += (s, e) => BlockAllAction();

            // Slot 2: Blue Action
            btnBlockRecall = new PexorisButton
            {
                Text = "🛡️ Block Recall Only",
                Font = PexorisTheme.FontBold(9.5f),
                Size = new Size(160, 42),
                Location = new Point(220, 4),
                StyleType = PexorisButton.ButtonStyleType.PrimaryBlue,
                CornerRadius = 8f
            };
            btnBlockRecall.Click += (s, e) => ToggleSingle("recall");

            // Slot 3: Blue Action
            btnBlockCopilot = new PexorisButton
            {
                Text = "🤖 Block Copilot Only",
                Font = PexorisTheme.FontBold(9.5f),
                Size = new Size(160, 42),
                Location = new Point(390, 4),
                StyleType = PexorisButton.ButtonStyleType.PrimaryBlue,
                CornerRadius = 8f
            };
            btnBlockCopilot.Click += (s, e) => ToggleSingle("copilot");

            // Slot 4: Outline Action
            btnRestoreDefaults = new PexorisButton
            {
                Text = "↺ Restore Defaults",
                Font = PexorisTheme.FontBold(9.5f),
                Size = new Size(140, 42),
                Location = new Point(560, 4),
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                CornerRadius = 8f
            };
            btnRestoreDefaults.Click += (s, e) => RestoreDefaultsAction();

            // Slot 5: Scan / Refresh
            btnRefresh = new PexorisButton
            {
                Text = "🔄 Scan",
                Font = PexorisTheme.FontBold(9.5f),
                Size = new Size(100, 42),
                Location = new Point(710, 4),
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                CornerRadius = 8f
            };
            btnRefresh.Click += (s, e) => ScanPolicies();

            pnlActions.Controls.Add(btnBlockAll);
            pnlActions.Controls.Add(btnBlockRecall);
            pnlActions.Controls.Add(btnBlockCopilot);
            pnlActions.Controls.Add(btnRestoreDefaults);
            pnlActions.Controls.Add(btnRefresh);

            // ==========================================
            // 5. FOOTER (Y = 598, H = 42px)
            // ==========================================
            pnlFooter = new Panel
            {
                Location = new Point(0, 598),
                Size = new Size(860, 42),
                BackColor = PexorisTheme.AppleWhite
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(PexorisTheme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
                }
            };

            chkAutoRestartExplorer = new CheckBox
            {
                Text = "Auto-restart Explorer to apply UI instantly",
                Checked = true,
                Font = PexorisTheme.FontBody(9f),
                ForeColor = PexorisTheme.TextSecondary,
                Location = new Point(20, 10),
                AutoSize = true
            };

            lblFooterStatus = new Label
            {
                Text = "Ready • 8 components scanned",
                Font = PexorisTheme.FontBody(9f),
                ForeColor = PexorisTheme.TextMuted,
                Location = new Point(310, 12),
                AutoSize = true
            };

            lnkPexoris = new LinkLabel
            {
                Text = "pexoris.com",
                Font = PexorisTheme.FontBold(9.5f),
                LinkColor = PexorisTheme.AppleIndigo,
                Location = new Point(860 - 110, 12),
                AutoSize = true
            };
            lnkPexoris.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com/tools/ai-shield/"); } catch { }
            };

            pnlFooter.Controls.Add(chkAutoRestartExplorer);
            pnlFooter.Controls.Add(lblFooterStatus);
            pnlFooter.Controls.Add(lnkPexoris);

            // Add all controls to Form
            this.Controls.Add(pnlTitleBar);
            this.Controls.Add(cardTop);
            this.Controls.Add(lblSection);
            this.Controls.Add(pnlListWrapper);
            this.Controls.Add(pnlActions);
            this.Controls.Add(pnlFooter);

            this.ResumeLayout(false);
        }

        private PexorisButton CreatePillButton(string text, Point loc, int width, bool isPrimary)
        {
            PexorisButton btn = new PexorisButton
            {
                Text = text,
                Location = loc,
                Size = new Size(width, 32),
                CornerRadius = 6f,
                Font = PexorisTheme.FontBold(8.5f),
                StyleType = isPrimary ? PexorisButton.ButtonStyleType.PresetIndigo : PexorisButton.ButtonStyleType.PresetGray
            };
            return btn;
        }

        private void ApplyCustomStyles()
        {
            // Title bar border
            pnlTitleBar.Paint += (s, e) =>
            {
                using (Pen p = new Pen(PexorisTheme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(p, 0, pnlTitleBar.Height - 1, pnlTitleBar.Width, pnlTitleBar.Height - 1);
                }
            };
        }

        private void CardTop_Paint(object sender, PaintEventArgs e)
        {
            PexorisTheme.DrawCard(e.Graphics, new Rectangle(0, 0, cardTop.Width, cardTop.Height));
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        // ==========================================
        // SCAN & LOAD
        // ==========================================
        private void ScanPolicies()
        {
            lvPolicies.Items.Clear();
            currentItems = AIShieldHelper.ScanAll();

            int shieldedCount = 0;

            foreach (var item in currentItems)
            {
                if (item.IsShielded) shieldedCount++;

                ListViewItem lvi = new ListViewItem(item.Name);
                lvi.SubItems.Add(item.Category);

                if (item.IsShielded)
                {
                    lvi.SubItems.Add("🛡️ BLOCKED / OFF");
                    lvi.BackColor = PexorisTheme.AppleGreenLight;
                    lvi.ForeColor = Color.FromArgb(20, 83, 45); // Dark Green
                }
                else
                {
                    lvi.SubItems.Add("⚠️ ACTIVE / EXPOSED");
                    lvi.BackColor = PexorisTheme.AppleRedLight;
                    lvi.ForeColor = Color.FromArgb(153, 27, 27); // Dark Red
                }

                lvi.SubItems.Add(item.PrivacyImpact);
                lvi.SubItems.Add(item.RegistryPath);
                lvi.Tag = item;

                lvPolicies.Items.Add(lvi);
            }

            // Update Pill Status
            if (shieldedCount == currentItems.Count)
            {
                pillStatus.Text = "🛡️ SHIELDED (8/8)";
                pillStatus.BackColor = PexorisTheme.AppleGreenLight;
                pillStatus.ForeColor = PexorisTheme.AppleGreen;
                lblFooterStatus.Text = "System fully shielded • AI telemetry disabled";
            }
            else if (shieldedCount == 0)
            {
                pillStatus.Text = "⚠️ AT RISK (0/8)";
                pillStatus.BackColor = PexorisTheme.AppleRedLight;
                pillStatus.ForeColor = PexorisTheme.AppleRed;
                lblFooterStatus.Text = "Windows defaults active • AI telemetry enabled";
            }
            else
            {
                pillStatus.Text = string.Format("⚖️ PARTIAL ({0}/{1})", shieldedCount, currentItems.Count);
                pillStatus.BackColor = PexorisTheme.AppleAmberLight;
                pillStatus.ForeColor = PexorisTheme.AppleAmber;
                lblFooterStatus.Text = string.Format("{0} of {1} components shielded", shieldedCount, currentItems.Count);
            }
        }

        // ==========================================
        // ACTIONS
        // ==========================================
        private void BlockAllAction()
        {
            try
            {
                AIShieldHelper.ApplyAll(true);
                if (chkAutoRestartExplorer.Checked)
                {
                    AIShieldHelper.RestartExplorer();
                }
                ScanPolicies();
                MessageBox.Show("All 8 Windows 11 AI, Recall, and Telemetry components have been completely blocked!", "Pexoris AIShield", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error applying policies: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BlockAIOnlyAction()
        {
            try
            {
                AIShieldHelper.SetProtection("recall", true);
                AIShieldHelper.SetProtection("copilot", true);
                if (chkAutoRestartExplorer.Checked)
                {
                    AIShieldHelper.RestartExplorer();
                }
                ScanPolicies();
                MessageBox.Show("Windows Recall and Copilot have been disabled successfully.", "Pexoris AIShield", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error applying AI policies: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestoreDefaultsAction()
        {
            DialogResult dr = MessageBox.Show("Are you sure you want to restore default Windows 11 settings? This will re-enable Microsoft AI and telemetry services.", "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                try
                {
                    AIShieldHelper.ApplyAll(false);
                    if (chkAutoRestartExplorer.Checked)
                    {
                        AIShieldHelper.RestartExplorer();
                    }
                    ScanPolicies();
                    MessageBox.Show("Windows default settings restored.", "Pexoris AIShield", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error restoring defaults: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ToggleSingle(string id)
        {
            try
            {
                AIShieldHelper.SetProtection(id, true);
                if (chkAutoRestartExplorer.Checked)
                {
                    AIShieldHelper.RestartExplorer();
                }
                ScanPolicies();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating component: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
