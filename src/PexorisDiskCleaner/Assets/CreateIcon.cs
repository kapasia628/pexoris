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
            string toolPngPath = @"c:\xampp\htdocs\tools\website\tools\disk-cleaner\disk-cleaner.png";
            string assetPngPath = @"c:\xampp\htdocs\tools\website\assets\icons\disk-cleaner.png";

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

                // Squircle background with Emerald / Teal Clean Gradient
                RectangleF rect = new RectangleF(10 * scale, 10 * scale, 236 * scale, 236 * scale);
                using (GraphicsPath path = GetSquircle(rect, 54 * scale))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(16, 185, 129),  // #10B981 Emerald 500
                        Color.FromArgb(4, 120, 87),    // #047857 Emerald 700
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

                // Hard Drive / SSD Base Enclosure (White / Silver with soft shadow)
                float dx = 42 * scale;
                float dy = 64 * scale;
                float dw = 172 * scale;
                float dh = 138 * scale;
                RectangleF driveRect = new RectangleF(dx, dy, dw, dh);

                using (GraphicsPath drivePath = GetSquircle(driveRect, 20 * scale))
                {
                    // Drive body
                    using (SolidBrush driveBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                    {
                        g.FillPath(driveBrush, drivePath);
                    }

                    // Drive edge outline
                    using (Pen drivePen = new Pen(Color.FromArgb(203, 213, 225), 3f * scale))
                    {
                        g.DrawPath(drivePen, drivePath);
                    }
                }

                // Inner Platter / Drive Core Window
                float cx = 110 * scale;
                float cy = 133 * scale;
                float cr = 48 * scale;
                using (Pen platterPen = new Pen(Color.FromArgb(148, 163, 184), 4f * scale))
                {
                    g.DrawEllipse(platterPen, cx - cr, cy - cr, cr * 2, cr * 2);
                }
                using (SolidBrush spindleBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    float sr = 14 * scale;
                    g.FillEllipse(spindleBrush, cx - sr, cy - sr, sr * 2, sr * 2);
                }
                using (SolidBrush spindleCenter = new SolidBrush(Color.FromArgb(241, 245, 249)))
                {
                    float scr = 5 * scale;
                    g.FillEllipse(spindleCenter, cx - scr, cy - scr, scr * 2, scr * 2);
                }

                // Front LED & Connectors on drive
                using (SolidBrush ledBrush = new SolidBrush(Color.FromArgb(52, 211, 153)))
                {
                    g.FillEllipse(ledBrush, 184 * scale, 86 * scale, 12 * scale, 12 * scale);
                }
                using (SolidBrush ledGlow = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillEllipse(ledGlow, 186 * scale, 106 * scale, 8 * scale, 8 * scale);
                }

                // Golden Broom / Sparkling Sweeper Sweeping Away Dust
                // Broom handle (diagonal wooden / golden rod)
                using (Pen handlePen = new Pen(Color.FromArgb(245, 158, 11), 10f * scale))
                {
                    handlePen.StartCap = LineCap.Round;
                    handlePen.EndCap = LineCap.Round;
                    g.DrawLine(handlePen, 216 * scale, 42 * scale, 158 * scale, 114 * scale);
                }

                // Broom bristles (Bright Amber / Yellow Fan)
                PointF[] bristlePts = new PointF[]
                {
                    new PointF(158 * scale, 114 * scale),
                    new PointF(134 * scale, 134 * scale),
                    new PointF(142 * scale, 156 * scale),
                    new PointF(170 * scale, 138 * scale)
                };
                using (SolidBrush bristleBrush = new SolidBrush(Color.FromArgb(251, 191, 36)))
                {
                    g.FillPolygon(bristleBrush, bristlePts);
                }
                using (Pen bristleOutline = new Pen(Color.FromArgb(217, 119, 6), 2.5f * scale))
                {
                    g.DrawPolygon(bristleOutline, bristlePts);
                }

                // Sparkling Clean Stars (Cyan / White 4-point sparkles)
                DrawSparkle(g, 72 * scale, 52 * scale, 16 * scale);
                DrawSparkle(g, 198 * scale, 180 * scale, 14 * scale);
                DrawSparkle(g, 46 * scale, 168 * scale, 11 * scale);
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
