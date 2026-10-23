using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace PexorisClipboardHistory.Assets
{
    class CreateIcon
    {
        static void Main()
        {
            int[] sizes = new int[] { 256, 48, 32, 16 };
            Bitmap[] bitmaps = new Bitmap[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                bitmaps[i] = RenderClipboardBitmap(sizes[i]);
            }

            // Save multi-resolution .ico
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            SaveMultiResolutionIcon(bitmaps, icoPath);
            Console.WriteLine("Saved: " + icoPath);

            // Save 512x512 PNGs for website
            using (Bitmap big = RenderClipboardBitmap(512))
            {
                string png1 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\website\tools\clipboard-history\clipboard-history.png"));
                string png2 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\website\assets\icons\clipboard-history.png"));
                
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

        static Bitmap RenderClipboardBitmap(int size)
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

                // Outer Squircle: Cobalt Sapphire Gradient (#2563EB to #1E293B)
                RectangleF rect = new RectangleF(pad, pad, w, h);
                using (GraphicsPath outerPath = new GraphicsPath())
                {
                    float r = w * 0.26f;
                    outerPath.AddRoundedRectangle(rect, r);

                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(rect, Color.FromArgb(37, 99, 235), Color.FromArgb(15, 23, 42), 65f))
                    {
                        g.FillPath(bgBrush, outerPath);
                    }

                    // Glossy border
                    using (Pen borderPen = new Pen(Color.FromArgb(140, 255, 255, 255), s * 0.018f))
                    {
                        g.DrawPath(borderPen, outerPath);
                    }
                }

                // Inner Dark Vault Card
                float cardMargin = s * 0.12f;
                float cardW = w - (cardMargin * 2f);
                float cardH = h - (cardMargin * 2f);
                float cardX = rect.X + cardMargin;
                float cardY = rect.Y + cardMargin;
                RectangleF innerRect = new RectangleF(cardX, cardY, cardW, cardH);

                using (GraphicsPath innerPath = new GraphicsPath())
                {
                    innerPath.AddRoundedRectangle(innerRect, cardW * 0.18f);
                    using (SolidBrush innerBrush = new SolidBrush(Color.FromArgb(30, 41, 59))) // Slate #1E293B
                    {
                        g.FillPath(innerBrush, innerPath);
                    }
                    using (Pen innerPen = new Pen(Color.FromArgb(96, 165, 250), s * 0.02f))
                    {
                        g.DrawPath(innerPen, innerPath);
                    }
                }

                // Clipboard Backboard (Warm Gray / Cream #F8FAFC)
                float boardW = cardW * 0.68f;
                float boardH = cardH * 0.74f;
                float boardX = cardX + (cardW - boardW) * 0.5f;
                float boardY = cardY + cardH * 0.18f;
                RectangleF boardRect = new RectangleF(boardX, boardY, boardW, boardH);

                using (GraphicsPath boardPath = new GraphicsPath())
                {
                    boardPath.AddRoundedRectangle(boardRect, boardW * 0.12f);
                    using (LinearGradientBrush boardBrush = new LinearGradientBrush(boardRect, Color.FromArgb(248, 250, 252), Color.FromArgb(226, 232, 240), 90f))
                    {
                        g.FillPath(boardBrush, boardPath);
                    }
                    using (Pen boardPen = new Pen(Color.FromArgb(203, 213, 225), s * 0.015f))
                    {
                        g.DrawPath(boardPen, boardPath);
                    }
                }

                // Clipboard Paper Lines (Snippet previews)
                float linePadX = boardW * 0.16f;
                float lineStartX = boardX + linePadX;
                float lineEndX = boardX + boardW - linePadX;
                float lineTopY = boardY + boardH * 0.28f;
                float lineGap = boardH * 0.14f;

                using (Pen linePen = new Pen(Color.FromArgb(148, 163, 184), s * 0.022f))
                {
                    linePen.StartCap = LineCap.Round;
                    linePen.EndCap = LineCap.Round;
                    g.DrawLine(linePen, lineStartX, lineTopY, lineEndX, lineTopY);
                    g.DrawLine(linePen, lineStartX, lineTopY + lineGap, lineEndX * 0.92f, lineTopY + lineGap);
                    g.DrawLine(linePen, lineStartX, lineTopY + lineGap * 2f, lineEndX * 0.75f, lineTopY + lineGap * 2f);
                }

                // Metallic Chrome Top Clip
                float clipW = boardW * 0.44f;
                float clipH = boardH * 0.18f;
                float clipX = boardX + (boardW - clipW) * 0.5f;
                float clipY = boardY - clipH * 0.45f;
                RectangleF clipRect = new RectangleF(clipX, clipY, clipW, clipH);

                using (GraphicsPath clipPath = new GraphicsPath())
                {
                    clipPath.AddRoundedRectangle(clipRect, clipW * 0.2f);
                    using (LinearGradientBrush clipBrush = new LinearGradientBrush(clipRect, Color.FromArgb(203, 213, 225), Color.FromArgb(100, 116, 139), 90f))
                    {
                        g.FillPath(clipBrush, clipPath);
                    }
                    using (Pen clipPen = new Pen(Color.White, s * 0.015f))
                    {
                        g.DrawPath(clipPen, clipPath);
                    }
                }

                // Clip mounting rivet/hole
                float rivetR = clipW * 0.12f;
                float rivetX = clipX + (clipW - rivetR * 2f) * 0.5f;
                float rivetY = clipY + (clipH - rivetR * 2f) * 0.5f;
                using (SolidBrush rivetBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
                {
                    g.FillEllipse(rivetBrush, rivetX, rivetY, rivetR * 2f, rivetR * 2f);
                }

                // Glowing Security Shield / Vault Badge (Bottom-Right of the clipboard)
                float shieldW = cardW * 0.38f;
                float shieldH = cardH * 0.42f;
                float shieldX = cardX + cardW - shieldW * 1.05f;
                float shieldY = cardY + cardH - shieldH * 1.02f;

                using (GraphicsPath shieldPath = new GraphicsPath())
                {
                    float cx = shieldX + shieldW * 0.5f;
                    shieldPath.AddLine(shieldX, shieldY, shieldX + shieldW, shieldY);
                    shieldPath.AddLine(shieldX + shieldW, shieldY, shieldX + shieldW, shieldY + shieldH * 0.55f);
                    shieldPath.AddBezier(
                        shieldX + shieldW, shieldY + shieldH * 0.55f,
                        shieldX + shieldW * 0.85f, shieldY + shieldH * 0.85f,
                        cx, shieldY + shieldH,
                        cx, shieldY + shieldH
                    );
                    shieldPath.AddBezier(
                        cx, shieldY + shieldH,
                        shieldX + shieldW * 0.15f, shieldY + shieldH * 0.85f,
                        shieldX, shieldY + shieldH * 0.55f,
                        shieldX, shieldY + shieldH * 0.55f
                    );
                    shieldPath.CloseFigure();

                    // Glow drop shadow
                    using (PathGradientBrush pgb = new PathGradientBrush(shieldPath))
                    {
                        pgb.CenterColor = Color.FromArgb(160, 16, 185, 129); // Emerald Glow
                        pgb.SurroundColors = new Color[] { Color.Transparent };
                    }

                    using (LinearGradientBrush sBrush = new LinearGradientBrush(new RectangleF(shieldX, shieldY, shieldW, shieldH), Color.FromArgb(16, 185, 129), Color.FromArgb(5, 150, 105), 60f))
                    {
                        g.FillPath(sBrush, shieldPath);
                    }
                    using (Pen sPen = new Pen(Color.FromArgb(209, 250, 229), s * 0.02f))
                    {
                        g.DrawPath(sPen, shieldPath);
                    }

                    // Checkmark inside shield
                    float chkX = shieldX + shieldW * 0.28f;
                    float chkY = shieldY + shieldH * 0.45f;
                    PointF[] chk = new PointF[] {
                        new PointF(chkX, chkY),
                        new PointF(chkX + shieldW * 0.18f, chkY + shieldH * 0.18f),
                        new PointF(chkX + shieldW * 0.52f, chkY - shieldH * 0.16f)
                    };
                    using (Pen chkPen = new Pen(Color.White, s * 0.035f))
                    {
                        chkPen.StartCap = LineCap.Round;
                        chkPen.EndCap = LineCap.Round;
                        chkPen.LineJoin = LineJoin.Round;
                        g.DrawLines(chkPen, chk);
                    }
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
