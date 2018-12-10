using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHotKey.MacroControllers
{
    [Serializable()]
    public class HotkeyPair
    {
        public HotkeyInfo Trigger
        {
            get; private set;
        }
        public HotkeyInfo Action
        {
            get; private set;
        }


        public HotkeyPair(HotkeyInfo trigger, HotkeyInfo action)
        {
            Trigger = trigger;
            Action = action;
        }
    }
}
