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
        public int Key { get; private set; } //이벤트 대상 키
        public int ToDo { get; private set; } //이벤트 대상 키

        public bool IsUp { get; private set; } //만약 KeyUp이면 True, Down이면 false

        public KeyEventArgs(int key, int todo, bool isUp)
        {
            Key = key;
            ToDo = todo;
            IsUp = isUp;
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
        private static HwndSource mSource;

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
        public event EventHandler<HotKeyEventArgs> OnHotKeyEvent;


        private LowLevelKeyboardProc mProc;
        private IntPtr mHookID = IntPtr.Zero;

        //Z=Shift처럼 특수키로 매핑되어있어 입력을 무시해야 하는 키들
        //이 리스트에 있는 키들이 눌리면 이벤트를 발생시키고 입력을 무시함
        //구현은 리스트에 없으면 바로 훅 함수를 정상적으로 리턴해버리는 식으로 구현해도 될 듯.
        private Dictionary<int, int> mKeyEventPairs = new Dictionary<int, int>();
        private Dictionary<int, bool> mIsPressedAlready = new Dictionary<int, bool>(); //이 키가 최초로 눌러진건지 판정하기 위함.


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
        public void AddNewKey(int newKey, int toDo)
        {
            mKeyEventPairs.Add(newKey, toDo);
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
                //TODO : 반복횟수를 읽어 최초 발생시에만 이벤트를 보내기
                int vkCode = Marshal.ReadInt32(lParam);


                if (!mKeyEventPairs.ContainsKey(vkCode))
                {
                    Debug.WriteLine("코드 : " + vkCode.ToString() + "가 리스트에 없다");

                    return CallNextHookEx(mHookID, nCode, wParam, lParam);
                }

                //이미 한번 눌러졌으면 이벤트 발생X
                if(mIsPressedAlready[vkCode])
                {
                    return new IntPtr(5);
                }

                if (OnKeyEvent != null) { OnKeyEvent(this, new KeyEventArgs(vkCode, mKeyEventPairs[vkCode], false)); }

                mIsPressedAlready[vkCode] = true;

                //0이 아닌 값을 리턴하면 입력을 중간에서 없애버릴 수 있다.
                return new IntPtr(5);
            }
            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                //여기서 등록되지 않은 키일 경우를 처리했으므로 아래에서는 무조건 등록된 키에 대한 처리만 하면 됨.
                //따라서 아래부터는 0이 아닌 값을 리턴해 버려야함.
                if (!mKeyEventPairs.ContainsKey(vkCode))
                    return CallNextHookEx(mHookID, nCode, wParam, lParam);

                if (OnKeyEvent != null) { OnKeyEvent(this, new KeyEventArgs(vkCode, mKeyEventPairs[vkCode], true)); }

                Debug.WriteLine("그냥 리턴)");
                mIsPressedAlready[vkCode] = false;

                //0이 아닌 값을 리턴하면 입력을 중간에서 없애버릴 수 있다.
                return new IntPtr(5);
            }

            return CallNextHookEx(mHookID, nCode, wParam, lParam);

        }
    }
}
