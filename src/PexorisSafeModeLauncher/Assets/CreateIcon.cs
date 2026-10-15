using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace CreateIcon
{
    class Program
    {
        static void Main(string[] args)
        {
            int size = 512;
            using (Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);

                    // Rounded Squircle Container with Crimson Red gradient
                    Rectangle rect = new Rectangle(24, 24, 464, 464);
                    using (GraphicsPath path = GetRoundedRect(rect, 108))
                    {
                        // Background gradient: Crimson Red to Deep Burgundy
                        using (LinearGradientBrush brush = new LinearGradientBrush(
                            new Point(24, 24), new Point(488, 488),
                            Color.FromArgb(220, 38, 38),   // #DC2626
                            Color.FromArgb(153, 27, 27)))  // #991B1B
                        {
                            g.FillPath(brush, path);
                        }

                        // Subtle inner gloss border
                        using (Pen pen = new Pen(Color.FromArgb(120, 255, 255, 255), 6f))
                        {
                            g.DrawPath(pen, path);
                        }
                    }

                    // Protective Shield Outline in bright gold/white
                    using (GraphicsPath shieldPath = new GraphicsPath())
                    {
                        PointF topCenter = new PointF(256, 110);
                        PointF topRight = new PointF(370, 150);
                        PointF bottomRight = new PointF(350, 310);
                        PointF bottomTip = new PointF(256, 400);
                        PointF bottomLeft = new PointF(162, 310);
                        PointF topLeft = new PointF(142, 150);

                        shieldPath.AddLine(topCenter, topRight);
                        shieldPath.AddLine(topRight, bottomRight);
                        shieldPath.AddLine(bottomRight, bottomTip);
                        shieldPath.AddLine(bottomTip, bottomLeft);
                        shieldPath.AddLine(bottomLeft, topLeft);
                        shieldPath.CloseFigure();

                        // Shield glow
                        using (PathGradientBrush pgb = new PathGradientBrush(shieldPath))
                        {
                            pgb.CenterColor = Color.FromArgb(70, 255, 255, 255);
                            pgb.SurroundColors = new Color[] { Color.FromArgb(10, 255, 255, 255) };
                            g.FillPath(pgb, shieldPath);
                        }

                        using (Pen shieldPen = new Pen(Color.FromArgb(220, 255, 255, 255), 14f))
                        {
                            shieldPen.LineJoin = LineJoin.Round;
                            g.DrawPath(shieldPen, shieldPath);
                        }
                    }

                    // Power / Boot symbol in center of shield (Amber-Gold)
                    // Arc (partial circle) + vertical power tick
                    Rectangle arcRect = new Rectangle(196, 210, 120, 120);
                    using (Pen powerPen = new Pen(Color.FromArgb(254, 240, 138), 18f)) // Bright Amber/Yellow
                    {
                        powerPen.StartCap = LineCap.Round;
                        powerPen.EndCap = LineCap.Round;
                        // Arc from -60 to 240 degrees (leaving top open)
                        g.DrawArc(powerPen, arcRect, -60, 300);

                        // Vertical bar in top opening
                        g.DrawLine(powerPen, new Point(256, 175), new Point(256, 245));
                    }

                    // Small indicator badge: "SAFE" or lock dot
                    using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(250, 204, 21))) // Gold
                    {
                        g.FillEllipse(dotBrush, 244, 345, 24, 24);
                    }
                }

                // Ensure target directories exist
                Directory.CreateDirectory(@"website\tools\safemode-launcher");
                Directory.CreateDirectory(@"website\assets\icons");

                // Save PNGs
                bmp.Save(@"website\tools\safemode-launcher\safemode-launcher.png", ImageFormat.Png);
                bmp.Save(@"website\assets\icons\safemode-launcher.png", ImageFormat.Png);

                // Create multi-res ICO
                using (FileStream fs = new FileStream(@"src\PexorisSafeModeLauncher\Assets\app.ico", FileMode.Create))
                {
                    WriteIco(fs, bmp);
                }

                Console.WriteLine("Icons generated successfully!");
            }
        }

        static GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void WriteIco(Stream stream, Bitmap source)
        {
            int[] sizes = new int[] { 256, 48, 32, 16 };
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                bw.Write((short)0); // Reserved
                bw.Write((short)1); // Type 1 = ICO
                bw.Write((short)sizes.Length); // Image count

                int offset = 6 + (sizes.Length * 16);
                byte[][] pngBuffers = new byte[sizes.Length][];

                for (int i = 0; i < sizes.Length; i++)
                {
                    int sz = sizes[i];
                    using (Bitmap resized = new Bitmap(sz, sz, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(resized))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(source, 0, 0, sz, sz);
                        }
                        using (MemoryStream ms = new MemoryStream())
                        {
                            resized.Save(ms, ImageFormat.Png);
                            pngBuffers[i] = ms.ToArray();
                        }
                    }

                    bw.Write((byte)(sz >= 256 ? 0 : sz)); // Width
                    bw.Write((byte)(sz >= 256 ? 0 : sz)); // Height
                    bw.Write((byte)0); // Color count
                    bw.Write((byte)0); // Reserved
                    bw.Write((short)1); // Color planes
                    bw.Write((short)32); // Bits per pixel
                    bw.Write(pngBuffers[i].Length); // Size of image data
                    bw.Write(offset); // Offset of image data
                    offset += pngBuffers[i].Length;
                }

                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write(pngBuffers[i]);
                }
            }
        }
    }
}
