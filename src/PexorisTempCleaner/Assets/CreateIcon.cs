using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace CreateIcon
{
    class Program
    {
        static void Main()
        {
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisTempCleaner\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\temp-cleaner\temp-cleaner.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\temp-cleaner.png";

            Directory.CreateDirectory(Path.GetDirectoryName(outIco));
            Directory.CreateDirectory(Path.GetDirectoryName(outPng));
            Directory.CreateDirectory(Path.GetDirectoryName(outAssetsPng));

            using (Bitmap bmp256 = RenderIcon(256))
            {
                bmp256.Save(outPng, ImageFormat.Png);
                bmp256.Save(outAssetsPng, ImageFormat.Png);

                using (Bitmap bmp48 = RenderIcon(48))
                using (Bitmap bmp32 = RenderIcon(32))
                using (Bitmap bmp16 = RenderIcon(16))
                using (FileStream fs = new FileStream(outIco, FileMode.Create))
                {
                    WriteIco(new Bitmap[] { bmp256, bmp48, bmp32, bmp16 }, fs);
                }
            }
            Console.WriteLine("Icon created successfully at: " + outIco);
        }

        static Bitmap RenderIcon(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);

                // Vibrant Amber to Deep Crimson/Rose Gradient Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(245, 158, 11),  // Amber 500
                    Color.FromArgb(190, 24, 93)))   // Rose 700
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255), size * 0.015f))
                {
                    g.DrawPath(pen, path);
                }

                // Visual Artwork: Stylized Hard Drive Platter + Precision Sweeping Broom / Sparkles
                float diskX = size * 0.22f;
                float diskY = size * 0.24f;
                float diskW = size * 0.56f;
                float diskH = size * 0.54f;
                float diskR = size * 0.10f;

                // Disk Drive Shadow
                using (GraphicsPath shadowPath = GetRoundedPath(new RectangleF(diskX, diskY + size * 0.03f, diskW, diskH), diskR))
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Disk Drive Body
                using (GraphicsPath diskPath = GetRoundedPath(new RectangleF(diskX, diskY, diskW, diskH), diskR))
                using (SolidBrush diskBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (Pen diskBorder = new Pen(Color.FromArgb(203, 213, 225), size * 0.015f))
                {
                    g.FillPath(diskBg, diskPath);
                    g.DrawPath(diskBorder, diskPath);
                }

                // Drive internal platter outline
                float pcx = diskX + diskW * 0.5f;
                float pcy = diskY + diskH * 0.45f;
                float pr = size * 0.16f;

                using (Pen platterPen = new Pen(Color.FromArgb(226, 232, 240), size * 0.02f))
                using (SolidBrush platterCenter = new SolidBrush(Color.FromArgb(203, 213, 225)))
                {
                    g.DrawEllipse(platterPen, pcx - pr, pcy - pr, pr * 2, pr * 2);
                    g.FillEllipse(platterCenter, pcx - size * 0.04f, pcy - size * 0.04f, size * 0.08f, size * 0.08f);
                }

                // Drive LED indicator
                using (SolidBrush ledGreen = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillEllipse(ledGreen, diskX + size * 0.06f, diskY + diskH - size * 0.08f, size * 0.035f, size * 0.035f);
                }
                using (Pen ledLine = new Pen(Color.FromArgb(148, 163, 184), size * 0.018f))
                {
                    ledLine.StartCap = LineCap.Round;
                    ledLine.EndCap = LineCap.Round;
                    g.DrawLine(ledLine, diskX + size * 0.14f, diskY + diskH - size * 0.062f, diskX + diskW - size * 0.06f, diskY + diskH - size * 0.062f);
                }

                // Magic Sweep / Sparkle Badge (Bottom Right)
                float badgeX = size * 0.56f;
                float badgeY = size * 0.52f;
                float badgeS = size * 0.36f;

                using (SolidBrush bShadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                {
                    g.FillEllipse(bShadow, badgeX, badgeY + size * 0.02f, badgeS, badgeS);
                }

                using (LinearGradientBrush bBg = new LinearGradientBrush(
                    new PointF(badgeX, badgeY),
                    new PointF(badgeX + badgeS, badgeY + badgeS),
                    Color.FromArgb(239, 68, 68),   // Red 500
                    Color.FromArgb(185, 28, 28)))  // Red 700
                using (Pen bPen = new Pen(Color.White, size * 0.025f))
                {
                    g.FillEllipse(bBg, badgeX, badgeY, badgeS, badgeS);
                    g.DrawEllipse(bPen, badgeX, badgeY, badgeS, badgeS);
                }

                // Dynamic Lightning / Clean sweep sparkle inside circle
                using (GraphicsPath sweepPath = new GraphicsPath())
                {
                    float bx = badgeX + badgeS * 0.5f;
                    float by = badgeY + badgeS * 0.18f;
                    sweepPath.AddPolygon(new PointF[] {
                        new PointF(bx + badgeS * 0.04f, by),
                        new PointF(bx - badgeS * 0.22f, by + badgeS * 0.36f),
                        new PointF(bx - badgeS * 0.02f, by + badgeS * 0.36f),
                        new PointF(bx - badgeS * 0.10f, by + badgeS * 0.68f),
                        new PointF(bx + badgeS * 0.24f, by + badgeS * 0.28f),
                        new PointF(bx + badgeS * 0.04f, by + badgeS * 0.28f),
                    });
                    using (SolidBrush sBrush = new SolidBrush(Color.White))
                    {
                        g.FillPath(sBrush, sweepPath);
                    }
                }

                // Little golden sparkle on top left of disk
                using (SolidBrush starBrush = new SolidBrush(Color.FromArgb(254, 240, 138)))
                {
                    DrawSparkle(g, starBrush, size * 0.20f, size * 0.22f, size * 0.10f);
                    DrawSparkle(g, starBrush, size * 0.76f, size * 0.18f, size * 0.07f);
                }
            }
            return bmp;
        }

        static void DrawSparkle(Graphics g, Brush b, float x, float y, float r)
        {
            PointF[] pts = new PointF[] {
                new PointF(x, y - r),
                new PointF(x + r * 0.3f, y - r * 0.3f),
                new PointF(x + r, y),
                new PointF(x + r * 0.3f, y + r * 0.3f),
                new PointF(x, y + r),
                new PointF(x - r * 0.3f, y + r * 0.3f),
                new PointF(x - r, y),
                new PointF(x - r * 0.3f, y - r * 0.3f),
            };
            g.FillPolygon(b, pts);
        }

        static GraphicsPath GetRoundedPath(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void WriteIco(Bitmap[] images, Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)images.Length);

            int offset = 6 + (images.Length * 16);
            byte[][] pngBytes = new byte[images.Length][];

            for (int i = 0; i < images.Length; i++)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    images[i].Save(ms, ImageFormat.Png);
                    pngBytes[i] = ms.ToArray();
                }

                int w = images[i].Width >= 256 ? 0 : images[i].Width;
                int h = images[i].Height >= 256 ? 0 : images[i].Height;

                writer.Write((byte)w);
                writer.Write((byte)h);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write((uint)pngBytes[i].Length);
                writer.Write((uint)offset);

                offset += pngBytes[i].Length;
            }

            for (int i = 0; i < images.Length; i++)
            {
                writer.Write(pngBytes[i]);
            }
        }
    }
}
