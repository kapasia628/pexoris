using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pexoris.ContextMenuEditor
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

        private PexorisButton btnPresetClassic;
        private PexorisButton btnPresetModern;
        private PexorisButton btnPresetRestartExp;

        private Label lblSection;
        private TextBox txtSearch;
        private Panel pnlListWrapper;
        private ListView lvItems;

        private Panel pnlActions;
        private PexorisButton btnToggle;
        private PexorisButton btnDelete;
        private PexorisButton btnQuickClassic;
        private PexorisButton btnQuickModern;
        private PexorisButton btnRefresh;

        private Panel pnlFooter;
        private CheckBox chkAutoNotify;
        private Label lblFooterStatus;
        private LinkLabel lnkBrand;

        private Bitmap iconBitmap;
        private List<ContextMenuItem> allItems = new List<ContextMenuItem>();

        public MainForm()
        {
            InitializeComponent();
            LoadApplicationIcon();
            RefreshMenuStatus();
            ScanItems();
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

        private void LoadApplicationIcon()
        {
            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    this.Icon = appIcon;
                    SendMessage(this.Handle, WM_SETICON, ICON_SMALL, appIcon.Handle.ToInt32());
                    SendMessage(this.Handle, WM_SETICON, ICON_BIG, appIcon.Handle.ToInt32());
                    iconBitmap = appIcon.ToBitmap();
                    picTitleIcon.Image = iconBitmap;
                    picCardIcon.Image = iconBitmap;
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.ClientSize = new Size(860, 640);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.BgCanvas;
            this.Text = "Pexoris ContextMenuEditor";
            this.DoubleBuffered = true;

            // ==========================================
            // 1. TITLE BAR (44px)
            // ==========================================
            pnlTitleBar = new Panel
            {
                Size = new Size(860, 44),
                Location = new Point(0, 0),
                BackColor = Theme.BgCard
            };
            pnlTitleBar.MouseDown += TitleBar_MouseDown;

            picTitleIcon = new PictureBox
            {
                Size = new Size(22, 22),
                Location = new Point(14, 11),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            picTitleIcon.MouseDown += TitleBar_MouseDown;

            lblTitleText = new Label
            {
                Text = "Pexoris",
                Font = Theme.FontTitleBar,
                ForeColor = Theme.TextHero,
                Location = new Point(42, 12),
                AutoSize = true
            };
            lblTitleText.MouseDown += TitleBar_MouseDown;

            lblTitleBadge = new Label
            {
                Text = "ContextMenuEditor v1.0",
                Font = Theme.FontSmallBold,
                ForeColor = Theme.PrimaryBlue,
                BackColor = Theme.PrimaryLight,
                Location = new Point(106, 12),
                Padding = new Padding(5, 2, 5, 2),
                AutoSize = true
            };
            lblTitleBadge.MouseDown += TitleBar_MouseDown;

            btnMinimize = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Theme.TextMuted,
                Size = new Size(40, 44),
                Location = new Point(780, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnMinimize.MouseEnter += (s, e) => { btnMinimize.BackColor = Color.FromArgb(241, 245, 249); btnMinimize.ForeColor = Theme.TextHero; };
            btnMinimize.MouseLeave += (s, e) => { btnMinimize.BackColor = Color.Transparent; btnMinimize.ForeColor = Theme.TextMuted; };
            btnMinimize.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };

            btnClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextMuted,
                Size = new Size(40, 44),
                Location = new Point(820, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Theme.DangerRed; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = Theme.TextMuted; };
            btnClose.Click += (s, e) => { this.Close(); };

            pnlTitleBar.Controls.AddRange(new Control[] { picTitleIcon, lblTitleText, lblTitleBadge, btnMinimize, btnClose });
            pnlTitleBar.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderSubtle, 1f))
                    e.Graphics.DrawLine(p, 0, 43, 860, 43);
            };

            // ==========================================
            // 2. TOP CONTROL CARD (120px)
            // ==========================================
            cardTop = new Panel
            {
                Size = new Size(824, 120),
                Location = new Point(18, 56),
                BackColor = Theme.BgCard
            };
            cardTop.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = Theme.GetRoundedPath(new RectangleF(0, 0, 823, 119), 10f))
                {
                    using (Pen p = new Pen(Theme.BorderSubtle, 1f))
                        g.DrawPath(p, path);
                }
            };

            picCardIcon = new PictureBox
            {
                Size = new Size(44, 44),
                Location = new Point(18, 16),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            lblCardTitle = new Label
            {
                Text = "Windows 11 Context Menu Precision Manager",
                Font = Theme.FontTitleLarge,
                ForeColor = Theme.TextHero,
                Location = new Point(72, 14),
                AutoSize = true
            };

            lblCardSubtitle = new Label
            {
                Text = "Restore instant Windows 10 classic right-click menu or clean bloated shell extension handlers.",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Location = new Point(74, 38),
                AutoSize = true
            };

            pillStatus = new Label
            {
                Text = "● CHECKING STATUS...",
                Font = Theme.FontSmallBold,
                ForeColor = Theme.PrimaryBlue,
                BackColor = Theme.PrimaryLight,
                Location = new Point(560, 14),
                Size = new Size(248, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pillStatus.Paint += (s, e) =>
            {
                using (GraphicsPath path = Theme.GetRoundedPath(new RectangleF(0, 0, pillStatus.Width - 1, pillStatus.Height - 1), 6f))
                {
                    using (Pen p = new Pen(Theme.PrimaryBlue, 1f))
                        e.Graphics.DrawPath(p, path);
                }
            };

            // Quick Preset Action Buttons in Card Row
            btnPresetClassic = new PexorisButton
            {
                Text = "⚡ Restore Win10 Classic Menu",
                StyleType = ButtonStyleType.PrimaryBlue,
                Size = new Size(220, 36),
                Location = new Point(18, 70),
                Font = Theme.FontSmallBold
            };
            btnPresetClassic.Click += BtnPresetClassic_Click;

            btnPresetModern = new PexorisButton
            {
                Text = "↺ Modern Win11 Menu",
                StyleType = ButtonStyleType.SecondaryOutline,
                Size = new Size(170, 36),
                Location = new Point(246, 70),
                Font = Theme.FontSmallBold
            };
            btnPresetModern.Click += BtnPresetModern_Click;

            btnPresetRestartExp = new PexorisButton
            {
                Text = "⟳ Restart Explorer",
                StyleType = ButtonStyleType.SecondaryOutline,
                Size = new Size(140, 36),
                Location = new Point(424, 70),
                Font = Theme.FontSmallBold
            };
            btnPresetRestartExp.Click += (s, e) =>
            {
                ContextMenuHelper.RestartExplorer();
                lblFooterStatus.Text = "Windows Explorer restarted successfully.";
            };

            cardTop.Controls.AddRange(new Control[] {
                picCardIcon, lblCardTitle, lblCardSubtitle, pillStatus,
                btnPresetClassic, btnPresetModern, btnPresetRestartExp
            });

            // ==========================================
            // 3. MIDDLE SECTION HEADER & SEARCH
            // ==========================================
            lblSection = new Label
            {
                Text = "REGISTERED RIGHT-CLICK CONTEXT MENU HANDLERS (SHELL EXTENSIONS)",
                Font = Theme.FontSmallBold,
                ForeColor = Theme.TextMuted,
                Location = new Point(20, 188),
                AutoSize = true
            };

            txtSearch = new TextBox
            {
                Size = new Size(220, 22),
                Location = new Point(622, 184),
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Text = "Search handlers..."
            };
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "Search handlers...") { txtSearch.Text = ""; txtSearch.ForeColor = Theme.TextHero; } };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Search handlers..."; txtSearch.ForeColor = Theme.TextMuted; } };
            txtSearch.TextChanged += (s, e) => FilterList();

            // ==========================================
            // 4. LISTVIEW TABLE OF SHELL EXTENSIONS
            // ==========================================
            pnlListWrapper = new Panel
            {
                Size = new Size(824, 290),
                Location = new Point(18, 212),
                BackColor = Theme.BgCard
            };
            pnlListWrapper.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderSubtle, 1f))
                    e.Graphics.DrawRectangle(p, 0, 0, 823, 289);
            };

            lvItems = new ListView
            {
                Size = new Size(822, 288),
                Location = new Point(1, 1),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.BgCard,
                Font = Theme.FontBody,
                ForeColor = Theme.TextHero,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvItems.Columns.Add("Extension / Handler Name", 230);
            lvItems.Columns.Add("Scope / Target", 150);
            lvItems.Columns.Add("CLSID / Value Data", 330);
            lvItems.Columns.Add("Status", 90);
            lvItems.SelectedIndexChanged += LvItems_SelectedIndexChanged;

            pnlListWrapper.Controls.Add(lvItems);

            // ==========================================
            // 5. ACTION BUTTONS BAR (Y: 508, H: 50)
            // ==========================================
            pnlActions = new Panel
            {
                Size = new Size(824, 50),
                Location = new Point(18, 508),
                BackColor = Color.Transparent
            };

            btnToggle = new PexorisButton
            {
                Text = "⚡ Toggle Selected Item",
                StyleType = ButtonStyleType.DestructiveRed,
                Size = new Size(200, 42),
                Location = new Point(0, 4),
                Enabled = false
            };
            btnToggle.Click += BtnToggle_Click;

            btnDelete = new PexorisButton
            {
                Text = "🗑️ Remove Item",
                StyleType = ButtonStyleType.SecondaryOutline,
                Size = new Size(140, 42),
                Location = new Point(208, 4),
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;

            btnQuickClassic = new PexorisButton
            {
                Text = "⚡ Classic Win10",
                StyleType = ButtonStyleType.PrimaryBlue,
                Size = new Size(150, 42),
                Location = new Point(356, 4)
            };
            btnQuickClassic.Click += BtnPresetClassic_Click;

            btnQuickModern = new PexorisButton
            {
                Text = "↺ Modern Win11",
                StyleType = ButtonStyleType.SecondaryOutline,
                Size = new Size(150, 42),
                Location = new Point(514, 4)
            };
            btnQuickModern.Click += BtnPresetModern_Click;

            btnRefresh = new PexorisButton
            {
                Text = "🔍 Scan",
                StyleType = ButtonStyleType.SecondaryOutline,
                Size = new Size(140, 42),
                Location = new Point(672, 4)
            };
            btnRefresh.Click += (s, e) => { ScanItems(); };

            pnlActions.Controls.AddRange(new Control[] {
                btnToggle, btnDelete, btnQuickClassic, btnQuickModern, btnRefresh
            });

            // ==========================================
            // 6. FOOTER STATUS BAR (38px, Dock=Bottom)
            // ==========================================
            pnlFooter = new Panel
            {
                Size = new Size(860, 38),
                Dock = DockStyle.Bottom,
                BackColor = Theme.BgCard
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Theme.BorderSubtle, 1f))
                    e.Graphics.DrawLine(p, 0, 0, 860, 0);
            };

            chkAutoNotify = new CheckBox
            {
                Text = "Auto-notify Windows Shell on changes",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextBody,
                Checked = true,
                Location = new Point(18, 8),
                AutoSize = true
            };

            lblFooterStatus = new Label
            {
                Text = "Ready. 100% Standalone & Zero Telemetry.",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Location = new Point(280, 10),
                Size = new Size(420, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lnkBrand = new LinkLabel
            {
                Text = "pexoris.com",
                Font = Theme.FontSmallBold,
                LinkColor = Theme.PrimaryBlue,
                Location = new Point(760, 10),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lnkBrand.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://pexoris.com/tools/context-menu-editor/") { UseShellExecute = true }); } catch { }
            };

            pnlFooter.Controls.AddRange(new Control[] { chkAutoNotify, lblFooterStatus, lnkBrand });

            // Add all controls to form
            this.Controls.AddRange(new Control[] {
                pnlTitleBar, cardTop, lblSection, txtSearch, pnlListWrapper, pnlActions, pnlFooter
            });

            this.ResumeLayout(false);
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void RefreshMenuStatus()
        {
            bool isClassic = ContextMenuHelper.IsClassicMenuEnabled();
            if (isClassic)
            {
                pillStatus.Text = "● WIN10 CLASSIC MENU ACTIVE";
                pillStatus.ForeColor = Theme.SuccessGreen;
                pillStatus.BackColor = Theme.SuccessLight;
            }
            else
            {
                pillStatus.Text = "● WIN11 MODERN MENU ACTIVE";
                pillStatus.ForeColor = Theme.PrimaryBlue;
                pillStatus.BackColor = Theme.PrimaryLight;
            }
            pillStatus.Invalidate();
        }

        private void ScanItems()
        {
            try
            {
                allItems = ContextMenuHelper.ScanContextMenuItems();
                FilterList();
                lblFooterStatus.Text = string.Format("Found {0} shell context menu handlers registered.", allItems.Count);
                RefreshMenuStatus();
            }
            catch (Exception ex)
            {
                lblFooterStatus.Text = "Scan error: " + ex.Message;
            }
        }

        private void FilterList()
        {
            lvItems.BeginUpdate();
            lvItems.Items.Clear();

            string query = (txtSearch.Text == "Search handlers...") ? "" : txtSearch.Text.Trim().ToLowerInvariant();

            foreach (var item in allItems)
            {
                if (!string.IsNullOrEmpty(query))
                {
                    if (!item.Name.ToLowerInvariant().Contains(query) &&
                        !item.Scope.ToLowerInvariant().Contains(query) &&
                        !item.ValueData.ToLowerInvariant().Contains(query))
                        continue;
                }

                ListViewItem lvi = new ListViewItem(item.Name);
                lvi.SubItems.Add(item.Scope);
                lvi.SubItems.Add(item.ValueData);
                lvi.SubItems.Add(item.DisplayStatus);
                lvi.Tag = item;

                if (!item.IsEnabled)
                {
                    lvi.ForeColor = Theme.TextSubtle;
                    lvi.BackColor = Color.FromArgb(254, 242, 242);
                }
                else
                {
                    lvi.ForeColor = Theme.TextHero;
                    lvi.BackColor = Theme.BgCard;
                }

                lvItems.Items.Add(lvi);
            }

            lvItems.EndUpdate();
            UpdateButtonsState();
        }

        private void LvItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonsState();
        }

        private void UpdateButtonsState()
        {
            bool hasSelection = lvItems.SelectedItems.Count > 0;
            btnToggle.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection;

            if (hasSelection)
            {
                ContextMenuItem item = lvItems.SelectedItems[0].Tag as ContextMenuItem;
                if (item != null)
                {
                    if (item.IsEnabled)
                    {
                        btnToggle.Text = "⚡ Disable Selected";
                        btnToggle.StyleType = ButtonStyleType.DestructiveRed;
                    }
                    else
                    {
                        btnToggle.Text = "✓ Enable Selected";
                        btnToggle.StyleType = ButtonStyleType.SuccessGreen;
                    }
                }
            }
            else
            {
                btnToggle.Text = "⚡ Toggle Selected Item";
                btnToggle.StyleType = ButtonStyleType.SecondaryOutline;
            }
        }

        private void BtnToggle_Click(object sender, EventArgs e)
        {
            if (lvItems.SelectedItems.Count == 0) return;
            ContextMenuItem item = lvItems.SelectedItems[0].Tag as ContextMenuItem;
            if (item == null) return;

            bool success = ContextMenuHelper.ToggleItem(item);
            if (success)
            {
                lblFooterStatus.Text = string.Format("Item '{0}' set to {1}.", item.Name, item.DisplayStatus);
                FilterList();
            }
            else
            {
                MessageBox.Show("Failed to modify registry permissions for " + item.Name, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lvItems.SelectedItems.Count == 0) return;
            ContextMenuItem item = lvItems.SelectedItems[0].Tag as ContextMenuItem;
            if (item == null) return;

            DialogResult dr = MessageBox.Show(
                string.Format("Are you sure you want to permanently delete context menu handler '{0}'?", item.Name),
                "Confirm Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                bool success = ContextMenuHelper.DeleteItem(item);
                if (success)
                {
                    allItems.Remove(item);
                    FilterList();
                    lblFooterStatus.Text = string.Format("Item '{0}' removed permanently.", item.Name);
                }
            }
        }

        private void BtnPresetClassic_Click(object sender, EventArgs e)
        {
            bool ok = ContextMenuHelper.EnableClassicMenu();
            if (ok)
            {
                RefreshMenuStatus();
                DialogResult dr = MessageBox.Show(
                    "Windows 10 Classic Context Menu has been enabled!\n\nWould you like to restart Windows Explorer now to apply changes immediately?",
                    "Pexoris ContextMenuEditor",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (dr == DialogResult.Yes)
                {
                    ContextMenuHelper.RestartExplorer();
                }
                lblFooterStatus.Text = "Windows 10 Classic Context Menu enabled.";
            }
        }

        private void BtnPresetModern_Click(object sender, EventArgs e)
        {
            bool ok = ContextMenuHelper.RestoreModernMenu();
            if (ok)
            {
                RefreshMenuStatus();
                DialogResult dr = MessageBox.Show(
                    "Windows 11 Modern Context Menu has been restored!\n\nWould you like to restart Windows Explorer now to apply changes immediately?",
                    "Pexoris ContextMenuEditor",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (dr == DialogResult.Yes)
                {
                    ContextMenuHelper.RestartExplorer();
                }
                lblFooterStatus.Text = "Windows 11 Modern Context Menu restored.";
            }
        }
    }
}
