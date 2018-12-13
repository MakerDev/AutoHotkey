using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoHotKey.MacroControllers
{
    public static class EModifiers
    {
        public const int NoMod = 0x0000;
        public const int Alt = 0x0001;
        public const int Ctrl = 0x0002;
        public const int Shift = 0x0004;
        public const int Win = 0x0008;
    }

    [Serializable()]
    public class HotkeyInfo
    {
        public int Key { get; private set; }    //윈도우 virtualKeyCode값
        public int Modifier { get; private set; } //EModifiers 값

        public HotkeyInfo(IntPtr lParam)
        {
            var lpInt = (int)lParam;
            Key = ((lpInt >> 16) & 0xFFFF);
            Modifier = (lpInt & 0xFFFF);
        }

        public HotkeyInfo(int key, int modifier)
        {
            Key = key;
            Modifier = modifier;
        }


        public override string ToString()
        {
            string info = "";

            if ((Modifier & EModifiers.Ctrl) == EModifiers.Ctrl)
            {
                info += "Ctrl + ";
            }
            if ((Modifier & EModifiers.Alt) == EModifiers.Alt)
            {
                info += "Alt + ";
            }
            if ((Modifier & EModifiers.Shift) == EModifiers.Shift)
            {
                info += "Shift + ";
            }
            if ((Modifier & EModifiers.Win) == EModifiers.Win)
            {
                info += "Win + ";
            }

            info += KeyInterop.KeyFromVirtualKey(Key).ToString();

            return info;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is HotkeyInfo))
                return false;

            HotkeyInfo info = obj as HotkeyInfo;
            
            if (Key == info.Key && Modifier == info.Modifier)
                return true;
            else
                return false;         
        }

        //TODO : 왜 해시코드도 같이 오버라이드 하며, 이런식으로 오버라이드 가능한 지 공부
        public override int GetHashCode()
        {
            return Key ^ Modifier;
        }
    }
}
