using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Pexoris.Assets
{
    class CreateIcon
    {
        static void Main(string[] args)
        {
            string outPath = args.Length > 0 ? args[0] : "app.ico";
            int[] sizes = new int[] { 256, 128, 64, 48, 32, 24, 16 };
            Bitmap[] bitmaps = new Bitmap[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                bitmaps[i] = RenderStunningPexorisIcon(sizes[i]);
            }

            SaveMultiIcon(bitmaps, outPath);
            Console.WriteLine("Awesome Icon created successfully at: " + outPath);
        }

        public static Bitmap RenderStunningPexorisIcon(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                float s = size;

                // 1. Soft Drop Shadow under the squircle
                float shadowOffset = s * 0.04f;
                float pad = s * 0.08f;
                RectangleF shadowRect = new RectangleF(pad, pad + shadowOffset, s - pad * 2, s - pad * 2);
                using (GraphicsPath shadowPath = GetRoundedRect(shadowRect, s * 0.24f))
                {
                    using (PathGradientBrush pgb = new PathGradientBrush(shadowPath))
                    {
                        pgb.CenterColor = Color.FromArgb(90, 2, 40, 90);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(pgb, shadowPath);
                    }
                }

                // 2. Vibrant Modern Squircle Shield (Rich Royal Blue to Cyan Gradient)
                RectangleF badgeRect = new RectangleF(pad, pad, s - pad * 2, s - pad * 2);
                using (GraphicsPath badgePath = GetRoundedRect(badgeRect, s * 0.24f))
                {
                    // Gradient: Electric Cyan (#00D2FF) at top to Deep Royal Blue (#0284C7 / #0369A1)
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                        badgeRect,
                        Color.FromArgb(255, 0, 210, 255),
                        Color.FromArgb(255, 2, 110, 210),
                        LinearGradientMode.ForwardDiagonal))
                    {
                        ColorBlend cb = new ColorBlend();
                        cb.Colors = new Color[] {
                            Color.FromArgb(255, 56, 225, 255),  // Vivid Sky
                            Color.FromArgb(255, 0, 160, 250),   // Electric Cyan
                            Color.FromArgb(255, 14, 116, 215)   // Deep Royal
                        };
                        cb.Positions = new float[] { 0f, 0.45f, 1f };
                        bgBrush.InterpolationColors = cb;
                        g.FillPath(bgBrush, badgePath);
                    }

                    // Inner border / highlight for glassmorphism
                    using (Pen innerPen = new Pen(Color.FromArgb(200, 255, 255, 255), Math.Max(1f, s * 0.035f)))
                    {
                        innerPen.Alignment = PenAlignment.Inset;
                        g.DrawPath(innerPen, badgePath);
                    }
                }

                // 3. Lock Shackle (OPEN POSITION = UNLOCKING!)
                float shackleW = s * 0.28f;
                float shackleH = s * 0.32f;
                float shackleLeft = s * 0.32f;
                float shackleTop = s * 0.20f;
                float shackleThick = Math.Max(2.5f, s * 0.08f);

                using (Pen shackleShadow = new Pen(Color.FromArgb(100, 2, 60, 130), shackleThick + 2f))
                {
                    shackleShadow.StartCap = LineCap.Round;
                    shackleShadow.EndCap = LineCap.Round;
                    GraphicsPath spShadow = new GraphicsPath();
                    spShadow.AddLine(shackleLeft, s * 0.50f, shackleLeft, shackleTop + (shackleW / 2f));
                    spShadow.AddArc(shackleLeft, shackleTop + shadowOffset * 0.5f, shackleW, shackleW, 180, 180);
                    spShadow.AddLine(shackleLeft + shackleW, shackleTop + (shackleW / 2f), shackleLeft + shackleW, shackleTop + (shackleW * 0.85f));
                    g.DrawPath(shackleShadow, spShadow);
                }

                using (Pen shacklePen = new Pen(Color.FromArgb(255, 255, 255, 255), shackleThick))
                {
                    shacklePen.StartCap = LineCap.Round;
                    shacklePen.EndCap = LineCap.Round;

                    GraphicsPath sp = new GraphicsPath();
                    // Left leg into lock body
                    sp.AddLine(shackleLeft, s * 0.52f, shackleLeft, shackleTop + (shackleW / 2f));
                    // Top curve
                    sp.AddArc(shackleLeft, shackleTop, shackleW, shackleW, 180, 180);
                    // Right leg open up
                    sp.AddLine(shackleLeft + shackleW, shackleTop + (shackleW / 2f), shackleLeft + shackleW, shackleTop + (shackleW * 0.85f));
                    g.DrawPath(shacklePen, sp);
                }

                // 4. Lock Body (Solid White & Silver with subtle 3D highlight)
                float bodyW = s * 0.46f;
                float bodyH = s * 0.35f;
                float bodyX = (s - bodyW) / 2f;
                float bodyY = s * 0.47f;
                RectangleF bodyRect = new RectangleF(bodyX, bodyY, bodyW, bodyH);

                // Body Drop Shadow
                RectangleF bodyShadow = new RectangleF(bodyX, bodyY + s * 0.03f, bodyW, bodyH);
                using (GraphicsPath bsp = GetRoundedRect(bodyShadow, s * 0.08f))
                {
                    using (SolidBrush bsb = new SolidBrush(Color.FromArgb(70, 0, 40, 100)))
                    {
                        g.FillPath(bsb, bsp);
                    }
                }

                using (GraphicsPath bodyPath = GetRoundedRect(bodyRect, s * 0.08f))
                {
                    // Clean White-Silver 3D Gradient
                    using (LinearGradientBrush bodyBrush = new LinearGradientBrush(
                        bodyRect,
                        Color.FromArgb(255, 255, 255, 255),
                        Color.FromArgb(255, 230, 238, 248),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(bodyBrush, bodyPath);
                    }

                    using (Pen borderPen = new Pen(Color.FromArgb(255, 190, 215, 240), Math.Max(1f, s * 0.02f)))
                    {
                        borderPen.Alignment = PenAlignment.Inset;
                        g.DrawPath(borderPen, bodyPath);
                    }
                }

                // 5. Electric Keyhole (Vibrant Cyan / Deep Blue)
                float khY = bodyY + bodyH * 0.40f;
                float khR = s * 0.055f;
                float khX = s / 2f;

                using (SolidBrush khBrush = new SolidBrush(Color.FromArgb(255, 2, 110, 210)))
                {
                    g.FillEllipse(khBrush, khX - khR, khY - khR, khR * 2, khR * 2);
                    PointF[] slot = new PointF[] {
                        new PointF(khX - (khR * 0.45f), khY),
                        new PointF(khX + (khR * 0.45f), khY),
                        new PointF(khX + (khR * 0.75f), khY + (khR * 1.6f)),
                        new PointF(khX - (khR * 0.75f), khY + (khR * 1.6f))
                    };
                    g.FillPolygon(khBrush, slot);
                }

                // 6. Dynamic Energy Spark on top right (Unlock glow)
                if (size >= 24)
                {
                    float sparkX = s * 0.73f;
                    float sparkY = s * 0.26f;
                    float sparkR = Math.Max(2f, s * 0.05f);

                    using (SolidBrush sparkBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
                    {
                        g.FillEllipse(sparkBrush, sparkX - sparkR, sparkY - sparkR, sparkR * 2, sparkR * 2);
                    }
                    using (Pen sparkGlow = new Pen(Color.FromArgb(200, 255, 255, 120), Math.Max(1f, s * 0.025f)))
                    {
                        g.DrawLine(sparkGlow, sparkX - sparkR * 1.8f, sparkY, sparkX + sparkR * 1.8f, sparkY);
                        g.DrawLine(sparkGlow, sparkX, sparkY - sparkR * 1.8f, sparkX, sparkY + sparkR * 1.8f);
                    }
                }
            }
            return bmp;
        }

        static GraphicsPath GetRoundedRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void SaveMultiIcon(Bitmap[] bitmaps, string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((short)0); // Reserved
                bw.Write((short)1); // ICO type
                bw.Write((short)bitmaps.Length); // Image count

                byte[][] pngBuffers = new byte[bitmaps.Length][];
                int offset = 6 + (16 * bitmaps.Length);

                for (int i = 0; i < bitmaps.Length; i++)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmaps[i].Save(ms, ImageFormat.Png);
                        pngBuffers[i] = ms.ToArray();
                    }
                }

                for (int i = 0; i < bitmaps.Length; i++)
                {
                    Bitmap b = bitmaps[i];
                    bw.Write((byte)(b.Width >= 256 ? 0 : b.Width));
                    bw.Write((byte)(b.Height >= 256 ? 0 : b.Height));
                    bw.Write((byte)0); // Colors
                    bw.Write((byte)0); // Reserved
                    bw.Write((short)1); // Color planes
                    bw.Write((short)32); // Bits per pixel
                    bw.Write((int)pngBuffers[i].Length); // Size
                    bw.Write((int)offset); // Offset
                    offset += pngBuffers[i].Length;
                }

                for (int i = 0; i < pngBuffers.Length; i++)
                {
                    bw.Write(pngBuffers[i]);
                }
            }
        }
    }
}
