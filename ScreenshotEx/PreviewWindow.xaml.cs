using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ScreenshotEx
{
    public partial class PreviewWindow : Window
    {
        #region PInvoke

        const int GWL_EXSTYLE = -20;
        const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("User32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("User32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion

        const int DelaySeconds = 5;
        bool _isShow;
        int _delaySeconds;
        public Action OpenImageAction;

        public PreviewWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }

        public void SetImage(string path)
        {
            GC.Collect();

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.UriSource = new Uri(path);
            int height = (int)((FrameworkElement)this.Content).ActualHeight;
            bitmapImage.DecodePixelHeight = height;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            image1.Source = bitmapImage;

            _delaySeconds = DelaySeconds;
            if (!_isShow)
            {
                this.Opacity = 1;
                Rect workArea = SystemParameters.WorkArea;
                this.Left = workArea.Width - this.Width;
                this.Top = workArea.Height - this.Height;

                _isShow = true;
                DelayHide();
            }
        }

        public void SetHide()
        {
            if (_isShow)
            {
                this.Opacity = 0;
                this.Left = -10000;
                this.Top = -10000;
                _isShow = false;
            }
        }

        async void DelayHide()
        {
            while (_delaySeconds > 0)
            {
                if (!_isShow)
                    return;

                _delaySeconds--;
                await Task.Delay(1000);
            }

            SetHide();
        }

        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenImageAction?.Invoke();
        }
    }
}
