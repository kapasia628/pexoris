using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Pexoris.DoHSwitcher
{
    public static class PexorisTheme
    {
        // Apple & macOS Light Mode Color Tokens
        public static readonly Color AppleBlue = Color.FromArgb(0, 122, 255);
        public static readonly Color AppleBlueHover = Color.FromArgb(0, 102, 224);
        public static readonly Color AppleBlueLight = Color.FromArgb(239, 246, 255);

        public static readonly Color AppleCyan = Color.FromArgb(0, 198, 255);
        public static readonly Color AppleCyanLight = Color.FromArgb(240, 253, 255);

        public static readonly Color AppleRed = Color.FromArgb(239, 68, 68);
        public static readonly Color AppleRedHover = Color.FromArgb(220, 38, 38);
        public static readonly Color AppleRedLight = Color.FromArgb(254, 242, 242);

        public static readonly Color AppleGreen = Color.FromArgb(16, 185, 129);
        public static readonly Color AppleGreenLight = Color.FromArgb(240, 253, 244);

        public static readonly Color AppleAmber = Color.FromArgb(245, 158, 11);
        public static readonly Color AppleAmberLight = Color.FromArgb(254, 243, 199);

        public static readonly Color AppleCanvas = Color.FromArgb(246, 248, 250);
        public static readonly Color AppleWhite = Color.FromArgb(255, 255, 255);
        public static readonly Color AppleCardHover = Color.FromArgb(248, 250, 252);

        public static readonly Color BorderLight = Color.FromArgb(226, 232, 240);
        public static readonly Color BorderMedium = Color.FromArgb(203, 213, 225);

        public static readonly Color TextPrimary = Color.FromArgb(29, 29, 31);
        public static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);

        public static Font FontHeader(float size = 10f) { return new Font("Segoe UI", size, FontStyle.Bold); }
        public static Font FontBody(float size = 9f) { return new Font("Segoe UI", size, FontStyle.Regular); }
        public static Font FontBold(float size = 9f) { return new Font("Segoe UI", size, FontStyle.Bold); }
        public static Font FontCode(float size = 9f) { return new Font("Consolas", size, FontStyle.Regular); }

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

        public static Bitmap GetDoHSwitcherLogoBitmap(int size = 24)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                Rectangle rect = new Rectangle(1, 1, size - 2, size - 2);
                using (GraphicsPath p = GetRoundedPath(rect, size * 0.28f))
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(
                        new Point(0, 0), new Point(0, size),
                        Color.FromArgb(0, 102, 255),
                        Color.FromArgb(0, 198, 255)))
                    {
                        g.FillPath(b, p);
                    }
                }

                // Inner white shield
                GraphicsPath shield = new GraphicsPath();
                float midX = size / 2f;
                float topY = size * 0.25f;
                float bottomY = size * 0.82f;
                float width = size * 0.52f;

                shield.AddLine(midX, topY, midX + width / 2f, topY + size * 0.12f);
                shield.AddLine(midX + width / 2f, topY + size * 0.12f, midX + width / 2f, topY + size * 0.35f);
                shield.AddBezier(midX + width / 2f, topY + size * 0.35f, midX + width / 2f, bottomY - size * 0.1f, midX, bottomY, midX, bottomY);
                shield.AddBezier(midX, bottomY, midX - width / 2f, bottomY - size * 0.1f, midX - width / 2f, topY + size * 0.35f, midX - width / 2f, topY + size * 0.35f);
                shield.AddLine(midX - width / 2f, topY + size * 0.35f, midX - width / 2f, topY + size * 0.12f);
                shield.CloseFigure();

                using (SolidBrush sb = new SolidBrush(Color.White))
                {
                    g.FillPath(sb, shield);
                }

                // Tiny central lock dot
                using (SolidBrush db = new SolidBrush(Color.FromArgb(0, 102, 255)))
                {
                    g.FillEllipse(db, midX - 2.5f, size * 0.44f, 5f, 5f);
                }
            }
            return bmp;
        }
    }

    public class PexorisButton : Button
    {
        public enum ButtonStyleType
        {
            PrimaryBlue,
            DestructiveRed,
            SecondaryOutline,
            SuccessGreen,
            AmberWarning
        }

        private ButtonStyleType _styleType = ButtonStyleType.SecondaryOutline;
        private bool _isHovered = false;
        private bool _isPressed = false;

        public ButtonStyleType StyleType
        {
            get { return _styleType; }
            set { _styleType = value; Invalidate(); }
        }

        public PexorisButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.Font = PexorisTheme.FontBold(9f);
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _isHovered = false; _isPressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs mevent) { base.OnMouseDown(mevent); _isPressed = true; Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs mevent) { base.OnMouseUp(mevent); _isPressed = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color parentBg = this.Parent != null ? this.Parent.BackColor : PexorisTheme.AppleCanvas;
            using (SolidBrush pb = new SolidBrush(parentBg))
            {
                g.FillRectangle(pb, this.ClientRectangle);
            }

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (GraphicsPath path = PexorisTheme.GetRoundedPath(rect, 8f))
            {
                Color bg = Color.White;
                Color border = PexorisTheme.BorderMedium;
                Color fg = PexorisTheme.TextPrimary;

                if (!this.Enabled)
                {
                    bg = Color.FromArgb(243, 244, 246);
                    border = Color.FromArgb(229, 231, 235);
                    fg = Color.FromArgb(156, 163, 175);
                }
                else
                {
                    switch (_styleType)
                    {
                        case ButtonStyleType.DestructiveRed:
                            bg = _isPressed ? Color.FromArgb(185, 28, 28) : (_isHovered ? PexorisTheme.AppleRedHover : PexorisTheme.AppleRed);
                            border = bg;
                            fg = Color.White;
                            break;

                        case ButtonStyleType.PrimaryBlue:
                            bg = _isPressed ? Color.FromArgb(0, 80, 180) : (_isHovered ? PexorisTheme.AppleBlueHover : PexorisTheme.AppleBlue);
                            border = bg;
                            fg = Color.White;
                            break;

                        case ButtonStyleType.SuccessGreen:
                            bg = _isPressed ? Color.FromArgb(4, 120, 87) : (_isHovered ? Color.FromArgb(5, 150, 105) : PexorisTheme.AppleGreen);
                            border = bg;
                            fg = Color.White;
                            break;

                        case ButtonStyleType.SecondaryOutline:
                            bg = _isPressed ? Color.FromArgb(241, 245, 249) : (_isHovered ? Color.FromArgb(248, 250, 252) : Color.White);
                            border = _isHovered ? PexorisTheme.AppleBlue : PexorisTheme.BorderMedium;
                            fg = _isHovered ? PexorisTheme.AppleBlue : PexorisTheme.TextPrimary;
                            break;

                        case ButtonStyleType.AmberWarning:
                            bg = _isPressed ? Color.FromArgb(217, 119, 6) : (_isHovered ? Color.FromArgb(245, 158, 11) : PexorisTheme.AppleAmberLight);
                            border = PexorisTheme.AppleAmber;
                            fg = Color.FromArgb(146, 64, 14);
                            break;
                    }
                }

                using (SolidBrush b = new SolidBrush(bg)) g.FillPath(b, path);
                using (Pen p = new Pen(border, 1f)) g.DrawPath(p, path);

                TextRenderer.DrawText(g, this.Text, this.Font, this.ClientRectangle, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }
}
