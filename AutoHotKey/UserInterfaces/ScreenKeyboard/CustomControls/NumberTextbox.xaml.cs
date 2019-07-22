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

namespace AutoHotKey.UserInterfaces.ScreenKeyboard.CustomControls
{
    /// <summary>
    /// NumberTextbox.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NumberTextbox : UserControl
    {
        public event EventHandler<EventArgs> OnNumberChanged;

        private int mLastNum = -1;
        private bool isinitial = true;

        public NumberTextbox()
        {
            InitializeComponent();
        }

        public void SetNumber(int num)
        {
            ntbTextBox.Text = num.ToString();

            if (isinitial)
            {
                isinitial = false;
                mLastNum = num;
                return;
            }

            mLastNum = ReadNumber();
        }

        public int ReadNumber()
        {
            return int.Parse(ntbTextBox.Text);
        }

        private void OnTextChanged(object sender, KeyEventArgs e)
        {
            int keycode;

            int.TryParse(KeyInterop.VirtualKeyFromKey(e.Key).ToString(), out keycode);

            if (!((keycode >= 48 && keycode <= 57) || (keycode >= 96 && keycode <= 105)) && (e.Key != Key.Back && e.Key != Key.Space && e.Key != Key.Delete)
                && (e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.Up && e.Key != Key.Down))
            {
                MessageBox.Show("Number only");
                e.Handled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (button.Tag.ToString() == "UP")
            {
                SetNumber(ReadNumber() + 1);
            }
            else
            {
                if (ReadNumber() > 1)
                {
                    SetNumber(ReadNumber() - 1);
                }
            }

            if (OnNumberChanged != null)
            {
                OnNumberChanged(this, null);
            }
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            int thisNum = ReadNumber();

            if (thisNum != mLastNum)
            {
                mLastNum = thisNum;

                if (OnNumberChanged != null)
                {
                    OnNumberChanged(this, null);
                }
            }
        }
    }
}
