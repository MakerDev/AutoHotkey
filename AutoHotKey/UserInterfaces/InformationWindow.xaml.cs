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

namespace AutoHotKey.UserInterfaces
{
    /// <summary>
    /// InformationWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InformationWindow : Window
    {
        public InformationWindow()
        {
            InitializeComponent();

            this.ShowInTaskbar = false;
        }

        public void ChangeCurrentProfile(int profileNum)
        {
            if(profileNum==0)
            {
                xLabelCurrentProfile.Content = "No Profile";
            }
            else
            {
                xLabelCurrentProfile.Content = "Profile " + profileNum.ToString();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
