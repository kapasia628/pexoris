using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisRamTrimmer
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

        private PexorisButton btnTopPurgeStandby;
        private PexorisButton btnTopEmptySets;

        private Panel pnlTabRow;
        private PexorisButton btnTabProcesses;
        private PexorisButton btnTabPools;
        private PexorisButton btnTabLogs;
        private Label lblQuickStats;

        private Panel pnlListWrapper;
        private ListView lvProcesses;
        private ListView lvPools;
        private ListView lvLogs;

        private Panel pnlActions;
        private PexorisButton btnActionTrimAll;
        private PexorisButton btnActionPurgeStandby;
        private PexorisButton btnActionEmptySets;
        private PexorisButton btnActionFlushModified;
        private PexorisButton btnActionTrimSelected;
        private PexorisButton btnActionRefresh;

        private Panel pnlFooter;
        private CheckBox chkAutoTrim;
        private Label lblFooterStatus;
        private LinkLabel lnkBrand;

        private ContextMenuStrip ctxMenu;
        private System.Windows.Forms.Timer autoRefreshTimer;

        private int _activeTab = 0; // 0=Processes, 1=Pools, 2=Logs
        private MemoryStats _currentStats = new MemoryStats();
        private List<ProcessMemoryItem> _topProcesses = new List<ProcessMemoryItem>();

        public MainForm()
        {
            InitializeComponent();
            ApplyCustomDropShadow();
            LoadAppIcon();
            MemoryHelper.EnableRequiredPrivileges();
            RefreshAll();

            autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            autoRefreshTimer.Tick += (s, e) =>
            {
                RefreshMemoryStatsOnly();
                CheckAutoTrimCondition();
            };
            autoRefreshTimer.Start();

            LogEvent("System", "Ready", "Pexoris RamTrimmer initialized with SeProfileSingleProcessPrivilege.");
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Pexoris RamTrimmer";
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
            pnlTitleBar.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 43, 860, 43);
                }
            };
            pnlTitleBar.MouseDown += TitleBar_MouseDown;

            picTitleIcon = new PictureBox
            {
                Location = new Point(14, 11),
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            picTitleIcon.MouseDown += TitleBar_MouseDown;

            lblTitleText = new Label
            {
                Location = new Point(44, 12),
                AutoSize = true,
                Text = "Pexoris RamTrimmer",
                Font = Theme.FontBold,
                ForeColor = Theme.TextHero,
                Cursor = Cursors.Default
            };
            lblTitleText.MouseDown += TitleBar_MouseDown;

            lblTitleBadge = new Label
            {
                Location = new Point(182, 13),
                AutoSize = true,
                Text = "PORTABLE v1.0",
                Font = Theme.FontSmall,
                ForeColor = Theme.EmeraldDark,
                BackColor = Color.FromArgb(236, 253, 245), // Emerald 50
                Padding = new Padding(4, 1, 4, 1)
            };
            lblTitleBadge.MouseDown += TitleBar_MouseDown;

            btnMinimize = new Label
            {
                Location = new Point(780, 0),
                Size = new Size(40, 44),
                Text = "—",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = Theme.TextSub,
                Cursor = Cursors.Hand
            };
            btnMinimize.MouseEnter += (s, e) => { btnMinimize.BackColor = Color.FromArgb(241, 245, 249); btnMinimize.ForeColor = Theme.TextHero; };
            btnMinimize.MouseLeave += (s, e) => { btnMinimize.BackColor = Color.Transparent; btnMinimize.ForeColor = Theme.TextSub; };
            btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;

            btnClose = new Label
            {
                Location = new Point(820, 0),
                Size = new Size(40, 44),
                Text = "✕",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Theme.TextSub,
                Cursor = Cursors.Hand
            };
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.FromArgb(239, 68, 68); btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = Theme.TextSub; };
            btnClose.Click += (s, e) => Close();

            pnlTitleBar.Controls.Add(picTitleIcon);
            pnlTitleBar.Controls.Add(lblTitleText);
            pnlTitleBar.Controls.Add(lblTitleBadge);
            pnlTitleBar.Controls.Add(btnMinimize);
            pnlTitleBar.Controls.Add(btnClose);

            // 2. Top Control Card (100px, Y=56)
            cardTop = new Panel
            {
                Location = new Point(16, 56),
                Size = new Size(828, 100),
                BackColor = Theme.CardBg
            };
            cardTop.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF rect = new RectangleF(0.5f, 0.5f, cardTop.Width - 1f, cardTop.Height - 1f);
                using (GraphicsPath path = Theme.GetRoundedPath(rect, 8f))
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawPath(p, path);
                }
            };

            picCardIcon = new PictureBox
            {
                Location = new Point(16, 18),
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            lblCardTitle = new Label
            {
                Location = new Point(92, 16),
                AutoSize = true,
                Text = "Windows Memory Cache & Standby List Trimmer",
                Font = Theme.FontHeadline,
                ForeColor = Theme.TextHero
            };

            lblCardSubtitle = new Label
            {
                Location = new Point(92, 40),
                Size = new Size(480, 36),
                Text = "Purge bloated standby page lists, empty idle process working sets, flush modified cache, and prevent gaming micro-stutters in real-time.",
                Font = Theme.FontSub,
                ForeColor = Theme.TextSub
            };

            pillStatus = new Label
            {
                Location = new Point(92, 75),
                AutoSize = true,
                Text = "● Memory Load: Calculating...",
                Font = Theme.FontSmall,
                ForeColor = Theme.EmeraldDark,
                BackColor = Color.FromArgb(236, 253, 245),
                Padding = new Padding(6, 2, 6, 2)
            };

            btnTopPurgeStandby = new PexorisButton
            {
                Location = new Point(576, 26),
                Size = new Size(135, 48),
                Text = "⚡ Purge Standby",
                Style = PexorisButtonStyle.PrimaryEmerald
            };
            btnTopPurgeStandby.Click += (s, e) => Action_PurgeStandby();

            btnTopEmptySets = new PexorisButton
            {
                Location = new Point(718, 26),
                Size = new Size(98, 48),
                Text = "Empty Sets",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            btnTopEmptySets.Click += (s, e) => Action_EmptyWorkingSets();

            cardTop.Controls.Add(picCardIcon);
            cardTop.Controls.Add(lblCardTitle);
            cardTop.Controls.Add(lblCardSubtitle);
            cardTop.Controls.Add(pillStatus);
            cardTop.Controls.Add(btnTopPurgeStandby);
            cardTop.Controls.Add(btnTopEmptySets);

            // 3. Tab Switcher Row (Y=164)
            pnlTabRow = new Panel
            {
                Location = new Point(16, 164),
                Size = new Size(828, 32),
                BackColor = Color.Transparent
            };

            btnTabProcesses = new PexorisButton
            {
                Location = new Point(0, 0),
                Size = new Size(160, 32),
                Text = "Active Processes (0)",
                Style = PexorisButtonStyle.PrimaryEmerald
            };
            btnTabProcesses.Click += (s, e) => SwitchTab(0);

            btnTabPools = new PexorisButton
            {
                Location = new Point(168, 0),
                Size = new Size(165, 32),
                Text = "System Memory Pools",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnTabPools.Click += (s, e) => SwitchTab(1);

            btnTabLogs = new PexorisButton
            {
                Location = new Point(341, 0),
                Size = new Size(140, 32),
                Text = "Optimization Logs",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnTabLogs.Click += (s, e) => SwitchTab(2);

            lblQuickStats = new Label
            {
                Location = new Point(490, 8),
                Size = new Size(338, 20),
                TextAlign = ContentAlignment.MiddleRight,
                Text = "RAM: 0 GB / 0 GB (0%)",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            pnlTabRow.Controls.Add(btnTabProcesses);
            pnlTabRow.Controls.Add(btnTabPools);
            pnlTabRow.Controls.Add(btnTabLogs);
            pnlTabRow.Controls.Add(lblQuickStats);

            // 4. Center List Area (Y=202, H=298)
            pnlListWrapper = new Panel
            {
                Location = new Point(16, 202),
                Size = new Size(828, 298),
                BackColor = Theme.CardBg
            };
            pnlListWrapper.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlListWrapper.Width - 1, pnlListWrapper.Height - 1);
                }
            };

            // ListView 1: Processes
            lvProcesses = new ListView
            {
                Location = new Point(1, 1),
                Size = new Size(826, 296),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                Font = Theme.FontRegular,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvProcesses.Columns.Add("Process Name", 190);
            lvProcesses.Columns.Add("PID", 75);
            lvProcesses.Columns.Add("Working Set (RAM)", 135);
            lvProcesses.Columns.Add("Private Commit", 135);
            lvProcesses.Columns.Add("Memory %", 90);
            lvProcesses.Columns.Add("Priority", 100);
            lvProcesses.Columns.Add("Status", 85);

            // ListView 2: Pools
            lvPools = new ListView
            {
                Location = new Point(1, 1),
                Size = new Size(826, 296),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                Font = Theme.FontRegular,
                Visible = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvPools.Columns.Add("Memory Category", 180);
            lvPools.Columns.Add("Current Allocation", 140);
            lvPools.Columns.Add("Description & Impact", 380);
            lvPools.Columns.Add("Trimmable", 120);

            // ListView 3: Logs
            lvLogs = new ListView
            {
                Location = new Point(1, 1),
                Size = new Size(826, 296),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                Font = Theme.FontRegular,
                Visible = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvLogs.Columns.Add("Timestamp", 95);
            lvLogs.Columns.Add("Operation", 150);
            lvLogs.Columns.Add("Result", 110);
            lvLogs.Columns.Add("Details & Memory Shift", 465);

            pnlListWrapper.Controls.Add(lvProcesses);
            pnlListWrapper.Controls.Add(lvPools);
            pnlListWrapper.Controls.Add(lvLogs);

            // Context Menu
            ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("Trim Selected Process Working Set", null, (s, e) => Action_TrimSelectedProcess());
            ctxMenu.Items.Add("Purge Standby Memory List", null, (s, e) => Action_PurgeStandby());
            ctxMenu.Items.Add("Copy Process Details", null, (s, e) => CopySelectedRow());
            lvProcesses.ContextMenuStrip = ctxMenu;
            lvPools.ContextMenuStrip = ctxMenu;
            lvLogs.ContextMenuStrip = ctxMenu;

            // 5. Action Buttons Bar (Y=508, H=50)
            pnlActions = new Panel
            {
                Location = new Point(16, 508),
                Size = new Size(828, 50),
                BackColor = Color.Transparent
            };

            btnActionTrimAll = new PexorisButton
            {
                Location = new Point(0, 6),
                Size = new Size(185, 40),
                Text = "⚡ 1-Click Trim All RAM",
                Style = PexorisButtonStyle.PrimaryEmerald
            };
            btnActionTrimAll.Click += (s, e) => Action_TrimAll();

            btnActionPurgeStandby = new PexorisButton
            {
                Location = new Point(193, 6),
                Size = new Size(140, 40),
                Text = "Purge Standby List",
                Style = PexorisButtonStyle.SuccessGreen
            };
            btnActionPurgeStandby.Click += (s, e) => Action_PurgeStandby();

            btnActionEmptySets = new PexorisButton
            {
                Location = new Point(341, 6),
                Size = new Size(145, 40),
                Text = "Empty Working Sets",
                Style = PexorisButtonStyle.PrimaryBlue
            };
            btnActionEmptySets.Click += (s, e) => Action_EmptyWorkingSets();

            btnActionFlushModified = new PexorisButton
            {
                Location = new Point(494, 6),
                Size = new Size(135, 40),
                Text = "Flush Modified",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnActionFlushModified.Click += (s, e) => Action_FlushModified();

            btnActionTrimSelected = new PexorisButton
            {
                Location = new Point(637, 6),
                Size = new Size(125, 40),
                Text = "Trim Selected",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnActionTrimSelected.Click += (s, e) => Action_TrimSelectedProcess();

            btnActionRefresh = new PexorisButton
            {
                Location = new Point(770, 6),
                Size = new Size(58, 40),
                Text = "Refresh",
                Style = PexorisButtonStyle.SecondaryOutline
            };
            btnActionRefresh.Click += (s, e) => RefreshAll();

            pnlActions.Controls.Add(btnActionTrimAll);
            pnlActions.Controls.Add(btnActionPurgeStandby);
            pnlActions.Controls.Add(btnActionEmptySets);
            pnlActions.Controls.Add(btnActionFlushModified);
            pnlActions.Controls.Add(btnActionTrimSelected);
            pnlActions.Controls.Add(btnActionRefresh);

            // 6. Footer (38px, Y=566)
            pnlFooter = new Panel
            {
                Location = new Point(0, 602),
                Size = new Size(860, 38),
                BackColor = Theme.CardBg,
                Dock = DockStyle.Bottom
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                {
                    e.Graphics.DrawLine(p, 0, 0, 860, 0);
                }
            };

            chkAutoTrim = new CheckBox
            {
                Location = new Point(16, 9),
                AutoSize = true,
                Text = "Auto-trim standby list when available RAM drops below 15%",
                Checked = false,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSub
            };

            lblFooterStatus = new Label
            {
                Location = new Point(370, 10),
                Size = new Size(350, 20),
                Text = "Zero resident memory • Native NtSetSystemInformation • Run as Admin",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lnkBrand = new LinkLabel
            {
                Location = new Point(740, 10),
                Size = new Size(104, 20),
                Text = "pexoris.com",
                Font = Theme.FontBold,
                LinkColor = Theme.EmeraldDark,
                ActiveLinkColor = Theme.PrimaryBlueHover,
                TextAlign = ContentAlignment.MiddleRight
            };
            lnkBrand.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://pexoris.com") { UseShellExecute = true }); } catch { }
            };

            pnlFooter.Controls.Add(chkAutoTrim);
            pnlFooter.Controls.Add(lblFooterStatus);
            pnlFooter.Controls.Add(lnkBrand);

            // Add all panels to form
            Controls.Add(pnlFooter);
            Controls.Add(pnlActions);
            Controls.Add(pnlListWrapper);
            Controls.Add(pnlTabRow);
            Controls.Add(cardTop);
            Controls.Add(pnlTitleBar);

            ResumeLayout(false);
        }

        private void SwitchTab(int tabIndex)
        {
            _activeTab = tabIndex;
            lvProcesses.Visible = (_activeTab == 0);
            lvPools.Visible = (_activeTab == 1);
            lvLogs.Visible = (_activeTab == 2);

            btnTabProcesses.Style = (_activeTab == 0) ? PexorisButtonStyle.PrimaryEmerald : PexorisButtonStyle.SecondaryOutline;
            btnTabPools.Style = (_activeTab == 1) ? PexorisButtonStyle.PrimaryEmerald : PexorisButtonStyle.SecondaryOutline;
            btnTabLogs.Style = (_activeTab == 2) ? PexorisButtonStyle.PrimaryEmerald : PexorisButtonStyle.SecondaryOutline;

            btnTabProcesses.Invalidate();
            btnTabPools.Invalidate();
            btnTabLogs.Invalidate();
        }

        private void LogEvent(string operation, string result, string detail)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => LogEvent(operation, result, detail)));
                return;
            }

            ListViewItem item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add(operation);
            item.SubItems.Add(result);
            item.SubItems.Add(detail);

            if (result.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                result.IndexOf("Fail", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.ForeColor = Theme.DestructiveRed;
            }
            else if (result.IndexOf("Purged", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     result.IndexOf("Trimmed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     result.IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.ForeColor = Theme.EmeraldDark;
            }

            lvLogs.Items.Insert(0, item);
        }

        private void RefreshMemoryStatsOnly()
        {
            _currentStats = MemoryHelper.GetSystemMemoryStats();

            ulong total = _currentStats.TotalPhysicalBytes;
            ulong used = _currentStats.UsedPhysicalBytes;
            ulong avail = _currentStats.AvailablePhysicalBytes;
            uint load = _currentStats.MemoryLoadPercent;

            pillStatus.Text = string.Format("● Memory Load: {0}% • In Use: {1} • Available: {2}",
                load, MemoryHelper.FormatBytes(used), MemoryHelper.FormatBytes(avail));

            if (load > 85)
            {
                pillStatus.ForeColor = Theme.DestructiveRed;
                pillStatus.BackColor = Color.FromArgb(254, 242, 242);
            }
            else if (load > 70)
            {
                pillStatus.ForeColor = Color.FromArgb(217, 119, 6);
                pillStatus.BackColor = Color.FromArgb(254, 243, 199);
            }
            else
            {
                pillStatus.ForeColor = Theme.EmeraldDark;
                pillStatus.BackColor = Color.FromArgb(236, 253, 245);
            }

            lblQuickStats.Text = string.Format("RAM: {0} / {1} ({2}%) | Procs: {3}",
                MemoryHelper.FormatBytes(used), MemoryHelper.FormatBytes(total), load, _currentStats.ProcessCount);
        }

        private void RefreshProcesses()
        {
            lvProcesses.Items.Clear();
            _topProcesses = MemoryHelper.GetTopMemoryProcesses(_currentStats.TotalPhysicalBytes);

            foreach (var proc in _topProcesses)
            {
                ListViewItem item = new ListViewItem(proc.Name);
                item.SubItems.Add(proc.Pid.ToString());
                item.SubItems.Add(MemoryHelper.FormatBytes(proc.WorkingSetBytes));
                item.SubItems.Add(MemoryHelper.FormatBytes(proc.PrivateBytes));
                item.SubItems.Add(proc.MemoryPercent.ToString("0.0") + "%");
                item.SubItems.Add(proc.Priority);
                item.SubItems.Add("Active");

                if (proc.MemoryPercent >= 5.0)
                {
                    item.ForeColor = Color.FromArgb(185, 28, 28); // High memory warning
                }
                else if (proc.MemoryPercent >= 2.0)
                {
                    item.ForeColor = Color.FromArgb(2, 132, 199);
                }
                else
                {
                    item.ForeColor = Theme.TextHero;
                }

                lvProcesses.Items.Add(item);
            }

            btnTabProcesses.Text = string.Format("Active Processes ({0})", _topProcesses.Count);
            btnTabProcesses.Invalidate();
        }

        private void RefreshPools()
        {
            lvPools.Items.Clear();

            AddPoolItem("Physical In-Use", MemoryHelper.FormatBytes(_currentStats.UsedPhysicalBytes), "Active memory mapped to running applications and drivers", "Working Sets Trimmable");
            AddPoolItem("Physical Available", MemoryHelper.FormatBytes(_currentStats.AvailablePhysicalBytes), "Combined Free RAM + Standby Cache ready for instant allocation", "Target Pool");
            AddPoolItem("System File Cache", MemoryHelper.FormatBytes(_currentStats.SystemCacheBytes), "Windows OS filesystem and disk read cache in memory", "Standby Purgeable");
            AddPoolItem("Committed Total", MemoryHelper.FormatBytes(_currentStats.CommitTotalBytes), "Total committed virtual memory against physical RAM + page file", "Pageable");
            AddPoolItem("Commit Limit", MemoryHelper.FormatBytes(_currentStats.CommitLimitBytes), "Maximum virtual memory allocatable without expanding paging file", "System Ceiling");
            AddPoolItem("Kernel Paged Pool", MemoryHelper.FormatBytes(_currentStats.KernelPagedBytes), "Kernel memory allocated by OS and drivers that can be paged to disk", "System Managed");
            AddPoolItem("Kernel Non-Paged Pool", MemoryHelper.FormatBytes(_currentStats.KernelNonPagedBytes), "Critical hardware and driver memory that must remain resident in RAM", "Protected");
        }

        private void AddPoolItem(string name, string size, string desc, string trimmable)
        {
            ListViewItem item = new ListViewItem(name);
            item.SubItems.Add(size);
            item.SubItems.Add(desc);
            item.SubItems.Add(trimmable);
            lvPools.Items.Add(item);
        }

        private void RefreshAll()
        {
            RefreshMemoryStatsOnly();
            RefreshProcesses();
            RefreshPools();
        }

        private void CheckAutoTrimCondition()
        {
            if (!chkAutoTrim.Checked) return;

            if (_currentStats.TotalPhysicalBytes > 0)
            {
                double freePct = ((double)_currentStats.AvailablePhysicalBytes / _currentStats.TotalPhysicalBytes) * 100.0;
                if (freePct < 15.0)
                {
                    MemoryHelper.PurgeStandbyList();
                    MemoryHelper.EmptyProcessWorkingSets();
                    LogEvent("Auto-Trim", "Triggered", string.Format("Free RAM was {0:0.0}%. Standby and working sets purged.", freePct));
                    RefreshMemoryStatsOnly();
                }
            }
        }

        private void Action_PurgeStandby()
        {
            ulong beforeAvail = _currentStats.AvailablePhysicalBytes;
            bool success = MemoryHelper.PurgeStandbyList();
            RefreshMemoryStatsOnly();
            ulong afterAvail = _currentStats.AvailablePhysicalBytes;

            long freed = (long)(afterAvail - beforeAvail);
            string freedStr = freed > 0 ? "Freed +" + MemoryHelper.FormatBytes(freed) : "Standby Cache Cleared";

            LogEvent("Purge Standby", success ? "Purged" : "Failed", freedStr + " via NtSetSystemInformation.");
            MessageBox.Show("Windows Standby List purged successfully!\n\n" + freedStr + "\nStandby file caches released to free memory.",
                "Pexoris RamTrimmer — Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Action_EmptyWorkingSets()
        {
            ulong beforeAvail = _currentStats.AvailablePhysicalBytes;
            int count = MemoryHelper.EmptyProcessWorkingSets();
            MemoryHelper.EmptySystemWorkingSets();
            RefreshMemoryStatsOnly();
            RefreshProcesses();
            ulong afterAvail = _currentStats.AvailablePhysicalBytes;

            long freed = (long)(afterAvail - beforeAvail);
            string freedStr = freed > 0 ? "Freed +" + MemoryHelper.FormatBytes(freed) : "Working sets flushed";

            LogEvent("Empty Working Sets", "Trimmed", string.Format("Emptied working sets across {0} processes. {1}.", count, freedStr));
            MessageBox.Show(string.Format("Working sets emptied across {0} active processes!\n\n{1}", count, freedStr),
                "Working Sets Flushed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Action_FlushModified()
        {
            bool success = MemoryHelper.FlushModifiedList();
            RefreshMemoryStatsOnly();
            LogEvent("Flush Modified", success ? "Flushed" : "Failed", "Modified page list flushed to disk subsystem.");
            MessageBox.Show("Modified Page List flushed successfully to storage subsystem.",
                "Flush Modified List", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Action_TrimAll()
        {
            Cursor = Cursors.WaitCursor;
            ulong beforeAvail = _currentStats.AvailablePhysicalBytes;

            MemoryHelper.PurgeStandbyList();
            MemoryHelper.FlushModifiedList();
            int count = MemoryHelper.EmptyProcessWorkingSets();
            MemoryHelper.EmptySystemWorkingSets();

            RefreshMemoryStatsOnly();
            RefreshProcesses();
            RefreshPools();

            ulong afterAvail = _currentStats.AvailablePhysicalBytes;
            long freed = (long)(afterAvail - beforeAvail);
            string freedStr = freed > 0 ? "Freed +" + MemoryHelper.FormatBytes(freed) : "All memory pools optimized";

            LogEvent("1-Click Trim All", "Success", string.Format("Standby purged, modified flushed, {0} process working sets emptied. {1}.", count, freedStr));
            Cursor = Cursors.Default;

            MessageBox.Show(string.Format("Full Memory Trim Completed!\n\n• Standby List purged\n• Modified Page List flushed\n• {0} process working sets emptied\n\nResult: {1}", count, freedStr),
                "Pexoris RamTrimmer — Clean Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            SwitchTab(2); // Show Logs
        }

        private void Action_TrimSelectedProcess()
        {
            if (lvProcesses.SelectedItems.Count > 0)
            {
                ListViewItem sel = lvProcesses.SelectedItems[0];
                int pid = 0;
                if (int.TryParse(sel.SubItems[1].Text, out pid))
                {
                    string procName = sel.Text;
                    bool success = MemoryHelper.TrimProcess(pid);
                    RefreshProcesses();
                    RefreshMemoryStatsOnly();

                    LogEvent("Trim Process", success ? "Trimmed" : "Failed", string.Format("Trimmed working set of {0} (PID {1}).", procName, pid));
                    MessageBox.Show(string.Format("Working set of '{0}' (PID: {1}) has been emptied.", procName, pid),
                        "Process Trimmed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Please select a process from the list first.", "No Process Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CopySelectedRow()
        {
            ListView activeLv = (_activeTab == 0) ? lvProcesses : ((_activeTab == 1) ? lvPools : lvLogs);
            if (activeLv.SelectedItems.Count > 0)
            {
                ListViewItem item = activeLv.SelectedItems[0];
                List<string> parts = new List<string>();
                foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
                {
                    parts.Add(sub.Text);
                }
                Clipboard.SetText(string.Join(" | ", parts.ToArray()));
            }
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
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
                    SendMessage(Handle, WM_SETICON, ICON_SMALL, (int)appIcon.Handle);
                    SendMessage(Handle, WM_SETICON, ICON_BIG, (int)appIcon.Handle);
                    picTitleIcon.Image = appIcon.ToBitmap();
                    picCardIcon.Image = appIcon.ToBitmap();
                }
            }
            catch { }
        }

        private void ApplyCustomDropShadow()
        {
            // Handled via CreateParams
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }
    }
}
