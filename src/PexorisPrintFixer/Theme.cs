using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Pexoris.PrintFixer
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
        
        public static readonly Color AppleAmber      = Color.FromArgb(245, 158, 11);    // #F59E0B Warning Amber
        public static readonly Color AppleAmberLight = Color.FromArgb(254, 243, 199);   // #FEF3C7
        
        public static readonly Color AppleTeal       = Color.FromArgb(16, 185, 129);    // #10B981 Emerald / Teal
        public static readonly Color AppleTealLight  = Color.FromArgb(236, 253, 245);   // #ECFDF5
        
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

        public static Font FontCode(float size)
        {
            try { return new Font("Consolas", size, FontStyle.Regular); }
            catch { return new Font(FontFamily.GenericMonospace, size); }
        }

        private static Font GetFont(float size, FontStyle style)
        {
            string[] preferred = new string[] { "Segoe UI Variable Text", "Segoe UI", "-apple-system", "Helvetica Neue" };
            for (int i = 0; i < preferred.Length; i++)
            {
                string fontName = preferred[i];
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

        public static Bitmap GetPrintFixerLogoBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                RenderPrintFixerLogo(g, new Rectangle(0, 0, size, size));
            }
            return bmp;
        }

        public static void RenderPrintFixerLogo(Graphics g, Rectangle r)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Math.Min(r.Width, r.Height);
            float pad = s * 0.08f;
            RectangleF badgeRect = new RectangleF(r.X + pad, r.Y + pad, s - pad * 2, s - pad * 2);

            using (GraphicsPath badgePath = GetRoundedPath(badgeRect, s * 0.24f))
            {
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                    badgeRect,
                    Color.FromArgb(255, 6, 182, 212),  // Vivid Cyan
                    Color.FromArgb(255, 15, 23, 42),   // Dark Slate
                    LinearGradientMode.ForwardDiagonal))
                {
                    ColorBlend cb = new ColorBlend();
                    cb.Colors = new Color[] {
                        Color.FromArgb(255, 6, 182, 212),
                        Color.FromArgb(255, 16, 185, 129),
                        Color.FromArgb(255, 15, 23, 42)
                    };
                    cb.Positions = new float[] { 0f, 0.5f, 1f };
                    bgBrush.InterpolationColors = cb;
                    g.FillPath(bgBrush, badgePath);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(160, 255, 255, 255), Math.Max(1f, s * 0.04f)))
                {
                    borderPen.Alignment = PenAlignment.Inset;
                    g.DrawPath(borderPen, badgePath);
                }
            }

            // Printer Body (White mini chassis)
            float bodyW = s * 0.55f;
            float bodyH = s * 0.28f;
            float bodyX = r.X + (s - bodyW) / 2f;
            float bodyY = r.Y + s * 0.44f;
            RectangleF bodyRect = new RectangleF(bodyX, bodyY, bodyW, bodyH);
            using (GraphicsPath bp = GetRoundedPath(bodyRect, s * 0.08f))
            {
                using (SolidBrush bB = new SolidBrush(Color.White))
                {
                    g.FillPath(bB, bp);
                }
            }

            // Paper Feed top
            float pW = s * 0.38f;
            float pH = s * 0.22f;
            float pX = r.X + (s - pW) / 2f;
            float pY = r.Y + s * 0.26f;
            RectangleF pRect = new RectangleF(pX, pY, pW, pH);
            using (GraphicsPath pp = GetRoundedPath(pRect, s * 0.04f))
            {
                using (SolidBrush pB = new SolidBrush(Color.FromArgb(235, 245, 255)))
                {
                    g.FillPath(pB, pp);
                }
            }

            // Green Fix checkmark
            if (s >= 20)
            {
                using (Pen chk = new Pen(Color.FromArgb(255, 16, 185, 129), Math.Max(1.5f, s * 0.07f)))
                {
                    chk.StartCap = LineCap.Round;
                    chk.EndCap = LineCap.Round;
                    chk.LineJoin = LineJoin.Round;
                    g.DrawLines(chk, new PointF[] {
                        new PointF(bodyX + bodyW * 0.32f, bodyY + bodyH * 0.52f),
                        new PointF(bodyX + bodyW * 0.46f, bodyY + bodyH * 0.76f),
                        new PointF(bodyX + bodyW * 0.72f, bodyY + bodyH * 0.28f)
                    });
                }
            }
        }

        public static void StyleAppleButton(Button btn, Color bg, Color hover, Color text, bool hasBorder)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = hasBorder ? 1 : 0;
            btn.FlatAppearance.BorderColor = BorderMedium;
            btn.BackColor = bg;
            btn.ForeColor = text;
            btn.Cursor = Cursors.Hand;
            btn.Font = FontBold(9f);

            btn.MouseEnter += delegate {
                btn.BackColor = hover;
            };
            btn.MouseLeave += delegate {
                btn.BackColor = bg;
            };
        }
    }
}
