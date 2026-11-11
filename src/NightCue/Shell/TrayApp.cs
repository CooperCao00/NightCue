using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace NightCue
{
    public class TrayApp : ApplicationContext, ISettingsActions
    {
        readonly NotifyIcon tray;
        readonly ContextMenuStrip menu;
        readonly ToolStripMenuItem status, snooze, dismiss, restore;
        readonly System.Windows.Forms.Timer timer;
        readonly HostWindow host;
        readonly EventWaitHandle showSignal;
        readonly Icon amber, gray, blue, sun;
        Options options;
        NightState state;
        Cue cue;
        SettingsForm settings;
        string lastState;
        string warning = "";
        bool optionsRefreshFailed;
        int ticks;
        bool dismissTargetDark;
        public TrayApp(bool install, bool show, EventWaitHandle signal)
        {
            showSignal = signal;
            options = Store.ReadRecovering<Options>("settings.xml", delegate(Options value) { value.Validate(); });
            L.Select(options.Language);
            options.Validate();
            state = Store.ReadRecovering<NightState>("state.xml", null);
            if (install)
            {
                Store.Save("settings.xml", options);
                Startup.Set(true);
            }

            sun = TrayIcon.CreateSun(Color.FromArgb(245, 180, 60));
            amber = TrayIcon.Create(Color.FromArgb(245, 180, 60), true);
            gray = TrayIcon.Create(Color.FromArgb(145, 151, 164), false);
            blue = TrayIcon.Create(Color.FromArgb(125, 164, 224), true);
            host = new HostWindow();
            host.Toggle += delegate
            {
                ChangeTheme(!Theme.IsDark());
            };
            host.SetHotkey(options.Hotkey);
            menu = new ContextMenuStrip
            {
                Font = new Font("Segoe UI", 10f),
                ShowImageMargin = false
            };
            status = new ToolStripMenuItem
            {
                Enabled = false
            };
            menu.Items.Add(status);
            menu.Items.Add(new ToolStripSeparator());
            AddMenu("SwitchDark", delegate
            {
                ChangeTheme(true);
            });
            AddMenu("SwitchLight", delegate
            {
                ChangeTheme(false);
            });
            snooze = new ToolStripMenuItem(L.Format("SnoozeMenu", options.SnoozeMinutes), null, delegate
            {
                Snooze();
            });
            menu.Items.Add(snooze);
            dismiss = new ToolStripMenuItem(L.Text("KeepLight"), null, delegate
            {
                ChangeTheme(dismissTargetDark);
            });
            menu.Items.Add(dismiss);
            restore = new ToolStripMenuItem(L.Text("Restore"), null, delegate
            {
                Schedule.Restore(DateTime.Now, options, state);
                Tick();
            });
            menu.Items.Add(restore);
            menu.Items.Add(new ToolStripSeparator());
            AddMenu("SettingsMenu", delegate
            {
                ShowSettings();
            });
            AddMenu("TrayGuide", delegate
            {
                MessageBox.Show(L.Text("TrayGuideBody"), "NightCue", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            AddMenu("Help", delegate
            {
                MessageBox.Show(L.Text("HelpBody"), L.Text("HelpTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            AddMenu("Exit", delegate
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
                Text = "NightCue",
                ContextMenuStrip = menu,
                Visible = true
            };
            tray.MouseUp += delegate (object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    menu.Close();
                    ShowSettings();
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
            L.Changed += RefreshLanguage;
            RefreshLanguage();
            timer.Start();
            Tick();
            if (show)
                ShowSettings();
        }

        void AddMenu(string key, EventHandler action)
        {
            var item = new ToolStripMenuItem(L.Text(key), null, action) { Tag = key };
            menu.Items.Add(item);
        }

        void RefreshLanguage()
        {
            Font previous = menu.Font;
            menu.Font = new Font(L.CurrentLanguage.Font, 10f);
            previous.Dispose();
            foreach (ToolStripItem item in menu.Items)
                if (item.Tag is string) item.Text = L.Text((string)item.Tag);
            dismiss.Text = L.Text("KeepLight");
            restore.Text = L.Text("Restore");
            Tick();
        }

        public Cue CurrentCue { get { return cue; } }
        public bool HotkeyEnabled { get { return options.Hotkey; } }
        public bool HotkeyAvailable { get { return host.HotkeyOK; } }
        public string Warning { get { return optionsRefreshFailed ? L.Text("ApplyRefreshFailed") : warning.Length == 0 ? "" : L.Text("ReadFailure"); } }
        public StartupRegistration StartupState { get { return Startup.Registration; } }

        string StateToken()
        {
            return state.Night + "|" + state.Decision + "|" + state.SnoozeUntilUtc.Ticks + "|" + state.Day + "|" + state.DayDecision + "|" + state.DaySnoozeUntilUtc.Ticks;
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
                warning = "";
                try { SaveState(); }
                catch (Exception saveFailure)
                {
                    warning = "ReadFailure";
                    Store.Log(saveFailure);
                }
                string text = StatusText();
                tray.Icon = cue == Cue.Pending ? amber : cue == Cue.LightPending ? sun : cue == Cue.Dark ? blue : gray;
                tray.Text = L.TrayText("NightCue · " + text);
                status.Text = text;
                bool night = Schedule.NightKey(DateTime.Now, options) != null;
                snooze.Text = L.Format("SnoozeMenu", options.SnoozeMinutes);
                snooze.Enabled = cue == Cue.Pending || cue == Cue.Snoozed || cue == Cue.LightPending || cue == Cue.LightSnoozed;
                // Keep the action consistent with the label the user sees.
                dismissTargetDark = !night;
                dismiss.Text = L.Text(night ? "KeepLight" : "KeepDark");
                dismiss.Enabled = night || options.MorningReminder;
                restore.Text = L.Text(night ? "Restore" : "RestoreDay");
                restore.Enabled = cue == Cue.Snoozed || cue == Cue.Dismissed || cue == Cue.LightSnoozed || cue == Cue.LightDismissed;
                if (settings != null && !settings.IsDisposed)
                    settings.UpdateStatus();
            }
            catch (Exception ex)
            {
                warning = "ReadFailure";
                Store.Log(ex);
            }
        }

        public string StatusText()
        {
            return L.Status(cue, options.Start, (cue == Cue.LightSnoozed ? state.DaySnoozeUntilUtc : state.SnoozeUntilUtc), Schedule.NightKey(DateTime.Now, options) != null);
        }

        public void ChangeTheme(bool dark)
        {
            try
            {
                Theme.Set(dark);
                Schedule.RecordDecision(DateTime.Now, options, state, dark);
                Tick();
            }
            catch (Exception ex)
            {
                Store.Log(ex);
                MessageBox.Show(L.Text("ThemeFailure"), "NightCue", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void Snooze()
        {
            Schedule.Snooze(DateTime.Now, DateTime.UtcNow, options, state);
            Tick();
        }

        public void ShowSettings()
        {
            if (settings == null || settings.IsDisposed)
            {
                SettingsForm next = new SettingsForm(this, options, amber);
                next.FormClosed += delegate
                {
                    if (Object.ReferenceEquals(settings, next)) settings = null;
                };
                settings = next;
            }

            if (settings.WindowState == FormWindowState.Minimized)
                settings.WindowState = FormWindowState.Normal;
            settings.Show();
            settings.BringToFront();
            settings.Activate();
            Native.SetForegroundWindow(settings.Handle);
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

        public bool SetStartup(bool enabled)
        {
            try
            {
                Startup.Set(enabled);
                return true;
            }
            catch (Exception ex)
            {
                Store.Log(ex);
                return false;
            }
        }

        public bool ApplyOptions(Options next)
        {
            Options accepted;
            try
            {
                accepted = next.Copy();
                accepted.Validate();
                Store.Save("settings.xml", accepted);
            }
            catch (Exception ex)
            {
                Store.Log(ex);
                return false;
            }

            // Persistence is committed. A refresh failure must not make the form
            // roll back its copy and later overwrite settings already on disk.
            bool hotkeyChanged = options.Hotkey != accepted.Hotkey;
            options = accepted;
            if (hotkeyChanged)
            {
                try { host.SetHotkey(options.Hotkey); }
                catch (Exception ex) { optionsRefreshFailed = true; Store.Log(ex); }
            }
            try { L.Select(options.Language); }
            catch (Exception ex) { optionsRefreshFailed = true; Store.Log(ex); }
            // Keep runtime refresh errors visible until restart; Tick clears its
            // own transient I/O warning independently.
            Tick();
            return true;
        }

        protected override void ExitThreadCore()
        {
            L.Changed -= RefreshLanguage;
            timer.Stop();
            timer.Dispose();
            tray.Visible = false;
            tray.Dispose();
            menu.Dispose();
            host.Dispose();
            amber.Dispose();
            gray.Dispose();
            blue.Dispose();
            sun.Dispose();
            if (settings != null)
                settings.Dispose();
            base.ExitThreadCore();
        }
    }
}
