using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisFontCacheRebuilder
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

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

        private ListView _lvCacheFiles;
        private Panel _pnlActionBar;
        private PexorisButton _btnRebuild;
        private PexorisButton _btnRestartServices;
        private PexorisButton _btnKillFontDrv;
        private PexorisButton _btnScan;
        private PexorisButton _btnRestartExplorer;

        private Panel _pnlFooter;
        private Label _lblStatus;
        private CheckBox _chkRestartExplorer;
        private LinkLabel _lnkSite;

        private List<FontCacheItem> _cachedItems = new List<FontCacheItem>();

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
            RefreshCacheList();
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
                Width = 480,
                Height = 20,
                Text = "Pexoris FontCacheRebuilder • Shell Typography Cache Repair",
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
                Width = 360,
                Height = 24,
                Text = "Windows Font Cache & Glyphs Diagnostic",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            _lblCardSub = new Label
            {
                Left = 72,
                Top = 40,
                Width = 530,
                Height = 20,
                Text = "Purge corrupt FNTCACHE.DAT, fix missing glyphs, square boxes & fontdrvhost CPU spikes",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lblPillBadge = new Label
            {
                Left = 620,
                Top = 16,
                Width = 182,
                Height = 26,
                Text = "● FontCache: RUNNING",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontSmall,
                ForeColor = Color.FromArgb(4, 120, 87),
                BackColor = Color.FromArgb(236, 253, 245)
            };
            _lblPillBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, _lblPillBadge.Width - 1, _lblPillBadge.Height - 1);
                using (Pen pen = new Pen(Color.FromArgb(167, 243, 208), 1f))
                {
                    using (GraphicsPath path = Theme.GetRoundedPath(rect, 13f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            // Search filter row inside card
            Label lblSearchIcon = new Label
            {
                Left = 18,
                Top = 80,
                Width = 24,
                Height = 26,
                Text = "🔍",
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.Transparent
            };

            _txtSearch = new TextBox
            {
                Left = 44,
                Top = 79,
                Width = 756,
                Height = 26,
                Font = Theme.FontRegular,
                ForeColor = Theme.TextHero,
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.TextChanged += (s, e) => ApplySearchFilter();

            _pnlTopCard.Controls.AddRange(new Control[]
            {
                _pbCardIcon, _lblCardTitle, _lblCardSub, _lblPillBadge,
                lblSearchIcon, _txtSearch
            });
            Controls.Add(_pnlTopCard);

            // 3. Explorer Details ListView (Y=190, H=306, W=820, X=20)
            _lvCacheFiles = new ListView
            {
                Left = 20,
                Top = 190,
                Width = 820,
                Height = 306,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Theme.FontRegular,
                BackColor = Color.White,
                ForeColor = Theme.TextBody,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            try { SetWindowTheme(_lvCacheFiles.Handle, "Explorer", null); } catch { }

            _lvCacheFiles.Columns.Add("Cache File / Target", 180);
            _lvCacheFiles.Columns.Add("Scope", 130);
            _lvCacheFiles.Columns.Add("Size (KB)", 90);
            _lvCacheFiles.Columns.Add("Lock State", 130);
            _lvCacheFiles.Columns.Add("Full Storage Path", 280);

            Controls.Add(_lvCacheFiles);

            // 4. Action Buttons Bar (Y=508, H=46, W=820, X=20)
            _pnlActionBar = new Panel { Left = 20, Top = 508, Width = 820, Height = 46, BackColor = Color.Transparent };

            _btnRebuild = new PexorisButton
            {
                Left = 0,
                Top = 0,
                Width = 250,
                Height = 44,
                Text = "⚡ Rebuild & Purge Font Cache",
                Style = PexorisButtonStyle.DestructiveRed
            };
            _btnRebuild.Click += BtnRebuild_Click;

            _btnRestartServices = new PexorisButton
            {
                Left = 260,
                Top = 0,
                Width = 180,
                Height = 44,
                Text = "🔄 Restart Font Services",
                Style = PexorisButtonStyle.PrimaryPurple
            };
            _btnRestartServices.Click += BtnRestartServices_Click;

            _btnKillFontDrv = new PexorisButton
            {
                Left = 450,
                Top = 0,
                Width = 170,
                Height = 44,
                Text = "🛑 Kill FontDrvHost",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            _btnKillFontDrv.Click += BtnKillFontDrvHost_Click;

            _btnScan = new PexorisButton
            {
                Left = 630,
                Top = 0,
                Width = 90,
                Height = 44,
                Text = "🔍 Scan",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnScan.Click += (s, e) => RefreshCacheList();

            _btnRestartExplorer = new PexorisButton
            {
                Left = 728,
                Top = 0,
                Width = 92,
                Height = 44,
                Text = "🔄 Explorer",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRestartExplorer.Click += BtnRestartExplorer_Click;

            _pnlActionBar.Controls.AddRange(new Control[]
            {
                _btnRebuild, _btnRestartServices, _btnKillFontDrv,
                _btnScan, _btnRestartExplorer
            });
            Controls.Add(_pnlActionBar);

            // 5. Footer (Y=566, H=60, W=820, X=20)
            _pnlFooter = new Panel { Left = 20, Top = 566, Width = 820, Height = 60, BackColor = Color.Transparent };

            _chkRestartExplorer = new CheckBox
            {
                Left = 0,
                Top = 6,
                Width = 480,
                Height = 22,
                Text = "Restart Windows Explorer after cache purge to refresh desktop glyphs immediately",
                Font = Theme.FontSub,
                ForeColor = Theme.TextBody,
                Checked = true
            };

            _lblStatus = new Label
            {
                Left = 0,
                Top = 32,
                Width = 550,
                Height = 20,
                Text = "Ready • Audited system font cache databases",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            _lnkSite = new LinkLabel
            {
                Left = 660,
                Top = 32,
                Width = 160,
                Height = 20,
                Text = "pexoris.com/tools",
                Font = Theme.FontBold,
                LinkColor = Theme.Purple,
                ActiveLinkColor = Theme.PurpleHover,
                TextAlign = ContentAlignment.TopRight
            };
            _lnkSite.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://pexoris.com/tools/font-cache-rebuilder/") { UseShellExecute = true }); } catch { }
            };

            _pnlFooter.Controls.AddRange(new Control[] { _chkRestartExplorer, _lblStatus, _lnkSite });
            Controls.Add(_pnlFooter);
        }

        private void RefreshCacheList()
        {
            try
            {
                string sStatus = FontCacheEngine.GetServiceStatus("FontCache");
                if (sStatus.Equals("Running", StringComparison.OrdinalIgnoreCase))
                {
                    _lblPillBadge.Text = "● FontCache: RUNNING";
                    _lblPillBadge.ForeColor = Color.FromArgb(4, 120, 87);
                    _lblPillBadge.BackColor = Color.FromArgb(236, 253, 245);
                }
                else
                {
                    _lblPillBadge.Text = "○ FontCache: " + sStatus.ToUpper();
                    _lblPillBadge.ForeColor = Color.FromArgb(185, 28, 28);
                    _lblPillBadge.BackColor = Color.FromArgb(254, 242, 242);
                }

                _cachedItems = FontCacheEngine.ScanCacheFiles();
                ApplySearchFilter();

                int totalCount = _cachedItems.Count;
                int lockedCount = 0;
                long totalBytes = 0;
                foreach (FontCacheItem it in _cachedItems)
                {
                    if (it.IsLocked) lockedCount++;
                    totalBytes += it.SizeBytes;
                }

                _lblStatus.Text = string.Format("Audited {0} cache locations ({1} locked, {2:N1} MB total) • Ready",
                    totalCount, lockedCount, totalBytes / (1024.0 * 1024.0));
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Scan error: " + ex.Message;
            }
        }

        private void ApplySearchFilter()
        {
            _lvCacheFiles.BeginUpdate();
            _lvCacheFiles.Items.Clear();

            string filter = (_txtSearch.Text ?? "").Trim().ToLowerInvariant();

            foreach (FontCacheItem item in _cachedItems)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    bool match = (item.Name != null && item.Name.ToLowerInvariant().Contains(filter)) ||
                                 (item.Scope != null && item.Scope.ToLowerInvariant().Contains(filter)) ||
                                 (item.FullPath != null && item.FullPath.ToLowerInvariant().Contains(filter));
                    if (!match) continue;
                }

                ListViewItem lvi = new ListViewItem(item.Name);
                lvi.SubItems.Add(item.Scope);
                lvi.SubItems.Add(item.Exists ? string.Format("{0:N1}", item.SizeBytes / 1024.0) : "0.0");
                lvi.SubItems.Add(item.Status);
                lvi.SubItems.Add(item.FullPath);

                if (item.IsLocked)
                {
                    lvi.BackColor = Color.FromArgb(254, 242, 242); // Soft Red highlight
                    lvi.ForeColor = Color.FromArgb(185, 28, 28);
                }
                else if (item.Exists)
                {
                    lvi.BackColor = Color.FromArgb(245, 243, 255); // Soft Purple
                }

                _lvCacheFiles.Items.Add(lvi);
            }

            _lvCacheFiles.EndUpdate();
        }

        private void BtnRebuild_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                "Are you sure you want to rebuild and purge all Windows font cache databases?\n\n" +
                "• Windows Font Cache Services will be safely stopped\n" +
                "• All corrupted FNTCACHE.DAT and .dat databases will be purged\n" +
                "• Services will restart automatically\n" +
                "• WM_FONTCHANGE will refresh all open applications\n\n" +
                "Click OK to proceed.",
                "Pexoris FontCacheRebuilder",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (dr != DialogResult.OK) return;

            _lblStatus.Text = "Rebuilding font cache... Please wait.";
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();

            string log;
            bool ok = FontCacheEngine.RebuildAllFontCaches(out log);

            if (_chkRestartExplorer.Checked)
            {
                _lblStatus.Text = "Refreshing Windows Explorer shell...";
                Application.DoEvents();
                FontCacheEngine.RestartWindowsExplorer();
            }

            Cursor = Cursors.Default;
            RefreshCacheList();

            MessageBox.Show(
                ok ? "Font cache rebuilt successfully!\n\n" + log : "Error rebuilding font cache:\n\n" + log,
                "Pexoris FontCacheRebuilder",
                MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnRestartServices_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _lblStatus.Text = "Restarting Windows Font Cache Service...";
            Application.DoEvents();

            using (StringWriter sw = new StringWriter())
            {
                FontCacheEngine.StopServiceSafe("FontCache", sw);
                FontCacheEngine.StartServiceSafe("FontCache", sw);
                FontCacheEngine.BroadcastFontChange();
            }

            Cursor = Cursors.Default;
            RefreshCacheList();
            MessageBox.Show("Windows Font Cache Service has been restarted.", "Pexoris FontCacheRebuilder", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnKillFontDrvHost_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _lblStatus.Text = "Terminating runaway fontdrvhost.exe processes...";
            Application.DoEvents();

            FontCacheEngine.KillFontDrvHost(null);

            Cursor = Cursors.Default;
            RefreshCacheList();
            MessageBox.Show("fontdrvhost.exe processes terminated. Windows will automatically re-instantiate clean instances.", "Pexoris FontCacheRebuilder", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRestartExplorer_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            _lblStatus.Text = "Restarting Windows Explorer...";
            Application.DoEvents();

            FontCacheEngine.RestartWindowsExplorer();

            Cursor = Cursors.Default;
            RefreshCacheList();
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }
    }
}
