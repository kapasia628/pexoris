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
            string toolPngPath = @"c:\xampp\htdocs\tools\website\tools\app-uninstaller\app-uninstaller.png";
            string assetPngPath = @"c:\xampp\htdocs\tools\website\assets\icons\app-uninstaller.png";

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

                // Squircle background with Royal Purple / Violet Gradient
                RectangleF rect = new RectangleF(10 * scale, 10 * scale, 236 * scale, 236 * scale);
                using (GraphicsPath path = GetSquircle(rect, 54 * scale))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(139, 92, 246),  // #8B5CF6 Violet 500
                        Color.FromArgb(109, 40, 217),  // #6D28D9 Violet 700
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

                // App Package Box / Software Cube (3D isometric box in white / silver)
                float cx = 120 * scale;
                float cy = 138 * scale;
                float bw = 52 * scale;
                float bh = 42 * scale;

                // Top Face of box
                PointF[] topFace = new PointF[]
                {
                    new PointF(cx, cy - bh),
                    new PointF(cx + bw, cy - bh / 2f),
                    new PointF(cx, cy),
                    new PointF(cx - bw, cy - bh / 2f)
                };
                using (SolidBrush topBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    g.FillPolygon(topBrush, topFace);
                }

                // Left Face of box
                PointF[] leftFace = new PointF[]
                {
                    new PointF(cx - bw, cy - bh / 2f),
                    new PointF(cx, cy),
                    new PointF(cx, cy + bh + 14 * scale),
                    new PointF(cx - bw, cy + bh / 2f + 14 * scale)
                };
                using (SolidBrush leftBrush = new SolidBrush(Color.FromArgb(203, 213, 225)))
                {
                    g.FillPolygon(leftBrush, leftFace);
                }

                // Right Face of box
                PointF[] rightFace = new PointF[]
                {
                    new PointF(cx, cy),
                    new PointF(cx + bw, cy - bh / 2f),
                    new PointF(cx + bw, cy + bh / 2f + 14 * scale),
                    new PointF(cx, cy + bh + 14 * scale)
                };
                using (SolidBrush rightBrush = new SolidBrush(Color.FromArgb(226, 232, 240)))
                {
                    g.FillPolygon(rightBrush, rightFace);
                }

                // Box edges
                using (Pen boxPen = new Pen(Color.FromArgb(148, 163, 184), 2f * scale))
                {
                    g.DrawPolygon(boxPen, topFace);
                    g.DrawPolygon(boxPen, leftFace);
                    g.DrawPolygon(boxPen, rightFace);
                }

                // Uninstaller Red Removal Badge / Shield with 'X' or minus at top right
                float badgeX = 176 * scale;
                float badgeY = 76 * scale;
                float badgeR = 34 * scale;

                using (SolidBrush badgeBrush = new SolidBrush(Color.FromArgb(239, 68, 68))) // Red 500
                {
                    g.FillEllipse(badgeBrush, badgeX - badgeR, badgeY - badgeR, badgeR * 2, badgeR * 2);
                }
                using (Pen badgeBorder = new Pen(Color.White, 3f * scale))
                {
                    g.DrawEllipse(badgeBorder, badgeX - badgeR, badgeY - badgeR, badgeR * 2, badgeR * 2);
                }

                // Minus / Cross symbol inside badge
                using (Pen crossPen = new Pen(Color.White, 6.5f * scale))
                {
                    crossPen.StartCap = LineCap.Round;
                    crossPen.EndCap = LineCap.Round;
                    float crossSize = 13 * scale;
                    g.DrawLine(crossPen, badgeX - crossSize, badgeY - crossSize, badgeX + crossSize, badgeY + crossSize);
                    g.DrawLine(crossPen, badgeX + crossSize, badgeY - crossSize, badgeX - crossSize, badgeY + crossSize);
                }

                // Whirling Trash / Purge arrow sweeping bottom left
                using (Pen sweepPen = new Pen(Color.FromArgb(254, 240, 138), 5f * scale)) // Warm Amber / Yellow
                {
                    sweepPen.StartCap = LineCap.Round;
                    sweepPen.EndCap = LineCap.Round;
                    g.DrawArc(sweepPen, 44 * scale, 140 * scale, 70 * scale, 70 * scale, 90, 180);
                }
            }
            return bmp;
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
