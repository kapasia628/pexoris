using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PexorisQuickHash
{
    public enum ButtonStyle
    {
        DestructiveRed, // Used as Primary Rose/Fuchsia for QuickHash
        PrimaryBlue,
        SuccessGreen,
        SecondaryOutline
    }

    public static class Theme
    {
        // Pexoris QuickHash Rose/Pink Palette
        public static readonly Color PrimaryRose = Color.FromArgb(219, 39, 119);       // #DB2777
        public static readonly Color PrimaryRoseHover = Color.FromArgb(190, 24, 93);   // #BE185D
        public static readonly Color PrimaryRoseActive = Color.FromArgb(157, 23, 77);  // #9D174D
        public static readonly Color PrimaryRoseLight = Color.FromArgb(253, 242, 248); // #FDF2F8
        public static readonly Color PrimaryRoseBorder = Color.FromArgb(251, 207, 232);// #FBCFE8

        // General Neutrals
        public static readonly Color Background = Color.FromArgb(248, 250, 252);       // #F8FAFC
        public static readonly Color Surface = Color.FromArgb(255, 255, 255);          // #FFFFFF
        public static readonly Color Border = Color.FromArgb(226, 232, 240);           // #E2E8F0
        public static readonly Color BorderLight = Color.FromArgb(241, 245, 249);      // #F1F5F9
        public static readonly Color TextHero = Color.FromArgb(15, 23, 42);            // #0F172A
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);        // #64748B

        // State accents
        public static readonly Color SuccessGreen = Color.FromArgb(16, 185, 129);      // #10B981
        public static readonly Color SuccessLight = Color.FromArgb(236, 253, 245);     // #ECFDF5
        public static readonly Color SuccessBorder = Color.FromArgb(167, 243, 208);    // #A7F3D0

        public static readonly Color DestructiveRed = Color.FromArgb(239, 68, 68);     // #EF4444
        public static readonly Color DestructiveLight = Color.FromArgb(254, 242, 242); // #FEF2F2
        public static readonly Color DestructiveBorder = Color.FromArgb(254, 202, 202);// #FECACA

        public static readonly Color PrimaryBlue = Color.FromArgb(37, 99, 235);        // #2563EB
        public static readonly Color BlueLight = Color.FromArgb(239, 246, 255);        // #EFF6FF

        // Fonts
        public static readonly Font TitleFont = new Font("Segoe UI", 12.5f, FontStyle.Bold);
        public static readonly Font SubtitleFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        public static readonly Font BodyFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static readonly Font BoldFont = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font CodeFont = new Font("Consolas", 8.5f, FontStyle.Regular);
        public static readonly Font CodeFontBold = new Font("Consolas", 8.5f, FontStyle.Bold);

        public static GraphicsPath GetRoundedRectangle(RectangleF rect, float radius)
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
    }

    public class PexorisButton : Button
    {
        private ButtonStyle _style;
        private bool _isHovered;
        private bool _isPressed;
        private float _cornerRadius;

        public ButtonStyle Style
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

            _style = ButtonStyle.SecondaryOutline;
            _cornerRadius = 7f;
            Font = Theme.BoldFont;
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
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
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = false;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle clientRect = ClientRectangle;
            if (clientRect.Width <= 0 || clientRect.Height <= 0) return;

            RectangleF rect = new RectangleF(0, 0, clientRect.Width - 1, clientRect.Height - 1);

            Color bgColor;
            Color borderColor;
            Color textColor;

            switch (_style)
            {
                case ButtonStyle.DestructiveRed: // Primary Rose
                    if (_isPressed)
                    {
                        bgColor = Theme.PrimaryRoseActive;
                        borderColor = Theme.PrimaryRoseActive;
                    }
                    else if (_isHovered)
                    {
                        bgColor = Theme.PrimaryRoseHover;
                        borderColor = Theme.PrimaryRoseHover;
                    }
                    else
                    {
                        bgColor = Theme.PrimaryRose;
                        borderColor = Theme.PrimaryRose;
                    }
                    textColor = Color.White;
                    break;

                case ButtonStyle.PrimaryBlue:
                    if (_isPressed)
                    {
                        bgColor = Color.FromArgb(29, 78, 216);
                        borderColor = Color.FromArgb(29, 78, 216);
                    }
                    else if (_isHovered)
                    {
                        bgColor = Color.FromArgb(30, 64, 175);
                        borderColor = Color.FromArgb(30, 64, 175);
                    }
                    else
                    {
                        bgColor = Theme.PrimaryBlue;
                        borderColor = Theme.PrimaryBlue;
                    }
                    textColor = Color.White;
                    break;

                case ButtonStyle.SuccessGreen:
                    if (_isPressed)
                    {
                        bgColor = Color.FromArgb(4, 120, 87);
                        borderColor = Color.FromArgb(4, 120, 87);
                    }
                    else if (_isHovered)
                    {
                        bgColor = Color.FromArgb(5, 150, 105);
                        borderColor = Color.FromArgb(5, 150, 105);
                    }
                    else
                    {
                        bgColor = Theme.SuccessGreen;
                        borderColor = Theme.SuccessGreen;
                    }
                    textColor = Color.White;
                    break;

                case ButtonStyle.SecondaryOutline:
                default:
                    if (_isPressed)
                    {
                        bgColor = Color.FromArgb(226, 232, 240);
                        borderColor = Color.FromArgb(203, 213, 225);
                        textColor = Theme.TextHero;
                    }
                    else if (_isHovered)
                    {
                        bgColor = Color.FromArgb(241, 245, 249);
                        borderColor = Color.FromArgb(203, 213, 225);
                        textColor = Theme.TextHero;
                    }
                    else
                    {
                        bgColor = Theme.Surface;
                        borderColor = Theme.Border;
                        textColor = Theme.TextHero;
                    }
                    break;
            }

            using (GraphicsPath path = Theme.GetRoundedRectangle(rect, _cornerRadius))
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

            // Draw Text
            TextRenderer.DrawText(g, Text, Font, clientRect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}
