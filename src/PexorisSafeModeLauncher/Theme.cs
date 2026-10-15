using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PexorisSafeModeLauncher
{
    public static class Theme
    {
        // Colors
        public static readonly Color BgCanvas = Color.FromArgb(248, 250, 252);     // #F8FAFC
        public static readonly Color CardWhite = Color.FromArgb(255, 255, 255);    // #FFFFFF
        public static readonly Color BorderLight = Color.FromArgb(226, 232, 240);  // #E2E8F0
        public static readonly Color BorderSubtle = Color.FromArgb(241, 245, 249); // #F1F5F9

        public static readonly Color TextHero = Color.FromArgb(15, 23, 42);        // #0F172A
        public static readonly Color TextBody = Color.FromArgb(51, 65, 85);        // #334155
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);    // #64748B

        // Brand Crimson Red
        public static readonly Color PrimaryRed = Color.FromArgb(220, 38, 38);      // #DC2626
        public static readonly Color PrimaryRedHover = Color.FromArgb(185, 28, 28); // #B91C1C
        public static readonly Color PrimaryRedPress = Color.FromArgb(153, 27, 27); // #991B1B
        public static readonly Color RedTint = Color.FromArgb(254, 242, 242);       // #FEF2F2

        // Status Colors
        public static readonly Color GreenSuccess = Color.FromArgb(16, 185, 129);   // #10B981
        public static readonly Color GreenTint = Color.FromArgb(236, 253, 245);     // #ECFDF5
        public static readonly Color BluePrimary = Color.FromArgb(37, 99, 235);     // #2563EB
        public static readonly Color BlueTint = Color.FromArgb(239, 246, 255);      // #EFF6FF
        public static readonly Color AmberWarning = Color.FromArgb(217, 119, 6);    // #D97706
        public static readonly Color AmberTint = Color.FromArgb(254, 243, 199);     // #FEF3C7

        // Fonts
        public static readonly Font FontTitle = new Font("Segoe UI", 12f, FontStyle.Bold);
        public static readonly Font FontSection = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        public static readonly Font FontBody = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        public static readonly Font FontBodyBold = new Font("Segoe UI", 9.25f, FontStyle.Bold);
        public static readonly Font FontSmall = new Font("Segoe UI", 8.25f, FontStyle.Regular);
        public static readonly Font FontSmallBold = new Font("Segoe UI", 8.25f, FontStyle.Bold);
        public static readonly Font FontMono = new Font("Consolas", 8.75f, FontStyle.Regular);

        // Win32 Methods
        public const int CS_DROPSHADOW = 0x00020000;
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public const int WM_SETICON = 0x80;
        public const int ICON_SMALL = 0;
        public const int ICON_BIG = 1;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static GraphicsPath GetRoundedRect(Rectangle bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            RectangleF arc = new RectangleF(bounds.Location, new SizeF(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public enum ButtonStyle
    {
        DestructiveRed,
        SuccessGreen,
        PrimaryBlue,
        SecondaryOutline,
        WarningAmber
    }

    public class PexorisButton : Button
    {
        public ButtonStyle Style { get; set; }
        public float CornerRadius { get; set; }

        private bool _isHovered = false;
        private bool _isPressed = false;

        public PexorisButton()
        {
            Style = ButtonStyle.SecondaryOutline;
            CornerRadius = 7f;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = Theme.FontBodyBold;
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

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            Color bg;
            Color text;
            Color border;

            switch (Style)
            {
                case ButtonStyle.DestructiveRed:
                    bg = _isPressed ? Theme.PrimaryRedPress : (_isHovered ? Theme.PrimaryRedHover : Theme.PrimaryRed);
                    text = Color.White;
                    border = bg;
                    break;
                case ButtonStyle.SuccessGreen:
                    bg = _isPressed ? Color.FromArgb(4, 120, 87) : (_isHovered ? Color.FromArgb(5, 150, 105) : Theme.GreenSuccess);
                    text = Color.White;
                    border = bg;
                    break;
                case ButtonStyle.PrimaryBlue:
                    bg = _isPressed ? Color.FromArgb(29, 78, 216) : (_isHovered ? Color.FromArgb(30, 64, 175) : Theme.BluePrimary);
                    text = Color.White;
                    border = bg;
                    break;
                case ButtonStyle.WarningAmber:
                    bg = _isPressed ? Color.FromArgb(180, 83, 9) : (_isHovered ? Color.FromArgb(217, 119, 6) : Theme.AmberWarning);
                    text = Color.White;
                    border = bg;
                    break;
                default: // SecondaryOutline
                    bg = _isPressed ? Color.FromArgb(226, 232, 240) : (_isHovered ? Color.FromArgb(241, 245, 249) : Color.White);
                    text = Theme.TextBody;
                    border = _isHovered ? Theme.PrimaryRed : Theme.BorderLight;
                    break;
            }

            using (GraphicsPath path = Theme.GetRoundedRect(rect, CornerRadius))
            {
                using (SolidBrush b = new SolidBrush(bg))
                {
                    g.FillPath(b, path);
                }

                using (Pen p = new Pen(border, 1.25f))
                {
                    g.DrawPath(p, path);
                }
            }

            TextRenderer.DrawText(g, Text, Font, rect, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordEllipsis);
        }
    }
}
