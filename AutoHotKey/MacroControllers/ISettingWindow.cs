using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoHotKey.MacroControllers
{
    public interface ISettingWindow
    {
        void EndSelectingSpecialKey(int selectedKeycode);
    }
}
