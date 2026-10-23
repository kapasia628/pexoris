using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisClipboardHistory
{
    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int CS_DROPSHADOW = 0x00020000;

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
        private Panel _titleBar;
        private PictureBox _titleIcon;
        private Label _titleLabel;
        private Button _btnClose;
        private Button _btnMin;

        private Panel _topCard;
        private PictureBox _cardIcon;
        private Label _cardHeadline;
        private Label _cardSubtitle;
        private Label _pillStatus;

        private TextBox _txtSearch;
        private Label _lblStatTotal;
        private Label _lblStatPinned;
        private Label _lblStatSensitive;
        private Label _lblStatMemory;

        private ListView _listView;
        private Panel _actionBar;
        private PexorisButton _btnWipe;
        private PexorisButton _btnPin;
        private PexorisButton _btnCopy;
        private PexorisButton _btnScrub;
        private PexorisButton _btnExport;

        private Panel _footerBar;
        private Label _lblStatus;
        private LinkLabel _lnkWebsite;

        private bool _listenerRegistered = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(860, 640);
            BackColor = Theme.CanvasBg;
            Text = "Pexoris ClipboardHistory";

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            // 1. Title Bar (44px)
            _titleBar = new Panel();
            _titleBar.Location = new Point(0, 0);
            _titleBar.Size = new Size(860, 44);
            _titleBar.BackColor = Theme.CardBg;
            _titleBar.MouseDown += TitleBar_MouseDown;

            _titleIcon = new PictureBox();
            _titleIcon.Location = new Point(16, 11);
            _titleIcon.Size = new Size(22, 22);
            _titleIcon.SizeMode = PictureBoxSizeMode.Zoom;
            if (Icon != null) _titleIcon.Image = Icon.ToBitmap();
            _titleIcon.MouseDown += TitleBar_MouseDown;

            _titleLabel = new Label();
            _titleLabel.Location = new Point(46, 13);
            _titleLabel.Size = new Size(540, 20);
            _titleLabel.Text = "Pexoris ClipboardHistory • Encrypted Portable Clipboard Vault";
            _titleLabel.Font = Theme.TitleFont;
            _titleLabel.ForeColor = Theme.TextHero;
            _titleLabel.MouseDown += TitleBar_MouseDown;

            _btnMin = new Button();
            _btnMin.Location = new Point(776, 0);
            _btnMin.Size = new Size(42, 44);
            _btnMin.FlatStyle = FlatStyle.Flat;
            _btnMin.FlatAppearance.BorderSize = 0;
            _btnMin.Text = "—";
            _btnMin.Font = new Font("Segoe UI", 9f);
            _btnMin.ForeColor = Theme.TextMuted;
            _btnMin.Cursor = Cursors.Hand;
            _btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;

            _btnClose = new Button();
            _btnClose.Location = new Point(818, 0);
            _btnClose.Size = new Size(42, 44);
            _btnClose.FlatStyle = FlatStyle.Flat;
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Text = "✕";
            _btnClose.Font = new Font("Segoe UI", 9f);
            _btnClose.ForeColor = Theme.TextMuted;
            _btnClose.Cursor = Cursors.Hand;
            _btnClose.MouseEnter += (s, e) => { _btnClose.BackColor = Theme.Destructive; _btnClose.ForeColor = Color.White; };
            _btnClose.MouseLeave += (s, e) => { _btnClose.BackColor = Theme.CardBg; _btnClose.ForeColor = Theme.TextMuted; };
            _btnClose.Click += (s, e) => Application.Exit();

            _titleBar.Controls.Add(_titleIcon);
            _titleBar.Controls.Add(_titleLabel);
            _titleBar.Controls.Add(_btnMin);
            _titleBar.Controls.Add(_btnClose);

            // 2. Top Control Card (Y=52, H=128)
            _topCard = new Panel();
            _topCard.Location = new Point(14, 52);
            _topCard.Size = new Size(832, 128);
            _topCard.BackColor = Theme.CardBg;
            _topCard.Paint += TopCard_Paint;

            _cardIcon = new PictureBox();
            _cardIcon.Location = new Point(16, 16);
            _cardIcon.Size = new Size(44, 44);
            _cardIcon.SizeMode = PictureBoxSizeMode.Zoom;
            if (Icon != null) _cardIcon.Image = Icon.ToBitmap();

            _cardHeadline = new Label();
            _cardHeadline.Location = new Point(68, 14);
            _cardHeadline.Size = new Size(460, 22);
            _cardHeadline.Text = "Encrypted Portable Clipboard Manager";
            _cardHeadline.Font = Theme.HeaderFont;
            _cardHeadline.ForeColor = Theme.TextHero;

            _cardSubtitle = new Label();
            _cardSubtitle.Location = new Point(68, 36);
            _cardSubtitle.Size = new Size(540, 18);
            _cardSubtitle.Text = "RAM-only clipboard vault with instant search, credential detection, and auto-purge.";
            _cardSubtitle.Font = Theme.SubtitleFont;
            _cardSubtitle.ForeColor = Theme.TextMuted;

            _pillStatus = new Label();
            _pillStatus.Location = new Point(615, 16);
            _pillStatus.Size = new Size(200, 26);
            _pillStatus.TextAlign = ContentAlignment.MiddleCenter;
            _pillStatus.Font = Theme.BodyBold;
            _pillStatus.BackColor = Theme.SuccessLight;
            _pillStatus.ForeColor = Theme.Success;
            _pillStatus.Text = "● Listener Active (RAM Vault)";
            _pillStatus.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.Success, 1f))
                    e.Graphics.DrawRectangle(p, 0, 0, _pillStatus.Width - 1, _pillStatus.Height - 1);
            };

            // Search Box and Stat Tiles Row (Y=66)
            _txtSearch = new TextBox();
            _txtSearch.Location = new Point(16, 76);
            _txtSearch.Size = new Size(200, 24);
            _txtSearch.Font = Theme.BodyFont;
            _txtSearch.ForeColor = Theme.TextHero;
            _txtSearch.Text = "";
            _txtSearch.TextChanged += (s, e) => RenderListView();

            Label lblSearchPrompt = new Label();
            lblSearchPrompt.Location = new Point(16, 58);
            lblSearchPrompt.Size = new Size(200, 16);
            lblSearchPrompt.Text = "🔍 Search History / Regex:";
            lblSearchPrompt.Font = Theme.SmallFont;
            lblSearchPrompt.ForeColor = Theme.TextMuted;

            _lblStatTotal = CreateStatTile(230, 64, 140, 50, "Total Clips", "0");
            _lblStatPinned = CreateStatTile(380, 64, 140, 50, "Pinned", "0");
            _lblStatSensitive = CreateStatTile(530, 64, 140, 50, "Secrets Filtered", "0");
            _lblStatMemory = CreateStatTile(680, 64, 135, 50, "RAM Usage", "0 KB");

            _topCard.Controls.Add(_cardIcon);
            _topCard.Controls.Add(_cardHeadline);
            _topCard.Controls.Add(_cardSubtitle);
            _topCard.Controls.Add(_pillStatus);
            _topCard.Controls.Add(lblSearchPrompt);
            _topCard.Controls.Add(_txtSearch);
            _topCard.Controls.Add(_lblStatTotal);
            _topCard.Controls.Add(_lblStatPinned);
            _topCard.Controls.Add(_lblStatSensitive);
            _topCard.Controls.Add(_lblStatMemory);

            // 3. Explorer Table (Y=188, H=314)
            _listView = new ListView();
            _listView.Location = new Point(14, 188);
            _listView.Size = new Size(832, 314);
            _listView.View = View.Details;
            _listView.FullRowSelect = true;
            _listView.GridLines = false;
            _listView.BorderStyle = BorderStyle.FixedSingle;
            _listView.Font = Theme.BodyFont;
            _listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _listView.DoubleClick += (s, e) => CopySelectedItem();

            _listView.Columns.Add("Time", 85);
            _listView.Columns.Add("Type", 100);
            _listView.Columns.Add("Preview Content", 445);
            _listView.Columns.Add("Length", 75);
            _listView.Columns.Add("Status / Security", 115);

            // 4. Action Buttons Bar (Y=510, H=46)
            _actionBar = new Panel();
            _actionBar.Location = new Point(14, 510);
            _actionBar.Size = new Size(832, 46);

            _btnWipe = new PexorisButton();
            _btnWipe.Location = new Point(0, 5);
            _btnWipe.Size = new Size(185, 36);
            _btnWipe.Text = "⚡ Wipe Clipboard & RAM";
            _btnWipe.ButtonStyle = PexorisButtonStyle.DestructiveRed;
            _btnWipe.Click += BtnWipe_Click;

            _btnPin = new PexorisButton();
            _btnPin.Location = new Point(195, 5);
            _btnPin.Size = new Size(150, 36);
            _btnPin.Text = "📌 Pin / Unpin";
            _btnPin.ButtonStyle = PexorisButtonStyle.PrimaryBlue;
            _btnPin.Click += BtnPin_Click;

            _btnCopy = new PexorisButton();
            _btnCopy.Location = new Point(355, 5);
            _btnCopy.Size = new Size(155, 36);
            _btnCopy.Text = "📋 Copy to Clipboard";
            _btnCopy.ButtonStyle = PexorisButtonStyle.PrimaryBlue;
            _btnCopy.Click += (s, e) => CopySelectedItem();

            _btnScrub = new PexorisButton();
            _btnScrub.Location = new Point(520, 5);
            _btnScrub.Size = new Size(155, 36);
            _btnScrub.Text = "🛡️ Scrub Secrets";
            _btnScrub.ButtonStyle = PexorisButtonStyle.SecondaryOutline;
            _btnScrub.Click += BtnScrub_Click;

            _btnExport = new PexorisButton();
            _btnExport.Location = new Point(685, 5);
            _btnExport.Size = new Size(147, 36);
            _btnExport.Text = "💾 Export CSV";
            _btnExport.ButtonStyle = PexorisButtonStyle.SecondaryOutline;
            _btnExport.Click += BtnExport_Click;

            _actionBar.Controls.Add(_btnWipe);
            _actionBar.Controls.Add(_btnPin);
            _actionBar.Controls.Add(_btnCopy);
            _actionBar.Controls.Add(_btnScrub);
            _actionBar.Controls.Add(_btnExport);

            // 5. Footer (38px)
            _footerBar = new Panel();
            _footerBar.Location = new Point(0, 562);
            _footerBar.Size = new Size(860, 38);
            _footerBar.BackColor = Theme.CardBg;

            _lblStatus = new Label();
            _lblStatus.Location = new Point(18, 11);
            _lblStatus.Size = new Size(620, 18);
            _lblStatus.Text = "Ready • In-memory encrypted clipboard vault (Double-click any row to copy)";
            _lblStatus.Font = Theme.SmallFont;
            _lblStatus.ForeColor = Theme.TextMuted;

            _lnkWebsite = new LinkLabel();
            _lnkWebsite.Location = new Point(720, 11);
            _lnkWebsite.Size = new Size(125, 18);
            _lnkWebsite.Text = "pexoris.com";
            _lnkWebsite.TextAlign = ContentAlignment.TopRight;
            _lnkWebsite.Font = Theme.SmallFont;
            _lnkWebsite.LinkColor = Theme.Primary;
            _lnkWebsite.ActiveLinkColor = Theme.PrimaryHover;
            _lnkWebsite.LinkClicked += (s, e) => {
                try { Process.Start("https://pexoris.com/tools/clipboard-history/"); } catch { }
            };

            _footerBar.Controls.Add(_lblStatus);
            _footerBar.Controls.Add(_lnkWebsite);

            Controls.Add(_titleBar);
            Controls.Add(_topCard);
            Controls.Add(_listView);
            Controls.Add(_actionBar);
            Controls.Add(_footerBar);

            // Hook engine events
            ClipboardEngine.OnClipsUpdated += (s, e) => {
                if (InvokeRequired) Invoke(new Action(RenderListView));
                else RenderListView();
            };
        }

        private Label CreateStatTile(int x, int y, int w, int h, string title, string initialVal)
        {
            Label lbl = new Label();
            lbl.Location = new Point(x, y);
            lbl.Size = new Size(w, h);
            lbl.BackColor = Color.FromArgb(248, 250, 252);
            lbl.Text = initialVal + "\n" + title;
            lbl.Font = Theme.HeaderFont;
            lbl.ForeColor = Theme.TextHero;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Paint += (s, e) => {
                using (Pen p = new Pen(Theme.BorderLight, 1f))
                    e.Graphics.DrawRectangle(p, 0, 0, w - 1, h - 1);
            };
            return lbl;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                SetWindowTheme(_listView.Handle, "Explorer", null);

                if (Icon != null)
                {
                    SendMessage(Handle, WM_SETICON, (IntPtr)ICON_SMALL, Icon.Handle);
                    SendMessage(Handle, WM_SETICON, (IntPtr)ICON_BIG, Icon.Handle);
                }

                if (!_listenerRegistered)
                {
                    _listenerRegistered = ClipboardEngine.AddClipboardFormatListener(Handle);
                }
            }
            catch { }

            // Take initial snapshot if clipboard contains text
            ClipboardEngine.CaptureCurrentClipboard();
            RenderListView();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_listenerRegistered)
            {
                try
                {
                    ClipboardEngine.RemoveClipboardFormatListener(Handle);
                    _listenerRegistered = false;
                }
                catch { }
            }
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == ClipboardEngine.WM_CLIPBOARDUPDATE)
            {
                ClipboardEngine.CaptureCurrentClipboard();
            }
            base.WndProc(ref m);
        }

        private void TopCard_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Theme.BorderLight, 1f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, _topCard.Width - 1, _topCard.Height - 1);
            }
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void RenderListView()
        {
            List<ClipItem> all = ClipboardEngine.GetAllClips();
            string filter = _txtSearch.Text.Trim();

            _listView.BeginUpdate();
            _listView.Items.Clear();

            foreach (ClipItem it in all)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    if (it.Content.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        it.ContentType.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                ListViewItem lvi = new ListViewItem(it.Timestamp.ToString("HH:mm:ss"));
                lvi.SubItems.Add(it.ContentType);
                lvi.SubItems.Add(it.PreviewText);
                lvi.SubItems.Add(it.Length + " chars");

                string status = "";
                if (it.IsPinned) status += "📌 Pinned ";
                if (it.IsSensitive) status += "🔒 Secret";
                if (string.IsNullOrEmpty(status)) status = "Standard";

                lvi.SubItems.Add(status);
                lvi.Tag = it;

                if (it.IsPinned)
                {
                    lvi.BackColor = Theme.PrimaryLight;
                    lvi.ForeColor = Theme.Primary;
                }
                else if (it.IsSensitive)
                {
                    lvi.BackColor = Theme.WarningLight;
                    lvi.ForeColor = Color.FromArgb(180, 83, 9);
                }

                _listView.Items.Add(lvi);
            }

            _listView.EndUpdate();

            _lblStatTotal.Text = ClipboardEngine.TotalClipsCount + "\nTotal Clips";
            _lblStatPinned.Text = ClipboardEngine.PinnedCount + "\nPinned";
            _lblStatSensitive.Text = ClipboardEngine.SensitiveCount + "\nSecrets Filtered";

            long memBytes = ClipboardEngine.ApproximateMemoryUsageBytes;
            string memDisplay = (memBytes / 1024) + " KB";
            _lblStatMemory.Text = memDisplay + "\nRAM Usage";

            _lblStatus.Text = string.Format("Active • Storing {0} items in encrypted RAM vault • Zero disk trace", ClipboardEngine.TotalClipsCount);
        }

        private void CopySelectedItem()
        {
            if (_listView.SelectedItems.Count == 0) return;
            ClipItem it = _listView.SelectedItems[0].Tag as ClipItem;
            if (it == null) return;

            ClipboardEngine.CopyToClipboard(it);
            _lblStatus.Text = string.Format("Copied to clipboard: {0} ({1} chars)", it.ContentType, it.Length);
        }

        private void BtnPin_Click(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a clip to pin or unpin.", "Pexoris ClipboardHistory", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ClipItem it = _listView.SelectedItems[0].Tag as ClipItem;
            ClipboardEngine.TogglePin(it);
        }

        private void BtnScrub_Click(object sender, EventArgs e)
        {
            int sensitive = ClipboardEngine.SensitiveCount;
            if (sensitive == 0)
            {
                MessageBox.Show("No unpinned sensitive credentials or secrets found in history.", "Clean Vault", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult dr = MessageBox.Show(
                string.Format("Found {0} sensitive item(s) (Credit Cards, API tokens, Private keys).\n\nScrub them permanently from memory?", sensitive),
                "Scrub Sensitive Secrets",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dr == DialogResult.Yes)
            {
                ClipboardEngine.ScrubSensitive();
                MessageBox.Show("All unpinned sensitive items have been scrubbed from memory.", "Purge Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnWipe_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                "Are you sure you want to WIPE all clipboard history from memory and empty the Windows clipboard?\n\nThis cannot be undone.",
                "Confirm Nuclear Wipe",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                ClipboardEngine.NuclearWipe();
                MessageBox.Show("Clipboard and in-memory vault wiped completely.", "Vault Cleared", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (ClipboardEngine.TotalClipsCount == 0)
            {
                MessageBox.Show("No clips to export.", "Empty Vault", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV File (*.csv)|*.csv|Text File (*.txt)|*.txt";
                sfd.FileName = "Pexoris_Clipboard_Audit_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string csv = ClipboardEngine.ExportToCsv();
                        File.WriteAllText(sfd.FileName, csv);
                        MessageBox.Show("Clipboard audit report exported successfully to:\n" + sfd.FileName, "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving report: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
