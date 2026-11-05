using System;
using System.Threading;
using System.Windows.Forms;

namespace NightCue
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            bool background = Array.IndexOf(args, "--background") >= 0;
            bool install = Array.IndexOf(args, "--install") >= 0;
            try
            {
                Native.SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                // Keep the original identifiers so older local builds share the same instance.
                bool created;
                using (Mutex mutex = new Mutex(true, @"Local\NightCue-CL-2026", out created))
                using (EventWaitHandle signal = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\NightCue-CL-Show"))
                {
                    if (!created)
                    {
                        if (!background)
                            signal.Set();
                        return;
                    }

                    try
                    {
                        Application.ThreadException += delegate (object s, ThreadExceptionEventArgs e)
                        {
                            Store.Log(e.Exception);
                        };
                        Application.Run(new TrayApp(install, !background, signal));
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            catch (Exception e)
            {
                Store.Log(e);
                if (!background)
                    MessageBox.Show(L.Text("StartFailure"), "NightCue", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
