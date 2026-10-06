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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisDNSFlusher\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\dns-flusher\dns-flusher.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\dns-flusher.png";

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

                // Vibrant Cyan-Teal to Deep Electric Blue Gradient Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(6, 182, 212),    // Cyan 500 (#06B6D4)
                    Color.FromArgb(29, 78, 216)))   // Blue 700 (#1D4ED8)
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255), size * 0.015f))
                {
                    g.DrawPath(pen, path);
                }

                // Visual Artwork: Stylized Network Globe / DNS Nodes + Rapid Refresh Flush Vortex
                float cx = size * 0.5f;
                float cy = size * 0.5f;
                float globeR = size * 0.26f;

                // Globe Drop Shadow
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                {
                    g.FillEllipse(shadowBrush, cx - globeR + 3, cy - globeR + 4, globeR * 2, globeR * 2);
                }

                // Network Globe Outer Ring
                using (SolidBrush globeBg = new SolidBrush(Color.FromArgb(240, 249, 255))) // Sky 50
                using (Pen globePen = new Pen(Color.White, size * 0.025f))
                {
                    g.FillEllipse(globeBg, cx - globeR, cy - globeR, globeR * 2, globeR * 2);
                    g.DrawEllipse(globePen, cx - globeR, cy - globeR, globeR * 2, globeR * 2);
                }

                // Globe Latitudes & Longitudes (Network Wireframe)
                using (Pen gridPen = new Pen(Color.FromArgb(186, 230, 253), size * 0.018f)) // Sky 200
                {
                    // Equator
                    g.DrawLine(gridPen, cx - globeR, cy, cx + globeR, cy);
                    // Longitude vertical ellipse
                    g.DrawEllipse(gridPen, cx - globeR * 0.45f, cy - globeR, globeR * 0.90f, globeR * 2);
                }

                // Rapid Dynamic Flush Arrows (Circular Vortex around Globe)
                float arrowR = size * 0.35f;
                using (Pen vortexPen = new Pen(Color.White, size * 0.035f))
                {
                    vortexPen.StartCap = LineCap.Round;
                    vortexPen.EndCap = LineCap.Round;

                    // Top-right sweep arc
                    g.DrawArc(vortexPen, cx - arrowR, cy - arrowR, arrowR * 2, arrowR * 2, -60, 110);
                    // Bottom-left sweep arc
                    g.DrawArc(vortexPen, cx - arrowR, cy - arrowR, arrowR * 2, arrowR * 2, 120, 110);
                }

                // Arrow Heads
                // Top Right Arrow Head
                float a1X = cx + arrowR * 0.90f;
                float a1Y = cy - arrowR * 0.38f;
                using (GraphicsPath head1 = new GraphicsPath())
                using (SolidBrush arrowBrush = new SolidBrush(Color.White))
                {
                    head1.AddPolygon(new PointF[] {
                        new PointF(a1X, a1Y - size * 0.04f),
                        new PointF(a1X + size * 0.04f, a1Y + size * 0.04f),
                        new PointF(a1X - size * 0.04f, a1Y + size * 0.02f)
                    });
                    g.FillPath(arrowBrush, head1);
                }

                // Bottom Left Arrow Head
                float a2X = cx - arrowR * 0.90f;
                float a2Y = cy + arrowR * 0.38f;
                using (GraphicsPath head2 = new GraphicsPath())
                using (SolidBrush arrowBrush = new SolidBrush(Color.White))
                {
                    head2.AddPolygon(new PointF[] {
                        new PointF(a2X, a2Y + size * 0.04f),
                        new PointF(a2X - size * 0.04f, a2Y - size * 0.04f),
                        new PointF(a2X + size * 0.04f, a2Y - size * 0.02f)
                    });
                    g.FillPath(arrowBrush, head2);
                }

                // Core Lightning / High-speed DNS packet spark in center of globe
                using (GraphicsPath coreBolt = new GraphicsPath())
                using (SolidBrush boltBrush = new SolidBrush(Color.FromArgb(2, 132, 199))) // Sky 600
                {
                    float bw = size * 0.08f;
                    float bh = size * 0.16f;
                    coreBolt.AddPolygon(new PointF[] {
                        new PointF(cx + bw * 0.1f, cy - bh * 0.5f),
                        new PointF(cx - bw * 0.5f, cy + bh * 0.05f),
                        new PointF(cx - bw * 0.05f, cy + bh * 0.05f),
                        new PointF(cx - bw * 0.15f, cy + bh * 0.5f),
                        new PointF(cx + bw * 0.5f, cy - bh * 0.05f),
                        new PointF(cx + bw * 0.05f, cy - bh * 0.05f)
                    });
                    g.FillPath(boltBrush, coreBolt);
                }

                // Sparkle indicator dots
                using (SolidBrush sparkBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                {
                    g.FillEllipse(sparkBrush, size * 0.20f, size * 0.22f, size * 0.04f, size * 0.04f);
                    g.FillEllipse(sparkBrush, size * 0.78f, size * 0.76f, size * 0.045f, size * 0.045f);
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
