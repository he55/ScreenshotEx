using System;
using System.Threading;
using System.Windows.Forms;

namespace ScreenshotEx
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Mutex obj = new Mutex(true, "Global\\ScreenshotEx", out bool createdNew);
            if (createdNew)
            {
#if NET5_0_OR_GREATER
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
#endif
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                GC.KeepAlive(obj);
            }
        }
    }
}
