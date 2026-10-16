using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PexorisDiskUsage
{
    public static class Theme
    {
        public static readonly Color Primary = Color.FromArgb(13, 148, 136);       // High-Tech Teal #0D9488
        public static readonly Color PrimaryHover = Color.FromArgb(15, 118, 110);  // #0F766E
        public static readonly Color PrimaryActive = Color.FromArgb(17, 94, 89);   // #115E59
        public static readonly Color PrimaryLight = Color.FromArgb(240, 253, 250); // #F0FDFA

        public static readonly Color Destructive = Color.FromArgb(220, 38, 38);    // Red #DC2626
        public static readonly Color DestructiveHover = Color.FromArgb(185, 28, 28);
        public static readonly Color DestructiveLight = Color.FromArgb(254, 242, 242);

        public static readonly Color Warning = Color.FromArgb(217, 119, 6);        // Amber #D97706
        public static readonly Color WarningHover = Color.FromArgb(180, 83, 9);
        public static readonly Color WarningLight = Color.FromArgb(254, 243, 199);

        public static readonly Color Success = Color.FromArgb(16, 185, 129);       // Emerald #10B981
        public static readonly Color SuccessHover = Color.FromArgb(5, 150, 105);
        public static readonly Color SuccessLight = Color.FromArgb(236, 253, 245);

        public static readonly Color CanvasBg = Color.FromArgb(248, 250, 252);     // #F8FAFC
        public static readonly Color CardBg = Color.FromArgb(255, 255, 255);       // #FFFFFF
        public static readonly Color TextHero = Color.FromArgb(15, 23, 42);        // #0F172A
        public static readonly Color TextBody = Color.FromArgb(51, 65, 85);        // #334155
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);    // #64748B
        public static readonly Color BorderLight = Color.FromArgb(226, 232, 240);  // #E2E8F0
        public static readonly Color BorderSubtle = Color.FromArgb(241, 245, 249); // #F1F5F9
        public static readonly Color BorderMedium = Color.FromArgb(203, 213, 225); // #CBD5E1

        public static readonly Font TitleFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        public static readonly Font SubtitleFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        public static readonly Font HeaderFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static readonly Font BodyBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Segoe UI", 8f, FontStyle.Regular);
        public static readonly Font MonospaceFont = new Font("Consolas", 8.5f, FontStyle.Regular);
    }

    public enum PexorisButtonStyle
    {
        PrimaryTeal,
        DestructiveRed,
        SuccessGreen,
        WarningAmber,
        SecondaryOutline
    }

    public class PexorisButton : Button
    {
        private bool _isHovered;
        private bool _isPressed;
        private PexorisButtonStyle _style;
        private float _cornerRadius;

        public PexorisButtonStyle ButtonStyle
        {
            get { return _style; }
            set { _style = value; Invalidate(); }
        }

        public float CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = value; Invalidate(); }
        }

        public PexorisButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);

            _style = PexorisButtonStyle.PrimaryTeal;
            _cornerRadius = 7f;
            Font = Theme.BodyBold;
            Cursor = Cursors.Hand;
            Size = new Size(130, 36);
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
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
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
            g.Clear(Parent != null ? Parent.BackColor : Theme.CardBg);

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            Color bg;
            Color text;
            Color border;

            switch (_style)
            {
                case PexorisButtonStyle.DestructiveRed:
                    bg = _isPressed ? Theme.DestructiveHover : (_isHovered ? Color.FromArgb(239, 68, 68) : Theme.Destructive);
                    text = Color.White;
                    border = bg;
                    break;

                case PexorisButtonStyle.SuccessGreen:
                    bg = _isPressed ? Theme.SuccessHover : (_isHovered ? Color.FromArgb(34, 197, 94) : Theme.Success);
                    text = Color.White;
                    border = bg;
                    break;

                case PexorisButtonStyle.WarningAmber:
                    bg = _isPressed ? Theme.WarningHover : (_isHovered ? Color.FromArgb(245, 158, 11) : Theme.Warning);
                    text = Color.White;
                    border = bg;
                    break;

                case PexorisButtonStyle.SecondaryOutline:
                    bg = _isPressed ? Color.FromArgb(241, 245, 249) : (_isHovered ? Color.FromArgb(248, 250, 252) : Color.White);
                    text = _isHovered ? Theme.TextHero : Theme.TextBody;
                    border = _isHovered ? Theme.Primary : Theme.BorderMedium;
                    break;

                case PexorisButtonStyle.PrimaryTeal:
                default:
                    bg = _isPressed ? Theme.PrimaryActive : (_isHovered ? Theme.PrimaryHover : Theme.Primary);
                    text = Color.White;
                    border = bg;
                    break;
            }

            if (!Enabled)
            {
                bg = Color.FromArgb(241, 245, 249);
                text = Color.FromArgb(148, 163, 184);
                border = Color.FromArgb(226, 232, 240);
            }

            using (GraphicsPath path = GetRoundedRectangleF(rect, _cornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(bg))
                {
                    g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(border, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.WordEllipsis);
        }

        private static GraphicsPath GetRoundedRectangleF(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            RectangleF arc = new RectangleF(bounds.Location, new SizeF(d, d));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
