using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHotKey.MacroControllers
{
    public class HoykeyController
    {
        public static int MAX_NUM_OF_PROFILES = 10;


        //메인 윈도우와 매크로 세팅 윈도우 모두에서 관여해야 하므로 싱글턴으로 제작
        private static HoykeyController mInstance = null;
        public static HoykeyController Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = new HoykeyController();

                }
                return mInstance;
            }           
        }
        
        public int NumOfProfiles { get; private set;}
        private int mProfileOnEdit = 0; //현재 편집중인 프로필. 아무것도 편집중이지 않으면 0;
        private HotKeyProfile[] mProfiles = new HotKeyProfile[MAX_NUM_OF_PROFILES];

        public HoykeyController()
        {
            LoadProfilesInternal();
        }

        //반드시 프로필 편집 전에 호출되어야 함. 어떤 프로필을 편집할 건지 알려주는 역할.
        public int StartEditProfile(int profile)
        {
            if (NumOfProfiles > profile)
                return -1;

            mProfileOnEdit = profile;

            return profile; //만약 편집하려는 프로필이 없다면 -1을 반환. 있으면 profile을 그대로 반환
        }

        //TODO : 동시에 두 개의 프로필 편집 불가능 하게 바꾸기. =>아니면 
        //프로필 편집 윈도우가 닫힐 때 자동으로 실행
        public void EndEditting()
        {
            mProfileOnEdit = 0;
        }

        //TODO:프로필 저장-자동으로 이루어 질 듯 
        public bool SaveNewProfile()
        {
            return true;
        }

        public bool MakeNewProfile()
        {
            return true;
        }

        //로드한 프로필 개수 반환
        private int LoadProfilesInternal()
        {
            return 0;
            //TODO : 저장된 프로필 모두 로드
        }
    }
}
