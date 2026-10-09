using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisWifiKeyRevealer
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

        // UI Controls
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

        private ListView _lvProfiles;
        private Panel _pnlActionBar;
        private PexorisButton _btnCopy;
        private PexorisButton _btnToggleMask;
        private PexorisButton _btnExport;
        private PexorisButton _btnForget;
        private PexorisButton _btnRefresh;

        private Panel _pnlFooter;
        private Label _lblStatus;
        private CheckBox _chkMaskDefault;
        private LinkLabel _lnkSite;

        private List<WifiProfileInfo> _allProfiles = new List<WifiProfileInfo>();
        private bool _isMasked = false;

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
                Text = "Pexoris WifiKeyRevealer • Portable Utility",
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
            _btnClose.MouseEnter += (s, e) => { _btnClose.BackColor = Theme.DestructiveRed; _btnClose.ForeColor = Color.White; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.BackColor = Color.White; _btnClose.ForeColor = Theme.TextSub; };
            _btnClose.Click += (s, e) => Close();

            _pnlTitleBar.Controls.AddRange(new Control[] { _pbTitleIcon, _lblTitle, _btnMin, _btnClose });

            // 2. Top Control Card (Y=54, H=120)
            _pnlTopCard = new Panel
            {
                Left = 14,
                Top = 54,
                Width = 832,
                Height = 120,
                BackColor = Theme.CardBg
            };
            _pnlTopCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF r = new RectangleF(0.5f, 0.5f, _pnlTopCard.Width - 1f, _pnlTopCard.Height - 1f);
                using (GraphicsPath gp = Theme.GetRoundedPath(r, 8f))
                using (Pen pen = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawPath(pen, gp);
                }
            };

            _pbCardIcon = new PictureBox
            {
                Left = 18,
                Top = 18,
                Width = 46,
                Height = 46,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try { if (Icon != null) _pbCardIcon.Image = Icon.ToBitmap(); } catch { }

            _lblCardTitle = new Label
            {
                Left = 76,
                Top = 18,
                Width = 520,
                Height = 24,
                Text = "Saved Wi-Fi Passwords & Profile Security Manager",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            _lblCardSub = new Label
            {
                Left = 76,
                Top = 46,
                Width = 540,
                Height = 36,
                Text = "Instantly reveals saved wireless network security keys (WPA2/WPA3), SSID profiles, and encryption modes without requiring Windows PIN prompts.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lblPillBadge = new Label
            {
                Left = 650,
                Top = 18,
                Width = 160,
                Height = 28,
                Text = "SCANNING...",
                Font = Theme.FontBold,
                ForeColor = Theme.TealDark,
                BackColor = Theme.TealLight,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _lblPillBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF r = new RectangleF(0.5f, 0.5f, _lblPillBadge.Width - 1f, _lblPillBadge.Height - 1f);
                using (GraphicsPath gp = Theme.GetRoundedPath(r, 6f))
                using (Pen pen = new Pen(Theme.TealBorder, 1f))
                {
                    e.Graphics.DrawPath(pen, gp);
                }
            };

            // Search filter box in Top Card
            Label lblSearchIcon = new Label
            {
                Left = 76,
                Top = 86,
                Width = 60,
                Height = 22,
                Text = "Filter:",
                Font = Theme.FontBold,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _txtSearch = new TextBox
            {
                Left = 135,
                Top = 84,
                Width = 240,
                Height = 24,
                Font = Theme.FontRegular,
                ForeColor = Theme.TextHero
            };
            _txtSearch.TextChanged += (s, e) => ApplySearchFilter(_txtSearch.Text);

            _pnlTopCard.Controls.AddRange(new Control[] { _pbCardIcon, _lblCardTitle, _lblCardSub, _lblPillBadge, lblSearchIcon, _txtSearch });

            // 3. Explorer Details ListView (Y=184, H=312)
            _lvProfiles = new ListView
            {
                Left = 14,
                Top = 184,
                Width = 832,
                Height = 312,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.FontRegular,
                BackColor = Color.White
            };

            _lvProfiles.Columns.Add("Network Name (SSID)", 220);
            _lvProfiles.Columns.Add("Security Key (Password)", 210);
            _lvProfiles.Columns.Add("Security / Authentication", 160);
            _lvProfiles.Columns.Add("Cipher", 90);
            _lvProfiles.Columns.Add("Connection Mode", 140);
            _lvProfiles.DoubleClick += LvProfiles_DoubleClick;

            // 4. Action Bar (Y=506, H=50)
            _pnlActionBar = new Panel
            {
                Left = 14,
                Top = 506,
                Width = 832,
                Height = 50,
                BackColor = Color.Transparent
            };

            // Slot 1: 📋 Copy Password (Teal)
            _btnCopy = new PexorisButton
            {
                Left = 0,
                Top = 5,
                Width = 200,
                Height = 40,
                Text = "📋 Copy Password",
                Style = PexorisButtonStyle.PrimaryTeal
            };
            _btnCopy.Click += BtnCopy_Click;

            // Slot 2: 👁️ Show / Hide Keys
            _btnToggleMask = new PexorisButton
            {
                Left = 210,
                Top = 5,
                Width = 170,
                Height = 40,
                Text = "👁️ Show / Hide Keys",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnToggleMask.Click += BtnToggleMask_Click;

            // Slot 3: 💾 Export Backup (CSV/TXT)
            _btnExport = new PexorisButton
            {
                Left = 390,
                Top = 5,
                Width = 150,
                Height = 40,
                Text = "💾 Export Report",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnExport.Click += BtnExport_Click;

            // Slot 4: 🗑️ Forget Network
            _btnForget = new PexorisButton
            {
                Left = 550,
                Top = 5,
                Width = 155,
                Height = 40,
                Text = "🗑️ Forget Network",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnForget.Click += BtnForget_Click;

            // Slot 5: 🔄 Scan / Refresh
            _btnRefresh = new PexorisButton
            {
                Left = 715,
                Top = 5,
                Width = 117,
                Height = 40,
                Text = "🔄 Refresh",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRefresh.Click += (s, e) => PerformAsyncScan();

            _pnlActionBar.Controls.AddRange(new Control[] { _btnCopy, _btnToggleMask, _btnExport, _btnForget, _btnRefresh });

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
                    e.Graphics.DrawLine(p, 0, 0, _pnlFooter.Width, 0);
                }
            };

            _lblStatus = new Label
            {
                Left = 14,
                Top = 10,
                Width = 430,
                Height = 18,
                Text = "Ready • Double-click any wireless network to copy password to clipboard",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            _chkMaskDefault = new CheckBox
            {
                Left = 450,
                Top = 9,
                Width = 240,
                Height = 20,
                Text = "Mask passwords with bullets (••••)",
                Checked = false,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextBody,
                BackColor = Color.Transparent
            };
            _chkMaskDefault.CheckedChanged += (s, e) =>
            {
                _isMasked = _chkMaskDefault.Checked;
                ApplySearchFilter(_txtSearch.Text);
            };

            _lnkSite = new LinkLabel
            {
                Left = 740,
                Top = 10,
                Width = 105,
                Height = 18,
                Text = "pexoris.com",
                Font = Theme.FontBold,
                LinkColor = Theme.Teal,
                ActiveLinkColor = Theme.TealDark,
                TextAlign = ContentAlignment.TopRight
            };
            _lnkSite.LinkClicked += (s, e) =>
            {
                try { System.Diagnostics.Process.Start("https://pexoris.com/tools/wifi-key-revealer/"); } catch { }
            };

            _pnlFooter.Controls.AddRange(new Control[] { _lblStatus, _chkMaskDefault, _lnkSite });

            // Add all controls to form
            Controls.AddRange(new Control[] { _pnlTitleBar, _pnlTopCard, _lvProfiles, _pnlActionBar, _pnlFooter });
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
            _lblStatus.Text = "Scanning all saved Wi-Fi profiles via Windows WLAN interface...";
            _lblPillBadge.Text = "SCANNING...";

            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                List<WifiProfileInfo> profiles = WifiEngine.GetAllProfiles();
                Invoke(new Action(() =>
                {
                    _allProfiles = profiles;
                    ApplySearchFilter(_txtSearch.Text);
                    _lblPillBadge.Text = string.Format("{0} NETWORKS", profiles.Count);
                    _lblStatus.Text = string.Format("Found {0} saved Wi-Fi networks • Ready to view and copy", profiles.Count);
                }));
            });
        }

        private void ApplySearchFilter(string query)
        {
            _lvProfiles.BeginUpdate();
            _lvProfiles.Items.Clear();

            string q = (query ?? "").Trim().ToLower();

            foreach (var p in _allProfiles)
            {
                if (!string.IsNullOrEmpty(q))
                {
                    if (!p.Ssid.ToLower().Contains(q) && !p.Authentication.ToLower().Contains(q))
                        continue;
                }

                ListViewItem lvi = new ListViewItem(p.Ssid);

                // Password display
                string displayedPassword;
                if (!p.HasPassword)
                {
                    displayedPassword = p.Password;
                }
                else if (_isMasked)
                {
                    displayedPassword = new string('•', Math.Min(12, Math.Max(8, p.Password.Length)));
                }
                else
                {
                    displayedPassword = p.Password;
                }

                lvi.SubItems.Add(displayedPassword);
                lvi.SubItems.Add(p.Authentication);
                lvi.SubItems.Add(p.Cipher);
                lvi.SubItems.Add(p.ConnectionMode);
                lvi.Tag = p;

                if (p.HasPassword)
                {
                    lvi.ForeColor = Theme.TextHero;
                }
                else
                {
                    lvi.ForeColor = Theme.TextMuted;
                }

                _lvProfiles.Items.Add(lvi);
            }

            _lvProfiles.EndUpdate();
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (_lvProfiles.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a Wi-Fi network from the list to copy its password.",
                    "No Network Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            WifiProfileInfo profile = _lvProfiles.SelectedItems[0].Tag as WifiProfileInfo;
            if (profile != null && profile.HasPassword)
            {
                Clipboard.SetText(profile.Password);
                _lblStatus.Text = string.Format("Password for '{0}' copied to clipboard!", profile.Ssid);
            }
            else
            {
                MessageBox.Show("This network does not have a saved security key (Open or Protected).",
                    "No Password", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LvProfiles_DoubleClick(object sender, EventArgs e)
        {
            BtnCopy_Click(sender, e);
        }

        private void BtnToggleMask_Click(object sender, EventArgs e)
        {
            _isMasked = !_isMasked;
            _chkMaskDefault.Checked = _isMasked;
            ApplySearchFilter(_txtSearch.Text);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_allProfiles.Count == 0)
            {
                MessageBox.Show("No saved Wi-Fi profiles found to export.", "Empty List", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Export Saved Wi-Fi Profiles";
                sfd.Filter = "CSV File (*.csv)|*.csv|Text Report (*.txt)|*.txt";
                sfd.FileName = string.Format("Pexoris_Wifi_Passwords_Backup_{0}.csv", DateTime.Now.ToString("yyyyMMdd"));

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string content = sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                            ? WifiEngine.ExportToCsv(_allProfiles)
                            : WifiEngine.ExportToTextReport(_allProfiles);

                        File.WriteAllText(sfd.FileName, content, System.Text.Encoding.UTF8);
                        MessageBox.Show(string.Format("Successfully exported {0} wireless profiles to:\n{1}", _allProfiles.Count, sfd.FileName),
                            "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to export file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnForget_Click(object sender, EventArgs e)
        {
            if (_lvProfiles.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a Wi-Fi network to remove/forget.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            WifiProfileInfo profile = _lvProfiles.SelectedItems[0].Tag as WifiProfileInfo;
            if (profile == null) return;

            DialogResult dr = MessageBox.Show(
                string.Format("Are you sure you want to forget and remove the network profile '{0}' from Windows?", profile.Ssid),
                "Confirm Forget Network",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dr == DialogResult.Yes)
            {
                string err;
                if (WifiEngine.DeleteProfile(profile.Ssid, out err))
                {
                    _lblStatus.Text = string.Format("Network '{0}' removed.", profile.Ssid);
                    PerformAsyncScan();
                }
                else
                {
                    MessageBox.Show("Could not delete profile: " + err, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
