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
            string toolPngPath = @"c:\xampp\htdocs\tools\website\tools\icon-cache-rebuilder\icon-cache-rebuilder.png";
            string assetPngPath = @"c:\xampp\htdocs\tools\website\assets\icons\icon-cache-rebuilder.png";

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

                // Squircle background with Amber / Warm Orange Gradient
                RectangleF rect = new RectangleF(10 * scale, 10 * scale, 236 * scale, 236 * scale);
                using (GraphicsPath path = GetSquircle(rect, 54 * scale))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(245, 158, 11),   // #F59E0B Amber 500
                        Color.FromArgb(217, 119, 6),    // #D97706 Amber 600
                        LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(brush, path);
                    }

                    // Subtle inner border
                    using (Pen p = new Pen(Color.FromArgb(254, 243, 199), 4f * scale)) // Amber 100
                    {
                        g.DrawPath(p, path);
                    }
                }

                // Inner App Window / Icon Surface (#FFFFFF card)
                RectangleF innerRect = new RectangleF(52 * scale, 52 * scale, 152 * scale, 152 * scale);
                using (GraphicsPath innerPath = GetSquircle(innerRect, 32 * scale))
                {
                    using (SolidBrush innerBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                    {
                        g.FillPath(innerBrush, innerPath);
                    }

                    // Shadow / subtle border
                    using (Pen pInner = new Pen(Color.FromArgb(253, 230, 138), 3f * scale))
                    {
                        g.DrawPath(pInner, innerPath);
                    }
                }

                // 4 Grid Application Icons inside the card (representing desktop icons)
                Color[] tileColors = new Color[]
                {
                    Color.FromArgb(59, 130, 246),  // Blue 500
                    Color.FromArgb(16, 185, 129),  // Emerald 500
                    Color.FromArgb(239, 68, 68),   // Red 500
                    Color.FromArgb(168, 85, 247)   // Purple 500
                };

                float tileSize = 38 * scale;
                float gap = 16 * scale;
                float startX = 81 * scale;
                float startY = 81 * scale;

                for (int row = 0; row < 2; row++)
                {
                    for (int col = 0; col < 2; col++)
                    {
                        int idx = row * 2 + col;
                        RectangleF tileRect = new RectangleF(startX + col * (tileSize + gap), startY + row * (tileSize + gap), tileSize, tileSize);
                        using (GraphicsPath tilePath = GetSquircle(tileRect, 10 * scale))
                        {
                            using (SolidBrush tb = new SolidBrush(tileColors[idx]))
                            {
                                g.FillPath(tb, tilePath);
                            }
                        }
                    }
                }

                // Circular Rebuild / Refresh arrows badge in bottom-right corner
                float badgeSize = 90 * scale;
                RectangleF badgeRect = new RectangleF(150 * scale, 150 * scale, badgeSize, badgeSize);
                using (SolidBrush bb = new SolidBrush(Color.FromArgb(15, 23, 42))) // #0F172A Slate 900
                {
                    g.FillEllipse(bb, badgeRect);
                }
                using (Pen bp = new Pen(Color.FromArgb(255, 255, 255), 5f * scale))
                {
                    g.DrawEllipse(bp, badgeRect);
                }

                // Rebuild arrows (circular sweep + arrowhead in badge)
                using (Pen arrowPen = new Pen(Color.FromArgb(245, 158, 11), 6f * scale)) // Amber 500
                {
                    arrowPen.StartCap = LineCap.Round;
                    arrowPen.EndCap = LineCap.Round;
                    RectangleF arcRect = new RectangleF(168 * scale, 168 * scale, 54 * scale, 54 * scale);
                    g.DrawArc(arrowPen, arcRect, 45, 260);
                }

                // Arrow head
                PointF[] arrowHead = new PointF[]
                {
                    new PointF(202 * scale, 168 * scale),
                    new PointF(216 * scale, 178 * scale),
                    new PointF(202 * scale, 188 * scale)
                };
                using (SolidBrush headBrush = new SolidBrush(Color.FromArgb(245, 158, 11)))
                {
                    g.FillPolygon(headBrush, arrowHead);
                }
            }
            return bmp;
        }

        static GraphicsPath GetSquircle(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (rect.Width <= 0 || rect.Height <= 0) return path;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void SaveIco(Bitmap[] bitmaps, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    // ICONDIR
                    bw.Write((short)0); // reserved
                    bw.Write((short)1); // icon type
                    bw.Write((short)bitmaps.Length); // count

                    int offset = 6 + (16 * bitmaps.Length);
                    byte[][] pngBytes = new byte[bitmaps.Length][];

                    for (int i = 0; i < bitmaps.Length; i++)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bitmaps[i].Save(ms, ImageFormat.Png);
                            pngBytes[i] = ms.ToArray();
                        }

                        int w = bitmaps[i].Width >= 256 ? 0 : bitmaps[i].Width;
                        int h = bitmaps[i].Height >= 256 ? 0 : bitmaps[i].Height;

                        // ICONDIRENTRY
                        bw.Write((byte)w);
                        bw.Write((byte)h);
                        bw.Write((byte)0); // color count
                        bw.Write((byte)0); // reserved
                        bw.Write((short)1); // color planes
                        bw.Write((short)32); // bpp
                        bw.Write((int)pngBytes[i].Length);
                        bw.Write((int)offset);

                        offset += pngBytes[i].Length;
                    }

                    for (int i = 0; i < bitmaps.Length; i++)
                    {
                        bw.Write(pngBytes[i]);
                    }
                }
            }
        }
    }
}
