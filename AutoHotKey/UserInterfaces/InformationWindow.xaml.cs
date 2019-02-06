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
using AutoHotKey.MacroControllers;

namespace AutoHotKey.UserInterfaces
{
    /// <summary>
    /// InformationWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InformationWindow : Window
    {
        public InformationWindow()
        {
            InitializeComponent();

            this.Loaded += OnInformationWindowLoaded;
            this.Closing += OninformationWindowClosing;
            this.ShowInTaskbar = false;
        }

        private void OnInformationWindowLoaded(object sender, RoutedEventArgs e)
        {
            if(OptionWindow.Options.IsSaveInformationWindowStartingPosition)
            {
                LoadPositionData();
            }
        }

        private void OninformationWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            List<double> positions = new List<double>();
            positions.Add(Left);
            positions.Add(Top);


            SavePositionData(positions);
        }

        public bool SavePositionData(List<double> positions)
        {
            //TODO : 포인트를 기반으로 현재 해상도와 비율을 구해서 절대 위치가 아닌
            //비율에 따른 상대위치로 저장하도록 하기

            string path = Environment.CurrentDirectory + "/" + "StartingData" + ".json";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            FileStream filestream = File.OpenWrite(path);

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<double>));
            serializer.WriteObject(filestream, positions);

            filestream.Close();

            return true;
        }

        public bool LoadPositionData()
        {
            string path = Environment.CurrentDirectory + "/" + "StartingData" + ".json";

            if (File.Exists(path) && OptionWindow.Options.IsSaveInformationWindowStartingPosition)
            {
                FileStream filestream = File.OpenRead(path);

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<double>));
                List<double> positions = serializer.ReadObject(filestream) as List<double>;

                Left = positions[0];
                Top = positions[1];

                filestream.Close();

                return true;
            }
            else
            {
                return false;
            }
        }


        //profileNum==-1 이면 비활성화 상태라는 뜻.
        public void ChangeCurrentProfile(int profileNum)
        {
            if (profileNum == 0)
            {
                xLabelCurrentProfile.Content = "Activated";
            }
            else if (profileNum == -1)
            {
                xLabelCurrentProfile.Content = "Deactivated";

            }
            else
            {
                xLabelCurrentProfile.Content = "Profile " + profileNum.ToString();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
