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
using AutoHotKey.MacroControllers.ScreenKeyboard;
using AutoHotKey.MacroControllers;

namespace AutoHotKey.UserInterfaces
{
    /// <summary>
    /// ScreenKeyboardSettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ScreenKeyboardSettingWindow : Window
    {
        private SelectKeyPage mKeySelectionPage = new SelectKeyPage(-1);
        private int mCurrentKeyboard;
        private ScreenKeyboard.ScreenKeyboardPage mScreenKeyboardPage;

        private ScreenKey mHotkeyForNewScreenKey = null;
        private ScreenKey mClickedKey = null;


        private Stack<ScreenKey> mLastStates = new Stack<ScreenKey>();
        private bool mIsSequencialUndoing = false;

        private bool mPreviewKeyWasToggled = false;


        //0 : Normal, 1 : Hover  2 : Pressed
        //10 : FontNormal, 11 : FontHover, 12 : FontPressed;
        private Dictionary<int, ScreenKeyboard.ColorSelector> mButtonColorSelectors = new Dictionary<int, ScreenKeyboard.ColorSelector>();
        private Dictionary<int, ScreenKeyboard.ColorSelector> mFontColorSelectors = new Dictionary<int, ScreenKeyboard.ColorSelector>();

        private bool mIsAdding = false;  //OK 키가 키를 추가할지 편집할 지 알아야 하므로

        public ScreenKeyboardSettingWindow(int currentKeyboard)
        {
            InitializeComponent();

            mCurrentKeyboard = currentKeyboard;

            mKeySelectionPage.LayoutTransform = new ScaleTransform(0.7, 0.7, 0, 0);
            xFrameKeySelector.Content = mKeySelectionPage;

            mScreenKeyboardPage = new ScreenKeyboard.ScreenKeyboardPage(ScreenKeyboardManager.GetScreenKeyboard(currentKeyboard));
            mScreenKeyboardPage.HorizontalAlignment = HorizontalAlignment.Left;
            mScreenKeyboardPage.VerticalAlignment = VerticalAlignment.Top;

            xFramePreview.Content = mScreenKeyboardPage;
            mScreenKeyboardPage.UpdateView();

            mKeySelectionPage.OnReturnHotkeyInfo += OnKeySelected;
            mScreenKeyboardPage.OnKeyClickedWhileEditing += OnScreenKeyClicked;

            xStackButtonOptions.Visibility = Visibility.Hidden;
            xLabelCurrentWindow.Content = "WINDOW " + currentKeyboard.ToString();

            xTextBoxBtnWidth.OnNumberChanged += OnNumberChanged;
            xTextBoxBtnHeight.OnNumberChanged += OnNumberChanged;

            SetScreenOption();
            SetColorOptionView();

            ScreenKeyboardManager.StartEditing();

            Closing += OnClosing;
        }

        private void SetColorOptionView()
        {
            string[] lables = { "Normal", "Hover", "Pressed" };

            for (int i = 0; i < 3; i++)
            {
                mButtonColorSelectors[i] = new ScreenKeyboard.ColorSelector();
                mFontColorSelectors[i] = new ScreenKeyboard.ColorSelector();


                mButtonColorSelectors[i].OnColorSelected += OnColorSelected;
                mFontColorSelectors[i].OnColorSelected += OnColorSelected;

                Label label = new Label();
                label.Content = lables[i];

                Label label2 = new Label();
                label2.Content = lables[i];


                xStackButtonColors.Children.Add(label);
                xStackButtonColors.Children.Add(mButtonColorSelectors[i]);

                xStackFontColors.Children.Add(label2);
                xStackFontColors.Children.Add(mFontColorSelectors[i]);
            }
        }

        private void SetScreenOption()
        {
            Size size = mScreenKeyboardPage.GetScreenSize();

            xNumberTextBoxScreenHeight.SetNumber((int)size.Height);
            xNumberTextBoxScreenWidth.SetNumber((int)size.Width);
        }


        private void UpdatePreviewButton()
        {
            ScreenKey key = ExtractNewKey();

            if (mLastStates.Count >= 2)
            {
                xBtnUndo.IsEnabled = true;
            }

            //기존의 키와 겹치기 않기 위해
            key.tag = -1;

            ScreenKeyboard.ScreenButton button = new ScreenKeyboard.ScreenButton(key);

            button.GetButtonElement().Margin = new Thickness(0);
            button.HorizontalAlignment = HorizontalAlignment.Center;
            button.VerticalAlignment = VerticalAlignment.Center;
            button.OnScreenKeyClicked += OnPreviewButtonClicked;


            xGridButtonPreview.Children.Clear();
            xGridButtonPreview.Children.Add(button);

        }

        private void OnPreviewButtonClicked(object sender, ScreenKeyboard.ScreenButtonEventArgs e)
        {
            ScreenKey thisKey = ExtractNewKey();

            if (mClickedKey != null)
            {
                thisKey.IsTogle = mClickedKey.IsTogle;
            }

            if (thisKey.IsTogle)
            {
                if (mPreviewKeyWasToggled)
                {
                    //button.Style = mScreenKeyboardPage.MakeButtonStyle(thisKey, false);
                    e.ScreenButton.SetButtonStyle(thisKey, false);
                    mPreviewKeyWasToggled = false;
                }
                else
                {
                    e.ScreenButton.SetButtonStyle(thisKey, true);
                    mPreviewKeyWasToggled = true;
                }
            }
        }

        private void SetOptionsWIthGivenKey(ScreenKey key, bool isUndoing = false)
        {
            xTextBoxBtnHeight.SetNumber((int)key.Size.Height);
            xTextBoxBtnWidth.SetNumber((int)key.Size.Width);
            xTextBoxBtnX.SetNumber((int)key.Location.X);
            xTextBoxBtnY.SetNumber((int)key.Location.Y);
            xTextBoxBtnLable.Text = key.Label;

            mButtonColorSelectors[0].SetColor(key.ButtonColor);
            mFontColorSelectors[0].SetColor(key.FontColor);

            mButtonColorSelectors[1].SetColor(key.ButtonHoverColor);
            mFontColorSelectors[1].SetColor(key.FontHoverColor);

            mButtonColorSelectors[2].SetColor(key.ButtonPressedColor);
            mFontColorSelectors[2].SetColor(key.FontPressedColor);

            if (isUndoing)
            {
                if (mClickedKey != null)
                {
                    xLableHotkeyInfoCurrent.Content = SetHotkeyLabel(mClickedKey);
                }
                else
                {
                    xLableHotkeyInfoCurrent.Content = "Current : None";
                }

                xLableHotkeyInfoNew.Content = SetHotkeyLabel(key, "New");
            }
            else
            {
                xLableHotkeyInfoCurrent.Content = SetHotkeyLabel(key);
                xLableHotkeyInfoNew.Content = "New : None";
            }


            UpdatePreviewButton();
        }

        private string SetHotkeyLabel(ScreenKey key, string startWith = "Current")
        {
            string output = startWith;

            if (key.Action != null)
            {
                output += " : " + key.Action.ToString();

                if (key.IsTogle)
                {
                    output += "(Togle)";
                }
            }
            else
            {
                output += " : None";
            }

            return output;
        }

        private void OnKeySelected(object sender, ScreenKey hotkey)
        {
            if(!mIsAdding && mClickedKey==null)
            {
                return;
            }

            xLableHotkeyInfoNew.Content = "New : " + hotkey.Action.ToString();

            if (hotkey.IsTogle)
            {
                xLableHotkeyInfoNew.Content += "(Togle)";

                //즉 최초로 togle이라고 고른 순간
                if (mHotkeyForNewScreenKey == null)
                {
                    mButtonColorSelectors[0].SetColor(ScreenKey.DefaultTogleKeyColor);

                    UpdatePreviewButton();
                }
            }

            mHotkeyForNewScreenKey = hotkey;
            SaveCurrentState();
        }

        private void OnScreenKeyClicked(object sender, ScreenKey key)
        {
            mIsAdding = false;
            mClickedKey = key;

            xStackButtonOptions.Visibility = Visibility.Visible;
            xBtnDeleteKey.Visibility = Visibility.Visible;
            mPreviewKeyWasToggled = false;

            SetOptionsWIthGivenKey(key);

            mLastStates.Clear();

            InitStateStack();
        }

        private void OnColorSelected(object sender, int selectorNum)
        {
            UpdatePreviewButton();

            SaveCurrentState();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ScreenKeyboardManager.EndEditing(mCurrentKeyboard);
        }

        private void OnOkClicked(object sender, RoutedEventArgs e)
        {
            ScreenKey newKey = ExtractNewKey();

            if (newKey == null)
            {
                return;
            }

            if (mIsAdding)
            {
                if (mHotkeyForNewScreenKey == null)
                {
                    MessageBox.Show("기능을 골라주세요!");
                    return;
                }

                ScreenKeyboardManager.AddScreenKey(mCurrentKeyboard, newKey);
            }
            else
            {
                if (newKey.Action == null)
                {
                    newKey.IsTogle = mClickedKey.IsTogle;
                    newKey.Action = mClickedKey.Action;
                }

                newKey.tag = mClickedKey.tag;

                ScreenKeyboardManager.EditScreenKey(mCurrentKeyboard, mClickedKey.tag, newKey);

            }

            xLableHotkeyInfoNew.Content = "New : None";


            xStackButtonOptions.Visibility = Visibility.Hidden;
            xBtnDeleteKey.Visibility = Visibility.Collapsed;

            MessageBox.Show("Success!");

            ResetParameters();

            mScreenKeyboardPage.UpdateView();
        }

        private ScreenKey ExtractNewKey()
        {
            ScreenKey newKey = new ScreenKey();

            newKey.Size = new Size(xTextBoxBtnWidth.ReadNumber(), xTextBoxBtnHeight.ReadNumber());
            newKey.Location = new Point(xTextBoxBtnX.ReadNumber(), xTextBoxBtnY.ReadNumber());

            if (mHotkeyForNewScreenKey != null)
            {
                newKey.Action = mHotkeyForNewScreenKey.Action;

                if (mHotkeyForNewScreenKey.IsTogle)
                {
                    newKey.ButtonColor = ScreenKey.DefaultTogleKeyColor;
                    newKey.IsTogle = true;
                }
                else
                {
                    newKey.ButtonColor = ScreenKey.DefaultColor;
                    newKey.IsTogle = false;
                }
            }

            newKey.Label = xTextBoxBtnLable.Text;

            AssignColorToKey(ref newKey);

            return newKey;
        }

        private void AssignColorToKey(ref ScreenKey key)
        {
            key.ButtonColor = mButtonColorSelectors[0].SelectedColor;
            key.FontColor = mFontColorSelectors[0].SelectedColor;

            key.ButtonHoverColor = mButtonColorSelectors[1].SelectedColor;
            key.FontHoverColor = mFontColorSelectors[1].SelectedColor;

            key.ButtonPressedColor = mButtonColorSelectors[2].SelectedColor;
            key.FontPressedColor = mFontColorSelectors[2].SelectedColor;
        }

        private void OnResizeClicked(object sender, RoutedEventArgs e)
        {
            int width = xNumberTextBoxScreenWidth.ReadNumber();
            int heigth = xNumberTextBoxScreenHeight.ReadNumber();

            mScreenKeyboardPage.ResizeScreen(width, heigth);
        }

        private void OnAddNewKeyClicked(object sender, RoutedEventArgs e)
        {
            xStackButtonOptions.Visibility = Visibility.Visible;
            xBtnDeleteKey.Visibility = Visibility.Collapsed;
            mIsAdding = true;
            mClickedKey = null;
            mPreviewKeyWasToggled = false;

            //새 키로 설정하니까 새로운 키 객체를 생성함
            SetOptionsWIthGivenKey(new ScreenKey());

            InitStateStack();
        }

        private void OnCancleClicked(object sender, RoutedEventArgs e)
        {
            xStackButtonOptions.Visibility = Visibility.Hidden;

            ResetParameters();
        }

        private void OnDeleteClicked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Sure to Delete this Key?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            ScreenKeyboardManager.DeleteScreenKey(mCurrentKeyboard, mClickedKey.tag);

            xStackButtonOptions.Visibility = Visibility.Hidden;

            ResetParameters();
            mScreenKeyboardPage.UpdateView();
        }

