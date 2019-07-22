using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using AutoHotKey.UserInterfaces;

namespace AutoHotKey.UserInterfaces
{
    /// <summary>
    /// MacroSettingWithPicture.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MacroSettingWithPicture : Window
    {
        private SelectKeyPage mSelectionPage;
        private int mCurrentProfile;
        private List<Window> mWindowsToClose = new List<Window>();

        //TODO : RenderTransform으로 해상도에 따른 크기 문제 해결하기  
        public MacroSettingWithPicture(int profileNum)
        {
            InitializeComponent();

            mCurrentProfile = profileNum;

            xLabelCurrentProfile.Content = "Current Profile : " + profileNum.ToString();

            mSelectionPage = new SelectKeyPage(profileNum);
            xFrameKeySelection.Content = mSelectionPage;
            Closing += OnMacroSettingClosing;
        }

        private void OnMacroSettingClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mSelectionPage.EndSelecting();

            foreach (var window in mWindowsToClose)
            {
                if (window != null)
                {
                    window.Close();
                }
            }
        }

        private void xBtnSetProfileChangingKeys_Click(object sender, RoutedEventArgs e)
        {
            if (HotKeyController.Instance.IsSettingProfileChangingKeys())
            {
                MessageBox.Show("You already Opened a Setting Window");
                return;
            }

            //TODO : 이미 편집 창이 열려 있으면 버튼 작동시키지 않기 + 핫 키 멈추기(프로필 키 바꾸는 걸 컨트롤러에 알리기)
            ProfileChangeKeysTable profileChangeKeysTable = new ProfileChangeKeysTable(mCurrentProfile);
            mWindowsToClose.Add(profileChangeKeysTable);
            profileChangeKeysTable.Show();
        }

        

    }

}
