using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace PexorisQuickHash.Assets
{
    static class GraphicsExtensions
    {
        public static void AddRoundedRectangle(this GraphicsPath path, RectangleF rect, float radius)
        {
            float d = radius * 2f;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
        }
    }

    class CreateIcon
    {
        static void Main()
        {
            int[] sizes = new int[] { 256, 48, 32, 16 };
            Bitmap[] bitmaps = new Bitmap[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                bitmaps[i] = RenderQuickHashBitmap(sizes[i]);
            }

            // Save multi-resolution .ico
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            SaveMultiResolutionIcon(bitmaps, icoPath);
            Console.WriteLine("Saved: " + icoPath);

            // Save 512x512 PNGs for website
            using (Bitmap big = RenderQuickHashBitmap(512))
            {
                string png1 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\website\tools\quickhash\quickhash.png"));
                string png2 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\website\assets\icons\quickhash.png"));
                
                string dir1 = Path.GetDirectoryName(png1);
                if (!Directory.Exists(dir1)) Directory.CreateDirectory(dir1);
                string dir2 = Path.GetDirectoryName(png2);
                if (!Directory.Exists(dir2)) Directory.CreateDirectory(dir2);

                big.Save(png1, ImageFormat.Png);
                big.Save(png2, ImageFormat.Png);
                Console.WriteLine("Saved: " + png1);
                Console.WriteLine("Saved: " + png2);
            }
        }

        static Bitmap RenderQuickHashBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float s = size;
                float pad = s * 0.08f;
                float w = s - (pad * 2f);
                float h = s - (pad * 2f);

                // Outer Squircle: Vibrant Rose / Fuchsia Gradient (#DB2777 to #831843)
                RectangleF rect = new RectangleF(pad, pad, w, h);
                using (GraphicsPath outerPath = new GraphicsPath())
                {
                    float r = w * 0.26f;
                    outerPath.AddRoundedRectangle(rect, r);

                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(rect, Color.FromArgb(219, 39, 119), Color.FromArgb(131, 24, 67), 65f))
                    {
                        g.FillPath(bgBrush, outerPath);
                    }

                    // Glossy border
                    using (Pen borderPen = new Pen(Color.FromArgb(160, 255, 255, 255), s * 0.018f))
                    {
                        g.DrawPath(borderPen, outerPath);
                    }
                }

                // Inner Dark Tech Card (Midnight Plum #1E1B4B)
                float cardMargin = s * 0.12f;
                float cardW = w - (cardMargin * 2f);
                float cardH = h - (cardMargin * 2f);
                float cardX = rect.X + cardMargin;
                float cardY = rect.Y + cardMargin;
                RectangleF innerRect = new RectangleF(cardX, cardY, cardW, cardH);

                using (GraphicsPath innerPath = new GraphicsPath())
                {
                    innerPath.AddRoundedRectangle(innerRect, cardW * 0.20f);
                    using (SolidBrush innerBrush = new SolidBrush(Color.FromArgb(24, 15, 38)))
                    {
                        g.FillPath(innerBrush, innerPath);
                    }
                    using (Pen innerPen = new Pen(Color.FromArgb(244, 114, 182), s * 0.02f)) // Rose 400
                    {
                        g.DrawPath(innerPen, innerPath);
                    }
                }

                // Central Hash Symbol '#' & Verification Checkmark
                // Hash bars
                float cx = cardX + cardW * 0.5f;
                float cy = cardY + cardH * 0.5f;
                float barLength = cardW * 0.62f;
                float barThickness = s * 0.052f;
                float spacing = cardW * 0.17f;

                using (Pen hashPen = new Pen(Color.FromArgb(253, 242, 248), barThickness)) // Soft white/rose
                {
                    hashPen.StartCap = LineCap.Round;
                    hashPen.EndCap = LineCap.Round;

                    // Horizontal bars (slight tilt)
                    g.DrawLine(hashPen, cx - barLength * 0.48f, cy - spacing, cx + barLength * 0.48f, cy - spacing);
                    g.DrawLine(hashPen, cx - barLength * 0.48f, cy + spacing, cx + barLength * 0.48f, cy + spacing);

                    // Vertical bars (slight slant)
                    float slant = barThickness * 0.8f;
                    g.DrawLine(hashPen, cx - spacing + slant, cy - barLength * 0.48f, cx - spacing - slant, cy + barLength * 0.48f);
                    g.DrawLine(hashPen, cx + spacing + slant, cy - barLength * 0.48f, cx + spacing - slant, cy + barLength * 0.48f);
                }

                // Inner Glowing Verification Shield / Badge at bottom-right
                float badgeSize = cardW * 0.44f;
                float badgeX = cardX + cardW - badgeSize * 1.05f;
                float badgeY = cardY + cardH - badgeSize * 1.05f;
                RectangleF badgeRect = new RectangleF(badgeX, badgeY, badgeSize, badgeSize);

                using (GraphicsPath badgePath = new GraphicsPath())
                {
                    badgePath.AddEllipse(badgeRect);
                    using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(16, 185, 129))) // Emerald Green #10B981
                    {
                        g.FillPath(badgeBg, badgePath);
                    }
                    using (Pen badgePen = new Pen(Color.FromArgb(255, 255, 255), s * 0.024f))
                    {
                        g.DrawPath(badgePen, badgePath);
                    }
                }

                // Crisp White Checkmark inside green badge
                using (Pen checkPen = new Pen(Color.White, s * 0.038f))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    checkPen.LineJoin = LineJoin.Round;

                    PointF p1 = new PointF(badgeX + badgeSize * 0.28f, badgeY + badgeSize * 0.52f);
                    PointF p2 = new PointF(badgeX + badgeSize * 0.45f, badgeY + badgeSize * 0.70f);
                    PointF p3 = new PointF(badgeX + badgeSize * 0.75f, badgeY + badgeSize * 0.35f);

                    g.DrawLines(checkPen, new PointF[] { p1, p2, p3 });
                }
            }
            return bmp;
        }

        static void SaveMultiResolutionIcon(Bitmap[] bitmaps, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR Header
                bw.Write((ushort)0); // Reserved
                bw.Write((ushort)1); // Type: 1 = ICO
                bw.Write((ushort)bitmaps.Length); // Count

                int offset = 6 + (16 * bitmaps.Length);

                byte[][] pngBytes = new byte[bitmaps.Length][];
                for (int i = 0; i < bitmaps.Length; i++)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmaps[i].Save(ms, ImageFormat.Png);
                        pngBytes[i] = ms.ToArray();
                    }
                }

                // ICONDIRENTRY for each image
                for (int i = 0; i < bitmaps.Length; i++)
                {
                    bw.Write((byte)(bitmaps[i].Width >= 256 ? 0 : bitmaps[i].Width));
                    bw.Write((byte)(bitmaps[i].Height >= 256 ? 0 : bitmaps[i].Height));
                    bw.Write((byte)0); // Color count (0 = >=8bpp)
                    bw.Write((byte)0); // Reserved
                    bw.Write((ushort)1); // Color planes
                    bw.Write((ushort)32); // Bits per pixel
                    bw.Write((uint)pngBytes[i].Length); // Size in bytes
                    bw.Write((uint)offset); // Offset to image data

                    offset += pngBytes[i].Length;
                }

                // Image Data (PNG streams)
                for (int i = 0; i < bitmaps.Length; i++)
                {
                    bw.Write(pngBytes[i]);
                }
            }
        }
    }
}
