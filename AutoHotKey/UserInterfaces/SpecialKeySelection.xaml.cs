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
using AutoHotKey.UserInterfaces;

namespace AutoHotKey.UserInterfaces
{
    /// <summary>
    /// SpecialKeySelection.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SpecialKeySelection : Window
    {
        private CheckBox mLastCheckBox = null;

        private int keycode = -1;
        private MacroSettingWithPicture mSettingWindow = null;
        private CheckBox mCurrentCheckBox = null;

        public SpecialKeySelection(MacroSettingWithPicture settingWindow)
        {
            InitializeComponent();

            Closing += SpecialKeySelection_Closing;

            mSettingWindow = settingWindow;
        }

        private void SpecialKeySelection_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mSettingWindow != null)
            {
                mSettingWindow.EndSelectingSpecialKey(-1);
            }
        }

        private void OnBtnOkClicked(object sender, RoutedEventArgs e)
        {
            if(mSettingWindow != null)
            {
                mSettingWindow.EndSelectingSpecialKey(keycode);
            }
        }

        private void OnCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            mCurrentCheckBox = sender as CheckBox;

            if (mCurrentCheckBox.IsChecked.Value)
            {
                keycode = Convert.ToInt32(mCurrentCheckBox.Tag.ToString());

                if (mLastCheckBox != null)
                {
                    mLastCheckBox.IsChecked = false;
                }

                mLastCheckBox = mCurrentCheckBox;
            }
        }

        private void OnCheckBoxUnChecked(object sender, RoutedEventArgs e)
        {
            CheckBox currentCheckBox = sender as CheckBox;

            if (mCurrentCheckBox == currentCheckBox)
            {
                keycode = -1;
                mLastCheckBox = null;
            }
        }
    }
}
