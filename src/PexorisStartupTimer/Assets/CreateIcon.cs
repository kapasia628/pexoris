using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace PexorisStartupTimer.Assets
{
    class CreateIcon
    {
        static void Main()
        {
            int[] sizes = new int[] { 256, 48, 32, 16 };
            Bitmap[] bitmaps = new Bitmap[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                bitmaps[i] = RenderStopwatchBitmap(sizes[i]);
            }

            // Save multi-resolution .ico
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            SaveMultiResolutionIcon(bitmaps, icoPath);
            Console.WriteLine("Saved: " + icoPath);

            // Save 512x512 PNGs for website
            using (Bitmap big = RenderStopwatchBitmap(512))
            {
                string png1 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\website\tools\startup-timer\startup-timer.png"));
                string png2 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\website\assets\icons\startup-timer.png"));
                
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

        static Bitmap RenderStopwatchBitmap(int size)
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

                // Top stopwatch pusher button
                float pusherW = s * 0.14f;
                float pusherH = s * 0.07f;
                float pusherX = (s - pusherW) / 2f;
                float pusherY = pad * 0.25f;
                using (GraphicsPath pusherPath = new GraphicsPath())
                {
                    pusherPath.AddRoundedRectangle(new RectangleF(pusherX, pusherY, pusherW, pusherH), pusherH * 0.4f);
                    using (LinearGradientBrush pBrush = new LinearGradientBrush(new PointF(pusherX, pusherY), new PointF(pusherX, pusherY + pusherH), Color.FromArgb(203, 213, 225), Color.FromArgb(148, 163, 184)))
                    {
                        g.FillPath(pBrush, pusherPath);
                    }
                }

                // Outer Squircle Background with deep subtle shadow
                RectangleF rect = new RectangleF(pad, pad, w, h);
                using (GraphicsPath outerPath = new GraphicsPath())
                {
                    float r = w * 0.26f;
                    outerPath.AddRoundedRectangle(rect, r);

                    // Deep gradient: Vibrant Cyan & Deep Sapphire Blue (#0284C7 to #1E3A8A)
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(rect, Color.FromArgb(2, 132, 199), Color.FromArgb(30, 58, 138), 60f))
                    {
                        g.FillPath(bgBrush, outerPath);
                    }

                    // Outer glossy border
                    using (Pen borderPen = new Pen(Color.FromArgb(120, 255, 255, 255), s * 0.018f))
                    {
                        g.DrawPath(borderPen, outerPath);
                    }
                }

                // Inner Dial Circle
                float dialMargin = s * 0.09f;
                float dialSize = w - (dialMargin * 2f);
                float dialX = rect.X + dialMargin;
                float dialY = rect.Y + dialMargin;
                RectangleF dialRect = new RectangleF(dialX, dialY, dialSize, dialSize);

                using (LinearGradientBrush dialBrush = new LinearGradientBrush(dialRect, Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), 90f))
                {
                    g.FillEllipse(dialBrush, dialRect);
                }
                using (Pen dialBorder = new Pen(Color.FromArgb(56, 189, 248), s * 0.024f))
                {
                    g.DrawEllipse(dialBorder, dialRect);
                }

                // Speed Arc Ring (0 to 220 degrees in vibrant Emerald/Electric Cyan)
                using (Pen arcPen = new Pen(Color.FromArgb(52, 211, 153), s * 0.035f))
                {
                    arcPen.StartCap = LineCap.Round;
                    arcPen.EndCap = LineCap.Round;
                    g.DrawArc(arcPen, dialX + s * 0.02f, dialY + s * 0.02f, dialSize - s * 0.04f, dialSize - s * 0.04f, 135f, 210f);
                }

                // Dial Center Pin
                float cx = dialRect.X + dialSize / 2f;
                float cy = dialRect.Y + dialSize / 2f;

                // Dial Tick Marks
                for (int deg = 0; deg < 360; deg += 30)
                {
                    double rad = deg * Math.PI / 180.0;
                    float innerR = dialSize * 0.38f;
                    float outerR = dialSize * 0.44f;
                    float x1 = cx + (float)(Math.Cos(rad) * innerR);
                    float y1 = cy + (float)(Math.Sin(rad) * innerR);
                    float x2 = cx + (float)(Math.Cos(rad) * outerR);
                    float y2 = cy + (float)(Math.Sin(rad) * outerR);

                    using (Pen tickPen = new Pen(Color.FromArgb(148, 163, 184), (deg % 90 == 0) ? s * 0.022f : s * 0.012f))
                    {
                        g.DrawLine(tickPen, x1, y1, x2, y2);
                    }
                }

                // Chronometer Hand pointing to ~2 o'clock (fast boot time angle)
                double handRad = (-40.0) * Math.PI / 180.0;
                float handLen = dialSize * 0.36f;
                float hx = cx + (float)(Math.Cos(handRad) * handLen);
                float hy = cy + (float)(Math.Sin(handRad) * handLen);

                using (Pen handPen = new Pen(Color.FromArgb(244, 63, 94), s * 0.036f)) // Vibrant Rose / Red hand
                {
                    handPen.StartCap = LineCap.Round;
                    handPen.EndCap = LineCap.Round;
                    g.DrawLine(handPen, cx, cy, hx, hy);
                }

                // Center Boss Knob
                float bossR = s * 0.05f;
                using (SolidBrush bossBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                {
                    g.FillEllipse(bossBrush, cx - bossR, cy - bossR, bossR * 2f, bossR * 2f);
                }
                using (Pen bossPen = new Pen(Color.FromArgb(15, 23, 42), s * 0.015f))
                {
                    g.DrawEllipse(bossPen, cx - bossR, cy - bossR, bossR * 2f, bossR * 2f);
                }

                // Lightning Speed Bolt in bottom left of dial
                float boltX = cx - dialSize * 0.22f;
                float boltY = cy + dialSize * 0.12f;
                float bs = dialSize * 0.24f;
                using (GraphicsPath bolt = new GraphicsPath())
                {
                    bolt.AddPolygon(new PointF[] {
                        new PointF(boltX + bs * 0.55f, boltY),
                        new PointF(boltX, boltY + bs * 0.55f),
                        new PointF(boltX + bs * 0.45f, boltY + bs * 0.55f),
                        new PointF(boltX + bs * 0.35f, boltY + bs),
                        new PointF(boltX + bs * 0.9f, boltY + bs * 0.4f),
                        new PointF(boltX + bs * 0.5f, boltY + bs * 0.4f)
                    });
                    using (SolidBrush boltBrush = new SolidBrush(Color.FromArgb(250, 204, 21))) // Yellow speed bolt
                    {
                        g.FillPath(boltBrush, bolt);
                    }
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
