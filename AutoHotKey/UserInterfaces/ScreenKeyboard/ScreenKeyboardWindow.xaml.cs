using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using AutoHotKey.MacroControllers.ScreenKeyboard;

namespace AutoHotKey.UserInterfaces.ScreenKeyboard
{
    public partial class ScreenKeyboardWindow : Window
    {
        //Focus를 뺐지 않기 위한 코드
        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLong(IntPtr hWnd,  int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private MacroControllers.ScreenKeyboard.ScreenKeyboard mScreenKeyboard;
        private ScreenKeyboardPage mScreenKeyboardPage;
        private int mKeyboardNum = -1;

        public ScreenKeyboardWindow(int keyboardNum)
        {
            InitializeComponent();

            mScreenKeyboard = ScreenKeyboardManager.GetScreenKeyboard(keyboardNum);

            Closing += OnScreenKeyboardWindow_Closing;

            mScreenKeyboardPage = new ScreenKeyboardPage(mScreenKeyboard);
            xMainFrame.Content =mScreenKeyboardPage;

            Width = mScreenKeyboard.GetScreenSize().Width;
            Height = mScreenKeyboard.GetScreenSize().Height;

            Left = mScreenKeyboard.StartingLocation.X;
            Top = mScreenKeyboard.StartingLocation.Y;

            mKeyboardNum = keyboardNum;

            this.ShowInTaskbar = false;
        }

        private void OnScreenKeyboardWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mScreenKeyboardPage.TogleUpAllKeys();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            this.DragMove();

            mScreenKeyboard.SetCurrentLocation(Left, Top);

            //로케이션을 저장하기 위해
            ScreenKeyboardManager.SaveKeyboardInternal(mKeyboardNum);


        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            //Set the window style to noactivate.
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SetWindowLong(helper.Handle, GWL_EXSTYLE,
                GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        }

    }
}
