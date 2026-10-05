using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Pexoris.Assets
{
    class ExportPng
    {
        static void Main(string[] args)
        {
            Bitmap bmp256 = CreateIcon.RenderStunningPexorisIcon(256);
            bmp256.Save(@"c:\xampp\htdocs\tools\website\assets\icons\file-unlocker.png", ImageFormat.Png);
            bmp256.Save(@"c:\xampp\htdocs\tools\website\tools\file-unlocker\file-unlocker.png", ImageFormat.Png);
            bmp256.Save(@"c:\xampp\htdocs\tools\website\tools\file-unlocker\brand-logo.png", ImageFormat.Png);
            Console.WriteLine("Successfully exported 256x256 high-resolution Padlock PNGs to all web locations!");
        }
    }
}
