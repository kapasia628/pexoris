using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pexoris.AIShield
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
        public static readonly Color BorderActive    = Color.FromArgb(99, 102, 241);    // #6366F1 Indigo Accent
        
        // Accents
        public static readonly Color AppleIndigo     = Color.FromArgb(99, 102, 241);    // #6366F1
        public static readonly Color AppleIndigoHover= Color.FromArgb(79, 70, 229);     // #4F46E5
        public static readonly Color AppleIndigoLight= Color.FromArgb(238, 242, 255);   // #EEF2FF
        
        public static readonly Color AppleBlue       = Color.FromArgb(0, 122, 255);     // #007AFF Apple Blue
        public static readonly Color AppleBlueHover  = Color.FromArgb(0, 102, 220);     // #0066DC
        public static readonly Color AppleBlueLight  = Color.FromArgb(239, 246, 255);   // #EFF6FF
        
        public static readonly Color AppleRed        = Color.FromArgb(255, 59, 48);     // #FF3B30 Apple Red
        public static readonly Color AppleRedHover   = Color.FromArgb(220, 38, 38);     // #DC2626
        public static readonly Color AppleRedLight   = Color.FromArgb(254, 242, 242);   // #FEF2F2
        
        public static readonly Color AppleGreen      = Color.FromArgb(52, 199, 89);     // #34C759 Apple Green
        public static readonly Color AppleGreenLight = Color.FromArgb(240, 253, 244);   // #F0FDF4
        
        public static readonly Color AppleAmber      = Color.FromArgb(245, 158, 11);    // #F59E0B Warning Amber
        public static readonly Color AppleAmberLight = Color.FromArgb(254, 243, 199);   // #FEF3C7
        
        public static readonly Color AppleViolet     = Color.FromArgb(139, 92, 246);    // #8B5CF6
        public static readonly Color AppleVioletLight= Color.FromArgb(245, 243, 255);   // #F5F3FF

        // Typography
        public static readonly Color TextPrimary     = Color.FromArgb(29, 29, 31);      // #1D1D1F
        public static readonly Color TextSecondary   = Color.FromArgb(107, 114, 128);   // #6B7280
        public static readonly Color TextMuted       = Color.FromArgb(156, 163, 175);   // #9CA3AF

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        public static void ApplyExplorerTheme(IntPtr handle)
        {
            try
            {
                SetWindowTheme(handle, "Explorer", null);
            }
            catch { }
        }

        public static Font FontHeader(float size)
        {
            return GetFont(size, FontStyle.Bold);
        }

        public static Font FontBody(float size)
        {
            return GetFont(size, FontStyle.Regular);
        }

        public static Font FontBold(float size)
        {
            return GetFont(size, FontStyle.Bold);
        }

        public static Font FontMono(float size)
        {
            try { return new Font("Consolas", size, FontStyle.Regular); }
            catch { return new Font(FontFamily.GenericMonospace, size, FontStyle.Regular); }
        }

        private static Font GetFont(float size, FontStyle style)
        {
            string[] preferred = { "Segoe UI Variable Text", "Segoe UI", "Plus Jakarta Sans", "Helvetica Neue", "Arial" };
            foreach (var fam in preferred)
            {
                try
                {
                    using (var test = new Font(fam, size, style))
                    {
                        if (test.Name.Equals(fam, StringComparison.OrdinalIgnoreCase))
                            return new Font(fam, size, style);
                    }
                }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, size, style);
        }

        public static GraphicsPath GetRoundedRectangleF(RectangleF bounds, float radius)
        {
            float d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawCard(Graphics g, Rectangle bounds, int radius = 12)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Soft drop shadow
            Rectangle shadowRect = new Rectangle(bounds.X, bounds.Y + 2, bounds.Width, bounds.Height);
            using (GraphicsPath sPath = RoundedRect(shadowRect, radius))
            using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(14, 0, 0, 0)))
            {
                g.FillPath(sBrush, sPath);
            }

            // Card body
            using (GraphicsPath path = RoundedRect(bounds, radius))
            {
                using (SolidBrush brush = new SolidBrush(AppleWhite))
                {
                    g.FillPath(brush, path);
                }
                using (Pen pen = new Pen(BorderLight, 1f))
                {
                    pen.Alignment = PenAlignment.Inset;
                    g.DrawPath(pen, path);
                }
            }
        }
    }

    public class PexorisButton : Button
    {
        public enum ButtonStyleType
        {
            PrimaryBlue,
            PrimaryIndigo,
            DestructiveRed,
            SecondaryOutline,
            IndigoOutline,
            PresetIndigo,
            PresetGray
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
            UseMnemonic = false;
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
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle client = ClientRectangle;
            RectangleF rect = new RectangleF(0.5f, 0.5f, client.Width - 1f, client.Height - 1f);

            // Paint parent background first to eliminate black corner artifacts
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
                        bgColor = _isPressed ? Color.FromArgb(200, 30, 30) : (_isHovered ? PexorisTheme.AppleRedHover : PexorisTheme.AppleRed);
                        fgColor = Color.White;
                        borderColor = bgColor;
                        break;

                    case ButtonStyleType.PrimaryIndigo:
                        bgColor = _isPressed ? Color.FromArgb(67, 56, 202) : (_isHovered ? PexorisTheme.AppleIndigoHover : PexorisTheme.AppleIndigo);
                        fgColor = Color.White;
                        borderColor = bgColor;
                        break;

                    case ButtonStyleType.IndigoOutline:
                        bgColor = _isPressed ? Color.FromArgb(224, 231, 255) : (_isHovered ? PexorisTheme.AppleIndigoLight : PexorisTheme.AppleWhite);
                        fgColor = PexorisTheme.AppleIndigo;
                        borderColor = _isHovered ? PexorisTheme.AppleIndigoHover : PexorisTheme.AppleIndigo;
                        break;

                    case ButtonStyleType.PresetIndigo:
                        bgColor = _isPressed ? Color.FromArgb(199, 210, 254) : (_isHovered ? Color.FromArgb(224, 231, 255) : PexorisTheme.AppleIndigoLight);
                        fgColor = Color.FromArgb(79, 70, 229);
                        borderColor = _isHovered ? Color.FromArgb(165, 180, 252) : Color.FromArgb(199, 210, 254);
                        break;

                    case ButtonStyleType.PresetGray:
                        bgColor = _isPressed ? Color.FromArgb(209, 213, 219) : (_isHovered ? Color.FromArgb(229, 231, 235) : Color.FromArgb(243, 244, 246));
                        fgColor = PexorisTheme.TextPrimary;
                        borderColor = _isHovered ? Color.FromArgb(156, 163, 175) : PexorisTheme.BorderMedium;
                        break;

                    case ButtonStyleType.SecondaryOutline:
                        bgColor = _isPressed ? Color.FromArgb(243, 244, 246) : (_isHovered ? Color.FromArgb(249, 250, 251) : Color.White);
                        fgColor = PexorisTheme.TextPrimary;
                        borderColor = _isHovered ? PexorisTheme.AppleBlue : PexorisTheme.BorderMedium;
                        break;

                    case ButtonStyleType.PrimaryBlue:
                    default:
                        bgColor = _isPressed ? Color.FromArgb(0, 85, 190) : (_isHovered ? PexorisTheme.AppleBlueHover : PexorisTheme.AppleBlue);
                        fgColor = Color.White;
                        borderColor = bgColor;
                        break;
                }
            }

            using (GraphicsPath path = PexorisTheme.GetRoundedRectangleF(rect, CornerRadius))
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

            // Draw Button Text
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                client,
                fgColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}
