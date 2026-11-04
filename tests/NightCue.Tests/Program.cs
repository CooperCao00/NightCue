using System;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Windows.Media.Imaging;
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

    static void CheckView(bool dark)
    {
        using (var icon = TrayApp.MakeIcon(System.Drawing.Color.Orange, true))
        using (var form = new SettingsForm(null, new Options(), icon, true, dark))
        {
            var view = form.View;
            view.Measure(new System.Windows.Size(940, 688));
            view.Arrange(new System.Windows.Rect(0, 0, 940, 688));
            view.UpdateLayout();
            var bitmap = new RenderTargetBitmap(1410, 1032, 144, 144, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(view);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var file = File.Create(Store.PathFor(dark ? "preview-dark.png" : "preview-light.png")))
                encoder.Save(file);
            var input = (System.Windows.Controls.TextBox)view.FindName("StartTime");
            Check(input.Text == "23:00" && view.ActualWidth == 940 && bitmap.PixelWidth == 1410, dark ? "Dark UI loads and renders" : "Light UI loads and renders");
        }
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
            Check(Eval("2026-11-05 09:00:00", o, s, true) == Cue.Dark, "Morning evaluation leaves dark alone");
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
            Rejects<InvalidDataException>(delegate
            {
                new Options
                {
                    SnoozeMinutes = 0
                }.Validate();
            }, "Reject zero snooze");
            Rejects<InvalidDataException>(delegate
            {
                new Options
                {
                    SnoozeMinutes = 121
                }.Validate();
            }, "Reject excessive snooze");
            Rejects<InvalidDataException>(delegate
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
            CheckView(false);
            CheckView(true);
            // Evaluation must not silently change the user's theme.
            bool before = Theme.IsDark();
            Eval("2026-11-03 23:00:00", o, new NightState());
            Check(Theme.IsDark() == before, "Schedule evaluation does not change Windows theme");
            using (var icon = TrayApp.MakeIcon(System.Drawing.Color.FromArgb(245, 180, 60), true))
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
