using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Pexoris.Assets
{
    class GenerateMasterLogo
    {
        static void Main()
        {
            int size = 512;
            Bitmap bmp = RenderUniquePexorisMonogram(size);
            
            // Save master brand PNG
            bmp.Save("website\\brand-logo.png", ImageFormat.Png);
            Console.WriteLine("Brand logo saved to website\\brand-logo.png");

            // Save multi-size favicon.ico for Pexoris Portal
            int[] sizes = new int[] { 256, 128, 64, 48, 32, 16 };
            Bitmap[] icons = new Bitmap[sizes.Length];
            for (int i = 0; i < sizes.Length; i++)
            {
                icons[i] = RenderUniquePexorisMonogram(sizes[i]);
            }
            SaveMultiIcon(icons, "website\\favicon.ico");
            Console.WriteLine("Master Favicon saved to website\\favicon.ico");
        }

        /// <summary>
        /// Renders the bespoke Pexoris "P" Prism Shield Monogram.
        /// Modern, mathematically precise, sharp isometric facets with Electric Cyan & Cobalt gradients.
        /// </summary>
        public static Bitmap RenderUniquePexorisMonogram(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                float s = size;

                // 1. Soft Ambient Drop Shadow under the entire logo
                float pad = s * 0.08f;
                RectangleF squircleRect = new RectangleF(pad, pad, s - pad * 2, s - pad * 2);
                float shadowOff = s * 0.04f;
                RectangleF shadowRect = new RectangleF(pad, pad + shadowOff, s - pad * 2, s - pad * 2);

                using (GraphicsPath sp = GetSquircle(shadowRect, s * 0.22f))
                {
                    using (PathGradientBrush pgb = new PathGradientBrush(sp))
                    {
                        pgb.CenterColor = Color.FromArgb(70, 0, 75, 180);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(pgb, sp);
                    }
                }

                // 2. High-Tech Glassmorphic Base Squircle (Deep Obsidian Navy with Neon Cyan rim)
                using (GraphicsPath bp = GetSquircle(squircleRect, s * 0.22f))
                {
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                        squircleRect,
                        Color.FromArgb(255, 10, 16, 28),     // Top Dark Slate
                        Color.FromArgb(255, 4, 8, 16),       // Bottom Pitch Obsidian
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(bgBrush, bp);
                    }

                    // Outer Rim: Ultra-sharp Electric Cyan to Deep Blue
                    using (LinearGradientBrush rimBrush = new LinearGradientBrush(
                        squircleRect,
                        Color.FromArgb(255, 0, 210, 255),
                        Color.FromArgb(120, 0, 100, 250),
                        LinearGradientMode.ForwardDiagonal))
                    {
                        using (Pen rimPen = new Pen(rimBrush, Math.Max(1.5f, s * 0.035f)))
                        {
                            rimPen.Alignment = PenAlignment.Inset;
                            g.DrawPath(rimPen, bp);
                        }
                    }
                }

                // 3. The Iconic "P" Hyper-Prism Glyph (Bespoke Geometric Facets)
                // Left Vertical Pillar
                float stemLeft = s * 0.25f;
                float stemTop = s * 0.24f;
                float stemWidth = s * 0.16f;
                float stemHeight = s * 0.52f;
                float cr = s * 0.05f;

                RectangleF stemRect = new RectangleF(stemLeft, stemTop, stemWidth, stemHeight);
                using (GraphicsPath stemPath = GetSquircle(stemRect, cr))
                {
                    using (LinearGradientBrush stemBrush = new LinearGradientBrush(
                        stemRect,
                        Color.FromArgb(255, 0, 230, 255),  // Electric Cyan Top
                        Color.FromArgb(255, 0, 102, 255),  // Vivid Royal Cobalt
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(stemBrush, stemPath);
                    }
                }

                // Loop of the "P" (Dynamic Forward Swept Hexagonal Loop)
                float loopLeft = stemLeft + stemWidth * 0.70f;
                float loopTop = stemTop;
                float loopWidth = s * 0.36f;
                float loopHeight = s * 0.32f;
                RectangleF loopRect = new RectangleF(loopLeft, loopTop, loopWidth, loopHeight);

                using (GraphicsPath loopOuter = GetSquircle(loopRect, cr * 1.5f))
                {
                    using (LinearGradientBrush loopBrush = new LinearGradientBrush(
                        loopRect,
                        Color.FromArgb(255, 0, 150, 255),
                        Color.FromArgb(255, 0, 245, 212),  // Neon Aqua
                        LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(loopBrush, loopOuter);
                    }

                    // Inner negative cutout of the loop
                    float innerPadX = loopWidth * 0.34f;
                    float innerPadY = loopHeight * 0.30f;
                    RectangleF innerRect = new RectangleF(
                        loopLeft + innerPadX * 0.4f,
                        loopTop + innerPadY,
                        loopWidth - innerPadX * 1.2f,
                        loopHeight - innerPadY * 2f
                    );
                    using (GraphicsPath loopInner = GetSquircle(innerRect, cr * 0.6f))
                    {
                        using (SolidBrush cutBrush = new SolidBrush(Color.FromArgb(255, 8, 14, 24)))
                        {
                            g.FillPath(cutBrush, loopInner);
                        }
                    }
                }

                // 4. Energy Precision Spark (Intersecting Core)
                if (size >= 32)
                {
                    float sparkX = stemLeft + stemWidth;
                    float sparkY = stemTop + loopHeight;
                    float sparkR = Math.Max(2f, s * 0.045f);

                    using (SolidBrush sparkBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
                    {
                        g.FillEllipse(sparkBrush, sparkX - sparkR, sparkY - sparkR, sparkR * 2, sparkR * 2);
                    }
                    using (Pen glowPen = new Pen(Color.FromArgb(180, 0, 240, 255), Math.Max(1f, s * 0.02f)))
                    {
                        g.DrawLine(glowPen, sparkX - sparkR * 2f, sparkY, sparkX + sparkR * 2f, sparkY);
                        g.DrawLine(glowPen, sparkX, sparkY - sparkR * 2f, sparkX, sparkY + sparkR * 2f);
                    }
                }
            }
            return bmp;
        }

        static GraphicsPath GetSquircle(RectangleF r, float radius)
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
                bw.Write((short)0);
                bw.Write((short)1);
                bw.Write((short)bitmaps.Length);

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
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write((short)1);
                    bw.Write((short)32);
                    bw.Write((int)pngBuffers[i].Length);
                    bw.Write((int)offset);
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
