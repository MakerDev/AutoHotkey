using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        //만약 key==0이라면 Z=Shift처럼 특수키 매핑인 것으로 간주함. 
        public int Key { get;  set; }    //윈도우 virtualKeyCode값
        public int Modifier { get;  set; } //EModifiers 값

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

            //마우스 이벤트일 경우
            if(Key >= 1 && Key <= 4)
            {
                return GetMouseEventExplanation(Key, Modifier);
            }

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

        // : 왜 해시코드도 같이 오버라이드 하며, 이런식으로 오버라이드 가능한 지 공부
        public override int GetHashCode()
        {
            return Key ^ Modifier;
        }

        public static string GetMouseEventExplanation(int button, int mouseEvent)
        {
            string explanation = "";

            if (button == 1) { explanation = "LeftMouseButton"; }
            if (button == 2) { explanation = "RightMouseButton"; }
            if (button == 4) { explanation = "MiddleMouseButton"; }

            if (mouseEvent == 0) { explanation += "\nClick"; }
            if (mouseEvent == 1) { explanation += "\nDouble Click"; }
            if (mouseEvent == 2) { explanation += "\nDown"; }

            return explanation;
        }

    }
}
