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

namespace AutoHotKey.MacroControllers
{
    public class HotKeyController
    {
        public const int START_PROFILE = 1;  //맨 처음 스타트 할 프로필은 1번
        public static int MAX_NUM_OF_PROFILES = 10;
        //public int NumOfProfiles { get; private set; }

        public const int HOTKEY_ID = 9000;

        private MainWindow mHelper = null;
        private HwndSource _source;

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

        private List<HotKeyProfile> mProfiles = new List<HotKeyProfile>();
        //private bool mIsHotkeyRegistered = false;

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
        }

        //반드시 핫 키 등록 전 메인 윈도우에서 호출해야함. 핸들을 얻기 위해서임.
        public void RegisterHelper(MainWindow window)
        {
            mHelper = window;
            RegisterHotkeyInternal(START_PROFILE);
        }

        //반드시 프로필 편집 전에 호출되어야 함. 어떤 프로필을 편집할 건지 알려주는 역할.
        public int StartEditProfile(int profile)
        {
            if (mProfiles.Count < profile)
            {
                MessageBox.Show("잘못된 접근입니다");
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
            mProfileActive = mProfileOnEdit;
            RegisterHotkeyInternal(mProfileActive);
            SaveAllProfilesInternal();
            mProfileOnEdit = 0;
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

        public List<HotkeyPair> GetHotkeyListOfCurrentProfile()
        {
            return mProfiles.ElementAt(mProfileOnEdit - 1).GetHotkeyList();
        }

        public void DeleteProfile(int currentProfile)
        {
            if (mProfileActive == currentProfile)
            {
                MessageBox.Show("현재 프로필이 핫키로 동작중입니다. 다른 프로필로 변경후 삭제하십시오");
                return;
            
            }

            DeleteAllProfileSaveFiles();
            mProfiles.RemoveAt(currentProfile - 1);
            SaveAllProfilesInternal();

            //프로필 개수를 벗어난 체인지키 작동 방지 -> 예를 들어 4번 프로필을 지웠으면 F4를 인식하면 안됨.
            UnRegisterHotKeyInternal();
            RegisterHotkeyInternal(mProfileActive);
        }

        public HotkeyPair GetHotKeyFromIndex(int index)
        {
            return mProfiles.ElementAt(mProfileOnEdit - 1).GetHotKeyFromIndex(index);
        }

        public bool AddNewHotkey(HotkeyPair hotkey)
        {
            int result = mProfiles.ElementAt(mProfileOnEdit - 1).AddNewHotKey(hotkey);

            if (result == 2)
            {
                MessageBox.Show("더 이상 핫 키를 추가할 수 없습니다.");

                return false;
            }
            else if (result == 1)
            {
                MessageBox.Show("이미 같은 핫키가 존재합니다.");
                return false;
            }
            else
            {
                MessageBox.Show("추가 성공");
                return true;
            }
        }

        //input으로 핫키 번호를 받는다. -> 인덱스 아님.
        public void DeleteHotkey(int hotkeyNum)
        {
            int index = hotkeyNum - 1;

            mProfiles.ElementAt(mProfileOnEdit - 1).DeleteHotKey(index);
        }

        //프로필에 있는 핫 키를 윈도우에 등록한다. 참고로 이건 클래스이지 윈도우가 아니므로 기존의 this를 대체할 뭔가가 필요함.
        private void RegisterHotkeyInternal(int profileNum)
        {
            var helper = new WindowInteropHelper(mHelper);

            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);


            List<HotkeyPair> list = mProfiles.ElementAt(profileNum - 1).GetHotkeyList();

            foreach (var hotkey in list)
            {
                uint vk = (uint)(hotkey.Trigger.Key);
                uint mod = (uint)(hotkey.Trigger.Modifier);


                if (!RegisterHotKey(helper.Handle, HOTKEY_ID, mod, vk))
                {
                    // handle error
                    MessageBox.Show("핫 키 등록 실패 : " + vk.ToString() + "  " + mod.ToString());
                    return;
                }
            }

            //프로필 등록과 동시에
            RegisterProfileChangingHotkeyInternal();

            //RegisterProfileChangingHotkeyInternal();

            //mIsHotkeyRegistered = true;

            MessageBox.Show("등록 완료" + profileNum.ToString());
        }

        //TODO : 나중에 F1키들 말고 자유설정으로 바꿀 수 있게 해주기. + 체인지키 목록을 관리하며 핫 키와 중복없도록 하기.
        private void RegisterProfileChangingHotkeyInternal()
        {
            var helper = new WindowInteropHelper(mHelper);

            //TODO : 메인 위도우가 꺼지면 helper가 무효화 되는 문제 해결하기
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);

            for (uint i = 0; i < mProfiles.Count; i++)
            {
                //112는 F1이다.
                uint vk = 112 + i;

                if (!RegisterHotKey(helper.Handle, HOTKEY_ID, 0, vk))
                {
                    // handle error
                    MessageBox.Show("체인지 키 등록 실패");
                    return;
                }
            }

            //ESC등록
            if (!RegisterHotKey(helper.Handle, HOTKEY_ID, 0, 27))
            {
                // handle error
                MessageBox.Show("체인지 키 등록 실패");
                return;
            }

            MessageBox.Show("체인지키 등록 완료");
        }

