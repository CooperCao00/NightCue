using System;
using System.Threading;
using Microsoft.Win32;

namespace NightCue
{
    public static class Theme
    {
        public const string Key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        public static bool IsDark()
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Key))
                return k != null && Convert.ToInt32(k.GetValue("AppsUseLightTheme", 1)) == 0 && Convert.ToInt32(k.GetValue("SystemUsesLightTheme", 1)) == 0;
        }

        public static void Set(bool dark)
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Key))
            {
                object apps = k.GetValue("AppsUseLightTheme"), system = k.GetValue("SystemUsesLightTheme");
                try
                {
                    k.SetValue("AppsUseLightTheme", dark ? 0 : 1, RegistryValueKind.DWord);
                    k.SetValue("SystemUsesLightTheme", dark ? 0 : 1, RegistryValueKind.DWord);
                    k.Flush();
                }
                catch
                {
                    if (apps == null)
                        k.DeleteValue("AppsUseLightTheme", false);
                    else
                        k.SetValue("AppsUseLightTheme", apps);
                    if (system == null)
                        k.DeleteValue("SystemUsesLightTheme", false);
                    else
                        k.SetValue("SystemUsesLightTheme", system);
                    throw;
                }
            }

            // Broadcast asynchronously so a hung application cannot block the tray UI.
            ThreadPool.QueueUserWorkItem(delegate
            {
                IntPtr result;
                Native.SendMessageTimeout(new IntPtr(0xffff), 0x001A, IntPtr.Zero, "ImmersiveColorSet", 0x0002, 200, out result);
            });
        }
    }
}
