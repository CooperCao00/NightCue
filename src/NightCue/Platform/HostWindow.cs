using System;
using System.Windows.Forms;

namespace NightCue
{
    public class HostWindow : NativeWindow, IDisposable
    {
        public event Action Toggle;
        public bool HotkeyOK;
        public HostWindow()
        {
            CreateHandle(new CreateParams { Caption = "NightCue message window" });
        }

        public void SetHotkey(bool enable)
        {
            Native.UnregisterHotKey(Handle, 1);
            HotkeyOK = enable && Native.RegisterHotKey(Handle, 1, 0x4000 | 1 | 2 | 4, (uint)Keys.D);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == 1 && Toggle != null)
                Toggle();
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            Native.UnregisterHotKey(Handle, 1);
            DestroyHandle();
        }
    }
}
