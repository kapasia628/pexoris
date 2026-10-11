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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisFontCacheRebuilder\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\font-cache-rebuilder\font-cache-rebuilder.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\font-cache-rebuilder.png";

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

                // Deep Purple / Violet to Dark Slate Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(124, 58, 237),  // Purple 600 (#7C3AED)
                    Color.FromArgb(30, 27, 75)))    // Dark Indigo 950 (#1E1B4B)
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(100, 196, 181, 253), size * 0.015f)) // Purple 300
                {
                    g.DrawPath(pen, path);
                }

                float cx = size * 0.5f;
                float cy = size * 0.5f;

                // Circular Rebuild / Refresh Ribbon behind typography
                float arcR = size * 0.34f;
                RectangleF arcRect = new RectangleF(cx - arcR, cy - arcR + size * 0.01f, arcR * 2, arcR * 2);
                using (Pen arcPen = new Pen(Color.FromArgb(140, 56, 189, 248), size * 0.055f)) // Sky 400
                {
                    arcPen.StartCap = LineCap.Round;
                    arcPen.EndCap = LineCap.Round;
                    g.DrawArc(arcPen, arcRect, 205, 230);
                }

                // Arrow head on the arc
                float arrowX = cx + arcR * (float)Math.Cos(205.0 * Math.PI / 180.0);
                float arrowY = (cy + size * 0.01f) + arcR * (float)Math.Sin(205.0 * Math.PI / 180.0);
                PointF[] arrowPts = new PointF[]
                {
                    new PointF(arrowX, arrowY),
                    new PointF(arrowX - size * 0.06f, arrowY - size * 0.04f),
                    new PointF(arrowX - size * 0.01f, arrowY - size * 0.08f)
                };
                using (SolidBrush arrowBrush = new SolidBrush(Color.FromArgb(220, 56, 189, 248)))
                {
                    g.FillPolygon(arrowBrush, arrowPts);
                }

                // Draw Majestic Typography Letter "A" (Serif)
                using (Font font = new Font("Georgia", size * 0.44f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    // Drop Shadow for "A"
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                    {
                        g.DrawString("A", font, shadowBrush, new RectangleF(0, size * 0.04f, size, size), sf);
                    }

                    // Gold / Brilliant White Gradient fill for "A"
                    using (LinearGradientBrush textBrush = new LinearGradientBrush(
                        new PointF(0, size * 0.25f),
                        new PointF(0, size * 0.75f),
                        Color.FromArgb(255, 255, 255),
                        Color.FromArgb(243, 232, 255))) // Purple 100
                    {
                        g.DrawString("A", font, textBrush, new RectangleF(0, 0, size, size), sf);
                    }

                    // Golden contour outline
                    using (GraphicsPath textPath = new GraphicsPath())
                    {
                        textPath.AddString("A", font.FontFamily, (int)font.Style, font.Size, new RectangleF(0, 0, size, size), sf);
                        using (Pen outlinePen = new Pen(Color.FromArgb(180, 216, 180, 254), size * 0.015f)) // Purple 300
                        {
                            g.DrawPath(outlinePen, textPath);
                        }
                    }
                }

                // Sparkling Star Accent at top-right
                float starX = cx + size * 0.26f;
                float starY = cy - size * 0.24f;
                float starR = size * 0.07f;
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
                using (SolidBrush starBrush = new SolidBrush(Color.FromArgb(245, 240, 253, 250)))
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
