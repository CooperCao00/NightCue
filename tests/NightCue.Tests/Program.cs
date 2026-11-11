using System;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NightCue;

class Tests
{
    static int count;
    static void Check(bool ok, string name)
    {
        if (!ok)
            throw new Exception("FAIL: " + name);
        count++;
        Console.WriteLine("PASS: " + name);
    }

    static DateTime T(string s)
    {
        return DateTime.ParseExact(s, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    static Cue Eval(string time, Options o, NightState s, bool dark = false)
    {
        DateTime t = T(time);
        return Schedule.Evaluate(t, t.AddHours(-8), o, s, dark);
    }

    static void Rejects<T>(Action action, string name)
        where T : Exception
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (T)
        {
            rejected = true;
        }

        Check(rejected, name);
    }

    static void CheckView(string locale, bool dark)
    {
        using (var icon = TrayIcon.Create(System.Drawing.Color.Orange, true))
        using (var form = new SettingsForm(null, new Options { Language = locale }, icon, true, dark))
        {
            var view = form.View;
            view.Measure(new System.Windows.Size(1000, 790));
            view.Arrange(new System.Windows.Rect(0, 0, 1000, 790));
            view.UpdateLayout();
            var bitmap = new RenderTargetBitmap(1500, 1185, 144, 144, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(view);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var file = File.Create(Store.PathFor((Store.RecoveryNotice ? "recovery-" : "preview-") + locale + (dark ? "-dark.png" : "-light.png"))))
                encoder.Save(file);
            var input = (System.Windows.Controls.TextBox)view.FindName("StartTime");
            var hide = (System.Windows.Controls.Button)view.FindName("HideButton");
            Check(input.Text == "23:00" && Math.Abs(view.ActualWidth - 1000) < 1 && hide.Content.ToString() == L.TextFor(locale, "Hide"),
                locale + (dark ? " dark" : " light") + " UI loads, localizes and renders");
            CheckTextLayout(view, locale);
            var startupStatus = (System.Windows.Controls.TextBlock)view.FindName("StartupStatus");
            foreach (string key in new[] { "StartupRegistered", "StartupRepair", "StartupUnknown", "SaveFailed" })
            {
                startupStatus.Text = L.Text(key);
                view.UpdateLayout();
                CheckTextLayout(view, locale);
            }
            var timeFeedback = (System.Windows.Controls.TextBlock)view.FindName("TimeFeedback");
            foreach (string key in new[] { "TimeHint", "InvalidStart", "InvalidEnd", "InvalidTimes", "SaveFailed" })
            {
                timeFeedback.Text = L.Text(key);
                view.UpdateLayout();
                CheckTextLayout(view, locale);
            }
            foreach (string field in new[] { "Minutes", "Hotkey", "Language", "Morning" })
            {
                var rowError = (System.Windows.Controls.TextBlock)view.FindName(field + "Feedback");
                rowError.Text = L.Text(field == "Minutes" ? "InvalidSnooze" : "SaveFailedShort");
                rowError.Visibility = System.Windows.Visibility.Visible;
            }
            ((System.Windows.Controls.TextBlock)view.FindName("Feedback")).Text = L.Text("SaveFailed");
            view.UpdateLayout();
            CheckTextLayout(view, locale);
            bitmap = new RenderTargetBitmap(1500, 1185, 144, 144, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(view);
            encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var file = File.Create(Store.PathFor("errors-" + locale + (dark ? "-dark.png" : "-light.png"))))
                encoder.Save(file);
            if (Store.RecoveryNotice)
            {
                var dismiss = (System.Windows.FrameworkElement)view.FindName("RecoveryDismissButton");
                var card = (System.Windows.FrameworkElement)view.FindName("CueStatusCard");
                double bottom = dismiss.TranslatePoint(new System.Windows.Point(0, dismiss.ActualHeight), view).Y;
                double top = card.TranslatePoint(new System.Windows.Point(0, 0), view).Y;
                Check(dismiss.Visibility == System.Windows.Visibility.Visible && bottom + 8 < top, locale + " recovery notice clears the cue card");
            }
        }
    }

    static void CheckTextLayout(System.Windows.DependencyObject node, string locale)
    {
        var text = node as System.Windows.Controls.TextBlock;
        if (text != null && text.ActualWidth > 1 && !String.IsNullOrEmpty(text.Text))
        {
            var measured = new System.Windows.Controls.TextBlock
            {
                Text = text.Text, FontFamily = text.FontFamily, FontSize = text.FontSize,
                FontWeight = text.FontWeight, FontStyle = text.FontStyle, FontStretch = text.FontStretch,
                FlowDirection = text.FlowDirection, Language = text.Language, TextWrapping = text.TextWrapping,
                LineHeight = text.LineHeight, LineStackingStrategy = text.LineStackingStrategy
            };
            System.Windows.Media.TextOptions.SetTextFormattingMode(measured, System.Windows.Media.TextFormattingMode.Display);
            // Small tolerance allows per-monitor pixel rounding in the offscreen host.
            measured.Measure(new System.Windows.Size(text.ActualWidth + 4, Double.PositiveInfinity));
            if (measured.DesiredSize.Height > text.ActualHeight + 3)
                throw new Exception("Clipped text [" + locale + "]: " + text.Text);
        }
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
            CheckTextLayout(System.Windows.Media.VisualTreeHelper.GetChild(node, i), locale);
    }

    static void CheckLocalization()
    {
        var baseline = new Dictionary<string, string>();
        foreach (LanguageInfo info in L.Languages) if (info.Code == "en") baseline = info.Strings;
        int languages = 0;
        foreach (LanguageInfo info in L.Languages)
        {
            languages++;
            bool complete = info.Strings.Count == baseline.Count;
            foreach (var entry in baseline)
            {
                string value;
                complete &= info.Strings.TryGetValue(entry.Key, out value) && !String.IsNullOrWhiteSpace(value);
                string expected = String.Join(",", Matches(entry.Value));
                if (value == null || expected != String.Join(",", Matches(value))) throw new Exception("Placeholder mismatch: " + info.Code + "/" + entry.Key);
                String.Format(CultureInfo.InvariantCulture, value, 15);
            }
            Check(complete, info.Code + " resource keys and placeholders complete");
            L.Select(info.Code);
            foreach (Cue cue in Enum.GetValues(typeof(Cue)))
                Check(L.TrayText("NightCue · " + L.Status(cue, "23:00", DateTime.UtcNow, true)).Length <= 63, info.Code + " tray tooltip limit: " + cue);
            CheckView(info.Code, false);
            CheckView(info.Code, true);
        }
        Check(languages == 10, "Ten built-in languages");
        string[,] cases = { { "en-GB", "en" }, { "zh-SG", "zh-CN" }, { "zh-Hans", "zh-CN" }, { "zh-HK", "zh-TW" },
            { "zh-Hant-TW", "zh-TW" }, { "ja-JP", "ja" }, { "ko-KR", "ko" }, { "de-CH", "de" },
            { "fr-CA", "fr" }, { "es-MX", "es" }, { "pt-PT", "pt-BR" }, { "ru-RU", "ru" }, { "ar-SA", "en" } };
        bool matched = true;
        for (int i = 0; i < cases.GetLength(0); i++) matched &= L.Resolve("system", cases[i, 0]) == cases[i, 1];
        Check(matched, "Regional and script language fallback rules");
        Check(L.Resolve("en", "zh-CN") == "en" && L.Resolve("de", "ja-JP") == "de", "Manual language takes priority over system");
        Check(L.Resolve("unknown", "zh-CN") == "en" && L.TextFor("unknown", "Hide") == baseline["Hide"], "Unsupported language falls back to English");
        CultureInfo previous = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR"); L.Select("system");
            Check(L.Current == "fr", "Follow Windows uses display language, not regional calendar");
        }
        finally { Thread.CurrentThread.CurrentUICulture = previous; }
        Store.Save("language.xml", new Options { Language = "ja" });
        Check(Store.Read<Options>("language.xml").Language == "ja", "Manual language survives configuration reload");
        File.WriteAllText(Store.PathFor("legacy.xml"), "<Options><Start>23:00</Start><End>07:00</End></Options>");
        Check(Store.Read<Options>("legacy.xml").Language == "system", "Older configuration defaults to system language");
        using (var icon = TrayIcon.Create(System.Drawing.Color.Orange, true))
        using (var form = new SettingsForm(null, new Options { Language = "en" }, icon, true, false))
        {
            var choice = (System.Windows.Controls.ComboBox)form.View.FindName("LanguageChoice");
            var hide = (System.Windows.Controls.Button)form.View.FindName("HideButton");
            choice.SelectedValue = "de";
            form.View.Measure(new System.Windows.Size(1000, 790));
            form.View.Arrange(new System.Windows.Rect(0, 0, 1000, 790));
            form.View.UpdateLayout();
            Check(L.Current == "de" && hide.Content.ToString() == L.Text("Hide"), "Language selection updates existing view immediately");
            var input = (System.Windows.Controls.TextBox)form.View.FindName("StartTime");
            input.Text = "bad";
            Commit(form, "CommitTimes");
            Check(((System.Windows.Controls.TextBlock)form.View.FindName("TimeFeedback")).Text == L.Text("InvalidStart"), "Validation message uses selected language");
        }
        Check(L.Current == "en", "Closing unsaved language preview restores original choice");
        Check(!WindowChrome.SupportsModernChrome(new Version(10, 0, 19045)) && WindowChrome.SupportsModernChrome(new Version(10, 0, 22000)), "Windows 10 skips Windows 11 caption attributes");
        L.Select("system");
    }
    static List<string> Matches(string value)
    {
        var result = new List<string>();
        foreach (Match match in Regex.Matches(value, @"\{[0-9]+\}")) result.Add(match.Value);
        result.Sort(StringComparer.Ordinal);
        return result;
    }


