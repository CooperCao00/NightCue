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
            using (var file = File.Create(Store.PathFor("preview-" + locale + (dark ? "-dark.png" : "-light.png"))))
                encoder.Save(file);
            var input = (System.Windows.Controls.TextBox)view.FindName("StartTime");
            var save = (System.Windows.Controls.Button)view.FindName("SaveButton");
            Check(input.Text == "23:00" && Math.Abs(view.ActualWidth - 1000) < 1 && save.Content.ToString() == L.TextFor(locale, "Save"),
                locale + (dark ? " dark" : " light") + " UI loads, localizes and renders");
            CheckTextLayout(view, locale);
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
        Check(L.Resolve("unknown", "zh-CN") == "en" && L.TextFor("unknown", "Save") == baseline["Save"], "Unsupported language falls back to English");
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
            var save = (System.Windows.Controls.Button)form.View.FindName("SaveButton");
            choice.SelectedValue = "de";
            form.View.Measure(new System.Windows.Size(1000, 790));
            form.View.Arrange(new System.Windows.Rect(0, 0, 1000, 790));
            form.View.UpdateLayout();
            Check(L.Current == "de" && save.Content.ToString() == L.Text("Save"), "Language selection updates existing view immediately");
            var input = (System.Windows.Controls.TextBox)form.View.FindName("StartTime");
            input.Text = "bad";
            save.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check(((System.Windows.Controls.TextBlock)form.View.FindName("Feedback")).Text == L.Text("InvalidStart"), "Validation message uses selected language");
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
        public bool IgnoreNextWrite;
        public string Read() { return Value; }
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

    [STAThread]
    static int Main()
    {
        try
        {
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
            CheckSettingsReopen();
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
