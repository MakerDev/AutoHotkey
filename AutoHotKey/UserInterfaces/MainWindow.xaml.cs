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

namespace AutoHotKey
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Button> buttons = new List<Button>();

        private Button addButton;
        
        private int currentProfile = 0; //만약 아무 프로필도 선택되지 않은 상태일때는 0


        public MainWindow()
        {
            InitializeComponent();
            
            StackPanel list = new StackPanel();
            list.HorizontalAlignment = HorizontalAlignment.Left;
            list.VerticalAlignment = VerticalAlignment.Top;

            for(int i=1; i<=3; i++)
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

            list.Children.Add(addButton);
           
            scrViewerProfiles.Content = list;
        }

        private void OnProfileClicked(object sender, RoutedEventArgs e)
        {
            string name = (string)((Button)sender).Content;
            string num = name[name.Length - 1].ToString();

            currentProfile = int.Parse(num);

            SetProfileView(currentProfile);
        }

        private void SetProfileView(int numOfProfile)
        {
            tbCurrentProfile.Text = "Profile " + numOfProfile.ToString();
            sProfileContent.Visibility = Visibility.Visible;

        }

        

    }
}
