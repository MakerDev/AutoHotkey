using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using WindowsInput.Native;

namespace AutoHotKey.MacroControllers
{
    public enum EMouseEvents
    {
        Click = 1,
        DoubleClick,
        Down
    }


    //일반적인 핫키의 경우 RegisterHotkey로 구현한다. 예컨대 x=Control + Z같은 것
    //하지만 Z=Shift 같은 특수 매핑키는 윈도우 훅으로 구현한다.
    //훅 이벤트 발생히 이벤트로 넘겨줄 인자.
    public class KeyEventArgs
    {
        public HotkeyInfo In { get; private set; } //이벤트 발생시킨 키
        public HotkeyInfo Out { get; private set; } //이벤트 내용;

        public HotkeyInfo EndingKey { get; private set; }

        public bool IsUp { get; private set; } //만약 KeyUp이면 True, Down이면 false
        public HotkeyInfo ToUp { get; private set; }  //upKey는 조합키를 뗄 떼를 위한 키코드. -1이면 무시
        public KeyEventArgs(HotkeyInfo input, HotkeyInfo output, bool isUp,  HotkeyInfo endingKey, HotkeyInfo toUp = null)
        {
            In = input;
            IsUp = isUp;
            Out = output;
            ToUp = toUp;

            EndingKey = endingKey;
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

    //핫키 이벤트 발생히 이벤트로 넘겨줄 인자.
    public class MouseEventArgs
    {
        public int Button { get; private set; } //어느 버튼인지
        public EMouseEvents MouseEvent { get; private set; } //만약 KeyUp이면 True, Down이면 false
        public bool IsUp { get; private set; } //Down이벤트인지 UP이벤트인지
        public int Modifier;


        public MouseEventArgs(int button, EMouseEvents mouseEvent, bool isUp, int mod)
        {
            this.Button = button;
            this.MouseEvent = mouseEvent;
            this.IsUp = isUp;
            this.Modifier = mod;
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
        public event EventHandler<HotkeyInfo> OnSwitchEvent;  //프로필 체인지 키 이벤트


        private LowLevelKeyboardProc mProc;
        private IntPtr mHookID = IntPtr.Zero;

        //Z=Shift처럼 특수키로 매핑되어있어 입력을 무시해야 하는 키들
        //이 리스트에 있는 키들이 눌리면 이벤트를 발생시키고 입력을 무시함
        //구현은 리스트에 없으면 바로 훅 함수를 정상적으로 리턴해버리는 식으로 구현해도 될 듯.
        private Dictionary<HotkeyInfo, HotkeyPair> mKeyEventPairs = new Dictionary<HotkeyInfo, HotkeyPair>();
        private Dictionary<HotkeyInfo, bool> mIsPressedAlready = new Dictionary<HotkeyInfo, bool>(); //이 키가 최초로 눌러진건지 판정하기 위함.

        private Dictionary<int, bool> mModifiersState = new Dictionary<int, bool>();
        private int mModifier = 0;

        //가장 마지막에 눌리거나 UP된 핫 키
        //입력된 키가 핫 키 리스트에 있지만 가장 마지막에 출력된 핫키에 의한 것이라면, 핫 키 이벤트를 발생시키지 않고 그냥 정상적으로 넘긴다.
        private HotkeyInfo mLastHotkeyDown;
        private HotkeyInfo mLastHotkeyUp;

        private HotkeyInfo mLastInput;

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
            //ClearKeys();
        }

        //입력키와 출력키 입력
        public void AddNewKey(HotkeyInfo newKey, HotkeyPair info)
        {
            mKeyEventPairs.Add(newKey, info);
            mIsPressedAlready.Add(newKey, false);

            int mod = newKey.Modifier;

            if ((mod & EModifiers.Ctrl) != 0 && !mModifiersState.ContainsKey(EModifiers.Ctrl)) { mModifiersState.Add(EModifiers.Ctrl, false); }
            if ((mod & EModifiers.Shift) != 0) { if (mModifiersState.ContainsKey(EModifiers.Shift)) mModifiersState.Add(EModifiers.Shift, false); }
            if ((mod & EModifiers.Alt) != 0) { if (mModifiersState.ContainsKey(EModifiers.Alt)) mModifiersState.Add(EModifiers.Alt, false); }
            if ((mod & EModifiers.Win) != 0) { if (mModifiersState.ContainsKey(EModifiers.Win)) mModifiersState.Add(EModifiers.Win, false); }
        }

        //mKeyEventPair에서 info가 둘 다 -1이면 스위칭 키로 간주함
        public void AddSwitchingKey(HotkeyInfo trigger)
        {
            //TODO : 추후에 조합키를 입력키로 하는게 가능해 지면 HotkeyInfo전체를 쓰기
            //콜백이벤트의 인자로 전달되는 HotkeyInfo의 경우 key의 정보를 HotkeyInfo 형식으로 생성하여 넘김
            //mKeyEventPairs.Add(trigger, new HotkeyInfo(-2, -2));        

            mKeyEventPairs.Add(trigger, new HotkeyPair(null, new HotkeyInfo(-2, -2), null));
            mIsPressedAlready.Add(trigger, false);
        }

        public void ClearKeys()
        {
            mKeyEventPairs.Clear();
            mIsPressedAlready.Clear();
            mModifiersState.Clear();
            mLastHotkeyDown = null;
            mLastHotkeyUp = null;
            mModifier = 0;
        }

        //윈도우 훅으로 조합키를 감지한 경우에는 이쪽에서 이벤트가 중복감지되면 안됨.
        //ctrl a 와 a가 모두 등록되어 있을 경우, ctrl+a를 통해 발생한 a 이벤트는 무시해야 함.
        public void OnCombinationKeyPressed(HotkeyInfo info)
        {

        }


        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private int KeycodeToModifier(int keycode)
        {
            int code = 0;

            if (keycode == 164 || keycode == 165) { code = EModifiers.Alt; return code; }    //LEFT RIGHT ALT를 그냥 ALT로
            if (keycode == 162 || keycode == 163) { code = EModifiers.Ctrl; return code; }  //Control
            if (keycode == 91 || keycode == 92) { code = EModifiers.Win; return code; }
            if (keycode == 160 || keycode == 161) { code = EModifiers.Shift; return code; }

            return keycode;
        }

        //키 다운이 최초로 발생했을 때 쉬프트 다운을 발생시키고
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (mKeyEventPairs.Count == 0)
                return CallNextHookEx(mHookID, nCode, wParam, lParam);

            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                int modMaybe = KeycodeToModifier(vkCode);

                //만약 ctrl, alt등의 modifier키 중 하나이면
                //TODO : 조합키 사용시, ctrl, alt등의 키는 단일키 입력으로 매핑 못하게 하기
                //modifier키는 어느 쪽 키인지 구분이 필요하다
                if (mModifiersState.ContainsKey(modMaybe))
                {
                    //최초 1회만 더함
                    if (mModifiersState[modMaybe] == false)
                    {
                        mModifier += modMaybe;
                    }

                    mModifiersState[modMaybe] = true;

                    return CallNextHookEx(mHookID, nCode, wParam, lParam);
                }


                //리스트에 없는 키는 무시 + 만약 최근의 출력으로 인해 이벤트가 발동된 것이라면 무시
                //입력은 한 번만 발생하니까 
                if (!mKeyEventPairs.ContainsKey(new HotkeyInfo(vkCode, mModifier)))
                {
                    return CallNextHookEx(mHookID, nCode, wParam, lParam);
                }

                HotkeyInfo currentHotkey = new HotkeyInfo(vkCode, mModifier);

                int key = mKeyEventPairs[currentHotkey].Action.Key;
                int mod = mKeyEventPairs[currentHotkey].Action.Modifier;


                //이미 한번 눌러졌으면 이벤트 발생X, 그냥 입력 무시 단, z=shift와 마우스 이벤트 같은 특수키에만 해당
                //0은 shift 같은 키가 출력인 애들이고 1, 2, 4,는 마우스 이므로 1이상 4이하로 설정
                if (mIsPressedAlready[currentHotkey] && (key >= 1 && key <= 4))
                {
                    return new IntPtr(5);
                }

                if (OnKeyEvent != null)
                {
                    //스위칭 키인 경우
                    if (key == -2 && mod == -2)
                    {
                        Debug.WriteLine("스위칭 실행");
                        OnSwitchEvent(this, new HotkeyInfo(vkCode, 0));
                    }
                    else if (key >= 1 && key <= 4)
                    {

                        //modOut이 ctrl 이런거고 mod가 클릭인지 더블클릭인지
                        int mouseEventMod = mod / 100;
                        int modOut = mod - mouseEventMod * 100;

                        //mod = Convert.ToInt32(mod.ToString().Substring(0, 1));


                        //여기서 modifier는 반드시 1, 2, 3여야 한다.
                        OnMouseEventHookOccur(new MouseEventArgs(key, (EMouseEvents)mouseEventMod, false, modOut));
                    }
                    else
                    {
                        if (mModifier != 0)
                        {
                            mLastInput = new HotkeyInfo(currentHotkey.Key, currentHotkey.Modifier);
                        }
                        else
                        {
                            mLastHotkeyDown = new HotkeyInfo(key, mod);
                        }


                        OnKeyEvent(this, new KeyEventArgs(currentHotkey, mKeyEventPairs[currentHotkey].Action, false, mKeyEventPairs[currentHotkey].EndingAction));
                    }
                }

                //Shift같은 특수키의 keydown이벤트가 여러변 입력되는 걸 막기 위함.
                mIsPressedAlready[currentHotkey] = true;

                //0이 아닌 값을 리턴하면 입력을 중간에서 없애버릴 수 있다.
                return new IntPtr(5);
            }
            else if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                HotkeyInfo currentHotkey = new HotkeyInfo(vkCode, 0);

                vkCode = KeycodeToModifier(vkCode);

                //ctrl a 등에서 a를 먼저 뗀 경우에는
                //Modifier키는 그대로 down상태로 두고 
                //HotkeyInfo currentHotkey = new HotkeyInfo(vkCode, mModifier);

                //만약 ctrl, alt등의 modifier키 중 하나이면
                if (mModifiersState.ContainsKey(vkCode))
                {
                    if (mModifiersState[vkCode] == true)
                    {
                        mModifier -= vkCode;
                    }

                    mModifiersState[vkCode] = false;

                    //만약 뗴는 키가 마지막 인풋의 조합키 중 하나가 아닐 때만.
                    if (mLastInput != null && (mLastInput.Modifier & vkCode) == 0)
                    {
                        return CallNextHookEx(mHookID, nCode, wParam, lParam);
                    }
                }


                //여기서 등록되지 않은 키일 경우를 처리했으므로 아래에서는 무조건 등록된 키에 대한 처리만 하면 됨.
                //따라서 아래부터는 0이 아닌 값을 리턴해 버려야함.
                if (!mKeyEventPairs.ContainsKey(currentHotkey))
                    return CallNextHookEx(mHookID, nCode, wParam, lParam);

                if (OnKeyEvent != null)
                {
                    int key = mKeyEventPairs[currentHotkey].Action.Key;
                    int mod = mKeyEventPairs[currentHotkey].Action.Modifier;

                    //마우스는 입력으로 사용되지 않으므로 연쇄에 대한 걱정을 할 필요가 없다
                    if (key >= 1 && key <= 4)
                    {
                        //modOut이 ctrl 이런거고 mod가 클릭인지 더블클릭인지
                        int mouseEventMod = mod / 100;
                        int modOut = mod - mouseEventMod*100;
                        
                        OnMouseEventHookOccur(new MouseEventArgs(key, (EMouseEvents)mouseEventMod, true, modOut));
                    }
                    else
                    {
                        //조합키를 떼면
                        if (currentHotkey.Modifier != 0)
                        {
                            mLastInput = null;

                            HotkeyInfo toUp = new HotkeyInfo(key, mod - mModifier);

                            //기존데이터의 변질을 막기 위해.
                            mLastHotkeyUp = new HotkeyInfo(key, mod - mModifier);

                            OnKeyEvent(this, new KeyEventArgs(currentHotkey, mKeyEventPairs[currentHotkey].Action, true, mKeyEventPairs[currentHotkey].EndingAction, toUp));
                        }
                        else
                        {
                            //기존데이터의 변질을 막기 위해.
                            mLastHotkeyUp = new HotkeyInfo(key, mod);

                            OnKeyEvent(this, new KeyEventArgs(currentHotkey, mKeyEventPairs[currentHotkey].Action, true, mKeyEventPairs[currentHotkey].EndingAction));
                        }

                    }
                }

                mIsPressedAlready[currentHotkey] = false;

                //0이 아닌 값을 리턴하면 입력을 중간에서 없애버릴 수 있다.
                return new IntPtr(5);
            }

