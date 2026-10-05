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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisHostsManager\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\hosts-manager\hosts-manager.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\hosts-manager.png";

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

                // Deep Indigo to Royal Blue Gradient Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(99, 102, 241),  // Indigo 500
                    Color.FromArgb(30, 27, 75)))   // Indigo 950
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255), size * 0.015f))
                {
                    g.DrawPath(pen, path);
                }

                // Visual Artwork: Stylized Network Server Rack / Hosts Node with DNS Routing
                float rackX = size * 0.22f;
                float rackY = size * 0.20f;
                float rackW = size * 0.56f;
                float rackH = size * 0.60f;
                float rackR = size * 0.06f;

                // Server Rack Drop Shadow
                using (GraphicsPath shadowPath = GetRoundedPath(new RectangleF(rackX, rackY + size * 0.03f, rackW, rackH), rackR))
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Server Rack Body
                using (GraphicsPath rackPath = GetRoundedPath(new RectangleF(rackX, rackY, rackW, rackH), rackR))
                using (SolidBrush rackBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (Pen rackBorder = new Pen(Color.FromArgb(203, 213, 225), size * 0.015f))
                {
                    g.FillPath(rackBg, rackPath);
                    g.DrawPath(rackBorder, rackPath);
                }

                // Server Bay 1 (Top unit)
                float bayPadX = rackX + size * 0.04f;
                float bayW = rackW - size * 0.08f;
                float bayH = size * 0.12f;
                float bay1Y = rackY + size * 0.06f;

                using (GraphicsPath bay1Path = GetRoundedPath(new RectangleF(bayPadX, bay1Y, bayW, bayH), size * 0.03f))
                using (SolidBrush bayBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (Pen bayPen = new Pen(Color.FromArgb(226, 232, 240), size * 0.01f))
                {
                    g.FillPath(bayBrush, bay1Path);
                    g.DrawPath(bayPen, bay1Path);
                }

                // LED indicators on Bay 1
                using (SolidBrush ledGreen = new SolidBrush(Color.FromArgb(16, 185, 129)))
                using (SolidBrush ledBlue = new SolidBrush(Color.FromArgb(59, 130, 246)))
                {
                    g.FillEllipse(ledGreen, bayPadX + size * 0.03f, bay1Y + bayH * 0.35f, size * 0.035f, size * 0.035f);
                    g.FillEllipse(ledBlue, bayPadX + size * 0.08f, bay1Y + bayH * 0.35f, size * 0.035f, size * 0.035f);
                }

                // Network Line / Data stream bars on Bay 1
                using (Pen dataPen = new Pen(Color.FromArgb(148, 163, 184), size * 0.018f))
                {
                    dataPen.StartCap = LineCap.Round;
                    dataPen.EndCap = LineCap.Round;
                    g.DrawLine(dataPen, bayPadX + size * 0.14f, bay1Y + bayH * 0.5f, bayPadX + bayW - size * 0.04f, bay1Y + bayH * 0.5f);
                }

                // Server Bay 2 (Middle unit)
                float bay2Y = bay1Y + size * 0.15f;
                using (GraphicsPath bay2Path = GetRoundedPath(new RectangleF(bayPadX, bay2Y, bayW, bayH), size * 0.03f))
                using (SolidBrush bayBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (Pen bayPen = new Pen(Color.FromArgb(226, 232, 240), size * 0.01f))
                {
                    g.FillPath(bayBrush, bay2Path);
                    g.DrawPath(bayPen, bay2Path);
                }

                using (SolidBrush ledGreen = new SolidBrush(Color.FromArgb(16, 185, 129)))
                {
                    g.FillEllipse(ledGreen, bayPadX + size * 0.03f, bay2Y + bayH * 0.35f, size * 0.035f, size * 0.035f);
                }
                using (Pen dataPen = new Pen(Color.FromArgb(148, 163, 184), size * 0.018f))
                {
                    dataPen.StartCap = LineCap.Round;
                    dataPen.EndCap = LineCap.Round;
                    g.DrawLine(dataPen, bayPadX + size * 0.10f, bay2Y + bayH * 0.5f, bayPadX + bayW - size * 0.10f, bay2Y + bayH * 0.5f);
                }

                // Server Bay 3 (Bottom unit)
                float bay3Y = bay2Y + size * 0.15f;
                using (GraphicsPath bay3Path = GetRoundedPath(new RectangleF(bayPadX, bay33(bay3Y), bayW, bayH), size * 0.03f))
                using (SolidBrush bayBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (Pen bayPen = new Pen(Color.FromArgb(226, 232, 240), size * 0.01f))
                {
                    g.FillPath(bayBrush, bay3Path);
                    g.DrawPath(bayPen, bay3Path);
                }
                using (SolidBrush ledPurple = new SolidBrush(Color.FromArgb(139, 92, 246)))
                {
                    g.FillEllipse(ledPurple, bayPadX + size * 0.03f, bay3Y + bayH * 0.35f, size * 0.035f, size * 0.035f);
                }
                using (Pen dataPen = new Pen(Color.FromArgb(148, 163, 184), size * 0.018f))
                {
                    dataPen.StartCap = LineCap.Round;
                    dataPen.EndCap = LineCap.Round;
                    g.DrawLine(dataPen, bayPadX + size * 0.10f, bay3Y + bayH * 0.5f, bayPadX + bayW - size * 0.04f, bay3Y + bayH * 0.5f);
                }

                // DNS Shield Badge (Bottom Right Overlay)
                float badgeX = size * 0.56f;
                float badgeY = size * 0.52f;
                float badgeS = size * 0.36f;

                // Shield badge shadow
                using (SolidBrush sShadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                {
                    g.FillEllipse(sShadow, badgeX, badgeY + size * 0.02f, badgeS, badgeS);
                }

                // Shield Badge circle background: Emerald Green gradient
                using (LinearGradientBrush sBg = new LinearGradientBrush(
                    new PointF(badgeX, badgeY),
                    new PointF(badgeX + badgeS, badgeY + badgeS),
                    Color.FromArgb(16, 185, 129),  // Emerald 500
                    Color.FromArgb(5, 150, 105)))  // Emerald 600
                using (Pen sPen = new Pen(Color.White, size * 0.025f))
                {
                    g.FillEllipse(sBg, badgeX, badgeY, badgeS, badgeS);
                    g.DrawEllipse(sPen, badgeX, badgeY, badgeS, badgeS);
                }

                // Clean Checkmark / Shield glyph inside badge
                using (Pen checkPen = new Pen(Color.White, size * 0.035f))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    checkPen.LineJoin = LineJoin.Round;

                    float cx = badgeX + badgeS * 0.28f;
                    float cy = badgeY + badgeS * 0.52f;
                    PointF[] checkPts = new PointF[] {
                        new PointF(cx, cy),
                        new PointF(cx + badgeS * 0.18f, cy + badgeS * 0.18f),
                        new PointF(cx + badgeS * 0.46f, cy - badgeS * 0.18f)
                    };
                    g.DrawLines(checkPen, checkPts);
                }
            }
            return bmp;
        }

        static float bay33(float y) { return y; }

        static GraphicsPath GetRoundedPath(RectangleF r, float radius)
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
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)images.Length);

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
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write((uint)pngBytes[i].Length);
                writer.Write((uint)offset);

                offset += pngBytes[i].Length;
            }

            for (int i = 0; i < images.Length; i++)
            {
                writer.Write(pngBytes[i]);
            }
        }
    }
}
