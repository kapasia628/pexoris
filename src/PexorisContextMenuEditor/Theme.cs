using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Pexoris.ContextMenuEditor
{
    public enum ButtonStyleType
    {
        PrimaryBlue,
        DestructiveRed,
        SuccessGreen,
        SecondaryOutline
    }

    public static class Theme
    {
        // Pexoris Light SaaS Palette Tokens
        public static readonly Color BgCanvas       = Color.FromArgb(248, 250, 252); // #F8FAFC
        public static readonly Color BgCard         = Color.FromArgb(255, 255, 255); // #FFFFFF
        public static readonly Color BgHeader       = Color.FromArgb(241, 245, 249); // #F1F5F9
        public static readonly Color BorderSubtle   = Color.FromArgb(226, 232, 240); // #E2E8F0
        public static readonly Color BorderStrong   = Color.FromArgb(203, 213, 225); // #CBD5E1

        public static readonly Color TextHero       = Color.FromArgb(15, 23, 42);    // #0F172A
        public static readonly Color TextBody       = Color.FromArgb(51, 65, 85);    // #334155
        public static readonly Color TextMuted      = Color.FromArgb(100, 116, 139); // #64748B
        public static readonly Color TextSubtle     = Color.FromArgb(148, 163, 184); // #94A3B8

        public static readonly Color PrimaryBlue    = Color.FromArgb(37, 99, 235);   // #2563EB
        public static readonly Color PrimaryHover   = Color.FromArgb(29, 78, 216);   // #1D4ED8
        public static readonly Color PrimaryLight   = Color.FromArgb(239, 246, 255); // #EFF6FF

        public static readonly Color DangerRed      = Color.FromArgb(239, 68, 68);   // #EF4444
        public static readonly Color DangerHover    = Color.FromArgb(220, 38, 38);   // #DC2626
        public static readonly Color DangerLight    = Color.FromArgb(254, 242, 242); // #FEF2F2

        public static readonly Color SuccessGreen   = Color.FromArgb(16, 185, 129);  // #10B981
        public static readonly Color SuccessHover   = Color.FromArgb(5, 150, 105);   // #059669
        public static readonly Color SuccessLight   = Color.FromArgb(236, 253, 245); // #ECFDF5

        public static readonly Color WarningAmber   = Color.FromArgb(245, 158, 11);  // #F59E0B
        public static readonly Color WarningLight   = Color.FromArgb(254, 243, 199); // #FEF3C7

        // Typography
        public static readonly Font FontTitleLarge  = new Font("Segoe UI", 12.5f, FontStyle.Bold);
        public static readonly Font FontTitleBar    = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font FontBodyBold    = new Font("Segoe UI", 9.0f, FontStyle.Bold);
        public static readonly Font FontBody        = new Font("Segoe UI", 9.0f, FontStyle.Regular);
        public static readonly Font FontSmallBold   = new Font("Segoe UI", 8.0f, FontStyle.Bold);
        public static readonly Font FontSmall       = new Font("Segoe UI", 8.0f, FontStyle.Regular);
        public static readonly Font FontMono        = new Font("Consolas", 8.5f, FontStyle.Regular);

        public static GraphicsPath GetRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
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
        public ButtonStyleType StyleType { get; set; }
        public float CornerRadius { get; set; }

        private bool isHovered = false;
        private bool isPressed = false;

        public PexorisButton()
        {
            StyleType = ButtonStyleType.PrimaryBlue;
            CornerRadius = 7f;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = Theme.FontBodyBold;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            if (mevent.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle client = ClientRectangle;
            if (client.Width <= 0 || client.Height <= 0) return;

            RectangleF rect = new RectangleF(0, 0, client.Width - 1, client.Height - 1);

            Color bg;
            Color text;
            Color border = Color.Transparent;

            switch (StyleType)
            {
                case ButtonStyleType.DestructiveRed:
                    bg = isPressed ? Color.FromArgb(185, 28, 28) : (isHovered ? Theme.DangerHover : Theme.DangerRed);
                    text = Color.White;
                    break;
                case ButtonStyleType.SuccessGreen:
                    bg = isPressed ? Color.FromArgb(4, 120, 87) : (isHovered ? Theme.SuccessHover : Theme.SuccessGreen);
                    text = Color.White;
                    break;
                case ButtonStyleType.SecondaryOutline:
                    bg = isPressed ? Color.FromArgb(226, 232, 240) : (isHovered ? Color.FromArgb(241, 245, 249) : Color.White);
                    text = Theme.TextBody;
                    border = isHovered ? Theme.BorderStrong : Theme.BorderSubtle;
                    break;
                case ButtonStyleType.PrimaryBlue:
                default:
                    bg = isPressed ? Color.FromArgb(30, 64, 175) : (isHovered ? Theme.PrimaryHover : Theme.PrimaryBlue);
                    text = Color.White;
                    break;
            }

            using (GraphicsPath path = Theme.GetRoundedPath(rect, CornerRadius))
            {
                using (SolidBrush b = new SolidBrush(bg))
                {
                    g.FillPath(b, path);
                }

                if (border != Color.Transparent)
                {
                    using (Pen p = new Pen(border, 1f))
                    {
                        g.DrawPath(p, path);
                    }
                }
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                client,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.WordEllipsis
            );
        }
    }
}
