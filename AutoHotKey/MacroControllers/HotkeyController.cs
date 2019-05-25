using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WindowsInput;
using WindowsInput.Native;
using AutoHotKey.MacroControllers;
using System.Diagnostics;
using System.Runtime.Serialization.Json;

namespace AutoHotKey.MacroControllers
{
    class ProfileChangedCallBackArgs : EventArgs
    {
        public int profile = 0;

        public ProfileChangedCallBackArgs(int newProfile)
        {
            profile = newProfile;
        }
    }


    public class HotKeyController
    {
        public event EventHandler ProfileChanged;

        public const int START_PROFILE = 1;  //맨 처음 스타트 할 프로필은 1번
        public static int MAX_NUM_OF_PROFILES = 10;
        //public int NumOfProfiles { get; private set; }

        public const int HOTKEY_ID = 9000;

        private MainWindow mHelper = null;
        private HwndSource _source;

        private WindowsHookController mHookController = null;

        //메인 윈도우와 매크로 세팅 윈도우 모두에서 관여해야 하므로 싱글턴으로 제작
        private static HotKeyController mInstance = null;
        public static HotKeyController Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = new HotKeyController();

                }
                return mInstance;
            }
        }

        private int mProfileOnEdit = 0; //현재 편집중인 프로필. 아무것도 편집중이지 않으면 0;
        private int mProfileActive = 0; //현재 핫 키로 동작중인 프로필, 아무것도 동작중이지 않으면 0;
        private bool mIsHotkeyRegisterd = false; //현재 핫 키나 변경키가 등록되었는지 여부
        private bool mIsHotkeyActivated = false; //프로필 전환키로 프로필을 실행시킬 수 있는 상태인지 여부
        private bool mIsSettingProfileChangeKeys = false; //프로필 변경 키를 수정 중인 지 여부

        private List<HotKeyProfile> mProfiles = new List<HotKeyProfile>();
        private ProfileChangeKeysContainer mProfileChangeKeyContainer;

        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(
           [In] IntPtr hWnd,
           [In] int id,
           [In] uint fsModifiers,
           [In] uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);


        private HotKeyController()
        {
            LoadProfilesInternal();

            mProfileChangeKeyContainer = new ProfileChangeKeysContainer(MAX_NUM_OF_PROFILES);
        }

        //반드시 핫 키 등록 전 메인 윈도우에서 호출해야함. 핸들을 얻기 위해서임.
        public void RegisterHelper(MainWindow window)
        {
            mHookController = new WindowsHookController(window);

            mHookController.OnKeyEvent += OnSpecialKeyEvent;
            mHookController.OnSwitchEvent += OnSwitchingKeyEvent;

            mHelper = window;

            RegisterProfileChangingHotkeyInternal();

            //스타트 프로필은 없다.
            //RegisterHotkeyInternal(START_PROFILE);
        }

        //반드시 프로필 편집 전에 호출되어야 함. 어떤 프로필을 편집할 건지 알려주는 역할.
        public int StartEditProfile(int profile)
        {
            if (mProfiles.Count < profile)
            {
                MessageBox.Show("Invalid Access 0");

                return -1;
            }

            mProfileOnEdit = profile;

            UnRegisterHotKeyInternal();

            return profile; //만약 편집하려는 프로필이 없다면 -1을 반환. 있으면 profile을 그대로 반환
        }

        //TODO : 동시에 두 개의 프로필 편집 불가능 하게 바꾸기. =>아니면 프로필 단위로만 관리를 
        //프로필 편집 윈도우가 닫힐 때 자동으로 실행
        public void EndEditting()
        {
            //TODO : 프로필 닫을 때 굳이 그 프로필로 변경할 필요가 있는지 다시 생각
            //mProfileActive = mProfileOnEdit;
            RegisterHotkeyInternal(mProfileActive);

            SaveAllProfilesInternal();
            mProfileOnEdit = 0;
        }


        public bool IsEdittingProfile()
        {
            if (mProfileOnEdit == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void StartSetProfileChangingKeys()
        {
            //완전히 모든 핫 키를 해제
            UnRegisterHotKeyInternal();
            mIsSettingProfileChangeKeys = true;
        }

        public bool IsSettingProfileChangingKeys()
        {
            return mIsSettingProfileChangeKeys;
        }

        public void EndSetProfileChangingKeys()
        {
            if (mProfileActive == -1)
            {
                RegisterProfileChangingHotkeyInternal();
            }
            else
            {
                RegisterHotkeyInternal(mProfileActive);
            }

            mIsSettingProfileChangeKeys = false;
            //편집하는 동안 변경된 상태를 원상복구 하기 위해 다시 한 번 로드한다.
            mProfileChangeKeyContainer.LoadProfileChangeKeys();
        }


        public void AddNewProfile()
        {
            HotKeyProfile profile = new HotKeyProfile();
            mProfiles.Add(profile);

            SaveProfilesInternal(mProfiles.Count);
        }

        public int GetNumOfProfiles()
        {
            return mProfiles.Count;
        }

        public int GetProfileChangeKeyFromIndex(int from, int to)
        {
            return mProfileChangeKeyContainer.GetProfileChangeKeyFromIndex(from, to);
        }

        /* 문제가 없으면 0
         * 프로필키와 중복이 생기면 1
         * 입력은 인덱스가 아니다
         * key로 -1이 들어오면 기본 체인지키로 바꾸겠다는 말임.
         */
        public int SetProfileChangeKey(int from, int to, int key)
        {
            if (key == -1)
            {
                mProfileChangeKeyContainer.SetToDefaultByIndex(from - 1, to - 1);

                return 0;
            }

            if (IsHaveSameKeyInProfile(from, key) || IsHaveSameKeyInChangeKey(from, key))
            {
                return 1;
            }

            mProfileChangeKeyContainer.SetChangeKeyByIndex(from - 1, to - 1, key);

            return 0;
        }

        //0이면 성공
        //11이면 1번 프로필안의 어떤 핫 키와 겹침
        //22면 2번 프로필의 체인지키들과 겹침.
        public int SetEscapeKey(int key)
        {
            for (int i = 0; i < mProfiles.Count; i++)
            {
                if (IsHaveSameKeyInProfile(i + 1, key))
                {
                    return (10 + i + 1);
                }
                else if (IsHaveSameKeyInChangeKey(i + 1, key))
                {
                    return (20 + i + 1);
                }
            }

            mProfileChangeKeyContainer.SetEscapeKey(key);

            return 0;
        }

        public int GetEscapeKey()
        {
            return mProfileChangeKeyContainer.GetEscapeKey();
        }

        //중복이 있으면 기본값으로 바꿔주는 역할. 리턴 값은 수정된 변경 키 개수
        //allToDefault = true 이면 모든 체인지키를 기본으로
        private int CheckAndSetProfileChangeKey(bool allToDefault, int profileNum)
        {
            int count = 0;

            for (int i = 0; i < MAX_NUM_OF_PROFILES; i++)
            {
                if (allToDefault)
                {
                    mProfileChangeKeyContainer.SetToDefaultByIndex(profileNum - 1, i);
                }
                else if (IsHaveSameKeyInProfile(profileNum, mProfileChangeKeyContainer.GetProfileChangeKeyFromIndex(profileNum - 1, i)))
                {
                    mProfileChangeKeyContainer.SetToDefaultByIndex(profileNum - 1, i);
                    count++;
                }
            }

            return count;
        }

        //체인지 키가 프로필 내의 핫 키와 중복되는 지 검사
        private bool IsHaveSameKeyInProfile(int profileNum, int key)
        {
            List<HotkeyPair> hotkeyList = mProfiles[profileNum - 1].GetHotkeyList();

            for (int i = 0; i < hotkeyList.Count; i++)
            {
                if (hotkeyList[i].Trigger.Key == key)
                {
                    return true;
                }
            }

            return false;
        }

        //핫 키가 프로필 체인키와 중복되는 지 검사.
        private bool IsHaveSameKeyInChangeKey(int profileNum, int key)
        {
            if (key == mProfileChangeKeyContainer.GetEscapeKey())
            {
                return true;
            }

            for (int i = 0; i < MAX_NUM_OF_PROFILES; i++)
            {
                if (key == mProfileChangeKeyContainer.GetProfileChangeKeyFromIndex(profileNum - 1, i))
                {
                    return true;
                }
            }

            return false;
        }

        //Delete나 Add 이벤트 마다 호출 되어야 함.
        private void SaveAllProfilesInternal()
        {
            for (int i = 0; i < mProfiles.Count; i++)
            {
                SaveProfilesInternal(i + 1);
            }
        }

        private void DeleteAllProfileSaveFiles()
        {
            string path;

            for (int i = 1; i <= mProfiles.Count; i++)
            {
                path = Environment.CurrentDirectory + "/" + "profile" + i.ToString() + ".json";
                File.Delete(path);
            }
        }

        //Delete나 Add 이벤트 마다 호출 되어야 함.
        private void SaveProfilesInternal(int profileNum)
        {
            string name = "profile" + profileNum.ToString();
            mProfiles.ElementAt(profileNum - 1).SaveProfile(name);
        }

        private int LoadProfilesInternal()
        {
            int count = 1;

            if (mProfiles.Count != 0)
                return 0;

            while (true)
            {
                string name = "profile" + count.ToString();

                HotKeyProfile newProfile = new HotKeyProfile();

                if (newProfile.LoadProfile(name))
                {
                    mProfiles.Add(newProfile);
                }
                else
                {
                    break;
                }


                count++;
            }

            return count;
        }

        //TODO : 여기서 핫키와 체인지 키의 중복문제를 검사하도록 해야함.
        public void DeleteProfile(int currentProfile)
        {
            if (mProfileActive == currentProfile)
            {
                MessageBox.Show("This profile is being activated. Change to other profile to delete this.");
                return;
            }

            DeleteAllProfileSaveFiles();
            mProfiles.RemoveAt(currentProfile - 1);
            SaveAllProfilesInternal();

            if (currentProfile < mProfileActive)
            {
                ChangeActiveProfileInternal(mProfileActive - 1);
            }

            for (int i = currentProfile; i <= MAX_NUM_OF_PROFILES; i++)
            {
                if (i <= mProfiles.Count)
                {
                    CheckAndSetProfileChangeKey(false, i);
                }
                else
                {
                    CheckAndSetProfileChangeKey(true, i);
                }
            }

            //가장 마지막 프로필로 이동하는 키는 삭제
            //ex 4개의 프로필이 있을 때 어떤 프로필을 삭제하면 3개의 프로필이 남게되니까
            //4번 프로필로 이동하는 체인지키들은 전부 디폴트키로 재설정한다.

            for (int i = 0; i < MAX_NUM_OF_PROFILES; i++)
            {
                mProfileChangeKeyContainer.SetToDefaultByIndex(i, mProfiles.Count);
            }

            SaveProfileChangeKeys();

            //프로필 개수를 벗어난 체인지키 작동 방지 -> 예를 들어 4번 프로필을 지웠으면 F4를 인식하면 안됨.
            UnRegisterHotKeyInternal();
            RegisterHotkeyInternal(mProfileActive);
        }

        public List<HotkeyPair> GetHotkeyListOfCurrentProfile()
        {
            return mProfiles.ElementAt(mProfileOnEdit - 1).GetHotkeyList();
        }

        public List<HotkeyPair> GetHotkeyListOfProfile(int profileNum)
        {
            return mProfiles.ElementAt(profileNum - 1).GetHotkeyList();
        }


        public HotkeyPair GetHotKeyFromIndex(int index)
        {
            return mProfiles.ElementAt(mProfileOnEdit - 1).GetHotKeyFromIndex(index);
        }

        public bool AddNewHotkey(HotkeyPair hotkey)
        {
            int key = hotkey.Trigger.Key;

            if (IsHaveSameKeyInChangeKey(mProfileOnEdit, key))
            {
                MessageBox.Show("Same Key Exists in Profile-Changing keys");

                return false;
            }

            int result = mProfiles.ElementAt(mProfileOnEdit - 1).AddNewHotKey(hotkey);

            if (result == 2)
            {
                MessageBox.Show("Can't Add more Hotkey");

                return false;
            }
            else if (result == 1)
            {
                MessageBox.Show("Same hotkey already exists");
                return false;
            }
            else
            {
                MessageBox.Show("Success");
                return true;
            }
        }

        //input으로 핫키 번호를 받는다. -> 인덱스 아님.
        public void DeleteHotkey(int hotkeyNum)
        {
            int index = hotkeyNum - 1;

            mProfiles.ElementAt(mProfileOnEdit - 1).DeleteHotKey(index);
        }

        public void SetHotkeyExplanation(int hotkeyNum, string ex)
        {
            mProfiles.ElementAt(mProfileOnEdit - 1).GetHotKeyFromIndex(hotkeyNum - 1).Explanation = ex;
        }

        //프로필에 있는 핫 키를 윈도우에 등록한다. 참고로 이건 클래스이지 윈도우가 아니므로 기존의 this를 대체할 뭔가가 필요함.
        //TODO : 프로필 체인지 키와 일반키/조합키 모두 훅 컨트롤러로 보내 전체를 대상으로 연쇄를 관리하게 바꾼다.
        //아니면 이벤트 방식으로 처리는 이 클래스에서, 인식은 훅 클래스에서 할 수도 있다.
        private void RegisterHotkeyInternal(int profileNum)
        {
            RegisterProfileChangingHotkeyInternal();

            if (profileNum <= 0)
                return;


            var helper = new WindowInteropHelper(mHelper);

            //프로필 등록과 동시에

            //프로필이 없으면 등록하지 않는다.
            if (mProfiles.Count == 0)
            {
                return;
            }

            List<HotkeyPair> list = mProfiles.ElementAt(profileNum - 1).GetHotkeyList();

            //새로 핫 키들을 등록하는 것이므로 딕셔너리를 한 번 비워준다. -> 프로필 변경키를 위해 비우지 않는다. UnHookKeyboard에서 이미 비움.
            //mHookController.ClearKeys();

            foreach (var hotkey in list)
            {
                int vk = (hotkey.Trigger.Key);
                int mod = (hotkey.Trigger.Modifier);

                int keyToDo = hotkey.Action.Key;


                if (mod == 0)
                {
                    Debug.WriteLine("스페셜키 : " + vk.ToString());
                    HotkeyInfo info = new HotkeyInfo(hotkey.Action.Key, hotkey.Action.Modifier);
                    mHookController.AddNewKey(new HotkeyInfo(vk, mod), info);
                }
                else
                {
                    HotkeyInfo input = new HotkeyInfo(vk, mod);
                    HotkeyInfo info = new HotkeyInfo(hotkey.Action.Key, hotkey.Action.Modifier);
                    Debug.WriteLine("조합키 : " + input.ToString());
                    mHookController.AddNewKey(input, info);

                }
            }

            mIsHotkeyRegisterd = true;

            //중복훅 등록 문제가 있는 듯함. 이미 프로필 변경키를 등록하며 훅을 실행했으니 여기서는 키만 추가한다
            //mHookController.HookKeyboard();
        }

        //TODO : 나중에 후킹 및 핫 키 등록 등의 윈도우 메시지 관련 함수들을 따로 클래스를 만들어 정리하는 것을 고려한다.
        //반드시 프로필이 등록되었을 때만 사용될 수 있다.
        private void RegisterProfileChangingHotkeyInternal()
        {
            var helper = new WindowInteropHelper(mHelper);

            //변경키부터 새로 등록하는 것이니 한 번 초기화
            mHookController.ClearKeys();

            int escape = mProfileChangeKeyContainer.GetEscapeKey();

            mHookController.AddSwitchingKey(new HotkeyInfo(escape, 0));

            //핫 키 프로그램이 활성화 상태일 때만 체인지 키를 등록해야함.
            if (mIsHotkeyActivated)
            {
                for (int i = 0; i < mProfiles.Count; i++)
                {
                    int vk = mProfileChangeKeyContainer.GetProfileChangeKeyFromIndex(mProfileActive - 1, i);

                    mHookController.AddSwitchingKey(new HotkeyInfo(vk, 0));
                }
            }

            mIsHotkeyRegisterd = true;

            //만약 프로필변경키만 사용할 경우에 대비해 여기서도 훅을 실행한다
            mHookController.HookKeyboard();
        }

        public void UnRegisterHotKeyInternal()
        {
            //if (mProfileActive == 0)
            //    return;
            if (!mIsHotkeyRegisterd)
            {
                return;
            }

            //_source.RemoveHook(HwndHook);
            //_source = null;

            var helper = new WindowInteropHelper(mHelper);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);

            mIsHotkeyRegisterd = false;

            mHookController.UnHookKeyboard();
            mHookController.ClearKeys();
        }

        //스페셜키와 단일 키 이벤트 처리기
        //TODO : 이 콜백이벤트를 그냥 hookControlller로 이동하는 것도 고려
        private void OnSpecialKeyEvent(object sender, KeyEventArgs keyEventArgs)
        {
            HotkeyInfo input = keyEventArgs.In;

            int todoKey = keyEventArgs.Out.Key;
            int todoMod = keyEventArgs.Out.Modifier;

            if (keyEventArgs.ToUp != null)
            {
                todoKey = keyEventArgs.ToUp.Key;
                todoMod = keyEventArgs.ToUp.Modifier;
            }

            VirtualKeyCode todoKeyVirtualKey = (VirtualKeyCode)todoKey;

            InputSimulator inputSimulator = new InputSimulator();

            List<VirtualKeyCode> modifiers = new List<VirtualKeyCode>();

            if ((todoMod & EModifiers.Ctrl) != 0) { modifiers.Add(VirtualKeyCode.CONTROL); }
            if ((todoMod & EModifiers.Shift) != 0) { modifiers.Add(VirtualKeyCode.SHIFT); }
            if ((todoMod & EModifiers.Alt) != 0) { modifiers.Add(VirtualKeyCode.MENU); }
            if ((todoMod & EModifiers.Win) != 0) { modifiers.Add(VirtualKeyCode.LWIN); }


            //TODO : 키 다운이나 업을 할 때 잠시 훅을 껐다켜는 방법으로 중복이나 연쇄문제를 해결 가능한지 테스트 -> 가능해보임
            //어느쪽을 눌렀는지 구분 필요

            mHookController.UnHookKeyboard();

            //KeyDown인 경우
            if (!keyEventArgs.IsUp)
            {
                Debug.WriteLine("키 다운");


                if (modifiers.Count != 0)
                {
                    foreach (var todoModifier in modifiers)
                    {
                        inputSimulator.Keyboard.KeyDown(todoModifier);
                    }
                }
                if (todoKey != 0)
                {
                    inputSimulator.Keyboard.KeyDown(todoKeyVirtualKey);
                }
            }
            else
            {
                Debug.WriteLine("키 업");

                if (todoKey != 0)
                {
                    inputSimulator.Keyboard.KeyUp(todoKeyVirtualKey);
                }
                if (modifiers.Count != 0)
                {
                    foreach (var todoModifier in modifiers)
                    {
                        inputSimulator.Keyboard.KeyUp(todoModifier);
                    }
                }
            }

            mHookController.HookKeyboard();
        }

        private void ChangeActiveProfileInternal(int toProfile)
        {
            mProfileActive = toProfile;

            //TODO : ProfileChanged가 null이 아니라면 호출하는 코드임. 공부 필요
            ProfileChanged?.Invoke(this, new ProfileChangedCallBackArgs(mProfileActive));
        }

        //입력키를 전부 단일 키로 설정할 시 이 함수는 체인지키와 탈출키만을 처리한다.
        private void OnSwitchingKeyEvent(object sender, HotkeyInfo trigger)
        {
            Debug.WriteLine("체인지 발동");

            //ESCAPE인 경우 탈충
            if (trigger.Modifier == 0 && trigger.Key == mProfileChangeKeyContainer.GetEscapeKey())
            {
                //체인지 키만 남기고 해제
                mIsHotkeyActivated = !mIsHotkeyActivated;
                UnRegisterHotKeyInternal();

                //만약 Deactivated 였으면 Profile1으로 간다.
                if (mIsHotkeyActivated)
                {
                    ChangeActiveProfileInternal(1);
                    RegisterHotkeyInternal(mProfileActive);
                }
                else //만약 원래 activated였으면 Deactivated로 바꾼다.
                {
                    RegisterProfileChangingHotkeyInternal();
                    ChangeActiveProfileInternal(-1);
                }

                return;
            }

            for (int i = 0; i < mProfiles.Count; i++)
            {
                if (trigger.Key == mProfileChangeKeyContainer.GetProfileChangeKeyFromIndex(mProfileActive - 1, i))
                {
                    UnRegisterHotKeyInternal();
                    ChangeActiveProfileInternal(i + 1);
                    RegisterHotkeyInternal(mProfileActive);

                    return;
                }
            }

        }

        public void SaveProfileChangeKeys()
        {
            mProfileChangeKeyContainer.SaveProfileChangeKeys();
        }
    }
}
