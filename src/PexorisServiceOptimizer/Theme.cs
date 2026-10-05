using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PexorisServiceOptimizer
{
    public enum PexorisButtonStyle
    {
        PrimaryBlue,
        SuccessGreen,
        DestructiveRed,
        SecondaryOutline,
        GamerPurple,
        WarningAmber
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

        // Accents
        public static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);    // #2563EB
        public static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);   // #1D4ED8
        public static readonly Color SuccessGreen = Color.FromArgb(16, 185, 129);  // #10B981
        public static readonly Color DestructiveRed = Color.FromArgb(220, 38, 38); // #DC2626
        public static readonly Color GamerPurple = Color.FromArgb(124, 58, 237);   // #7C3AED
        public static readonly Color GamerHover = Color.FromArgb(109, 40, 217);    // #6D28D9
        public static readonly Color WarningAmber = Color.FromArgb(217, 119, 6);   // #D97706

        public static Font FontHeadline = new Font("Segoe UI", 12f, FontStyle.Bold);
        public static Font FontSub = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        public static Font FontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static Font FontRegular = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static Font FontSmall = new Font("Segoe UI", 8.25f, FontStyle.Regular);
        public static Font FontMono = new Font("Consolas", 8.75f, FontStyle.Regular);

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

    public class PexorisButton : Button
    {
        public PexorisButtonStyle Style { get; set; }
        public float CornerRadius { get; set; }

        private bool _isHovered = false;
        private bool _isPressed = false;

        public PexorisButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = Theme.FontBold;
            CornerRadius = 7f;
            Cursor = Cursors.Hand;
            Style = PexorisButtonStyle.SecondaryOutline;
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

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            _isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle client = ClientRectangle;
            g.Clear(Parent != null ? Parent.BackColor : Theme.BgCanvas);

            RectangleF rect = new RectangleF(0.5f, 0.5f, client.Width - 1f, client.Height - 1f);

            Color bgColor, textColor, borderColor;

            switch (Style)
            {
                case PexorisButtonStyle.PrimaryBlue:
                    bgColor = _isPressed ? Color.FromArgb(30, 64, 175) : (_isHovered ? Theme.PrimaryHover : Theme.PrimaryBlue);
                    textColor = Color.White;
                    borderColor = bgColor;
                    break;

                case PexorisButtonStyle.SuccessGreen:
                    bgColor = _isPressed ? Color.FromArgb(4, 120, 87) : (_isHovered ? Color.FromArgb(5, 150, 105) : Theme.SuccessGreen);
                    textColor = Color.White;
                    borderColor = bgColor;
                    break;

                case PexorisButtonStyle.DestructiveRed:
                    bgColor = _isPressed ? Color.FromArgb(185, 28, 28) : (_isHovered ? Color.FromArgb(239, 68, 68) : Theme.DestructiveRed);
                    textColor = Color.White;
                    borderColor = bgColor;
                    break;

                case PexorisButtonStyle.GamerPurple:
                    bgColor = _isPressed ? Color.FromArgb(91, 33, 182) : (_isHovered ? Theme.GamerHover : Theme.GamerPurple);
                    textColor = Color.White;
                    borderColor = bgColor;
                    break;

                case PexorisButtonStyle.WarningAmber:
                    bgColor = _isPressed ? Color.FromArgb(180, 83, 9) : (_isHovered ? Color.FromArgb(245, 158, 11) : Theme.WarningAmber);
                    textColor = Color.White;
                    borderColor = bgColor;
                    break;

                case PexorisButtonStyle.SecondaryOutline:
                default:
                    bgColor = _isPressed ? Color.FromArgb(226, 232, 240) : (_isHovered ? Color.FromArgb(241, 245, 249) : Color.White);
                    textColor = _isHovered ? Theme.PrimaryBlue : Theme.TextHero;
                    borderColor = _isHovered ? Theme.PrimaryBlue : Theme.BorderMedium;
                    break;
            }

            if (!Enabled)
            {
                bgColor = Color.FromArgb(241, 245, 249);
                textColor = Color.FromArgb(148, 163, 184);
                borderColor = Color.FromArgb(226, 232, 240);
            }

            using (GraphicsPath path = Theme.GetRoundedPath(rect, CornerRadius))
            {
                using (SolidBrush bgBrush = new SolidBrush(bgColor))
                {
                    g.FillPath(bgBrush, path);
                }

                using (Pen borderPen = new Pen(borderColor, 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            TextRenderer.DrawText(g, Text, Font, client, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}
