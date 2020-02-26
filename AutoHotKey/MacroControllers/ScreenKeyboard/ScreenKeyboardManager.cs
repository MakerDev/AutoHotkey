using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AutoHotKey.MacroControllers.ScreenKeyboard;

namespace AutoHotKey.MacroControllers.ScreenKeyboard
{
    static class ScreenKeyboardManager
    {
        public static int MAX_NUM_OF_KEYBOARD = 10;

        public static bool IsEditing { get; private set; } = false;
        public static int NumOfKeayboardsOn { get; private set; } = 0;

        private static UserInterfaces.ScreenKeyboard.ScreenKeyboardWindow[] mScreenKeyboardWindows
            = new UserInterfaces.ScreenKeyboard.ScreenKeyboardWindow[MAX_NUM_OF_KEYBOARD];

        private static List<ScreenKeyboard> mScreenKeyboards = new List<ScreenKeyboard>();

        static ScreenKeyboardManager()
        {
            LoadAllKeyboardsInternal();
        }


        public static int GetNumberOfScreenKeyboards()
        {
            return mScreenKeyboards.Count;
        }

        public static void OpenScreenKeyboard(int keyboardNum)
        {
            NumOfKeayboardsOn++;
            UserInterfaces.ScreenKeyboard.ScreenKeyboardWindow screenKeyboard = new AutoHotKey.UserInterfaces.ScreenKeyboard.ScreenKeyboardWindow(keyboardNum);

            mScreenKeyboardWindows[keyboardNum - 1] = screenKeyboard;
            screenKeyboard.Show();

            HotKeyController.Instance.OnScreenKeyboardOpen();
        }

        public static void CloseScreenKeyboard(int keyboardNum)
        {
            if (mScreenKeyboardWindows[keyboardNum - 1] != null)
            {
                NumOfKeayboardsOn--;
                mScreenKeyboardWindows[keyboardNum - 1].Close();
                mScreenKeyboardWindows[keyboardNum - 1] = null;
            }

            if(NumOfKeayboardsOn <= 0)
            {
                HotKeyController.Instance.OnAllScreenKeyboardClose();
            }
        }

        public static void CloseAllWindows()
        {
            for (int i = 1; i <= MAX_NUM_OF_KEYBOARD; i++)
            {
                CloseScreenKeyboard(i);
            }

            NumOfKeayboardsOn = 0;
        }


        public static void AddScreenKeyboard()
        {
            ScreenKeyboard newKeyboard = new ScreenKeyboard();
            mScreenKeyboards.Add(newKeyboard);

            SaveKeyboardInternal(mScreenKeyboards.Count);
        }

        public static void DeleteScreenKeyboard(int keyboardNum)
        {
            DeleteAllSaveFilesInternal();
            mScreenKeyboards.RemoveAt(keyboardNum - 1);
            SaveAllKeyboardsInternal();
        }

        public static void AddScreenKey(int keyboardNum, ScreenKey newKey)
        {
            newKey.tag = 0;

            if (mScreenKeyboards[keyboardNum - 1].GetDictScreenKeys().Count > 0)
            {
                newKey.tag = mScreenKeyboards[keyboardNum - 1].GetDictScreenKeys().Keys.Max() + 1;
            }

            mScreenKeyboards[keyboardNum - 1].AddNewKey(newKey);
        }

        public static void EditScreenKey(int keyboardNum, int tag, ScreenKey newKey)
        {
            mScreenKeyboards[keyboardNum - 1].EditKey(tag, newKey);
        }

        public static void DeleteScreenKey(int keyboardNum, int tag)
        {
            mScreenKeyboards[keyboardNum - 1].DeleteKey(tag);
        }

        public static void StartEditing()
        {
            IsEditing = true;
        }

        public static void EndEditing(int keyboardNum, bool isSavedRightBeforeClosing = false)
        {
            IsEditing = false;

            if(!isSavedRightBeforeClosing)
            {
                if (MessageBox.Show("Do you want to save?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
            }

            SaveAllKeyboardsInternal();
        }

        public static ScreenKeyboard GetScreenKeyboard(int keyboardNum)
        {
            return mScreenKeyboards[keyboardNum - 1];
        }

        public static void SaveKeyboardInternal(int keyboardNum)
        {
            string path = Environment.CurrentDirectory + "/SaveFiles/ScreenKeyboard" + keyboardNum.ToString() + ".json";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            FileStream filestream = File.OpenWrite(path);

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ScreenKeyboard));
            serializer.WriteObject(filestream, mScreenKeyboards[keyboardNum - 1]);

            filestream.Close();
        }

        private static void DeleteAllSaveFilesInternal()
        {
            string path;

            for (int i = 1; i <= mScreenKeyboards.Count; i++)
            {
                path = Environment.CurrentDirectory + "/SaveFiles/ScreenKeyboard" + i.ToString() + ".json";
                File.Delete(path);
            }
        }

        private static void SaveAllKeyboardsInternal()
        {
            for (int i = 1; i <= mScreenKeyboards.Count; i++)
            {
                SaveKeyboardInternal(i);
            }
        }

        //KeyboardNum은 1부터 시작임

        private static void LoadAllKeyboardsInternal()
        {
            int i = 1;

            while (LoadKeyboardInternal(i))
            {
                i++;
            }
        }

        private static bool LoadKeyboardInternal(int keyboardNum)
        {
            string path = Environment.CurrentDirectory + "/SaveFiles/ScreenKeyboard" + keyboardNum.ToString() + ".json";

            if (File.Exists(path))
            {
                FileStream filestream = File.OpenRead(path);

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ScreenKeyboard));

                ScreenKeyboard keyboard = serializer.ReadObject(filestream) as ScreenKeyboard;

                mScreenKeyboards.Add(keyboard);

                filestream.Close();

                return true;
            }

            return false;
        }
    }
}
