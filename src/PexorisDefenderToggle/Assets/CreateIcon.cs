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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisDefenderToggle\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\defender-toggle\defender-toggle.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\defender-toggle.png";

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

                // Vibrant Crimson-Rose to Deep Slate Blue Gradient Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(225, 29, 72),    // Rose 600 (#E11D48)
                    Color.FromArgb(30, 41, 59)))     // Slate 800 (#1E293B)
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255), size * 0.015f))
                {
                    g.DrawPath(pen, path);
                }

                // Visual Artwork: Security Shield + Toggle Switch / Lightning Speed Bolt
                float cx = size * 0.5f;
                float cy = size * 0.48f;
                float sw = size * 0.54f;
                float sh = size * 0.60f;

                // Shield Path
                using (GraphicsPath shield = new GraphicsPath())
                {
                    float topY = cy - (sh * 0.48f);
                    float midY = cy;
                    float botY = cy + (sh * 0.50f);
                    float leftX = cx - (sw * 0.5f);
                    float rightX = cx + (sw * 0.5f);

                    shield.AddLine(leftX, topY, rightX, topY);
                    shield.AddBezier(rightX, topY, rightX + (sw * 0.05f), midY, cx, botY - (sh * 0.1f), cx, botY);
                    shield.AddBezier(cx, botY, cx, botY - (sh * 0.1f), leftX - (sw * 0.05f), midY, leftX, topY);
                    shield.CloseFigure();

                    // Subtle Shield Drop Shadow
                    using (Matrix m = new Matrix())
                    {
                        m.Translate(0, size * 0.03f);
                        using (GraphicsPath shadowPath = (GraphicsPath)shield.Clone())
                        {
                            shadowPath.Transform(m);
                            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                            {
                                g.FillPath(shadowBrush, shadowPath);
                            }
                        }
                    }

                    // Shield Main Body (Pure White)
                    using (SolidBrush shieldBrush = new SolidBrush(Color.White))
                    using (Pen shieldPen = new Pen(Color.FromArgb(226, 232, 240), size * 0.02f))
                    {
                        g.FillPath(shieldBrush, shield);
                        g.DrawPath(shieldPen, shield);
                    }
                }

                // Left Half Shield Tint (Rose / Crimson)
                using (GraphicsPath leftHalf = new GraphicsPath())
                {
                    float topY = cy - (sh * 0.48f);
                    float midY = cy;
                    float botY = cy + (sh * 0.50f);
                    float leftX = cx - (sw * 0.5f);

                    leftHalf.AddLine(leftX, topY, cx, topY);
                    leftHalf.AddLine(cx, topY, cx, botY);
                    leftHalf.AddBezier(cx, botY, cx, botY - (sh * 0.1f), leftX - (sw * 0.05f), midY, leftX, topY);
                    leftHalf.CloseFigure();

                    using (SolidBrush tintBrush = new SolidBrush(Color.FromArgb(244, 63, 94))) // Rose 500
                    {
                        g.FillPath(tintBrush, leftHalf);
                    }
                }

                // Right Half Shield Tint (Sky Blue / Teal - Dev Compilation)
                using (GraphicsPath rightHalf = new GraphicsPath())
                {
                    float topY = cy - (sh * 0.48f);
                    float midY = cy;
                    float botY = cy + (sh * 0.50f);
                    float rightX = cx + (sw * 0.5f);

                    rightHalf.AddLine(cx, topY, rightX, topY);
                    rightHalf.AddBezier(rightX, topY, rightX + (sw * 0.05f), midY, cx, botY - (sh * 0.1f), cx, botY);
                    rightHalf.AddLine(cx, botY, cx, topY);
                    rightHalf.CloseFigure();

                    using (SolidBrush tintBrush = new SolidBrush(Color.FromArgb(14, 165, 233))) // Sky 500
                    {
                        g.FillPath(tintBrush, rightHalf);
                    }
                }

                // Central Fast Toggle / Pause / Lightning Emblem
                // Crisp White Stylized Lightning Bolt in Center
                using (GraphicsPath bolt = new GraphicsPath())
                {
                    float bx = cx;
                    float by = cy - (size * 0.04f);
                    float bw = size * 0.18f;
                    float bh = size * 0.30f;

                    bolt.AddPolygon(new PointF[] {
                        new PointF(bx + bw * 0.15f, by - bh * 0.5f),
                        new PointF(bx - bw * 0.50f, by + bh * 0.05f),
                        new PointF(bx - bw * 0.05f, by + bh * 0.05f),
                        new PointF(bx - bw * 0.20f, by + bh * 0.5f),
                        new PointF(bx + bw * 0.55f, by - bh * 0.05f),
                        new PointF(bx + bw * 0.05f, by - bh * 0.05f)
                    });

                    // Bolt Shadow
                    using (Matrix m = new Matrix())
                    {
                        m.Translate(0, size * 0.02f);
                        using (GraphicsPath shadowBolt = (GraphicsPath)bolt.Clone())
                        {
                            shadowBolt.Transform(m);
                            using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                            {
                                g.FillPath(sBrush, shadowBolt);
                            }
                        }
                    }

                    // Bolt Body (Golden Yellow / White Glow)
                    using (SolidBrush boltBrush = new SolidBrush(Color.White))
                    using (Pen boltPen = new Pen(Color.FromArgb(254, 240, 138), size * 0.02f)) // Yellow 200
                    {
                        g.FillPath(boltBrush, bolt);
                        g.DrawPath(boltPen, bolt);
                    }
                }

                // Subtle Speed Sparks / Whitelist Indicator Dots
                using (SolidBrush sparkBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                {
                    g.FillEllipse(sparkBrush, size * 0.74f, size * 0.18f, size * 0.05f, size * 0.05f);
                    g.FillEllipse(sparkBrush, size * 0.82f, size * 0.26f, size * 0.035f, size * 0.035f);
                    g.FillEllipse(sparkBrush, size * 0.20f, size * 0.76f, size * 0.04f, size * 0.04f);
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
