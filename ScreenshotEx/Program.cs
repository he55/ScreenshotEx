using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ScreenshotEx
{
    internal static class Program
    {
        [DllImport("User32.dll", SetLastError = false, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProcessDPIAware();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Mutex obj = new Mutex(true, "Global\\ScreenshotEx", out bool createdNew);
            if (createdNew)
            {
#if NETCOREAPP3_0_OR_GREATER
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
#else
                SetProcessDPIAware();
#endif
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                GC.KeepAlive(obj);
            }
        }
    }
}
