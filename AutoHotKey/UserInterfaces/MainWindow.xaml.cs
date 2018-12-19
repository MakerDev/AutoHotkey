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

        private InformationWindow mInfoWindow = null;
         

        public MainWindow()
        {
            InitializeComponent();
            
            list = new StackPanel();
            list.HorizontalAlignment = HorizontalAlignment.Left;
            list.VerticalAlignment = VerticalAlignment.Top;

            ClearViewInternal();
            
            scrViewerProfiles.Content = list;

            this.Loaded += OnMainWindowLoaded;
            Closing += OnMainWindowClosing;


            mInfoWindow = new InformationWindow();
            mInfoWindow.ChangeCurrentProfile(0);

            mInfoWindow.Show();

            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("Main.ico");
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
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
            mInfoWindow.Close();
        }

        //생성자가 실행되었을 때 발동
        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            HotKeyController.Instance.RegisterHelper(this);
            HotKeyController.Instance.ProfileChanged += OnActiveProfileChanged;

        }

        // : 만약 프로필 순서 바꾸는 기능을 추가하려면 구현은 파일이름을 바꾸고 다시 로딩하는 걸로 구현하면 됨 
        //예를 들면 프로필2와 3의 순서를 바꾸려면 profile2의 이름을 profile3으로, profile3의 이름을 profile2로 바꾸고 다시 로딩하면 됨.


        private void OnProfileClicked(object sender, RoutedEventArgs e)
        {
            string name = (string)((Button)sender).Content;
            string num = name[name.Length - 1].ToString();

            currentProfile = int.Parse(num);

            SetProfileView(currentProfile);
        }

        private void SetProfileView(int numOfProfile)
        {
            if(numOfProfile==0)
            {
                tbCurrentProfile.Text = "Profile을 선택하십시오";
            }
            else
            {
                tbCurrentProfile.Text = "Profile " + numOfProfile.ToString();
            }

            sProfileContent.Visibility = Visibility.Visible;
        }

        //프로필이 추가, 삭제 되었을 때 호출
        private void ClearViewInternal()
        {
            currentProfile = 0;
            SetProfileView(currentProfile);

            list.Children.Clear();

            for (int i = 1; i <= HotKeyController.Instance.GetNumOfProfiles() ; i++)
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
            HotKeyController.Instance.AddNewProfile();
            ClearViewInternal();
        }

        private void OnEditProfileCliked(object sender, RoutedEventArgs e)
        {         
            if(currentProfile==0)
            {
                MessageBox.Show("프로필을 선택하십시오");
                return;
            }

            if(HotKeyController.Instance.IsEdittingProfile())
            {
                MessageBox.Show("이미 편집중인 프로필이 있습니다");
                return;
            }

            UserInterfaces.MacroSetting macroSettingWindow = new UserInterfaces.MacroSetting(currentProfile);
            //:추후에 visibility 옵션 히든으로 조정
            macroSettingWindow.Show();

        }

        private void OnDeleteProfileClicked(object sender, RoutedEventArgs e)
        {
            if (currentProfile == 0)
            {
                MessageBox.Show("프로필을 선택하십시오");
                return;
            }

            if (MessageBox.Show("정말 삭제하시겠습니까?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            HotKeyController.Instance.DeleteProfile(currentProfile);
            ClearViewInternal();
        }


        private void OnKeyInput(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ((TextBox)sender).Text = e.Key.ToString();

            //tbKeySet.Text = e.Key.ToString();
            if (ReferenceEquals(sender, tbKeySet))
            {
                //currentKeyIn = int.Parse(KeyInterop.VirtualKeyFromKey(e.Key).ToString());
            }
        }

        //TODO : 프로필 키 변경 내용 구현
        private void OnCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            const int VK_SPACE = 0x20;
            const int VK_DELETE = 0x2E;

            if (ReferenceEquals(sender, cbNoMod) && cbNoMod.IsChecked.Value)
            {
                cbAlt.IsChecked = false;
                cbControl.IsChecked = false;
                cbWindow.IsChecked = false;
                cbShift.IsChecked = false;
            }
            else
            {
                var checkBox = sender as CheckBox;
                if (checkBox != null && (!ReferenceEquals(sender, xCbDelete) && !ReferenceEquals(sender, xCbSpace)) && checkBox.IsChecked.Value)
                {
                    cbNoMod.IsChecked = false;
                }
            }

            if (ReferenceEquals(sender, xCbSpace) && xCbSpace.IsChecked.Value)
            {
                tbKeySet.Text = "";
                //currentKeyOut = VK_SPACE;
                xCbDelete.IsChecked = false;
            }
            else if (ReferenceEquals(sender, xCbDelete) && xCbDelete.IsChecked.Value)
            {
                tbKeySet.Text = "";
                //currentKeyOut = VK_DELETE;
                xCbSpace.IsChecked = false;
            }
        }

        private void OnBtnSetProfileChangeKeyClicked(object sender, RoutedEventArgs e)
        {



        }
    }
}
