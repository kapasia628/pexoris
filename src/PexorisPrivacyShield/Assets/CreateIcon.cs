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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisPrivacyShield\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\privacy-shield\privacy-shield.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\privacy-shield.png";

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

                // Luxurious Cyber Privacy Gradient Squircle (Indigo 600 -> Slate 900)
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(79, 70, 229),    // Indigo 600 (#4F46E5)
                    Color.FromArgb(15, 23, 42)))     // Slate 900 (#0F172A)
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle neon glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(80, 165, 180, 252), size * 0.015f)) // Indigo 300
                {
                    g.DrawPath(pen, path);
                }

                // Center Coordinates for Artwork
                float cx = size * 0.5f;
                float cy = size * 0.48f;
                float sw = size * 0.54f;
                float sh = size * 0.60f;

                // Shield Outer Path
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

                    // Subtle Drop Shadow
                    using (Matrix m = new Matrix())
                    {
                        m.Translate(0, size * 0.03f);
                        using (GraphicsPath shadowPath = (GraphicsPath)shield.Clone())
                        {
                            shadowPath.Transform(m);
                            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                            {
                                g.FillPath(shadowBrush, shadowPath);
                            }
                        }
                    }

                    // Shield Main Body (Pure White Background)
                    using (SolidBrush shieldBrush = new SolidBrush(Color.White))
                    using (Pen shieldPen = new Pen(Color.FromArgb(224, 231, 255), size * 0.02f))
                    {
                        g.FillPath(shieldBrush, shield);
                        g.DrawPath(shieldPen, shield);
                    }
                }

                // Left Half Shield Tint (Vibrant Indigo-Purple #6366F1)
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

                    using (SolidBrush tintBrush = new SolidBrush(Color.FromArgb(99, 102, 241))) // Indigo 500
                    {
                        g.FillPath(tintBrush, leftHalf);
                    }
                }

                // Right Half Shield Tint (Teal / Emerald Cyber Green #10B981)
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

                    using (SolidBrush tintBrush = new SolidBrush(Color.FromArgb(16, 185, 129))) // Emerald 500
                    {
                        g.FillPath(tintBrush, rightHalf);
                    }
                }

                // Central Privacy Emblem: Modern Padlock / Privacy Vault with Telemetry Slash Guard
                float lockW = size * 0.22f;
                float lockH = size * 0.18f;
                float lockY = cy - (size * 0.02f);

                // Padlock Shackle (Top Arc)
                float shackleW = lockW * 0.65f;
                float shackleH = size * 0.14f;
                float shackleX = cx - (shackleW * 0.5f);
                float shackleY = lockY - (shackleH * 0.75f);

                using (Pen shacklePen = new Pen(Color.White, size * 0.035f))
                {
                    shacklePen.StartCap = LineCap.Round;
                    shacklePen.EndCap = LineCap.Round;
                    using (GraphicsPath shacklePath = new GraphicsPath())
                    {
                        shacklePath.AddArc(shackleX, shackleY, shackleW, shackleH, 180, 180);
                        shacklePath.AddLine(shackleX + shackleW, shackleY + (shackleH * 0.5f), shackleX + shackleW, lockY);
                        shacklePath.AddLine(shackleX, shackleY + (shackleH * 0.5f), shackleX, lockY);
                        g.DrawPath(shacklePen, shacklePath);
                    }
                }

                // Padlock Solid Body (Squircle)
                RectangleF lockRect = new RectangleF(cx - (lockW * 0.5f), lockY, lockW, lockH);
                using (GraphicsPath lockPath = GetRoundedPath(lockRect, size * 0.035f))
                using (SolidBrush lockBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(lockBrush, lockPath);
                }

                // Padlock Keyhole (Indigo Dot + Slot)
                using (SolidBrush holeBrush = new SolidBrush(Color.FromArgb(49, 46, 129))) // Indigo 900
                {
                    float dotR = size * 0.022f;
                    g.FillEllipse(holeBrush, cx - dotR, lockY + (lockH * 0.28f), dotR * 2, dotR * 2);

                    using (GraphicsPath keySlot = new GraphicsPath())
                    {
                        keySlot.AddPolygon(new PointF[] {
                            new PointF(cx - (dotR * 0.7f), lockY + (lockH * 0.38f)),
                            new PointF(cx + (dotR * 0.7f), lockY + (lockH * 0.38f)),
                            new PointF(cx + (dotR * 0.4f), lockY + (lockH * 0.72f)),
                            new PointF(cx - (dotR * 0.4f), lockY + (lockH * 0.72f))
                        });
                        g.FillPath(holeBrush, keySlot);
                    }
                }

                // Decorative Cyber Shield Sparkles / Telemetry Block Dots
                using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                {
                    g.FillEllipse(dotBrush, size * 0.75f, size * 0.18f, size * 0.045f, size * 0.045f);
                    g.FillEllipse(dotBrush, size * 0.83f, size * 0.26f, size * 0.03f, size * 0.03f);
                    g.FillEllipse(dotBrush, size * 0.19f, size * 0.75f, size * 0.038f, size * 0.038f);
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
