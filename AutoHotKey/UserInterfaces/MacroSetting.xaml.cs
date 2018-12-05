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
using System.Windows.Shapes;
using AutoHotKey.MacroControllers;

namespace AutoHotKey.UserInterfaces
{
    /// <summary>
    /// MacroSetting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MacroSetting : Window
    {
        private const int MAXHOTKEY = 35;

        private Button[] hotkeyList = new Button[MAXHOTKEY];
        private HotkeyInfo[] hotkeyInfos = new HotkeyInfo[MAXHOTKEY];


        //TODO : 파일 읽기 및 쓰기 클래스 제작
        //TODO : 생성자를 통해 몇 번째 프로필인지 전달받기.
        public MacroSetting()
        {
            InitializeComponent();

            StackPanel list = new StackPanel();
            list.HorizontalAlignment = HorizontalAlignment.Left;
            list.VerticalAlignment = VerticalAlignment.Top;

            Button addButton = new Button();
            addButton.Content = "ADD NEW HOTKEY";
            addButton.Margin = new Thickness(0, 0, 0, 5);
            addButton.Width = 330;
            addButton.Height = 35;
            addButton.Click += new RoutedEventHandler(OnAddClicked);

            list.Children.Add(addButton);


        }

        private void OnAddClicked(object sender, RoutedEventArgs e)
        {
            if (hotkeyList.Length >= MAXHOTKEY)
                return;
            
        }

        private void OnHotkeyClicked(object sender, RoutedEventArgs e)
        {
            


        }
    }
}
