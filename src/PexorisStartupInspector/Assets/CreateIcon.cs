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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisStartupInspector\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\startup-inspector\startup-inspector.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\startup-inspector.png";

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

                // Deep Indigo to Electric Violet Gradient Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(79, 70, 229),   // Indigo 600 (#4F46E5)
                    Color.FromArgb(147, 51, 234)))  // Purple 600 (#9333EA)
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255), size * 0.015f))
                {
                    g.DrawPath(pen, path);
                }

                // Visual Artwork: Stylized Startup Rocket / Speedometer Booster + Windows Boot Toggles
                // Rocket body angled upwards at 45 degrees
                float cx = size * 0.5f;
                float cy = size * 0.5f;

                g.TranslateTransform(cx, cy);
                g.RotateTransform(-45f); // Angled up-right

                // Rocket Body (Sleek aerodynamic capsule)
                float rw = size * 0.22f;
                float rh = size * 0.44f;

                // Subtle rocket shadow
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                {
                    g.FillEllipse(shadowBrush, -rw * 0.5f + 4f, -rh * 0.5f + 4f, rw, rh);
                }

                // Main Rocket Body (Crisp Pure White)
                using (GraphicsPath rocketBody = new GraphicsPath())
                {
                    // Nose cone to streamlined tail
                    rocketBody.AddArc(-rw * 0.5f, -rh * 0.5f, rw, rw, 180, 180);
                    rocketBody.AddLine(rw * 0.5f, 0, rw * 0.4f, rh * 0.4f);
                    rocketBody.AddLine(rw * 0.4f, rh * 0.4f, -rw * 0.4f, rh * 0.4f);
                    rocketBody.AddLine(-rw * 0.4f, rh * 0.4f, -rw * 0.5f, 0);
                    rocketBody.CloseFigure();

                    using (SolidBrush bodyBrush = new SolidBrush(Color.White))
                    {
                        g.FillPath(bodyBrush, rocketBody);
                    }
                }

                // Side Fins (Cyan / Electric Blue accents)
                using (GraphicsPath leftFin = new GraphicsPath())
                using (GraphicsPath rightFin = new GraphicsPath())
                using (SolidBrush finBrush = new SolidBrush(Color.FromArgb(56, 189, 248))) // Sky 400
                {
                    leftFin.AddPolygon(new PointF[] {
                        new PointF(-rw * 0.4f, rh * 0.1f),
                        new PointF(-rw * 0.85f, rh * 0.42f),
                        new PointF(-rw * 0.35f, rh * 0.38f)
                    });
                    rightFin.AddPolygon(new PointF[] {
                        new PointF(rw * 0.4f, rh * 0.1f),
                        new PointF(rw * 0.85f, rh * 0.42f),
                        new PointF(rw * 0.35f, rh * 0.38f)
                    });
                    g.FillPath(finBrush, leftFin);
                    g.FillPath(finBrush, rightFin);
                }

                // Porthole Window (Indigo glass)
                float portR = rw * 0.35f;
                using (SolidBrush portBrush = new SolidBrush(Color.FromArgb(99, 102, 241))) // Indigo 500
                using (Pen portPen = new Pen(Color.FromArgb(224, 231, 255), size * 0.015f))
                {
                    g.FillEllipse(portBrush, -portR, -rh * 0.22f - portR, portR * 2, portR * 2);
                    g.DrawEllipse(portPen, -portR, -rh * 0.22f - portR, portR * 2, portR * 2);
                }

                // Exhaust Thrust Flames (Vibrant Amber & Yellow)
                using (GraphicsPath flameOuter = new GraphicsPath())
                using (SolidBrush flameBrush = new SolidBrush(Color.FromArgb(245, 158, 11))) // Amber 500
                {
                    flameOuter.AddPolygon(new PointF[] {
                        new PointF(-rw * 0.3f, rh * 0.4f),
                        new PointF(0, rh * 0.72f),
                        new PointF(rw * 0.3f, rh * 0.4f)
                    });
                    g.FillPath(flameBrush, flameOuter);
                }

                using (GraphicsPath flameCore = new GraphicsPath())
                using (SolidBrush coreBrush = new SolidBrush(Color.FromArgb(253, 224, 71))) // Yellow 300
                {
                    flameCore.AddPolygon(new PointF[] {
                        new PointF(-rw * 0.15f, rh * 0.4f),
                        new PointF(0, rh * 0.60f),
                        new PointF(rw * 0.15f, rh * 0.4f)
                    });
                    g.FillPath(coreBrush, flameCore);
                }

                g.ResetTransform();

                // Speed streaks / inspection sparkle at top right
                using (Pen streakPen = new Pen(Color.FromArgb(180, 255, 255, 255), size * 0.02f))
                {
                    streakPen.StartCap = LineCap.Round;
                    streakPen.EndCap = LineCap.Round;
                    g.DrawLine(streakPen, size * 0.72f, size * 0.22f, size * 0.82f, size * 0.12f);
                    g.DrawLine(streakPen, size * 0.82f, size * 0.34f, size * 0.89f, size * 0.27f);
                    g.DrawLine(streakPen, size * 0.58f, size * 0.14f, size * 0.65f, size * 0.07f);
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