    sealed class MemoryStartup : IStartupStore
    {
        public string Value;
        public bool FailNextWrite;
        public bool FailRead;
        public bool IgnoreNextWrite;
        public string Read() { if (FailRead) throw new UnauthorizedAccessException("simulated access denied"); return Value; }
        public void Write(string value)
        {
            if (FailNextWrite) { FailNextWrite = false; throw new IOException("Simulated registry failure"); }
            if (IgnoreNextWrite) { IgnoreNextWrite = false; return; }
            Value = value;
        }
    }

    static void CheckStartup()
    {
        string old = Startup.BuildCommand(@"C:\Old Folder\NightCue.exe");
        string next = Startup.BuildCommand(@"C:\新目录\NightCue.exe");
        Check(next == "\"C:\\新目录\\NightCue.exe\" --background", "Startup quotes Unicode and spaced paths");
        var store = new MemoryStartup { Value = old };
        Check(Startup.Inspect(store, next) == StartupRegistration.DifferentPath, "Moved startup registration is distinguished");
        store.Value = next.ToUpperInvariant();
        Check(Startup.Inspect(store, next) == StartupRegistration.CurrentPath, "Registered path comparison ignores case");
        store.Value = null;
        Check(Startup.Inspect(store, next) == StartupRegistration.Unregistered, "Missing startup is distinguished");
        store.FailRead = true;
        Check(Startup.Inspect(store, next) == StartupRegistration.Unavailable, "Unreadable registration is not reported as disabled");
        store.FailRead = false; store.Value = old;
        Rejects<ArgumentException>(delegate { Startup.SaveSettings(store, Startup.BuildCommand(@"C:\" + new string('x', 260) + @"\NightCue.exe"), delegate { }); }, "Reject overlong Windows startup command");
        bool saved = false;
        Startup.SaveSettings(store, next, delegate { saved = true; });
        Check(saved && store.Value == next, "Save repairs registration after a move");
        store.Value = old;
        Rejects<IOException>(delegate { Startup.SaveSettings(store, next, delegate { throw new IOException("disk full"); }); }, "Save failure is reported");
        Check(store.Value == old, "Failure restores exact old startup path");
        Startup.SaveSettings(store, null, delegate { });
        Check(store.Value == null, "Disabling startup removes entry");
        Rejects<IOException>(delegate { Startup.SaveSettings(store, next, delegate { throw new IOException("disk full"); }); }, "First registration failure is reported");
        Check(store.Value == null, "Failed first registration leaves no entry");
        store.Value = old; store.FailNextWrite = true; saved = false;
        Rejects<IOException>(delegate { Startup.SaveSettings(store, next, delegate { saved = true; }); }, "Registry failure is reported");
        Check(!saved && store.Value == old, "Registry failure does not save settings or remove old path");
        store.IgnoreNextWrite = true;
        Rejects<InvalidOperationException>(delegate { Startup.SaveSettings(store, next, delegate { saved = true; }); }, "Unapplied registry write is detected");
        Check(!saved && store.Value == old, "Unapplied write preserves settings and registration");
    }

    static void CheckRecovery()
    {
        string diag = Store.DiagnosticsRoot;
        string malformed = "<Options><Start>";
        File.WriteAllText(Store.PathFor("recover.xml"), malformed);
        Options recovered = Store.ReadRecovering<Options>("recover.xml", delegate(Options value) { value.Validate(); });
        Check(recovered.Start == "23:00" && Store.Read<Options>("recover.xml").Start == "23:00", "Malformed settings recover to persisted defaults");
        string[] backups = Directory.GetFiles(Store.DiagnosticDirectory, "recover.xml.corrupt-*.bak", SearchOption.AllDirectories);
        Check(backups.Length == 1 && File.ReadAllText(backups[0]) == malformed, "Recovery preserves the original broken file");
        Check(Store.RecoveryNotice && File.Exists(Path.Combine(Store.DiagnosticDirectory, "recovery.pending")), "Recovery notice persists on disk");
        var childInfo = new System.Diagnostics.ProcessStartInfo(System.Reflection.Assembly.GetExecutingAssembly().Location,
            "--check-recovery \"" + Store.DiagnosticsRoot + "\"") { UseShellExecute = false, CreateNoWindow = true };
        using (var child = System.Diagnostics.Process.Start(childInfo))
        {
            bool exited = child.WaitForExit(10000);
            if (!exited) child.Kill();
            Check(exited && child.ExitCode == 0, "Recovery notice survives a fresh process");
        }
        Store.Save("invalid-settings.xml", new Options { Start = "25:00" });
        recovered = Store.ReadRecovering<Options>("invalid-settings.xml", delegate(Options value) { value.Validate(); });
        Check(recovered.Start == "23:00", "Semantically invalid settings recover");
        File.WriteAllText(Store.PathFor("broken-state.xml"), "<NightState>");
        Check(Store.ReadRecovering<NightState>("broken-state.xml", null).Decision == "", "Malformed reminder state recovers independently");
        File.WriteAllText(Store.PathFor("recover-dtd.xml"), "<!DOCTYPE Options [<!ENTITY value '23:00'>]><Options><Start>&value;</Start></Options>");
        Check(Store.ReadRecovering<Options>("recover-dtd.xml", null).Start == "23:00", "Recovery still prohibits DTD entities");
        Store.Save("valid-settings.xml", new Options { Start = "22:15", Language = "de" });
        recovered = Store.ReadRecovering<Options>("valid-settings.xml", delegate(Options value) { value.Validate(); });
        Check(recovered.Start == "22:15" && recovered.Language == "de", "Valid settings are preserved");
        foreach (LanguageInfo locale in L.Languages) CheckView(locale.Code, false);
        Store.AcknowledgeRecovery();
        Check(!Store.RecoveryNotice && !File.Exists(Path.Combine(Store.DiagnosticDirectory, "recovery.pending")), "Acknowledgement clears the persistent recovery notice");

        File.WriteAllText(Store.PathFor("locked.xml"), malformed);
        using (var locked = new FileStream(Store.PathFor("locked.xml"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check(Store.ReadRecovering<Options>("locked.xml", null).Start == "23:00", "Unreadable settings use in-memory defaults");
        Rejects<IOException>(delegate { Store.Save("locked.xml", new Options()); }, "Later writes cannot overwrite an unreadable original");
        Check(File.ReadAllText(Store.PathFor("locked.xml")) == malformed, "Unreadable original remains intact");
        Store.AcknowledgeRecovery();

        string blockedDiagnostics = Store.PathFor("not-a-directory");
        File.WriteAllText(blockedDiagnostics, "occupied");
        Store.DiagnosticsRoot = blockedDiagnostics;
        File.WriteAllText(Store.PathFor("no-backup.xml"), malformed);
        Check(Store.ReadRecovering<Options>("no-backup.xml", null).Start == "23:00", "Unavailable diagnostics cannot prevent in-memory recovery");
        Rejects<IOException>(delegate { Store.Save("no-backup.xml", new Options()); }, "Backup failure blocks subsequent writes");
        Check(File.ReadAllText(Store.PathFor("no-backup.xml")) == malformed, "Backup failure preserves source bytes");
        Store.Diagnostics("test", "unwritable diagnostics must not throw");
        Store.DiagnosticsRoot = diag;
        Store.AcknowledgeRecovery();
        string log = Path.Combine(Store.DiagnosticDirectory, "diagnostics.log");
        for (int i = 0; i < 8; i++) Store.Diagnostics("test", new string('x', 40000));
        Check(File.Exists(log) && new FileInfo(log).Length <= 131072, "Diagnostic file size is bounded");
    }

    static void CheckMorning()
    {
        var o = new Options();
        var s = new NightState();
        Check(Eval("2026-11-04 06:59:59", o, s, true) == Cue.Dark, "No light cue before the end boundary");
        Check(Eval("2026-11-04 07:00:00", o, s, true) == Cue.LightPending, "Light cue at the end boundary");
        Check(s.Night == "2026-11-03" && s.Day == "2026-11-04", "Night and day identities are independent");
        DateTime local = T("2026-11-04 07:00:00");
        Schedule.Snooze(local, local.AddHours(-8), o, s);
        Store.Save("morning.xml", s);
        s = Store.Read<NightState>("morning.xml");
        Check(Eval("2026-11-04 07:14:59", o, s, true) == Cue.LightSnoozed, "Morning snooze survives restart");
        Check(Eval("2026-11-04 07:15:00", o, s, true) == Cue.LightPending, "Morning snooze expires exactly on time");
        Schedule.RecordDecision(T("2026-11-04 07:16:00"), o, s, true);
        Store.Save("morning.xml", s);
        s = Store.Read<NightState>("morning.xml");
        Check(Eval("2026-11-04 22:59:59", o, s, true) == Cue.LightDismissed, "Keep dark survives restart and suppresses daytime cue");
        Schedule.Restore(T("2026-11-04 08:00:00"), o, s);
        Check(Eval("2026-11-04 08:00:00", o, s, true) == Cue.LightPending, "Restore brings daytime cue back");
        Check(Eval("2026-11-04 08:01:00", o, s) == Cue.Day, "Observed light theme resolves daytime cue");
        Check(Eval("2026-11-04 08:02:00", o, s, true) == Cue.LightDismissed, "External return to dark does not nag again");
        Check(Eval("2026-11-04 23:00:00", o, s) == Cue.Pending, "Daytime choice does not suppress the next night");
        Check(Eval("2026-11-05 07:00:00", o, s, true) == Cue.LightPending, "Next morning resets the day choice");
        o.MorningReminder = false;
        Check(Eval("2026-11-05 07:01:00", o, s, true) == Cue.Dark, "Morning reminders can be disabled");
        Check(Eval("2026-11-05 07:02:00", o, s) == Cue.Day, "Disabled morning reminders remain quiet in light mode");
        o.MorningReminder = true;
        s = new NightState { Night = "2026-11-02", Decision = "light" };
        local = T("2026-11-05 23:00:00");
        Schedule.Snooze(local, local.AddHours(-8), o, s);
        Check(Eval("2026-11-05 23:00:01", o, s) == Cue.Snoozed, "Snooze action initializes a newly crossed night boundary");
        local = T("2026-11-06 07:00:00");
        Schedule.Snooze(local, local.AddHours(-8), o, s);
        Check(Eval("2026-11-06 07:00:01", o, s, true) == Cue.LightSnoozed, "Snooze action initializes a newly crossed day boundary");
        var shortWindow = new Options { Start = "18:00", End = "22:00" };
        s = new NightState();
        Schedule.RecordDecision(T("2026-11-04 22:01:00"), shortWindow, s, true);
        Check(Eval("2026-11-05 01:00:00", shortWindow, s, true) == Cue.LightDismissed && s.Day == "2026-11-04", "Daytime choice crosses midnight for a non-overnight window");
        Check(Eval("2026-11-05 22:00:00", shortWindow, s, true) == Cue.LightPending, "Non-overnight window gets a new day period at its end");
        Check(Store.Read<Options>("legacy.xml").MorningReminder, "Legacy settings receive the morning-cue default");
        File.WriteAllText(Store.PathFor("legacy-state.xml"), "<NightState><Night>2026-11-03</Night><Decision>light</Decision></NightState>");
        s = Store.Read<NightState>("legacy-state.xml");
        Check(Eval("2026-11-04 01:00:00", o, s) == Cue.Dismissed && Eval("2026-11-04 07:00:00", o, s, true) == Cue.LightPending, "Legacy state preserves the night decision and supports the new morning cue");
        using (var icon = TrayIcon.CreateSun(System.Drawing.Color.Gold)) Check(icon.Width == 64, "Sun tray icon renders");
        bool before = Theme.IsDark();
        Eval("2026-11-05 07:00:00", o, new NightState(), true);
        Check(Theme.IsDark() == before, "Morning evaluation never changes Windows theme");
    }

    static void CheckSettingsReopen()
    {
        // Reuse the tray-owned icon while recreating a closed WPF/WinForms host.
        // Create the native handle without showing a window or changing Windows settings.
        using (var icon = TrayIcon.Create(System.Drawing.Color.Orange, true))
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                using (var form = new SettingsForm(null, new Options { Language = "en" }, icon, true, false))
                {
                    IntPtr handle = form.Handle;
                    var choice = (System.Windows.Controls.ComboBox)form.View.FindName("LanguageChoice");
                    choice.SelectedValue = "de";
                    form.Close();
                    Check(handle != IntPtr.Zero && form.IsDisposed && L.Current == "en",
                        "Settings closes, restores language and can reopen with shared icon: " + attempt);
                }
            }
        }
        L.Select("system");
    }

    static T Control<T>(SettingsForm form, string name) where T : System.Windows.FrameworkElement
    {
        return (T)form.View.FindName(name);
    }

    static bool Commit(SettingsForm form, string method)
    {
        return (bool)typeof(SettingsForm).GetMethod(method,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(form, null);
    }

    static void Click(SettingsForm form, string name)
    {
        Control<System.Windows.Controls.Button>(form, name).RaiseEvent(
            new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
    }

    sealed class MemorySettingsActions : ISettingsActions
    {
        public Options Stored = new Options { Language = "en" };
        public StartupRegistration Registration = StartupRegistration.Unregistered;
        public StartupRegistration? FailedStartupState;
        public int OptionCalls, StartupCalls, ThemeCalls;
        public bool FailOptionsOnce, FailStartupOnce, ShortcutAvailable = true, LastTheme;
        public bool ApplyOptions(Options next)
        {
            next.Validate();
            OptionCalls++;
            if (FailOptionsOnce) { FailOptionsOnce = false; return false; }
            Stored = next.Copy();
            L.Select(Stored.Language);
            return true;
        }
        public bool SetStartup(bool enabled)
        {
            StartupCalls++;
            if (FailStartupOnce)
            {
                FailStartupOnce = false;
                if (FailedStartupState.HasValue) Registration = FailedStartupState.Value;
                return false;
            }
            Registration = enabled ? StartupRegistration.CurrentPath : StartupRegistration.Unregistered;
            return true;
        }
        public StartupRegistration StartupState { get { return Registration; } }
        public bool HotkeyEnabled { get { return Stored.Hotkey; } }
        public bool HotkeyAvailable { get { return ShortcutAvailable; } }
        public string Warning { get { return ""; } }
        public Cue CurrentCue { get { return Cue.Pending; } }
        public string StatusText() { return L.Text("Pending"); }
        public void ChangeTheme(bool dark) { ThemeCalls++; LastTheme = dark; }
        public void OpenMenuAt(System.Drawing.Point point) { }
    }

    static void CheckAutoSave()
    {
        // Real form event handlers, isolated host: no theme, hotkey or Run-key writes.
        var host = new MemorySettingsActions();
        Options supplied = host.Stored.Copy();
        using (var icon = TrayIcon.Create(System.Drawing.Color.Orange, true))
        {
            using (var form = new SettingsForm(host, supplied, icon))
            {
                var start = Control<System.Windows.Controls.TextBox>(form, "StartTime");
                var end = Control<System.Windows.Controls.TextBox>(form, "EndTime");
                var minutes = Control<System.Windows.Controls.TextBox>(form, "Minutes");
                var language = Control<System.Windows.Controls.ComboBox>(form, "LanguageChoice");
                var morning = Control<System.Windows.Controls.Primitives.ToggleButton>(form, "MorningToggle");
                var startup = Control<System.Windows.Controls.Primitives.ToggleButton>(form, "StartupToggle");
                var hotkey = Control<System.Windows.Controls.Primitives.ToggleButton>(form, "HotkeyToggle");
                var hide = Control<System.Windows.Controls.Button>(form, "HideButton");
                Check(host.OptionCalls == 0 && host.StartupCalls == 0 && host.ThemeCalls == 0,
                    "Opening settings does not commit or perform actions");
                Check(!hide.IsDefault && form.AcceptButton == null,
                    "Enter has no default hide or save-and-close action");

                start.Text = "2";
                Check(host.OptionCalls == 0 && host.Stored.Start == "23:00", "Partial time input is only a draft");
                Check(!Commit(form, "CommitTimes") && host.OptionCalls == 0,
                    "Incomplete time cannot be applied");
                start.Text = "07:00";
                end.Text = "07:00";
                Check(!Commit(form, "CommitTimes") && host.Stored.Start == "23:00" &&
                    Control<System.Windows.Controls.TextBlock>(form, "TimeFeedback").Text == L.Text("InvalidTimes"),
                    "Equal start and end keep the last valid time pair");

                startup.IsChecked = true;
                Check(host.StartupCalls == 1 && host.Registration == StartupRegistration.CurrentPath && host.OptionCalls == 0,
                    "Startup commits independently of invalid time drafts");
                morning.IsChecked = false;
                Check(!host.Stored.MorningReminder && host.Stored.Start == "23:00" && host.Stored.End == "07:00",
                    "Morning toggle saves without copying invalid time drafts");
                language.SelectedValue = "ja";
                Check(host.Stored.Language == "ja" && L.Current == "ja" && !host.Stored.MorningReminder && host.StartupCalls == 1,
                    "Language commits immediately and preserves other settings without touching startup");

                int beforeTyping = host.OptionCalls;
                minutes.Text = "3";
                minutes.Text = "35";
                Check(host.OptionCalls == beforeTyping && host.Stored.SnoozeMinutes == 15,
                    "Typing minutes does not save intermediate values");
                minutes.RaiseEvent(new System.Windows.Input.KeyboardFocusChangedEventArgs(
                    System.Windows.Input.Keyboard.PrimaryDevice, 0, minutes, startup)
                    { RoutedEvent = System.Windows.Input.Keyboard.LostKeyboardFocusEvent });
                Check(host.Stored.SnoozeMinutes == 35 && host.Stored.Start == "23:00",
                    "Minutes commit on focus loss independently of invalid times");
                minutes.Text = "0";
                Check(!Commit(form, "CommitMinutes") && host.Stored.SnoozeMinutes == 35 &&
                    Control<System.Windows.Controls.TextBlock>(form, "MinutesFeedback").Text == L.Text("InvalidSnooze"),
                    "Invalid minutes retain the saved duration with inline feedback");
                Click(form, "Plus");
                Check(host.Stored.SnoozeMinutes == 36 && minutes.Text == "36",
                    "Plus recovers an invalid draft from the committed duration and saves immediately");
                Click(form, "Minus");
                Check(host.Stored.SnoozeMinutes == 35, "Minus saves immediately");

                start.Text = "22：30";
                end.Text = "6:45";
                Check(Commit(form, "CommitTimes") && host.Stored.Start == "22:30" && host.Stored.End == "06:45" &&
                    start.Text == "22:30" && end.Text == "06:45" && !form.IsDisposed,
                    "Completed time pair is normalized and committed without closing settings");
                int afterTimes = host.OptionCalls;
                Commit(form, "CommitTimes");
                Commit(form, "CommitMinutes");
                Check(host.OptionCalls == afterTimes, "Unchanged text commits do not write again");

                host.FailOptionsOnce = true;
                morning.IsChecked = true;
                Check(morning.IsChecked == false && !host.Stored.MorningReminder &&
                    Control<System.Windows.Controls.TextBlock>(form, "MorningFeedback").Visibility == System.Windows.Visibility.Visible,
                    "Failed switch save rolls back the control and exposes the failure");
                host.FailOptionsOnce = true;
                language.SelectedValue = "fr";
                Check(host.Stored.Language == "ja" && L.Current == "ja" &&
                    ((LanguageInfo)language.SelectedItem).Code == "ja",
                    "Failed language save restores the selected language and existing interface");

                host.FailOptionsOnce = true;
                minutes.Text = "40";
                Check(!Commit(form, "CommitMinutes") && host.Stored.SnoozeMinutes == 35,
                    "Failed minute save leaves the effective duration unchanged");
                morning.IsChecked = true;
                Check(host.Stored.MorningReminder && host.Stored.SnoozeMinutes == 35 &&
                    host.Stored.Start == "22:30" && host.Stored.Language == "ja",
                    "Later independent change uses the last successful snapshot, not a failed draft");
                Check(Control<System.Windows.Controls.TextBlock>(form, "MinutesFeedback").Visibility == System.Windows.Visibility.Visible &&
                    Control<System.Windows.Controls.TextBlock>(form, "Feedback").Text == L.Text("SaveFailed"),
                    "An unrelated successful save does not hide an earlier failure");
                minutes.Text = "35";
                Commit(form, "CommitMinutes");

                start.Text = "20:00";
                end.Text = "08:00";
                host.FailOptionsOnce = true;
                Check(!Commit(form, "CommitTimes") && host.Stored.Start == "22:30" && host.Stored.End == "06:45",
                    "Failed time save retains the entire prior pair");
                hotkey.IsChecked = false;
                Check(!host.Stored.Hotkey && host.Stored.Start == "22:30" && host.Stored.End == "06:45",
                    "Hotkey toggle does not apply a failed time draft");
                host.ShortcutAvailable = false;
                hotkey.IsChecked = true;
                Check(host.Stored.Hotkey && Control<System.Windows.Controls.TextBlock>(form, "ShortcutHint").Text == L.Text("HotkeyConflictShort"),
                    "Saved shortcut preference still reports an unavailable hotkey");
                start.Text = "22:30";
                end.Text = "06:45";
                Commit(form, "CommitTimes");

                host.FailStartupOnce = true;
                int optionCalls = host.OptionCalls;
                startup.IsChecked = false;
                Check(startup.IsChecked == true && host.Registration == StartupRegistration.CurrentPath && host.OptionCalls == optionCalls,
                    "Failed startup action rolls back its control without resaving ordinary options");
                startup.IsChecked = false;
                Check(startup.IsChecked == false && host.Registration == StartupRegistration.Unregistered,
                    "Retry can disable startup immediately");

                host.FailStartupOnce = true;
                host.FailedStartupState = StartupRegistration.Unavailable;
                startup.IsChecked = true;
                Check(!startup.IsEnabled && Control<System.Windows.Controls.TextBlock>(form, "StartupStatus").Text == L.Text("StartupUnknown"),
                    "An unverifiable startup rollback shows the current unknown state instead of a guessed switch value");
                int startupCalls = host.StartupCalls;
                typeof(System.Windows.Forms.Form).GetMethod("OnActivated",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(form, new object[] { EventArgs.Empty });
                Check(!startup.IsEnabled && host.StartupCalls == startupCalls, "Refreshing unavailable startup registration never writes it back");
                language.SelectedValue = "de";
                Check(host.Stored.Language == "de" && L.Current == "de",
                    "Ordinary preferences save when startup registration is unavailable");
                Click(form, "DarkButton");
                Check(host.ThemeCalls == 1 && host.LastTheme, "Theme action remains immediate and independent");
                Check(supplied.Start == "23:00" && supplied.Language == "en" && supplied.SnoozeMinutes == 15,
                    "Editing never mutates the caller's configuration snapshot");

                IntPtr handle = form.Handle;
                minutes.Text = "37";
                start.Text = "2";
                host.FailOptionsOnce = true;
                Click(form, "HideButton");
                Check(!form.IsDisposed && host.Stored.SnoozeMinutes == 35 &&
                    Control<System.Windows.Controls.TextBlock>(form, "MinutesFeedback").Visibility == System.Windows.Visibility.Visible,
                    "Closing stays open when a valid pending setting cannot be saved");
                Click(form, "HideButton");
                Check(handle != IntPtr.Zero && form.IsDisposed && host.Stored.Start == "22:30" &&
                    host.Stored.SnoozeMinutes == 37 && host.Stored.Language == "de" && L.Current == "de",
                    "Closing commits valid pending text, discards incomplete time and keeps the saved language");
            }
            int callsBeforeReopen = host.OptionCalls;
            using (var reopened = new SettingsForm(host, host.Stored.Copy(), icon))
            {
                Check(Control<System.Windows.Controls.TextBox>(reopened, "StartTime").Text == "22:30" &&
                    Control<System.Windows.Controls.TextBox>(reopened, "Minutes").Text == "37" &&
                    ((LanguageInfo)Control<System.Windows.Controls.ComboBox>(reopened, "LanguageChoice").SelectedItem).Code == "de" &&
                    host.OptionCalls == callsBeforeReopen,
                    "Reopened settings reflect only committed values without saving during initialization");
            }
        }
        L.Select("system");
    }

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            if (args.Length == 2 && args[0] == "--check-recovery")
            {
                Store.DiagnosticsRoot = args[1];
                return Store.RecoveryNotice ? 0 : 1;
            }
            if (args.Length == 2 && args[0] == "--tray-smoke")
            {
                Store.Root = args[1];
                Directory.CreateDirectory(Store.Root);
                Store.DiagnosticsRoot = Path.Combine(Store.Root, "diagnostics");
                Store.Save("settings.xml", new Options { Hotkey = false });
                string broken = "<NightState>";
                File.WriteAllText(Store.PathFor("state.xml"), broken);
                using (var locked = new FileStream(Store.PathFor("state.xml"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                using (var signal = new EventWaitHandle(false, EventResetMode.AutoReset))
                {
                    var app = new TrayApp(false, false, signal);
                    try
                    {
                        var field = typeof(TrayApp).GetField("tray", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var tray = (System.Windows.Forms.NotifyIcon)field.GetValue(app);
                        Check(tray.Text == L.TrayText("NightCue · " + app.StatusText()) && tray.Text != "NightCue", "Protected state does not block tray presentation");
                        Check(app.Warning.Length > 0, "Protected state reports persistence warning");
                        var optionsField = typeof(TrayApp).GetField("options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        string settingsBefore = File.ReadAllText(Store.PathFor("settings.xml"));
                        var next = ((Options)optionsField.GetValue(app)).Copy();
                        next.SnoozeMinutes = 27;
                        using (var settingsLock = new FileStream(Store.PathFor("settings.xml"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {
                            Check(!app.ApplyOptions(next) && ((Options)optionsField.GetValue(app)).SnoozeMinutes == 15,
                                "Failed options write leaves active settings unchanged");
                        }
                        Check(File.ReadAllText(Store.PathFor("settings.xml")) == settingsBefore,
                            "Failed options write preserves the previous file exactly");
                        Check(app.ApplyOptions(next) && ((Options)optionsField.GetValue(app)).SnoozeMinutes == 27 &&
                            Store.Read<Options>("settings.xml").SnoozeMinutes == 27,
                            "Unlocked settings commit both disk and active configuration");
                        next.SnoozeMinutes = 99;
                        Check(((Options)optionsField.GetValue(app)).SnoozeMinutes == 27 &&
                            Store.Read<Options>("settings.xml").SnoozeMinutes == 27,
                            "Accepted options are copied instead of retaining the caller's mutable object");
                        next = ((Options)optionsField.GetValue(app)).Copy();
                        next.Language = L.Current == "en" ? "ja" : "en";
                        Action failedRefresh = delegate { throw new InvalidOperationException("simulated language refresh failure after persistence"); };
                        L.Changed += failedRefresh;
                        try
                        {
                            Check(app.ApplyOptions(next) && ((Options)optionsField.GetValue(app)).Language == next.Language &&
                                Store.Read<Options>("settings.xml").Language == next.Language,
                                "Refresh failure after persistence does not falsely reject the committed settings");
                            Check(app.Warning == L.Text("ApplyRefreshFailed") && !app.HotkeyEnabled,
                                "Refresh failure remains visible and the smoke test keeps hotkeys disabled");
                        }
                        finally { L.Changed -= failedRefresh; }
                    }
                    finally { app.ExitThread(); }
                }
                Check(File.ReadAllText(Store.PathFor("state.xml")) == broken, "Tray update preserves unreadable original state");
                Console.WriteLine("Tray smoke passed");
                return 0;
            }
            Options o = new Options();
            o.Validate();
            NightState s = new NightState();
            Check(Eval("2026-11-03 22:59:59", o, s) == Cue.Day, "No reminder before 23:00");
            Check(Eval("2026-11-03 23:00:00", o, s) == Cue.Pending, "Reminder at 23:00");
            Check(s.Night == "2026-11-03", "Night identity");
            Check(Eval("2026-11-04 00:00:00", o, s) == Cue.Pending && s.Night == "2026-11-03", "Midnight does not reset choice");
            s.SnoozeUntilUtc = T("2026-11-04 00:05:00").AddHours(-8);
            Check(Eval("2026-11-04 00:04:59", o, s) == Cue.Snoozed, "Snooze stays quiet");
            Check(Eval("2026-11-04 00:05:00", o, s) == Cue.Pending, "Snooze returns on time");
            s.Decision = "light";
            Check(Eval("2026-11-04 06:59:59", o, s) == Cue.Dismissed, "Keep light suppresses rest of night");
            Check(Eval("2026-11-04 07:00:00", o, s) == Cue.Day, "End is exclusive");
            Check(Eval("2026-11-04 23:00:00", o, s) == Cue.Pending, "New night resets decision");
            Check(Eval("2026-11-04 23:01:00", o, s, true) == Cue.Dark, "Existing dark theme clears reminder");
            Check(Eval("2026-11-04 23:02:00", o, s) == Cue.Dismissed, "External return to light does not nag again");
            Check(Eval("2026-11-05 09:00:00", o, s, true) == Cue.LightPending, "Morning offers light mode");
            s = new NightState();
            Eval("2026-11-03 23:50:00", o, s);
            s.SnoozeUntilUtc = T("2026-11-04 00:05:00").AddHours(-8);
            Store.Root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-data");
            Directory.CreateDirectory(Store.Root);
            Store.DiagnosticsRoot = Path.Combine(Store.Root, "diagnostics-" + Guid.NewGuid().ToString("N"));
            Store.Save("state.xml", s);
            NightState loaded = Store.Read<NightState>("state.xml");
            Check(Eval("2026-11-04 00:01:00", o, loaded) == Cue.Snoozed, "Restart preserves snooze across midnight");
            loaded.Decision = "light";
            Store.Save("state.xml", loaded);
            loaded = Store.Read<NightState>("state.xml");
            Check(Eval("2026-11-04 00:02:00", o, loaded) == Cue.Dismissed, "Atomic replacement preserves decision after restart");
            Check(Eval("2026-11-10 23:01:00", o, loaded) == Cue.Pending, "Resume after days gets current night");
            Options day = new Options
            {
                Start = "18:00",
                End = "22:00"
            };
            day.Validate();
            Check(Schedule.NightKey(T("2026-11-03 17:59:00"), day) == null, "Non-overnight before start");
            Check(Schedule.NightKey(T("2026-11-03 18:00:00"), day) == "2026-11-03", "Non-overnight at start");
            Check(Schedule.NightKey(T("2026-11-03 22:00:00"), day) == null, "Non-overnight at end");
            bool rejected = false;
            try
            {
                new Options
                {
                    Start = "07:00",
                    End = "07:00"
                }.Validate();
            }
            catch
            {
                rejected = true;
            }

            Check(rejected, "Reject identical times");
            rejected = false;
            try
            {
                new Options
                {
                    Start = "25:00"
                }.Validate();
            }
            catch
            {
                rejected = true;
            }

            Check(rejected, "Reject invalid time");
            Store.Save("settings.xml", o);
            Check(Store.Read<Options>("settings.xml").Start == "23:00", "Config roundtrip");
            new Options
            {
                SnoozeMinutes = 1
            }.Validate();
            new Options
            {
                SnoozeMinutes = 120
            }.Validate();
            Check(true, "Snooze bounds accepted");
            Rejects<ArgumentException>(delegate
            {
                new Options
                {
                    SnoozeMinutes = 0
                }.Validate();
            }, "Reject zero snooze");
            Rejects<ArgumentException>(delegate
            {
                new Options
                {
                    SnoozeMinutes = 121
                }.Validate();
            }, "Reject excessive snooze");
            Rejects<ArgumentException>(delegate
            {
                new Options
                {
                    Start = null
                }.Validate();
            }, "Reject missing start time");
            CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("th-TH");
                Check(Schedule.NightKey(T("2026-11-04 01:00:00"), o) == "2026-11-03", "Night identity is independent of system calendar");
                NightState priorChoice = new NightState
                {
                    Night = "2026-11-03",
                    Decision = "light"
                };
                Check(Eval("2026-11-04 01:00:00", o, priorChoice) == Cue.Dismissed, "Culture change preserves tonight's decision");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }

            NightState nullDecision = new NightState
            {
                Night = "2026-11-03",
                Decision = null
            };
            Store.Save("null-decision.xml", nullDecision);
            Check(Eval("2026-11-03 23:10:00", o, Store.Read<NightState>("null-decision.xml")) == Cue.Pending, "Null saved decision remains usable");
            File.WriteAllText(Store.PathFor("null-root.xml"), "<Options xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\" />");
            Check(Store.Read<Options>("null-root.xml").Start == "23:00", "Null XML root uses defaults");
            Check(Store.Read<Options>(Guid.NewGuid().ToString("N") + ".xml").Start == "23:00", "Missing configuration uses defaults");
            File.WriteAllText(Store.PathFor("dtd.xml"), "<!DOCTYPE Options [<!ENTITY value '23:00'>]><Options><Start>&value;</Start></Options>");
            Rejects<InvalidOperationException>(delegate
            {
                Store.Read<Options>("dtd.xml");
            }, "Reject DTD configuration");
            File.WriteAllText(Store.PathFor("malformed.xml"), "<Options><Start>");
            Rejects<InvalidOperationException>(delegate
            {
                Store.Read<Options>("malformed.xml");
            }, "Reject malformed XML");
            File.WriteAllText(Store.PathFor("oversized.xml"), "<Options><Start>" + new string ('x', 1048577) + "</Start></Options>");
            Rejects<InvalidOperationException>(delegate
            {
                Store.Read<Options>("oversized.xml");
            }, "Reject oversized configuration");
            CheckLocalization();
            CheckMorning();
            CheckStartup();
            CheckRecovery();
            CheckSettingsReopen();
            CheckAutoSave();
            // Evaluation must not silently change the user's theme.
            bool before = Theme.IsDark();
            Eval("2026-11-03 23:00:00", o, new NightState());
            Check(Theme.IsDark() == before, "Schedule evaluation does not change Windows theme");
            using (var icon = TrayIcon.Create(System.Drawing.Color.FromArgb(245, 180, 60), true))
            {
                Check(icon.Width > 0, "Tray icon generation");
                using (var f = File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NightCue.ico")))
                    icon.Save(f);
            }

            Console.WriteLine(count + " tests passed");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }
}
