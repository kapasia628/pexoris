using System;
using System.Windows.Forms;

namespace Pexoris.Unlocker
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string targetPath = null;
            if (args != null && args.Length > 0)
            {
                targetPath = args[0].Trim('"', ' ');
            }

            Application.Run(new MainForm(targetPath));
        }
    }
}
