using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace PexorisDiskUsage.Assets
{
    class CreateIcon
    {
        static void Main()
        {
            int[] sizes = new int[] { 256, 48, 32, 16 };
            Bitmap[] bitmaps = new Bitmap[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                bitmaps[i] = RenderDiskUsageBitmap(sizes[i]);
            }

            // Save multi-resolution .ico
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            SaveMultiResolutionIcon(bitmaps, icoPath);
            Console.WriteLine("Saved: " + icoPath);

            // Save 512x512 PNGs for website
            using (Bitmap big = RenderDiskUsageBitmap(512))
            {
                string png1 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\website\tools\disk-usage\disk-usage.png"));
                string png2 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\website\assets\icons\disk-usage.png"));
                
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

        static Bitmap RenderDiskUsageBitmap(int size)
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

                // Outer Squircle: Deep Teal & Emerald gradient (#0D9488 to #042F2E)
                RectangleF rect = new RectangleF(pad, pad, w, h);
                using (GraphicsPath outerPath = new GraphicsPath())
                {
                    float r = w * 0.26f;
                    outerPath.AddRoundedRectangle(rect, r);

                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(rect, Color.FromArgb(13, 148, 136), Color.FromArgb(4, 47, 46), 65f))
                    {
                        g.FillPath(bgBrush, outerPath);
                    }

                    // Glossy border
                    using (Pen borderPen = new Pen(Color.FromArgb(120, 255, 255, 255), s * 0.018f))
                    {
                        g.DrawPath(borderPen, outerPath);
                    }
                }

                // Inner Storage Enclosure Card
                float cardMargin = s * 0.11f;
                float cardW = w - (cardMargin * 2f);
                float cardH = h - (cardMargin * 2f);
                float cardX = rect.X + cardMargin;
                float cardY = rect.Y + cardMargin;
                RectangleF innerRect = new RectangleF(cardX, cardY, cardW, cardH);

                using (GraphicsPath innerPath = new GraphicsPath())
                {
                    innerPath.AddRoundedRectangle(innerRect, cardW * 0.16f);
                    using (SolidBrush innerBrush = new SolidBrush(Color.FromArgb(15, 23, 42))) // Slate #0F172A
                    {
                        g.FillPath(innerBrush, innerPath);
                    }
                    using (Pen innerPen = new Pen(Color.FromArgb(45, 212, 191), s * 0.02f))
                    {
                        g.DrawPath(innerPen, innerPath);
                    }
                }

                // Segmented Treemap Blocks inside the card
                float tmX = cardX + cardW * 0.1f;
                float tmY = cardY + cardH * 0.12f;
                float tmW = cardW * 0.8f;
                float tmH = cardH * 0.54f;

                // Block 1 (Large left: System/Apps in Vibrant Teal #14B8A6)
                RectangleF b1 = new RectangleF(tmX, tmY, tmW * 0.58f, tmH);
                using (GraphicsPath p1 = new GraphicsPath())
                {
                    p1.AddRoundedRectangle(b1, s * 0.03f);
                    using (LinearGradientBrush brush1 = new LinearGradientBrush(b1, Color.FromArgb(45, 212, 191), Color.FromArgb(13, 148, 136), 45f))
                    {
                        g.FillPath(brush1, p1);
                    }
                }

                // Block 2 (Top right: Media/Videos in Amber #F59E0B)
                RectangleF b2 = new RectangleF(tmX + tmW * 0.62f, tmY, tmW * 0.38f, tmH * 0.46f);
                using (GraphicsPath p2 = new GraphicsPath())
                {
                    p2.AddRoundedRectangle(b2, s * 0.025f);
                    using (LinearGradientBrush brush2 = new LinearGradientBrush(b2, Color.FromArgb(251, 191, 36), Color.FromArgb(217, 119, 6), 45f))
                    {
                        g.FillPath(brush2, p2);
                    }
                }

                // Block 3 (Bottom right: Free space in Emerald/Cyan #10B981)
                RectangleF b3 = new RectangleF(tmX + tmW * 0.62f, tmY + tmH * 0.54f, tmW * 0.38f, tmH * 0.46f);
                using (GraphicsPath p3 = new GraphicsPath())
                {
                    p3.AddRoundedRectangle(b3, s * 0.025f);
                    using (LinearGradientBrush brush3 = new LinearGradientBrush(b3, Color.FromArgb(52, 211, 153), Color.FromArgb(5, 150, 105), 45f))
                    {
                        g.FillPath(brush3, p3);
                    }
                }

                // Storage Drive Meter Bar at the bottom
                float barX = cardX + cardW * 0.1f;
                float barY = cardY + cardH * 0.76f;
                float barW = cardW * 0.8f;
                float barH = cardH * 0.12f;
                RectangleF barBg = new RectangleF(barX, barY, barW, barH);
                using (GraphicsPath barBgPath = new GraphicsPath())
                {
                    barBgPath.AddRoundedRectangle(barBg, barH * 0.5f);
                    using (SolidBrush barBgBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
                    {
                        g.FillPath(barBgBrush, barBgPath);
                    }
                }

                // Filled portion of Meter Bar (70% full)
                RectangleF barFill = new RectangleF(barX, barY, barW * 0.72f, barH);
                using (GraphicsPath barFillPath = new GraphicsPath())
                {
                    barFillPath.AddRoundedRectangle(barFill, barH * 0.5f);
                    using (LinearGradientBrush barFillBrush = new LinearGradientBrush(barFill, Color.FromArgb(45, 212, 191), Color.FromArgb(20, 184, 166), 0f))
                    {
                        g.FillPath(barFillBrush, barFillPath);
                    }
                }

                // Glowing Activity LED Pin (top right of the card)
                float ledR = s * 0.032f;
                float ledX = cardX + cardW - cardW * 0.12f;
                float ledY = cardY + cardH * 0.06f;
                using (SolidBrush ledGlow = new SolidBrush(Color.FromArgb(90, 52, 211, 153)))
                {
                    g.FillEllipse(ledGlow, ledX - ledR * 1.5f, ledY - ledR * 1.5f, ledR * 3f, ledR * 3f);
                }
                using (SolidBrush ledSolid = new SolidBrush(Color.FromArgb(52, 211, 153)))
                {
                    g.FillEllipse(ledSolid, ledX - ledR, ledY - ledR, ledR * 2f, ledR * 2f);
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
