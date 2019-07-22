using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoHotKey.UserInterfaces.ScreenKeyboard
{
    public partial class ColorSelector : System.Windows.Controls.UserControl
    {
        public event EventHandler<int> OnColorSelected;

        public Color SelectedColor
        {
            get; private set;
        } 

        private ColorDialog mColorDialog = null;

        public ColorSelector()
        {
            InitializeComponent();
        }

        public void SetColor(Color intialColor)
        {
            SelectedColor = intialColor;
            UpdateView();
        }

        private void OnOtherClicked(object sender, RoutedEventArgs e)
        {
            mColorDialog = new ColorDialog();

            if (mColorDialog.ShowDialog() == DialogResult.OK)
            {
                SelectedColor = new SolidColorBrush(Color.FromArgb(mColorDialog.Color.A, mColorDialog.Color.R, mColorDialog.Color.G, mColorDialog.Color.B)).Color;

                UpdateView();

                mColorDialog.Dispose();
                mColorDialog = null;

                OnColorSelected(this, -1);
            }
        }

        private void UpdateView()
        {
            Rect_Color.Fill = new SolidColorBrush(Color.FromArgb(SelectedColor.A, SelectedColor.R, SelectedColor.G, SelectedColor.B));
            Label_ColorPIcked.Content = SelectedColor.ToString();
        }
    }
}
