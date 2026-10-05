using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Pexoris.PrintFixer.Assets
{
    public class CreateIcon
    {
        static void Main(string[] args)
        {
            string outIco = args.Length > 0 ? args[0] : @"src\PexorisPrintFixer\Assets\app.ico";
            string dir = Path.GetDirectoryName(outIco);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            int[] sizes = new int[] { 256, 128, 64, 48, 32, 24, 16 };
            Bitmap[] bitmaps = new Bitmap[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                bitmaps[i] = RenderStunningPrintFixerIcon(sizes[i]);
            }

            SaveMultiIcon(bitmaps, outIco);
            Console.WriteLine("PrintFixer Icon created successfully at: " + outIco);

            // Also export 256 PNG for web assets
            Bitmap bmp256 = RenderStunningPrintFixerIcon(256);
            string pngWebDir = @"website\assets\icons";
            if (!Directory.Exists(pngWebDir)) Directory.CreateDirectory(pngWebDir);
            bmp256.Save(Path.Combine(pngWebDir, "print-fixer.png"), ImageFormat.Png);

            string pngToolDir = @"website\tools\print-fixer";
            if (!Directory.Exists(pngToolDir)) Directory.CreateDirectory(pngToolDir);
            bmp256.Save(Path.Combine(pngToolDir, "print-fixer.png"), ImageFormat.Png);

            // Also copy app.ico as favicon.ico in tool folder
            File.Copy(outIco, Path.Combine(pngToolDir, "favicon.ico"), true);

            Console.WriteLine("Exported PrintFixer PNGs and favicon to web assets!");
        }

        public static Bitmap RenderStunningPrintFixerIcon(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                float s = size;
                float pad = s * 0.08f;
                float shadowOffset = s * 0.04f;

                // 1. Drop Shadow
                RectangleF shadowRect = new RectangleF(pad, pad + shadowOffset, s - pad * 2, s - pad * 2);
                using (GraphicsPath sp = GetRoundedRect(shadowRect, s * 0.24f))
                {
                    using (PathGradientBrush pgb = new PathGradientBrush(sp))
                    {
                        pgb.CenterColor = Color.FromArgb(90, 10, 40, 30);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(pgb, sp);
                    }
                }

                // 2. Modern Squircle Badge (Teal Cyan to Emerald Deep Navy Gradient)
                RectangleF badgeRect = new RectangleF(pad, pad, s - pad * 2, s - pad * 2);
                using (GraphicsPath badgePath = GetRoundedRect(badgeRect, s * 0.24f))
                {
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                        badgeRect,
                        Color.FromArgb(255, 16, 185, 129), // Emerald
                        Color.FromArgb(255, 6, 78, 59),    // Deep Pine Green
                        LinearGradientMode.ForwardDiagonal))
                    {
                        ColorBlend cb = new ColorBlend();
                        cb.Colors = new Color[] {
                            Color.FromArgb(255, 6, 182, 212),  // Vivid Cyan #06B6D4
                            Color.FromArgb(255, 16, 185, 129), // Emerald #10B981
                            Color.FromArgb(255, 15, 23, 42)    // Dark Slate #0F172A
                        };
                        cb.Positions = new float[] { 0f, 0.45f, 1f };
                        bgBrush.InterpolationColors = cb;
                        g.FillPath(bgBrush, badgePath);
                    }

                    // Glassmorphism Highlight Border
                    using (Pen innerPen = new Pen(Color.FromArgb(190, 255, 255, 255), Math.Max(1f, s * 0.035f)))
                    {
                        innerPen.Alignment = PenAlignment.Inset;
                        g.DrawPath(innerPen, badgePath);
                    }
                }

                // 3. Top Input Paper Sheet
                float paperW = s * 0.46f;
                float paperH = s * 0.30f;
                float paperX = (s - paperW) / 2f;
                float paperY = s * 0.22f;
                RectangleF topPaperRect = new RectangleF(paperX, paperY, paperW, paperH);
                using (GraphicsPath paperPath = GetRoundedRect(topPaperRect, s * 0.05f))
                {
                    using (SolidBrush paperB = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                    {
                        g.FillPath(paperB, paperPath);
                    }
                    using (Pen paperBorder = new Pen(Color.FromArgb(160, 220, 240, 255), Math.Max(1f, s * 0.02f)))
                    {
                        g.DrawPath(paperBorder, paperPath);
                    }
                }

                // Subtle text lines on top paper
                if (size >= 32)
                {
                    using (Pen linePen = new Pen(Color.FromArgb(120, 100, 116, 139), Math.Max(1f, s * 0.025f)))
                    {
                        g.DrawLine(linePen, paperX + paperW * 0.2f, paperY + paperH * 0.35f, paperX + paperW * 0.8f, paperY + paperH * 0.35f);
                        g.DrawLine(linePen, paperX + paperW * 0.2f, paperY + paperH * 0.60f, paperX + paperW * 0.6f, paperY + paperH * 0.60f);
                    }
                }

                // 4. Printer Body Chassis (Modern Apple-style rounded box)
                float bodyW = s * 0.66f;
                float bodyH = s * 0.34f;
                float bodyX = (s - bodyW) / 2f;
                float bodyY = s * 0.44f;
                RectangleF bodyRect = new RectangleF(bodyX, bodyY, bodyW, bodyH);

                // Body Drop Shadow
                RectangleF bodyShadow = new RectangleF(bodyX, bodyY + s * 0.03f, bodyW, bodyH);
                using (GraphicsPath bsp = GetRoundedRect(bodyShadow, s * 0.08f))
                {
                    using (SolidBrush sB = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                    {
                        g.FillPath(sB, bsp);
                    }
                }

                using (GraphicsPath bodyPath = GetRoundedRect(bodyRect, s * 0.08f))
                {
                    using (LinearGradientBrush bodyBrush = new LinearGradientBrush(
                        bodyRect,
                        Color.FromArgb(255, 255, 255, 255), // Pure crisp white
                        Color.FromArgb(255, 226, 232, 240), // Sleek light slate
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(bodyBrush, bodyPath);
                    }
                    using (Pen bodyPen = new Pen(Color.FromArgb(255, 203, 213, 225), Math.Max(1f, s * 0.025f)))
                    {
                        g.DrawPath(bodyPen, bodyPath);
                    }
                }

                // Printer Output Slot (Dark sleek bar)
                float slotW = s * 0.48f;
                float slotH = s * 0.065f;
                float slotX = (s - slotW) / 2f;
                float slotY = bodyY + bodyH * 0.58f;
                RectangleF slotRect = new RectangleF(slotX, slotY, slotW, slotH);
                using (GraphicsPath slotPath = GetRoundedRect(slotRect, s * 0.03f))
                {
                    using (SolidBrush slotB = new SolidBrush(Color.FromArgb(255, 30, 41, 59)))
                    {
                        g.FillPath(slotB, slotPath);
                    }
                }

                // 5. Emerging Output Paper with Fresh Crisp Print
                float outW = s * 0.44f;
                float outH = s * 0.26f;
                float outX = (s - outW) / 2f;
                float outY = slotY + slotH * 0.5f;
                RectangleF outRect = new RectangleF(outX, outY, outW, outH);
                using (GraphicsPath outPath = GetRoundedRect(outRect, s * 0.05f))
                {
                    using (LinearGradientBrush outBrush = new LinearGradientBrush(
                        outRect,
                        Color.FromArgb(255, 255, 255, 255),
                        Color.FromArgb(255, 241, 245, 249),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(outBrush, outPath);
                    }
                    using (Pen outPen = new Pen(Color.FromArgb(200, 203, 213, 225), Math.Max(1f, s * 0.02f)))
                    {
                        g.DrawPath(outPen, outPath);
                    }
                }

                // 6. Vivid Green "Fixed / Healthy" Status LED & Clean Spark
                float ledR = Math.Max(2f, s * 0.038f);
                float ledX = bodyX + bodyW * 0.82f;
                float ledY = bodyY + bodyH * 0.28f;
                using (SolidBrush ledB = new SolidBrush(Color.FromArgb(255, 16, 185, 129))) // Emerald Green
                {
                    g.FillEllipse(ledB, ledX - ledR, ledY - ledR, ledR * 2, ledR * 2);
                }
                // LED Glow
                using (Pen glowPen = new Pen(Color.FromArgb(140, 52, 211, 153), Math.Max(1f, s * 0.02f)))
                {
                    g.DrawEllipse(glowPen, ledX - ledR * 1.5f, ledY - ledR * 1.5f, ledR * 3, ledR * 3);
                }

                // Lightning Fix Spark / Ready Checkmark on Output Paper
                if (size >= 32)
                {
                    PointF[] chkPoints = new PointF[] {
                        new PointF(outX + outW * 0.32f, outY + outH * 0.48f),
                        new PointF(outX + outW * 0.46f, outY + outH * 0.68f),
                        new PointF(outX + outW * 0.72f, outY + outH * 0.28f)
                    };
                    using (Pen chkPen = new Pen(Color.FromArgb(255, 16, 185, 129), Math.Max(2f, s * 0.05f)))
                    {
                        chkPen.StartCap = LineCap.Round;
                        chkPen.EndCap = LineCap.Round;
                        chkPen.LineJoin = LineJoin.Round;
                        g.DrawLines(chkPen, chkPoints);
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
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((ushort)0); // Reserved
                bw.Write((ushort)1); // Type: 1 = ICO
                bw.Write((ushort)bitmaps.Length); // Count

                byte[][] pngBytes = new byte[bitmaps.Length][];
                for (int i = 0; i < bitmaps.Length; i++)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmaps[i].Save(ms, ImageFormat.Png);
                        pngBytes[i] = ms.ToArray();
                    }
                }

                int offset = 6 + (16 * bitmaps.Length);
                for (int i = 0; i < bitmaps.Length; i++)
                {
                    bw.Write((byte)(bitmaps[i].Width >= 256 ? 0 : bitmaps[i].Width));
                    bw.Write((byte)(bitmaps[i].Height >= 256 ? 0 : bitmaps[i].Height));
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write((ushort)1);  // Planes
                    bw.Write((ushort)32); // BPP
                    bw.Write((uint)pngBytes[i].Length);
                    bw.Write((uint)offset);
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