        private void ResetParameters()
        {
            mClickedKey = null;
            mIsAdding = false;
            mPreviewKeyWasToggled = false;
            mHotkeyForNewScreenKey = null;
            xBtnUndo.IsEnabled = false;
            mLastStates.Clear();
        }

        private void OnNumberChanged(object sender, EventArgs e)
        {
            UpdatePreviewButton();
            SaveCurrentState();
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePreviewButton();
            SaveCurrentState();
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            ScreenKeyboardManager.SaveKeyboardInternal(mCurrentKeyboard);
            MessageBox.Show("SAVED");
        }

        private void OnUndoClicked(object sender, RoutedEventArgs e)
        {
            //맨 위에 있는 건 현재 상태이므로
            if (!mIsSequencialUndoing)
            {
                mLastStates.Pop();
            }

            if(mLastStates.Peek().Action == null)
            {
                mHotkeyForNewScreenKey = null;
            }

            if (mLastStates.Count >= 2)
            {               
                SetOptionsWIthGivenKey(mLastStates.Pop(), true);
            }
            else
            {
                SetOptionsWIthGivenKey(mLastStates.Peek(), true);
                xBtnUndo.IsEnabled = false;
            }

            mIsSequencialUndoing = true;
        }

        private void InitStateStack()
        {
            mLastStates.Clear();
            xBtnUndo.IsEnabled = false;
            SaveCurrentState();
        }

        private void SaveCurrentState()
        {
            ScreenKey key = ExtractNewKey();

            mLastStates.Push(key);

            if (mLastStates.Count >= 2)
            {

                xBtnUndo.IsEnabled = true;
            }

            mIsSequencialUndoing = false;
        }
    }
}
