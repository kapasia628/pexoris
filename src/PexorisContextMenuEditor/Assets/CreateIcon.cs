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
            string outIco = @"c:\xampp\htdocs\tools\src\PexorisContextMenuEditor\Assets\app.ico";
            string outPng = @"c:\xampp\htdocs\tools\website\tools\context-menu-editor\context-menu-editor.png";
            string outAssetsPng = @"c:\xampp\htdocs\tools\website\assets\icons\context-menu-editor.png";

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

                // Deep Azure to Cobalt Blue Gradient Squircle
                float pad = size * 0.05f;
                float w = size - (pad * 2);
                float r = size * 0.22f;

                using (GraphicsPath path = GetRoundedRect(new RectangleF(pad, pad, w, w), r))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new PointF(pad, pad),
                    new PointF(pad + w, pad + w),
                    Color.FromArgb(37, 99, 235),   // #2563EB Blue 600
                    Color.FromArgb(30, 58, 138)))  // #1E3A8A Blue 900
                {
                    g.FillPath(brush, path);
                }

                // Inner subtle glow border
                using (GraphicsPath path = GetRoundedRect(new RectangleF(pad + 1, pad + 1, w - 2, w - 2), r - 1))
                using (Pen pen = new Pen(Color.FromArgb(80, 255, 255, 255), size * 0.015f))
                {
                    g.DrawPath(pen, path);
                }

                // Visual Artwork: Stylized Right-Click Context Menu Card with Cursor Pointer
                // Menu Card background
                float menuX = size * 0.22f;
                float menuY = size * 0.18f;
                float menuW = size * 0.56f;
                float menuH = size * 0.64f;
                float menuR = size * 0.08f;

                // Menu Drop Shadow
                using (GraphicsPath shadowPath = GetRoundedRect(new RectangleF(menuX, menuY + size * 0.03f, menuW, menuH), menuR))
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Menu Window Body
                using (GraphicsPath menuPath = GetRoundedRect(new RectangleF(menuX, menuY, menuW, menuH), menuR))
                using (SolidBrush menuBg = new SolidBrush(Color.FromArgb(248, 250, 252))) // #F8FAFC
                using (Pen menuBorder = new Pen(Color.FromArgb(203, 213, 225), size * 0.015f))
                {
                    g.FillPath(menuBg, menuPath);
                    g.DrawPath(menuBorder, menuPath);
                }

                // Context Menu Row 1 (Header/Title bar line)
                float rowPadX = menuX + size * 0.06f;
                float rowW = menuW - size * 0.12f;

                // Menu items (horizontal bars representing menu items)
                // Item 1 (Highlighted / Active Item)
                float item1Y = menuY + size * 0.08f;
                float itemH = size * 0.085f;
                using (GraphicsPath item1Path = GetRoundedRect(new RectangleF(menuX + size * 0.03f, item1Y, menuW - size * 0.06f, itemH), size * 0.03f))
                using (SolidBrush activeBrush = new SolidBrush(Color.FromArgb(37, 99, 235))) // Active Blue #2563EB
                {
                    g.FillPath(activeBrush, item1Path);
                }
                // Text line inside active item
                using (Pen penWhite = new Pen(Color.White, size * 0.025f))
                {
                    penWhite.StartCap = LineCap.Round;
                    penWhite.EndCap = LineCap.Round;
                    g.DrawLine(penWhite, rowPadX + size * 0.02f, item1Y + itemH * 0.5f, rowPadX + rowW * 0.65f, item1Y + itemH * 0.5f);
                }

                // Item 2 (Standard item)
                float item2Y = item1Y + size * 0.11f;
                using (Pen penGray1 = new Pen(Color.FromArgb(148, 163, 184), size * 0.022f))
                {
                    penGray1.StartCap = LineCap.Round;
                    penGray1.EndCap = LineCap.Round;
                    g.DrawLine(penGray1, rowPadX + size * 0.02f, item2Y + itemH * 0.5f, rowPadX + rowW * 0.55f, item2Y + itemH * 0.5f);
                }

                // Divider line
                float divY = item2Y + size * 0.10f;
                using (Pen penDiv = new Pen(Color.FromArgb(226, 232, 240), size * 0.01f))
                {
                    g.DrawLine(penDiv, menuX + size * 0.04f, divY, menuX + menuW - size * 0.04f, divY);
                }

                // Item 3 (Standard item)
                float item3Y = divY + size * 0.05f;
                using (Pen penGray2 = new Pen(Color.FromArgb(148, 163, 184), size * 0.022f))
                {
                    penGray2.StartCap = LineCap.Round;
                    penGray2.EndCap = LineCap.Round;
                    g.DrawLine(penGray2, rowPadX + size * 0.02f, item3Y + itemH * 0.5f, rowPadX + rowW * 0.75f, item3Y + itemH * 0.5f);
                }

                // Item 4 (Standard item with small checkmark or badge)
                float item4Y = item3Y + size * 0.10f;
                using (Pen penGray3 = new Pen(Color.FromArgb(148, 163, 184), size * 0.022f))
                {
                    penGray3.StartCap = LineCap.Round;
                    penGray3.EndCap = LineCap.Round;
                    g.DrawLine(penGray3, rowPadX + size * 0.02f, item4Y + itemH * 0.5f, rowPadX + rowW * 0.45f, item4Y + itemH * 0.5f);
                }

                // Lightning / Quick Bolt badge on bottom right
                float boltX = size * 0.65f;
                float boltY = size * 0.60f;
                float boltS = size * 0.28f;

                // Gold Badge circle
                using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(245, 158, 11))) // Amber Gold #F59E0B
                using (Pen badgeBorder = new Pen(Color.White, size * 0.02f))
                {
                    g.FillEllipse(badgeBg, boltX, boltY, boltS, boltS);
                    g.DrawEllipse(badgeBorder, boltX, boltY, boltS, boltS);
                }

                // Sharp Lightning Bolt inside circle
                using (GraphicsPath boltPath = new GraphicsPath())
                {
                    float bx = boltX + boltS * 0.5f;
                    float by = boltY + boltS * 0.2f;
                    boltPath.AddPolygon(new PointF[] {
                        new PointF(bx + boltS * 0.04f, by),
                        new PointF(bx - boltS * 0.22f, by + boltS * 0.35f),
                        new PointF(bx - boltS * 0.02f, by + boltS * 0.35f),
                        new PointF(bx - boltS * 0.10f, by + boltS * 0.65f),
                        new PointF(bx + boltS * 0.22f, by + boltS * 0.26f),
                        new PointF(bx + boltS * 0.02f, by + boltS * 0.26f),
                    });
                    using (SolidBrush boltBrush = new SolidBrush(Color.White))
                    {
                        g.FillPath(boltBrush, boltPath);
                    }
                }
            }
            return bmp;
        }

        static GraphicsPath GetRoundedRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        static void WriteIco(Bitmap[] images, Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((ushort)0); // Reserved
            writer.Write((ushort)1); // Type (1 = ICO)
            writer.Write((ushort)images.Length); // Count

            int offset = 6 + (images.Length * 16);
            byte[][] pngBytes = new byte[images.Length][];

            for (int i = 0; i < images.Length; i++)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    images[i].Save(ms, ImageFormat.Png);
                    pngBytes[i] = ms.ToArray();
                }

                int w = images[i].Width >= 256 ? 0 : images[i].Width;
                int h = images[i].Height >= 256 ? 0 : images[i].Height;

                writer.Write((byte)w);
                writer.Write((byte)h);
                writer.Write((byte)0); // Color palette
                writer.Write((byte)0); // Reserved
                writer.Write((ushort)1); // Color planes
                writer.Write((ushort)32); // Bits per pixel
                writer.Write((uint)pngBytes[i].Length); // Size of image data
                writer.Write((uint)offset); // Offset to image data

                offset += pngBytes[i].Length;
            }

            for (int i = 0; i < images.Length; i++)
            {
                writer.Write(pngBytes[i]);
            }
        }
    }
}
