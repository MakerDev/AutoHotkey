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

namespace AutoHotKey
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Button> buttons = new List<Button>();
        private System.Windows.Forms.NotifyIcon ni;
        private Button addButton;
        private StackPanel list;

        private int currentProfile = 0; //만약 아무 프로필도 선택되지 않은 상태일때는 0


        private List<Window> mWindowsToClose = new List<Window>();

        private InformationWindow mInfoWindow = null;
        //private MacroSetting macroSettingWindow = null;
        private MacroSettingWithPicture mSettingWindow = null;
        

        //TODO : 현재 창에서 연 다른 창들은 현재 창이 꺼지면 함께 꺼지도록 한다.
        //TODO : 이미 프로그램이 실행중인 경우 두 번째 프로그램은 실행시키지 않도록 한다.
        public MainWindow()
        {
            InitializeComponent();

            list = new StackPanel();
            list.HorizontalAlignment = HorizontalAlignment.Left;
            list.VerticalAlignment = VerticalAlignment.Top;

            ClearViewInternal();

            scrViewerProfiles.Content = list;

            //TODO : loaded쓸 지 그냥 여기서 실행 시킬 지 결정
            this.Loaded += OnMainWindowLoaded;

            Closing += OnMainWindowClosing;

            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("Main2.ico");
            ni.Visible = true;
            ni.DoubleClick += Open;


            System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.Add("Open", new EventHandler(Open));
            contextMenu.MenuItems.Add("Exit", new EventHandler(Exit));

            ni.ContextMenu = contextMenu;

            mInfoWindow = new InformationWindow();
            mInfoWindow.ChangeCurrentProfile(-1);

            mWindowsToClose.Add(mInfoWindow);

            mInfoWindow.Show();
        }

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

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                this.Hide();

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

            foreach(var window in mWindowsToClose)
            {
                window.Close();
            }
        }

        //생성자가 실행되었을 때 발동
        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            HotKeyController.Instance.RegisterHelper(this);
            HotKeyController.Instance.ProfileChanged += OnActiveProfileChanged;


            this.WindowState = WindowState.Minimized;
            Hide();
        }

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
        private void ClearViewInternal()
        {
            currentProfile = 0;
            SetProfileView(currentProfile);

            list.Children.Clear();

            for (int i = 1; i <= HotKeyController.Instance.GetNumOfProfiles(); i++)
            {
                Button button1 = new Button();
                button1.Content = "Profile " + i.ToString();
                button1.Margin = new Thickness(0, 0, 0, 5);
                button1.Width = 330;
                button1.Height = 35;
                button1.Click += new RoutedEventHandler(OnProfileClicked);

                buttons.Add(button1);
                list.Children.Add(button1);
            }


            addButton = new Button();
            addButton.Content = "ADD Profile";
            addButton.Margin = new Thickness(0, 0, 0, 5);
            addButton.Width = 330;
            addButton.Height = 35;
            addButton.Click += OnAddProfileClicked;

            list.Children.Add(addButton);

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
            ClearViewInternal();
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


            //TODO : 추후에 메인 윈도우 먼저 못 끄게 하기
            mSettingWindow = new MacroSettingWithPicture(currentProfile);

            mWindowsToClose.Add(mSettingWindow);
            mSettingWindow.Show();
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
            ClearViewInternal();
        }


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

    }
}
