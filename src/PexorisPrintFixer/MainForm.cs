using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pexoris.PrintFixer
{
    public class MainForm : Form
    {
        // Native Window Dragging
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        // UI Controls
        private Panel pnlTitleBar;
        private Label lblTitleText;
        private Label btnMinimize;
        private Label btnClose;

        private Panel cardTop;
        private Label lblCardIcon;
        private Label lblCardTitle;
        private Label lblCardSubtitle;
        private Label pillStatus;

        private ComboBox cboPrinters;
        private Button btnFilterAll;
        private Button btnFilterJobs;
        private Button btnFilterOffline;

        private Label lblSection;
        private Panel pnlListWrapper;
        private ListView lvJobs;

        private Panel pnlActions;
        private Button btnPurgeAll;
        private Button btnPrintTest;
        private Button btnRestartSpooler;
        private Button btnCancelJob;
        private Button btnRefresh;

        private Panel pnlFooter;
        private CheckBox chkAutoDeleteFiles;
        private Label lblFooterStatus;
        private LinkLabel lnkPexoris;

        private Timer timerStatus;

        public MainForm()
        {
            InitializeComponent();
            ApplyCustomStyles();
            LoadPrintersAndJobs();
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
            this.Text = "Pexoris PrintFixer";
            this.DoubleBuffered = true;

            // Load Window Icon
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\app.ico");
                if (File.Exists(icoPath))
                {
                    this.Icon = new Icon(icoPath);
                }
            }
            catch { }

            // ==========================================
            // 1. 44px TITLE BAR (Standard)
            // ==========================================
            pnlTitleBar = new Panel();
            pnlTitleBar.Size = new Size(860, 44);
            pnlTitleBar.Location = new Point(0, 0);
            pnlTitleBar.BackColor = PexorisTheme.AppleWhite;
            pnlTitleBar.Paint += PnlTitleBar_Paint;
            pnlTitleBar.MouseDown += TitleBar_MouseDown;

            PictureBox picLogo = new PictureBox();
            picLogo.Size = new Size(22, 22);
            picLogo.Location = new Point(14, 11);
            picLogo.Image = PexorisTheme.GetPrintFixerLogoBitmap(22);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.Controls.Add(picLogo);

            lblTitleText = new Label();
            lblTitleText.Text = "Pexoris PrintFixer  •  Portable Utility";
            lblTitleText.Location = new Point(44, 12);
            lblTitleText.AutoSize = true;
            lblTitleText.Font = PexorisTheme.FontHeader(10f);
            lblTitleText.ForeColor = PexorisTheme.TextPrimary;
            lblTitleText.MouseDown += TitleBar_MouseDown;
            pnlTitleBar.Controls.Add(lblTitleText);

            btnMinimize = new Label();
            btnMinimize.Text = "—";
            btnMinimize.Size = new Size(36, 44);
            btnMinimize.Location = new Point(784, 0);
            btnMinimize.TextAlign = ContentAlignment.MiddleCenter;
            btnMinimize.Font = PexorisTheme.FontBold(11f);
            btnMinimize.ForeColor = PexorisTheme.TextSecondary;
            btnMinimize.Cursor = Cursors.Hand;
            btnMinimize.MouseEnter += delegate { btnMinimize.BackColor = Color.FromArgb(243, 244, 246); };
            btnMinimize.MouseLeave += delegate { btnMinimize.BackColor = Color.Transparent; };
            btnMinimize.Click += delegate { this.WindowState = FormWindowState.Minimized; };
            pnlTitleBar.Controls.Add(btnMinimize);

            btnClose = new Label();
            btnClose.Text = "✕";
            btnClose.Size = new Size(40, 44);
            btnClose.Location = new Point(820, 0);
            btnClose.TextAlign = ContentAlignment.MiddleCenter;
            btnClose.Font = PexorisTheme.FontBold(10f);
            btnClose.ForeColor = PexorisTheme.TextSecondary;
            btnClose.Cursor = Cursors.Hand;
            btnClose.MouseEnter += delegate {
                btnClose.BackColor = PexorisTheme.AppleRed;
                btnClose.ForeColor = Color.White;
            };
            btnClose.MouseLeave += delegate {
                btnClose.BackColor = Color.Transparent;
                btnClose.ForeColor = PexorisTheme.TextSecondary;
            };
            btnClose.Click += delegate { this.Close(); };
            pnlTitleBar.Controls.Add(btnClose);

            this.Controls.Add(pnlTitleBar);

            // ==========================================
            // 2. TOP CONTROL CARD (Y: 58, H: 120, W: 824)
            // ==========================================
            cardTop = new Panel();
            cardTop.Location = new Point(18, 58);
            cardTop.Size = new Size(824, 120);
            cardTop.BackColor = PexorisTheme.AppleWhite;
            cardTop.Paint += CardTop_Paint;

            lblCardIcon = new Label();
            lblCardIcon.Text = "🖨️";
            lblCardIcon.Font = new Font("Segoe UI Emoji", 20f);
            lblCardIcon.Location = new Point(16, 14);
            lblCardIcon.Size = new Size(44, 44);
            lblCardIcon.TextAlign = ContentAlignment.MiddleCenter;
            cardTop.Controls.Add(lblCardIcon);

            lblCardTitle = new Label();
            lblCardTitle.Text = "Purge Stuck Print Queue & Restart Spooler";
            lblCardTitle.UseMnemonic = false;
            lblCardTitle.Font = PexorisTheme.FontHeader(11f);
            lblCardTitle.ForeColor = PexorisTheme.TextPrimary;
            lblCardTitle.Location = new Point(66, 16);
            lblCardTitle.AutoSize = true;
            cardTop.Controls.Add(lblCardTitle);

            lblCardSubtitle = new Label();
            lblCardSubtitle.Text = "1-Click clear spool cache (C:\\Windows\\System32\\spool\\PRINTERS) & reset print services";
            lblCardSubtitle.UseMnemonic = false;
            lblCardSubtitle.Font = PexorisTheme.FontBody(8.5f);
            lblCardSubtitle.ForeColor = PexorisTheme.TextSecondary;
            lblCardSubtitle.Location = new Point(66, 38);
            lblCardSubtitle.AutoSize = true;
            cardTop.Controls.Add(lblCardSubtitle);

            pillStatus = new Label();
            pillStatus.Text = "SPOOLER RUNNING";
            pillStatus.Location = new Point(640, 18);
            pillStatus.Size = new Size(166, 26);
            pillStatus.TextAlign = ContentAlignment.MiddleCenter;
            pillStatus.Font = PexorisTheme.FontBold(8f);
            pillStatus.ForeColor = PexorisTheme.AppleGreen;
            pillStatus.BackColor = PexorisTheme.AppleGreenLight;
            pillStatus.Paint += PillStatus_Paint;
            cardTop.Controls.Add(pillStatus);

            // Card Bottom Row (Printer selector + presets)
            cboPrinters = new ComboBox();
            cboPrinters.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPrinters.Font = PexorisTheme.FontBody(9.5f);
            cboPrinters.Location = new Point(18, 72);
            cboPrinters.Size = new Size(440, 30);
            cboPrinters.SelectedIndexChanged += CboPrinters_SelectedIndexChanged;
            cardTop.Controls.Add(cboPrinters);

            btnFilterAll = new Button();
            btnFilterAll.Text = "All Printers";
            btnFilterAll.Location = new Point(470, 70);
            btnFilterAll.Size = new Size(100, 32);
            PexorisTheme.StyleAppleButton(btnFilterAll, PexorisTheme.AppleWhite, PexorisTheme.AppleCardHover, PexorisTheme.TextPrimary, true);
            btnFilterAll.Click += delegate { LoadPrintersAndJobs(); };
            cardTop.Controls.Add(btnFilterAll);

            btnFilterJobs = new Button();
            btnFilterJobs.Text = "Stuck Jobs";
            btnFilterJobs.Location = new Point(578, 70);
            btnFilterJobs.Size = new Size(110, 32);
            PexorisTheme.StyleAppleButton(btnFilterJobs, PexorisTheme.AppleWhite, PexorisTheme.AppleCardHover, PexorisTheme.TextPrimary, true);
            btnFilterJobs.Click += delegate { FilterOnlyStuckJobs(); };
            cardTop.Controls.Add(btnFilterJobs);

            btnFilterOffline = new Button();
            btnFilterOffline.Text = "Open Spool Folder";
            btnFilterOffline.Location = new Point(696, 70);
            btnFilterOffline.Size = new Size(112, 32);
            PexorisTheme.StyleAppleButton(btnFilterOffline, PexorisTheme.AppleWhite, PexorisTheme.AppleCardHover, PexorisTheme.TextPrimary, true);
            btnFilterOffline.Click += delegate { OpenSpoolFolder(); };
            cardTop.Controls.Add(btnFilterOffline);

            this.Controls.Add(cardTop);

            // ==========================================
            // 3. SECTION HEADER (Y: 188)
            // ==========================================
            lblSection = new Label();
            lblSection.Text = "Active Print Jobs & Connected Printers :";
            lblSection.UseMnemonic = false;
            lblSection.Font = PexorisTheme.FontBold(9.5f);
            lblSection.ForeColor = PexorisTheme.TextPrimary;
            lblSection.Location = new Point(20, 188);
            lblSection.AutoSize = true;
            this.Controls.Add(lblSection);

            // ==========================================
            // 4. EXPLORER LISTVIEW (Y: 212, H: 284, W: 824)
            // ==========================================
            pnlListWrapper = new Panel();
            pnlListWrapper.Location = new Point(18, 212);
            pnlListWrapper.Size = new Size(824, 284);
            pnlListWrapper.BackColor = PexorisTheme.AppleWhite;
            pnlListWrapper.Paint += PnlListWrapper_Paint;

            lvJobs = new ListView();
            lvJobs.Dock = DockStyle.Fill;
            lvJobs.View = View.Details;
            lvJobs.FullRowSelect = true;
            lvJobs.GridLines = false;
            lvJobs.BorderStyle = BorderStyle.None;
            lvJobs.Font = PexorisTheme.FontBody(9f);
            lvJobs.HideSelection = false;
            lvJobs.MultiSelect = false;

            lvJobs.Columns.Add("Job ID", 64, HorizontalAlignment.Left);
            lvJobs.Columns.Add("Document Name", 230, HorizontalAlignment.Left);
            lvJobs.Columns.Add("Printer Name", 140, HorizontalAlignment.Left);
            lvJobs.Columns.Add("Status", 110, HorizontalAlignment.Left);
            lvJobs.Columns.Add("Owner", 80, HorizontalAlignment.Left);
            lvJobs.Columns.Add("Pages", 64, HorizontalAlignment.Left);
            lvJobs.Columns.Add("Size", 70, HorizontalAlignment.Left);
            lvJobs.Columns.Add("Submitted Time", 140, HorizontalAlignment.Left);

            pnlListWrapper.Controls.Add(lvJobs);
            this.Controls.Add(pnlListWrapper);

            // ==========================================
            // 5. ACTION BUTTONS BAR (Y: 508, H: 50, W: 824)
            // ==========================================
            pnlActions = new Panel();
            pnlActions.Location = new Point(18, 508);
            pnlActions.Size = new Size(824, 50);

            // Slot 1: Primary Red Destructive Purge Button
            btnPurgeAll = new Button();
            btnPurgeAll.Text = "⚡ Purge Queue & Restart Spooler";
            btnPurgeAll.UseMnemonic = false;
            btnPurgeAll.Location = new Point(0, 5);
            btnPurgeAll.Size = new Size(224, 40);
            btnPurgeAll.Font = PexorisTheme.FontBold(9.5f);
            PexorisTheme.StyleAppleButton(btnPurgeAll, PexorisTheme.AppleRed, PexorisTheme.AppleRedHover, Color.White, false);
            btnPurgeAll.Click += BtnPurgeAll_Click;
            pnlActions.Controls.Add(btnPurgeAll);

            // Slot 2: Print Test Page
            btnPrintTest = new Button();
            btnPrintTest.Text = "🖨️ Print Test Page";
            btnPrintTest.Location = new Point(232, 5);
            btnPrintTest.Size = new Size(140, 40);
            btnPrintTest.Font = PexorisTheme.FontBold(9f);
            PexorisTheme.StyleAppleButton(btnPrintTest, PexorisTheme.AppleBlue, PexorisTheme.AppleBlueHover, Color.White, false);
            btnPrintTest.Click += BtnPrintTest_Click;
            pnlActions.Controls.Add(btnPrintTest);

            // Slot 3: Restart Spooler Service
            btnRestartSpooler = new Button();
            btnRestartSpooler.Text = "🔄 Restart Spooler";
            btnRestartSpooler.Location = new Point(380, 5);
            btnRestartSpooler.Size = new Size(144, 40);
            btnRestartSpooler.Font = PexorisTheme.FontBold(9f);
            PexorisTheme.StyleAppleButton(btnRestartSpooler, PexorisTheme.AppleBlue, PexorisTheme.AppleBlueHover, Color.White, false);
            btnRestartSpooler.Click += BtnRestartSpooler_Click;
            pnlActions.Controls.Add(btnRestartSpooler);

            // Slot 4: Cancel Selected Job (Outline)
            btnCancelJob = new Button();
            btnCancelJob.Text = "❌ Cancel Selected";
            btnCancelJob.Location = new Point(532, 5);
            btnCancelJob.Size = new Size(144, 40);
            btnCancelJob.Font = PexorisTheme.FontBold(9f);
            PexorisTheme.StyleAppleButton(btnCancelJob, PexorisTheme.AppleWhite, PexorisTheme.AppleCardHover, PexorisTheme.TextPrimary, true);
            btnCancelJob.Click += BtnCancelJob_Click;
            pnlActions.Controls.Add(btnCancelJob);

            // Slot 5: Refresh Queue (Scan button anchored right)
            btnRefresh = new Button();
            btnRefresh.Text = "🔍 Refresh";
            btnRefresh.Location = new Point(704, 5);
            btnRefresh.Size = new Size(120, 40);
            btnRefresh.Font = PexorisTheme.FontBold(9f);
            PexorisTheme.StyleAppleButton(btnRefresh, PexorisTheme.AppleWhite, PexorisTheme.AppleCardHover, PexorisTheme.TextPrimary, true);
            btnRefresh.Click += delegate { LoadPrintersAndJobs(); };
            pnlActions.Controls.Add(btnRefresh);

            this.Controls.Add(pnlActions);

            // ==========================================
            // 6. FOOTER (Y: 596, H: 44, W: 860)
            // ==========================================
            pnlFooter = new Panel();
            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.Height = 44;
            pnlFooter.BackColor = PexorisTheme.AppleWhite;
            pnlFooter.Paint += PnlFooter_Paint;

            chkAutoDeleteFiles = new CheckBox();
            chkAutoDeleteFiles.Text = "Automatically delete corrupt .SPL/.SHD spool files";
            chkAutoDeleteFiles.Checked = true;
            chkAutoDeleteFiles.Font = PexorisTheme.FontBody(8.5f);
            chkAutoDeleteFiles.ForeColor = PexorisTheme.TextSecondary;
            chkAutoDeleteFiles.Location = new Point(18, 12);
            chkAutoDeleteFiles.AutoSize = true;
            pnlFooter.Controls.Add(chkAutoDeleteFiles);

            lblFooterStatus = new Label();
            lblFooterStatus.Text = "Ready. 100% Standalone & Portable.";
            lblFooterStatus.Font = PexorisTheme.FontBody(8.5f);
            lblFooterStatus.ForeColor = PexorisTheme.TextMuted;
            lblFooterStatus.Location = new Point(410, 13);
            lblFooterStatus.AutoSize = true;
            pnlFooter.Controls.Add(lblFooterStatus);

            lnkPexoris = new LinkLabel();
            lnkPexoris.Text = "pexoris.com";
            lnkPexoris.Font = PexorisTheme.FontBold(9f);
            lnkPexoris.LinkColor = PexorisTheme.AppleBlue;
            lnkPexoris.ActiveLinkColor = PexorisTheme.AppleBlueHover;
            lnkPexoris.Location = new Point(765, 12);
            lnkPexoris.AutoSize = true;
            lnkPexoris.LinkClicked += delegate {
                try { Process.Start("https://pexoris.com"); } catch { }
            };
            pnlFooter.Controls.Add(lnkPexoris);

            this.Controls.Add(pnlFooter);

            // Status Polling Timer (Checks Spooler service health every 5s)
            timerStatus = new Timer();
            timerStatus.Interval = 5000;
            timerStatus.Tick += delegate { UpdateSpoolerStatusBadge(); };
            timerStatus.Start();

            this.ResumeLayout(false);
        }

        private void ApplyCustomStyles()
        {
            PexorisTheme.ApplyExplorerTheme(lvJobs.Handle);
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
        // PAINT EVENTS & ROUNDED EDGES
        // ==========================================
        private void PnlTitleBar_Paint(object sender, PaintEventArgs e)
        {
            using (Pen p = new Pen(PexorisTheme.BorderLight))
            {
                e.Graphics.DrawLine(p, 0, pnlTitleBar.Height - 1, pnlTitleBar.Width, pnlTitleBar.Height - 1);
            }
        }

        private void CardTop_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF r = new RectangleF(0, 0, cardTop.Width - 1, cardTop.Height - 1);
            using (GraphicsPath path = PexorisTheme.GetRoundedPath(r, 8f))
            {
                using (Pen p = new Pen(PexorisTheme.BorderMedium))
                {
                    e.Graphics.DrawPath(p, path);
                }
            }
        }

        private void PnlListWrapper_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF r = new RectangleF(0, 0, pnlListWrapper.Width - 1, pnlListWrapper.Height - 1);
            using (GraphicsPath path = PexorisTheme.GetRoundedPath(r, 6f))
            {
                using (Pen p = new Pen(PexorisTheme.BorderMedium))
                {
                    e.Graphics.DrawPath(p, path);
                }
            }
        }

        private void PillStatus_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF r = new RectangleF(0, 0, pillStatus.Width - 1, pillStatus.Height - 1);
            using (GraphicsPath path = PexorisTheme.GetRoundedPath(r, 12f))
            {
                Color borderC = pillStatus.ForeColor == PexorisTheme.AppleGreen ? Color.FromArgb(160, 52, 199, 89) : Color.FromArgb(160, 255, 59, 48);
                using (Pen p = new Pen(borderC))
                {
                    e.Graphics.DrawPath(p, path);
                }
            }
        }

        private void PnlFooter_Paint(object sender, PaintEventArgs e)
        {
            using (Pen p = new Pen(PexorisTheme.BorderLight))
            {
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            }
        }

        // ==========================================
        // DATA POPULATION & LOGIC
        // ==========================================
        private void LoadPrintersAndJobs()
        {
            // Populate Printers ComboBox
            string prevSel = cboPrinters.SelectedItem != null ? cboPrinters.SelectedItem.ToString() : "";
            cboPrinters.Items.Clear();
            cboPrinters.Items.Add("All Printers (Show Global Queue)");

            List<PrinterItem> printers = SpoolerHelper.GetInstalledPrinters();
            int selIdx = 0;
            for (int i = 0; i < printers.Count; i++)
            {
                PrinterItem p = printers[i];
                int idx = cboPrinters.Items.Add(p.Name + (p.IsDefault ? " [Default]" : "") + " (" + p.Status + ")");
                if (p.IsDefault && string.IsNullOrEmpty(prevSel))
                {
                    selIdx = idx;
                }
                else if (!string.IsNullOrEmpty(prevSel) && prevSel.Contains(p.Name))
                {
                    selIdx = idx;
                }
            }

            if (cboPrinters.Items.Count > 0)
            {
                cboPrinters.SelectedIndex = selIdx;
            }

            RefreshJobsListView();
            UpdateSpoolerStatusBadge();
        }

        private string GetSelectedPrinterName()
        {
            if (cboPrinters.SelectedItem == null) return null;
            string text = cboPrinters.SelectedItem.ToString();
            if (text.StartsWith("All Printers", StringComparison.OrdinalIgnoreCase)) return null;

            if (text.Contains(" [Default]")) text = text.Replace(" [Default]", "");
            if (text.Contains(" (")) text = text.Substring(0, text.IndexOf(" (")).Trim();
            return text;
        }

        private void RefreshJobsListView()
        {
            lvJobs.BeginUpdate();
            lvJobs.Items.Clear();

            string filter = GetSelectedPrinterName();
            List<PrintJobItem> jobs = SpoolerHelper.GetAllPrintJobs(filter);

            int stuckCount = 0;

            if (jobs.Count == 0)
            {
                ListViewItem empty = new ListViewItem("—");
                empty.SubItems.Add("No active print jobs in queue (Spooler is healthy)");
                empty.SubItems.Add(filter != null ? filter : "All Printers");
                empty.SubItems.Add("Idle");
                empty.SubItems.Add("—");
                empty.SubItems.Add("—");
                empty.SubItems.Add("—");
                empty.SubItems.Add("—");
                empty.ForeColor = PexorisTheme.TextMuted;
                lvJobs.Items.Add(empty);
            }
            else
            {
                for (int i = 0; i < jobs.Count; i++)
                {
                    PrintJobItem j = jobs[i];
                    ListViewItem lvi = new ListViewItem(j.JobId.ToString());
                    lvi.SubItems.Add(j.DocumentName);
                    lvi.SubItems.Add(j.PrinterName);
                    lvi.SubItems.Add(j.Status);
                    lvi.SubItems.Add(j.Owner);
                    lvi.SubItems.Add(j.Pages);
                    lvi.SubItems.Add(j.Size);
                    lvi.SubItems.Add(j.SubmittedTime);

                    lvi.Tag = j;

                    if (j.IsStuck)
                    {
                        stuckCount++;
                        lvi.BackColor = PexorisTheme.AppleRedLight;
                        lvi.ForeColor = PexorisTheme.AppleRedHover;
                    }
                    else
                    {
                        lvi.ForeColor = PexorisTheme.TextPrimary;
                    }

                    lvJobs.Items.Add(lvi);
                }
            }

            lvJobs.EndUpdate();

            if (stuckCount > 0)
            {
                pillStatus.Text = string.Format("⚠️ {0} STUCK JOB(S)", stuckCount);
                pillStatus.ForeColor = PexorisTheme.AppleRed;
                pillStatus.BackColor = PexorisTheme.AppleRedLight;
            }
            else
            {
                UpdateSpoolerStatusBadge();
            }

            lblFooterStatus.Text = string.Format("Ready. {0} active job(s)  •  Spooler service: {1}", jobs.Count, SpoolerHelper.GetSpoolerStatusString());
        }

        private void UpdateSpoolerStatusBadge()
        {
            string status = SpoolerHelper.GetSpoolerStatusString();
            if (status.Equals("Running", StringComparison.OrdinalIgnoreCase))
            {
                pillStatus.Text = "SPOOLER RUNNING";
                pillStatus.ForeColor = PexorisTheme.AppleGreen;
                pillStatus.BackColor = PexorisTheme.AppleGreenLight;
            }
            else
            {
                pillStatus.Text = "SPOOLER " + status.ToUpper();
                pillStatus.ForeColor = PexorisTheme.AppleRed;
                pillStatus.BackColor = PexorisTheme.AppleRedLight;
            }
            pillStatus.Invalidate();
        }

        private void FilterOnlyStuckJobs()
        {
            lvJobs.BeginUpdate();
            lvJobs.Items.Clear();

            List<PrintJobItem> jobs = SpoolerHelper.GetAllPrintJobs(null);
            int added = 0;

            for (int i = 0; i < jobs.Count; i++)
            {
                PrintJobItem j = jobs[i];
                if (j.IsStuck)
                {
                    ListViewItem lvi = new ListViewItem(j.JobId.ToString());
                    lvi.SubItems.Add(j.DocumentName);
                    lvi.SubItems.Add(j.PrinterName);
                    lvi.SubItems.Add(j.Status);
                    lvi.SubItems.Add(j.Owner);
                    lvi.SubItems.Add(j.Pages);
                    lvi.SubItems.Add(j.Size);
                    lvi.SubItems.Add(j.SubmittedTime);
                    lvi.Tag = j;
                    lvi.BackColor = PexorisTheme.AppleRedLight;
                    lvi.ForeColor = PexorisTheme.AppleRedHover;
                    lvJobs.Items.Add(lvi);
                    added++;
                }
            }

            if (added == 0)
            {
                ListViewItem empty = new ListViewItem("—");
                empty.SubItems.Add("Clean! Zero stuck or errored jobs detected.");
                empty.SubItems.Add("All");
                empty.SubItems.Add("Ready");
                empty.SubItems.Add("—");
                empty.SubItems.Add("—");
                empty.SubItems.Add("—");
                empty.SubItems.Add("—");
                empty.ForeColor = PexorisTheme.AppleGreen;
                lvJobs.Items.Add(empty);
            }

            lvJobs.EndUpdate();
        }

        private void CboPrinters_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshJobsListView();
        }

        private void OpenSpoolFolder()
        {
            try
            {
                string spoolDir = SpoolerHelper.SpoolPrintersDirectory;
                if (!Directory.Exists(spoolDir)) Directory.CreateDirectory(spoolDir);
                Process.Start("explorer.exe", spoolDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open Spool folder: " + ex.Message, "Pexoris PrintFixer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ==========================================
        // ACTION BUTTON HANDLERS
        // ==========================================
        private void BtnPurgeAll_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                "Are you sure you want to purge all stuck print jobs?\n\nThis will temporarily stop the Print Spooler, safely delete all corrupt .SPL and .SHD spool files from C:\\Windows\\System32\\spool\\PRINTERS, and cleanly restart the service.",
                "Purge Stuck Print Queue",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            btnPurgeAll.Enabled = false;
            btnPurgeAll.Text = "Purging Spool Queue...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                PurgeResult result = SpoolerHelper.PurgeAllPrintQueuesAndFiles();
                this.Cursor = Cursors.Default;
                btnPurgeAll.Enabled = true;
                btnPurgeAll.Text = "⚡ Purge Queue & Restart Spooler";

                RefreshJobsListView();
                UpdateSpoolerStatusBadge();

                MessageBox.Show(result.Message, "Pexoris PrintFixer", MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                btnPurgeAll.Enabled = true;
                btnPurgeAll.Text = "⚡ Purge Queue & Restart Spooler";
                MessageBox.Show("Error during purge: " + ex.Message, "Pexoris PrintFixer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrintTest_Click(object sender, EventArgs e)
        {
            string pName = GetSelectedPrinterName();
            if (string.IsNullOrEmpty(pName))
            {
                MessageBox.Show("Please select a specific printer from the dropdown above to send a test page.", "Pexoris PrintFixer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool ok = SpoolerHelper.PrintTestPage(pName);
            if (ok)
            {
                lblFooterStatus.Text = "Windows test page sent to: " + pName;
            }
            else
            {
                MessageBox.Show("Failed to trigger test page on printer: " + pName, "Pexoris PrintFixer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnRestartSpooler_Click(object sender, EventArgs e)
        {
            btnRestartSpooler.Enabled = false;
            btnRestartSpooler.Text = "Restarting...";
            this.Cursor = Cursors.WaitCursor;

            bool ok = SpoolerHelper.RestartSpoolerService();

            this.Cursor = Cursors.Default;
            btnRestartSpooler.Enabled = true;
            btnRestartSpooler.Text = "🔄 Restart Spooler";

            UpdateSpoolerStatusBadge();
            RefreshJobsListView();

            if (ok)
            {
                MessageBox.Show("Print Spooler service restarted successfully!", "Pexoris PrintFixer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Unable to restart Print Spooler. Ensure you ran as Administrator.", "Pexoris PrintFixer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnCancelJob_Click(object sender, EventArgs e)
        {
            if (lvJobs.SelectedItems.Count == 0 || lvJobs.SelectedItems[0].Tag == null)
            {
                MessageBox.Show("Please select a print job from the list to cancel.", "Pexoris PrintFixer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PrintJobItem job = (PrintJobItem)lvJobs.SelectedItems[0].Tag;
            DialogResult dr = MessageBox.Show(
                string.Format("Cancel Job #{0} (\"{1}\") on \"{2}\"?", job.JobId, job.DocumentName, job.PrinterName),
                "Cancel Print Job",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                bool ok = SpoolerHelper.CancelJob(job.PrinterName, job.JobId);
                if (ok)
                {
                    lblFooterStatus.Text = string.Format("Job #{0} cancelled successfully.", job.JobId);
                }
                else
                {
                    MessageBox.Show("Could not cancel job via Windows API. Try '⚡ Purge Queue & Restart Spooler'.", "Pexoris PrintFixer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                RefreshJobsListView();
            }
        }
    }
}
