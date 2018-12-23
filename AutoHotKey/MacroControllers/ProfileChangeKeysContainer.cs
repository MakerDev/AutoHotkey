using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHotKey.MacroControllers
{
    class ProfileChangeKeysContainer
    {
        const int VK_F1 = 112;

        private int MAX_PROFILE = 10;
        private int[,] mProfileChangeKeys;



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
        }

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
            if(from >= MAX_PROFILE || from < 0 || to >= MAX_PROFILE || to < 0)
            {
                Debug.Assert(false, "인덱스를 벗어남");
            }

            return mProfileChangeKeys[from, to];
        }
    }
}
