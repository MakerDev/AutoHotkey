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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AutoHotKey.MacroControllers;
using AutoHotKey.UserInterfaces;
using AutoHotKey.MacroControllers.ScreenKeyboard;
using System.IO;

namespace AutoHotKey
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon ni;

        private List<Button> mProfileButtons = new List<Button>();
        private Button mAddProfiileButton;
        private StackPanel mProfileList;

        private int currentProfile = 0; //만약 아무 프로필도 선택되지 않은 상태일때는 0

        private List<Button> mKeyboardButtons = new List<Button>();
        private Button mAddKeyboardButton;
        private StackPanel mKeyboardList;

        private int mCurrentKeyboardNum = 0;

        private List<Window> mWindowsToClose = new List<Window>();

        private InformationWindow mInfoWindow = null;
        private OptionWindow mOptionWindow = null;
        private bool mIsEditingOptions = false;
        //private MacroSetting macroSettingWindow = null;


        //TODO : 현재 창에서 연 다른 창들은 현재 창이 꺼지면 함께 꺼지도록 한다.
        //TODO : 이미 프로그램이 실행중인 경우 두 번째 프로그램은 실행시키지 않도록 한다.
        public MainWindow()
        {
            InitializeComponent();

            mProfileList = new StackPanel();
            mProfileList.HorizontalAlignment = HorizontalAlignment.Left;
            mProfileList.VerticalAlignment = VerticalAlignment.Top;

            ClearProfileViewInternal();

            scrViewerProfiles.Content = mProfileList;

            mKeyboardList = new StackPanel();
            mKeyboardList.HorizontalAlignment = HorizontalAlignment.Left;
            mKeyboardList.VerticalAlignment = VerticalAlignment.Top;

            ClearKeyboardViewInternal();

            scrViewerScreenKeyboards.Content = mKeyboardList;

            CheckSaveFolder();


            //TODO : loaded쓸 지 그냥 여기서 실행 시킬 지 결정
            this.Loaded += OnMainWindowLoaded;

            Closing += OnMainWindowClosing;

            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("Main2.ico");
            ni.Visible = true;
            ni.DoubleClick += Open;

            CreateMenuItems();

            //infoWindow의 옵션설정을 위해 먼저 생성되어야함
            mOptionWindow = new OptionWindow(this);
            mWindowsToClose.Add(mOptionWindow);

            mInfoWindow = new InformationWindow();
            mInfoWindow.ChangeCurrentProfile(-1);

            mWindowsToClose.Add(mInfoWindow);

            mInfoWindow.Show();

        }

        private void CheckSaveFolder()
        {
            string path = Environment.CurrentDirectory + "/SaveFiles";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        #region BasicSettings
        private void OnBtnHelpClicked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tips : If you can't type a key you want to use for output, You can use 'select key' button to visaully select the key instead of trying to type it in the textbox!");
        }

        private void OnExitBtnClicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Exit(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Open(object sender, EventArgs args)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void CreateMenuItems()
        {
            System.Windows.Forms.MenuItem open = new System.Windows.Forms.MenuItem();
            open.Text = "Settings";
            open.Click += Open;

            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem();
            exit.Name = "EXIT";
            exit.Text = "Exit";
            exit.Click += Exit;

            System.Windows.Forms.MenuItem closeAllScreenKeyboard = new System.Windows.Forms.MenuItem();
            closeAllScreenKeyboard.Text = "Close All WIN";
            closeAllScreenKeyboard.Click += new EventHandler(OnCloseAllClicked);

            System.Windows.Forms.MenuItem onoff_popup = new System.Windows.Forms.MenuItem();
            onoff_popup.Text = "On/Off Info WIndow";
            onoff_popup.Click += OnOffPopUp;

            System.Windows.Forms.MenuItem screenKeyboardMenu = new System.Windows.Forms.MenuItem();
            screenKeyboardMenu.Name = "FW";
            screenKeyboardMenu.Text = "FW";

            System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.Add(screenKeyboardMenu);
            contextMenu.MenuItems.Add(closeAllScreenKeyboard);
            contextMenu.MenuItems.Add(onoff_popup);
            contextMenu.MenuItems.Add(open);
            contextMenu.MenuItems.Add(exit);



            ni.ContextMenu = contextMenu;

            RefreshFloatingWindowMenu();
        }

        private void OnOffPopUp(object sender, EventArgs e)
        {
            if (mInfoWindow.Visibility == Visibility.Visible)
            {
                mInfoWindow.Visibility = Visibility.Hidden;
            }
            else
            {
                mInfoWindow.Visibility = Visibility.Visible;
            }
        }

        private void RefreshFloatingWindowMenu()
        {
            System.Windows.Forms.MenuItem screenKeyboardMenu = ni.ContextMenu.MenuItems.Find("FW", false)[0];
            screenKeyboardMenu.MenuItems.Clear();


            for (int i = 1; i <= ScreenKeyboardManager.GetNumberOfScreenKeyboards(); i++)
            {
                System.Windows.Forms.MenuItem window = new System.Windows.Forms.MenuItem();
                window.Name = i.ToString();
                window.Text = "WIN" + i.ToString();
                window.RadioCheck = true;
                window.Checked = false;
                window.Click += new EventHandler(OnWindowMenuClicked);

                screenKeyboardMenu.MenuItems.Add(window);
            }

        }

        private void OnCloseAllClicked(object sender, EventArgs args)
        {
            ScreenKeyboardManager.CloseAllWindows();

            System.Windows.Forms.MenuItem screenKeyboardMenu = ni.ContextMenu.MenuItems.Find("FW", false)[0];

            foreach (System.Windows.Forms.MenuItem window in screenKeyboardMenu.MenuItems)
            {
                if (window.RadioCheck == true)
                {
                    window.Checked = false;
                }
            }

        }
        private void OnWindowMenuClicked(object sender, EventArgs args)
        {
            System.Windows.Forms.MenuItem item = sender as System.Windows.Forms.MenuItem;

            if (!item.Checked)
            {
                if (ScreenKeyboardManager.IsEditing)
                {
                    MessageBox.Show("Finish editing before opening a window");
                    return;
                }

                item.Checked = true;
                ScreenKeyboardManager.OpenScreenKeyboard(int.Parse(item.Name));
            }
            else
            {
                item.Checked = false;
                ScreenKeyboardManager.CloseScreenKeyboard(int.Parse(item.Name));
            }

        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                this.Hide();
            else if (WindowState == WindowState.Normal)
                this.Activate();

            base.OnStateChanged(e);
        }

        private void OnActiveProfileChanged(object sender, EventArgs e)
        {
            ProfileChangedCallBackArgs info = e as ProfileChangedCallBackArgs;

            mInfoWindow.ChangeCurrentProfile(info.profile);
        }

        private void OnMainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ni.Visible = false;
            ni.Dispose();
            ni = null;

            foreach (var window in mWindowsToClose)
            {
                //후속 윈도우를 먼저 끌 수도 있으니 null체크를 반드시 해야함.
                if (window != null)
                {
                    window.Close();
                }
            }

            ScreenKeyboardManager.CloseAllWindows();
        }


        //생성자가 실행되었을 때 발동
        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            HotKeyController.Instance.RegisterHelper(this);
            HotKeyController.Instance.ProfileChanged += OnActiveProfileChanged;

            if (OptionWindow.Options.IsMinimizeMainWindowOnStart)
            {
                this.WindowState = WindowState.Minimized;
                Hide();
            }
        }
        #endregion


        #region ProfileMain

        // : 만약 프로필 순서 바꾸는 기능을 추가하려면 구현은 파일이름을 바꾸고 다시 로딩하는 걸로 구현하면 됨 
        //예를 들면 프로필2와 3의 순서를 바꾸려면 profile2의 이름을 profile3으로, profile3의 이름을 profile2로 바꾸고 다시 로딩하면 됨.
        private void OnProfileClicked(object sender, RoutedEventArgs e)
        {
            string name = (string)((Button)sender).Content;
            string num = name.Substring(7, name.Length - 7);

            currentProfile = int.Parse(num);

            SetProfileView(currentProfile);
        }

        private void SetProfileView(int numOfProfile)
        {
            if (numOfProfile == 0)
            {
                tbCurrentProfile.Content = "Please Select a Profile";
            }
            else
            {
                tbCurrentProfile.Content = "Profile " + numOfProfile.ToString();
            }

            sProfileContent.Visibility = Visibility.Visible;
        }

        //프로필이 추가, 삭제 되었을 때 호출
        private void ClearProfileViewInternal()
        {
            currentProfile = 0;
            SetProfileView(currentProfile);

            mProfileList.Children.Clear();

            for (int i = 1; i <= HotKeyController.Instance.GetNumOfProfiles(); i++)
            {
                Button button1 = new Button();
                button1.Content = "Profile " + i.ToString();
                button1.Margin = new Thickness(0, 0, 0, 5);
                button1.Width = 330;
                button1.Height = 35;
                button1.Click += new RoutedEventHandler(OnProfileClicked);

                mProfileButtons.Add(button1);
                mProfileList.Children.Add(button1);
            }


            mAddProfiileButton = new Button();
            mAddProfiileButton.Content = "ADD Profile";
            mAddProfiileButton.Margin = new Thickness(0, 0, 0, 5);
            mAddProfiileButton.Width = 330;
            mAddProfiileButton.Height = 35;
            mAddProfiileButton.Click += OnAddProfileClicked;

            mProfileList.Children.Add(mAddProfiileButton);

        }

        private void OnAddProfileClicked(object sender, RoutedEventArgs e)
        {
            if (HotKeyController.Instance.GetNumOfProfiles() == HotKeyController.MAX_NUM_OF_PROFILES)
            {
                MessageBox.Show("Can't Add more Profile");

                return;
            }

            if (HotKeyController.Instance.IsEdittingProfile())
            {
                MessageBox.Show("You're editing a profile. Please end editing before add a profile");
                return;
            }

            HotKeyController.Instance.AddNewProfile();
            ClearProfileViewInternal();
        }

        private void OnEditProfileCliked(object sender, RoutedEventArgs e)
        {
            if (currentProfile == 0)
            {
                MessageBox.Show("Please Select a Profile");
                return;
            }

            if (HotKeyController.Instance.IsEdittingProfile())
            {
                MessageBox.Show("There is a profile still on editing");

                return;
            }

            if (OptionWindow.Options.WhichSettingWindow == 0)
            {
                Window mSettingWindow = new MacroSettingWithPicture(currentProfile);

                mWindowsToClose.Add(mSettingWindow);
                mSettingWindow.Show();
            }
            else if (OptionWindow.Options.WhichSettingWindow == 1)
            {
                Window mSettingWindow = new MacroSetting(currentProfile);

                mWindowsToClose.Add(mSettingWindow);
                mSettingWindow.Show();
            }
        }

        private void OnDeleteProfileClicked(object sender, RoutedEventArgs e)
        {
            if (currentProfile == 0)
            {
                MessageBox.Show("Select a Profile");
                return;
            }

            if (MessageBox.Show("Sure to Delete this Profile?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            if (HotKeyController.Instance.IsEdittingProfile())
            {
                MessageBox.Show("You're Editing a profile. Please End editing before delete a profile");
                return;
            }

            HotKeyController.Instance.DeleteProfile(currentProfile);
            ClearProfileViewInternal();
        }

        #endregion

        #region ScreenKeyboardMain

        private void OnKeyboardClicked(object sender, RoutedEventArgs e)
        {
            string name = (string)((Button)sender).Content;
            string num = name.Substring(15, name.Length - 15);

            mCurrentKeyboardNum = int.Parse(num);

            SetKeyboardeView(mCurrentKeyboardNum);
        }

        private void SetKeyboardeView(int numOfKeyboard)
        {
            if (numOfKeyboard == 0)
            {
                tbCurrentScreenKeyboard.Content = "Select a Screen Keyboard";
            }
            else
            {
                tbCurrentScreenKeyboard.Content = "Keyboard " + numOfKeyboard.ToString();
            }

            sScreenKeyboardContent.Visibility = Visibility.Visible;
        }

        //프로필이 추가, 삭제 되었을 때 호출
        private void ClearKeyboardViewInternal()
        {
            mCurrentKeyboardNum = 0;
            SetKeyboardeView(mCurrentKeyboardNum);

            mKeyboardList.Children.Clear();

            for (int i = 1; i <= ScreenKeyboardManager.GetNumberOfScreenKeyboards(); i++)
            {
                Button button1 = new Button();
                button1.Content = "ScreenKeyboard " + i.ToString();
                button1.Margin = new Thickness(0, 0, 0, 5);
                button1.Width = 330;
                button1.Height = 35;
                button1.Click += new RoutedEventHandler(OnKeyboardClicked);

                mKeyboardButtons.Add(button1);
                mKeyboardList.Children.Add(button1);
            }


            mAddKeyboardButton = new Button();
            mAddKeyboardButton.Content = "ADD Keyboard";
            mAddKeyboardButton.Margin = new Thickness(0, 0, 0, 5);
            mAddKeyboardButton.Width = 330;
            mAddKeyboardButton.Height = 35;
            mAddKeyboardButton.Click += OnAddKeyboardClicked;

            mKeyboardList.Children.Add(mAddKeyboardButton);

        }

        private void OnAddKeyboardClicked(object sender, RoutedEventArgs e)
        {
            if (ScreenKeyboardManager.GetNumberOfScreenKeyboards() == ScreenKeyboardManager.MAX_NUM_OF_KEYBOARD)
            {
                MessageBox.Show("Can't Add more Keyboard");

                return;
            }

            if (ScreenKeyboardManager.IsEditing)
            {
                MessageBox.Show("You're editing a Keyboard. Please end editing before add a keyboard");
                return;
            }

            ScreenKeyboardManager.AddScreenKeyboard();
            ClearKeyboardViewInternal();
            RefreshFloatingWindowMenu();
        }

        private void OnEditKeyboardCliked(object sender, RoutedEventArgs e)
        {
            if (mCurrentKeyboardNum == 0)
            {
                MessageBox.Show("Please Select a Keyboard");
                return;
            }
            else if (ScreenKeyboardManager.IsEditing)
            {
                MessageBox.Show("There is a keyboard still on editing");

                return;
            }
            else if (ScreenKeyboardManager.NumOfKeayboardsOn > 0)
            {
                MessageBox.Show("There are keyboards still running!");
                return;
            }

            Window settingWindow = new ScreenKeyboardSettingWindow(mCurrentKeyboardNum);

            mWindowsToClose.Add(settingWindow);
            settingWindow.Show();
        }

        private void OnDeleteKeyboardClicked(object sender, RoutedEventArgs e)
        {
            if (mCurrentKeyboardNum == 0)
            {
                MessageBox.Show("Select a Profile");
                return;
            }

            if (MessageBox.Show("Sure to Delete this Keyboard?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            if (ScreenKeyboardManager.IsEditing)
            {
                MessageBox.Show("You're Editing a profile. Please End editing before delete a profile");
                return;
            }

            ScreenKeyboardManager.DeleteScreenKeyboard(mCurrentKeyboardNum);

            ClearKeyboardViewInternal();
            RefreshFloatingWindowMenu();
        }

        #endregion

        private void OnBtnSetProfileChangeKeyClicked(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;

            int param = Convert.ToInt32(item.Tag.ToString());

            if (HotKeyController.Instance.IsSettingProfileChangingKeys())
            {
                MessageBox.Show("You already Opened a Setting Window");
                return;
            }

            //TODO : 이미 편집 창이 열려 있으면 버튼 작동시키지 않기 + 핫 키 멈추기(프로필 키 바꾸는 걸 컨트롤러에 알리기)
            ProfileChangeKeysTable profileChangeKeysTable = new ProfileChangeKeysTable(param);

            mWindowsToClose.Add(profileChangeKeysTable);
            profileChangeKeysTable.Show();
        }

        private void OnBtnSettingOptionsClicked(object sender, RoutedEventArgs e)
        {
            if (!mIsEditingOptions)
            {
                mIsEditingOptions = true;
                mOptionWindow = new OptionWindow(this);
                mOptionWindow.Show();
            }
            else
            {
                MessageBox.Show("You're already editing options!");
            }
        }

        public void OnOptionWindowCloseing()
        {
            mIsEditingOptions = false;
        }
    }
}
