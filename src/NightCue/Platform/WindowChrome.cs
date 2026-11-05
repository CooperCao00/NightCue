using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace NightCue
{
    public static class WindowChrome
    {
        public static bool SupportsModernChrome(Version version)
        {
            return version.Major > 10 || (version.Major == 10 && version.Build >= 22000);
        }
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

        public static void Apply(IntPtr window, bool dark, Color background)
        {
            // These attributes are documented for Windows 11. Windows 10 keeps
            // its native caption and corners; application content still themes.
            if (!SupportsModernChrome(Environment.OSVersion.Version)) return;
            int darkValue = dark ? 1 : 0, round = 2;
            int caption = background.R | (background.G << 8) | (background.B << 16);
            try
            {
                DwmSetWindowAttribute(window, 20, ref darkValue, 4);
                DwmSetWindowAttribute(window, 33, ref round, 4);
                DwmSetWindowAttribute(window, 35, ref caption, 4);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }
    }
}
