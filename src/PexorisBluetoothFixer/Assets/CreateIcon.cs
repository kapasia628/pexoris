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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisBluetoothFixer\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\bluetooth-fixer\bluetooth-fixer.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\bluetooth-fixer.png";

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

                // Deep Azure to Cobalt Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(29, 78, 216),   // Blue 700 (#1D4ED8)
                    Color.FromArgb(15, 23, 42)))    // Slate Navy 900 (#0F172A)
                {
                    g.FillPath(brush, path);
                }

                // Inner electric blue glow border
                using (GraphicsPath path = GetRoundedPath(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(80, 96, 165, 250), size * 0.015f))
                {
                    g.DrawPath(pen, path);
                }

                float cx = size * 0.5f;
                float cy = size * 0.5f;

                // Surrounding Radio Waves / Signal Emission Arcs
                float waveR1 = size * 0.35f;
                float waveR2 = size * 0.40f;
                using (Pen wavePen = new Pen(Color.FromArgb(90, 56, 189, 248), size * 0.03f))
                {
                    wavePen.StartCap = LineCap.Round;
                    wavePen.EndCap = LineCap.Round;
                    // Left wave
                    g.DrawArc(wavePen, cx - waveR1, cy - waveR1, waveR1 * 2, waveR1 * 2, 140, 80);
                    // Right wave
                    g.DrawArc(wavePen, cx - waveR1, cy - waveR1, waveR1 * 2, waveR1 * 2, 320, 80);
                }
                using (Pen wavePen2 = new Pen(Color.FromArgb(50, 14, 165, 233), size * 0.025f))
                {
                    wavePen2.StartCap = LineCap.Round;
                    wavePen2.EndCap = LineCap.Round;
                    g.DrawArc(wavePen2, cx - waveR2, cy - waveR2, waveR2 * 2, waveR2 * 2, 150, 60);
                    g.DrawArc(wavePen2, cx - waveR2, cy - waveR2, waveR2 * 2, waveR2 * 2, 330, 60);
                }

                // Bluetooth Rune Emblem (Crisp White and Cyan Accent)
                float runeH = size * 0.55f;
                float runeW = size * 0.30f;
                float topY = cy - (runeH * 0.5f);
                float botY = cy + (runeH * 0.5f);

                PointF pSpineTop = new PointF(cx - (runeW * 0.15f), topY);
                PointF pSpineBot = new PointF(cx - (runeW * 0.15f), botY);
                PointF pMidSpine = new PointF(cx - (runeW * 0.15f), cy);

                PointF pTopRight = new PointF(cx + (runeW * 0.55f), cy - (runeH * 0.25f));
                PointF pBotRight = new PointF(cx + (runeW * 0.55f), cy + (runeH * 0.25f));

                PointF pTopLeft = new PointF(cx - (runeW * 0.50f), cy - (runeH * 0.22f));
                PointF pBotLeft = new PointF(cx - (runeW * 0.50f), cy + (runeH * 0.22f));

                float penWidth = size * 0.07f;
                using (Pen bthPen = new Pen(Color.White, penWidth))
                {
                    bthPen.StartCap = LineCap.Round;
                    bthPen.EndCap = LineCap.Round;
                    bthPen.LineJoin = LineJoin.Round;

                    // Central vertical spine
                    g.DrawLine(bthPen, pSpineTop, pSpineBot);

                    // Cross lines
                    // From bottom left through mid to top right
                    g.DrawLine(bthPen, pBotLeft, pTopRight);
                    // From top right down to mid spine
                    g.DrawLine(bthPen, pTopRight, pMidSpine);
                    // From mid spine down to bottom right
                    g.DrawLine(bthPen, pMidSpine, pBotRight);
                    // From bottom right up through mid to top left
                    g.DrawLine(bthPen, pBotRight, pTopLeft);
                }

                // Restoration Repair Sparkle / Gear Badge at lower right
                float badgeX = cx + (size * 0.25f);
                float badgeY = cy + (size * 0.24f);
                float badgeR = size * 0.14f;

                using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(16, 185, 129))) // Emerald Green
                using (Pen badgeBorder = new Pen(Color.White, size * 0.035f))
                {
                    g.FillEllipse(badgeBg, badgeX - badgeR, badgeY - badgeR, badgeR * 2, badgeR * 2);
                    g.DrawEllipse(badgeBorder, badgeX - badgeR, badgeY - badgeR, badgeR * 2, badgeR * 2);
                }

                // Checkmark in badge
                using (Pen checkPen = new Pen(Color.White, size * 0.035f))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    PointF c1 = new PointF(badgeX - (badgeR * 0.45f), badgeY);
                    PointF c2 = new PointF(badgeX - (badgeR * 0.1f), badgeY + (badgeR * 0.42f));
                    PointF c3 = new PointF(badgeX + (badgeR * 0.5f), badgeY - (badgeR * 0.35f));
                    g.DrawLines(checkPen, new PointF[] { c1, c2, c3 });
                }
            }
            return bmp;
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
