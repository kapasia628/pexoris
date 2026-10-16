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

                    // Rounded Squircle Container with Cobalt Blue / Indigo gradient
                    Rectangle rect = new Rectangle(24, 24, 464, 464);
                    using (GraphicsPath path = GetRoundedRect(rect, 108))
                    {
                        // Background gradient: Deep Indigo to Cobalt Blue
                        using (LinearGradientBrush brush = new LinearGradientBrush(
                            new Point(24, 24), new Point(488, 488),
                            Color.FromArgb(30, 58, 138),   // #1E3A8A
                            Color.FromArgb(15, 23, 42)))   // #0F172A
                        {
                            g.FillPath(brush, path);
                        }

                        // Subtle inner glow border
                        using (Pen pen = new Pen(Color.FromArgb(90, 59, 130, 246), 6f)) // Blue #3B82F6
                        {
                            g.DrawPath(pen, path);
                        }
                    }

                    // Protective Firewall Shield Outline
                    using (GraphicsPath shieldPath = new GraphicsPath())
                    {
                        PointF topCenter = new PointF(256, 95);
                        PointF topRight = new PointF(380, 140);
                        PointF bottomRight = new PointF(360, 310);
                        PointF bottomTip = new PointF(256, 410);
                        PointF bottomLeft = new PointF(152, 310);
                        PointF topLeft = new PointF(132, 140);

                        shieldPath.AddLine(topCenter, topRight);
                        shieldPath.AddLine(topRight, bottomRight);
                        shieldPath.AddLine(bottomRight, bottomTip);
                        shieldPath.AddLine(bottomTip, bottomLeft);
                        shieldPath.AddLine(bottomLeft, topLeft);
                        shieldPath.CloseFigure();

                        // Shield inner fill
                        using (LinearGradientBrush shieldBrush = new LinearGradientBrush(
                            new Point(132, 95), new Point(380, 410),
                            Color.FromArgb(220, 37, 99, 235), // Vibrant Blue #2563EB
                            Color.FromArgb(240, 30, 64, 175))) // Darker Blue #1E40AF
                        {
                            g.FillPath(shieldBrush, shieldPath);
                        }

                        // Shield border
                        using (Pen shieldPen = new Pen(Color.FromArgb(255, 147, 197, 253), 8f)) // Sky blue border
                        {
                            g.DrawPath(shieldPen, shieldPath);
                        }
                    }

                    // Firewall Brick Grid inside the Shield
                    // Draw stylish stylized firewall bricks
                    using (Pen brickPen = new Pen(Color.FromArgb(180, 255, 255, 255), 4f))
                    {
                        // Horizontal brick mortar lines
                        g.DrawLine(brickPen, 180, 180, 332, 180);
                        g.DrawLine(brickPen, 160, 225, 352, 225);
                        g.DrawLine(brickPen, 165, 270, 347, 270);
                        g.DrawLine(brickPen, 190, 315, 322, 315);

                        // Vertical brick staggered seams
                        // Row 1 (between 180 and 225)
                        g.DrawLine(brickPen, 225, 180, 225, 225);
                        g.DrawLine(brickPen, 287, 180, 287, 225);

                        // Row 2 (between 225 and 270)
                        g.DrawLine(brickPen, 195, 225, 195, 270);
                        g.DrawLine(brickPen, 256, 225, 256, 270);
                        g.DrawLine(brickPen, 317, 225, 317, 270);

                        // Row 3 (between 270 and 315)
                        g.DrawLine(brickPen, 225, 270, 225, 315);
                        g.DrawLine(brickPen, 287, 270, 287, 315);

                        // Row 4 (between 315 and bottom)
                        g.DrawLine(brickPen, 256, 315, 256, 355);
                    }

                    // Bright Crimson Block Badge in the center
                    Rectangle badgeRect = new Rectangle(206, 206, 100, 100);
                    using (LinearGradientBrush badgeBrush = new LinearGradientBrush(
                        new Point(206, 206), new Point(306, 306),
                        Color.FromArgb(239, 68, 68),   // Red #EF4444
                        Color.FromArgb(185, 28, 28)))  // Dark Red #B91C1C
                    {
                        g.FillEllipse(badgeBrush, badgeRect);
                    }
                    using (Pen badgePen = new Pen(Color.White, 6f))
                    {
                        g.DrawEllipse(badgePen, badgeRect);
                        // Slash through block badge (Prohibition line)
                        g.DrawLine(badgePen, 224, 224, 288, 288);
                    }
                }

                // Ensure output directories exist
                string scriptDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectRoot = Path.GetFullPath(Path.Combine(scriptDir, "..\\..\\..\\"));
                
                string assetsDir = Path.Combine(projectRoot, "src\\PexorisFirewallBlocker\\Assets");
                string websiteIconsDir = Path.Combine(projectRoot, "website\\assets\\icons");
                string websiteToolDir = Path.Combine(projectRoot, "website\\tools\\firewall-blocker");

                Directory.CreateDirectory(assetsDir);
                Directory.CreateDirectory(websiteIconsDir);
                Directory.CreateDirectory(websiteToolDir);

                string icoPath = Path.Combine(assetsDir, "app.ico");
                string png1 = Path.Combine(websiteIconsDir, "firewall-blocker.png");
                string png2 = Path.Combine(websiteToolDir, "firewall-blocker.png");

                // Save PNGs
                bmp.Save(png1, ImageFormat.Png);
                bmp.Save(png2, ImageFormat.Png);

                // Build multi-res ICO
                SaveMultiResolutionIcon(bmp, icoPath);

                Console.WriteLine("Successfully created FirewallBlocker assets: " + icoPath);
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

        static void SaveMultiResolutionIcon(Bitmap src, string outputPath)
        {
            int[] sizes = new int[] { 256, 48, 32, 16 };
            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR header
                bw.Write((ushort)0); // reserved
                bw.Write((ushort)1); // 1 = ICO
                bw.Write((ushort)sizes.Length);

                byte[][] pngBuffers = new byte[sizes.Length][];
                for (int i = 0; i < sizes.Length; i++)
                {
                    int sz = sizes[i];
                    using (Bitmap scaled = new Bitmap(sz, sz, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(scaled))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.DrawImage(src, 0, 0, sz, sz);
                        }
                        using (MemoryStream ms = new MemoryStream())
                        {
                            scaled.Save(ms, ImageFormat.Png);
                            pngBuffers[i] = ms.ToArray();
                        }
                    }
                }

                int offset = 6 + (16 * sizes.Length);
                for (int i = 0; i < sizes.Length; i++)
                {
                    int sz = sizes[i];
                    bw.Write((byte)(sz >= 256 ? 0 : sz));
                    bw.Write((byte)(sz >= 256 ? 0 : sz));
                    bw.Write((byte)0); // color palette
                    bw.Write((byte)0); // reserved
                    bw.Write((ushort)1); // color planes
                    bw.Write((ushort)32); // bpp
                    bw.Write((uint)pngBuffers[i].Length);
                    bw.Write((uint)offset);
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
