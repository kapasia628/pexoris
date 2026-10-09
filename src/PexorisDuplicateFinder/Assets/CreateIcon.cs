using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace PexorisIconGen
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string icoPath = Path.Combine(baseDir, "app.ico");
            string toolPngPath = @"c:\xampp\htdocs\tools\website\tools\duplicate-finder\duplicate-finder.png";
            string assetPngPath = @"c:\xampp\htdocs\tools\website\assets\icons\duplicate-finder.png";

            int[] sizes = new int[] { 256, 48, 32, 16 };
            Bitmap[] bitmaps = new Bitmap[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                bitmaps[i] = DrawIconBitmap(sizes[i]);
            }

            // Save High-Res PNGs
            Directory.CreateDirectory(Path.GetDirectoryName(toolPngPath));
            Directory.CreateDirectory(Path.GetDirectoryName(assetPngPath));
            bitmaps[0].Save(toolPngPath, ImageFormat.Png);
            bitmaps[0].Save(assetPngPath, ImageFormat.Png);

            // Save multi-res ICO
            SaveIco(bitmaps, icoPath);
            Console.WriteLine("Icon generated successfully: " + icoPath);
        }

        static Bitmap DrawIconBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                float scale = size / 256f;

                // Squircle background with Deep Indigo / Violet Gradient
                RectangleF rect = new RectangleF(10 * scale, 10 * scale, 236 * scale, 236 * scale);
                using (GraphicsPath path = GetSquircle(rect, 54 * scale))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(79, 70, 229),   // #4F46E5 Indigo 600
                        Color.FromArgb(67, 56, 202),   // #4338CA Indigo 700
                        LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(brush, path);
                    }

                    // Inner border subtle glow
                    using (Pen borderPen = new Pen(Color.FromArgb(70, 255, 255, 255), 2.5f * scale))
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                // Card 1 (Back Document - slightly shifted to left and higher)
                float bX = 46 * scale;
                float bY = 52 * scale;
                float cW = 106 * scale;
                float cH = 138 * scale;
                RectangleF backCardRect = new RectangleF(bX, bY, cW, cH);

                using (GraphicsPath backCard = GetSquircle(backCardRect, 14 * scale))
                {
                    using (SolidBrush backBrush = new SolidBrush(Color.FromArgb(200, 224, 231, 255))) // Soft Lavender / Blue
                    {
                        g.FillPath(backBrush, backCard);
                    }
                    using (Pen backPen = new Pen(Color.FromArgb(165, 180, 252), 2.5f * scale))
                    {
                        g.DrawPath(backPen, backCard);
                    }
                }

                // Card 2 (Front Document - shifted right and lower)
                float fX = 94 * scale;
                float fY = 72 * scale;
                RectangleF frontCardRect = new RectangleF(fX, fY, cW, cH);

                // Drop shadow under front card
                RectangleF shadowRect = new RectangleF(fX - 4 * scale, fY + 4 * scale, cW + 4 * scale, cH);
                using (GraphicsPath shadowPath = GetSquircle(shadowRect, 14 * scale))
                {
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                    {
                        g.FillPath(shadowBrush, shadowPath);
                    }
                }

                using (GraphicsPath frontCard = GetSquircle(frontCardRect, 14 * scale))
                {
                    using (SolidBrush frontBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                    {
                        g.FillPath(frontBrush, frontCard);
                    }
                    using (Pen frontPen = new Pen(Color.FromArgb(199, 210, 254), 2.5f * scale))
                    {
                        g.DrawPath(frontPen, frontCard);
                    }
                }

                // Header band on front card
                RectangleF frontHeader = new RectangleF(fX + 10 * scale, fY + 12 * scale, cW - 20 * scale, 20 * scale);
                using (GraphicsPath headerPath = GetSquircle(frontHeader, 6 * scale))
                {
                    using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(99, 102, 241)))
                    {
                        g.FillPath(headerBrush, headerPath);
                    }
                }

                // Content lines on front card
                using (Pen linePen = new Pen(Color.FromArgb(226, 232, 240), 5f * scale))
                {
                    linePen.StartCap = LineCap.Round;
                    linePen.EndCap = LineCap.Round;
                    g.DrawLine(linePen, fX + 16 * scale, fY + 46 * scale, fX + cW - 16 * scale, fY + 46 * scale);
                    g.DrawLine(linePen, fX + 16 * scale, fY + 62 * scale, fX + cW - 24 * scale, fY + 62 * scale);
                    g.DrawLine(linePen, fX + 16 * scale, fY + 78 * scale, fX + cW - 32 * scale, fY + 78 * scale);
                }

                // Twin Clone Equality Symbol "=" or Copy Emblem (Electric Cyan / Blue Pill)
                float eqX = 118 * scale;
                float eqY = 164 * scale;
                float eqW = 74 * scale;
                float eqH = 46 * scale;
                RectangleF eqRect = new RectangleF(eqX, eqY, eqW, eqH);

                using (GraphicsPath eqPath = GetSquircle(eqRect, 12 * scale))
                {
                    using (LinearGradientBrush eqBrush = new LinearGradientBrush(
                        eqRect,
                        Color.FromArgb(14, 165, 233), // Sky Blue
                        Color.FromArgb(2, 132, 199),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(eqBrush, eqPath);
                    }
                    using (Pen eqPen = new Pen(Color.FromArgb(255, 255, 255), 2f * scale))
                    {
                        g.DrawPath(eqPen, eqPath);
                    }
                }

                // "=" symbol in white inside pill
                using (Pen barPen = new Pen(Color.White, 4.5f * scale))
                {
                    barPen.StartCap = LineCap.Round;
                    barPen.EndCap = LineCap.Round;
                    g.DrawLine(barPen, eqX + 18 * scale, eqY + 16 * scale, eqX + eqW - 18 * scale, eqY + 16 * scale);
                    g.DrawLine(barPen, eqX + 18 * scale, eqY + 28 * scale, eqX + eqW - 18 * scale, eqY + 28 * scale);
                }

                // Sparkles (White 4-point stars)
                DrawSparkle(g, 68 * scale, 40 * scale, 14 * scale);
                DrawSparkle(g, 208 * scale, 124 * scale, 15 * scale);
                DrawSparkle(g, 52 * scale, 186 * scale, 11 * scale);
            }
            return bmp;
        }

        static void DrawSparkle(Graphics g, float x, float y, float radius)
        {
            PointF[] star = new PointF[]
            {
                new PointF(x, y - radius),
                new PointF(x + radius * 0.25f, y - radius * 0.25f),
                new PointF(x + radius, y),
                new PointF(x + radius * 0.25f, y + radius * 0.25f),
                new PointF(x, y + radius),
                new PointF(x - radius * 0.25f, y + radius * 0.25f),
                new PointF(x - radius, y),
                new PointF(x - radius * 0.25f, y - radius * 0.25f)
            };
            using (SolidBrush starBrush = new SolidBrush(Color.White))
            {
                g.FillPolygon(starBrush, star);
            }
        }

        static GraphicsPath GetSquircle(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void SaveIco(Bitmap[] bitmaps, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((short)0); // reserved
                bw.Write((short)1); // type 1 = icon
                bw.Write((short)bitmaps.Length); // count

                int offset = 6 + (16 * bitmaps.Length);
                byte[][] rawBuffers = new byte[bitmaps.Length][];

                for (int i = 0; i < bitmaps.Length; i++)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmaps[i].Save(ms, ImageFormat.Png);
                        rawBuffers[i] = ms.ToArray();
                    }
                }

                for (int i = 0; i < bitmaps.Length; i++)
                {
                    int w = bitmaps[i].Width >= 256 ? 0 : bitmaps[i].Width;
                    int h = bitmaps[i].Height >= 256 ? 0 : bitmaps[i].Height;

                    bw.Write((byte)w);
                    bw.Write((byte)h);
                    bw.Write((byte)0); // colors
                    bw.Write((byte)0); // reserved
                    bw.Write((short)1); // planes
                    bw.Write((short)32); // bpp
                    bw.Write(rawBuffers[i].Length); // size
                    bw.Write(offset); // offset

                    offset += rawBuffers[i].Length;
                }

                for (int i = 0; i < bitmaps.Length; i++)
                {
                    bw.Write(rawBuffers[i]);
                }
            }
        }
    }
}
