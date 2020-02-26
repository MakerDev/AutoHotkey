using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WindowsInput.Native;

namespace AutoHotKey.MacroControllers
{
    [Serializable()]
    class HotKeyProfile
    {
        public static int MAXHOTKEY = 60;

        private List<HotkeyPair> hotkeyList = new List<HotkeyPair>();
        public HotkeyInfo ChangeKey { get; private set; }  //프로필 체인지 키

        //0은 성공 1은 중복 2는 오버플로우 3은 체인지키와 겹침
        public int AddNewHotKey(HotkeyPair info)
        {
            if (hotkeyList.Count >= MAXHOTKEY)
            {
                return 2;
            }
            else if (IsSameHotkeyExists(info.Trigger))
            {
                return 1;
            }

            hotkeyList.Add(info);

            return 0;
        }

        public HotkeyPair GetHotKeyFromIndex(int index)
        {
            if (index >= hotkeyList.Count)
            {
                return null;
            }

            return hotkeyList.ElementAt(index);
        }


        //파일을 저장할 때 반드시 기존 내용을 지워야 함. => 기존파일 삭제하고 새로 만들기
        public bool SaveProfile(string name)
        {
            //직렬화를 이용해 hotkeyList를 저장한다.
            string path = Environment.CurrentDirectory + "/SaveFiles/" + name + ".json";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            FileStream filestream = File.OpenWrite(path);

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<HotkeyPair>));
            serializer.WriteObject(filestream, hotkeyList);

            filestream.Close();

            return true;
        }

        public bool LoadProfile(string name)
        {
            string path = Environment.CurrentDirectory + "/SaveFiles/" + name + ".json";

            if (File.Exists(path))
            {
                FileStream filestream = File.OpenRead(path);

                try
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<HotkeyPair>));
                    hotkeyList = serializer.ReadObject(filestream) as List<HotkeyPair>;
                }
                catch (System.Runtime.Serialization.SerializationException)
                {

                    filestream.Seek(0, SeekOrigin.Begin);  //다시 읽으려고 파일스트림 위치 초기화
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<HotkeyPair_legacy>));
                    List<HotkeyPair_legacy> hotkeyList_legacy = serializer.ReadObject(filestream) as List<HotkeyPair_legacy>;

                    ConvertLegacyDataIntoNewDataForm(hotkeyList_legacy);
                }


                filestream.Close();

                return true; 
            }
            else
            {
                return false;
            }
        }

        public void DeleteHotKey(int index)
        {
            hotkeyList.RemoveAt(index);
        }

        public bool IsSameHotkeyExists(HotkeyInfo trriger)
        {
            foreach (var info in hotkeyList)
            {
                if (info.Trigger.Equals(trriger))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetChangeKey(HotkeyInfo newChangeKey)
        {

        }

        private void ConvertLegacyDataIntoNewDataForm(List<HotkeyPair_legacy> hotkeyList_Legacies)
        {
            int length = hotkeyList_Legacies.Count;

            for(int i=0; i<length; i++)
            {
                HotkeyPair pair = new HotkeyPair(hotkeyList_Legacies[i].Trigger, hotkeyList_Legacies[i].Action);
                pair.Explanation = hotkeyList_Legacies[i].Explanation;

                hotkeyList.Add(pair);
            }

        }

        internal List<HotkeyPair> GetHotkeyList()
        {
            return hotkeyList;
        }
    }
}
