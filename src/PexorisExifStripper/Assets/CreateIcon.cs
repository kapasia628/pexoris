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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisExifStripper\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\exif-stripper\exif-stripper.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\exif-stripper.png";

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

                // Vibrant Violet/Purple to Deep Indigo Gradient Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(139, 92, 246),  // Violet 500 (#8B5CF6)
                    Color.FromArgb(109, 40, 217)))  // Purple 700 (#6D28D9)
                {
                    g.FillPath(brush, path);
                }

                // Inner high-tech gloss border
                using (GraphicsPath borderPath = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen p = new Pen(Color.FromArgb(120, 255, 255, 255), size * 0.015f))
                {
                    g.DrawPath(p, borderPath);
                }

                // Camera Body Shape
                float camW = size * 0.60f;
                float camH = size * 0.44f;
                float camX = (size - camW) / 2f;
                float camY = size * 0.32f;
                float camR = size * 0.08f;

                // Camera top flash/viewfinder bump
                float bumpW = camW * 0.38f;
                float bumpH = size * 0.08f;
                float bumpX = camX + (camW - bumpW) / 2f;
                float bumpY = camY - bumpH + 2;

                using (GraphicsPath bumpPath = GetRoundedPath(new RectangleF(bumpX, bumpY, bumpW, bumpH * 2), size * 0.04f))
                using (SolidBrush whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(whiteBrush, bumpPath);
                }

                using (GraphicsPath camPath = GetRoundedPath(new RectangleF(camX, camY, camW, camH), camR))
                using (SolidBrush camBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(camBrush, camPath);
                }

                // Camera Lens (Concentric Circles)
                float lensDiameter = camH * 0.65f;
                float lensX = (size - lensDiameter) / 2f;
                float lensY = camY + (camH - lensDiameter) / 2f;

                using (SolidBrush lensDark = new SolidBrush(Color.FromArgb(76, 29, 149))) // Deep Purple
                {
                    g.FillEllipse(lensDark, lensX, lensY, lensDiameter, lensDiameter);
                }

                float innerLens = lensDiameter * 0.60f;
                float innerX = (size - innerLens) / 2f;
                float innerY = lensY + (lensDiameter - innerLens) / 2f;

                using (SolidBrush lensCyan = new SolidBrush(Color.FromArgb(167, 139, 250))) // Light Purple
                {
                    g.FillEllipse(lensCyan, innerX, innerY, innerLens, innerLens);
                }

                // Flash Dot on Camera
                float flashSize = size * 0.05f;
                float flashX = camX + camW - size * 0.12f;
                float flashY = camY + size * 0.08f;
                using (SolidBrush flashBrush = new SolidBrush(Color.FromArgb(244, 63, 94))) // Rose Dot
                {
                    g.FillEllipse(flashBrush, flashX, flashY, flashSize, flashSize);
                }

                // Privacy Shield / Eraser Slash (Diagonal Clean Cut Badge)
                float slashW = size * 0.065f;
                PointF p1 = new PointF(size * 0.22f, size * 0.78f);
                PointF p2 = new PointF(size * 0.78f, size * 0.22f);

                using (Pen slashShadow = new Pen(Color.FromArgb(100, 30, 10, 60), slashW + size * 0.03f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(slashShadow, p1, p2);
                }

                using (Pen slashPen = new Pen(Color.FromArgb(244, 63, 94), slashW) { StartCap = LineCap.Round, EndCap = LineCap.Round }) // Rose 500
                {
                    g.DrawLine(slashPen, p1, p2);
                }

                // Location GPS Pin Silhouette in the top right corner with a slash
                float pinX = size * 0.74f;
                float pinY = size * 0.62f;
                float pinR = size * 0.14f;

                using (SolidBrush pinBg = new SolidBrush(Color.FromArgb(16, 185, 129))) // Emerald badge
                {
                    g.FillEllipse(pinBg, pinX - pinR, pinY - pinR, pinR * 2, pinR * 2);
                }

                using (Pen pinPen = new Pen(Color.White, size * 0.025f))
                {
                    g.DrawEllipse(pinPen, pinX - pinR * 0.5f, pinY - pinR * 0.5f, pinR, pinR);
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
