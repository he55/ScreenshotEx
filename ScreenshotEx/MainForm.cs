using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenshotEx
{
    public partial class MainForm : Form
    {
        #region PInvoke

        const int HC_ACTION = 0;
        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYUP = 0x0101;
        const int WM_SYSKEYUP = 0x0105;
        const int VK_SNAPSHOT = 0x2C;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hmod, int dwThreadId);

        [DllImport("User32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("User32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle([Optional] string lpModuleName);

        [DllImport("User32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo = default);

        #endregion

        const string PrefixName = "截屏";
        readonly HookProc _hookProc;
        readonly IntPtr _hhook;
        readonly Settings _settings = Settings.Load();
        readonly SoundPlayer _soundPlayer;
        readonly PreviewWindow _previewWindow;
        string _saveFilePath;
        int _nameIndex = 1;

        public MainForm()
        {
            InitializeComponent();
            this.Icon = ScreenshotEx.Properties.Resources.AppIcon;
            notifyIcon1.Icon = this.Icon;

            textBox1.Text = _settings.SavePath;
            comboBox1.SelectedIndex = _settings.SaveExtension;
            comboBox2.SelectedIndex = _settings.SaveName;
            comboBox3.SelectedIndex = _settings.OpenApp;
            checkBox1.Checked = _settings.IsShowPreview;
            checkBox2.Checked = _settings.IsPlaySound;
            checkBox3.Checked = Helper.CheckStartOnBoot();
            groupBox1.Enabled = !_settings.UseHotkey;
            radioButton1.Checked = !_settings.UseHotkey;
            radioButton2.Checked = _settings.UseHotkey;

            _hookProc = new HookProc(LowLevelKeyboardProc);
            _hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(null), 0);
            _soundPlayer = new SoundPlayer(Properties.Resources.Screenshot);
            _previewWindow = new PreviewWindow();
            _previewWindow.OpenImageAction = OpenImage;
            _previewWindow.Show();

            if (_settings.IsFirstRun)
            {
                notifyIcon1.ShowBalloonTip(1000, "", "ScreenshotEx 已被最小化到系统托盘。", ToolTipIcon.None);
                _settings.IsFirstRun = false;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!this.IsHandleCreated)
            {
                value = false;
                this.CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        #region 私有方法

        void SendHotkey()
        {
            const int KEYEVENTF_EXTENDEDKEY = 0x0001;
            const int KEYEVENTF_KEYUP = 0x0002;
            const int VK_LWIN = 0x5B;
            const int VK_SHIFT = 0x10;
            const int VK_S = 0x53;

            // Simulate a key press
            keybd_event(VK_LWIN,
                         0x45,
                         KEYEVENTF_EXTENDEDKEY | 0,
                         IntPtr.Zero);
            keybd_event(VK_SHIFT,
                         0x45,
                         KEYEVENTF_EXTENDEDKEY | 0,
                         IntPtr.Zero);
            keybd_event(VK_S,
                         0x45,
                         KEYEVENTF_EXTENDEDKEY | 0,
                         IntPtr.Zero);

            // Simulate a key release
            keybd_event(VK_LWIN,
                         0x45,
                         KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP,
                         IntPtr.Zero);
            keybd_event(VK_SHIFT,
                         0x45,
                         KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP,
                         IntPtr.Zero);
            keybd_event(VK_S,
                         0x45,
                         KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP,
                         IntPtr.Zero);
        }

        void OpenImage()
        {
            if (_settings.OpenApp == 0)
            {
                const string PaintPath = "C:\\Windows\\System32\\mspaint.exe";
                if (File.Exists(PaintPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = PaintPath,
                        Arguments = $"\"{_saveFilePath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    Helper.OpenLink(_saveFilePath);
                }
            }

            _previewWindow.SetHide();
        }

        void SaveImage()
        {
            if (Clipboard.ContainsImage())
            {
                if (!Directory.Exists(_settings.SavePath))
                    Directory.CreateDirectory(_settings.SavePath);

                string ext = "png";
                ImageFormat imageFormat = ImageFormat.Png;
                switch (_settings.SaveExtension)
                {
                    case 0:
                        imageFormat = ImageFormat.Png;
                        ext = "png";
                        break;
                    case 1:
                        imageFormat = ImageFormat.Jpeg;
                        ext = "jpg";
                        break;
                    case 2:
                        imageFormat = ImageFormat.Bmp;
                        ext = "bmp";
                        break;
                }

                if (_settings.SaveName == 0)
                {
                    string name = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
                    _saveFilePath = Path.Combine(_settings.SavePath, $"{PrefixName} {name}.{ext}");
                }
                else
                {
                    do
                    {
                        _saveFilePath = Path.Combine(_settings.SavePath, $"{PrefixName} {_nameIndex}.{ext}");
                        _nameIndex++;
                    } while (File.Exists(_saveFilePath));
                }

                Image image = Clipboard.GetImage();
                image.Save(_saveFilePath, imageFormat);

                if (_settings.IsPlaySound)
                    _soundPlayer.Play();

                if (_settings.IsShowPreview)
                    _previewWindow.SetImage(_saveFilePath);
            }
        }

        IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (nCode == HC_ACTION)
            {
                if (lParam.vkCode == VK_SNAPSHOT)
                {
                    if ((int)wParam == WM_KEYUP || (int)wParam == WM_SYSKEYUP)
                    {
                        if (!_settings.UseHotkey)
                            SaveImage();
                        else
                            SendHotkey();
                    }
                    else
                    {
                        if (!_settings.UseHotkey)
                            _previewWindow.SetHide();
                    }
                }
            }
            return CallNextHookEx(_hhook, nCode, wParam, ref lParam);
        }

        #endregion

        #region 控件事件

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                Settings.Save();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                _settings.SavePath = textBox1.Text = folderBrowserDialog.SelectedPath;
        }

        private void comboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (sender == comboBox1)
                _settings.SaveExtension = comboBox1.SelectedIndex;
            else if (sender == comboBox2)
                _settings.SaveName = comboBox2.SelectedIndex;
            else if (sender == comboBox3)
                _settings.OpenApp = comboBox3.SelectedIndex;
        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            _settings.IsShowPreview = checkBox1.Checked;
            if (!_settings.IsShowPreview)
                _previewWindow.SetHide();
        }

        private void checkBox2_Click(object sender, EventArgs e)
        {
            _settings.IsPlaySound = checkBox2.Checked;
        }

        private void checkBox3_Click(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
                Helper.SetStartOnBoot();
            else
                Helper.RemoveStartOnBoot();
        }

        private void radioButton_Click(object sender, EventArgs e)
        {
            groupBox1.Enabled = radioButton1.Checked;
            _settings.UseHotkey = !radioButton1.Checked;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Helper.OpenLink("https://github.com/he55/ScreenshotEx");
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            tabControl1.SelectedIndex = 0;
            this.Show();
            this.Activate();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
            this.Show();
            this.Activate();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            UnhookWindowsHookEx(_hhook);
            Settings.Save();
            Application.Exit();
        }

        #endregion
    }
}
