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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisBatteryOptimizer\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\battery-optimizer\battery-optimizer.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\battery-optimizer.png";

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

                // Luxurious Emerald to Deep Forest Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(16, 185, 129),  // Emerald 500 (#10B981)
                    Color.FromArgb(6, 78, 59)))     // Emerald 900 (#064E3B)
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle mint glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(100, 167, 243, 208), size * 0.015f)) // Emerald 200
                {
                    g.DrawPath(pen, path);
                }

                float cx = size * 0.5f;
                float cy = size * 0.5f;

                // Battery Outer Body (Upright Capsule/Rectangle)
                float bw = size * 0.44f;
                float bh = size * 0.56f;
                float bx = cx - bw * 0.5f;
                float by = cy - bh * 0.46f;
                float br = size * 0.07f;

                // Terminal Cap (+ Pole on top)
                float capW = bw * 0.42f;
                float capH = size * 0.055f;
                float capX = cx - capW * 0.5f;
                float capY = by - capH * 0.85f;
                RectangleF capRect = new RectangleF(capX, capY, capW, capH);
                using (GraphicsPath capPath = GetRoundedPath(capRect, size * 0.02f))
                using (SolidBrush capBrush = new SolidBrush(Color.FromArgb(209, 250, 229))) // Mint 100
                {
                    g.FillPath(capBrush, capPath);
                }

                // Main Battery Body
                RectangleF battRect = new RectangleF(bx, by, bw, bh);
                using (GraphicsPath battPath = GetRoundedPath(battRect, br))
                {
                    // Battery Body Drop Shadow
                    using (Matrix m = new Matrix())
                    {
                        m.Translate(0, size * 0.03f);
                        using (GraphicsPath shadowPath = (GraphicsPath)battPath.Clone())
                        {
                            shadowPath.Transform(m);
                            using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                            {
                                g.FillPath(sBrush, shadowPath);
                            }
                        }
                    }

                    // Battery Body Background (Dark Slate Emerald)
                    using (SolidBrush battBg = new SolidBrush(Color.FromArgb(4, 47, 46))) // Cyan/Teal 950
                    using (Pen battPen = new Pen(Color.FromArgb(255, 255, 255), size * 0.032f))
                    {
                        g.FillPath(battBg, battPath);
                        g.DrawPath(battPen, battPath);
                    }
                }

                // Liquid Charge Bar (Filling 75% of battery)
                float chargePad = size * 0.038f;
                float fillH = (bh - chargePad * 2) * 0.72f;
                float fillW = bw - chargePad * 2;
                float fillX = bx + chargePad;
                float fillY = by + bh - chargePad - fillH;
                RectangleF fillRect = new RectangleF(fillX, fillY, fillW, fillH);
                using (GraphicsPath fillPath = GetRoundedPath(fillRect, size * 0.04f))
                using (LinearGradientBrush fillBrush = new LinearGradientBrush(
                    new PointF(fillX, fillY),
                    new PointF(fillX, fillY + fillH),
                    Color.FromArgb(52, 211, 153),  // Emerald 400
                    Color.FromArgb(16, 185, 129))) // Emerald 500
                {
                    g.FillPath(fillBrush, fillPath);
                }

                // Golden Lightning Bolt in Center
                PointF[] boltPts = new PointF[]
                {
                    new PointF(cx + size * 0.02f, cy - size * 0.17f),
                    new PointF(cx - size * 0.10f, cy + size * 0.01f),
                    new PointF(cx - size * 0.01f, cy + size * 0.01f),
                    new PointF(cx - size * 0.05f, cy + size * 0.19f),
                    new PointF(cx + size * 0.10f, cy - size * 0.01f),
                    new PointF(cx + size * 0.01f, cy - size * 0.01f)
                };

                // Bolt Shadow
                using (Matrix m = new Matrix())
                {
                    m.Translate(0, size * 0.02f);
                    PointF[] shadowPts = (PointF[])boltPts.Clone();
                    for (int i = 0; i < shadowPts.Length; i++)
                    {
                        shadowPts[i].Y += size * 0.015f;
                    }
                    using (SolidBrush boltShadow = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                    {
                        g.FillPolygon(boltShadow, shadowPts);
                    }
                }

                // Bolt Fill (Electric Gold)
                using (LinearGradientBrush boltBrush = new LinearGradientBrush(
                    new PointF(cx, cy - size * 0.18f),
                    new PointF(cx, cy + size * 0.18f),
                    Color.FromArgb(254, 240, 138),  // Amber 200
                    Color.FromArgb(245, 158, 11)))   // Amber 500
                using (Pen boltPen = new Pen(Color.FromArgb(255, 255, 255), size * 0.018f))
                {
                    g.FillPolygon(boltBrush, boltPts);
                    g.DrawPolygon(boltPen, boltPts);
                }

                // Sparkle at top-right
                float starX = cx + size * 0.28f;
                float starY = cy - size * 0.22f;
                float starR = size * 0.065f;
                PointF[] starPts = new PointF[]
                {
                    new PointF(starX, starY - starR),
                    new PointF(starX + starR * 0.28f, starY - starR * 0.28f),
                    new PointF(starX + starR, starY),
                    new PointF(starX + starR * 0.28f, starY + starR * 0.28f),
                    new PointF(starX, starY + starR),
                    new PointF(starX - starR * 0.28f, starY + starR * 0.28f),
                    new PointF(starX - starR, starY),
                    new PointF(starX - starR * 0.28f, starY - starR * 0.28f)
                };
                using (SolidBrush starBrush = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
                {
                    g.FillPolygon(starBrush, starPts);
                }
            }
            return bmp;
        }

        static GraphicsPath GetRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void WriteIco(Bitmap[] bitmaps, Stream stream)
        {
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                bw.Write((ushort)0); // Reserved
                bw.Write((ushort)1); // ICO type
                bw.Write((ushort)bitmaps.Length); // Count

                int offset = 6 + (bitmaps.Length * 16);
                byte[][] pngBytes = new byte[bitmaps.Length][];

                for (int i = 0; i < bitmaps.Length; i++)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmaps[i].Save(ms, ImageFormat.Png);
                        pngBytes[i] = ms.ToArray();
                    }
                }

                for (int i = 0; i < bitmaps.Length; i++)
                {
                    Bitmap b = bitmaps[i];
                    bw.Write((byte)(b.Width >= 256 ? 0 : b.Width));
                    bw.Write((byte)(b.Height >= 256 ? 0 : b.Height));
                    bw.Write((byte)0); // Colors
                    bw.Write((byte)0); // Reserved
                    bw.Write((ushort)1); // Color planes
                    bw.Write((ushort)32); // BPP
                    bw.Write((uint)pngBytes[i].Length); // Size
                    bw.Write((uint)offset); // Offset
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
