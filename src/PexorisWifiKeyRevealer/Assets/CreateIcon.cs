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
            string toolPngPath = @"c:\xampp\htdocs\tools\website\tools\wifi-key-revealer\wifi-key-revealer.png";
            string assetPngPath = @"c:\xampp\htdocs\tools\website\assets\icons\wifi-key-revealer.png";

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

                // Squircle background with Cyan / Teal / Emerald Gradient
                RectangleF rect = new RectangleF(10 * scale, 10 * scale, 236 * scale, 236 * scale);
                using (GraphicsPath path = GetSquircle(rect, 54 * scale))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(14, 165, 233),   // #0EA5E9 Sky 500
                        Color.FromArgb(13, 148, 136),   // #0D9488 Teal 600
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

                // Wi-Fi Signal Arcs
                float cx = 128f * scale;
                float cy = 168f * scale;

                // Arc 1 (Outer)
                float r1 = 92f * scale;
                using (Pen arcPen = new Pen(Color.White, 16f * scale))
                {
                    arcPen.StartCap = LineCap.Round;
                    arcPen.EndCap = LineCap.Round;
                    g.DrawArc(arcPen, cx - r1, cy - r1, r1 * 2, r1 * 2, 225, 90);

                    // Arc 2 (Middle)
                    float r2 = 62f * scale;
                    g.DrawArc(arcPen, cx - r2, cy - r2, r2 * 2, r2 * 2, 225, 90);

                    // Arc 3 (Inner)
                    float r3 = 34f * scale;
                    g.DrawArc(arcPen, cx - r3, cy - r3, r3 * 2, r3 * 2, 225, 90);
                }

                // Base dot
                using (SolidBrush dotBrush = new SolidBrush(Color.White))
                {
                    float dotR = 12f * scale;
                    g.FillEllipse(dotBrush, cx - dotR, cy - dotR + 8f * scale, dotR * 2, dotR * 2);
                }

                // Floating Golden Key Emblem at top right
                float kx = 165f * scale;
                float ky = 70f * scale;
                float keyR = 24f * scale;

                // Golden Key Ring
                using (Pen keyPen = new Pen(Color.FromArgb(245, 158, 11), 10f * scale))
                {
                    g.DrawEllipse(keyPen, kx - keyR, ky - keyR, keyR * 2, keyR * 2);
                }
                using (Pen keyInnerPen = new Pen(Color.FromArgb(254, 240, 138), 3f * scale))
                {
                    g.DrawEllipse(keyInnerPen, kx - keyR, ky - keyR, keyR * 2, keyR * 2);
                }

                // Key Shaft pointing down-left
                using (Pen shaftPen = new Pen(Color.FromArgb(245, 158, 11), 9f * scale))
                {
                    shaftPen.StartCap = LineCap.Round;
                    shaftPen.EndCap = LineCap.Round;
                    g.DrawLine(shaftPen, kx - 16f * scale, ky + 16f * scale, kx - 52f * scale, ky + 52f * scale);

                    // Key Teeth
                    g.DrawLine(shaftPen, kx - 34f * scale, ky + 34f * scale, kx - 42f * scale, ky + 26f * scale);
                    g.DrawLine(shaftPen, kx - 46f * scale, ky + 46f * scale, kx - 54f * scale, ky + 38f * scale);
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
