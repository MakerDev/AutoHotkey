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
    public partial class SelectKeyPage : Page, ISettingWindow
    {
        public event EventHandler<MacroControllers.ScreenKeyboard.ScreenKey> OnReturnHotkeyInfo;

        private const double SCREEN_WIDTH_RITIO = 0.89;

        private List<HotkeyPair> mHotkeyList;

        private Button mBtnCurrentSelected = null;
        private Dictionary<int, Button> mDictKeysOnProfile = new Dictionary<int, Button>();

        private int currentKeyIn = 0;
        private int currentKeyOut = 0;
        private int currentEndingKey = 0;

        private int mCurrentHotkeySelected = 0; //현재 버튼에 의해 눌린 핫키코드
        private int currentProfile;
        private bool mIsSelectingKey = false;
        private string mBtnColorBefore = null;
        private bool mIsSelectingSpecialKey = false;
        private List<Window> mWindowsToClose = new List<Window>();

        private int mModifierIn = 0;
        private int mModifierOut = 0;

        public SelectKeyPage(int profileNum)
        {
            InitializeComponent();

            currentProfile = profileNum;


            if (profileNum >= 0)
            {
                HotKeyController.Instance.StartEditProfile(profileNum);
                SetView();
            }
            else
            {
                xCheckBoxIsTogle.Visibility = Visibility.Visible;
                sHotKeySetting.Visibility = Visibility.Visible;
            }

            ClearHotkeySettingOptions();
            SetWindowSizeToResolution();

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

                    sEndingKeySettingStack.Visibility = Visibility.Collapsed;
                    xLabelEndingKeyAvailability.Visibility = Visibility.Visible;

                    //xStackModifierSetter.Visibility = Visibility.Collapsed;
                    //xLabelNoModifier.Visibility = Visibility.Visible;
                }
                else
                {
                    tbKeySetToDo.Text = KeyInterop.KeyFromVirtualKey(currentKeyOut).ToString();

                    sEndingKeySettingStack.Visibility = Visibility.Visible;
                    xLabelEndingKeyAvailability.Visibility = Visibility.Collapsed;

                    //xStackModifierSetter.Visibility = Visibility.Visible;
                    //xLabelNoModifier.Visibility = Visibility.Collapsed;

                }
            }
        }

        public void EndSelecting()
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
        public void SetSelectKeyOption(bool toSelect)
        {
            mIsSelectingKey = toSelect;

            if (mIsSelectingKey)
            {
                xBtnToSelectMode.Content = "Stop Selceting";
            }
            else
            {
                xBtnToSelectMode.Content = "Select Key";
            }

        }


        private string GetMouseEventExplanation(int keycode)
        {
            int button = Convert.ToInt32((keycode.ToString()).Substring(1, 1));
            int mouseEvent = Convert.ToInt32((keycode.ToString()).Substring(2, 1));

            return HotkeyInfo.GetMouseEventExplanation(button, mouseEvent);
        }


        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            if (currentProfile > 0)
            {
                SetButtonColor(mBtnCurrentSelected, Brushes.White.ToString());
                BackToHotkeySelectionViewInternal();
            }

            ClearHotkeySettingOptions();
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
            if (currentProfile < 0)
            {
                return;
            }

            int screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            int windowToWidth = (int)(screenWidth * SCREEN_WIDTH_RITIO);

            //Scaling 정보
            double scaling = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice.M11;
            double ratioToAdjust = windowToWidth / xGridMain.Width / scaling;

            xGridMain.LayoutTransform = new ScaleTransform(ratioToAdjust, ratioToAdjust, 0, 0);
        }

        //발견된 인덱스를 반환, -1일 경우 없다는 뜻
        private int GetHotkeyIndexByKeycode(int keycode, List<HotkeyPair> list)
        {
            if (currentProfile == -1)
            {
                return -1;
            }

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

        public void ClearHotkeySettingOptions()
        {
            cbNoModToDo.IsChecked = false;
            cbAltToDo.IsChecked = false;
            cbControlToDo.IsChecked = false;
            cbWindowToDo.IsChecked = false;
            cbShiftToDo.IsChecked = false;

            cbNoMod.IsChecked = false;
            cbAlt.IsChecked = false;
            cbControl.IsChecked = false;
            cbWindow.IsChecked = false;
            cbShift.IsChecked = false;

            xCheckBoxIsTogle.IsChecked = false;

            xStackModifierSetter.Visibility = Visibility.Visible;
            xLabelNoModifier.Visibility = Visibility.Collapsed;

            xLabelEndingKeyAvailability.Visibility = Visibility.Visible;
            sEndingKeySettingStack.Visibility = Visibility.Collapsed;


            tbKeySetToDo.Text = "";
            tbKeySetToDoOnEnding.Text = "";

            currentKeyIn = 0;
            currentKeyOut = 0;
            currentEndingKey = 0;

            mModifierIn = 0;
            mModifierOut = 0;

            SetSelectKeyOption(false);
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

            //TODO : 어떤 select key인지 구분하고, 동시에 select키 두 개가 작동하지 않도록 하기
            if (mIsSelectingKey)
            {
                tbKeySetToDo.Text = KeyInterop.KeyFromVirtualKey(keycode).ToString();
                currentKeyOut = keycode;

                xStackModifierSetter.Visibility = Visibility.Visible;
                xLabelNoModifier.Visibility = Visibility.Collapsed;
                sEndingKeySettingStack.Visibility = Visibility.Visible;
                xLabelEndingKeyAvailability.Visibility = Visibility.Collapsed;

                //현재는 출력 값을 선택하기 위해 버튼을 누르는 것이므로 현재 버튼은 그대로 유지되어야 하기 때문
                return;
            }

            int indexOfHotkey = GetHotkeyIndexByKeycode(keycode, mHotkeyList);

            string tempColor = Brushes.Black.ToString();

            if (indexOfHotkey != -1)
            {
                HotkeyPair currentHotkey = mHotkeyList[indexOfHotkey];
                labelCurrentHotkey.Content = tempCurrentButton.Tag.ToString();
                //TODO : 조합키 표시 때문에
                labelCurrentHotkey.Content = currentHotkey.Trigger.ToString();

                string todoText = currentHotkey.Action.ToString();

                if(currentHotkey.EndingAction != null && currentHotkey.EndingAction.Key > 0)
                {
                    todoText += " | Ending Key : ";
                    todoText += currentHotkey.EndingAction.ToString();
                }

                labelCurrentToDo.Text = todoText;

                xTextBoxExplanation.Text = HotKeyController.Instance.GetHotKeyFromIndex(indexOfHotkey).Explanation;

                sHotKeyClicked.Visibility = Visibility.Visible;
                sHotKeySetting.Visibility = Visibility.Hidden;
            }
            else
            {
                //TODO : 설명을 태그로 바꾸는 걸 고려
                tempColor = Brushes.White.ToString();

                currentKeyIn = mCurrentHotkeySelected;
                //xLableCurrnetIn.Content = KeyInterop.KeyFromVirtualKey(currentKeyIn).ToString();
                //xLableCurrnetIn.Content = tempCurrentButton.Tag.ToString();
                sHotKeyClicked.Visibility = Visibility.Hidden;
                sHotKeySetting.Visibility = Visibility.Visible;
            }

            SetButtonColor(tempCurrentButton, "#FF0080FF");

            if(mBtnCurrentSelected != null && mBtnCurrentSelected != tempCurrentButton)
            {
                if(mBtnColorBefore != null)
                {
                    SetButtonColor(mBtnCurrentSelected, mBtnColorBefore);
                }

            }

            mBtnCurrentSelected = tempCurrentButton;
            mBtnColorBefore = tempColor;
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

        //TODO : 훅 컨트롤러에서 처럼. 그냥 nomod를 없애고 체크박스에 따라서 mod에 더하거나 빼는 방식 채택
        private void OnCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;

            if (ReferenceEquals(sender, cbNoMod) && cbNoMod.IsChecked.Value)
            {
                cbAlt.IsChecked = false;
                cbControl.IsChecked = false;
                cbWindow.IsChecked = false;
                cbShift.IsChecked = false;
            }
            else
            {
                var check = sender as CheckBox;
                if (check != null && check.IsChecked.Value)
                {
                    cbNoMod.IsChecked = false;
                }
            }

            if (checkBox.IsChecked.Value)
            {
                mModifierIn |= Convert.ToInt32(checkBox.Tag.ToString());
            }
            else
            {
                mModifierIn -= Convert.ToInt32(checkBox.Tag.ToString());
            }
        }

        private void OnToDoCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            CheckBox box = sender as CheckBox;

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

            if (box.IsChecked.Value)
            {
                mModifierOut |= Convert.ToInt32(box.Tag.ToString());
            }
            else
            {
                mModifierOut -= Convert.ToInt32(box.Tag.ToString());
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
            int modOut = EModifiers.NoMod;

            //마우스인 경우의 처리
            if (currentKeyOut < 0)
            {              
                modOut = Convert.ToInt32(currentKeyOut.ToString().Substring(2, 1));
                modOut *= 100;

                //modifier의 최대크기가 0x0008로 한 자리 수이니 100배한 click, down여부를 더하면 정보가 섞이지 않는다.
                if (mModifierOut > 0)
                {
                    modOut += mModifierOut;

                }

                currentKeyOut = Convert.ToInt32(currentKeyOut.ToString().Substring(1, 1));
            }
            else
            {

                modOut = mModifierOut;

            }


            HotkeyPair hotkey;

            HotkeyInfo endingKey = null;

            if(currentEndingKey != 0)
            {
                endingKey = new HotkeyInfo(currentEndingKey, EModifiers.NoMod);
            }

            hotkey = new HotkeyPair(new HotkeyInfo(currentKeyIn, mModifierIn), new HotkeyInfo(currentKeyOut, modOut), endingKey);

            //TODO : 여기만 이벤트를 등록해서 처리하도록 바꾸면 될 것 같음. 아니면 처리 코드는 여기다 다 써놓고 어떤걸 실행할지를 결정...?
            if (currentProfile >= 0)
            {
                HotKeyController.Instance.AddNewHotkey(hotkey);

                mDictKeysOnProfile.Add(currentKeyIn, mBtnCurrentSelected);
                SetButtonColor(mBtnCurrentSelected, Brushes.Black.ToString());
            }
            else if (currentProfile < 0)
            {
                MacroControllers.ScreenKeyboard.ScreenKey screenKey = new MacroControllers.ScreenKeyboard.ScreenKey();
                screenKey.IsTogle = xCheckBoxIsTogle.IsChecked.Value;
                screenKey.Action = new HotkeyInfo(currentKeyOut, modOut);

                OnReturnHotkeyInfo(this, screenKey);
            }
            else
            {
                SetButtonColor(mBtnCurrentSelected, Brushes.White.ToString());
            }

            ClearHotkeySettingOptions();

            if (currentProfile > 0)
            {
                BackToHotkeySelectionViewInternal();
            }
        }


        private void OnKeyInput(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ((TextBox)sender).Text = e.Key.ToString();

            if (ReferenceEquals(sender, tbKeySetToDo))
            {
                currentKeyOut = int.Parse(KeyInterop.VirtualKeyFromKey(e.Key).ToString());

                xStackModifierSetter.Visibility = Visibility.Visible;
                xLabelNoModifier.Visibility = Visibility.Collapsed;

                xLabelEndingKeyAvailability.Visibility = Visibility.Collapsed;
                sEndingKeySettingStack.Visibility = Visibility.Visible;
            }
            else if (ReferenceEquals(sender, tbKeySetToDoOnEnding))
            {
                currentEndingKey = int.Parse(KeyInterop.VirtualKeyFromKey(e.Key).ToString());
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
            SetSelectKeyOption(!mIsSelectingKey);
        }

    }
}

