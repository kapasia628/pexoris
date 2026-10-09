using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PexorisDuplicateFinder
{
    public enum PexorisButtonStyle
    {
        PrimaryIndigo,
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

        // Accents - Modern Indigo Palette
        public static readonly Color Indigo = Color.FromArgb(79, 70, 229);         // #4F46E5
        public static readonly Color IndigoHover = Color.FromArgb(67, 56, 202);    // #4338CA
        public static readonly Color IndigoDark = Color.FromArgb(55, 48, 163);     // #3730A3
        public static readonly Color IndigoLight = Color.FromArgb(238, 242, 255);  // #EEF2FF
        public static readonly Color IndigoBorder = Color.FromArgb(199, 210, 254); // #C7D2FE

        public static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);    // #2563EB
        public static readonly Color PrimaryBlueHover = Color.FromArgb(29, 78, 216);
        public static readonly Color SuccessGreen = Color.FromArgb(16, 185, 129);   // #10B981
        public static readonly Color SuccessGreenHover = Color.FromArgb(5, 150, 105);
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

            RectangleF rect = new RectangleF(0, 0, Width - 1, Height - 1);

            Color bg;
            Color textCol;
            Color borderCol;

            switch (Style)
            {
                case PexorisButtonStyle.PrimaryIndigo:
                    bg = _isPressed ? Theme.IndigoDark : (_isHovered ? Theme.IndigoHover : Theme.Indigo);
                    textCol = Color.White;
                    borderCol = bg;
                    break;
                case PexorisButtonStyle.PrimaryBlue:
                    bg = _isPressed ? Color.FromArgb(29, 78, 216) : (_isHovered ? Theme.PrimaryBlueHover : Theme.PrimaryBlue);
                    textCol = Color.White;
                    borderCol = bg;
                    break;
                case PexorisButtonStyle.SuccessGreen:
                    bg = _isPressed ? Color.FromArgb(4, 120, 87) : (_isHovered ? Theme.SuccessGreenHover : Theme.SuccessGreen);
                    textCol = Color.White;
                    borderCol = bg;
                    break;
                case PexorisButtonStyle.DestructiveRed:
                    bg = _isPressed ? Color.FromArgb(153, 27, 27) : (_isHovered ? Theme.DestructiveRedHover : Theme.DestructiveRed);
                    textCol = Color.White;
                    borderCol = bg;
                    break;
                default: // SecondaryOutline
                    bg = _isPressed ? Color.FromArgb(226, 232, 240) : (_isHovered ? Color.FromArgb(241, 245, 249) : Color.White);
                    textCol = Theme.TextBody;
                    borderCol = _isHovered ? Theme.BorderMedium : Theme.BorderLight;
                    break;
            }

            using (GraphicsPath path = Theme.GetRoundedPath(rect, CornerRadius))
            {
                using (SolidBrush b = new SolidBrush(bg))
                {
                    g.FillPath(b, path);
                }
                using (Pen p = new Pen(borderCol, 1f))
                {
                    g.DrawPath(p, path);
                }
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, textCol,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}
