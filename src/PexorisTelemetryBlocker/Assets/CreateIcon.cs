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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisTelemetryBlocker\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\telemetry-blocker\telemetry-blocker.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\telemetry-blocker.png";

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

                // Premium Deep Indigo to Royal Violet Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(99, 102, 241),  // Indigo 500 (#6366F1)
                    Color.FromArgb(30, 27, 75)))    // Indigo 950 (#1E1B4B)
                {
                    g.FillPath(brush, path);
                }

                // Inner glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(70, 255, 255, 255), size * 0.015f))
                {
                    g.DrawPath(pen, path);
                }

                // Central Cyber Security Privacy Shield with Radar Wave Neutralization
                float cx = size * 0.5f;
                float cy = size * 0.51f;
                float sw = size * 0.54f;
                float sh = size * 0.62f;

                float sl = cx - (sw * 0.5f);
                float sr = cx + (sw * 0.5f);
                float st = cy - (sh * 0.48f);
                float sm = cy + (sh * 0.06f);
                float sb = cy + (sh * 0.52f);

                using (GraphicsPath shieldPath = new GraphicsPath())
                {
                    shieldPath.AddLine(cx, st, sr, st + (sh * 0.1f));
                    shieldPath.AddLine(sr, st + (sh * 0.1f), sr, sm);
                    shieldPath.AddBezier(new PointF(sr, sm), new PointF(sr, sb * 0.7f), new PointF(cx + sw * 0.15f, sb), new PointF(cx, sb));
                    shieldPath.AddBezier(new PointF(cx, sb), new PointF(cx - sw * 0.15f, sb), new PointF(sl, sb * 0.7f), new PointF(sl, sm));
                    shieldPath.AddLine(sl, sm, sl, st + (sh * 0.1f));
                    shieldPath.CloseFigure();

                    // Shield background: semi-translucent dark slate/indigo
                    using (LinearGradientBrush sBrush = new LinearGradientBrush(
                        new PointF(cx, st),
                        new PointF(cx, sb),
                        Color.FromArgb(235, 15, 23, 42),
                        Color.FromArgb(245, 30, 27, 75)))
                    {
                        g.FillPath(sBrush, shieldPath);
                    }

                    // Shield stroke (cyan to violet accent)
                    using (Pen sPen = new Pen(Color.FromArgb(220, 199, 210, 254), size * 0.024f))
                    {
                        g.DrawPath(sPen, shieldPath);
                    }
                }

                // Radar Telemetry Waves (Arcs) centered inside shield
                float arcCy = cy - (size * 0.04f);
                using (Pen wavePen1 = new Pen(Color.FromArgb(160, 165, 180, 252), size * 0.022f))
                using (Pen wavePen2 = new Pen(Color.FromArgb(210, 224, 231, 255), size * 0.022f))
                {
                    wavePen1.StartCap = LineCap.Round;
                    wavePen1.EndCap = LineCap.Round;
                    wavePen2.StartCap = LineCap.Round;
                    wavePen2.EndCap = LineCap.Round;

                    // Inner telemetry arc
                    float r1 = size * 0.11f;
                    g.DrawArc(wavePen1, cx - r1, arcCy - r1, r1 * 2, r1 * 2, 220, 100);

                    // Outer telemetry arc
                    float r2 = size * 0.19f;
                    g.DrawArc(wavePen2, cx - r2, arcCy - r2, r2 * 2, r2 * 2, 225, 90);
                }

                // Central radar transmitter dot
                float dotR = size * 0.038f;
                using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
                {
                    g.FillEllipse(dotBrush, cx - dotR, arcCy - dotR, dotR * 2, dotR * 2);
                }

                // Powerful Diagonal Block/Slash Line across telemetry
                float slashLen = size * 0.32f;
                using (Pen slashPen = new Pen(Color.FromArgb(255, 244, 63, 94), size * 0.034f)) // Rose/Red-500
                {
                    slashPen.StartCap = LineCap.Round;
                    slashPen.EndCap = LineCap.Round;
                    g.DrawLine(slashPen, cx - (slashLen * 0.5f), cy - (slashLen * 0.4f), cx + (slashLen * 0.5f), cy + (slashLen * 0.4f));
                }

                // Crisp Star Sparkle Accent
                float starX = pad + w * 0.84f;
                float starY = pad + w * 0.16f;
                float starR = size * 0.045f;
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
