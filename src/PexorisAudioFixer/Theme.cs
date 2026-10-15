using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PexorisAudioFixer
{
    public enum PexorisButtonStyle
    {
        PrimaryIndigo,
        SecondaryCyan,
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

        // Accents - Audio Sonic Indigo & Soundwave Violet Palette
        public static readonly Color PrimaryIndigo = Color.FromArgb(79, 70, 229);    // #4F46E5
        public static readonly Color PrimaryIndigoHover = Color.FromArgb(67, 56, 202); // #4338CA
        public static readonly Color PrimaryIndigoDark = Color.FromArgb(49, 46, 129);  // #312E81
        public static readonly Color PrimaryIndigoLight = Color.FromArgb(238, 242, 255); // #EEF2FF
        public static readonly Color PrimaryIndigoBorder = Color.FromArgb(199, 210, 254); // #C7D2FE

        public static readonly Color Cyan = Color.FromArgb(2, 132, 199);             // #0284C7
        public static readonly Color CyanHover = Color.FromArgb(3, 105, 161);
        public static readonly Color CyanLight = Color.FromArgb(240, 249, 255);

        public static readonly Color SuccessGreen = Color.FromArgb(16, 185, 129);   // #10B981
        public static readonly Color SuccessGreenHover = Color.FromArgb(5, 150, 105);
        public static readonly Color SuccessGreenLight = Color.FromArgb(236, 253, 245);

        public static readonly Color DestructiveRed = Color.FromArgb(220, 38, 38); // #DC2626
        public static readonly Color DestructiveRedHover = Color.FromArgb(185, 28, 28);
        public static readonly Color DestructiveRedLight = Color.FromArgb(254, 242, 242);

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
            if (_isPressed)
            {
                _isPressed = false;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            Color fill;
            Color border;
            Color text;

            switch (Style)
            {
                case PexorisButtonStyle.DestructiveRed:
                    fill = _isPressed ? Theme.DestructiveRedHover : (_isHovered ? Theme.DestructiveRedHover : Theme.DestructiveRed);
                    border = fill;
                    text = Color.White;
                    break;

                case PexorisButtonStyle.PrimaryIndigo:
                    fill = _isPressed ? Theme.PrimaryIndigoHover : (_isHovered ? Theme.PrimaryIndigoHover : Theme.PrimaryIndigo);
                    border = fill;
                    text = Color.White;
                    break;

                case PexorisButtonStyle.SecondaryCyan:
                    fill = _isPressed ? Theme.CyanHover : (_isHovered ? Theme.CyanHover : Theme.Cyan);
                    border = fill;
                    text = Color.White;
                    break;

                case PexorisButtonStyle.SuccessGreen:
                    fill = _isPressed ? Theme.SuccessGreenHover : (_isHovered ? Theme.SuccessGreenHover : Theme.SuccessGreen);
                    border = fill;
                    text = Color.White;
                    break;

                case PexorisButtonStyle.SecondaryOutline:
                default:
                    fill = _isPressed ? Color.FromArgb(241, 245, 249) : (_isHovered ? Color.FromArgb(248, 250, 252) : Color.White);
                    border = _isHovered ? Theme.BorderMedium : Theme.BorderLight;
                    text = Theme.TextBody;
                    break;
            }

            using (GraphicsPath path = Theme.GetRoundedPath(rect, CornerRadius))
            {
                using (SolidBrush b = new SolidBrush(fill))
                {
                    g.FillPath(b, path);
                }

                using (Pen p = new Pen(border, 1f))
                {
                    g.DrawPath(p, path);
                }
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}
