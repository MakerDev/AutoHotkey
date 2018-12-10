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
using AutoHotKey.UserInterfaces;

namespace AutoHotKey.UserInterfaces
{
    /// <summary>
    /// MacroSetting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MacroSetting : Window
    {
        private const int MAXHOTKEY = 35;

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
        }

        private void OnHotkeyClicked(object sender, RoutedEventArgs e)
        {
            //리스트의 핫키가 눌리면 우측에 핫 키 정보 및 수정 여부 묻기
            Button button = sender as Button;
            string btnName = (string)button.Content;
            mCurrentHotkeySelected = int.Parse(btnName[0].ToString());

            HotkeyPair currentHotkey = HotKeyController.Instance.GetHotKeyFromIndex(mCurrentHotkeySelected - 1);
            labelCurrentHotkey.Content = currentHotkey.Trigger.ToString();
            labelCurrentToDo.Content = "핫 키:";
            labelCurrentToDo.Content += currentHotkey.Action.ToString();

            sHotKeyClicked.Visibility = Visibility.Visible;
        }

        private void OnCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(sender, cbNoMod) && cbNoMod.IsChecked.Value)
            {
                cbAlt.IsChecked = false;
                cbControl.IsChecked = false;
                cbWindow.IsChecked = false;
                cbShift.IsChecked = false;

            }
            else
            {
                var checkBox = sender as CheckBox;
                if (checkBox != null && checkBox.IsChecked.Value)
                {
                    cbNoMod.IsChecked = false;
                }
            }
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
                if (checkBox != null && checkBox.IsChecked.Value)
                {
                    cbNoModToDo.IsChecked = false;
                }
            }
        }


        private void OnDeleteHotkeyClicked(object sender, RoutedEventArgs e)
        {
            if (mCurrentHotkeySelected==0)
            {
                MessageBox.Show("핫키를 선택하세요.");
                return;
            }
            if (MessageBox.Show("정말 삭제하시겠습니까?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            HotKeyController.Instance.DeleteHotkey(mCurrentHotkeySelected);
            mCurrentHotkeySelected = 0;
            sHotKeyClicked.Visibility = Visibility.Hidden;
            LoadHotkeyButtonList();
        }

        private void OnSetkeyClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnOkClicked(object sender, RoutedEventArgs e)
        {
            if ((!cbNoMod.IsChecked.Value && !cbControl.IsChecked.Value && !cbAlt.IsChecked.Value && !cbWindow.IsChecked.Value && !cbShift.IsChecked.Value)
                || (String.IsNullOrEmpty(tbKeySet.Text)) || String.IsNullOrWhiteSpace(tbKeySet.Text))
            {
                MessageBox.Show("입력 조건을 정확히 설정하십시오.");
                return;
            }

            if ((!cbNoModToDo.IsChecked.Value && !cbControlToDo.IsChecked.Value && !cbAltToDo.IsChecked.Value
                && !cbWindowToDo.IsChecked.Value && !cbShiftToDo.IsChecked.Value) || (String.IsNullOrEmpty(tbKeySetToDo.Text)) || String.IsNullOrWhiteSpace(tbKeySetToDo.Text))
            {
                MessageBox.Show("출력 조건을 정확히 설정하십시오.");
                return;
            }

            int modIn = EModifiers.NoMod;
            if (cbAlt.IsChecked.Value) modIn |= EModifiers.Alt;
            if (cbControl.IsChecked.Value) modIn |= EModifiers.Ctrl;
            if (cbShift.IsChecked.Value) modIn |= EModifiers.Shift;
            if (cbWindow.IsChecked.Value) modIn |= EModifiers.Win;


            int modOut = EModifiers.NoMod;
            if (cbAltToDo.IsChecked.Value) modOut |= EModifiers.Alt;
            if (cbControlToDo.IsChecked.Value) modOut |= EModifiers.Ctrl;
            if (cbShiftToDo.IsChecked.Value) modOut |= EModifiers.Shift;
            if (cbWindowToDo.IsChecked.Value) modOut |= EModifiers.Win;

            HotkeyPair hotkey = new HotkeyPair(new HotkeyInfo(currentKeyIn, modIn), new HotkeyInfo(currentKeyOut, modOut));

            if (HotKeyController.Instance.AddNewHotkey(hotkey))
            {
                hotkeyListBtn.Add(CreateNewHotKeyButton(hotkey));
            }


            SetView();
            BackToHotkeySelectionViewInternal();
        }

        private Button CreateNewHotKeyButton(HotkeyPair info)
        {
            Button button = new Button();

            //버튼이름을 통해서 버튼이 어떤 핫 키를 가리키는 지 알 수 있도록 함.
            string buttonText = (hotkeyListBtn.Count + 1).ToString() + ". ";

            buttonText += info.Trigger.ToString();

            button.Content = buttonText;
            button.FontSize = 15;
            button.Width = 330;
            button.Height = 35;
            button.Click += new RoutedEventHandler(OnHotkeyClicked);

            return button;
        }

        private void OnKeyInput(object sender, KeyEventArgs e)
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
            }
        }

    }
}
