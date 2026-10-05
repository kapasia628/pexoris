using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Pexoris.PortKiller.Assets
{
    public class CreateIcon
    {
        static void Main(string[] args)
        {
            string outIco = args.Length > 0 ? args[0] : "app.ico";
            int[] sizes = new int[] { 256, 128, 64, 48, 32, 24, 16 };
            Bitmap[] bitmaps = new Bitmap[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                bitmaps[i] = RenderStunningPortKillerIcon(sizes[i]);
            }

            SaveMultiIcon(bitmaps, outIco);
            Console.WriteLine("PortKiller Icon created successfully at: " + outIco);

            // Also save 256 PNG for web
            Bitmap bmp256 = RenderStunningPortKillerIcon(256);
            string pngWebPath = @"c:\xampp\htdocs\tools\website\assets\icons\port-killer.png";
            string pngToolPath = @"c:\xampp\htdocs\tools\website\tools\port-killer\port-killer.png";
            string pngToolDir = Path.GetDirectoryName(pngToolPath);
            if (!Directory.Exists(pngToolDir)) Directory.CreateDirectory(pngToolDir);

            bmp256.Save(pngWebPath, ImageFormat.Png);
            bmp256.Save(pngToolPath, ImageFormat.Png);
            Console.WriteLine("Exported PortKiller PNGs to web assets!");
        }

        public static Bitmap RenderStunningPortKillerIcon(int size)
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
                        pgb.CenterColor = Color.FromArgb(90, 20, 10, 60);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(pgb, sp);
                    }
                }

                // 2. Modern Squircle Badge (Midnight Navy & Electric Indigo Gradient)
                RectangleF badgeRect = new RectangleF(pad, pad, s - pad * 2, s - pad * 2);
                using (GraphicsPath badgePath = GetRoundedRect(badgeRect, s * 0.24f))
                {
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                        badgeRect,
                        Color.FromArgb(255, 14, 165, 233), // Vivid Sky
                        Color.FromArgb(255, 30, 27, 75),    // Deep Midnight Indigo
                        LinearGradientMode.ForwardDiagonal))
                    {
                        ColorBlend cb = new ColorBlend();
                        cb.Colors = new Color[] {
                            Color.FromArgb(255, 14, 165, 233), // Top-left: Cyan #0EA5E9
                            Color.FromArgb(255, 79, 70, 229),  // Center: Royal Indigo #4F46E5
                            Color.FromArgb(255, 15, 23, 42)    // Bottom-right: Dark Slate #0F172A
                        };
                        cb.Positions = new float[] { 0f, 0.5f, 1f };
                        bgBrush.InterpolationColors = cb;
                        g.FillPath(bgBrush, badgePath);
                    }

                    // Glassmorphism Highlight Border
                    using (Pen innerPen = new Pen(Color.FromArgb(180, 255, 255, 255), Math.Max(1f, s * 0.035f)))
                    {
                        innerPen.Alignment = PenAlignment.Inset;
                        g.DrawPath(innerPen, badgePath);
                    }
                }

                // 3. Network Port Ring (Ethernet / Socket Circular Arc)
                float ringR = s * 0.28f;
                float ringX = s / 2f;
                float ringY = s * 0.52f;
                RectangleF ringRect = new RectangleF(ringX - ringR, ringY - ringR, ringR * 2, ringR * 2);
                using (Pen ringPen = new Pen(Color.FromArgb(120, 255, 255, 255), Math.Max(1.5f, s * 0.045f)))
                {
                    ringPen.DashStyle = DashStyle.Dash;
                    g.DrawEllipse(ringPen, ringRect);
                }

                // 4. Vibrant Golden Lightning Bolt (The Port "Killer")
                PointF[] boltPoints = new PointF[] {
                    new PointF(s * 0.54f, s * 0.16f), // Top peak
                    new PointF(s * 0.32f, s * 0.50f), // Mid left inner
                    new PointF(s * 0.48f, s * 0.50f), // Mid inner notch
                    new PointF(s * 0.38f, s * 0.84f), // Bottom sharp tip
                    new PointF(s * 0.68f, s * 0.44f), // Mid right outer
                    new PointF(s * 0.52f, s * 0.44f)  // Mid inner return
                };

                // Bolt Shadow
                PointF[] shadowBolt = new PointF[boltPoints.Length];
                for (int i = 0; i < boltPoints.Length; i++)
                {
                    shadowBolt[i] = new PointF(boltPoints[i].X, boltPoints[i].Y + s * 0.025f);
                }
                using (SolidBrush sbBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                {
                    g.FillPolygon(sbBrush, shadowBolt);
                }

                // Bolt Gradient: Electric White to Vivid Gold/Amber
                using (GraphicsPath boltPath = new GraphicsPath())
                {
                    boltPath.AddPolygon(boltPoints);
                    using (LinearGradientBrush boltBrush = new LinearGradientBrush(
                        badgeRect,
                        Color.FromArgb(255, 255, 255, 255), // Pure White Core
                        Color.FromArgb(255, 245, 158, 11),  // Vivid Amber #F59E0B
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(boltBrush, boltPath);
                    }

                    using (Pen boltBorder = new Pen(Color.FromArgb(255, 254, 243, 199), Math.Max(1f, s * 0.02f)))
                    {
                        g.DrawPath(boltBorder, boltPath);
                    }
                }

                // 5. Energy Spark
                if (size >= 24)
                {
                    float sparkX = s * 0.70f;
                    float sparkY = s * 0.28f;
                    float sparkR = Math.Max(1.5f, s * 0.04f);

                    using (SolidBrush sparkB = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
                    {
                        g.FillEllipse(sparkB, sparkX - sparkR, sparkY - sparkR, sparkR * 2, sparkR * 2);
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
