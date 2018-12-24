using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;

namespace AutoHotKey.MacroControllers
{
    class ProfileChangeKeysContainer
    {
        const int VK_F1 = 112;
        const int VK_ESC = 27;

        private int MAX_PROFILE = 10;
        private int[,] mProfileChangeKeys;

        private int mEscapeKey;

        //기본적으로 이동키는 그 자리의 f# 키를 쓴다.
        /*  1   2   3   4   5
         *  -1  a   b   c   d   
         *   a  -1  b   c   d 
         *   a   b  -1  c   d
         *   a   b  c  -1   d
         *   a   b   c  d  -1
         * 
         * 행 = 현재 프로필, 열 = 다음 프로필
         */
        public ProfileChangeKeysContainer(int MAX_PROFILE)
        {
            mProfileChangeKeys = new int[MAX_PROFILE, MAX_PROFILE];

            this.MAX_PROFILE = MAX_PROFILE;

            if (!LoadProfileChangeKeys())
            {
                SetToDefaultTable();
            }
        }

        public int GetEscapeKey()
        {
            return mEscapeKey;
        }

        public void SetEscapeKey(int key)
        {
            mEscapeKey = key;
        }

        //키 중복등의 문제로 그 자리의 키를 기본전환키로 변경해야할 시
        public void SetToDefaultByIndex(int from, int to)
        {
            mProfileChangeKeys[from, to] = VK_F1 + to;
        }

        //TODO : 기존 핫 키와의 중복체크는 컨트롤러에서 했다고 가정함.
        public bool SetChangeKeyByIndex(int from, int to, int newKey)
        {
            if (from == to)
                return false;

            mProfileChangeKeys[from, to] = newKey;

            return true;
        }

        //인덱스로 받기
        public int GetProfileChangeKeyFromIndex(int from, int to)
        {
            if (from >= MAX_PROFILE || from < 0 || to >= MAX_PROFILE || to < 0)
            {
                Debug.Assert(false, "인덱스를 벗어남");
            }

            return mProfileChangeKeys[from, to];
        }

        public bool SaveProfileChangeKeys()
        {
            //직렬화를 이용해 hotkeyList를 저장한다.
            string path = Environment.CurrentDirectory + "/" + "ProfileChangeKeys" + ".json";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            List<int> keyList = new List<int>();

            for (int i = 0; i < MAX_PROFILE; i++)
            {
                for (int j = 0; j < MAX_PROFILE; j++)
                {
                    keyList.Add(mProfileChangeKeys[i, j]);
                }
            }

            keyList.Add(mEscapeKey);

            FileStream filestream = File.OpenWrite(path);

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<int>));
            serializer.WriteObject(filestream, keyList);

            filestream.Close();

            return true;
        }

        public bool LoadProfileChangeKeys()
        {
            //파일 존재 여부 반드시 체크

            string path = Environment.CurrentDirectory + "/" + "ProfileChangeKeys" + ".json";

            List<int> keyList = new List<int>();

            if (File.Exists(path))
            {
                FileStream filestream = File.OpenRead(path);

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<int>));
                keyList = serializer.ReadObject(filestream) as List<int>;

                filestream.Close();
            }
            else
            {
                return false;
            }

            for (int i = 0; i < MAX_PROFILE; i++)
            {
                for (int j = 0; j < MAX_PROFILE; j++)
                {
                    mProfileChangeKeys[i, j] = keyList[10 * i + j];
                }
            }

            int esc = MAX_PROFILE * MAX_PROFILE;

            mEscapeKey = keyList[esc];

            return true;
        }

        private void SetToDefaultTable()
        {
            for (int i = 0; i < MAX_PROFILE; i++)
            {
                for (int j = 0; j < MAX_PROFILE; j++)
                {
                    if (i == j)
                    {
                        mProfileChangeKeys[i, i] = -1;  //자기 자신으로의 키는 -1로 설정해 둠.
                    }
                    else
                    {
                        mProfileChangeKeys[i, j] = VK_F1 + j;
                    }
                }
            }

            mEscapeKey = VK_ESC;
        }

    }
}
