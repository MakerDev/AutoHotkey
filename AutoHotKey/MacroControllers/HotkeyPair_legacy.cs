using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHotKey.MacroControllers
{
    [Serializable()]
    public class HotkeyPair_legacy
    {
        public string Explanation = "";

        public HotkeyInfo Trigger
        {
            get; private set;
        }
        public HotkeyInfo Action
        {
            get; private set;
        }

        public HotkeyPair_legacy(HotkeyInfo trigger, HotkeyInfo action)
        {
            Trigger = trigger;
            Action = action;
        }
    }
}
