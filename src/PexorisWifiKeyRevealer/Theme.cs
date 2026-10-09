using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PexorisWifiKeyRevealer
{
    public enum PexorisButtonStyle
    {
        PrimaryTeal,
        PrimaryBlue,
        SuccessGreen,
        DestructiveRed,
        SecondaryOutline
    }

    public static class Theme
    {
        public static readonly Color BgCanvas = Color.FromArgb(248, 250, 252);     // #F8FAFC
        public static readonly Color CardBg = Color.FromArgb(255, 255, 255);       // #FFFFFF
        public static readonly Color BorderLight = Color.FromArgb(226, 232, 240);  // #E2E8F0
        public static readonly Color BorderMedium = Color.FromArgb(203, 213, 225); // #CBD5E1
        public static readonly Color TextHero = Color.FromArgb(15, 23, 42);        // #0F172A
        public static readonly Color TextBody = Color.FromArgb(51, 65, 85);        // #334155
        public static readonly Color TextSub = Color.FromArgb(100, 116, 139);      // #64748B
        public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);    // #94A3B8

        // Accents - Modern Teal / Cyan Palette
        public static readonly Color Teal = Color.FromArgb(13, 148, 136);          // #0D9488 Teal 600
        public static readonly Color TealHover = Color.FromArgb(15, 118, 110);     // #0F766E Teal 700
        public static readonly Color TealDark = Color.FromArgb(17, 94, 89);        // #115E59 Teal 800
        public static readonly Color TealLight = Color.FromArgb(240, 253, 250);    // #F0FDFA Teal 50
        public static readonly Color TealBorder = Color.FromArgb(153, 246, 228);   // #99F6E4 Teal 200

        public static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);    // #2563EB
        public static readonly Color PrimaryBlueHover = Color.FromArgb(29, 78, 216);
        public static readonly Color SuccessGreen = Color.FromArgb(16, 185, 129);   // #10B981
        public static readonly Color DestructiveRed = Color.FromArgb(220, 38, 38); // #DC2626
        public static readonly Color DestructiveRedHover = Color.FromArgb(185, 28, 28);

        public static Font FontHeadline = new Font("Segoe UI", 12f, FontStyle.Bold);
        public static Font FontSub = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        public static Font FontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static Font FontRegular = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static Font FontSmall = new Font("Segoe UI", 8.25f, FontStyle.Regular);
        public static Font FontMono = new Font("Consolas", 9f, FontStyle.Regular);

        public static GraphicsPath GetRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (rect.Width <= 0 || rect.Height <= 0) return path;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class PexorisButton : Control
    {
        public PexorisButtonStyle Style { get; set; }
        public float CornerRadius { get; set; }

        private bool _isHovered;
        private bool _isPressed;

        public PexorisButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            Style = PexorisButtonStyle.SecondaryOutline;
            CornerRadius = 7f;
            Font = Theme.FontBold;
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            Color bg;
            Color border;
            Color text;

            switch (Style)
            {
                case PexorisButtonStyle.PrimaryTeal:
                    bg = _isPressed ? Theme.TealDark : (_isHovered ? Theme.TealHover : Theme.Teal);
                    border = bg;
                    text = Color.White;
                    break;

                case PexorisButtonStyle.PrimaryBlue:
                    bg = _isPressed ? Color.FromArgb(30, 64, 175) : (_isHovered ? Theme.PrimaryBlueHover : Theme.PrimaryBlue);
                    border = bg;
                    text = Color.White;
                    break;

                case PexorisButtonStyle.SuccessGreen:
                    bg = _isPressed ? Color.FromArgb(4, 120, 87) : (_isHovered ? Color.FromArgb(5, 150, 105) : Theme.SuccessGreen);
                    border = bg;
                    text = Color.White;
                    break;

                case PexorisButtonStyle.DestructiveRed:
                    bg = _isPressed ? Color.FromArgb(153, 27, 27) : (_isHovered ? Theme.DestructiveRedHover : Theme.DestructiveRed);
                    border = bg;
                    text = Color.White;
                    break;

                case PexorisButtonStyle.SecondaryOutline:
                default:
                    bg = _isPressed ? Color.FromArgb(241, 245, 249) : (_isHovered ? Color.FromArgb(248, 250, 252) : Color.White);
                    border = _isHovered ? Theme.BorderMedium : Theme.BorderLight;
                    text = Theme.TextHero;
                    break;
            }

            using (GraphicsPath path = Theme.GetRoundedPath(rect, CornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(bg))
                {
                    g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(border, 1.2f))
                {
                    g.DrawPath(pen, path);
                }
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
    }
}
