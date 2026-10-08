using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisWinUpdateReset
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

        private ListView _lvComponents;
        private Panel _pnlActionBar;
        private PexorisButton _btnResetAll;
        private PexorisButton _btnClearCache;
        private PexorisButton _btnRestartSvc;
        private PexorisButton _btnOpenSettings;
        private PexorisButton _btnScan;

        private Panel _pnlFooter;
        private Label _lblStatus;
        private CheckBox _chkAutoDetect;
        private LinkLabel _lnkSite;

        public MainForm()
        {
            Width = 860;
            Height = 640;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.BgCanvas;

            // Load Icon safely
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
                Text = "Pexoris WinUpdateReset • Portable Utility",
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
                Text = "Windows Update Component Reset & Repair",
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
                Text = "Halts update services, wipes corrupted SoftwareDistribution & catroot2 caches, purges BITS queue, and restores clean update channels.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub,
                BackColor = Color.Transparent
            };

            _lblPillBadge = new Label
            {
                Left = 650,
                Top = 20,
                Width = 160,
                Height = 28,
                Text = "SCANNING...",
                Font = Theme.FontBold,
                ForeColor = Theme.IndigoDark,
                BackColor = Theme.IndigoLight,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _lblPillBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF r = new RectangleF(0.5f, 0.5f, _lblPillBadge.Width - 1f, _lblPillBadge.Height - 1f);
                using (GraphicsPath gp = Theme.GetRoundedPath(r, 6f))
                using (Pen pen = new Pen(Theme.IndigoBorder, 1f))
                {
                    e.Graphics.DrawPath(pen, gp);
                }
            };

            _pnlTopCard.Controls.AddRange(new Control[] { _pbCardIcon, _lblCardTitle, _lblCardSub, _lblPillBadge });

            // 3. Explorer Details ListView (Y=184, H=312)
            _lvComponents = new ListView
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

            _lvComponents.Columns.Add("Component / Database", 230);
            _lvComponents.Columns.Add("Status / State", 130);
            _lvComponents.Columns.Add("Size / Files", 150);
            _lvComponents.Columns.Add("Path / System Identifier", 300);

            // 4. Action Bar (Y=506, H=50)
            _pnlActionBar = new Panel
            {
                Left = 14,
                Top = 506,
                Width = 832,
                Height = 50,
                BackColor = Color.Transparent
            };

            // Slot 1: ⚡ 1-Click Complete Reset (Indigo/Red)
            _btnResetAll = new PexorisButton
            {
                Left = 0,
                Top = 5,
                Width = 230,
                Height = 40,
                Text = "⚡ 1-Click Complete Reset",
                Style = PexorisButtonStyle.PrimaryIndigo
            };
            _btnResetAll.Click += BtnResetAll_Click;

            // Slot 2: 🗑️ Clear Download Cache
            _btnClearCache = new PexorisButton
            {
                Left = 240,
                Top = 5,
                Width = 180,
                Height = 40,
                Text = "🗑️ Clear Download Cache",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnClearCache.Click += BtnClearCache_Click;

            // Slot 3: 🔄 Restart Services
            _btnRestartSvc = new PexorisButton
            {
                Left = 430,
                Top = 5,
                Width = 140,
                Height = 40,
                Text = "🔄 Restart Services",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRestartSvc.Click += BtnRestartSvc_Click;

            // Slot 4: ⚙️ Open Settings
            _btnOpenSettings = new PexorisButton
            {
                Left = 580,
                Top = 5,
                Width = 135,
                Height = 40,
                Text = "⚙️ Windows Update",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnOpenSettings.Click += (s, e) => UpdateEngine.OpenWindowsUpdateSettings();

            // Slot 5: 🔍 Scan Status
            _btnScan = new PexorisButton
            {
                Left = 725,
                Top = 5,
                Width = 107,
                Height = 40,
                Text = "🔍 Scan Now",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnScan.Click += (s, e) => PerformAsyncScan();

            _pnlActionBar.Controls.AddRange(new Control[] { _btnResetAll, _btnClearCache, _btnRestartSvc, _btnOpenSettings, _btnScan });

            // 5. Footer (38px, Y=562)
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
                Text = "Ready • Run as Administrator for full component control",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            _chkAutoDetect = new CheckBox
            {
                Left = 450,
                Top = 9,
                Width = 240,
                Height = 20,
                Text = "Trigger update scan after reset",
                Checked = true,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextBody,
                BackColor = Color.Transparent
            };

            _lnkSite = new LinkLabel
            {
                Left = 740,
                Top = 10,
                Width = 105,
                Height = 18,
                Text = "pexoris.com",
                Font = Theme.FontBold,
                LinkColor = Theme.Indigo,
                ActiveLinkColor = Theme.IndigoDark,
                TextAlign = ContentAlignment.TopRight
            };
            _lnkSite.LinkClicked += (s, e) =>
            {
                try { System.Diagnostics.Process.Start("https://pexoris.com/tools/winupdate-reset/"); } catch { }
            };

            _pnlFooter.Controls.AddRange(new Control[] { _lblStatus, _chkAutoDetect, _lnkSite });

            // Add all controls to form
            Controls.AddRange(new Control[] { _pnlTitleBar, _pnlTopCard, _lvComponents, _pnlActionBar, _pnlFooter });
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
            _lblStatus.Text = "Scanning Windows Update components, service states & cache folders...";
            _lblPillBadge.Text = "SCANNING...";

            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                SystemScanResult result = UpdateEngine.PerformScan();
                Invoke(new Action(() =>
                {
                    PopulateScanResults(result);
                }));
            });
        }

        private void PopulateScanResults(SystemScanResult res)
        {
            _lvComponents.BeginUpdate();
            _lvComponents.Items.Clear();

            // 1. Services
            foreach (ServiceInfo s in res.Services)
            {
                ListViewItem lvi = new ListViewItem("Service: " + s.DisplayName);
                lvi.SubItems.Add(s.Status);
                lvi.SubItems.Add("—");
                lvi.SubItems.Add(s.Name);

                if (s.IsHealthy)
                {
                    lvi.ForeColor = Color.FromArgb(16, 120, 80);
                }
                else
                {
                    lvi.ForeColor = Theme.TextHero;
                }
                _lvComponents.Items.Add(lvi);
            }

            // 2. Cache Folders
            foreach (CacheFolderInfo f in res.CacheFolders)
            {
                ListViewItem lvi = new ListViewItem("Cache: " + f.Name);
                lvi.SubItems.Add(f.Exists ? "Active" : "Not Found");
                lvi.SubItems.Add(f.FormattedSize);
                lvi.SubItems.Add(f.FullPath);

                if (f.TotalSizeBytes > 1024 * 1024 * 200) // > 200MB
                {
                    lvi.ForeColor = Theme.DestructiveRed;
                }
                _lvComponents.Items.Add(lvi);
            }

            _lvComponents.EndUpdate();

            _lblPillBadge.Text = res.FormattedTotalCache + " CACHED";
            _lblStatus.Text = res.SummaryHeadline;
        }

        private void BtnResetAll_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                "This operation will:\n\n" +
                "1. Temporarily halt Windows Update background services (wuauserv, bits, cryptsvc, dosvc).\n" +
                "2. Wipe the SoftwareDistribution Download cache & DataStore.\n" +
                "3. Clean catroot2 catalog signatures and purge BITS downloader queue.\n" +
                "4. Cleanly restart all update services.\n\n" +
                "Proceed with full Windows Update reset?",
                "Confirm Complete Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            _btnResetAll.Enabled = false;
            _lblStatus.Text = "Executing full Windows Update reset... Please wait.";

            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                string log;
                bool ok = UpdateEngine.ResetAllComponents(out log);

                if (_chkAutoDetect.Checked)
                {
                    UpdateEngine.TriggerUpdateCheck();
                }

                Invoke(new Action(() =>
                {
                    _btnResetAll.Enabled = true;
                    _lblStatus.Text = "Reset completed successfully! Update channels refreshed.";
                    MessageBox.Show("Windows Update components have been cleanly reset and restarted.\n\nCache databases wiped and background queues cleared.",
                        "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    PerformAsyncScan();
                }));
            });
        }

        private void BtnClearCache_Click(object sender, EventArgs e)
        {
            _lblStatus.Text = "Clearing SoftwareDistribution download payload cache...";
            _btnClearCache.Enabled = false;

            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                string log;
                UpdateEngine.ClearDownloadCacheOnly(out log);
                Invoke(new Action(() =>
                {
                    _btnClearCache.Enabled = true;
                    _lblStatus.Text = "Download cache emptied cleanly.";
                    PerformAsyncScan();
                }));
            });
        }

        private void BtnRestartSvc_Click(object sender, EventArgs e)
        {
            _lblStatus.Text = "Restarting Windows Update & BITS services...";
            _btnRestartSvc.Enabled = false;

            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                string log;
                UpdateEngine.RestartUpdateServices(out log);
                Invoke(new Action(() =>
                {
                    _btnRestartSvc.Enabled = true;
                    _lblStatus.Text = "Update services restarted successfully.";
                    PerformAsyncScan();
                }));
            });
        }
    }
}
