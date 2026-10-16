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

                    // Rounded Squircle Container with Royal Purple / Violet gradient
                    Rectangle rect = new Rectangle(24, 24, 464, 464);
                    using (GraphicsPath path = GetRoundedRect(rect, 108))
                    {
                        using (LinearGradientBrush brush = new LinearGradientBrush(
                            new Point(24, 24), new Point(488, 488),
                            Color.FromArgb(124, 58, 237),  // #7C3AED (Violet)
                            Color.FromArgb(76, 29, 149)))   // #4C1D95 (Deep Purple)
                        {
                            g.FillPath(brush, path);
                        }

                        // Subtle inner glow border
                        using (Pen pen = new Pen(Color.FromArgb(120, 196, 181, 253), 6f)) // #C4B5FD
                        {
                            g.DrawPath(pen, path);
                        }
                    }

                    // Shortcut File Card Document Base (White rounded rectangle)
                    Rectangle docRect = new Rectangle(140, 110, 232, 280);
                    using (GraphicsPath docPath = GetRoundedRect(docRect, 28))
                    {
                        using (SolidBrush docBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                        {
                            g.FillPath(docBrush, docPath);
                        }
                        using (Pen docPen = new Pen(Color.FromArgb(226, 232, 240), 4f))
                        {
                            g.DrawPath(docPen, docPath);
                        }
                    }

                    // Document Header Bar (Soft purple accent inside document)
                    Rectangle headRect = new Rectangle(160, 134, 192, 40);
                    using (GraphicsPath headPath = GetRoundedRect(headRect, 10))
                    {
                        using (SolidBrush headBrush = new SolidBrush(Color.FromArgb(237, 233, 254))) // #EDE9FE
                        {
                            g.FillPath(headBrush, headPath);
                        }
                    }

                    // Medical Cross / Plus Badge in center of document (Doctor repair motif)
                    int crossW = 22;
                    int crossL = 70;
                    int cx = 256;
                    int cy = 250;
                    using (SolidBrush crossBrush = new SolidBrush(Color.FromArgb(109, 40, 217))) // #6D28D9
                    {
                        g.FillRectangle(crossBrush, cx - crossW / 2, cy - crossL / 2, crossW, crossL);
                        g.FillRectangle(crossBrush, cx - crossL / 2, cy - crossW / 2, crossL, crossW);
                    }

                    // Windows Shortcut Curved Jump Arrow Badge at bottom-left corner
                    Rectangle arrowBadgeRect = new Rectangle(110, 310, 110, 110);
                    using (GraphicsPath arrowBadge = GetRoundedRect(arrowBadgeRect, 28))
                    {
                        using (LinearGradientBrush abBrush = new LinearGradientBrush(
                            new Point(110, 310), new Point(220, 420),
                            Color.FromArgb(37, 99, 235),   // #2563EB
                            Color.FromArgb(29, 78, 216)))  // #1D4ED8
                        {
                            g.FillPath(abBrush, arrowBadge);
                        }
                        using (Pen abPen = new Pen(Color.White, 5f))
                        {
                            g.DrawPath(abPen, arrowBadge);
                        }
                    }

                    // Draw shortcut jump curved arrow inside badge
                    using (GraphicsPath arrowPath = new GraphicsPath())
                    {
                        // Curved trunk and arrow head
                        PointF p1 = new PointF(138, 385);
                        PointF p2 = new PointF(158, 350);
                        PointF p3 = new PointF(185, 342);

                        using (Pen arrowPen = new Pen(Color.White, 8f))
                        {
                            arrowPen.StartCap = LineCap.Round;
                            arrowPen.EndCap = LineCap.Round;
                            g.DrawCurve(arrowPen, new PointF[] { p1, p2, p3 });
                        }

                        // Arrowhead
                        PointF tip = new PointF(195, 332);
                        PointF left = new PointF(170, 330);
                        PointF right = new PointF(192, 355);

                        using (SolidBrush tipBrush = new SolidBrush(Color.White))
                        {
                            g.FillPolygon(tipBrush, new PointF[] { tip, left, right });
                        }
                    }

                    // Stethoscope / Sparkle Star in top right of card (repair perfection)
                    PointF starCenter = new PointF(365, 115);
                    DrawSparkle(g, starCenter, 22, Color.FromArgb(250, 204, 21)); // Gold star
                }

                // Ensure directories exist
                string scriptDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectRoot = Path.GetFullPath(Path.Combine(scriptDir, "..\\..\\..\\"));

                string assetsDir = Path.Combine(projectRoot, "src\\PexorisShortcutDoctor\\Assets");
                string websiteIconsDir = Path.Combine(projectRoot, "website\\assets\\icons");
                string websiteToolDir = Path.Combine(projectRoot, "website\\tools\\shortcut-doctor");

                Directory.CreateDirectory(assetsDir);
                Directory.CreateDirectory(websiteIconsDir);
                Directory.CreateDirectory(websiteToolDir);

                string icoPath = Path.Combine(assetsDir, "app.ico");
                string png1 = Path.Combine(websiteIconsDir, "shortcut-doctor.png");
                string png2 = Path.Combine(websiteToolDir, "shortcut-doctor.png");

                // Save PNGs
                bmp.Save(png1, ImageFormat.Png);
                bmp.Save(png2, ImageFormat.Png);

                // Build multi-res ICO
                SaveMultiResolutionIcon(bmp, icoPath);

                Console.WriteLine("Successfully created ShortcutDoctor assets: " + icoPath);
            }
        }

        static void DrawSparkle(Graphics g, PointF center, float radius, Color color)
        {
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddLine(center.X, center.Y - radius, center.X + radius * 0.3f, center.Y - radius * 0.3f);
                p.AddLine(center.X + radius * 0.3f, center.Y - radius * 0.3f, center.X + radius, center.Y);
                p.AddLine(center.X + radius, center.Y, center.X + radius * 0.3f, center.Y + radius * 0.3f);
                p.AddLine(center.X + radius * 0.3f, center.Y + radius * 0.3f, center.X, center.Y + radius);
                p.AddLine(center.X, center.Y + radius, center.X - radius * 0.3f, center.Y + radius * 0.3f);
                p.AddLine(center.X - radius * 0.3f, center.Y + radius * 0.3f, center.X - radius, center.Y);
                p.AddLine(center.X - radius, center.Y, center.X - radius * 0.3f, center.Y - radius * 0.3f);
                p.CloseFigure();

                using (SolidBrush b = new SolidBrush(color))
                {
                    g.FillPath(b, p);
                }
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
