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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisRamTrimmer\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\ram-trimmer\ram-trimmer.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\ram-trimmer.png";

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

                // Vibrant Emerald Green to Deep Forest Slate Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(16, 185, 129),   // Emerald 500 (#10B981)
                    Color.FromArgb(4, 120, 87)))     // Emerald 700 (#047857)
                {
                    g.FillPath(brush, path);
                }

                // Inner high-tech gloss border
                using (GraphicsPath borderPath = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen p = new Pen(Color.FromArgb(120, 255, 255, 255), size * 0.015f))
                {
                    g.DrawPath(p, borderPath);
                }

                // RAM Chip Stick Graphic
                float ramW = size * 0.64f;
                float ramH = size * 0.38f;
                float ramX = (size - ramW) / 2f;
                float ramY = (size - ramH) / 2f;
                float chipR = size * 0.05f;

                // PCB Body (Deep Teal/Dark Emerald)
                using (GraphicsPath pcbPath = GetRoundedPath(new RectangleF(ramX, ramY, ramW, ramH), chipR))
                using (SolidBrush pcbBrush = new SolidBrush(Color.FromArgb(6, 78, 59))) // Deep emerald PCB
                using (Pen pcbPen = new Pen(Color.FromArgb(209, 250, 229), size * 0.02f))
                {
                    g.FillPath(pcbBrush, pcbPath);
                    g.DrawPath(pcbPen, pcbPath);
                }

                // Gold Pins at the bottom of the RAM stick
                int pinCount = 7;
                float pinW = size * 0.045f;
                float pinH = size * 0.07f;
                float pinSpacing = (ramW - (pinCount * pinW)) / (pinCount + 1);

                for (int i = 0; i < pinCount; i++)
                {
                    float px = ramX + pinSpacing + i * (pinW + pinSpacing);
                    float py = ramY + ramH - (pinH * 0.6f);
                    using (SolidBrush pinBrush = new SolidBrush(Color.FromArgb(251, 191, 36))) // Gold pin
                    {
                        g.FillRectangle(pinBrush, px, py, pinW, pinH);
                    }
                }

                // 3 Memory IC Chips on the PCB
                int chipCount = 3;
                float icW = size * 0.13f;
                float icH = size * 0.18f;
                float icSpacing = (ramW - (chipCount * icW)) / (chipCount + 1);
                float icY = ramY + size * 0.06f;

                for (int i = 0; i < chipCount; i++)
                {
                    float cx = ramX + icSpacing + i * (icW + icSpacing);
                    using (GraphicsPath icPath = GetRoundedPath(new RectangleF(cx, icY, icW, icH), size * 0.02f))
                    using (SolidBrush icBrush = new SolidBrush(Color.FromArgb(15, 23, 42))) // Dark Slate chip
                    using (Pen icPen = new Pen(Color.FromArgb(52, 211, 153), size * 0.012f))
                    {
                        g.FillPath(icBrush, icPath);
                        g.DrawPath(icPen, icPath);
                    }
                }

                // High-Speed Lightning Spark / Optimization Wave across the front
                using (GraphicsPath boltPath = new GraphicsPath())
                {
                    boltPath.AddPolygon(new PointF[]
                    {
                        new PointF(size * 0.66f, size * 0.20f),
                        new PointF(size * 0.50f, size * 0.52f),
                        new PointF(size * 0.60f, size * 0.52f),
                        new PointF(size * 0.44f, size * 0.82f),
                        new PointF(size * 0.56f, size * 0.46f),
                        new PointF(size * 0.46f, size * 0.46f)
                    });

                    // Glow behind bolt
                    using (Pen glowPen = new Pen(Color.FromArgb(100, 255, 255, 255), size * 0.06f) { LineJoin = LineJoin.Round })
                    {
                        g.DrawPath(glowPen, boltPath);
                    }

                    using (SolidBrush boltBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                    {
                        g.FillPath(boltBrush, boltPath);
                    }
                }
            }
            return bmp;
        }

        static GraphicsPath GetRoundedPath(RectangleF rect, float radius)
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

        static void WriteIco(Bitmap[] bitmaps, Stream stream)
        {
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                bw.Write((short)0); // reserved
                bw.Write((short)1); // type 1 = icon
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

                    int bWidth = bitmaps[i].Width >= 256 ? 0 : bitmaps[i].Width;
                    int bHeight = bitmaps[i].Height >= 256 ? 0 : bitmaps[i].Height;

                    bw.Write((byte)bWidth);
                    bw.Write((byte)bHeight);
                    bw.Write((byte)0); // colors
                    bw.Write((byte)0); // reserved
                    bw.Write((short)1); // planes
                    bw.Write((short)32); // bit count
                    bw.Write(pngBytes[i].Length); // bytes in resource
                    bw.Write(offset); // image offset

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
