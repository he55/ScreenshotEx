using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenshotEx
{
    public partial class PreviewWindow : Window
    {
        bool _isShow;
        int _delaySeconds;
        public Action OpenImageAction;

        public PreviewWindow()
        {
            InitializeComponent();
        }

        public void SetImage(string path)
        {
            GC.Collect();

            int height = (int)((FrameworkElement)this.Content).ActualHeight;

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.UriSource = new Uri(path);
            bitmapImage.DecodePixelHeight = height;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            image1.Source = bitmapImage;

            _delaySeconds = 5;
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
