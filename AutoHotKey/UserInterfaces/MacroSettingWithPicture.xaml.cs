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
    public partial class MacroSettingWithPicture : Window, ISettingWindow
    {
        private const int MAXHOTKEY = 35;
        const int VK_SPACE = 0x20;
        const int VK_DELETE = 0x2E;

        private List<HotkeyPair> mHotkeyList;

        private Button mBtnCurrentSelected = null;
        private Dictionary<int, Button> mDictKeysOnProfile = new Dictionary<int, Button>();

        private int currentKeyIn = 0;
        private int currentKeyOut = 0;
        private int mCurrentHotkeySelected = 0; //현재 버튼에 의해 눌린 핫키코드
        private int currentProfile;
        private bool mIsSelectingKey = false;
        private string mBtnColorBefore = null;
        private bool mIsSelectingSpecialKey = false;
        private List<Window> mWindowsToClose = new List<Window>();

        //TODO : RenderTransform으로 해상도에 따른 크기 문제 해결하기  
        public MacroSettingWithPicture(int profileNum)
        {
            InitializeComponent();

            currentProfile = profileNum;

            Closing += OnMacroSettingClosing;
            xLabelCurrentProfile.Content = "Current Profile : " + profileNum.ToString();

            HotKeyController.Instance.StartEditProfile(profileNum);

            SetView();
        }

        public void EndSelectingSpecialKey(int selectedKeycode)
        {
            mIsSelectingSpecialKey = false;

            if (selectedKeycode != -1)
            {
                currentKeyOut = selectedKeycode;

                //0은 선택되지 않음을 나타내는 예약숫자임
                if (currentKeyOut < 0)
                {
                    tbKeySetToDo.Text = GetMouseEventExplanation(currentKeyOut);
                    xStackModifierSetter.Visibility = Visibility.Collapsed;
                    xLabelNoModifier.Visibility = Visibility.Visible;
                }
                else
                {
                    tbKeySetToDo.Text = KeyInterop.KeyFromVirtualKey(currentKeyOut).ToString();
                    xStackModifierSetter.Visibility = Visibility.Visible;
                    xLabelNoModifier.Visibility = Visibility.Collapsed;

                }
            }
        }

        private string GetMouseEventExplanation(int keycode)
        {
            int button = Convert.ToInt32((keycode.ToString()).Substring(1, 1));
            int mouseEvent = Convert.ToInt32((keycode.ToString()).Substring(2, 1));

            return HotkeyInfo.GetMouseEventExplanation(button, mouseEvent);
        }

        private void OnMacroSettingClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            HotKeyController.Instance.EndEditting();

            foreach (var window in mWindowsToClose)
            {
                if (window != null)
                {
                    window.Close();
                }
            }
        }


        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            SetButtonColor(mBtnCurrentSelected, Brushes.White.ToString());

            ClearHotkeySettingOptions();
            BackToHotkeySelectionViewInternal();
        }

        private void BackToHotkeySelectionViewInternal()
        {
            sHotKeyClicked.Visibility = Visibility.Hidden;
            sHotKeySetting.Visibility = Visibility.Hidden;
            //scrViewerClickAvoider.Visibility = Visibility.Hidden;

            xBtnToSelectMode.Content = "Select Key";

            mIsSelectingKey = false;

            mBtnCurrentSelected = null;
            mCurrentHotkeySelected = 0;
        }

        //TODO : 이제 검색은 이름으로 하고 키 정보는 OEM같은 거 대신 태그로 표시하는 걸 고려. 태그로 표시할 시 전부 대문자로 변경 후 출력
        //처음 프로필을 로드했을 때의 화면을 세팅한다
        //TODO : 병목을 줄이기 위해 Xaml의 그리드에서 알파벳, 숫자, 넘패드, 맨 윗줄, 특수키 와 같은 식으로 그룹을 만들고 
        //가장 유력한 그룹먼저 탐색을 한 뒤, 모든 핫 키를 다 소진하면 더 이상의 탐색없이 종료시킨다.
        //알파벳도 여러 그룹으로 나눌 수 있다. 왼쪽 그룹, 중앙그룹, 오른쪽 그룹
        private void SetView()
        {
            Grid gridKeys = xGridKeyButtons;

            //카피 없이 쓰면 참조 방식이기 때문에 원래 있던 데이터와 동기화된 상태이다.
            mHotkeyList = HotKeyController.Instance.GetHotkeyListOfCurrentProfile();

            foreach (HotkeyPair keyPair in mHotkeyList)
            {
                int inputKeycode = keyPair.Trigger.Key;

                Button keyButton = FindButtonByKeycode(inputKeycode);

                if (keyButton != null)
                {
                    SetButtonColor(keyButton, Brushes.Black.ToString());

                    mDictKeysOnProfile.Add(inputKeycode, keyButton);
                }
            }

            ClearHotkeySettingOptions();
            SetWindowSizeToResolution();
        }

        private void SetWindowSizeToResolution()
        {
            int screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;

            //MessageBox.Show(screenWidth.ToString());

        }

        //발견된 인덱스를 반환, -1일 경우 없다는 뜻
        private int GetHotkeyIndexByKeycode(int keycode, List<HotkeyPair> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                HotkeyPair keyPair = list[i];

                if (keyPair.Trigger.Key == keycode)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ClearHotkeySettingOptions()
        {
            cbNoModToDo.IsChecked = false;
            cbAltToDo.IsChecked = false;
            cbControlToDo.IsChecked = false;
            cbWindowToDo.IsChecked = false;
            cbShiftToDo.IsChecked = false;

            xStackModifierSetter.Visibility = Visibility.Visible;
            xLabelNoModifier.Visibility = Visibility.Collapsed;

            tbKeySetToDo.Text = "";

            currentKeyIn = 0;
            currentKeyOut = 0;
        }

        //그림의 키보드가 눌린경우 실행됨. name을 통해 어떤 키인지 파악하고 
        //기존에 등록된 키인지 여부는 Tag의 keycode로 조회하여 확인
        //TAG가 0x로 시작하면 16진수로 읽고 아니면 그냥 10진수로 읽는다.
        private void OnKeyClicked(object sender, RoutedEventArgs e)
        {
            Button tempCurrentButton = sender as Button;
            string name = tempCurrentButton.Name;

            int keycode = Convert.ToInt32(name.Substring(4));
            mCurrentHotkeySelected = keycode;


            if (mIsSelectingKey)
            {
                tbKeySetToDo.Text = KeyInterop.KeyFromVirtualKey(keycode).ToString();
                currentKeyOut = keycode;

                xStackModifierSetter.Visibility = Visibility.Visible;
                xLabelNoModifier.Visibility = Visibility.Collapsed;

                //현재는 출력 값을 선택하기 위해 버튼을 누르는 것이므로 현재 버튼은 그대로 유지되어야 하기 때문
                return;
            }

            int indexOfHotkey = GetHotkeyIndexByKeycode(keycode, mHotkeyList);

            if (indexOfHotkey != -1)
            {
                HotkeyPair currentHotkey = mHotkeyList[indexOfHotkey];
                labelCurrentHotkey.Content = tempCurrentButton.Tag.ToString();

                labelCurrentToDo.Text = currentHotkey.Action.ToString();

                xTextBoxExplanation.Text = HotKeyController.Instance.GetHotKeyFromIndex(indexOfHotkey).Explanation;

                sHotKeyClicked.Visibility = Visibility.Visible;
                sHotKeySetting.Visibility = Visibility.Hidden;
            }
            else
            {
                //TODO : 설명을 태그로 바꾸는 걸 고려

                currentKeyIn = mCurrentHotkeySelected;
                //xLableCurrnetIn.Content = KeyInterop.KeyFromVirtualKey(currentKeyIn).ToString();
                xLableCurrnetIn.Content = tempCurrentButton.Tag.ToString();
                sHotKeyClicked.Visibility = Visibility.Hidden;
                sHotKeySetting.Visibility = Visibility.Visible;
            }

            if (mBtnCurrentSelected != null && mBtnColorBefore != null)
            {
                SetButtonColor(mBtnCurrentSelected, mBtnColorBefore);
            }

            mBtnColorBefore = tempCurrentButton.Background.ToString();

            SetButtonColor(tempCurrentButton, "#FF0080FF");

            mBtnCurrentSelected = tempCurrentButton;
        }

        //keycode로 버튼 찾기
        private Button FindButtonByKeycode(int keycode)
        {
            object wantedNode = xGridKeyButtons.FindName("xBtn" + keycode.ToString());

            if (wantedNode is Button && wantedNode != null)
            {
                Button wantedChild = wantedNode as Button;

                return wantedChild;
            }
            else
            {
                return null;
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
            if (mCurrentHotkeySelected == 0)
            {
                MessageBox.Show("Select a Hotkey");
                return;
            }
            if (MessageBox.Show("Really sure to delete this?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            HotKeyController.Instance.DeleteHotkey(GetHotkeyIndexByKeycode(mCurrentHotkeySelected, mHotkeyList) + 1);

            SetButtonColor(mDictKeysOnProfile[mCurrentHotkeySelected], Brushes.White.ToString());
            mDictKeysOnProfile.Remove(mCurrentHotkeySelected);
            mCurrentHotkeySelected = 0;
            mBtnCurrentSelected = null;

            sHotKeyClicked.Visibility = Visibility.Hidden;
        }


        private void SetButtonColor(Button button, string color)
        {
            var bc = new BrushConverter();
            button.Background = (Brush)bc.ConvertFrom(color);
        }

        private void OnOkClicked(object sender, RoutedEventArgs e)
        {
            //마우스 키는 modifier의 영향을 받지 않음
            if (currentKeyOut >= 0)
            {
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
                }
            }

            //입력은 반드시 단일키
            int modIn = EModifiers.NoMod;
            int modOut = EModifiers.NoMod;

            if (currentKeyOut < 0)
            {
                modOut = Convert.ToInt32(currentKeyOut.ToString().Substring(2, 1));
                currentKeyOut = Convert.ToInt32(currentKeyOut.ToString().Substring(1, 1));
            }
            else
            {
                if (cbAltToDo.IsChecked.Value) modOut |= EModifiers.Alt;
                if (cbControlToDo.IsChecked.Value) modOut |= EModifiers.Ctrl;
                if (cbShiftToDo.IsChecked.Value) modOut |= EModifiers.Shift;
                if (cbWindowToDo.IsChecked.Value) modOut |= EModifiers.Win;
            }



            HotkeyPair hotkey;

            //else 지움
            hotkey = new HotkeyPair(new HotkeyInfo(currentKeyIn, modIn), new HotkeyInfo(currentKeyOut, modOut));


            if (HotKeyController.Instance.AddNewHotkey(hotkey))
            {
                mDictKeysOnProfile.Add(currentKeyIn, mBtnCurrentSelected);
                SetButtonColor(mBtnCurrentSelected, Brushes.Black.ToString());
            }
            else
            {
                SetButtonColor(mBtnCurrentSelected, Brushes.White.ToString());
            }

            ClearHotkeySettingOptions();

            BackToHotkeySelectionViewInternal();
        }


        private void OnKeyInput(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ((TextBox)sender).Text = e.Key.ToString();

            if (ReferenceEquals(sender, tbKeySetToDo))
            {
                currentKeyOut = int.Parse(KeyInterop.VirtualKeyFromKey(e.Key).ToString());

                xStackModifierSetter.Visibility = Visibility.Visible;
                xLabelNoModifier.Visibility = Visibility.Collapsed;
            }
        }

        private void xTextBoxExplanation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (mCurrentHotkeySelected == 0)
                return;

            //현재 선택된 버튼을 통해           
            string txt = xTextBoxExplanation.Text;
            HotKeyController.Instance.SetHotkeyExplanation(GetHotkeyIndexByKeycode(mCurrentHotkeySelected, mHotkeyList) + 1, txt);
        }

        private void xBtnSetProfileChangingKeys_Click(object sender, RoutedEventArgs e)
        {
            if (HotKeyController.Instance.IsSettingProfileChangingKeys())
            {
                MessageBox.Show("You already Opened a Setting Window");
                return;
            }

            //TODO : 이미 편집 창이 열려 있으면 버튼 작동시키지 않기 + 핫 키 멈추기(프로필 키 바꾸는 걸 컨트롤러에 알리기)
            ProfileChangeKeysTable profileChangeKeysTable = new ProfileChangeKeysTable(currentProfile);
            mWindowsToClose.Add(profileChangeKeysTable);
            profileChangeKeysTable.Show();
        }

        private void OnBtnOtherKeys_Click(object sender, RoutedEventArgs e)
        {
            if (!mIsSelectingSpecialKey)
            {
                SpecialKeySelection specialKeySelection = new SpecialKeySelection(this);

                mWindowsToClose.Add(specialKeySelection);
                specialKeySelection.Show();
                mIsSelectingSpecialKey = true;
            }
            else
            {
                MessageBox.Show("Already selecting special keys");
            }
        }

        private void OnSelectKeyClicked(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            mIsSelectingKey = !mIsSelectingKey;

            if (mIsSelectingKey)
            {
                btn.Content = "Stop Selceting";
            }
            else
            {
                btn.Content = "Select Key";
            }
        }
    }
}
