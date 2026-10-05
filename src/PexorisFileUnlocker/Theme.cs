using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Pexoris.Core
{
    public static class PexorisTheme
    {
        // ==========================================
        // APPLE macOS DESIGN SYSTEM FOR PEXORIS
        // ==========================================
        
        // Base Surfaces (macOS Light Theme)
        public static readonly Color AppleCanvas    = Color.FromArgb(246, 247, 249);   // #F6F7F9 Soft macOS Canvas
        public static readonly Color AppleWhite     = Color.FromArgb(255, 255, 255);   // #FFFFFF Crisp Card Surface
        public static readonly Color AppleCardHover = Color.FromArgb(249, 250, 251);   // #F9FAFB Subtle Hover
        public static readonly Color AppleInput     = Color.FromArgb(255, 255, 255);   // #FFFFFF Input Field
        
        // Crisp Borders (Apple 1px Clean Dividers)
        public static readonly Color BorderLight    = Color.FromArgb(229, 231, 235);   // #E5E7EB Subtle Card Border
        public static readonly Color BorderMedium   = Color.FromArgb(209, 213, 219);   // #D1D5DB Button & Input Border
        public static readonly Color BorderActive   = Color.FromArgb(0, 122, 255);     // #007AFF Apple Blue Focus

        // Apple System Accent Colors
        public static readonly Color AppleBlue      = Color.FromArgb(0, 122, 255);     // #007AFF Apple System Blue
        public static readonly Color AppleBlueHover = Color.FromArgb(0, 102, 220);     // #0066DC
        public static readonly Color AppleBlueLight = Color.FromArgb(239, 246, 255);   // #EFF6FF Soft Blue Pill

        public static readonly Color AppleRed       = Color.FromArgb(255, 59, 48);     // #FF3B30 Apple System Red
        public static readonly Color AppleRedHover  = Color.FromArgb(220, 38, 38);     // #DC2626
        public static readonly Color AppleRedLight  = Color.FromArgb(254, 242, 242);   // #FEF2F2 Soft Red Pill

        public static readonly Color AppleGreen     = Color.FromArgb(52, 199, 89);     // #34C759 Apple System Green
        public static readonly Color AppleGreenLight= Color.FromArgb(240, 253, 244);   // #F0FDF4 Soft Green Pill

        public static readonly Color AppleYellow    = Color.FromArgb(255, 204, 0);     // #FFCC00 Traffic Light
        public static readonly Color TrafficRed     = Color.FromArgb(255, 95, 87);     // #FF5F57 macOS Close Dot
        public static readonly Color TrafficYellow  = Color.FromArgb(254, 188, 46);    // #FEBC2E macOS Minimize Dot

        // Apple Typography Colors (Clean, Deep, Crystal Clear)
        public static readonly Color TextPrimary    = Color.FromArgb(29, 29, 31);      // #1D1D1F Apple Dark Slate
        public static readonly Color TextSecondary  = Color.FromArgb(107, 114, 128);   // #6B7280 Muted Gray
        public static readonly Color TextMuted      = Color.FromArgb(156, 163, 175);   // #9CA3AF Caption Gray

        // Typography Helpers (Apple SF Pro Style)
        public static Font FontHeader(float size = 11f)
        {
            return GetFont(size, FontStyle.Bold);
        }

        public static Font FontBody(float size = 9.5f)
        {
            return GetFont(size, FontStyle.Regular);
        }

        public static Font FontBold(float size = 9.5f)
        {
            return GetFont(size, FontStyle.Bold);
        }

        public static Font FontCode(float size = 9f)
        {
            try { return new Font("SF Mono", size, FontStyle.Regular); }
            catch { }
            try { return new Font("Consolas", size, FontStyle.Regular); }
            catch { return new Font(FontFamily.GenericMonospace, size); }
        }

        private static Font GetFont(float size, FontStyle style)
        {
            string[] preferred = new string[] { "SF Pro Text", "Segoe UI Variable Text", "Segoe UI", "Helvetica Neue", "Arial" };
            foreach (string name in preferred)
            {
                try
                {
                    using (Font test = new Font(name, size, style))
                    {
                        if (test.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                            return new Font(name, size, style);
                    }
                }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, size, style);
        }

        public static GraphicsPath GetRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Renders the official Pexoris Shield & Padlock Brand Logo as a high-resolution Bitmap.
        /// </summary>
        public static Bitmap GetBrandLogoBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                float s = size;
                float pad = s * 0.06f;

                // 1. Vibrant Squircle Shield
                RectangleF badgeRect = new RectangleF(pad, pad, s - pad * 2, s - pad * 2);
                using (GraphicsPath badgePath = GetRoundedPath(badgeRect, s * 0.24f))
                {
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                        badgeRect,
                        Color.FromArgb(255, 0, 195, 255),
                        Color.FromArgb(255, 0, 105, 225),
                        LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(bgBrush, badgePath);
                    }

                    using (Pen innerPen = new Pen(Color.FromArgb(180, 255, 255, 255), Math.Max(1f, s * 0.04f)))
                    {
                        innerPen.Alignment = PenAlignment.Inset;
                        g.DrawPath(innerPen, badgePath);
                    }
                }

                // 2. Open Lock Shackle
                float shackleW = s * 0.28f;
                float shackleLeft = s * 0.32f;
                float shackleTop = s * 0.20f;
                float shackleThick = Math.Max(1.8f, s * 0.085f);

                using (Pen shacklePen = new Pen(Color.FromArgb(255, 255, 255, 255), shackleThick))
                {
                    shacklePen.StartCap = LineCap.Round;
                    shacklePen.EndCap = LineCap.Round;

                    GraphicsPath sp = new GraphicsPath();
                    sp.AddLine(shackleLeft, s * 0.52f, shackleLeft, shackleTop + (shackleW / 2f));
                    sp.AddArc(shackleLeft, shackleTop, shackleW, shackleW, 180, 180);
                    sp.AddLine(shackleLeft + shackleW, shackleTop + (shackleW / 2f), shackleLeft + shackleW, shackleTop + (shackleW * 0.85f));
                    g.DrawPath(shacklePen, sp);
                }

                // 3. Lock Body
                float bodyW = s * 0.46f;
                float bodyH = s * 0.35f;
                float bodyX = (s - bodyW) / 2f;
                float bodyY = s * 0.48f;
                RectangleF bodyRect = new RectangleF(bodyX, bodyY, bodyW, bodyH);

                using (GraphicsPath bodyPath = GetRoundedPath(bodyRect, s * 0.09f))
                {
                    using (LinearGradientBrush bodyBrush = new LinearGradientBrush(
                        bodyRect,
                        Color.FromArgb(255, 255, 255, 255),
                        Color.FromArgb(255, 230, 238, 248),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(bodyBrush, bodyPath);
                    }
                }

                // 4. Cyan Keyhole
                float khY = bodyY + bodyH * 0.40f;
                float khR = s * 0.055f;
                float khX = s / 2f;
                using (SolidBrush khBrush = new SolidBrush(Color.FromArgb(255, 0, 110, 220)))
                {
                    g.FillEllipse(khBrush, khX - khR, khY - khR, khR * 2, khR * 2);
                    PointF[] slot = new PointF[] {
                        new PointF(khX - (khR * 0.45f), khY),
                        new PointF(khX + (khR * 0.45f), khY),
                        new PointF(khX + (khR * 0.75f), khY + (khR * 1.5f)),
                        new PointF(khX - (khR * 0.75f), khY + (khR * 1.5f))
                    };
                    g.FillPolygon(khBrush, slot);
                }
            }
            return bmp;
        }
    }

    // ==========================================
    // APPLE macOS STYLE BUTTON (NO BLACK CORNERS!)
    // ==========================================
    public class PexorisButton : Button
    {
        public enum ButtonStyleType
        {
            PrimaryBlue,
            DestructiveRed,
            SecondaryOutline,
            SuccessGreen
        }

        public ButtonStyleType StyleType { get; set; }
        public float CornerRadius { get; set; }

        private bool _isHovered = false;
        private bool _isPressed = false;

        public PexorisButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            StyleType = ButtonStyleType.PrimaryBlue;
            CornerRadius = 7f; // Apple macOS button curvature
            Font = PexorisTheme.FontBold(9.5f);
            Height = 36;
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs mevent) { _isPressed = true; Invalidate(); base.OnMouseDown(mevent); }
        protected override void OnMouseUp(MouseEventArgs mevent) { _isPressed = false; Invalidate(); base.OnMouseUp(mevent); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle client = ClientRectangle;
            RectangleF rect = new RectangleF(0.5f, 0.5f, client.Width - 1f, client.Height - 1f);

            // 1. CRITICAL: Paint parent container background first to COMPLETELY PREVENT BLACK CORNER ARTIFACTS!
            Color parentBg = (Parent != null && Parent.BackColor != Color.Transparent) ? Parent.BackColor : PexorisTheme.AppleCanvas;
            using (SolidBrush parentBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(parentBrush, client);
            }

            Color bgColor, fgColor, borderColor;

            if (!Enabled)
            {
                bgColor = Color.FromArgb(243, 244, 246);
                fgColor = PexorisTheme.TextMuted;
                borderColor = PexorisTheme.BorderLight;
            }
            else
            {
                switch (StyleType)
                {
                    case ButtonStyleType.DestructiveRed:
                        bgColor = _isPressed ? PexorisTheme.AppleRedHover : (_isHovered ? Color.FromArgb(248, 113, 113) : PexorisTheme.AppleRed);
                        fgColor = Color.White;
                        borderColor = _isPressed ? PexorisTheme.AppleRedHover : PexorisTheme.AppleRed;
                        break;

                    case ButtonStyleType.SuccessGreen:
                        bgColor = _isPressed ? Color.FromArgb(22, 163, 74) : (_isHovered ? Color.FromArgb(74, 222, 128) : PexorisTheme.AppleGreen);
                        fgColor = Color.White;
                        borderColor = bgColor;
                        break;

                    case ButtonStyleType.SecondaryOutline:
                        // Apple macOS Finder Secondary Button (White surface, crisp 1px gray border)
                        bgColor = _isPressed ? Color.FromArgb(229, 231, 235) : (_isHovered ? Color.FromArgb(249, 250, 251) : Color.White);
                        fgColor = PexorisTheme.TextPrimary;
                        borderColor = _isHovered ? PexorisTheme.AppleBlue : PexorisTheme.BorderMedium;
                        break;

                    case ButtonStyleType.PrimaryBlue:
                    default:
                        // Apple macOS Primary Button (Apple Blue, White text)
                        bgColor = _isPressed ? PexorisTheme.AppleBlueHover : (_isHovered ? Color.FromArgb(59, 130, 246) : PexorisTheme.AppleBlue);
                        fgColor = Color.White;
                        borderColor = _isPressed ? PexorisTheme.AppleBlueHover : PexorisTheme.AppleBlue;
                        break;
                }
            }

            // 2. Draw Apple Rounded Shape with Crisp 1px Border
            using (GraphicsPath path = PexorisTheme.GetRoundedPath(rect, CornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(borderColor, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 3. Draw Text (NoPrefix ensures '&' is drawn cleanly, not as '_')
            TextRenderer.DrawText(g, Text, Font, client, fgColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.WordEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    // ==========================================
    // APPLE macOS DIALOG (INPUT PROMPT)
    // ==========================================
    public static class PexorisDialogs
    {
        public static string ShowInput(string prompt, string title, string defaultValue = "")
        {
            using (Form promptForm = new Form())
            {
                promptForm.Width = 460;
                promptForm.Height = 190;
                promptForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                promptForm.Text = title;
                promptForm.StartPosition = FormStartPosition.CenterParent;
                promptForm.BackColor = PexorisTheme.AppleWhite;
                promptForm.ForeColor = PexorisTheme.TextPrimary;
                promptForm.MaximizeBox = false;
                promptForm.MinimizeBox = false;

                Label textLabel = new Label()
                {
                    Left = 24, Top = 18, Width = 400, Height = 25,
                    Text = prompt,
                    Font = PexorisTheme.FontBold(9.5f),
                    ForeColor = PexorisTheme.TextPrimary
                };

                TextBox textBox = new TextBox()
                {
                    Left = 24, Top = 48, Width = 396, Height = 28,
                    Text = defaultValue,
                    BackColor = PexorisTheme.AppleWhite,
                    ForeColor = PexorisTheme.TextPrimary,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = PexorisTheme.FontBody(10f)
                };

                PexorisButton confirmation = new PexorisButton()
                {
                    Text = "OK",
                    Left = 226, Width = 92, Top = 94, Height = 34,
                    DialogResult = DialogResult.OK,
                    StyleType = PexorisButton.ButtonStyleType.PrimaryBlue
                };

                PexorisButton cancel = new PexorisButton()
                {
                    Text = "Cancel",
                    Left = 328, Width = 92, Top = 94, Height = 34,
                    DialogResult = DialogResult.Cancel,
                    StyleType = PexorisButton.ButtonStyleType.SecondaryOutline
                };

                promptForm.Controls.Add(textLabel);
                promptForm.Controls.Add(textBox);
                promptForm.Controls.Add(confirmation);
                promptForm.Controls.Add(cancel);
                promptForm.AcceptButton = confirmation;
                promptForm.CancelButton = cancel;

                return promptForm.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : "";
            }
        }
    }
}
