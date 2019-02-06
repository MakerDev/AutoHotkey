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
using AutoHotKey.MacroControllers;


namespace AutoHotKey.UserInterfaces
{
    /// <summary>
    /// SpecialKeySelection.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SpecialKeySelection : Window
    {
        private CheckBox mLastCheckBox = null;

        private int keycode = -1;
        private ISettingWindow mSettingWindow = null;
        private CheckBox mCurrentCheckBox = null;
        private int mMouseOption = 0;

        public SpecialKeySelection(ISettingWindow settingWindow, bool IsGeneralKeyNeeded = false)
        {
            InitializeComponent();

            Closing += SpecialKeySelection_Closing;

            mSettingWindow = settingWindow;

            if(IsGeneralKeyNeeded)
            {
                Width = 725;
                xStackGeneralKeys.Visibility = Visibility.Visible;
            }
        }


        private void SpecialKeySelection_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mSettingWindow != null)
            {
                mSettingWindow.EndSelectingSpecialKey(-1);
            }
        }

        //마우스 이벤트는 키코드를 음수로 표시한다 ex) -12 면 left버튼 더블클릭이라는뜻. 
        private void OnBtnOkClicked(object sender, RoutedEventArgs e)
        {
            if (mSettingWindow != null)
            {
                mSettingWindow.EndSelectingSpecialKey(keycode);
            }

            this.Close();
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

                //마우스 이벤트라면
                if(keycode >= 1 && keycode <= 4)
                {
                    string code = "-" + keycode.ToString() + mMouseOption.ToString();
                    keycode = Convert.ToInt32(code);
                    xStackMouseOptions.Visibility = Visibility.Visible;
                }

            }
        }

        private void OnCheckBoxUnChecked(object sender, RoutedEventArgs e)
        {
            CheckBox currentCheckBox = sender as CheckBox;

            int tag = Convert.ToInt32(currentCheckBox.Tag.ToString());

            if(tag >= 1 && tag <= 4)
            {
                xStackMouseOptions.Visibility = Visibility.Hidden;
            }

            if (mCurrentCheckBox == currentCheckBox)
            {
                keycode = -1;
                mLastCheckBox = null;
            }
        }

        //Tag 0는 click이고 Tag 1은 double click tag 2은 down
        private void OnMouseOptionChanged(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;

            mMouseOption = Convert.ToInt32(radioButton.Tag.ToString());

            if(keycode != -1)
            {
                string code = keycode.ToString().Substring(0, 2) + mMouseOption.ToString();
                keycode = Convert.ToInt32(code);
            }
        }
    }
}
