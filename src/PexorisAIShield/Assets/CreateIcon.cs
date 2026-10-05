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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisAIShield\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\ai-shield\ai-shield.png";

            Directory.CreateDirectory(Path.GetDirectoryName(outIco));
            Directory.CreateDirectory(Path.GetDirectoryName(outPng));

            using (Bitmap bmp256 = RenderIcon(256))
            {
                bmp256.Save(outPng, ImageFormat.Png);

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

                // Violet / Indigo Royal Gradient Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedRect(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(99, 102, 241),    // #6366F1 Indigo 500
                    Color.FromArgb(139, 92, 246)))   // #8B5CF6 Violet 500
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle glow border
                using (GraphicsPath path = GetRoundedRect(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(160, 255, 255, 255), size * 0.02f))
                {
                    g.DrawPath(pen, path);
                }

                // Security Shield Silhouette
                float cx = size * 0.5f;
                float topY = size * 0.24f;
                float shW = size * 0.48f;
                float shH = size * 0.52f;

                using (GraphicsPath shield = new GraphicsPath())
                {
                    shield.StartFigure();
                    shield.AddLine(cx - (shW / 2), topY, cx + (shW / 2), topY);
                    shield.AddBezier(
                        cx + (shW / 2), topY,
                        cx + (shW / 2), topY + (shH * 0.45f),
                        cx + (shW * 0.35f), topY + (shH * 0.82f),
                        cx, topY + shH);
                    shield.AddBezier(
                        cx, topY + shH,
                        cx - (shW * 0.35f), topY + (shH * 0.82f),
                        cx - (shW / 2), topY + (shH * 0.45f),
                        cx - (shW / 2), topY);
                    shield.CloseFigure();

                    // Semi-translucent Shield Body
                    using (SolidBrush shBrush = new SolidBrush(Color.FromArgb(50, 255, 255, 255)))
                    {
                        g.FillPath(shBrush, shield);
                    }
                    using (Pen shPen = new Pen(Color.FromArgb(240, 255, 255, 255), size * 0.038f))
                    {
                        shPen.LineJoin = LineJoin.Round;
                        g.DrawPath(shPen, shield);
                    }
                }

                // AI Neural Circuit Core (Center Eye / Digital Slash)
                float coreY = topY + (shH * 0.42f);
                float dotR = size * 0.065f;

                // Center glowing AI core
                using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                {
                    g.FillEllipse(dotBrush, cx - dotR, coreY - dotR, dotR * 2, dotR * 2);
                }

                // Digital Shield Slash / Cross protection beam
                using (Pen beamPen = new Pen(Color.FromArgb(255, 255, 255), size * 0.032f))
                {
                    beamPen.StartCap = LineCap.Round;
                    beamPen.EndCap = LineCap.Round;

                    // Circuit trace lines
                    g.DrawLine(beamPen, cx - (size * 0.14f), coreY, cx - (dotR * 1.5f), coreY);
                    g.DrawLine(beamPen, cx + (dotR * 1.5f), coreY, cx + (size * 0.14f), coreY);
                    g.DrawLine(beamPen, cx, coreY - (size * 0.12f), cx, coreY - (dotR * 1.5f));
                    g.DrawLine(beamPen, cx, coreY + (dotR * 1.5f), cx, coreY + (size * 0.15f));
                }
            }
            return bmp;
        }

        static GraphicsPath GetRoundedRect(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void WriteIco(Bitmap[] bitmaps, Stream stream)
        {
            BinaryWriter bw = new BinaryWriter(stream);
            bw.Write((short)0); // reserved
            bw.Write((short)1); // type 1 = ICO
            bw.Write((short)bitmaps.Length);

            int offset = 6 + (16 * bitmaps.Length);
            byte[][] pngBytes = new byte[bitmaps.Length][];

            for (int i = 0; i < bitmaps.Length; i++)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmaps[i].Save(ms, ImageFormat.Png);
                    pngBytes[i] = ms.ToArray();
                }

                int w = bitmaps[i].Width >= 256 ? 0 : bitmaps[i].Width;
                int h = bitmaps[i].Height >= 256 ? 0 : bitmaps[i].Height;

                bw.Write((byte)w);
                bw.Write((byte)h);
                bw.Write((byte)0); // colors
                bw.Write((byte)0); // reserved
                bw.Write((short)1); // planes
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
