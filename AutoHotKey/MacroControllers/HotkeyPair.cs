﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHotKey.MacroControllers
{
    [Serializable()]
    public class HotkeyPair
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

        public HotkeyInfo EndingAction
        {
            get; private set;
        }

        public HotkeyPair(HotkeyInfo trigger, HotkeyInfo action, HotkeyInfo endingAction = null)
        {
            Trigger = trigger;
            Action = action;
            EndingAction = endingAction;
        }
    }
}
