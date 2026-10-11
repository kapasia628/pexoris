using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisProcessTamer
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

        private ListView _lvProcesses;
        private Panel _pnlActionBar;
        private PexorisButton _btnTame;
        private PexorisButton _btnBoost;
        private PexorisButton _btnAutoTameToggle;
        private PexorisButton _btnKill;
        private PexorisButton _btnRefresh;

        private Panel _pnlFooter;
        private Label _lblStatus;
        private CheckBox _chkAutoThrottle;
        private LinkLabel _lnkSite;

        private System.Windows.Forms.Timer _sampleTimer;
        private List<ProcessItem> _processes = new List<ProcessItem>();
        private bool _autoTameEnabled = true;

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

            _sampleTimer = new System.Windows.Forms.Timer();
            _sampleTimer.Interval = 2000; // 2s sample rate
            _sampleTimer.Tick += (s, e) => RefreshProcessList();
            _sampleTimer.Start();

            RefreshProcessList();
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
                Text = "Pexoris ProcessTamer • CPU Governor & Priority Tamer",
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
                Width = 340,
                Height = 24,
                Text = "Runaway CPU Governor & Process Tamer",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero,
                BackColor = Color.Transparent
            };

            _lblCardSub = new Label
            {
                Left = 72,
                Top = 40,
                Width = 510,
                Height = 20,
                Text = "Auto-throttle CPU spikes from background tasks and boost foreground game responsiveness",
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
                Text = "🟢 Auto-Tamer: ACTIVE",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.FontSmall,
                ForeColor = Theme.SuccessGreen,
                BackColor = Color.FromArgb(236, 253, 245)
            };
            _lblPillBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, _lblPillBadge.Width - 1, _lblPillBadge.Height - 1);
                using (Pen p = new Pen(Color.FromArgb(167, 243, 208), 1f))
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
                Text = "Search active processes by name or PID..."
            };
            _txtSearch.GotFocus += (s, e) =>
            {
                if (_txtSearch.Text == "Search active processes by name or PID...")
                {
                    _txtSearch.Text = "";
                    _txtSearch.ForeColor = Theme.TextHero;
                }
            };
            _txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(_txtSearch.Text.Trim()))
                {
                    _txtSearch.Text = "Search active processes by name or PID...";
                    _txtSearch.ForeColor = Color.Gray;
                }
            };
            _txtSearch.TextChanged += (s, e) => RenderListView();

            _pnlTopCard.Controls.AddRange(new Control[] {
                _pbCardIcon, _lblCardTitle, _lblCardSub, _lblPillBadge, _txtSearch
            });
            Controls.Add(_pnlTopCard);

            // 3. Process Table ListView (Y=190, H=305, W=820, X=20)
            _lvProcesses = new ListView
            {
                Left = 20,
                Top = 190,
                Width = 820,
                Height = 305,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = true,
                Font = Theme.FontRegular,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lvProcesses.Columns.Add("Process Name", 220);
            _lvProcesses.Columns.Add("PID", 70, HorizontalAlignment.Center);
            _lvProcesses.Columns.Add("CPU %", 85, HorizontalAlignment.Right);
            _lvProcesses.Columns.Add("RAM (MB)", 95, HorizontalAlignment.Right);
            _lvProcesses.Columns.Add("Priority Class", 130);
            _lvProcesses.Columns.Add("Tamer Status", 130);
            _lvProcesses.Columns.Add("Threads", 70, HorizontalAlignment.Center);

            Controls.Add(_lvProcesses);

            // 4. Action Buttons Bar (Y=508, H=50, W=820, X=20)
            _pnlActionBar = new Panel
            {
                Left = 20,
                Top = 508,
                Width = 820,
                Height = 50,
                BackColor = Color.Transparent
            };

            _btnTame = new PexorisButton
            {
                Left = 0,
                Top = 6,
                Width = 175,
                Height = 38,
                Text = "⚡ Tame Process (Idle)",
                Style = PexorisButtonStyle.PrimaryOrange
            };
            _btnTame.Click += BtnTame_Click;

            _btnBoost = new PexorisButton
            {
                Left = 185,
                Top = 6,
                Width = 165,
                Height = 38,
                Text = "🚀 Boost (High Prio)",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            _btnBoost.Click += BtnBoost_Click;

            _btnAutoTameToggle = new PexorisButton
            {
                Left = 360,
                Top = 6,
                Width = 160,
                Height = 38,
                Text = "🛡️ Auto-Tamer: ON",
                Style = PexorisButtonStyle.SuccessGreen
            };
            _btnAutoTameToggle.Click += BtnAutoTameToggle_Click;

            _btnKill = new PexorisButton
            {
                Left = 530,
                Top = 6,
                Width = 150,
                Height = 38,
                Text = "✕ Terminate Task",
                Style = PexorisButtonStyle.DestructiveRed
            };
            _btnKill.Click += BtnKill_Click;

            _btnRefresh = new PexorisButton
            {
                Left = 690,
                Top = 6,
                Width = 130,
                Height = 38,
                Text = "🔄 Refresh List",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            _btnRefresh.Click += (s, e) => RefreshProcessList();

            _pnlActionBar.Controls.AddRange(new Control[] {
                _btnTame, _btnBoost, _btnAutoTameToggle, _btnKill, _btnRefresh
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

            _chkAutoThrottle = new CheckBox
            {
                Left = 20,
                Top = 9,
                Width = 280,
                Height = 20,
                Text = "Auto-throttle background tasks > 75% CPU",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextHero,
                Checked = true
            };
            _chkAutoThrottle.CheckedChanged += (s, e) =>
            {
                _autoTameEnabled = _chkAutoThrottle.Checked;
                UpdateTamerStatusPill();
            };

            _lblStatus = new Label
            {
                Left = 310,
                Top = 10,
                Width = 380,
                Height = 20,
                Text = "Ready • Sampling active processes every 2 seconds",
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
                LinkColor = Theme.Orange,
                ActiveLinkColor = Theme.OrangeDark,
                VisitedLinkColor = Theme.Orange
            };
            _lnkSite.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://pexoris.com"); } catch { }
            };

            _pnlFooter.Controls.AddRange(new Control[] { _chkAutoThrottle, _lblStatus, _lnkSite });
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

        private void RefreshProcessList()
        {
            _processes = ProcessEngine.ScanProcesses();

            if (_autoTameEnabled)
            {
                int tamed = ProcessEngine.AutoTameHogs(75.0);
                if (tamed > 0)
                {
                    _lblStatus.Text = string.Format("Auto-Tamer throttled {0} high-CPU background hog(s) to BelowNormal priority.", tamed);
                }
            }

            RenderListView();
        }

        private void RenderListView()
        {
            _lvProcesses.BeginUpdate();
            _lvProcesses.Items.Clear();

            string filter = _txtSearch.Text;
            if (filter == "Search active processes by name or PID...") filter = "";
            filter = filter.Trim().ToLowerInvariant();

            int highCpuCount = 0;

            foreach (var p in _processes)
            {
                if (p.CpuPercent >= 50.0) highCpuCount++;

                if (!string.IsNullOrEmpty(filter))
                {
                    bool match = p.Name.ToLowerInvariant().Contains(filter) ||
                                 p.Id.ToString().Contains(filter);
                    if (!match) continue;
                }

                ListViewItem item = new ListViewItem(p.Name);
                item.SubItems.Add(p.Id.ToString());
                item.SubItems.Add(p.CpuPercent.ToString("0.0") + "%");
                item.SubItems.Add(p.MemoryMb.ToString("#,##0") + " MB");
                item.SubItems.Add(p.PriorityDisplay);

                string tamerStatus = "Normal";
                if (p.IsWhitelisted) tamerStatus = "System Protected";
                else if (p.IsTamed) tamerStatus = "Tamed (Throttled)";
                else if (p.Priority == ProcessPriorityClass.High || p.Priority == ProcessPriorityClass.RealTime) tamerStatus = "Boosted";
                item.SubItems.Add(tamerStatus);

                item.SubItems.Add(p.ThreadCount.ToString());

                // Styling
                if (p.CpuPercent >= 50.0)
                {
                    item.ForeColor = Color.FromArgb(220, 38, 38); // Red
                    item.Font = Theme.FontBold;
                }
                else if (p.IsTamed)
                {
                    item.ForeColor = Color.FromArgb(234, 88, 12); // Orange
                }
                else if (p.Priority == ProcessPriorityClass.High)
                {
                    item.ForeColor = Color.FromArgb(37, 99, 235); // Blue
                }

                item.Tag = p;
                _lvProcesses.Items.Add(item);
            }

            _lvProcesses.EndUpdate();
            UpdateTamerStatusPill();
        }

        private void UpdateTamerStatusPill()
        {
            if (_autoTameEnabled)
            {
                _lblPillBadge.Text = "🟢 Auto-Tamer: ACTIVE";
                _lblPillBadge.ForeColor = Theme.SuccessGreen;
                _lblPillBadge.BackColor = Color.FromArgb(236, 253, 245);
            }
            else
            {
                _lblPillBadge.Text = "⏸️ Auto-Tamer: PAUSED";
                _lblPillBadge.ForeColor = Theme.TextSub;
                _lblPillBadge.BackColor = Color.FromArgb(241, 245, 249);
            }
        }

        private void BtnTame_Click(object sender, EventArgs e)
        {
            if (_lvProcesses.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select one or more processes to tame.", "Pexoris ProcessTamer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int count = 0;
            foreach (ListViewItem item in _lvProcesses.SelectedItems)
            {
                ProcessItem p = item.Tag as ProcessItem;
                if (p != null)
                {
                    if (ProcessEngine.SetPriority(p.Id, ProcessPriorityClass.Idle))
                    {
                        count++;
                    }
                }
            }

            _lblStatus.Text = string.Format("Throttled priority to Idle for {0} process(es).", count);
            RefreshProcessList();
        }

        private void BtnBoost_Click(object sender, EventArgs e)
        {
            if (_lvProcesses.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a foreground process to boost.", "Pexoris ProcessTamer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int count = 0;
            foreach (ListViewItem item in _lvProcesses.SelectedItems)
            {
                ProcessItem p = item.Tag as ProcessItem;
                if (p != null)
                {
                    if (ProcessEngine.SetPriority(p.Id, ProcessPriorityClass.High))
                    {
                        count++;
                    }
                }
            }

            _lblStatus.Text = string.Format("Boosted priority to High for {0} process(es).", count);
            RefreshProcessList();
        }

        private void BtnAutoTameToggle_Click(object sender, EventArgs e)
        {
            _autoTameEnabled = !_autoTameEnabled;
            _chkAutoThrottle.Checked = _autoTameEnabled;
            if (_autoTameEnabled)
            {
                _btnAutoTameToggle.Text = "🛡️ Auto-Tamer: ON";
                _btnAutoTameToggle.Style = PexorisButtonStyle.SuccessGreen;
            }
            else
            {
                _btnAutoTameToggle.Text = "⏸️ Auto-Tamer: OFF";
                _btnAutoTameToggle.Style = PexorisButtonStyle.SecondaryOutline;
            }
            _btnAutoTameToggle.Invalidate();
            UpdateTamerStatusPill();
        }

        private void BtnKill_Click(object sender, EventArgs e)
        {
            if (_lvProcesses.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a process to terminate.", "Pexoris ProcessTamer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult dr = MessageBox.Show(
                string.Format("Are you sure you want to terminate the {0} selected process(es)?\nUnsaved application data will be lost.", _lvProcesses.SelectedItems.Count),
                "Terminate Process", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (dr != DialogResult.Yes) return;

            int killed = 0;
            foreach (ListViewItem item in _lvProcesses.SelectedItems)
            {
                ProcessItem p = item.Tag as ProcessItem;
                if (p != null)
                {
                    if (ProcessEngine.KillProcess(p.Id))
                    {
                        killed++;
                    }
                }
            }

            _lblStatus.Text = string.Format("Terminated {0} rogue process(es).", killed);
            RefreshProcessList();
        }
    }
}
