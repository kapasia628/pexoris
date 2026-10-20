using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace PexorisStartupNotifier.Assets
{
    class CreateIcon
    {
        static void Main()
        {
            int[] sizes = new int[] { 256, 48, 32, 16 };
            Bitmap[] bitmaps = new Bitmap[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                bitmaps[i] = RenderStartupNotifierBitmap(sizes[i]);
            }

            // Save multi-resolution .ico
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            SaveMultiResolutionIcon(bitmaps, icoPath);
            Console.WriteLine("Saved: " + icoPath);

            // Save 512x512 PNGs for website
            using (Bitmap big = RenderStartupNotifierBitmap(512))
            {
                string png1 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\website\tools\startup-notifier\startup-notifier.png"));
                string png2 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\website\assets\icons\startup-notifier.png"));
                
                string dir1 = Path.GetDirectoryName(png1);
                if (!Directory.Exists(dir1)) Directory.CreateDirectory(dir1);
                string dir2 = Path.GetDirectoryName(png2);
                if (!Directory.Exists(dir2)) Directory.CreateDirectory(dir2);

                big.Save(png1, ImageFormat.Png);
                big.Save(png2, ImageFormat.Png);
                Console.WriteLine("Saved: " + png1);
                Console.WriteLine("Saved: " + png2);
            }
        }

        static Bitmap RenderStartupNotifierBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float s = size;
                float pad = s * 0.08f;
                float w = s - (pad * 2f);
                float h = s - (pad * 2f);

                // Outer Squircle: Electric Indigo & Royal Purple gradient (#4F46E5 to #1E1B4B)
                RectangleF rect = new RectangleF(pad, pad, w, h);
                using (GraphicsPath outerPath = new GraphicsPath())
                {
                    float r = w * 0.26f;
                    outerPath.AddRoundedRectangle(rect, r);

                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(rect, Color.FromArgb(79, 70, 229), Color.FromArgb(30, 27, 75), 65f))
                    {
                        g.FillPath(bgBrush, outerPath);
                    }

                    // Glossy border
                    using (Pen borderPen = new Pen(Color.FromArgb(140, 255, 255, 255), s * 0.018f))
                    {
                        g.DrawPath(borderPen, outerPath);
                    }
                }

                // Inner Dark Badge Card
                float cardMargin = s * 0.12f;
                float cardW = w - (cardMargin * 2f);
                float cardH = h - (cardMargin * 2f);
                float cardX = rect.X + cardMargin;
                float cardY = rect.Y + cardMargin;
                RectangleF innerRect = new RectangleF(cardX, cardY, cardW, cardH);

                using (GraphicsPath innerPath = new GraphicsPath())
                {
                    innerPath.AddRoundedRectangle(innerRect, cardW * 0.18f);
                    using (SolidBrush innerBrush = new SolidBrush(Color.FromArgb(15, 23, 42))) // Slate #0F172A
                    {
                        g.FillPath(innerBrush, innerPath);
                    }
                    using (Pen innerPen = new Pen(Color.FromArgb(129, 140, 248), s * 0.02f))
                    {
                        g.DrawPath(innerPen, innerPath);
                    }
                }

                // Center Alert Bell Motif
                float bellCenterX = cardX + cardW * 0.5f;
                float bellTopY = cardY + cardH * 0.22f;
                float bellW = cardW * 0.52f;
                float bellH = cardH * 0.44f;

                using (GraphicsPath bellPath = new GraphicsPath())
                {
                    // Bell dome and skirt
                    bellPath.AddArc(bellCenterX - bellW * 0.22f, bellTopY, bellW * 0.44f, bellH * 0.4f, 180, 180);
                    bellPath.AddBezier(
                        bellCenterX + bellW * 0.22f, bellTopY + bellH * 0.2f,
                        bellCenterX + bellW * 0.26f, bellTopY + bellH * 0.5f,
                        bellCenterX + bellW * 0.44f, bellTopY + bellH * 0.72f,
                        bellCenterX + bellW * 0.48f, bellTopY + bellH * 0.78f
                    );
                    bellPath.AddLine(bellCenterX + bellW * 0.48f, bellTopY + bellH * 0.78f, bellCenterX - bellW * 0.48f, bellTopY + bellH * 0.78f);
                    bellPath.AddBezier(
                        bellCenterX - bellW * 0.48f, bellTopY + bellH * 0.78f,
                        bellCenterX - bellW * 0.44f, bellTopY + bellH * 0.72f,
                        bellCenterX - bellW * 0.26f, bellTopY + bellH * 0.5f,
                        bellCenterX - bellW * 0.22f, bellTopY + bellH * 0.2f
                    );
                    bellPath.CloseFigure();

                    using (LinearGradientBrush bellBrush = new LinearGradientBrush(new RectangleF(bellCenterX - bellW * 0.5f, bellTopY, bellW, bellH), Color.FromArgb(244, 243, 255), Color.FromArgb(199, 210, 254), 90f))
                    {
                        g.FillPath(bellBrush, bellPath);
                    }
                }

                // Bell clapper
                float clapperR = bellW * 0.12f;
                using (SolidBrush clapperBrush = new SolidBrush(Color.FromArgb(251, 191, 36))) // Amber #FBBF24
                {
                    g.FillEllipse(clapperBrush, bellCenterX - clapperR, bellTopY + bellH * 0.74f, clapperR * 2f, clapperR * 1.8f);
                }

                // Bell handle loop at top
                float loopR = bellW * 0.08f;
                using (Pen loopPen = new Pen(Color.FromArgb(199, 210, 254), s * 0.024f))
                {
                    g.DrawEllipse(loopPen, bellCenterX - loopR, bellTopY - loopR * 1.2f, loopR * 2f, loopR * 2f);
                }

                // Lightning / Startup Spark inside bell
                float boltX = bellCenterX;
                float boltY = bellTopY + bellH * 0.28f;
                PointF[] bolt = new PointF[] {
                    new PointF(boltX + s * 0.01f, boltY),
                    new PointF(boltX - s * 0.04f, boltY + s * 0.08f),
                    new PointF(boltX - s * 0.005f, boltY + s * 0.08f),
                    new PointF(boltX - s * 0.02f, boltY + s * 0.16f),
                    new PointF(boltX + s * 0.04f, boltY + s * 0.07f),
                    new PointF(boltX + s * 0.005f, boltY + s * 0.07f)
                };
                using (SolidBrush boltBrush = new SolidBrush(Color.FromArgb(79, 70, 229)))
                {
                    g.FillPolygon(boltBrush, bolt);
                }

                // Glowing Alert Pulse Ring (top-right of the bell)
                float pulseX = cardX + cardW * 0.78f;
                float pulseY = cardY + cardH * 0.22f;
                float pulseR = s * 0.04f;

                // Outer glow
                using (SolidBrush glow = new SolidBrush(Color.FromArgb(80, 239, 68, 68)))
                {
                    g.FillEllipse(glow, pulseX - pulseR * 1.8f, pulseY - pulseR * 1.8f, pulseR * 3.6f, pulseR * 3.6f);
                }
                // Solid red alert dot with exclamation mark
                using (SolidBrush dot = new SolidBrush(Color.FromArgb(239, 68, 68)))
                {
                    g.FillEllipse(dot, pulseX - pulseR, pulseY - pulseR, pulseR * 2f, pulseR * 2f);
                }
                using (SolidBrush whiteDot = new SolidBrush(Color.White))
                {
                    g.FillEllipse(whiteDot, pulseX - pulseR * 0.25f, pulseY + pulseR * 0.3f, pulseR * 0.5f, pulseR * 0.5f);
                    g.FillRectangle(whiteDot, pulseX - pulseR * 0.25f, pulseY - pulseR * 0.55f, pulseR * 0.5f, pulseR * 0.65f);
                }
            }
            return bmp;
        }

        static void SaveMultiResolutionIcon(Bitmap[] bitmaps, string outputPath)
        {
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((short)0); // Reserved
                bw.Write((short)1); // Type (1 = ICO)
                bw.Write((short)bitmaps.Length); // Image count

                int offset = 6 + (16 * bitmaps.Length);

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
                    int w = bitmaps[i].Width >= 256 ? 0 : bitmaps[i].Width;
                    int h = bitmaps[i].Height >= 256 ? 0 : bitmaps[i].Height;

                    bw.Write((byte)w);
                    bw.Write((byte)h);
                    bw.Write((byte)0); // Color palette
                    bw.Write((byte)0); // Reserved
                    bw.Write((short)1); // Color planes
                    bw.Write((short)32); // Bits per pixel
                    bw.Write(pngBytes[i].Length); // Size of image data
                    bw.Write(offset); // Offset to image data

                    offset += pngBytes[i].Length;
                }

                for (int i = 0; i < bitmaps.Length; i++)
                {
                    bw.Write(pngBytes[i]);
                }
            }
        }
    }

    public static class GraphicsPathExtensions
    {
        public static void AddRoundedRectangle(this GraphicsPath path, RectangleF bounds, float radius)
        {
            float diameter = radius * 2f;
            SizeF size = new SizeF(diameter, diameter);
            RectangleF arc = new RectangleF(bounds.Location, size);

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
        }
    }
}
