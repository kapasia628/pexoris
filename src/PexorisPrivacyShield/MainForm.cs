using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PexorisPrivacyShield
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

        private ListView _lvRules;
        private Panel _pnlActionBar;
        private PexorisButton _btnApplySelected;
        private PexorisButton _btnApplyRecommended;
        private PexorisButton _btnRestoreDefaults;
        private PexorisButton _btnRefresh;

        private Panel _pnlFooter;
        private Label _lblStatus;
        private LinkLabel _lnkSite;

        private List<PrivacyRule> _rules = new List<PrivacyRule>();
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
                Width = 460,
                Height = 20,
                Text = "Pexoris PrivacyShield • Windows Telemetry & Privacy Guard",
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
                Width = 320,
                Height = 24,
                Text = "Windows 11 Privacy & Telemetry Guard",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            _lblCardSub = new Label
            {
                Left = 72,
                Top = 40,
                Width = 500,
                Height = 20,
                Text = "Disable diagnostic reporting, advertising ID, keystroke logs, and cloud telemetry",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lblPillBadge = new Label
            {
                Left = 630,
                Top = 16,
                Width = 172,
                Height = 26,
                Text = "● Scanning Status...",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontSmall,
                ForeColor = Theme.Indigo,
                BackColor = Theme.IndigoLight
            };
            _lblPillBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, _lblPillBadge.Width - 1, _lblPillBadge.Height - 1);
                using (Pen p = new Pen(Theme.IndigoBorder, 1f))
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
                Text = "Filter privacy tweaks by name, category, or registry path..."
            };
            _txtSearch.GotFocus += (s, e) =>
            {
                if (_txtSearch.Text == "Filter privacy tweaks by name, category, or registry path...")
                {
                    _txtSearch.Text = "";
                    _txtSearch.ForeColor = Theme.TextHero;
                }
            };
            _txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(_txtSearch.Text.Trim()))
                {
                    _txtSearch.Text = "Filter privacy tweaks by name, category, or registry path...";
                    _txtSearch.ForeColor = Color.Gray;
                }
            };
            _txtSearch.TextChanged += (s, e) => RenderListView();

            _pnlTopCard.Controls.AddRange(new Control[] {
                _pbCardIcon, _lblCardTitle, _lblCardSub, _lblPillBadge, _txtSearch
            });
            Controls.Add(_pnlTopCard);

            // 3. Explorer Table ListView (Y=190, H=305, W=820, X=20)
            _lvRules = new ListView
            {
                Left = 20,
                Top = 190,
                Width = 820,
                Height = 305,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                CheckBoxes = true,
                Font = Theme.FontRegular,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lvRules.Columns.Add("Privacy Component / Setting", 260);
            _lvRules.Columns.Add("Category", 160);
            _lvRules.Columns.Add("Current State", 140);
            _lvRules.Columns.Add("Preset", 90, HorizontalAlignment.Center);
            _lvRules.Columns.Add("Target Policy / Service", 240);

            Controls.Add(_lvRules);

            // 4. Action Buttons Bar (Y=508, H=50, W=820, X=20)
            _pnlActionBar = new Panel
            {
                Left = 20,
                Top = 508,
                Width = 820,
                Height = 50,
                BackColor = Color.Transparent
            };

            _btnApplySelected = new PexorisButton
            {
                Left = 0,
                Top = 6,
                Width = 200,
                Height = 38,
                Text = "🛡️ Harden Selected Privacy",
                Style = PexorisButtonStyle.PrimaryIndigo
            };
            _btnApplySelected.Click += BtnApplySelected_Click;

            _btnApplyRecommended = new PexorisButton
            {
                Left = 210,
                Top = 6,
                Width = 180,
                Height = 38,
                Text = "⚡ Check Recommended",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            _btnApplyRecommended.Click += BtnSelectRecommended_Click;

            _btnRestoreDefaults = new PexorisButton
            {
                Left = 400,
                Top = 6,
                Width = 170,
                Height = 38,
                Text = "↩️ Restore Defaults",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRestoreDefaults.Click += BtnRestoreDefaults_Click;

            _btnRefresh = new PexorisButton
            {
                Left = 690,
                Top = 6,
                Width = 130,
                Height = 38,
                Text = "🔄 Refresh Scan",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRefresh.Click += (s, e) => PerformAsyncScan();

            _pnlActionBar.Controls.AddRange(new Control[] {
                _btnApplySelected, _btnApplyRecommended, _btnRestoreDefaults, _btnRefresh
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
                using (Pen pen = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
                }
            };

            _lblStatus = new Label
            {
                Left = 20,
                Top = 10,
                Width = 600,
                Height = 20,
                Text = "Ready • Windows 10/11 telemetry & diagnostic shielding engine",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            _lnkSite = new LinkLabel
            {
                Left = 710,
                Top = 10,
                Width = 130,
                Height = 20,
                Text = "pexoris.com/tools",
                TextAlign = ContentAlignment.MiddleRight,
                Font = Theme.FontSmall,
                LinkColor = Theme.Indigo,
                ActiveLinkColor = Theme.IndigoDark,
                VisitedLinkColor = Theme.Indigo
            };
            _lnkSite.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            _pnlFooter.Controls.AddRange(new Control[] { _lblStatus, _lnkSite });
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
            _lblPillBadge.Text = "● Scanning System...";
            _lblStatus.Text = "Scanning Windows telemetry policies, background services, and scheduled tasks...";

            ThreadPool.QueueUserWorkItem(state =>
            {
                List<PrivacyRule> rules = PrivacyEngine.GetRules();
                PrivacyEngine.ScanAll(rules);

                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                    {
                        _rules = rules;
                        _isBusy = false;
                        RenderListView();
                    }));
                }
            });
        }

        private void RenderListView()
        {
            _lvRules.BeginUpdate();
            _lvRules.Items.Clear();

            string filter = _txtSearch.Text;
            if (filter == "Filter privacy tweaks by name, category, or registry path...")
            {
                filter = "";
            }
            filter = filter.Trim().ToLowerInvariant();

            int blockedCount = 0;
            int total = _rules.Count;

            foreach (var rule in _rules)
            {
                if (rule.IsBlocked) blockedCount++;

                if (!string.IsNullOrEmpty(filter))
                {
                    bool match = rule.Name.ToLowerInvariant().Contains(filter) ||
                                 rule.Category.ToLowerInvariant().Contains(filter) ||
                                 rule.TargetPath.ToLowerInvariant().Contains(filter);
                    if (!match) continue;
                }

                ListViewItem item = new ListViewItem(rule.Name);
                item.SubItems.Add(rule.Category);
                item.SubItems.Add(rule.IsBlocked ? "Protected (Blocked)" : "Active (Telemetry)");
                item.SubItems.Add(rule.IsRecommended ? "Recommended" : "Optional");
                item.SubItems.Add(rule.TargetPath);

                item.Checked = !rule.IsBlocked; // Check items that need blocking
                item.ForeColor = rule.IsBlocked ? Color.FromArgb(16, 185, 129) : Color.FromArgb(220, 38, 38);
                item.Tag = rule;

                _lvRules.Items.Add(item);
            }

            _lvRules.EndUpdate();

            // Update badge pill
            if (blockedCount == total && total > 0)
            {
                _lblPillBadge.Text = "🟢 " + blockedCount + "/" + total + " Protected";
                _lblPillBadge.ForeColor = Theme.SuccessGreen;
                _lblPillBadge.BackColor = Color.FromArgb(236, 253, 245);
            }
            else
            {
                _lblPillBadge.Text = "🟡 " + blockedCount + "/" + total + " Hardened";
                _lblPillBadge.ForeColor = Color.FromArgb(217, 119, 6);
                _lblPillBadge.BackColor = Color.FromArgb(254, 243, 199);
            }

            _lblStatus.Text = string.Format("Scan Complete • {0} of {1} telemetry tracking vectors currently blocked", blockedCount, total);
        }

        private void BtnSelectRecommended_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in _lvRules.Items)
            {
                PrivacyRule rule = item.Tag as PrivacyRule;
                if (rule != null)
                {
                    item.Checked = rule.IsRecommended;
                }
            }
            _lblStatus.Text = "Selected all recommended privacy hardening tweaks.";
        }

        private void BtnApplySelected_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            List<PrivacyRule> toApply = new List<PrivacyRule>();
            foreach (ListViewItem item in _lvRules.Items)
            {
                if (item.Checked)
                {
                    PrivacyRule rule = item.Tag as PrivacyRule;
                    if (rule != null) toApply.Add(rule);
                }
            }

            if (toApply.Count == 0)
            {
                MessageBox.Show("Please select at least one privacy tweak to harden.", "Pexoris PrivacyShield", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isBusy = true;
            _lblStatus.Text = "Applying privacy hardening policies and disabling telemetry...";

            ThreadPool.QueueUserWorkItem(state =>
            {
                int applied = 0;
                foreach (var rule in toApply)
                {
                    try
                    {
                        if (rule.ApplyBlock != null)
                        {
                            rule.ApplyBlock();
                            applied++;
                        }
                    }
                    catch { }
                }

                // Rescan
                PrivacyEngine.ScanAll(_rules);

                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                    {
                        _isBusy = false;
                        RenderListView();
                        MessageBox.Show(string.Format("Successfully applied {0} privacy protection tweaks!\nWindows telemetry, advertising profiling, and diagnostic logging have been disabled.", applied),
                            "Privacy Hardened", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                }
            });
        }

        private void BtnRestoreDefaults_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            DialogResult dr = MessageBox.Show(
                "Are you sure you want to restore Microsoft Windows default telemetry and diagnostic settings?\n\nThis will re-enable standard telemetry policies, diagnostic tasks, and system services.",
                "Restore Windows Defaults",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            _isBusy = true;
            _lblStatus.Text = "Restoring Windows default settings...";

            ThreadPool.QueueUserWorkItem(state =>
            {
                int restored = 0;
                foreach (var rule in _rules)
                {
                    try
                    {
                        if (rule.RestoreDefault != null)
                        {
                            rule.RestoreDefault();
                            restored++;
                        }
                    }
                    catch { }
                }

                PrivacyEngine.ScanAll(_rules);

                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                    {
                        _isBusy = false;
                        RenderListView();
                        MessageBox.Show("Windows default privacy and telemetry settings restored successfully.",
                            "Defaults Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                }
            });
        }
    }
}
