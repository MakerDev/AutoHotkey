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
using AutoHotKey.MacroControllers.ScreenKeyboard;

namespace AutoHotKey.UserInterfaces.ScreenKeyboard
{
    
    public class ScreenButtonEventArgs
    {
        public ScreenButton ScreenButton;
        public Button Button;

        public ScreenButtonEventArgs(ScreenButton screenButton, Button button)
        {
            this.ScreenButton = screenButton;
            this.Button = button;
        }

    }


    public partial class ScreenButton : UserControl
    {
        public event EventHandler<ScreenButtonEventArgs> OnScreenKeyClicked;

        public ScreenButton(ScreenKey key)
        {
            InitializeComponent();

            xButton.Content = key.Label;
            xButton.Tag = key.tag.ToString();
            xButton.Width = key.Size.Width;
            xButton.Height = key.Size.Height;

            xButton.HorizontalAlignment = HorizontalAlignment.Left;
            xButton.VerticalAlignment = VerticalAlignment.Top;
            xButton.Margin = new Thickness(key.Location.X, key.Location.Y, 0, 0);
            xButton.Focusable = false;

            SetButtonStyle(key);
        }

        public void SetButtonStyle(ScreenKey key, bool isForTogle = false)
        {
            Style style = new Style(typeof(Button));

            Trigger mouseOver = new Trigger();
            mouseOver.Property = Button.IsMouseOverProperty;
            mouseOver.Value = true;
            mouseOver.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(key.ButtonHoverColor)));
            mouseOver.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(key.FontHoverColor)));

            Trigger buttonPressed = new Trigger();
            buttonPressed.Property = Button.IsPressedProperty;
            buttonPressed.Value = true;
            buttonPressed.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(key.ButtonPressedColor)));
            buttonPressed.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(key.FontPressedColor)));

            Trigger mouseOut = new Trigger();
            Trigger buttonNotPressed = new Trigger();

            if (isForTogle)
            {
                mouseOut.Property = Button.IsMouseOverProperty;
                mouseOut.Value = false;
                mouseOut.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(key.ButtonPressedColor)));
                mouseOut.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(key.FontPressedColor)));

                buttonNotPressed.Property = Button.IsPressedProperty;
                buttonNotPressed.Value = false;
                buttonNotPressed.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(key.ButtonPressedColor)));
                buttonNotPressed.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(key.FontPressedColor)));
            }
            else
            {
                mouseOut.Property = Button.IsMouseOverProperty;
                mouseOut.Value = false;
                mouseOut.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(key.ButtonColor)));
                mouseOut.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(key.FontColor)));

                buttonNotPressed.Property = Button.IsPressedProperty;
                buttonNotPressed.Value = false;
                buttonNotPressed.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(key.ButtonColor)));
                buttonNotPressed.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(key.FontColor)));
            }

            style.Triggers.Add(mouseOver);
            style.Triggers.Add(mouseOut);
            style.Triggers.Add(buttonPressed);

            //나중에 trigger키에 한 해 hovereffect를 빼고 싶을때 사용
            //style.Triggers.Add(buttonNotPressed);

            xButton.Style = style;
        }

        public Button GetButtonElement()
        {
            return xButton;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OnScreenKeyClicked(this, new ScreenButtonEventArgs(this, xButton));
        }
    }
}
