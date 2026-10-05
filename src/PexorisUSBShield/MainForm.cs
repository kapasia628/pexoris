using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pexoris.USBShield
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

        private Label lblDriveSelect;
        private ComboBox cmbDrives;
        private PexorisButton btnPresetMax;
        private PexorisButton btnPresetReadOnly;
        private PexorisButton btnPresetDefaults;

        private Label lblSection;
        private Panel pnlListWrapper;
        private ListView lvPolicies;

        private Panel pnlActions;
        private PexorisButton btnBlockAll;
        private PexorisButton btnEnableWriteProtect;
        private PexorisButton btnDisableWriteProtect;
        private PexorisButton btnImmunize;
        private PexorisButton btnRefresh;

        private Panel pnlFooter;
        private CheckBox chkAutoScan;
        private Label lblFooterStatus;
        private LinkLabel lnkPexoris;

        private List<PolicyItem> currentPolicies = new List<PolicyItem>();
        private List<UsbDriveItem> currentDrives = new List<UsbDriveItem>();

        public MainForm()
        {
            SetupAppIcon();
            InitializeComponent();
            ApplyCustomStyles();
            ScanAll();
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
            this.Text = "Pexoris USBShield";
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
                Text = "Pexoris USBShield • Portable USB & Storage Defense",
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
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = PexorisTheme.TextSecondary,
                Size = new Size(46, 44),
                Location = new Point(860 - 92, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnMinimize.MouseEnter += (s, e) => { btnMinimize.BackColor = Color.FromArgb(243, 244, 246); };
            btnMinimize.MouseLeave += (s, e) => { btnMinimize.BackColor = Color.Transparent; };
            btnMinimize.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };

            btnClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = PexorisTheme.TextSecondary,
                Size = new Size(46, 44),
                Location = new Point(860 - 46, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = PexorisTheme.AppleRed; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = PexorisTheme.TextSecondary; };
            btnClose.Click += (s, e) => { this.Close(); };

            pnlTitleBar.Controls.Add(picTitleIcon);
            pnlTitleBar.Controls.Add(lblTitleText);
            pnlTitleBar.Controls.Add(btnMinimize);
            pnlTitleBar.Controls.Add(btnClose);

            // ==========================================
            // 2. TOP CONTROL CARD (Y = 54, H = 126, W = 820)
            // ==========================================
            cardTop = new Panel
            {
                Location = new Point(20, 54),
                Size = new Size(820, 126),
                BackColor = PexorisTheme.AppleWhite
            };

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
                Text = "USB Storage, AutoPlay & Write-Protection Guard",
                Font = PexorisTheme.FontHeader(13f),
                ForeColor = PexorisTheme.TextPrimary,
                Location = new Point(74, 15),
                AutoSize = true,
                UseMnemonic = false
            };

            lblCardSubtitle = new Label
            {
                Text = "Prevent USB worm execution, disable AutoPlay popups, and lock ports to Read-Only mode.",
                Font = PexorisTheme.FontBody(10f),
                ForeColor = PexorisTheme.TextSecondary,
                Location = new Point(76, 39),
                AutoSize = true,
                UseMnemonic = false
            };

            pillStatus = new Label
            {
                Text = "● SCANNING...",
                Font = PexorisTheme.FontBold(8.5f),
                ForeColor = Color.FromArgb(16, 185, 129),
                BackColor = Color.FromArgb(236, 253, 245),
                Location = new Point(820 - 215, 16),
                Size = new Size(195, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false
            };

            // Bottom row in card: Drive selector + Action Presets
            lblDriveSelect = new Label
            {
                Text = "Target USB Drive:",
                Font = PexorisTheme.FontBold(9.5f),
                ForeColor = PexorisTheme.TextPrimary,
                Location = new Point(20, 80),
                AutoSize = true
            };

            cmbDrives = new ComboBox
            {
                Location = new Point(140, 76),
                Size = new Size(290, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = PexorisTheme.FontBody(9.5f),
                BackColor = PexorisTheme.AppleInput
            };

            btnPresetMax = new PexorisButton
            {
                Text = "🛡️ Full Protection",
                Location = new Point(445, 74),
                Size = new Size(115, 30),
                Font = PexorisTheme.FontBold(8.5f),
                StyleType = PexorisButton.ButtonStyleType.PresetEmerald,
                CornerRadius = 6f
            };
            btnPresetMax.Click += (s, e) => { ApplyBlockAll(); };

            btnPresetReadOnly = new PexorisButton
            {
                Text = "🔒 Read-Only",
                Location = new Point(568, 74),
                Size = new Size(110, 30),
                Font = PexorisTheme.FontBold(8.5f),
                StyleType = PexorisButton.ButtonStyleType.PresetBlue,
                CornerRadius = 6f
            };
            btnPresetReadOnly.Click += (s, e) => { ApplyWriteProtect(true); };

            btnPresetDefaults = new PexorisButton
            {
                Text = "↺ Defaults",
                Location = new Point(686, 74),
                Size = new Size(118, 30),
                Font = PexorisTheme.FontBold(8.5f),
                StyleType = PexorisButton.ButtonStyleType.PresetGray,
                CornerRadius = 6f
            };
            btnPresetDefaults.Click += (s, e) => { ApplyRestoreDefaults(); };

            cardTop.Controls.Add(picCardIcon);
            cardTop.Controls.Add(lblCardTitle);
            cardTop.Controls.Add(lblCardSubtitle);
            cardTop.Controls.Add(pillStatus);
            cardTop.Controls.Add(lblDriveSelect);
            cardTop.Controls.Add(cmbDrives);
            cardTop.Controls.Add(btnPresetMax);
            cardTop.Controls.Add(btnPresetReadOnly);
            cardTop.Controls.Add(btnPresetDefaults);

            // ==========================================
            // 3. EXPLORER LISTVIEW (Y = 190, H = 300, W = 820)
            // ==========================================
            lblSection = new Label
            {
                Text = "ACTIVE WINDOWS POLICIES & STORAGE SECURITY CONTROLS",
                Font = PexorisTheme.FontBold(9f),
                ForeColor = PexorisTheme.TextSecondary,
                Location = new Point(22, 190),
                AutoSize = true
            };

            pnlListWrapper = new Panel
            {
                Location = new Point(20, 212),
                Size = new Size(820, 282),
                BackColor = PexorisTheme.AppleWhite
            };

            lvPolicies = new ListView
            {
                Location = new Point(1, 1),
                Size = new Size(818, 280),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = PexorisTheme.FontBody(9.5f),
                BackColor = PexorisTheme.AppleWhite
            };

            lvPolicies.Columns.Add("Protection Policy / Component", 250);
            lvPolicies.Columns.Add("Category", 140);
            lvPolicies.Columns.Add("Status / Setting", 180);
            lvPolicies.Columns.Add("Registry Hive & Path", 240);

            PexorisTheme.ApplyExplorerTheme(lvPolicies.Handle);
            pnlListWrapper.Controls.Add(lvPolicies);

            // ==========================================
            // 4. ACTION BAR (Y = 504, H = 50, W = 820)
            // ==========================================
            pnlActions = new Panel
            {
                Location = new Point(20, 504),
                Size = new Size(820, 50),
                BackColor = Color.Transparent
            };

            // Slot 1: Primary Action (Red)
            btnBlockAll = new PexorisButton
            {
                Text = "⚡ Block All Threats & Protect",
                Location = new Point(0, 5),
                Size = new Size(240, 42),
                Font = PexorisTheme.FontBold(10f),
                StyleType = PexorisButton.ButtonStyleType.DestructiveRed,
                CornerRadius = 8f
            };
            btnBlockAll.Click += (s, e) => { ApplyBlockAll(); };

            // Slot 2: Blue Write-Protect ON
            btnEnableWriteProtect = new PexorisButton
            {
                Text = "🔒 Write-Protect ON",
                Location = new Point(250, 5),
                Size = new Size(170, 42),
                Font = PexorisTheme.FontBold(9.5f),
                StyleType = PexorisButton.ButtonStyleType.PrimaryBlue,
                CornerRadius = 8f
            };
            btnEnableWriteProtect.Click += (s, e) => { ApplyWriteProtect(true); };

            // Slot 3: Outline Write-Protect OFF
            btnDisableWriteProtect = new PexorisButton
            {
                Text = "🔓 Write-Protect OFF",
                Location = new Point(430, 5),
                Size = new Size(170, 42),
                Font = PexorisTheme.FontBold(9.5f),
                StyleType = PexorisButton.ButtonStyleType.BlueOutline,
                CornerRadius = 8f
            };
            btnDisableWriteProtect.Click += (s, e) => { ApplyWriteProtect(false); };

            // Slot 4: Teal Outline Immunize Drive
            btnImmunize = new PexorisButton
            {
                Text = "💉 Immunize USB",
                Location = new Point(610, 5),
                Size = new Size(130, 42),
                Font = PexorisTheme.FontBold(9.5f),
                StyleType = PexorisButton.ButtonStyleType.TealOutline,
                CornerRadius = 8f
            };
            btnImmunize.Click += (s, e) => { ApplyImmunizeDrive(); };

            // Slot 5: Scan / Refresh
            btnRefresh = new PexorisButton
            {
                Text = "🔄 Scan",
                Location = new Point(750, 5),
                Size = new Size(70, 42),
                Font = PexorisTheme.FontBold(9.5f),
                StyleType = PexorisButton.ButtonStyleType.SecondaryOutline,
                CornerRadius = 8f
            };
            btnRefresh.Click += (s, e) => { ScanAll(); };

            pnlActions.Controls.Add(btnBlockAll);
            pnlActions.Controls.Add(btnEnableWriteProtect);
            pnlActions.Controls.Add(btnDisableWriteProtect);
            pnlActions.Controls.Add(btnImmunize);
            pnlActions.Controls.Add(btnRefresh);

            // ==========================================
            // 5. FOOTER (Y = 596, H = 44)
            // ==========================================
            pnlFooter = new Panel
            {
                Location = new Point(0, 596),
                Size = new Size(860, 44),
                BackColor = PexorisTheme.AppleWhite
            };

            chkAutoScan = new CheckBox
            {
                Text = "Auto-scan drives",
                Font = PexorisTheme.FontBody(9f),
                ForeColor = PexorisTheme.TextSecondary,
                Location = new Point(20, 12),
                AutoSize = true,
                Checked = true
            };

            lblFooterStatus = new Label
            {
                Text = "Ready. 6 security policies scanned.",
                Font = PexorisTheme.FontBody(9f),
                ForeColor = PexorisTheme.TextSecondary,
                Location = new Point(170, 14),
                Size = new Size(540, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };

            lnkPexoris = new LinkLabel
            {
                Text = "pexoris.com",
                Font = PexorisTheme.FontBold(9f),
                LinkColor = PexorisTheme.AppleEmerald,
                ActiveLinkColor = PexorisTheme.AppleEmeraldHover,
                Location = new Point(860 - 110, 14),
                AutoSize = true
            };
            lnkPexoris.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://pexoris.com/tools/usb-shield/") { UseShellExecute = true }); }
                catch { }
            };

            pnlFooter.Controls.Add(chkAutoScan);
            pnlFooter.Controls.Add(lblFooterStatus);
            pnlFooter.Controls.Add(lnkPexoris);

            // Add all to Form
            this.Controls.Add(pnlTitleBar);
            this.Controls.Add(cardTop);
            this.Controls.Add(lblSection);
            this.Controls.Add(pnlListWrapper);
            this.Controls.Add(pnlActions);
            this.Controls.Add(pnlFooter);

            this.ResumeLayout(false);
        }

        private void ApplyCustomStyles()
        {
            // TitleBar Bottom Border
            pnlTitleBar.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(PexorisTheme.BorderLight, 1))
                {
                    e.Graphics.DrawLine(pen, 0, pnlTitleBar.Height - 1, pnlTitleBar.Width, pnlTitleBar.Height - 1);
                }
            };

            // Top Card Border & Shadow
            cardTop.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(PexorisTheme.BorderLight, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, cardTop.Width - 1, cardTop.Height - 1);
                }
            };

            // List Wrapper Border
            pnlListWrapper.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(PexorisTheme.BorderLight, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlListWrapper.Width - 1, pnlListWrapper.Height - 1);
                }
            };

            // Footer Top Border
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(PexorisTheme.BorderLight, 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
                }
            };

            // Custom Paint for Capsule Status Badge
            pillStatus.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = PexorisTheme.GetRoundedRectangle(new Rectangle(0, 0, pillStatus.Width - 1, pillStatus.Height - 1), 14))
                using (SolidBrush b = new SolidBrush(pillStatus.BackColor))
                using (Pen p = new Pen(pillStatus.ForeColor, 1))
                {
                    e.Graphics.FillPath(b, path);
                    e.Graphics.DrawPath(p, path);
                }
                TextRenderer.DrawText(e.Graphics, pillStatus.Text, pillStatus.Font, pillStatus.ClientRectangle, pillStatus.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        // ==========================================
        // SCANNING & DATA BINDING
        // ==========================================
        public void ScanAll()
        {
            currentPolicies = USBShieldHelper.ScanPolicies();
            currentDrives = USBShieldHelper.ScanRemovableDrives();

            // Populate Drives Dropdown
            cmbDrives.Items.Clear();
            if (currentDrives.Count == 0)
            {
                cmbDrives.Items.Add("No Removable USB Drive Detected");
                cmbDrives.SelectedIndex = 0;
                btnImmunize.Enabled = false;
            }
            else
            {
                foreach (var d in currentDrives)
                {
                    cmbDrives.Items.Add(d.DisplayText);
                }
                cmbDrives.SelectedIndex = 0;
                btnImmunize.Enabled = true;
            }

            // Populate ListView
            lvPolicies.Items.Clear();
            int protectedCount = 0;
            bool isWriteProtected = false;

            foreach (var item in currentPolicies)
            {
                if (item.IsProtected) protectedCount++;
                if (item.Id == "WriteProtect" && item.IsProtected) isWriteProtected = true;

                ListViewItem lvi = new ListViewItem(item.Name);
                lvi.SubItems.Add(item.Category);
                lvi.SubItems.Add(item.StatusText);
                lvi.SubItems.Add(item.RegPath);
                lvi.Tag = item;

                if (item.IsProtected)
                {
                    lvi.BackColor = Color.FromArgb(240, 253, 244);   // Soft Emerald
                    lvi.ForeColor = Color.FromArgb(22, 101, 52);      // Dark Emerald
                }
                else
                {
                    lvi.BackColor = Color.FromArgb(254, 242, 242);   // Soft Red
                    lvi.ForeColor = Color.FromArgb(153, 27, 27);      // Dark Red
                }

                lvPolicies.Items.Add(lvi);
            }

            // Append Connected Drives to ListView
            if (currentDrives.Count == 0)
            {
                ListViewItem lvi = new ListViewItem("Removable USB Storage");
                lvi.SubItems.Add("USB Hardware");
                lvi.SubItems.Add("No Flash Drive Connected");
                lvi.SubItems.Add("Plug in a USB drive to scan, immunize, or write-protect.");
                lvi.BackColor = Color.FromArgb(249, 250, 251);
                lvi.ForeColor = PexorisTheme.TextMuted;
                lvPolicies.Items.Add(lvi);
            }
            else
            {
                foreach (var d in currentDrives)
                {
                    ListViewItem lvi = new ListViewItem("Removable Drive: " + d.RootPath);
                    lvi.SubItems.Add("Hardware Storage");
                    lvi.SubItems.Add(d.IsImmunized ? "IMMUNIZED (Vaccine Present)" : "EXPOSED (No Vaccine)");
                    lvi.SubItems.Add(d.DisplayText);

                    if (d.IsImmunized)
                    {
                        lvi.BackColor = Color.FromArgb(240, 253, 250);   // Soft Teal
                        lvi.ForeColor = Color.FromArgb(15, 118, 110);
                    }
                    else
                    {
                        lvi.BackColor = Color.FromArgb(254, 243, 199);   // Soft Amber
                        lvi.ForeColor = Color.FromArgb(146, 64, 14);
                    }

                    lvPolicies.Items.Add(lvi);
                }
            }

            // Update Status Capsule Pill
            if (isWriteProtected)
            {
                pillStatus.Text = "🔒 READ-ONLY MODE ACTIVE";
                pillStatus.ForeColor = Color.FromArgb(30, 64, 175);
                pillStatus.BackColor = Color.FromArgb(239, 246, 255);
            }
            else if (protectedCount >= 5)
            {
                pillStatus.Text = string.Format("● SECURE: PROTECTED ({0}/{1})", protectedCount, currentPolicies.Count);
                pillStatus.ForeColor = Color.FromArgb(22, 101, 52);
                pillStatus.BackColor = Color.FromArgb(240, 253, 244);
            }
            else
            {
                pillStatus.Text = string.Format("⚠️ EXPOSED ({0}/{1})", currentPolicies.Count - protectedCount, currentPolicies.Count);
                pillStatus.ForeColor = Color.FromArgb(153, 27, 27);
                pillStatus.BackColor = Color.FromArgb(254, 242, 242);
            }
            pillStatus.Invalidate();

            lblFooterStatus.Text = string.Format("Ready. {0} policies active. {1} removable drive(s) detected.",
                protectedCount, currentDrives.Count);
        }

        // ==========================================
        // ACTION HANDLERS
        // ==========================================
        private void ApplyBlockAll()
        {
            this.Cursor = Cursors.WaitCursor;
            bool ok = USBShieldHelper.BlockAllThreats();
            
            // Also immunize any connected removable drives
            foreach (var d in currentDrives)
            {
                USBShieldHelper.ImmunizeDrive(d.RootPath);
            }

            this.Cursor = Cursors.Default;
            ScanAll();

            if (ok)
            {
                MessageBox.Show(
                    "All USB threats and AutoPlay execution vectors have been BLOCKED successfully!\n\n" +
                    "• AutoPlay disabled (NoDriveTypeAutoRun = 0xFF)\n" +
                    "• Windows Shell NoAutorun enabled\n" +
                    "• IniFileMapping Autorun.inf parser redirected to null\n" +
                    "• Connected USB drives immunized",
                    "Pexoris USBShield — Protection Applied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to apply some policies. Please ensure this tool was run as Administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplyWriteProtect(bool enable)
        {
            this.Cursor = Cursors.WaitCursor;
            bool ok = enable ? USBShieldHelper.EnableWriteProtect() : USBShieldHelper.DisableWriteProtect();
            this.Cursor = Cursors.Default;
            ScanAll();

            if (ok)
            {
                string msg = enable
                    ? "USB Port Write-Protection is now ENABLED.\n\nAll connected and newly inserted USB flash drives are locked to Read-Only mode. Files cannot be modified, deleted, or copied to the drives."
                    : "USB Port Write-Protection is now DISABLED.\n\nFull Read/Write access restored.";
                
                MessageBox.Show(msg, "Pexoris USBShield — Storage Policies", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to update WriteProtect policy in registry. Please run as Administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ApplyRestoreDefaults()
        {
            DialogResult dr = MessageBox.Show(
                "Are you sure you want to restore default Windows AutoPlay and USB access settings?",
                "Restore Windows Defaults",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                this.Cursor = Cursors.WaitCursor;
                USBShieldHelper.RestoreDefaults();
                this.Cursor = Cursors.Default;
                ScanAll();
            }
        }

        private void ApplyImmunizeDrive()
        {
            if (currentDrives.Count == 0 || cmbDrives.SelectedIndex < 0 || cmbDrives.SelectedIndex >= currentDrives.Count)
            {
                MessageBox.Show("Please select a valid connected USB drive to immunize.", "No Drive Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var d = currentDrives[cmbDrives.SelectedIndex];
            bool ok = USBShieldHelper.ImmunizeDrive(d.RootPath);
            ScanAll();

            if (ok)
            {
                MessageBox.Show(
                    string.Format("USB Drive {0} has been immunized successfully!\n\nAn undeletable, locked 'autorun.inf' folder has been created on the root directory to prevent viruses from writing execution scripts.", d.RootPath),
                    "Drive Immunized",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to immunize drive. Ensure drive is not write-protected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
