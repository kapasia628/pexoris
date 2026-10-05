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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisServiceOptimizer\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\service-optimizer\service-optimizer.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\service-optimizer.png";

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

                // Deep Teal/Cyan to Emerald Gradient Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedRect(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(13, 148, 136),  // #0D9488 Teal 600
                    Color.FromArgb(15, 23, 42)))   // #0F172A Slate 900
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle border highlight
                using (GraphicsPath path = GetRoundedRect(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255), size * 0.015f))
                {
                    g.DrawPath(pen, path);
                }

                // Precision Gear / Cog Artwork
                float cx = size * 0.46f;
                float cy = size * 0.48f;
                float outerR = size * 0.32f;
                float innerR = size * 0.22f;
                float holeR = size * 0.11f;
                int teeth = 8;

                using (GraphicsPath gearPath = new GraphicsPath())
                {
                    PointF[] pts = new PointF[teeth * 4];
                    double angleStep = Math.PI * 2 / (teeth * 4);
                    for (int i = 0; i < teeth * 4; i++)
                    {
                        double a = i * angleStep;
                        float rad = (i % 4 == 1 || i % 4 == 2) ? outerR : innerR;
                        pts[i] = new PointF(
                            cx + (float)(Math.Cos(a) * rad),
                            cy + (float)(Math.Sin(a) * rad)
                        );
                    }
                    gearPath.AddPolygon(pts);
                    gearPath.AddEllipse(cx - holeR, cy - holeR, holeR * 2, holeR * 2);

                    // Gear shadow
                    using (Matrix shadowMat = new Matrix())
                    {
                        shadowMat.Translate(0, size * 0.025f);
                        using (GraphicsPath shadowPath = (GraphicsPath)gearPath.Clone())
                        {
                            shadowPath.Transform(shadowMat);
                            using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                            {
                                g.FillPath(sBrush, shadowPath);
                            }
                        }
                    }

                    // Gear Fill: Crisp Platinum White / Silver gradient
                    using (LinearGradientBrush gearBrush = new LinearGradientBrush(
                        new PointF(cx - outerR, cy - outerR),
                        new PointF(cx + outerR, cy + outerR),
                        Color.FromArgb(255, 255, 255),
                        Color.FromArgb(203, 213, 225))) // Slate 300
                    {
                        g.FillPath(gearBrush, gearPath);
                    }

                    using (Pen gearPen = new Pen(Color.FromArgb(148, 163, 184), size * 0.012f))
                    {
                        g.DrawPath(gearPen, gearPath);
                    }
                }

                // Dynamic Lightning Optimizer Badge (Bottom Right)
                float badgeX = size * 0.58f;
                float badgeY = size * 0.54f;
                float badgeS = size * 0.34f;

                // Glowing emerald badge circle
                using (SolidBrush badgeShadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                {
                    g.FillEllipse(badgeShadow, badgeX, badgeY + size * 0.02f, badgeS, badgeS);
                }

                using (LinearGradientBrush badgeBg = new LinearGradientBrush(
                    new PointF(badgeX, badgeY),
                    new PointF(badgeX + badgeS, badgeY + badgeS),
                    Color.FromArgb(16, 185, 129),  // Emerald 500
                    Color.FromArgb(5, 150, 105)))  // Emerald 600
                using (Pen badgeBorder = new Pen(Color.White, size * 0.025f))
                {
                    g.FillEllipse(badgeBg, badgeX, badgeY, badgeS, badgeS);
                    g.DrawEllipse(badgeBorder, badgeX, badgeY, badgeS, badgeS);
                }

                // Sharp Lightning Bolt inside the Emerald circle
                using (GraphicsPath boltPath = new GraphicsPath())
                {
                    float bx = badgeX + badgeS * 0.5f;
                    float by = badgeY + badgeS * 0.18f;
                    boltPath.AddPolygon(new PointF[] {
                        new PointF(bx + badgeS * 0.04f, by),
                        new PointF(bx - badgeS * 0.22f, by + badgeS * 0.36f),
                        new PointF(bx - badgeS * 0.02f, by + badgeS * 0.36f),
                        new PointF(bx - badgeS * 0.10f, by + badgeS * 0.68f),
                        new PointF(bx + badgeS * 0.24f, by + badgeS * 0.28f),
                        new PointF(bx + badgeS * 0.04f, by + badgeS * 0.28f),
                    });
                    using (SolidBrush boltBrush = new SolidBrush(Color.White))
                    {
                        g.FillPath(boltBrush, boltPath);
                    }
                }
            }
            return bmp;
        }

        static GraphicsPath GetRoundedRect(RectangleF r, float radius)
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
            writer.Write((ushort)0); // Reserved
            writer.Write((ushort)1); // Type (1 = ICO)
            writer.Write((ushort)images.Length); // Count

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
                writer.Write((byte)0); // Color palette
                writer.Write((byte)0); // Reserved
                writer.Write((ushort)1); // Color planes
                writer.Write((ushort)32); // Bits per pixel
                writer.Write((uint)pngBytes[i].Length); // Size of image data
                writer.Write((uint)offset); // Offset to image data

                offset += pngBytes[i].Length;
            }

            for (int i = 0; i < images.Length; i++)
            {
                writer.Write(pngBytes[i]);
            }
        }
    }
}
