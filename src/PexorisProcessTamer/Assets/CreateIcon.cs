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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisProcessTamer\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\process-tamer\process-tamer.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\process-tamer.png";

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

                // Luxurious Fire Orange to Deep Slate Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(249, 115, 22),   // Orange 500 (#F97316)
                    Color.FromArgb(15, 23, 42)))     // Slate 900 (#0F172A)
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle amber/gold glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(90, 253, 186, 116), size * 0.015f)) // Orange 300
                {
                    g.DrawPath(pen, path);
                }

                // Artwork Coordinates
                float cx = size * 0.5f;
                float cy = size * 0.5f;

                // Central CPU Chip Base Plate (Dark Slate Blue)
                float chipSize = size * 0.52f;
                RectangleF chipRect = new RectangleF(cx - chipSize * 0.5f, cy - chipSize * 0.5f, chipSize, chipSize);
                using (GraphicsPath chipPath = GetRoundedPath(chipRect, size * 0.05f))
                {
                    // Chip Shadow
                    using (Matrix m = new Matrix())
                    {
                        m.Translate(0, size * 0.03f);
                        using (GraphicsPath shadowPath = (GraphicsPath)chipPath.Clone())
                        {
                            shadowPath.Transform(m);
                            using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                            {
                                g.FillPath(sBrush, shadowPath);
                            }
                        }
                    }

                    using (SolidBrush chipBrush = new SolidBrush(Color.FromArgb(30, 41, 59))) // Slate 800
                    using (Pen chipPen = new Pen(Color.FromArgb(51, 65, 85), size * 0.02f))
                    {
                        g.FillPath(chipBrush, chipPath);
                        g.DrawPath(chipPen, chipPath);
                    }
                }

                // Chip Pins (Gold Contact Pins on 4 sides)
                using (SolidBrush pinBrush = new SolidBrush(Color.FromArgb(251, 191, 36))) // Amber 400
                {
                    float pinW = size * 0.035f;
                    float pinL = size * 0.045f;
                    for (int i = -2; i <= 2; i++)
                    {
                        float offset = i * (size * 0.08f);
                        // Top pins
                        g.FillRectangle(pinBrush, cx + offset - pinW * 0.5f, chipRect.Top - pinL, pinW, pinL);
                        // Bottom pins
                        g.FillRectangle(pinBrush, cx + offset - pinW * 0.5f, chipRect.Bottom, pinW, pinL);
                        // Left pins
                        g.FillRectangle(pinBrush, chipRect.Left - pinL, cy + offset - pinW * 0.5f, pinL, pinW);
                        // Right pins
                        g.FillRectangle(pinBrush, chipRect.Right, cy + offset - pinW * 0.5f, pinL, pinW);
                    }
                }

                // Center Tachometer / CPU Governor Dial Gauge
                float dialR = size * 0.19f;
                RectangleF dialRect = new RectangleF(cx - dialR, cy - dialR, dialR * 2, dialR * 2);

                // Dial Arc (Cool Cyan to Hot Amber Gauge)
                using (Pen gaugePen = new Pen(Color.FromArgb(226, 232, 240), size * 0.03f))
                {
                    gaugePen.StartCap = LineCap.Round;
                    gaugePen.EndCap = LineCap.Round;
                    g.DrawArc(gaugePen, dialRect, 135, 270);
                }

                using (Pen activePen = new Pen(Color.FromArgb(249, 115, 22), size * 0.038f)) // Orange
                {
                    activePen.StartCap = LineCap.Round;
                    activePen.EndCap = LineCap.Round;
                    g.DrawArc(activePen, dialRect, 135, 175);
                }

                // Tachometer Needle (Crisp White Governor Needle pointing precisely)
                using (GraphicsPath needle = new GraphicsPath())
                {
                    float nLen = dialR * 0.95f;
                    float rad = (float)(240 * Math.PI / 180.0);
                    float nx = cx + (float)(Math.Cos(rad) * nLen);
                    float ny = cy + (float)(Math.Sin(rad) * nLen);

                    needle.AddLine(cx - size * 0.02f, cy, nx, ny);
                    needle.AddLine(cx + size * 0.02f, cy, nx, ny);
                    needle.CloseFigure();

                    using (SolidBrush needleBrush = new SolidBrush(Color.White))
                    using (Pen nPen = new Pen(Color.FromArgb(254, 215, 170), size * 0.015f))
                    {
                        g.FillPath(needleBrush, needle);
                        g.DrawPath(nPen, needle);
                    }
                }

                // Central Needle Pivot Hub (Orange Dot with White Ring)
                float hubR = size * 0.045f;
                using (SolidBrush hubBrush = new SolidBrush(Color.FromArgb(234, 88, 12))) // Orange 600
                using (Pen hubPen = new Pen(Color.White, size * 0.02f))
                {
                    g.FillEllipse(hubBrush, cx - hubR, cy - hubR, hubR * 2, hubR * 2);
                    g.DrawEllipse(hubPen, cx - hubR, cy - hubR, hubR * 2, hubR * 2);
                }

                // Speed sparks / Throttle Energy particles
                using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                {
                    g.FillEllipse(dotBrush, size * 0.77f, size * 0.19f, size * 0.04f, size * 0.04f);
                    g.FillEllipse(dotBrush, size * 0.83f, size * 0.28f, size * 0.025f, size * 0.025f);
                    g.FillEllipse(dotBrush, size * 0.18f, size * 0.76f, size * 0.035f, size * 0.035f);
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
