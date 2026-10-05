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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisUSBShield\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\usb-shield\usb-shield.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\usb-shield.png";

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

                // Emerald to Teal Gradient Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedRect(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(16, 185, 129),   // #10B981 Emerald 500
                    Color.FromArgb(13, 148, 136)))  // #0D9488 Teal 600
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle glow border
                using (GraphicsPath path = GetRoundedRect(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(170, 255, 255, 255), size * 0.02f))
                {
                    g.DrawPath(pen, path);
                }

                // Security Shield Contour
                float cx = size * 0.5f;
                float topY = size * 0.22f;
                float shW = size * 0.52f;
                float shH = size * 0.56f;

                using (GraphicsPath shield = new GraphicsPath())
                {
                    shield.StartFigure();
                    shield.AddLine(cx - (shW / 2), topY, cx + (shW / 2), topY);
                    shield.AddBezier(
                        cx + (shW / 2), topY,
                        cx + (shW / 2), topY + (shH * 0.45f),
                        cx + (shW * 0.35f), topY + (shH * 0.82f),
                        cx, topY + shH);
                    shield.AddBezier(
                        cx, topY + shH,
                        cx - (shW * 0.35f), topY + (shH * 0.82f),
                        cx - (shW / 2), topY + (shH * 0.45f),
                        cx - (shW / 2), topY);
                    shield.CloseFigure();

                    // Translucent Shield Fill
                    using (SolidBrush shBrush = new SolidBrush(Color.FromArgb(55, 255, 255, 255)))
                    {
                        g.FillPath(shBrush, shield);
                    }
                    using (Pen shPen = new Pen(Color.FromArgb(245, 255, 255, 255), size * 0.038f))
                    {
                        shPen.LineJoin = LineJoin.Round;
                        g.DrawPath(shPen, shield);
                    }
                }

                // USB Trident Vector in Crisp White
                float usbStemTop = size * 0.33f;
                float usbStemBottom = size * 0.63f;
                float usbBranchY = size * 0.46f;
                float usbSpread = size * 0.12f;

                using (Pen usbPen = new Pen(Color.White, size * 0.042f))
                {
                    usbPen.StartCap = LineCap.Round;
                    usbPen.EndCap = LineCap.Round;

                    // Central Trunk
                    g.DrawLine(usbPen, cx, usbStemTop + (size * 0.05f), cx, usbStemBottom);

                    // Left Branch
                    g.DrawLine(usbPen, cx, usbBranchY + (size * 0.05f), cx - usbSpread, usbBranchY);
                    g.DrawLine(usbPen, cx - usbSpread, usbBranchY, cx - usbSpread, usbBranchY - (size * 0.05f));

                    // Right Branch
                    g.DrawLine(usbPen, cx, usbBranchY + (size * 0.02f), cx + usbSpread, usbBranchY - (size * 0.02f));
                    g.DrawLine(usbPen, cx + usbSpread, usbBranchY - (size * 0.02f), cx + usbSpread, usbBranchY - (size * 0.07f));
                }

                // Central Arrow Tip
                using (GraphicsPath arrow = new GraphicsPath())
                {
                    float arrW = size * 0.055f;
                    float arrH = size * 0.065f;
                    arrow.AddPolygon(new PointF[] {
                        new PointF(cx, usbStemTop),
                        new PointF(cx + arrW, usbStemTop + arrH),
                        new PointF(cx - arrW, usbStemTop + arrH)
                    });
                    using (SolidBrush b = new SolidBrush(Color.White))
                    {
                        g.FillPath(b, arrow);
                    }
                }

                // Left Node: Square
                float sqSize = size * 0.07f;
                using (SolidBrush b = new SolidBrush(Color.White))
                {
                    g.FillRectangle(b, cx - usbSpread - (sqSize / 2), (usbBranchY - (size * 0.05f)) - (sqSize / 2), sqSize, sqSize);
                }

                // Right Node: Circle
                float cirSize = size * 0.075f;
                using (SolidBrush b = new SolidBrush(Color.White))
                {
                    g.FillEllipse(b, cx + usbSpread - (cirSize / 2), (usbBranchY - (size * 0.07f)) - (cirSize / 2), cirSize, cirSize);
                }

                // Bottom Base Circle
                float baseCir = size * 0.09f;
                using (SolidBrush b = new SolidBrush(Color.White))
                {
                    g.FillEllipse(b, cx - (baseCir / 2), usbStemBottom - (baseCir / 2), baseCir, baseCir);
                }
            }
            return bmp;
        }

        static GraphicsPath GetRoundedRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void WriteIco(Bitmap[] images, Stream stream)
        {
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                bw.Write((ushort)0);
                bw.Write((ushort)1);
                bw.Write((ushort)images.Length);

                int offset = 6 + (images.Length * 16);
                byte[][] pngBytes = new byte[images.Length][];

                for (int i = 0; i < images.Length; i++)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        images[i].Save(ms, ImageFormat.Png);
                        pngBytes[i] = ms.ToArray();
                    }
                }

                for (int i = 0; i < images.Length; i++)
                {
                    Bitmap img = images[i];
                    bw.Write((byte)(img.Width >= 256 ? 0 : img.Width));
                    bw.Write((byte)(img.Height >= 256 ? 0 : img.Height));
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write((ushort)1);
                    bw.Write((ushort)32);
                    bw.Write((uint)pngBytes[i].Length);
                    bw.Write((uint)offset);

                    offset += pngBytes[i].Length;
                }

                for (int i = 0; i < pngBytes.Length; i++)
                {
                    bw.Write(pngBytes[i]);
                }
            }
        }
    }
}
