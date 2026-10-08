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
            string toolPngPath = @"c:\xampp\htdocs\tools\website\tools\winupdate-reset\winupdate-reset.png";
            string assetPngPath = @"c:\xampp\htdocs\tools\website\assets\icons\winupdate-reset.png";

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

                // Squircle background with Indigo / Deep Azure Gradient
                RectangleF rect = new RectangleF(10 * scale, 10 * scale, 236 * scale, 236 * scale);
                using (GraphicsPath path = GetSquircle(rect, 54 * scale))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(99, 102, 241),   // #6366F1 Indigo 500
                        Color.FromArgb(67, 56, 202),    // #4338CA Indigo 700
                        LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(brush, path);
                    }

                    // Inner border subtle glow
                    using (Pen borderPen = new Pen(Color.FromArgb(60, 255, 255, 255), 2.5f * scale))
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                // Inner Circular Glow Ring (representing Windows Update loop)
                float cx = 128f * scale;
                float cy = 128f * scale;
                float ringR = 64f * scale;

                using (Pen ringPen = new Pen(Color.FromArgb(255, 255, 255), 18f * scale))
                {
                    ringPen.StartCap = LineCap.Round;
                    ringPen.EndCap = LineCap.Round;
                    // Upper-right arc
                    g.DrawArc(ringPen, cx - ringR, cy - ringR, ringR * 2, ringR * 2, 205, 125);
                    // Lower-left arc
                    g.DrawArc(ringPen, cx - ringR, cy - ringR, ringR * 2, ringR * 2, 25, 125);
                }

                // Arrow Heads on the arcs
                // Arrow 1 at bottom right (pointing up-right)
                using (SolidBrush whiteBrush = new SolidBrush(Color.White))
                {
                    PointF[] arrow1 = new PointF[] {
                        new PointF(cx + 64f * scale, cy + 28f * scale),
                        new PointF(cx + 42f * scale, cy + 62f * scale),
                        new PointF(cx + 78f * scale, cy + 54f * scale)
                    };
                    g.FillPolygon(whiteBrush, arrow1);

                    // Arrow 2 at top left (pointing down-left)
                    PointF[] arrow2 = new PointF[] {
                        new PointF(cx - 64f * scale, cy - 28f * scale),
                        new PointF(cx - 42f * scale, cy - 62f * scale),
                        new PointF(cx - 78f * scale, cy - 54f * scale)
                    };
                    g.FillPolygon(whiteBrush, arrow2);
                }

                // Central Lightning / Reset Bolt (Golden Emerald accent)
                PointF[] bolt = new PointF[] {
                    new PointF(cx + 4f * scale, cy - 38f * scale),
                    new PointF(cx - 22f * scale, cy + 4f * scale),
                    new PointF(cx - 2f * scale, cy + 4f * scale),
                    new PointF(cx - 6f * scale, cy + 38f * scale),
                    new PointF(cx + 22f * scale, cy - 4f * scale),
                    new PointF(cx + 2f * scale, cy - 4f * scale)
                };
                using (LinearGradientBrush boltBrush = new LinearGradientBrush(
                    new RectangleF(cx - 22f * scale, cy - 38f * scale, 44f * scale, 76f * scale),
                    Color.FromArgb(254, 240, 138),  // #FEF08A Light Yellow
                    Color.FromArgb(245, 158, 11),   // #F59E0B Amber
                    LinearGradientMode.Vertical))
                {
                    g.FillPolygon(boltBrush, bolt);
                }

                using (Pen boltPen = new Pen(Color.FromArgb(180, 83, 9), 2f * scale))
                {
                    g.DrawPolygon(boltPen, bolt);
                }
            }
            return bmp;
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
