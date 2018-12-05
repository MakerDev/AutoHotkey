using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHotKey.MacroControllers
{
    class HotkeyInfo
    {
        public int Key { get; private set; }
        public int Modifier { get; private set; }

        private HotkeyInfo(IntPtr lParam)
        {
            var lpInt = (int)lParam;
            Key = ((lpInt >> 16) & 0xFFFF);
            Modifier = (lpInt & 0xFFFF);
        }

        public static HotkeyInfo GetHotkeyInfo(IntPtr lparam)
        {
            return new HotkeyInfo(lparam);
        }

        //TODO : toString()을 오버라이드 하여 현재 핫 키 조합을 문자열로 변환해 주기.
    }
}
