using System;
using System.Reflection;
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
            string launch = "Launch metadata unavailable";
            try
            {
                launch = "version=" + Assembly.GetExecutingAssembly().GetName().Version +
                    "; executable=" + Application.ExecutablePath + "; arguments=" + String.Join(" | ", args);
                Store.Diagnostics("startup-attempt", launch);
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    Store.Diagnostics("unhandled-exception", launch + "; terminating=" + e.IsTerminating);
                    Exception failure = e.ExceptionObject as Exception;
                    if (failure != null) Store.Log(failure);
                };
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
                        Store.Diagnostics("duplicate-instance", launch + "; action=" + (background ? "exit-background-copy" : "signal-existing-instance"));
                        if (!background)
                            signal.Set();
                        return;
                    }

                    try
                    {
                        Application.ThreadException += delegate (object s, ThreadExceptionEventArgs e)
                        {
                            Store.Diagnostics("ui-thread-exception", launch);
                            Store.Log(e.Exception);
                        };
                        TrayApp application = new TrayApp(install, !background, signal);
                        Store.Diagnostics("tray-initialized", launch + "; background=" + background);
                        Application.Run(application);
                        Store.Diagnostics("normal-exit", launch);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            catch (Exception e)
            {
                Store.Diagnostics("startup-or-run-failed", launch);
                Store.Log(e);
                if (!background)
                    MessageBox.Show(L.Text("StartFailure"), "NightCue", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
