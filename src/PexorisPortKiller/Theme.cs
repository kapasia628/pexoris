using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Pexoris.PortKiller
{
    public static class PexorisTheme
    {
        // ==========================================
        // APPLE macOS & MODERN FLUENT LIGHT THEME
        // ==========================================
        public static readonly Color AppleCanvas     = Color.FromArgb(246, 247, 249);   // #F6F7F9 Soft macOS Canvas
        public static readonly Color AppleWhite      = Color.FromArgb(255, 255, 255);   // #FFFFFF Crisp Card Surface
        public static readonly Color AppleCardHover  = Color.FromArgb(249, 250, 251);   // #F9FAFB
        public static readonly Color AppleInput      = Color.FromArgb(255, 255, 255);   // #FFFFFF
        
        // Borders
        public static readonly Color BorderLight     = Color.FromArgb(229, 231, 235);   // #E5E7EB
        public static readonly Color BorderMedium    = Color.FromArgb(209, 213, 219);   // #D1D5DB
        public static readonly Color BorderActive    = Color.FromArgb(0, 122, 255);     // #007AFF
        
        // Accents
        public static readonly Color AppleBlue       = Color.FromArgb(0, 122, 255);     // #007AFF Apple Blue
        public static readonly Color AppleBlueHover  = Color.FromArgb(0, 102, 220);     // #0066DC
        public static readonly Color AppleBlueLight  = Color.FromArgb(239, 246, 255);   // #EFF6FF
        
        public static readonly Color AppleRed        = Color.FromArgb(255, 59, 48);     // #FF3B30 Apple Red
        public static readonly Color AppleRedHover   = Color.FromArgb(220, 38, 38);     // #DC2626
        public static readonly Color AppleRedLight   = Color.FromArgb(254, 242, 242);   // #FEF2F2
        
        public static readonly Color AppleGreen      = Color.FromArgb(52, 199, 89);     // #34C759 Apple Green
        public static readonly Color AppleGreenLight = Color.FromArgb(240, 253, 244);   // #F0FDF4
        
        public static readonly Color AppleAmber      = Color.FromArgb(245, 158, 11);    // #F59E0B Lightning Amber
        public static readonly Color AppleAmberLight = Color.FromArgb(254, 243, 199);   // #FEF3C7
        
        // Typography
        public static readonly Color TextPrimary     = Color.FromArgb(29, 29, 31);      // #1D1D1F
        public static readonly Color TextSecondary   = Color.FromArgb(107, 114, 128);   // #6B7280
        public static readonly Color TextMuted       = Color.FromArgb(156, 163, 175);   // #9CA3AF
        
        // Traffic controls
        public static readonly Color TrafficRed      = Color.FromArgb(255, 95, 87);     // #FF5F57
        public static readonly Color TrafficYellow   = Color.FromArgb(254, 188, 46);    // #FEBC2E
        public static readonly Color TrafficGreen    = Color.FromArgb(40, 200, 64);     // #28C840

        public static Font FontHeader(float size = 11f)
        {
            return GetFont(size, FontStyle.Bold);
        }

        public static Font FontBody(float size = 9.5f)
        {
            return GetFont(size, FontStyle.Regular);
        }

        public static Font FontBold(float size = 9.5f)
        {
            return GetFont(size, FontStyle.Bold);
        }

        public static Font FontCode(float size = 9f)
        {
            try { return new Font("Consolas", size, FontStyle.Regular); }
            catch { return new Font(FontFamily.GenericMonospace, size); }
        }

        private static Font GetFont(float size, FontStyle style)
        {
            string[] preferred = new string[] { "Segoe UI Variable Text", "Segoe UI", "-apple-system", "Helvetica Neue" };
            foreach (string fontName in preferred)
            {
                try
                {
                    using (Font f = new Font(fontName, size, style))
                    {
                        if (f.Name.Equals(fontName, StringComparison.OrdinalIgnoreCase))
                        {
                            return new Font(fontName, size, style);
                        }
                    }
                }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, size, style);
        }

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

        public static Bitmap GetPortKillerLogoBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                RenderPortKillerLogo(g, new Rectangle(0, 0, size, size));
            }
            return bmp;
        }

        public static void RenderPortKillerLogo(Graphics g, Rectangle r)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Math.Min(r.Width, r.Height);
            float pad = s * 0.08f;
            RectangleF badgeRect = new RectangleF(r.X + pad, r.Y + pad, s - pad * 2, s - pad * 2);

            using (GraphicsPath badgePath = GetRoundedPath(badgeRect, s * 0.24f))
            {
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                    badgeRect,
                    Color.FromArgb(255, 14, 165, 233),
                    Color.FromArgb(255, 30, 27, 75),
                    LinearGradientMode.ForwardDiagonal))
                {
                    ColorBlend cb = new ColorBlend();
                    cb.Colors = new Color[] {
                        Color.FromArgb(255, 14, 165, 233),
                        Color.FromArgb(255, 79, 70, 229),
                        Color.FromArgb(255, 15, 23, 42)
                    };
                    cb.Positions = new float[] { 0f, 0.5f, 1f };
                    bgBrush.InterpolationColors = cb;
                    g.FillPath(bgBrush, badgePath);
                }

                using (Pen innerPen = new Pen(Color.FromArgb(180, 255, 255, 255), Math.Max(1f, s * 0.035f)))
                {
                    innerPen.Alignment = PenAlignment.Inset;
                    g.DrawPath(innerPen, badgePath);
                }
            }

            // Lightning Bolt
            PointF[] boltPoints = new PointF[] {
                new PointF(r.X + s * 0.54f, r.Y + s * 0.16f),
                new PointF(r.X + s * 0.32f, r.Y + s * 0.50f),
                new PointF(r.X + s * 0.48f, r.Y + s * 0.50f),
                new PointF(r.X + s * 0.38f, r.Y + s * 0.84f),
                new PointF(r.X + s * 0.68f, r.Y + s * 0.44f),
                new PointF(r.X + s * 0.52f, r.Y + s * 0.44f)
            };

            using (GraphicsPath boltPath = new GraphicsPath())
            {
                boltPath.AddPolygon(boltPoints);
                using (SolidBrush boltBrush = new SolidBrush(Color.FromArgb(255, 254, 240, 138)))
                {
                    g.FillPath(boltBrush, boltPath);
                }
                using (Pen boltPen = new Pen(Color.White, Math.Max(1f, s * 0.02f)))
                {
                    g.DrawPath(boltPen, boltPath);
                }
            }
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
            AmberPreset
        }

        public ButtonStyleType StyleType { get; set; }
        public float CornerRadius { get; set; }

        private bool _isHovered = false;
        private bool _isPressed = false;

        public PexorisButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            StyleType = ButtonStyleType.PrimaryBlue;
            CornerRadius = 7f;
            Font = PexorisTheme.FontBold(9.5f);
            Height = 36;
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs mevent) { _isPressed = true; Invalidate(); base.OnMouseDown(mevent); }
        protected override void OnMouseUp(MouseEventArgs mevent) { _isPressed = false; Invalidate(); base.OnMouseUp(mevent); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle client = ClientRectangle;
            RectangleF rect = new RectangleF(0.5f, 0.5f, client.Width - 1f, client.Height - 1f);

            // Paint parent background first to completely eliminate black corner artifacts
            Color parentBg = (Parent != null && Parent.BackColor != Color.Transparent) ? Parent.BackColor : PexorisTheme.AppleCanvas;
            using (SolidBrush parentBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(parentBrush, client);
            }

            Color bgColor, fgColor, borderColor;

            if (!Enabled)
            {
                bgColor = Color.FromArgb(243, 244, 246);
                fgColor = PexorisTheme.TextMuted;
                borderColor = PexorisTheme.BorderLight;
            }
            else
            {
                switch (StyleType)
                {
                    case ButtonStyleType.DestructiveRed:
                        bgColor = _isPressed ? PexorisTheme.AppleRedHover : (_isHovered ? Color.FromArgb(248, 113, 113) : PexorisTheme.AppleRed);
                        fgColor = Color.White;
                        borderColor = _isPressed ? PexorisTheme.AppleRedHover : PexorisTheme.AppleRed;
                        break;

                    case ButtonStyleType.SuccessGreen:
                        bgColor = _isPressed ? Color.FromArgb(22, 163, 74) : (_isHovered ? Color.FromArgb(74, 222, 128) : PexorisTheme.AppleGreen);
                        fgColor = Color.White;
                        borderColor = _isPressed ? Color.FromArgb(22, 163, 74) : PexorisTheme.AppleGreen;
                        break;

                    case ButtonStyleType.AmberPreset:
                        bgColor = _isPressed ? Color.FromArgb(217, 119, 6) : (_isHovered ? Color.FromArgb(251, 191, 36) : Color.FromArgb(245, 158, 11));
                        fgColor = Color.White;
                        borderColor = bgColor;
                        break;

                    case ButtonStyleType.SecondaryOutline:
                        bgColor = _isPressed ? Color.FromArgb(243, 244, 246) : (_isHovered ? Color.FromArgb(249, 250, 251) : Color.White);
                        fgColor = PexorisTheme.TextPrimary;
                        borderColor = _isHovered ? PexorisTheme.AppleBlue : PexorisTheme.BorderMedium;
                        break;

                    case ButtonStyleType.PrimaryBlue:
                    default:
                        bgColor = _isPressed ? PexorisTheme.AppleBlueHover : (_isHovered ? Color.FromArgb(37, 137, 255) : PexorisTheme.AppleBlue);
                        fgColor = Color.White;
                        borderColor = _isPressed ? PexorisTheme.AppleBlueHover : PexorisTheme.AppleBlue;
                        break;
                }
            }

            using (GraphicsPath path = PexorisTheme.GetRoundedPath(rect, CornerRadius))
            {
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(borderColor, 1f))
                {
                    pen.Alignment = PenAlignment.Inset;
                    g.DrawPath(pen, path);
                }
            }

            // Draw text without mnemonic underline (NoPrefix)
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | 
                                    TextFormatFlags.VerticalCenter | 
                                    TextFormatFlags.SingleLine | 
                                    TextFormatFlags.WordEllipsis |
                                    TextFormatFlags.NoPrefix;
            TextRenderer.DrawText(g, Text, Font, client, fgColor, flags);
        }
    }
}
