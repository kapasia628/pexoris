using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace CreateIcon
{
    class Program
    {
        static void Main()
        {
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisNetworkReset\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\network-reset\network-reset.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\network-reset.png";

            Directory.CreateDirectory(Path.GetDirectoryName(outIco));
            Directory.CreateDirectory(Path.GetDirectoryName(outPng));
            Directory.CreateDirectory(Path.GetDirectoryName(outAssetsPng));

            using (Bitmap bmp256 = RenderIcon(256))
            {
                bmp256.Save(outPng, ImageFormat.Png);
                bmp256.Save(outAssetsPng, ImageFormat.Png);

                using (Bitmap bmp48 = RenderIcon(48))
                using (Bitmap bmp32 = RenderIcon(32))
                using (Bitmap bmp16 = RenderIcon(16))
                using (FileStream fs = new FileStream(outIco, FileMode.Create))
                {
                    WriteIco(new Bitmap[] { bmp256, bmp48, bmp32, bmp16 }, fs);
                }
            }
            Console.WriteLine("Icon created successfully at: " + outIco);
        }

        static Bitmap RenderIcon(int size)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);

                // Premium Deep Royal Blue / Cobalt Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(37, 99, 235),   // Royal Blue (#2563EB)
                    Color.FromArgb(15, 23, 42)))    // Slate Navy 900 (#0F172A)
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(90, 56, 189, 248), size * 0.015f)) // Cyan accent
                {
                    g.DrawPath(pen, path);
                }

                float cx = size * 0.5f;
                float cy = size * 0.5f;

                // Circular Reset Orbital Ring (Dual sleek arrows)
                float ringR = size * 0.32f;
                float ringThickness = size * 0.065f;
                using (Pen ringPen = new Pen(Color.FromArgb(220, 14, 165, 233), ringThickness)) // Sky Blue
                {
                    ringPen.StartCap = LineCap.Round;
                    ringPen.EndCap = LineCap.Round;
                    // Upper-right arc
                    g.DrawArc(ringPen, cx - ringR, cy - ringR, ringR * 2, ringR * 2, 210, 120);
                    // Lower-left arc
                    g.DrawArc(ringPen, cx - ringR, cy - ringR, ringR * 2, ringR * 2, 30, 120);
                }

                // Arrow heads for the dual orbital ring
                // Top-right arrow head
                PointF topArrowTip = PointOnCircle(cx, cy, ringR, 330);
                DrawArrowHead(g, topArrowTip, 60, size * 0.08f, Color.FromArgb(56, 189, 248));

                // Bottom-left arrow head
                PointF botArrowTip = PointOnCircle(cx, cy, ringR, 150);
                DrawArrowHead(g, botArrowTip, 240, size * 0.08f, Color.FromArgb(56, 189, 248));

                // Central Network Hub Nodes (3-node topology linked with glowing data cables)
                float nodeR = size * 0.055f;
                PointF nTop = new PointF(cx, cy - (size * 0.16f));
                PointF nLeft = new PointF(cx - (size * 0.14f), cy + (size * 0.12f));
                PointF nRight = new PointF(cx + (size * 0.14f), cy + (size * 0.12f));
                PointF nCenter = new PointF(cx, cy);

                // Network link lines
                using (Pen linkPen = new Pen(Color.FromArgb(200, 255, 255, 255), size * 0.035f))
                {
                    linkPen.StartCap = LineCap.Round;
                    linkPen.EndCap = LineCap.Round;
                    g.DrawLine(linkPen, nTop, nCenter);
                    g.DrawLine(linkPen, nLeft, nCenter);
                    g.DrawLine(linkPen, nRight, nCenter);
                }

                // Center hub core (Cyan)
                float centerR = size * 0.075f;
                using (SolidBrush hubBrush = new SolidBrush(Color.FromArgb(14, 165, 233)))
                using (Pen hubPen = new Pen(Color.White, size * 0.03f))
                {
                    g.FillEllipse(hubBrush, nCenter.X - centerR, nCenter.Y - centerR, centerR * 2, centerR * 2);
                    g.DrawEllipse(hubPen, nCenter.X - centerR, nCenter.Y - centerR, centerR * 2, centerR * 2);
                }

                // Peripheral nodes (White with cyan core)
                DrawNetworkNode(g, nTop, nodeR, size);
                DrawNetworkNode(g, nLeft, nodeR, size);
                DrawNetworkNode(g, nRight, nodeR, size);

                // Center core lightning / recovery spark
                float sparkW = size * 0.045f;
                using (SolidBrush sparkBrush = new SolidBrush(Color.White))
                {
                    PointF[] spark = new PointF[] {
                        new PointF(cx - sparkW * 0.4f, cy - sparkW * 0.9f),
                        new PointF(cx + sparkW * 0.5f, cy - sparkW * 0.2f),
                        new PointF(cx + sparkW * 0.05f, cy - sparkW * 0.1f),
                        new PointF(cx + sparkW * 0.4f, cy + sparkW * 0.9f),
                        new PointF(cx - sparkW * 0.5f, cy + sparkW * 0.2f),
                        new PointF(cx - sparkW * 0.05f, cy + sparkW * 0.1f)
                    };
                    g.FillPolygon(sparkBrush, spark);
                }
            }
            return bmp;
        }

        static void DrawNetworkNode(Graphics g, PointF pt, float r, int size)
        {
            using (SolidBrush fill = new SolidBrush(Color.White))
            using (Pen border = new Pen(Color.FromArgb(14, 165, 233), size * 0.02f))
            {
                g.FillEllipse(fill, pt.X - r, pt.Y - r, r * 2, r * 2);
                g.DrawEllipse(border, pt.X - r, pt.Y - r, r * 2, r * 2);
            }
        }

        static PointF PointOnCircle(float cx, float cy, float r, float angleDegrees)
        {
            double rad = angleDegrees * Math.PI / 180.0;
            return new PointF(cx + (float)(r * Math.Cos(rad)), cy + (float)(r * Math.Sin(rad)));
        }

        static void DrawArrowHead(Graphics g, PointF tip, float angleDegrees, float length, Color color)
        {
            double rad = angleDegrees * Math.PI / 180.0;
            double leftRad = (angleDegrees + 150) * Math.PI / 180.0;
            double rightRad = (angleDegrees - 150) * Math.PI / 180.0;

            PointF pLeft = new PointF(tip.X + (float)(length * Math.Cos(leftRad)), tip.Y + (float)(length * Math.Sin(leftRad)));
            PointF pRight = new PointF(tip.X + (float)(length * Math.Cos(rightRad)), tip.Y + (float)(length * Math.Sin(rightRad)));

            using (SolidBrush b = new SolidBrush(color))
            {
                g.FillPolygon(b, new PointF[] { tip, pLeft, pRight });
            }
        }

        static GraphicsPath GetRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void WriteIco(Bitmap[] bitmaps, Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write((short)0); // reserved
            bw.Write((short)1); // ICO type
            bw.Write((short)bitmaps.Length); // count

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
                Bitmap b = bitmaps[i];
                bw.Write((byte)(b.Width >= 256 ? 0 : b.Width));
                bw.Write((byte)(b.Height >= 256 ? 0 : b.Height));
                bw.Write((byte)0); // palette
                bw.Write((byte)0); // reserved
                bw.Write((short)1); // color planes
                bw.Write((short)32); // bpp
                bw.Write(pngBytes[i].Length); // size
                bw.Write(offset); // offset
                offset += pngBytes[i].Length;
            }

            for (int i = 0; i < bitmaps.Length; i++)
            {
                bw.Write(pngBytes[i]);
            }
        }
    }
}