            return CallNextHookEx(mHookID, nCode, wParam, lParam);

        }





        private void OnMouseEventHookOccur(MouseEventArgs e)
        {


            List<VirtualKeyCode> modifiers = new List<VirtualKeyCode>();

            if ((e.Modifier & EModifiers.Ctrl) != 0) { modifiers.Add(VirtualKeyCode.CONTROL); }
            if ((e.Modifier & EModifiers.Shift) != 0) { modifiers.Add(VirtualKeyCode.SHIFT); }
            if ((e.Modifier & EModifiers.Alt) != 0) { modifiers.Add(VirtualKeyCode.MENU); }
            if ((e.Modifier & EModifiers.Win) != 0) { modifiers.Add(VirtualKeyCode.LWIN); }




            if (e.IsUp)
            {
                //Down으로 설정되지 않는 것들은 up이벤트에 반응할 필요가 없다.
                if (e.MouseEvent != EMouseEvents.Down)
                    return;

                WindowsInput.InputSimulator inputSimulator = new WindowsInput.InputSimulator();

                switch (e.Button)
                {
                    //Left
                    case 1:
                        inputSimulator.Mouse.LeftButtonUp();
                        break;

                    //Right
                    case 2:
                        inputSimulator.Mouse.RightButtonUp();
                        break;

                    //Middle
                    case 4:
                        inputSimulator.Mouse.MiddleButtonUp();
                        break;

                    default:
                        break;
                }

                UpModifiersInternal(modifiers);
       
            }
            else
            {
                WindowsInput.InputSimulator inputSimulator = new WindowsInput.InputSimulator();

                if (e.Modifier > 0)
                {
                    foreach (var todoMod in modifiers)
                    {                     
                        inputSimulator.Keyboard.KeyDown(todoMod);

                    }
                }

                EMouseEvents mouseEvent = e.MouseEvent;
                switch (e.MouseEvent)
                {
                    //Left
                    case EMouseEvents.Click:
                        if (e.Button == 1) { inputSimulator.Mouse.LeftButtonClick(); }
                        if (e.Button == 2) { inputSimulator.Mouse.RightButtonClick(); }
                        if (e.Button == 4) { inputSimulator.Mouse.MiddleButtonClick(); }

                        UpModifiersInternal(modifiers);

                        break;

                    //Right
                    case EMouseEvents.DoubleClick:
                        if (e.Button == 1) { inputSimulator.Mouse.LeftButtonDoubleClick(); }
                        if (e.Button == 2) { inputSimulator.Mouse.RightButtonDoubleClick(); }
                        if (e.Button == 4) { inputSimulator.Mouse.MiddleButtonDoubleClick(); }

                        UpModifiersInternal(modifiers);

                        break;

                    //Middle
                    case EMouseEvents.Down:
                        if (e.Button == 1) { inputSimulator.Mouse.LeftButtonDown(); }
                        if (e.Button == 2) { inputSimulator.Mouse.RightButtonDown(); }
                        if (e.Button == 4) { inputSimulator.Mouse.MiddleButtonDown(); }

                        break;


                    default:
                        break;
                }


            }


        }

        private void UpModifiersInternal(List<VirtualKeyCode> modifiers)
        {

            WindowsInput.InputSimulator inputSimulator = new WindowsInput.InputSimulator();

            foreach (var todoMod in modifiers)
            {
                inputSimulator.Keyboard.KeyUp(todoMod);
            }
        }

    }
}
