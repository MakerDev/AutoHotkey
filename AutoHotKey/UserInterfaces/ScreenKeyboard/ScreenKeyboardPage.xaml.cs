using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AutoHotKey.MacroControllers;
using AutoHotKey.MacroControllers.ScreenKeyboard;
using WindowsInput;
using WindowsInput.Native;

namespace AutoHotKey.UserInterfaces.ScreenKeyboard
{
    /// <summary>
    /// ScreenKeyboardPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ScreenKeyboardPage : Page
    {
        public event EventHandler<ScreenKey> OnKeyClickedWhileEditing;


        private MacroControllers.ScreenKeyboard.ScreenKeyboard mScreenKeybaord;
        private Dictionary<int, ScreenKey> mDictScreenKeys = new Dictionary<int, ScreenKey>();

        private MacroControllers.ScreenKeyboard.ScreenKeyboard mScreenKeyboardForUndo = null;

        public ScreenKeyboardPage(MacroControllers.ScreenKeyboard.ScreenKeyboard screenKeyboard)
        {
            InitializeComponent();

            mScreenKeybaord = screenKeyboard;

            this.Focusable = false;

            UpdateView();
        }

        public void UpdateView()
        {
            this.Width = mScreenKeybaord.WindowSize.Width;
            this.Height = mScreenKeybaord.WindowSize.Height;

            mDictScreenKeys = new Dictionary<int, ScreenKey>(mScreenKeybaord.GetDictScreenKeys());

            xGridMain.Children.Clear();

            foreach (ScreenKey key in mDictScreenKeys.Values)
            {
                ScreenButton button = new ScreenButton(key);

                if (key.IsTogle)
                {
                    button.OnScreenKeyClicked += OnTogleKeyClicked;
                }
                else
                {
                    button.OnScreenKeyClicked += OnGeneralKeyClicked;
                }

                xGridMain.Children.Add(button);
            }

        }

        public void ResizeScreen(int width, int height)
        {
            SaveCurrentStateInternal();

            mScreenKeybaord.WindowSize.Width = width;
            mScreenKeybaord.WindowSize.Height = height;

            UpdateView();
        }

        public Size GetScreenSize()
        {
            return mScreenKeybaord.WindowSize;
        }


        //TODO : 한 번 되돌리기 기능도 추가하기
        public void UndoEditing()
        {
            if (mScreenKeyboardForUndo == null)
            {
                MessageBox.Show("되돌릴 상태가 없습니다");
                return;
            }

            mScreenKeybaord = mScreenKeyboardForUndo;
            mScreenKeyboardForUndo = null;

            UpdateView();
        }

        public void TogleUpAllKeys()
        {
            foreach (ScreenKey screenKey in mDictScreenKeys.Values)
            {
                if (screenKey.IsOn)
                {
                    UpKeyInternal(screenKey.Action);

                    mDictScreenKeys[screenKey.tag].IsOn = false;
                }
            }
        }

        private void SaveCurrentStateInternal()
        {
            mScreenKeyboardForUndo = mScreenKeybaord;
        }

        private void OnGeneralKeyClicked(object sender, ScreenKeyboard.ScreenButtonEventArgs e)
        {
            Button button = e.Button;

            ScreenKey thisKey = mDictScreenKeys[int.Parse(button.Tag.ToString())];

            if (ScreenKeyboardManager.IsEditing)
            {
                OnKeyClickedWhileEditing(this, thisKey);

                return;
            }


            DownKeyInternal(thisKey.Action);
            UpKeyInternal(thisKey.Action);

        }

        private void OnTogleKeyClicked(object sender, ScreenButtonEventArgs e)
        {
            Button button = e.Button;

            ScreenKey thisKey = mDictScreenKeys[int.Parse(button.Tag.ToString())];

            if (ScreenKeyboardManager.IsEditing)
            {
                OnKeyClickedWhileEditing(this, thisKey);

                return;
            }

            if (thisKey.IsOn)
            {
                e.ScreenButton.SetButtonStyle(thisKey);
                thisKey.IsOn = false;

                UpKeyInternal(thisKey.Action);
            }
            else
            {
                e.ScreenButton.SetButtonStyle(thisKey, true);
                thisKey.IsOn = true;

                DownKeyInternal(thisKey.Action);
            }
        }

        private void DownKeyInternal(HotkeyInfo key)
        {
            InputSimulator inputSimulator = new InputSimulator();

            foreach (var keyOrMod in AnalizeHotkeyInfoInternal(key))
            {
                inputSimulator.Keyboard.KeyDown(keyOrMod);
            }
        }

        private void UpKeyInternal(HotkeyInfo key)
        {
            InputSimulator inputSimulator = new InputSimulator();

            foreach (var keyOrMod in AnalizeHotkeyInfoInternal(key))
            {
                inputSimulator.Keyboard.KeyUp(keyOrMod);
            }
        }

        private List<VirtualKeyCode> AnalizeHotkeyInfoInternal(HotkeyInfo key)
        {
            int todoKey = key.Key;
            int todoMod = key.Modifier;

            VirtualKeyCode todoKeyVirtualKey = (VirtualKeyCode)todoKey;

            List<VirtualKeyCode> keys = new List<VirtualKeyCode>();

            if ((todoMod & EModifiers.Ctrl) != 0) { keys.Add(VirtualKeyCode.CONTROL); }
            if ((todoMod & EModifiers.Shift) != 0) { keys.Add(VirtualKeyCode.SHIFT); }
            if ((todoMod & EModifiers.Alt) != 0) { keys.Add(VirtualKeyCode.MENU); }
            if ((todoMod & EModifiers.Win) != 0) { keys.Add(VirtualKeyCode.LWIN); }

            keys.Add(todoKeyVirtualKey);

            return keys;
        }
    }
}
