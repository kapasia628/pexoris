using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Pexoris.AIShield;

namespace RenderTool
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (MainForm form = new MainForm())
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(0, 0);
                form.Opacity = 0.01;
                form.Show();

                // Process initial render events
                for (int i = 0; i < 10; i++)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(30);
                }

                Bitmap bmp = new Bitmap(860, 640);
                form.DrawToBitmap(bmp, new Rectangle(0, 0, 860, 640));
                
                string outPath = @"c:\xampp\htdocs\tools\build\AIShield_Render.png";
                bmp.Save(outPath, ImageFormat.Png);
                Console.WriteLine("Render saved to: " + outPath);

                form.Close();
            }
        }
    }
}
