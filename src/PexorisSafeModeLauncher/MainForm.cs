using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace PexorisSafeModeLauncher
{
    public class MainForm : Form
    {
        private Panel pnlTitleBar;
        private Label lblTitle;
        private Label lblSubTitle;
        private PictureBox pbIcon;
        private Button btnClose;
        private Button btnMinimize;

        private Panel pnlTopCard;
        private PictureBox pbCardIcon;
        private Label lblCardHeadline;
        private Label lblCardSub;
        private Label lblStatusPill;

        private Panel pnlStatTile1;
        private Panel pnlStatTile2;
        private Panel pnlStatTile3;
        private Panel pnlStatTile4;

        private Label lblTile1Val;
        private Label lblTile2Val;
        private Label lblTile3Val;
        private Label lblTile4Val;

        private Panel pnlOptions;
        private RadioButton rbMinimal;
        private RadioButton rbNetwork;
        private RadioButton rbCmd;
        private CheckBox chkRebootNow;

        private ListView lvBcd;
        private Panel pnlActionBar;
        private PexorisButton btnApplySafe;
        private PexorisButton btnRestoreNormal;
        private PexorisButton btnToggleF8;
        private PexorisButton btnRefresh;
        private PexorisButton btnWinRE;

        private Panel pnlFooter;
        private Label lblFooterStatus;
        private Label lblFooterTip;
        private LinkLabel lnkWeb;

        private BcdStatus _currentStatus;
        private Icon _appIcon;

        public MainForm()
        {
            InitializeComponent();
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

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.ClientSize = new Size(860, 640);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgCanvas;
            this.Text = "Pexoris SafeModeLauncher";

            // 1. Title Bar (44px)
            pnlTitleBar = new Panel
            {
                Size = new Size(860, 44),
                Location = new Point(0, 0),
                BackColor = Theme.CardWhite
            };
            pnlTitleBar.MouseDown += TitleBar_MouseDown;

            pbIcon = new PictureBox
            {
                Size = new Size(22, 22),
                Location = new Point(14, 11),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            lblTitle = new Label
            {
                Text = "Pexoris SafeModeLauncher v1.0",
                Font = Theme.FontTitle,
                ForeColor = Theme.TextHero,
                AutoSize = true,
                Location = new Point(42, 10)
            };
            lblTitle.MouseDown += TitleBar_MouseDown;

            lblSubTitle = new Label
            {
                Text = "• Windows Safe Mode & BCD Manager",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(292, 14)
            };
            lblSubTitle.MouseDown += TitleBar_MouseDown;

            btnClose = new Button
            {
                Text = "✕",
                Size = new Size(38, 28),
                Location = new Point(814, 8),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Theme.PrimaryRed; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = Theme.TextMuted; };
            btnClose.Click += (s, e) => this.Close();

            btnMinimize = new Button
            {
                Text = "—",
                Size = new Size(38, 28),
                Location = new Point(772, 8),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                Cursor = Cursors.Hand
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.MouseEnter += (s, e) => { btnMinimize.BackColor = Theme.BorderSubtle; };
            btnMinimize.MouseLeave += (s, e) => { btnMinimize.BackColor = Color.Transparent; };
            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            pnlTitleBar.Controls.AddRange(new Control[] { pbIcon, lblTitle, lblSubTitle, btnMinimize, btnClose });

            // 2. Top Control Card (126px)
            pnlTopCard = new Panel
            {
                Size = new Size(828, 126),
                Location = new Point(16, 52),
                BackColor = Theme.CardWhite
            };
            pnlTopCard.Paint += (s, e) => DrawBorder(e.Graphics, pnlTopCard.ClientRectangle);

            pbCardIcon = new PictureBox
            {
                Size = new Size(44, 44),
                Location = new Point(18, 16),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            lblCardHeadline = new Label
            {
                Text = "Windows Safe Mode & Boot Configuration",
                Font = Theme.FontSection,
                ForeColor = Theme.TextHero,
                AutoSize = true,
                Location = new Point(70, 16)
            };

            lblCardSub = new Label
            {
                Text = "Configure BCD boot targets, toggle Legacy F8, and restore normal boot without loops.",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(70, 38)
            };

            lblStatusPill = new Label
            {
                Text = "● Normal Boot",
                Font = Theme.FontSmallBold,
                ForeColor = Theme.GreenSuccess,
                BackColor = Theme.GreenTint,
                Padding = new Padding(6, 4, 6, 4),
                AutoSize = true,
                Location = new Point(690, 18)
            };

            // Stat Tiles (Y = 66, H = 46)
            int tileW = 190;
            pnlStatTile1 = CreateStatTile(18, 66, tileW, "BOOT MODE", "Normal", out lblTile1Val);
            pnlStatTile2 = CreateStatTile(218, 66, tileW, "BOOT MENU POLICY", "Standard", out lblTile2Val);
            pnlStatTile3 = CreateStatTile(418, 66, tileW, "RECOVERY (WinRE)", "Enabled", out lblTile3Val);
            pnlStatTile4 = CreateStatTile(618, 66, tileW, "BITLOCKER C:", "Safe", out lblTile4Val);

            pnlTopCard.Controls.AddRange(new Control[] {
                pbCardIcon, lblCardHeadline, lblCardSub, lblStatusPill,
                pnlStatTile1, pnlStatTile2, pnlStatTile3, pnlStatTile4
            });

            // 3. Mode Selection Strip (42px)
            pnlOptions = new Panel
            {
                Size = new Size(828, 42),
                Location = new Point(16, 186),
                BackColor = Theme.CardWhite
            };
            pnlOptions.Paint += (s, e) => DrawBorder(e.Graphics, pnlOptions.ClientRectangle);

            Label lblOptionsHeader = new Label
            {
                Text = "Safe Mode Target:",
                Font = Theme.FontBodyBold,
                ForeColor = Theme.TextHero,
                AutoSize = true,
                Location = new Point(14, 11)
            };

            rbMinimal = new RadioButton
            {
                Text = "Minimal (Default)",
                Font = Theme.FontBody,
                ForeColor = Theme.TextBody,
                Checked = true,
                AutoSize = true,
                Location = new Point(150, 10),
                Cursor = Cursors.Hand
            };

            rbNetwork = new RadioButton
            {
                Text = "Network (With Internet)",
                Font = Theme.FontBody,
                ForeColor = Theme.TextBody,
                AutoSize = true,
                Location = new Point(295, 10),
                Cursor = Cursors.Hand
            };

            rbCmd = new RadioButton
            {
                Text = "Command Prompt (CMD)",
                Font = Theme.FontBody,
                ForeColor = Theme.TextBody,
                AutoSize = true,
                Location = new Point(475, 10),
                Cursor = Cursors.Hand
            };

            chkRebootNow = new CheckBox
            {
                Text = "Reboot immediately",
                Font = Theme.FontBodyBold,
                ForeColor = Theme.PrimaryRed,
                Checked = true,
                AutoSize = true,
                Location = new Point(665, 11),
                Cursor = Cursors.Hand
            };

            pnlOptions.Controls.AddRange(new Control[] { lblOptionsHeader, rbMinimal, rbNetwork, rbCmd, chkRebootNow });

            // 4. BCD Explorer ListView
            lvBcd = new ListView
            {
                Size = new Size(828, 276),
                Location = new Point(16, 236),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Theme.CardWhite,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.FontBody
            };
            lvBcd.Columns.Add("Parameter", 180);
            lvBcd.Columns.Add("Value", 310);
            lvBcd.Columns.Add("Status", 160);
            lvBcd.Columns.Add("Category", 150);

            // 5. Action Buttons Bar (Y=520, H=50)
            pnlActionBar = new Panel
            {
                Size = new Size(828, 50),
                Location = new Point(16, 520),
                BackColor = Color.Transparent
            };

            btnApplySafe = new PexorisButton
            {
                Text = "⚡ Apply Safe Mode",
                Style = ButtonStyle.DestructiveRed,
                Size = new Size(185, 42),
                Location = new Point(0, 4)
            };
            btnApplySafe.Click += BtnApplySafe_Click;

            btnRestoreNormal = new PexorisButton
            {
                Text = "🛡️ Force Normal Boot",
                Style = ButtonStyle.SuccessGreen,
                Size = new Size(185, 42),
                Location = new Point(195, 4)
            };
            btnRestoreNormal.Click += BtnRestoreNormal_Click;

            btnToggleF8 = new PexorisButton
            {
                Text = "⚙️ Toggle Legacy F8",
                Style = ButtonStyle.PrimaryBlue,
                Size = new Size(165, 42),
                Location = new Point(390, 4)
            };
            btnToggleF8.Click += BtnToggleF8_Click;

            btnWinRE = new PexorisButton
            {
                Text = "🛠️ Reboot to WinRE",
                Style = ButtonStyle.WarningAmber,
                Size = new Size(155, 42),
                Location = new Point(565, 4)
            };
            btnWinRE.Click += BtnWinRE_Click;

            btnRefresh = new PexorisButton
            {
                Text = "🔄 Refresh",
                Style = ButtonStyle.SecondaryOutline,
                Size = new Size(95, 42),
                Location = new Point(730, 4)
            };
            btnRefresh.Click += (s, e) => RefreshStatus();

            pnlActionBar.Controls.AddRange(new Control[] {
                btnApplySafe, btnRestoreNormal, btnToggleF8, btnWinRE, btnRefresh
            });

            // 6. Footer (38px)
            pnlFooter = new Panel
            {
                Size = new Size(860, 38),
                Location = new Point(0, 602),
                BackColor = Theme.CardWhite
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, 860, 0);
                }
            };

            lblFooterStatus = new Label
            {
                Text = "Ready • Standalone Portable",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(16, 10)
            };

            lblFooterTip = new Label
            {
                Text = "💡 Tip: If using Windows Hello PIN, choose Network Safe Mode to avoid login lockout.",
                Font = Theme.FontSmallBold,
                ForeColor = Theme.AmberWarning,
                AutoSize = true,
                Location = new Point(220, 10)
            };

            lnkWeb = new LinkLabel
            {
                Text = "pexoris.com",
                Font = Theme.FontSmallBold,
                LinkColor = Theme.PrimaryRed,
                ActiveLinkColor = Theme.PrimaryRedHover,
                AutoSize = true,
                Location = new Point(775, 10)
            };
            lnkWeb.LinkClicked += (s, e) => Process.Start("https://pexoris.com");

            pnlFooter.Controls.AddRange(new Control[] { lblFooterStatus, lblFooterTip, lnkWeb });

            // Form assembly
            this.Controls.AddRange(new Control[] {
                pnlTitleBar, pnlTopCard, pnlOptions, lvBcd, pnlActionBar, pnlFooter
            });

            this.Load += MainForm_Load;
            this.ResumeLayout(false);
        }

        private Panel CreateStatTile(int x, int y, int w, string title, string initialVal, out Label valLabel)
        {
            Panel p = new Panel
            {
                Size = new Size(w, 48),
                Location = new Point(x, y),
                BackColor = Theme.BgCanvas
            };
            p.Paint += (s, e) => DrawBorder(e.Graphics, p.ClientRectangle);

            Label lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(8, 6)
            };

            valLabel = new Label
            {
                Text = initialVal,
                Font = Theme.FontBodyBold,
                ForeColor = Theme.TextHero,
                AutoSize = true,
                Location = new Point(8, 22)
            };

            p.Controls.Add(lbl);
            p.Controls.Add(valLabel);
            return p;
        }

        private void DrawBorder(Graphics g, Rectangle r)
        {
            using (Pen pen = new Pen(Theme.BorderLight, 1f))
            {
                g.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (_appIcon != null)
                {
                    this.Icon = _appIcon;
                    Theme.SendMessage(this.Handle, Theme.WM_SETICON, (IntPtr)Theme.ICON_BIG, _appIcon.Handle);
                    Theme.SendMessage(this.Handle, Theme.WM_SETICON, (IntPtr)Theme.ICON_SMALL, _appIcon.Handle);
                    pbIcon.Image = _appIcon.ToBitmap();
                    pbCardIcon.Image = _appIcon.ToBitmap();
                }
            }
            catch { }

            RefreshStatus();
        }

        private void RefreshStatus()
        {
            Cursor = Cursors.WaitCursor;
            lblFooterStatus.Text = "Auditing BCD store...";

            _currentStatus = BootEngine.Audit();

            // Update Status Pill & Stats
            if (_currentStatus.Mode == SafeBootMode.Normal)
            {
                lblStatusPill.Text = "● Normal Boot Mode";
                lblStatusPill.ForeColor = Theme.GreenSuccess;
                lblStatusPill.BackColor = Theme.GreenTint;
                lblTile1Val.Text = "Normal Boot";
                lblTile1Val.ForeColor = Theme.GreenSuccess;
            }
            else
            {
                string modeDesc = _currentStatus.Mode == SafeBootMode.Minimal ? "Minimal Safe Mode" :
                                 (_currentStatus.Mode == SafeBootMode.Network ? "Safe Mode (Network)" : "Safe Mode (CMD)");
                lblStatusPill.Text = "● " + modeDesc;
                lblStatusPill.ForeColor = Theme.PrimaryRed;
                lblStatusPill.BackColor = Theme.RedTint;
                lblTile1Val.Text = modeDesc;
                lblTile1Val.ForeColor = Theme.PrimaryRed;
            }

            lblTile2Val.Text = _currentStatus.BootMenuPolicy.Equals("Legacy", StringComparison.OrdinalIgnoreCase) ? "Legacy F8 Active" : "Standard UEFI";
            lblTile3Val.Text = _currentStatus.RecoveryEnabled ? "Enabled" : "Disabled";
            lblTile4Val.Text = _currentStatus.BitLockerStatus;
            lblTile4Val.ForeColor = _currentStatus.BitLockerStatus.Contains("Locked") ? Theme.AmberWarning : Theme.TextHero;

            // Populate ListView
            lvBcd.Items.Clear();
            foreach (var item in _currentStatus.Items)
            {
                ListViewItem lvi = new ListViewItem(item.Parameter);
                lvi.SubItems.Add(item.Value);
                lvi.SubItems.Add(item.Status);
                lvi.SubItems.Add(item.Category);

                if (item.Status.Equals("Modified") || item.Status.Equals("Active"))
                {
                    lvi.ForeColor = Theme.PrimaryRed;
                }
                else if (item.Status.Equals("Warning"))
                {
                    lvi.ForeColor = Theme.AmberWarning;
                }

                lvBcd.Items.Add(lvi);
            }

            lblFooterStatus.Text = "Ready • BCD Status Refreshed";
            Cursor = Cursors.Default;
        }

        private void BtnApplySafe_Click(object sender, EventArgs e)
        {
            SafeBootMode selected = rbMinimal.Checked ? SafeBootMode.Minimal :
                                   (rbNetwork.Checked ? SafeBootMode.Network : SafeBootMode.CommandPrompt);

            string modeName = selected == SafeBootMode.Minimal ? "Minimal Safe Mode" :
                             (selected == SafeBootMode.Network ? "Safe Mode with Networking" : "Safe Mode with Command Prompt");

            DialogResult dr = MessageBox.Show(
                "Are you sure you want to configure Windows to boot into " + modeName + "?" +
                (chkRebootNow.Checked ? "\n\nYour computer will restart immediately." : "\n\nChanges will apply on your next reboot."),
                "Confirm Safe Mode Configuration",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dr == DialogResult.Yes)
            {
                lblFooterStatus.Text = "Applying Safe Mode BCD settings...";
                string result = BootEngine.SetSafeBoot(selected);
                RefreshStatus();

                if (chkRebootNow.Checked)
                {
                    lblFooterStatus.Text = "Restarting computer...";
                    BootEngine.RebootNow();
                }
                else
                {
                    MessageBox.Show(result + "\n\nSafe Mode is staged for the next system restart.", "Pexoris SafeModeLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnRestoreNormal_Click(object sender, EventArgs e)
        {
            lblFooterStatus.Text = "Restoring normal boot configuration...";
            string result = BootEngine.RestoreNormalBoot();
            RefreshStatus();

            MessageBox.Show(result, "Pexoris SafeModeLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnToggleF8_Click(object sender, EventArgs e)
        {
            bool currentlyLegacy = _currentStatus != null && _currentStatus.BootMenuPolicy.Equals("Legacy", StringComparison.OrdinalIgnoreCase);
            bool enable = !currentlyLegacy;

            string msg = enable ? "Enable Legacy F8 boot menu on startup?" : "Restore standard Windows boot menu?";
            if (MessageBox.Show(msg, "Toggle Boot Menu Policy", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string res = BootEngine.ToggleLegacyF8(enable);
                RefreshStatus();
                MessageBox.Show(res, "Pexoris SafeModeLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnWinRE_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                "Would you like to restart into Windows Recovery Environment (Advanced Startup)?\n\nYour computer will restart now.",
                "Reboot to Windows Recovery Environment",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                lblFooterStatus.Text = "Rebooting to Advanced Startup...";
                BootEngine.RebootWinRE();
            }
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Theme.ReleaseCapture();
                Theme.SendMessage(Handle, Theme.WM_NCLBUTTONDOWN, Theme.HT_CAPTION, 0);
            }
        }
    }
}
