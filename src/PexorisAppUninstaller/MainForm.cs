using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PexorisAppUninstaller
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WM_SETICON = 0x0080;
        private static readonly IntPtr ICON_SMALL = new IntPtr(0);
        private static readonly IntPtr ICON_BIG = new IntPtr(1);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        // Controls
        private Panel _pnlTitleBar;
        private PictureBox _pbTitleIcon;
        private Label _lblTitle;
        private Button _btnMin;
        private Button _btnClose;

        private Panel _pnlTopCard;
        private PictureBox _pbCardIcon;
        private Label _lblCardTitle;
        private Label _lblCardSub;
        private Label _lblPillBadge;
        private TextBox _txtSearch;

        private ListView _lvApps;
        private Panel _pnlActionBar;
        private PexorisButton _btnUninstall;
        private PexorisButton _btnQuiet;
        private PexorisButton _btnForceRemove;
        private PexorisButton _btnExport;
        private PexorisButton _btnRefresh;

        private Panel _pnlFooter;
        private Label _lblStatus;
        private CheckBox _chkConfirm;
        private LinkLabel _lnkSite;

        private List<InstalledAppInfo> _allApps = new List<InstalledAppInfo>();
        private bool _isBusy = false;

        public MainForm()
        {
            Width = 860;
            Height = 640;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.BgCanvas;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            InitComponents();
            PerformAsyncScan();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                if (Icon != null)
                {
                    SendMessage(Handle, WM_SETICON, ICON_SMALL, Icon.Handle);
                    SendMessage(Handle, WM_SETICON, ICON_BIG, Icon.Handle);
                }
            }
            catch { }
        }

        private void InitComponents()
        {
            // 1. Title Bar (44px)
            _pnlTitleBar = new Panel { Left = 0, Top = 0, Width = 860, Height = 44, BackColor = Color.White };
            _pnlTitleBar.MouseDown += TitleBar_MouseDown;

            _pbTitleIcon = new PictureBox
            {
                Left = 14,
                Top = 11,
                Width = 22,
                Height = 22,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try { if (Icon != null) _pbTitleIcon.Image = Icon.ToBitmap(); } catch { }
            _pbTitleIcon.MouseDown += TitleBar_MouseDown;

            _lblTitle = new Label
            {
                Left = 44,
                Top = 12,
                Width = 400,
                Height = 20,
                Text = "Pexoris AppUninstaller • Portable Utility",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };
            _lblTitle.MouseDown += TitleBar_MouseDown;

            _btnMin = new Button
            {
                Left = 770,
                Top = 0,
                Width = 45,
                Height = 44,
                Text = "—",
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontRegular,
                ForeColor = Theme.TextSub,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnMin.FlatAppearance.BorderSize = 0;
            _btnMin.MouseEnter += (s, e) => _btnMin.BackColor = Color.FromArgb(241, 245, 249);
            _btnMin.MouseLeave += (s, e) => _btnMin.BackColor = Color.White;
            _btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;

            _btnClose = new Button
            {
                Left = 815,
                Top = 0,
                Width = 45,
                Height = 44,
                Text = "✕",
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontRegular,
                ForeColor = Theme.TextSub,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.MouseEnter += (s, e) => { _btnClose.BackColor = Color.FromArgb(239, 68, 68); _btnClose.ForeColor = Color.White; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.BackColor = Color.White; _btnClose.ForeColor = Theme.TextSub; };
            _btnClose.Click += (s, e) => Application.Exit();

            _pnlTitleBar.Controls.AddRange(new Control[] { _pbTitleIcon, _lblTitle, _btnMin, _btnClose });
            Controls.Add(_pnlTitleBar);

            // 2. Top Control Card (Y=54, H=124, W=820, X=20)
            _pnlTopCard = new Panel
            {
                Left = 20,
                Top = 54,
                Width = 820,
                Height = 124,
                BackColor = Theme.CardBg
            };
            _pnlTopCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, _pnlTopCard.Width - 1, _pnlTopCard.Height - 1);
                using (Pen pen = new Pen(Theme.BorderLight, 1f))
                {
                    using (GraphicsPath path = Theme.GetRoundedPath(rect, 10f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            _pbCardIcon = new PictureBox
            {
                Left = 16,
                Top = 18,
                Width = 44,
                Height = 44,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try { if (Icon != null) _pbCardIcon.Image = Icon.ToBitmap(); } catch { }

            _lblCardTitle = new Label
            {
                Left = 72,
                Top = 16,
                Width = 260,
                Height = 24,
                Text = "Pexoris AppUninstaller",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            _lblCardSub = new Label
            {
                Left = 72,
                Top = 40,
                Width = 460,
                Height = 20,
                Text = "Clean uninstall stubborn software, leftover files, and invalid registry entries",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lblPillBadge = new Label
            {
                Left = 650,
                Top = 16,
                Width = 150,
                Height = 26,
                Text = "● Scanning Apps...",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontSmall,
                ForeColor = Theme.Purple,
                BackColor = Theme.PurpleLight
            };
            _lblPillBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, _lblPillBadge.Width - 1, _lblPillBadge.Height - 1);
                using (Pen p = new Pen(Theme.PurpleBorder, 1f))
                {
                    using (GraphicsPath gp = Theme.GetRoundedPath(rect, 13f))
                    {
                        e.Graphics.DrawPath(p, gp);
                    }
                }
            };

            // Search Box
            _txtSearch = new TextBox
            {
                Left = 16,
                Top = 76,
                Width = 786,
                Height = 32,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.Gray,
                Text = "Search installed programs by name or publisher..."
            };
            _txtSearch.GotFocus += (s, e) =>
            {
                if (_txtSearch.Text == "Search installed programs by name or publisher...")
                {
                    _txtSearch.Text = "";
                    _txtSearch.ForeColor = Theme.TextHero;
                }
            };
            _txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(_txtSearch.Text.Trim()))
                {
                    _txtSearch.Text = "Search installed programs by name or publisher...";
                    _txtSearch.ForeColor = Color.Gray;
                }
            };
            _txtSearch.TextChanged += (s, e) => FilterApps();

            _pnlTopCard.Controls.AddRange(new Control[] {
                _pbCardIcon, _lblCardTitle, _lblCardSub, _lblPillBadge, _txtSearch
            });
            Controls.Add(_pnlTopCard);

            // 3. Explorer Table ListView (Y=190, H=305, W=820, X=20)
            _lvApps = new ListView
            {
                Left = 20,
                Top = 190,
                Width = 820,
                Height = 305,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = Theme.FontRegular,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lvApps.Columns.Add("Program Name", 280);
            _lvApps.Columns.Add("Publisher", 180);
            _lvApps.Columns.Add("Version", 100);
            _lvApps.Columns.Add("Installed On", 95);
            _lvApps.Columns.Add("Size", 85, HorizontalAlignment.Right);
            _lvApps.Columns.Add("Arch", 60, HorizontalAlignment.Center);

            _lvApps.SelectedIndexChanged += LvApps_SelectedIndexChanged;
            _lvApps.DoubleClick += (s, e) => BtnUninstall_Click(null, null);
            Controls.Add(_lvApps);

            // 4. Action Buttons Bar (Y=508, H=50, W=820, X=20)
            _pnlActionBar = new Panel
            {
                Left = 20,
                Top = 508,
                Width = 820,
                Height = 50,
                BackColor = Color.Transparent
            };

            _btnUninstall = new PexorisButton
            {
                Left = 0,
                Top = 4,
                Width = 190,
                Height = 42,
                Text = "⚡ Uninstall Selected",
                Style = PexorisButtonStyle.DestructiveRed
            };
            _btnUninstall.Click += BtnUninstall_Click;

            _btnQuiet = new PexorisButton
            {
                Left = 198,
                Top = 4,
                Width = 190,
                Height = 42,
                Text = "🚀 Silent / Quiet Uninstall",
                Style = PexorisButtonStyle.PrimaryPurple
            };
            _btnQuiet.Click += BtnQuiet_Click;

            _btnForceRemove = new PexorisButton
            {
                Left = 396,
                Top = 4,
                Width = 176,
                Height = 42,
                Text = "🗑️ Force Remove (Purge)",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnForceRemove.Click += BtnForceRemove_Click;

            _btnExport = new PexorisButton
            {
                Left = 580,
                Top = 4,
                Width = 142,
                Height = 42,
                Text = "Export Inventory",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnExport.Click += BtnExport_Click;

            _btnRefresh = new PexorisButton
            {
                Left = 728,
                Top = 4,
                Width = 92,
                Height = 42,
                Text = "Refresh",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRefresh.Click += (s, e) => PerformAsyncScan();

            _pnlActionBar.Controls.AddRange(new Control[] {
                _btnUninstall, _btnQuiet, _btnForceRemove, _btnExport, _btnRefresh
            });
            Controls.Add(_pnlActionBar);

            // 5. Footer (38px, Y=602)
            _pnlFooter = new Panel
            {
                Left = 0,
                Top = 602,
                Width = 860,
                Height = 38,
                BackColor = Color.White
            };
            _pnlFooter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, 860, 0);
                }
            };

            _chkConfirm = new CheckBox
            {
                Left = 20,
                Top = 9,
                Width = 160,
                Height = 20,
                Text = "Prompt before uninstalling",
                Checked = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lblStatus = new Label
            {
                Left = 190,
                Top = 9,
                Width = 520,
                Height = 20,
                Text = "Ready • Double-click any program to launch uninstaller",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextBody,
                BackColor = Color.Transparent
            };

            _lnkSite = new LinkLabel
            {
                Left = 740,
                Top = 9,
                Width = 100,
                Height = 20,
                Text = "pexoris.com",
                Font = Theme.FontBold,
                LinkColor = Theme.Purple,
                ActiveLinkColor = Theme.PurpleDark,
                TextAlign = ContentAlignment.TopRight,
                BackColor = Color.Transparent
            };
            _lnkSite.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            _pnlFooter.Controls.AddRange(new Control[] { _chkConfirm, _lblStatus, _lnkSite });
            Controls.Add(_pnlFooter);
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void PerformAsyncScan()
        {
            if (_isBusy) return;
            _isBusy = true;
            _lblPillBadge.Text = "● Scanning Registry...";
            _lblPillBadge.ForeColor = Theme.PrimaryBlue;
            _lblPillBadge.BackColor = Color.FromArgb(239, 246, 255);

            Thread worker = new Thread(() =>
            {
                List<InstalledAppInfo> apps = UninstallEngine.ScanInstalledApps();
                if (IsHandleCreated)
                {
                    Invoke(new Action(() =>
                    {
                        _allApps = apps;
                        _isBusy = false;
                        _lblPillBadge.Text = string.Format("● {0} Apps Found", _allApps.Count);
                        _lblPillBadge.ForeColor = Theme.Purple;
                        _lblPillBadge.BackColor = Theme.PurpleLight;
                        FilterApps();
                    }));
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void FilterApps()
        {
            string q = _txtSearch.Text.Trim();
            if (q == "Search installed programs by name or publisher...") q = "";

            _lvApps.BeginUpdate();
            _lvApps.Items.Clear();

            for (int i = 0; i < _allApps.Count; i++)
            {
                InstalledAppInfo app = _allApps[i];
                if (!string.IsNullOrEmpty(q))
                {
                    bool match = app.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 app.Publisher.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!match) continue;
                }

                ListViewItem item = new ListViewItem(app.DisplayName);
                item.Tag = app;
                item.SubItems.Add(string.IsNullOrEmpty(app.Publisher) ? "-" : app.Publisher);
                item.SubItems.Add(string.IsNullOrEmpty(app.DisplayVersion) ? "-" : app.DisplayVersion);
                item.SubItems.Add(string.IsNullOrEmpty(app.InstallDate) ? "-" : app.InstallDate);
                item.SubItems.Add(app.FormattedSize);
                item.SubItems.Add(app.Is64Bit ? "64-bit" : "32-bit");

                _lvApps.Items.Add(item);
            }
            _lvApps.EndUpdate();
            UpdateSelectionStatus();
        }

        private void LvApps_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSelectionStatus();
        }

        private void UpdateSelectionStatus()
        {
            if (_lvApps.SelectedItems.Count > 0)
            {
                InstalledAppInfo app = _lvApps.SelectedItems[0].Tag as InstalledAppInfo;
                if (app != null)
                {
                    _lblStatus.Text = string.Format("Selected: {0} {1} • {2}",
                        app.DisplayName,
                        string.IsNullOrEmpty(app.DisplayVersion) ? "" : "(" + app.DisplayVersion + ")",
                        app.FormattedSize);
                    return;
                }
            }

            _lblStatus.Text = string.Format("Showing {0} of {1} installed applications", _lvApps.Items.Count, _allApps.Count);
        }

        private InstalledAppInfo GetSelectedApp()
        {
            if (_lvApps.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select an application from the list.", "Pexoris AppUninstaller", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            return _lvApps.SelectedItems[0].Tag as InstalledAppInfo;
        }

        private void BtnUninstall_Click(object sender, EventArgs e)
        {
            InstalledAppInfo app = GetSelectedApp();
            if (app == null) return;

            if (_chkConfirm.Checked)
            {
                DialogResult res = MessageBox.Show(
                    string.Format("Are you sure you want to uninstall:\n\n{0} (v{1})?\n\nPublisher: {2}",
                        app.DisplayName,
                        string.IsNullOrEmpty(app.DisplayVersion) ? "N/A" : app.DisplayVersion,
                        string.IsNullOrEmpty(app.Publisher) ? "Unknown" : app.Publisher),
                    "Confirm Standard Uninstall",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res != DialogResult.Yes) return;
            }

            try
            {
                UninstallEngine.LaunchStandardUninstall(app);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch uninstaller:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnQuiet_Click(object sender, EventArgs e)
        {
            InstalledAppInfo app = GetSelectedApp();
            if (app == null) return;

            if (_chkConfirm.Checked)
            {
                DialogResult res = MessageBox.Show(
                    string.Format("Attempt silent/quiet uninstallation for:\n\n{0}?\n\nThis will pass automated flags (/qn, /SILENT) to suppress wizard dialogs.", app.DisplayName),
                    "Confirm Silent Uninstall",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res != DialogResult.Yes) return;
            }

            try
            {
                UninstallEngine.LaunchQuietUninstall(app);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch quiet uninstaller:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnForceRemove_Click(object sender, EventArgs e)
        {
            InstalledAppInfo app = GetSelectedApp();
            if (app == null) return;

            DialogResult res = MessageBox.Show(
                string.Format("FORCE REMOVAL will purge this application's registry entries and install folder directly without running the setup wizard.\n\nTarget App: {0}\nRegistry: {1}\\{2}\nFolder: {3}\n\nProceed with force purge?",
                    app.DisplayName,
                    app.RegistryRoot,
                    app.SubKeyName,
                    string.IsNullOrEmpty(app.InstallLocation) ? "N/A" : app.InstallLocation),
                "Confirm Force Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res != DialogResult.Yes) return;

            try
            {
                UninstallEngine.ForcePurgeApp(app);
                MessageBox.Show("Force purge completed! Removed registry entries and installation directory.", "Pexoris AppUninstaller", MessageBoxButtons.OK, MessageBoxIcon.Information);
                PerformAsyncScan();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Force purge error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Export Installed Applications Inventory";
                sfd.Filter = "CSV Spreadsheet (*.csv)|*.csv|Text File (*.txt)|*.txt";
                sfd.FileName = string.Format("Pexoris_Software_Inventory_{0}.csv", DateTime.Now.ToString("yyyyMMdd"));

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        bool asCsv = sfd.FilterIndex == 1;
                        UninstallEngine.ExportReport(_allApps, sfd.FileName, asCsv);
                        MessageBox.Show("Software inventory exported successfully to:\n" + sfd.FileName, "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to export: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
