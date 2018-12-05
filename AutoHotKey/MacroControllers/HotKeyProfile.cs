using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHotKey.MacroControllers
{
    class HotKeyProfile
    {
        public static int MAXHOTKEY = 40;

        private HotkeyInfo[] hotkeyList = new HotkeyInfo[MAXHOTKEY];



        public bool AddNewHotKey(HotkeyInfo info)
        {
            if(hotkeyList.Length>=MAXHOTKEY)
                return false;

            //TODO : 핫 키 추가 처리

            return true;
        }

        public bool LoadProfile(int profileNum)
        {


            return false;
        }

    }
}
