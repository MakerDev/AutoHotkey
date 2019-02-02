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
    /// ProfileChangeKeysTable.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProfileChangeKeysTable : Window
    {
        int mNumOfProfiles = 0;

        //만약 프로필 내에서 핫 키를 변경중에 set switching 을 하려고 하면 한 줄만 표시하기 위한 인자
        //0이 전달되면 그냥 메인 화면에서 실행한 것으로 간주
        //-1이 전달되면 deactivating 키를 설정하려는 것
        public ProfileChangeKeysTable(int fromProfile = 0)
        {
            InitializeComponent();

            HotKeyController.Instance.StartSetProfileChangingKeys();

            mNumOfProfiles = HotKeyController.Instance.GetNumOfProfiles();

            Closing += OnProfileChangeKeyTabelClosed;

            int keyEsc = HotKeyController.Instance.GetEscapeKey();
            xTxtBoxEscape.Text = KeyInterop.KeyFromVirtualKey(keyEsc).ToString();

            if(fromProfile != -1)
            {
                xLabelTitle.Visibility = Visibility.Visible;
                xStackDeactivatingSetting.Visibility = Visibility.Collapsed;

                //맨 윗줄
                //TODO : 맨 윗줄, 탈출키는 따로 설정하도록 한다.
                StackPanel firstLine = new StackPanel();
                firstLine.Orientation = Orientation.Horizontal;
                firstLine.HorizontalAlignment = HorizontalAlignment.Center;

                Label label = CreateNewLabel("To->");
                firstLine.Children.Add(label);

                int i = 0;

                for (i = 1; i <= mNumOfProfiles; i++)
                {
                    firstLine.Children.Add(CreateNewLabel(i.ToString()));
                }



                xStackMain.Children.Add(firstLine);

                i = 0;

                if(fromProfile != 0)
                {
                    i = fromProfile-1;
                }

                while(i < mNumOfProfiles)
                {
                    StackPanel line = new StackPanel();
                    line.Orientation = Orientation.Horizontal;
                    line.HorizontalAlignment = HorizontalAlignment.Center;

                    line.Children.Add(CreateNewLabel((i + 1).ToString()));

                    for (int j = 0; j < mNumOfProfiles; j++)
                    {
                        string tag = (i + 1).ToString() + (j + 1).ToString();
                        int key = HotKeyController.Instance.GetProfileChangeKeyFromIndex(i, j);

                        //자기 자신으로 가는 게 아니면
                        if (key != -1)
                        {
                            string keyName = KeyInterop.KeyFromVirtualKey(key).ToString();
                            line.Children.Add(CreateNewTextBox(tag, keyName));
                        }
                        else
                        {
                            line.Children.Add(CreateNewLabel("Self"));
                        }

                    }

                    xStackMain.Children.Add(line);     
                    
                    if(fromProfile != 0)
                    {
                        break;
                    }

                    i++;
                }
 
            }


            Button saveButton = new Button();
            saveButton.Content = "SAVE";
            saveButton.Click += OnSaveBtnClicked;
            saveButton.Width = 150;
            saveButton.Height = 30;
            saveButton.Margin = new Thickness(0, 20, 0, 15);

            xStackMain.Children.Add(saveButton);
        }

        private void OnProfileChangeKeyTabelClosed(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HotKeyController.Instance.EndSetProfileChangingKeys();
        }

        private Label CreateNewLabel(string text)
        {
            Label label = new Label();
            label.Width = 50;
            label.Height = 30;
            label.HorizontalContentAlignment = HorizontalAlignment.Center;
            label.Content = text;
            label.BorderBrush = Brushes.Black;
            label.BorderThickness = new Thickness(1, 1, 1, 1);

            return label;
        }

        //태그는 위치를 말해줌 ex)12는 1번 프로필에서 2번으로 가는 키
        private TextBox CreateNewTextBox(string tag, string currnetKey)
        {
            TextBox textBox = new TextBox();
            textBox.MaxLength = 1;
            textBox.Width = 50;
            textBox.Height = 30;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.HorizontalAlignment = HorizontalAlignment.Center;
            textBox.VerticalAlignment = VerticalAlignment.Center;
            textBox.BorderThickness = new Thickness(1);
            textBox.BorderBrush = Brushes.Black;
            textBox.Text = currnetKey;
            textBox.HorizontalContentAlignment = HorizontalAlignment.Center;
            textBox.VerticalContentAlignment = VerticalAlignment.Center;
            textBox.Tag = tag;
            textBox.KeyDown += OnKeyInput;

            return textBox;
        }


        private void OnKeyInput(object sender, System.Windows.Input.KeyEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string txtBefore = textBox.Text;

            String tag = ((TextBox)sender).Tag.ToString();

            int keyIn = int.Parse(KeyInterop.VirtualKeyFromKey(e.Key).ToString());

            if (tag == "escape")
            {
                int re = HotKeyController.Instance.SetEscapeKey(keyIn);

                int temp = re / 10;

                switch (temp)
                {
                    case 0:
                        textBox.Text = e.Key.ToString();
                        return;
                    case 1:
                        MessageBox.Show("Profile" + (re - temp * 10).ToString() + "Contains Same Hotkey");
                        return;
                    case 2:
                        MessageBox.Show("Profile" + (re - temp * 10).ToString() + "Contains Same Profile-Changing Key");
                        return;
                    default:
                        break;
                }

                return;
            }

            int num = int.Parse(tag);

            int from = num / 10;
            int to = num - (from * 10);


            int result = HotKeyController.Instance.SetProfileChangeKey(from, to, keyIn);

            if (result == 1)
            {
                MessageBox.Show("Same Key Exists in this profile! ");

                textBox.Text = txtBefore;
            }
            else
            {
                ((TextBox)sender).Text = e.Key.ToString();
            }
        }

        private void OnSaveBtnClicked(object sender, RoutedEventArgs e)
        {
            HotKeyController.Instance.SaveProfileChangeKeys();
            MessageBox.Show("Saved");
        }
    }
}
