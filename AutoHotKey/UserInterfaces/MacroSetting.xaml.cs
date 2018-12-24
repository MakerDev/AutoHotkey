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
    /// MacroSetting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MacroSetting : Window
    {
        private const int MAXHOTKEY = 35;
        const int VK_SPACE = 0x20;
        const int VK_DELETE = 0x2E;

        //버튼 순서로 어떤 핫 키 인지 구분
        private List<Button> hotkeyListBtn = new List<Button>();
        private StackPanel list;
        private int currentKeyIn = -1;
        private int currentKeyOut = -1;
        private int mCurrentHotkeySelected = 0; //현재 버튼에 의해 눌린 핫키

        public MacroSetting(int profileNum)
        {
            InitializeComponent();

            list = new StackPanel();
            list.HorizontalAlignment = HorizontalAlignment.Left;
            list.VerticalAlignment = VerticalAlignment.Top;

            SetView();

            scrViewerMacroList.Content = list;

            Closing += EndEditting;

            HotKeyController.Instance.StartEditProfile(profileNum);
            LoadHotkeyButtonList();
        }




        private void EndEditting(object sender, System.ComponentModel.CancelEventArgs e)
        {

            HotKeyController.Instance.EndEditting();

            //throw new NotImplementedException();

            //TODO:편집 창 종료 시 할 일, 세팅 창 켤 때 프로필 창 끄고 세팅 창 끌 때 프로필 창 다시 열기
        }

        //이제 컨트롤러는 우리가 어떤 프로필을 편집하는 지 알고 있다.
        private void OnAddClicked(object sender, RoutedEventArgs e)
        {
            sHotKeyClicked.Visibility = Visibility.Hidden;
            sHotKeySetting.Visibility = Visibility.Visible;
            scrViewerClickAvoider.Visibility = Visibility.Visible;

        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            ClearHotkeySettingOptions();
            BackToHotkeySelectionViewInternal();
        }

        private void BackToHotkeySelectionViewInternal()
        {
            sHotKeyClicked.Visibility = Visibility.Hidden;
            sHotKeySetting.Visibility = Visibility.Hidden;
            scrViewerClickAvoider.Visibility = Visibility.Hidden;

            mCurrentHotkeySelected = 0;

        }

        private void LoadHotkeyButtonList()
        {
            List<HotkeyPair> list = HotKeyController.Instance.GetHotkeyListOfCurrentProfile();

            hotkeyListBtn.Clear();

            foreach (var hotkey in list)
            {
                hotkeyListBtn.Add(CreateNewHotKeyButton(hotkey));
            }

            SetView();
        }

        //핫키를 추가 및 제거했을 때 리스트 목록을 다시 표시하고 오른쪽에 상태 표시를 Hidden으로 바꿔준다.
        private void SetView()
        {
            list.Children.Clear();

            foreach (var btn in hotkeyListBtn)
            {
                list.Children.Add(btn);
            }


            Button addButton = new Button();
            addButton.Content = "ADD NEW HOTKEY";
            addButton.Margin = new Thickness(0, 0, 0, 5);
            addButton.Width = 330;
            addButton.Height = 35;
            addButton.Click += new RoutedEventHandler(OnAddClicked);

            list.Children.Add(addButton);


            ClearHotkeySettingOptions();
        }

        private void ClearHotkeySettingOptions()
        {
            cbNoModToDo.IsChecked = false;
            cbAltToDo.IsChecked = false;
            cbControlToDo.IsChecked = false;
            cbWindowToDo.IsChecked = false;
            cbShiftToDo.IsChecked = false;

            tbKeySet.Text = "";
            tbKeySetToDo.Text = "";

            currentKeyIn = -1;
            currentKeyOut = -1;

        }


        private void OnHotkeyClicked(object sender, RoutedEventArgs e)
        {
            //리스트의 핫키가 눌리면 우측에 핫 키 정보 및 수정 여부 묻기
            Button button = sender as Button;
            string btnName = (string)button.Content;

            int digit = GetHotKeyNumberFromButtonNameInternal(btnName);

            mCurrentHotkeySelected = int.Parse(btnName.Substring(0, digit).ToString());


            HotkeyPair currentHotkey = HotKeyController.Instance.GetHotKeyFromIndex(mCurrentHotkeySelected - 1);
            labelCurrentHotkey.Content = currentHotkey.Trigger.ToString();
            labelCurrentToDo.Content = "Hotkey :";
            labelCurrentToDo.Content += currentHotkey.Action.ToString();

            xTextBoxExplanation.Text = HotKeyController.Instance.GetHotKeyFromIndex(mCurrentHotkeySelected - 1).Explanation;

            sHotKeyClicked.Visibility = Visibility.Visible;
        }

        private int GetHotKeyNumberFromButtonNameInternal(string name)
        {
            //숫자가 두 자리 이상일때도 처리하기 위해
            int digit = 0;

            for (int i = 0; i < 3; i++)
            {
                //만약 i번째 글자가 숫자라면 자릿 수 하나씩 증가
                if (int.TryParse(name[i].ToString(), out int temp))
                {
                    digit++;
                }
                else
                {
                    break;
                }
            }

            return digit;
        }

        private void OnToDoCheckBoxChanged(object sender, RoutedEventArgs e)
        {

            if (ReferenceEquals(sender, cbNoModToDo) && cbNoModToDo.IsChecked.Value)
            {
                cbAltToDo.IsChecked = false;
                cbControlToDo.IsChecked = false;
                cbWindowToDo.IsChecked = false;
                cbShiftToDo.IsChecked = false;
            }
            else
            {
                var checkBox = sender as CheckBox;
                if (checkBox != null && (!ReferenceEquals(sender, xCbDeleteToDo) && !ReferenceEquals(sender, xCbSpaceToDo)) && checkBox.IsChecked.Value)
                {
                    cbNoModToDo.IsChecked = false;
                }
            }

            if (ReferenceEquals(sender, xCbSpaceToDo) && xCbSpaceToDo.IsChecked.Value)
            {
                tbKeySetToDo.Text = "";
                currentKeyOut = VK_SPACE;
                xCbDeleteToDo.IsChecked = false;
            }
            else if (ReferenceEquals(sender, xCbDeleteToDo) && xCbDeleteToDo.IsChecked.Value)
            {
                tbKeySetToDo.Text = "";
                currentKeyOut = VK_DELETE;
                xCbSpaceToDo.IsChecked = false;
            }


        }


        private void OnDeleteHotkeyClicked(object sender, RoutedEventArgs e)
        {
            if (mCurrentHotkeySelected == 0)
            {
                MessageBox.Show("Select a Hotkey");
                return;
            }
            if (MessageBox.Show("Really sure to delete this?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            HotKeyController.Instance.DeleteHotkey(mCurrentHotkeySelected);
            mCurrentHotkeySelected = 0;
            sHotKeyClicked.Visibility = Visibility.Hidden;
            LoadHotkeyButtonList();
        }

        private void OnOkClicked(object sender, RoutedEventArgs e)
        {
            if ((String.IsNullOrEmpty(tbKeySet.Text)) || String.IsNullOrWhiteSpace(tbKeySet.Text))
            {
                MessageBox.Show("Invalid Input Setting");
                return;
            }

            //출력에서는 아무것도 체크하지 않은 것은 부정한 것으로 판단.
            if ((!cbNoModToDo.IsChecked.Value && !cbControlToDo.IsChecked.Value && !cbAltToDo.IsChecked.Value
                && !cbWindowToDo.IsChecked.Value && !cbShiftToDo.IsChecked.Value))
            {
                MessageBox.Show("Invalid Output Setting");
                return;
            }

            int count = 0;
            //만약 키가 빈칸인 경우, NoMod만 체크되거나 여러가지 조합키가 선택되면 부정.
            if ((String.IsNullOrEmpty(tbKeySetToDo.Text) || String.IsNullOrWhiteSpace(tbKeySetToDo.Text)) && !cbNoModToDo.IsChecked.Value)
            {
                if (cbAltToDo.IsChecked.Value) count++;
                if (cbControlToDo.IsChecked.Value) count++;
                if (cbShiftToDo.IsChecked.Value) count++;
                if (cbWindowToDo.IsChecked.Value) count++;

                if (count > 1)
                {
                    MessageBox.Show("Invalid Output Setting");
                    return;
                }
                else if(currentKeyOut==-1)
                {
                    currentKeyOut = 0;
                }
            }

            //입력은 반드시 단일키
            int modIn = EModifiers.NoMod;

            int modOut = EModifiers.NoMod;
            if (cbAltToDo.IsChecked.Value) modOut |= EModifiers.Alt;
            if (cbControlToDo.IsChecked.Value) modOut |= EModifiers.Ctrl;
            if (cbShiftToDo.IsChecked.Value) modOut |= EModifiers.Shift;
            if (cbWindowToDo.IsChecked.Value) modOut |= EModifiers.Win;


            HotkeyPair hotkey;

            //else 지움
            hotkey = new HotkeyPair(new HotkeyInfo(currentKeyIn, modIn), new HotkeyInfo(currentKeyOut, modOut));
            

            if (HotKeyController.Instance.AddNewHotkey(hotkey))
            {
                hotkeyListBtn.Add(CreateNewHotKeyButton(hotkey));
            }


            SetView();
            BackToHotkeySelectionViewInternal();
        }

        private Button CreateNewHotKeyButton(HotkeyPair info)
        {
            string newLine = Environment.NewLine;

            Button button = new Button();

            //버튼이름을 통해서 버튼이 어떤 핫 키를 가리키는 지 알 수 있도록 함.
            string buttonText = (hotkeyListBtn.Count + 1).ToString() + ". ";

            buttonText += info.Trigger.ToString();

            if (!String.IsNullOrEmpty(info.Explanation))
            {
                buttonText += (" " + info.Explanation);
            }

            button.FontSize = 15;
            button.Content = buttonText;
            button.Width = 330;
            button.Height = 35;
            button.Click += new RoutedEventHandler(OnHotkeyClicked);

            return button;
        }

        private void OnKeyInput(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ((TextBox)sender).Text = e.Key.ToString();

            //tbKeySet.Text = e.Key.ToString();
            if (ReferenceEquals(sender, tbKeySet))
            {
                currentKeyIn = int.Parse(KeyInterop.VirtualKeyFromKey(e.Key).ToString());
            }
            else if (ReferenceEquals(sender, tbKeySetToDo))
            {
                currentKeyOut = int.Parse(KeyInterop.VirtualKeyFromKey(e.Key).ToString());
                xCbSpaceToDo.IsChecked = false;
                xCbDeleteToDo.IsChecked = false;
            }
        }

        private void xTextBoxExplanation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (mCurrentHotkeySelected == 0)
                return;

            //현재 선택된 버튼을 통해           
            string txt = xTextBoxExplanation.Text;
            HotKeyController.Instance.SetHotkeyExplanation(mCurrentHotkeySelected, txt);

            string buttonText = mCurrentHotkeySelected.ToString() + ". ";
            HotkeyPair info = HotKeyController.Instance.GetHotKeyFromIndex(mCurrentHotkeySelected - 1);


            buttonText += info.Trigger.ToString();

            if (String.IsNullOrEmpty(xTextBoxExplanation.Text))
            {
                hotkeyListBtn.ElementAt(mCurrentHotkeySelected - 1).Content = buttonText;
                return;
            }


            if (!String.IsNullOrEmpty(info.Explanation))
            {
                buttonText += " ";
                buttonText += (info.Explanation);
                hotkeyListBtn.ElementAt(mCurrentHotkeySelected - 1).Content = buttonText;
                hotkeyListBtn.ElementAt(mCurrentHotkeySelected - 1).FontSize = 15;
            }
        }
    }
}
