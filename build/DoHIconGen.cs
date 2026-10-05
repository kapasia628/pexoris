using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace IconGenerator
{
    class Program
    {
        static void Main()
        {
            int size = 256;
            using (Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                // Rounded Squircle Background (Apple Style #0066FF to #00C6FF Gradient)
                Rectangle rect = new Rectangle(12, 12, 232, 232);
                using (GraphicsPath path = GetRoundedRect(rect, 54f))
                {
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        new Point(0, 12), new Point(0, 244),
                        Color.FromArgb(255, 0, 102, 255),
                        Color.FromArgb(255, 0, 198, 255)))
                    {
                        g.FillPath(brush, path);
                    }

                    // Inner border highlight
                    using (Pen pen = new Pen(Color.FromArgb(120, 255, 255, 255), 3f))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                // Draw Shield / Lock / Globe Motif (Symbolizing Encrypted DoH DNS)
                // Draw Stylized Network Globe Lines
                using (Pen globePen = new Pen(Color.FromArgb(140, 255, 255, 255), 5f))
                {
                    globePen.DashStyle = DashStyle.Solid;
                    // Equator & meridian
                    g.DrawEllipse(globePen, 64, 64, 128, 128);
                    g.DrawEllipse(globePen, 96, 64, 64, 128);
                    g.DrawLine(globePen, 64, 128, 192, 128);
                }

                // Draw Central Golden Shield Lock (Encrypted DNS)
                GraphicsPath shield = new GraphicsPath();
                shield.AddLine(128, 88, 166, 104);
                shield.AddLine(166, 104, 166, 144);
                shield.AddBezier(166, 144, 166, 180, 128, 196, 128, 196);
                shield.AddBezier(128, 196, 90, 180, 90, 144, 90, 144);
                shield.AddLine(90, 144, 90, 104);
                shield.CloseFigure();

                using (LinearGradientBrush sBrush = new LinearGradientBrush(
                    new Point(90, 88), new Point(166, 196),
                    Color.FromArgb(255, 255, 255, 255),
                    Color.FromArgb(240, 240, 255, 255)))
                {
                    g.FillPath(sBrush, shield);
                }

                using (Pen sPen = new Pen(Color.FromArgb(255, 0, 80, 200), 4f))
                {
                    g.DrawPath(sPen, shield);
                }

                // Lock Shackle on Shield
                using (Pen lockPen = new Pen(Color.FromArgb(255, 0, 102, 255), 5f))
                {
                    g.DrawArc(lockPen, 116, 118, 24, 24, 180, 180);
                }
                // Keyhole
                using (SolidBrush kb = new SolidBrush(Color.FromArgb(255, 0, 102, 255)))
                {
                    g.FillEllipse(kb, 124, 134, 8, 8);
                    g.FillPolygon(kb, new Point[] {
                        new Point(126, 138),
                        new Point(130, 138),
                        new Point(131, 150),
                        new Point(125, 150)
                    });
                }

                // Lightning / Fast indicator
                using (SolidBrush spark = new SolidBrush(Color.FromArgb(255, 255, 215, 0)))
                {
                    Point[] bolt = new Point[] {
                        new Point(184, 52),
                        new Point(168, 82),
                        new Point(180, 82),
                        new Point(164, 116),
                        new Point(194, 76),
                        new Point(182, 76)
                    };
                    g.FillPolygon(spark, bolt);
                }

                // Save PNG files
                string pngPath1 = @"website\tools\doh-switcher\doh-switcher.png";
                string pngPath2 = @"website\assets\icons\doh-switcher.png";
                bmp.Save(pngPath1, ImageFormat.Png);
                bmp.Save(pngPath2, ImageFormat.Png);

                // Export to ICO
                ExportIco(bmp, @"src\PexorisDoHSwitcher\Assets\app.ico");
                ExportIco(bmp, @"website\tools\doh-switcher\favicon.ico");
            }

            Console.WriteLine("DOH_ICON_SUCCESS");
        }

        static GraphicsPath GetRoundedRect(Rectangle bounds, float radius)
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

        static void ExportIco(Bitmap bmp, string icoPath)
        {
            using (FileStream fs = new FileStream(icoPath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR
                bw.Write((short)0);
                bw.Write((short)1);
                bw.Write((short)1);

                // ICONDIRENTRY (48x48)
                using (Bitmap small = new Bitmap(bmp, new Size(48, 48)))
                using (MemoryStream ms = new MemoryStream())
                {
                    small.Save(ms, ImageFormat.Png);
                    byte[] pngBytes = ms.ToArray();

                    bw.Write((byte)48);
                    bw.Write((byte)48);
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write((short)1);
                    bw.Write((short)32);
                    bw.Write((int)pngBytes.Length);
                    bw.Write((int)22);

                    bw.Write(pngBytes);
                }
            }
        }
    }
}
