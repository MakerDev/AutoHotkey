using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace AutoHotKey.MacroControllers
{
    //일반적인 핫키의 경우 RegisterHotkey로 구현한다. 예컨대 x=Control + Z같은 것
    //하지만 Z=Shift 같은 특수 매핑키는 윈도우 훅으로 구현한다.

    //훅 이벤트 발생히 이벤트로 넘겨줄 인자.
    public class KeyEventArgs
    {
        public int Key { get; private set; } //이벤트 발생시킨 키
        public HotkeyInfo eventinfo { get; private set; } //이벤트 내용;

        public bool IsUp { get; private set; } //만약 KeyUp이면 True, Down이면 false

        public KeyEventArgs(int key, HotkeyInfo info, bool isUp)
        {
            Key = key;
            IsUp = isUp;
            eventinfo = info;
        }
    }

    //핫키 이벤트 발생히 이벤트로 넘겨줄 인자.
    public class HotKeyEventArgs
    {
        public int key { get; private set; } //이벤트 대상 키
        public bool isUp { get; private set; } //만약 KeyUp이면 True, Down이면 false

        public HotKeyEventArgs(int key, bool isUp)
        {
            this.key = key;
            this.isUp = isUp;
        }
    }

    //Static 클래스로 구성?
    class WindowsHookController
    {
        public const int HOTKEY_ID = 9000;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;

        private static MainWindow mHelper = null;

        //TODO : 추후에 훅 이벤트도 이 클래스로 리펙토링 하기
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

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public event EventHandler<KeyEventArgs> OnKeyEvent;


        private LowLevelKeyboardProc mProc;
        private IntPtr mHookID = IntPtr.Zero;

        //Z=Shift처럼 특수키로 매핑되어있어 입력을 무시해야 하는 키들
        //이 리스트에 있는 키들이 눌리면 이벤트를 발생시키고 입력을 무시함
        //구현은 리스트에 없으면 바로 훅 함수를 정상적으로 리턴해버리는 식으로 구현해도 될 듯.
        private Dictionary<int, HotkeyInfo> mKeyEventPairs = new Dictionary<int, HotkeyInfo>();
        private Dictionary<int, bool> mIsPressedAlready = new Dictionary<int, bool>(); //이 키가 최초로 눌러진건지 판정하기 위함.

        //가장 마지막에 눌리거나 UP된 핫 키
        //입력된 키가 핫 키 리스트에 있지만 가장 마지막에 출력된 핫키에 의한 것이라면, 핫 키 이벤트를 발생시키지 않고 그냥 정상적으로 넘긴다.
        private HotkeyInfo mLastHotkeyDown;
        private HotkeyInfo mLastHotkeyUp;

        public WindowsHookController(MainWindow helper)
        {
            mHelper = helper;
            mProc = HookCallback;
        }

        //후킹 스타트
        public void HookKeyboard()
        {
            mHookID = SetHook(mProc);
        }

        public void UnHookKeyboard()
        {
            UnhookWindowsHookEx(mHookID);
            ClearKeys();
        }

        //입력키와 출력키 입력
        public void AddNewKey(int newKey, HotkeyInfo info)
        {
            mKeyEventPairs.Add(newKey, info);
            mIsPressedAlready.Add(newKey, false);
        }

        public void ClearKeys()
        {
            mKeyEventPairs.Clear();
            mIsPressedAlready.Clear();
        }


        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        //키 다운이 최초로 발생했을 때 쉬프트 다운을 발생시키고
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (mKeyEventPairs.Count == 0)
                return CallNextHookEx(mHookID, nCode, wParam, lParam);

            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                //리스트에 없는 키는 무시 + 만약 최근의 출력으로 인해 이벤트가 발동된 것이라면 무시
                //입력은 한 번만 발생하니까 
                if (!mKeyEventPairs.ContainsKey(vkCode))
                {
                    return CallNextHookEx(mHookID, nCode, wParam, lParam);
                }

                if (mLastHotkeyDown != null)
                {
                    if (mLastHotkeyDown.Key == vkCode)
                    {
                        mLastHotkeyDown.Key = -1;
                    }
                    else if (mLastHotkeyDown.Modifier == vkCode)
                    {
                        mLastHotkeyDown.Modifier = -1;
                    }

                    if (mKeyEventPairs.ContainsKey(mLastHotkeyDown.Key) && mKeyEventPairs.ContainsKey(mLastHotkeyDown.Modifier))
                    {
                        return CallNextHookEx(mHookID, nCode, wParam, lParam);
                    }
                    else
                    {
                        if (mLastHotkeyDown.Key == -1 || mLastHotkeyDown.Modifier == -1)
                        {
                            mLastHotkeyDown = null;

                            return CallNextHookEx(mHookID, nCode, wParam, lParam);
                        }
                    }
                }




                //이미 한번 눌러졌으면 이벤트 발생X, 그냥 입력 무시 단, z=shift같은 특수키에만 해당.
                if (mIsPressedAlready[vkCode] && mKeyEventPairs[vkCode].Key==0)
                {
                    return new IntPtr(5);
                }

                //처음 발동 될 때 가장 최근 아웃풋을 설정해놈.
                if (OnKeyEvent != null)
                {
                    mLastHotkeyDown = new HotkeyInfo(mKeyEventPairs[vkCode].Key, mKeyEventPairs[vkCode].Modifier);
                    Debug.WriteLine("현재 아웃풋" + mLastHotkeyDown.ToString());
                    OnKeyEvent(this, new KeyEventArgs(vkCode, mKeyEventPairs[vkCode], false));
                }

                //Shift같은 특수키의 keydown이벤트가 여러변 입력되는 걸 막기 위함.
                mIsPressedAlready[vkCode] = true;

                //0이 아닌 값을 리턴하면 입력을 중간에서 없애버릴 수 있다.
                return new IntPtr(5);
            }
            else if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                //여기서 등록되지 않은 키일 경우를 처리했으므로 아래에서는 무조건 등록된 키에 대한 처리만 하면 됨.
                //따라서 아래부터는 0이 아닌 값을 리턴해 버려야함.
                if (!mKeyEventPairs.ContainsKey(vkCode))
                    return CallNextHookEx(mHookID, nCode, wParam, lParam);

                if (mLastHotkeyUp != null)
                {
                    if (mLastHotkeyUp.Key == vkCode)
                    {
                        mLastHotkeyUp.Key = -1;
                    }
                    else if (mLastHotkeyUp.Modifier == vkCode)
                    {
                        mLastHotkeyUp.Modifier = -1;
                    }

                    if (mKeyEventPairs.ContainsKey(mLastHotkeyUp.Key) && mKeyEventPairs.ContainsKey(mLastHotkeyUp.Modifier))
                    {
                        return CallNextHookEx(mHookID, nCode, wParam, lParam);
                    }
                    else
                    {
                        if(mLastHotkeyUp.Key == -1 || mLastHotkeyUp.Modifier == -1)
                        {
                            mLastHotkeyUp = null;

                            return CallNextHookEx(mHookID, nCode, wParam, lParam);
                        }
                    }
                }


                //만약 f=ctrl + z로 설정하면 f, ctrl과 z에서 각각 keyup 이벤트가 발생하여 keyup이벤트가 세 번 발생한다.
                //
                if (OnKeyEvent != null)
                {
                    //기존데이터의 변질을 막기 위해.
                    mLastHotkeyUp = new HotkeyInfo(mKeyEventPairs[vkCode].Key, mKeyEventPairs[vkCode].Modifier);

                    OnKeyEvent(this, new KeyEventArgs(vkCode, mKeyEventPairs[vkCode], true));
                    Debug.WriteLine("업 이벤트 발생" + vkCode.ToString());
                    //mLastOut = null;    //키를 땔 때는 최근 아웃풋을 초기화함.
                }

                mIsPressedAlready[vkCode] = false;

                //0이 아닌 값을 리턴하면 입력을 중간에서 없애버릴 수 있다.
                return new IntPtr(5);
            }

            return CallNextHookEx(mHookID, nCode, wParam, lParam);

        }

    }
}
