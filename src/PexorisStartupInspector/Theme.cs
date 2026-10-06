using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PexorisStartupInspector
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
        public static readonly Color PrimaryBlue = Color.FromArgb(79, 70, 229);    // #4F46E5 Indigo
        public static readonly Color PrimaryHover = Color.FromArgb(67, 56, 202);   // #4338CA
        public static readonly Color SuccessGreen = Color.FromArgb(16, 185, 129);  // #10B981
        public static readonly Color SuccessHover = Color.FromArgb(5, 150, 105);
        public static readonly Color DestructiveRed = Color.FromArgb(220, 38, 38); // #DC2626
        public static readonly Color DestructiveHover = Color.FromArgb(185, 28, 28);
        public static readonly Color GamerPurple = Color.FromArgb(147, 51, 234);   // #9333EA
        public static readonly Color WarningAmber = Color.FromArgb(217, 119, 6);   // #D97706
        public static readonly Color CyanAccent = Color.FromArgb(14, 165, 233);    // #0EA5E9 Sky 500

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

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle clientRect = ClientRectangle;
            if (clientRect.Width <= 0 || clientRect.Height <= 0) return;

            RectangleF rect = new RectangleF(0.5f, 0.5f, clientRect.Width - 1f, clientRect.Height - 1f);

            Color bg;
            Color fg;
            Color border;

            if (!Enabled)
            {
                bg = Color.FromArgb(241, 245, 249);
                fg = Color.FromArgb(148, 163, 184);
                border = Color.FromArgb(226, 232, 240);
            }
            else
            {
                switch (Style)
                {
                    case PexorisButtonStyle.PrimaryBlue:
                        bg = _isPressed ? Color.FromArgb(55, 48, 163) : (_isHovered ? Theme.PrimaryHover : Theme.PrimaryBlue);
                        fg = Color.White;
                        border = bg;
                        break;

                    case PexorisButtonStyle.SuccessGreen:
                        bg = _isPressed ? Color.FromArgb(4, 120, 87) : (_isHovered ? Theme.SuccessHover : Theme.SuccessGreen);
                        fg = Color.White;
                        border = bg;
                        break;

                    case PexorisButtonStyle.DestructiveRed:
                        bg = _isPressed ? Color.FromArgb(153, 27, 27) : (_isHovered ? Theme.DestructiveHover : Theme.DestructiveRed);
                        fg = Color.White;
                        border = bg;
                        break;

                    case PexorisButtonStyle.GamerPurple:
                        bg = _isPressed ? Color.FromArgb(107, 33, 168) : (_isHovered ? Color.FromArgb(126, 34, 206) : Theme.GamerPurple);
                        fg = Color.White;
                        border = bg;
                        break;

                    case PexorisButtonStyle.WarningAmber:
                        bg = _isPressed ? Color.FromArgb(180, 83, 9) : (_isHovered ? Color.FromArgb(180, 83, 9) : Theme.WarningAmber);
                        fg = Color.White;
                        border = bg;
                        break;

                    case PexorisButtonStyle.SecondaryOutline:
                    default:
                        bg = _isPressed ? Color.FromArgb(226, 232, 240) : (_isHovered ? Color.FromArgb(241, 245, 249) : Color.White);
                        fg = Theme.TextHero;
                        border = _isHovered ? Color.FromArgb(148, 163, 184) : Theme.BorderMedium;
                        break;
                }
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

            TextRenderer.DrawText(g, Text, Font, clientRect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}
