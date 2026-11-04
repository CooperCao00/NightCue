using System;
using System.IO;
using System.Globalization;
using System.Xml;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace NightCue
{
    public class Options
    {
        public string Start = "23:00";
        public string End = "07:00";
        public bool Hotkey = true;
        public int SnoozeMinutes = 15;
        public void Validate()
        {
            TimeSpan s, e;
            if (!TimeSpan.TryParseExact(Start, @"hh\:mm", null, out s) || !TimeSpan.TryParseExact(End, @"hh\:mm", null, out e) || s == e)
                throw new InvalidDataException("开始和结束时间须为不同的 24 小时时间。");
            if (SnoozeMinutes < 1 || SnoozeMinutes > 120)
                throw new InvalidDataException("延后时间须为 1–120 分钟。");
        }
    }

    public class NightState
    {
        public string Night = "";
        public string Decision = "";
        public DateTime SnoozeUntilUtc = DateTime.MinValue;
    }

    public enum Cue
    {
        Day,
        Pending,
        Snoozed,
        Dark,
        Dismissed
    }

    public static class Schedule
    {
        public static string NightKey(DateTime local, Options o)
        {
            TimeSpan start = TimeSpan.ParseExact(o.Start, @"hh\:mm", CultureInfo.InvariantCulture), end = TimeSpan.ParseExact(o.End, @"hh\:mm", CultureInfo.InvariantCulture), t = local.TimeOfDay;
            if (start < end)
                return t >= start && t < end ? local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
            if (t >= start)
                return local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (t < end)
                return local.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return null;
        }

        public static Cue Evaluate(DateTime local, DateTime utc, Options o, NightState state, bool dark)
        {
            string night = NightKey(local, o);
            if (night == null)
                return dark ? Cue.Dark : Cue.Day;
            if (state.Night != night)
            {
                state.Night = night;
                state.Decision = "";
                state.SnoozeUntilUtc = DateTime.MinValue;
            }

            if (dark)
            {
                state.Decision = "dark";
                state.SnoozeUntilUtc = DateTime.MinValue;
                return Cue.Dark;
            }

            if (!String.IsNullOrEmpty(state.Decision))
                return Cue.Dismissed;
            return utc < state.SnoozeUntilUtc ? Cue.Snoozed : Cue.Pending;
        }
    }

    public static class Store
    {
        public static string Root = AppDomain.CurrentDomain.BaseDirectory;
        public static string PathFor(string name)
        {
            return Path.Combine(Root, name);
        }

        public static T Read<T>(string name)
            where T : new()
        {
            string path = PathFor(name);
            if (!File.Exists(path))
                return new T();
            // Configuration is local data: never resolve external entities or DTDs.
            XmlReaderSettings settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 1048576
            };
            using (FileStream f = File.OpenRead(path))
            using (XmlReader reader = XmlReader.Create(f, settings))
            {
                object value = new XmlSerializer(typeof(T)).Deserialize(reader);
                return value == null ? new T() : (T)value;
            }
        }

        public static void Save<T>(string name, T value)
        {
            string path = PathFor(name), temp = path + ".tmp";
            using (FileStream f = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new XmlSerializer(typeof(T)).Serialize(f, value);
                f.Flush(true);
            }

            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        public static void Log(Exception e)
        {
            try
            {
                string p = PathFor("error.log");
                if (File.Exists(p) && new FileInfo(p).Length > 131072)
                    File.WriteAllText(p, "");
                File.AppendAllText(p, DateTime.Now.ToString("s") + " " + e + Environment.NewLine);
            }
            catch
            {
            }
        }
    }

    public static class Native
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(IntPtr h, uint m, IntPtr w, string l, uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint key);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr h);
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr h);
    }

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

    public static class Startup
    {
        const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Name = "NightCue";
        public static string Command
        {
            get
            {
                return "\"" + Application.ExecutablePath + "\" --background";
            }
        }

        public static bool Enabled
        {
            get
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Key))
                    return k != null && String.Equals((string)k.GetValue(Name, ""), Command, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static void Set(bool enabled)
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Key))
            {
                if (enabled)
                    k.SetValue(Name, Command);
                else
                    k.DeleteValue(Name, false);
            }
        }
    }

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

    public class TrayApp : ApplicationContext
    {
        readonly NotifyIcon tray;
        readonly ContextMenuStrip menu;
        readonly ToolStripMenuItem status, snooze, dismiss, restore;
        readonly System.Windows.Forms.Timer timer;
        readonly HostWindow host;
        readonly EventWaitHandle showSignal;
        readonly Icon amber, gray, blue;
        Options options;
        NightState state;
        Cue cue;
        SettingsForm settings;
        string lastState;
        string warning = "";
        int ticks;
        public TrayApp(bool install, bool show, EventWaitHandle signal)
        {
            showSignal = signal;
            options = Store.Read<Options>("settings.xml");
            options.Validate();
            state = Store.Read<NightState>("state.xml");
            if (install)
            {
                Store.Save("settings.xml", options);
                Startup.Set(true);
            }

            amber = MakeIcon(Color.FromArgb(245, 180, 60), true);
            gray = MakeIcon(Color.FromArgb(145, 151, 164), false);
            blue = MakeIcon(Color.FromArgb(125, 164, 224), true);
            host = new HostWindow();
            host.Toggle += delegate
            {
                ChangeTheme(!Theme.IsDark());
            };
            host.SetHotkey(options.Hotkey);
            menu = new ContextMenuStrip
            {
                Font = new Font("Microsoft YaHei UI", 10f),
                ShowImageMargin = false
            };
            status = new ToolStripMenuItem
            {
                Enabled = false
            };
            menu.Items.Add(status);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("进入深色模式", null, delegate
            {
                ChangeTheme(true);
            });
            menu.Items.Add("切换到浅色模式", null, delegate
            {
                ChangeTheme(false);
            });
            snooze = new ToolStripMenuItem("15 分钟后提醒", null, delegate
            {
                Snooze();
            });
            menu.Items.Add(snooze);
            dismiss = new ToolStripMenuItem("今晚保持浅色", null, delegate
            {
                ChangeTheme(false);
            });
            menu.Items.Add(dismiss);
            restore = new ToolStripMenuItem("恢复今晚提醒", null, delegate
            {
                state.Decision = "";
                state.SnoozeUntilUtc = DateTime.MinValue;
                Tick();
            });
            menu.Items.Add(restore);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("设置…", null, delegate
            {
                ShowSettings();
            });
            menu.Items.Add("使用说明", null, delegate
            {
                Process.Start(Store.PathFor("使用说明.txt"));
            });
            menu.Items.Add("退出", null, delegate
            {
                ExitThread();
            });
            menu.Padding = new Padding(6);
            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripMenuItem)
                    item.Padding = new Padding(12, 7, 12, 7);
            }

            menu.Opening += delegate
            {
                menu.Renderer = new QuietMenuRenderer(Theme.IsDark());
                Tick();
            };
            tray = new NotifyIcon
            {
                Icon = gray,
                Text = "夜色提醒",
                ContextMenuStrip = menu,
                Visible = true
            };
            tray.MouseUp += delegate (object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    Native.SetForegroundWindow(host.Handle);
                    menu.Show(Cursor.Position);
                }
            };
            timer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            timer.Tick += delegate
            {
                if (showSignal.WaitOne(0))
                    ShowSettings();
                if (++ticks >= 10)
                {
                    ticks = 0;
                    Tick();
                }
            };
            timer.Start();
            Tick();
            if (show)
                ShowSettings();
        }

        public static Icon MakeIcon(Color color, bool moon)
        {
            using (Bitmap b = new Bitmap(64, 64))
            using (Graphics g = Graphics.FromImage(b))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(color))
                {
                    if (moon)
                    {
                        using (GraphicsPath outer = new GraphicsPath())
                        using (GraphicsPath cut = new GraphicsPath())
                        {
                            outer.AddEllipse(9, 7, 46, 49);
                            cut.AddEllipse(26, 0, 39, 42);
                            using (Region r = new Region(outer))
                            {
                                r.Exclude(cut);
                                g.FillRegion(brush, r);
                            }
                        }

                        g.FillEllipse(brush, 46, 13, 5, 5);
                    }
                    else
                    {
                        using (Pen p = new Pen(color, 5))
                        {
                            g.DrawEllipse(p, 16, 16, 32, 32);
                        }
                    }
                }

                IntPtr h = b.GetHicon();
                try
                {
                    return (Icon)Icon.FromHandle(h).Clone();
                }
                finally
                {
                    Native.DestroyIcon(h);
                }
            }
        }

        string StateToken()
        {
            return state.Night + "|" + state.Decision + "|" + state.SnoozeUntilUtc.Ticks;
        }

        void SaveState()
        {
            string token = StateToken();
            if (token == lastState)
                return;
            Store.Save("state.xml", state);
            lastState = token;
        }

        void Tick()
        {
            try
            {
                cue = Schedule.Evaluate(DateTime.Now, DateTime.UtcNow, options, state, Theme.IsDark());
                SaveState();
                string text = StatusText();
                tray.Icon = cue == Cue.Pending ? amber : cue == Cue.Dark ? blue : gray;
                tray.Text = "夜色提醒 · " + text;
                status.Text = text;
                bool night = Schedule.NightKey(DateTime.Now, options) != null;
                snooze.Text = options.SnoozeMinutes + " 分钟后提醒";
                snooze.Enabled = night && (cue == Cue.Pending || cue == Cue.Snoozed);
                dismiss.Enabled = night;
                restore.Enabled = night && cue != Cue.Pending && cue != Cue.Dark;
                if (settings != null && !settings.IsDisposed)
                    settings.UpdateStatus(text, HotkeyText(), warning);
            }
            catch (Exception ex)
            {
                warning = "保存或读取失败，请查看文件夹内的 error.log。";
                Store.Log(ex);
            }
        }

        public string StatusText()
        {
            switch (cue)
            {
                case Cue.Pending:
                    return "可以切换深色了，等你方便时";
                case Cue.Snoozed:
                    return "已延后至 " + state.SnoozeUntilUtc.ToLocalTime().ToString("HH:mm");
                case Cue.Dark:
                    return Schedule.NightKey(DateTime.Now, options) != null ? "当前为深色，今晚不再提醒" : "当前为深色，可手动切回浅色";
                case Cue.Dismissed:
                    return "今晚不再提醒";
                default:
                    return options.Start + " 开始提醒";
            }
        }

        string HotkeyText()
        {
            if (!options.Hotkey)
                return "快捷键已关闭";
            return host.HotkeyOK ? "Ctrl + Alt + Shift + D" : "快捷键被其他应用占用，可用托盘菜单切换";
        }

        void RecordDecision(string decision)
        {
            string night = Schedule.NightKey(DateTime.Now, options);
            if (night != null)
            {
                state.Night = night;
                state.Decision = decision;
                state.SnoozeUntilUtc = DateTime.MinValue;
            }
        }

        public void ChangeTheme(bool dark)
        {
            try
            {
                Theme.Set(dark);
                RecordDecision(dark ? "dark" : "light");
                Tick();
            }
            catch (Exception ex)
            {
                Store.Log(ex);
                MessageBox.Show("无法切换主题：" + ex.Message, "夜色提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void Snooze()
        {
            if (Schedule.NightKey(DateTime.Now, options) == null)
                return;
            state.Decision = "";
            state.SnoozeUntilUtc = DateTime.UtcNow.AddMinutes(options.SnoozeMinutes);
            Tick();
        }

        public void ShowSettings()
        {
            if (settings != null && !settings.IsDisposed)
            {
                settings.Show();
                settings.Activate();
                return;
            }

            settings = new SettingsForm(this, options, amber);
            settings.Show();
            Tick();
        }

        public void OpenMenuAt(Point point)
        {
            menu.Show(point);
        }

        public void OpenMenu(Control anchor)
        {
            menu.Show(anchor, new Point(0, anchor.Height));
        }

        public bool ApplyOptions(Options next, bool startup)
        {
            try
            {
                next.Validate();
                bool oldStartup = Startup.Enabled;
                Startup.Set(startup);
                try
                {
                    Store.Save("settings.xml", next);
                }
                catch
                {
                    Startup.Set(oldStartup);
                    throw;
                }

                options = next;
                host.SetHotkey(options.Hotkey);
                Tick();
                return true;
            }
            catch (Exception ex)
            {
                Store.Log(ex);
                MessageBox.Show("未能保存设置：" + ex.Message, "夜色提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
        }

        protected override void ExitThreadCore()
        {
            timer.Stop();
            timer.Dispose();
            tray.Visible = false;
            tray.Dispose();
            menu.Dispose();
            host.Dispose();
            amber.Dispose();
            gray.Dispose();
            blue.Dispose();
            if (settings != null)
                settings.Dispose();
            base.ExitThreadCore();
        }
    }

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
                    MessageBox.Show("夜色提醒未能启动：" + e.Message + "\n详情见文件夹中的 error.log。", "夜色提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
