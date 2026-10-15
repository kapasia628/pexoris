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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisAudioFixer\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\audio-fixer\audio-fixer.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\audio-fixer.png";

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

                // Deep Indigo to Violet Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(79, 70, 229),   // Indigo 600 (#4F46E5)
                    Color.FromArgb(15, 23, 42)))    // Slate Navy 900 (#0F172A)
                {
                    g.FillPath(brush, path);
                }

                // Inner glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(90, 165, 180, 252), size * 0.015f))
                {
                    g.DrawPath(pen, path);
                }

                float cx = size * 0.5f;
                float cy = size * 0.5f;

                // Dynamic Audio Frequency Equalizer Waves (Right Side)
                float[] waveRadii = new float[] { size * 0.28f, size * 0.38f, size * 0.47f };
                int[] waveAlphas = new int[] { 250, 180, 110 };

                for (int i = 0; i < waveRadii.Length; i++)
                {
                    float wr = waveRadii[i];
                    using (Pen wavePen = new Pen(Color.FromArgb(waveAlphas[i], 129, 140, 248), size * (0.045f - (i * 0.008f))))
                    {
                        wavePen.StartCap = LineCap.Round;
                        wavePen.EndCap = LineCap.Round;
                        // Right acoustic emission arc (-45 to +45 deg)
                        g.DrawArc(wavePen, cx - wr - (size * 0.04f), cy - wr, wr * 2, wr * 2, -45, 90);
                    }
                }

                // Speaker Cone Vector Geometry
                float spkLeft = size * 0.22f;
                float spkBoxW = size * 0.14f;
                float spkBoxH = size * 0.24f;
                float spkTop = cy - (spkBoxH * 0.5f);
                float spkConeW = size * 0.17f;
                float spkConeH = size * 0.52f;

                // Speaker Body & Flare
                using (GraphicsPath spkPath = new GraphicsPath())
                {
                    // Rectangle base
                    spkPath.AddLine(spkLeft, spkTop, spkLeft + spkBoxW, spkTop);
                    // Flare up to cone top
                    spkPath.AddLine(spkLeft + spkBoxW, spkTop, spkLeft + spkBoxW + spkConeW, cy - (spkConeH * 0.5f));
                    // Cone front vertical curve
                    spkPath.AddLine(spkLeft + spkBoxW + spkConeW, cy - (spkConeH * 0.5f), spkLeft + spkBoxW + spkConeW, cy + (spkConeH * 0.5f));
                    // Flare down to cone bot
                    spkPath.AddLine(spkLeft + spkBoxW + spkConeW, cy + (spkConeH * 0.5f), spkLeft + spkBoxW, spkTop + spkBoxH);
                    // Base bottom
                    spkPath.AddLine(spkLeft + spkBoxW, spkTop + spkBoxH, spkLeft, spkTop + spkBoxH);
                    spkPath.CloseFigure();

                    // Pure white fill with slight soft gradient
                    using (LinearGradientBrush spkBrush = new LinearGradientBrush(
                        new PointF(spkLeft, spkTop),
                        new PointF(spkLeft + spkBoxW + spkConeW, cy + (spkConeH * 0.5f)),
                        Color.FromArgb(255, 255, 255),
                        Color.FromArgb(224, 231, 255)))
                    {
                        g.FillPath(spkBrush, spkPath);
                    }
                }

                // Speaker inner acoustic driver core
                float coreW = size * 0.035f;
                float coreH = spkConeH * 0.65f;
                using (GraphicsPath corePath = new GraphicsPath())
                {
                    corePath.AddArc(spkLeft + spkBoxW + spkConeW - (coreW * 1.5f), cy - (coreH * 0.5f), coreW * 2, coreH, -90, 180);
                    using (Pen corePen = new Pen(Color.FromArgb(165, 180, 252), size * 0.025f))
                    {
                        corePen.StartCap = LineCap.Round;
                        corePen.EndCap = LineCap.Round;
                        g.DrawPath(corePen, corePath);
                    }
                }

                // Repair Spark / Lightning Bolt Accent (Bottom Right)
                float boltX = size * 0.64f;
                float boltY = size * 0.58f;
                float boltScale = size * 0.28f;

                using (GraphicsPath boltPath = new GraphicsPath())
                {
                    boltPath.AddPolygon(new PointF[] {
                        new PointF(boltX + (boltScale * 0.45f), boltY),
                        new PointF(boltX + (boltScale * 0.05f), boltY + (boltScale * 0.55f)),
                        new PointF(boltX + (boltScale * 0.38f), boltY + (boltScale * 0.55f)),
                        new PointF(boltX + (boltScale * 0.18f), boltY + (boltScale * 1.00f)),
                        new PointF(boltX + (boltScale * 0.85f), boltY + (boltScale * 0.40f)),
                        new PointF(boltX + (boltScale * 0.52f), boltY + (boltScale * 0.40f)),
                        new PointF(boltX + (boltScale * 0.75f), boltY)
                    });

                    // Glowing Amber / Emerald Lightning Bolt
                    using (LinearGradientBrush boltBrush = new LinearGradientBrush(
                        new PointF(boltX, boltY),
                        new PointF(boltX + boltScale, boltY + boltScale),
                        Color.FromArgb(56, 189, 248),   // Light Sky Blue (#38BDF8)
                        Color.FromArgb(16, 185, 129)))  // Emerald Green (#10B981)
                    {
                        g.FillPath(boltBrush, boltPath);
                    }

                    using (Pen boltPen = new Pen(Color.FromArgb(255, 255, 255), size * 0.02f))
                    {
                        boltPen.LineJoin = LineJoin.Round;
                        g.DrawPath(boltPen, boltPath);
                    }
                }
            }
            return bmp;
        }

        static GraphicsPath GetRoundedPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            RectangleF arc = new RectangleF(rect.X, rect.Y, diameter, diameter);

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void WriteIco(Bitmap[] images, Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((ushort)0); // reserved
            writer.Write((ushort)1); // ICO type
            writer.Write((ushort)images.Length);

            int offset = 6 + (16 * images.Length);

            byte[][] pngBytes = new byte[images.Length][];
            for (int i = 0; i < images.Length; i++)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    images[i].Save(ms, ImageFormat.Png);
                    pngBytes[i] = ms.ToArray();
                }
            }

            for (int i = 0; i < images.Length; i++)
            {
                int w = images[i].Width >= 256 ? 0 : images[i].Width;
                int h = images[i].Height >= 256 ? 0 : images[i].Height;

                writer.Write((byte)w);
                writer.Write((byte)h);
                writer.Write((byte)0); // colors
                writer.Write((byte)0); // reserved
                writer.Write((ushort)1); // color planes
                writer.Write((ushort)32); // bpp
                writer.Write((uint)pngBytes[i].Length);
                writer.Write((uint)offset);

                offset += pngBytes[i].Length;
            }

            for (int i = 0; i < images.Length; i++)
            {
                writer.Write(pngBytes[i]);
            }
        }
    }
}
