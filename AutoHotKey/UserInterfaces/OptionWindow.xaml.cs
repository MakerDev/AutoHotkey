using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
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

namespace AutoHotKey.UserInterfaces
{

    public class OptionData
    {
        public bool IsSaveInformationWindowStartingPosition { get; set; } = true;
        public bool IsMinimizeMainWindowOnStart { get;  set; } = false;
        public int WhichSettingWindow { get; set; } = 0; //0이면 그림있는거, 1이면 리스트방식
    }

    public partial class OptionWindow : Window
    {
        public static OptionData Options = new OptionData();
        private MainWindow mWindow;

        public OptionWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            LoadOptionData();
            Closing += OnClosing;

            mWindow = mainWindow;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mWindow.OnOptionWindowCloseing();
        }

        private void OnCheckBoxStateChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            Options.IsSaveInformationWindowStartingPosition = xCheckSavePosition.IsChecked.Value;
            Options.IsMinimizeMainWindowOnStart = xCheckMinizeMainWindow.IsChecked.Value;
        }

        private void OnBtnSaveClicked(object sender, RoutedEventArgs e)
        {
            //직렬화를 이용해 hotkeyList를 저장한다.
            string path = Environment.CurrentDirectory + "/Options"  + ".json";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            FileStream filestream = File.OpenWrite(path);

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(OptionData));
            serializer.WriteObject(filestream, Options);

            filestream.Close();

            MessageBox.Show("Saved");

            Close();
        }

        private void LoadOptionData()
        {
            string path = Environment.CurrentDirectory + "/Options" + ".json";

            if (File.Exists(path))
            {
                FileStream filestream = File.OpenRead(path);

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(OptionData));
                OptionData data = serializer.ReadObject(filestream) as OptionData;

                xCheckMinizeMainWindow.IsChecked = Options.IsMinimizeMainWindowOnStart =  data.IsMinimizeMainWindowOnStart;
                xCheckSavePosition.IsChecked = Options.IsSaveInformationWindowStartingPosition = data.IsSaveInformationWindowStartingPosition;
                Options.WhichSettingWindow = data.WhichSettingWindow;

                if(Options.WhichSettingWindow == 0)
                {
                    xRadioGrapic.IsChecked = true;
                    xRadioList.IsChecked = false;
                }
                else
                {
                    xRadioGrapic.IsChecked = false;
                    xRadioList.IsChecked = true;
                }

                filestream.Close();
            }
        }

        private void OnRadioChecked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;

            Options.WhichSettingWindow = Convert.ToInt32(radioButton.Tag.ToString());
        }
    }
}