        public void UnRegisterHotKeyInternal()
        {

            _source.RemoveHook(HwndHook);
            _source = null;

            var helper = new WindowInteropHelper(mHelper);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);

            MessageBox.Show("등록해제");
        }


        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            MessageBox.Show("발동");
                            HotkeyInfo hotkeyClicked = new HotkeyInfo(lParam);
                            ActHotkeyEvent(hotkeyClicked);

                            handled = true;
                            break;
                    }
                    break;

                default:
                    break;
            }

            return IntPtr.Zero;
        }

        //현재 동작중인 프로필을 입력받고 적절한 Action을 반환함. 만약에 action이 null일 경우 esc를 눌러 핫 키를 해제한 것 
        private void ActHotkeyEvent(HotkeyInfo trigger)
        {
            //ESC인 경우 탈충
            if (trigger.Modifier == 0 && trigger.Key == KeyInterop.VirtualKeyFromKey(Key.Escape))
            {
                //체인지 키만 남기고 해제
                UnRegisterHotKeyInternal();
                RegisterProfileChangingHotkeyInternal();
                mProfileActive = 0;
                MessageBox.Show("탈출");

                return;
            }

            //TODO : 체인지키 변경 가능하게 하면 바꾸기
            if(trigger.Modifier == 0 && (trigger.Key>=112 && trigger.Key < 112 + mProfiles.Count))
            {
                mProfileActive = trigger.Key - 111;

                UnRegisterHotKeyInternal();

                RegisterHotkeyInternal(mProfileActive);

                return;

            }

            //만약 ESC나 체인지키가 아니라면 핫키라는 말임.
            
            List<HotkeyPair> list = mProfiles.ElementAt(mProfileActive - 1).GetHotkeyList();

            HotkeyInfo todo = null;

            foreach (var hotkey in list)
            {
                if (hotkey.Trigger.Equals(trigger))
                {
                    todo = hotkey.Action;
                }
            }

            InputSimulator inputSimulator = new InputSimulator();
            List<VirtualKeyCode> modifiers = new List<VirtualKeyCode>();

            if ((todo.Modifier & EModifiers.Ctrl) != 0) { modifiers.Add(VirtualKeyCode.CONTROL); }
            if ((todo.Modifier & EModifiers.Alt) != 0) { modifiers.Add(VirtualKeyCode.MENU); }
            if ((todo.Modifier & EModifiers.Win) != 0) { modifiers.Add(VirtualKeyCode.LWIN); }
            if ((todo.Modifier & EModifiers.Shift) != 0) { modifiers.Add(VirtualKeyCode.SHIFT); }

            if (modifiers.Count == 0)
            {
                inputSimulator.Keyboard.KeyPress((VirtualKeyCode)todo.Key);
            }
            else
            {
                inputSimulator.Keyboard.ModifiedKeyStroke(modifiers, (VirtualKeyCode)todo.Key);
            }
        }
    }
}
