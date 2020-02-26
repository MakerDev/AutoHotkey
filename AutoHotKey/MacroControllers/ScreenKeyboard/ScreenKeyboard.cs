using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoHotKey.MacroControllers;
using System.Windows;
using System.Windows.Media;

namespace AutoHotKey.MacroControllers.ScreenKeyboard
{
    public class ScreenKey : ICloneable
    {
        [NonSerialized]
        public static readonly Color DefaultColor = Color.FromArgb(255, 51, 51, 51);
        [NonSerialized]
        public static readonly Color DefaultFontColor = Color.FromArgb(255, 255, 255, 255);
        [NonSerialized]
        public static readonly Color DefaultTogleKeyColor = Color.FromArgb(255, 131, 131, 131);

        [NonSerialized]
        public static readonly Color DefaultHoverColor = Color.FromArgb(255, 210, 210, 210);
        [NonSerialized]
        public static readonly Color DefaultHoverFontColor = Color.FromArgb(255, 0, 0, 0);

        [NonSerialized]
        public static readonly Color DefaultPressedColor = Color.FromArgb(255, 00, 118, 215);
        [NonSerialized]
        public static readonly Color DefaultPressedFontColor = Color.FromArgb(255, 255, 255, 255);

        public Point Location = new Point(0, 0);
        public string Label = "";
        public HotkeyInfo Action = null;
        public Size Size = new Size(30, 30);

        public Color ButtonColor = DefaultColor;
        public Color FontColor = DefaultFontColor;

        public Color ButtonHoverColor = DefaultHoverColor;
        public Color FontHoverColor = DefaultHoverFontColor;

        public Color ButtonPressedColor = DefaultPressedColor;
        public Color FontPressedColor = DefaultPressedFontColor;

        public int tag = 0;
        public bool IsTogle = false;
        public bool IsOn = false; //토글인 경우에만 사용

        public object Clone()
        {
            ScreenKey newKey = (ScreenKey)this.MemberwiseClone();



            return newKey;
        }
    }

    [Serializable]
    public class ScreenKeyboard
    {
        public Size WindowSize = new Size(150, 280);
        public Point StartingLocation = new Point(0,0);

        private Dictionary<int, ScreenKey> mDictScreenKeys = new Dictionary<int, ScreenKey>();


        public Size GetScreenSize()
        {
            return WindowSize;
        }

        public void SetCurrentLocation(double left, double top)
        {
            StartingLocation.X = left;
            StartingLocation.Y = top;
        }

        public void AddNewKey(ScreenKey newKey)
        {
            mDictScreenKeys.Add(newKey.tag, newKey);
        }

        public void EditKey(int tag, ScreenKey editedKey)
        {
            mDictScreenKeys[tag] = editedKey;
        }

        public void DeleteKey(int tag)
        {
            mDictScreenKeys.Remove(tag);
        }

        public Dictionary<int, ScreenKey> GetDictScreenKeys()
        {
            return mDictScreenKeys;
        }

    }
}
