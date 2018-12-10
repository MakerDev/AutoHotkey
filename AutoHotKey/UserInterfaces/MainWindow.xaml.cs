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

namespace AutoHotKey
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Button> buttons = new List<Button>();

        private Button addButton;
        private StackPanel list;

        private int currentProfile = 0; //만약 아무 프로필도 선택되지 않은 상태일때는 0


        public MainWindow()
        {
            InitializeComponent();
            
            list = new StackPanel();
            list.HorizontalAlignment = HorizontalAlignment.Left;
            list.VerticalAlignment = VerticalAlignment.Top;

            ClearViewInternal();
            
            scrViewerProfiles.Content = list;

            this.Loaded += OnMainWindowLoaded;
        }

        //생성자가 실행되었을 때 발동
        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            HotKeyController.Instance.RegisterHelper(this);
        }

        //TODO : 만약 프로필 순서 바꾸는 기능을 추가하려면 구현은 파일이름을 바꾸고 다시 로딩하는 걸로 구현하면 됨 
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

            UserInterfaces.MacroSetting macroSettingWindow = new UserInterfaces.MacroSetting(currentProfile);
            //TODO:추후에 visibility 옵션 히든으로 조정
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
    }
}
