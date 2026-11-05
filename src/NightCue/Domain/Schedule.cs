using System;
using System.Globalization;

namespace NightCue
{
    // Pure clock/state logic: callers provide local time, UTC and observed theme.
    public static class Schedule
    {
        public static string NightKey(DateTime local, Options o)
        {
            TimeSpan start = TimeSpan.ParseExact(o.Start, @"hh\:mm", CultureInfo.InvariantCulture);
            TimeSpan end = TimeSpan.ParseExact(o.End, @"hh\:mm", CultureInfo.InvariantCulture);
            if (start < end)
                return local.TimeOfDay >= start && local.TimeOfDay < end ? DateKey(local) : null;
            if (local.TimeOfDay >= start) return DateKey(local);
            return local.TimeOfDay < end ? DateKey(local.AddDays(-1)) : null;
        }

        public static string DayKey(DateTime local, Options o)
        {
            if (NightKey(local, o) != null) return null;
            TimeSpan end = TimeSpan.ParseExact(o.End, @"hh\:mm", CultureInfo.InvariantCulture);
            return DateKey(local.TimeOfDay < end ? local.AddDays(-1) : local);
        }

        static string DateKey(DateTime value) { return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }

        // Initialize the period before commands too: a click may cross a boundary
        // between the menu's last refresh and its action.
        static bool Prepare(DateTime local, Options o, NightState state)
        {
            string night = NightKey(local, o);
            if (night != null)
            {
                if (state.Night != night)
                {
                    state.Night = night;
                    state.Decision = "";
                    state.SnoozeUntilUtc = DateTime.MinValue;
                }
                return true;
            }
            string day = DayKey(local, o);
            if (state.Day != day)
            {
                state.Day = day;
                state.DayDecision = "";
                state.DaySnoozeUntilUtc = DateTime.MinValue;
            }
            return false;
        }

        public static Cue Evaluate(DateTime local, DateTime utc, Options o, NightState state, bool dark)
        {
            if (Prepare(local, o, state))
            {
                if (dark)
                {
                    state.Decision = "dark";
                    state.SnoozeUntilUtc = DateTime.MinValue;
                    return Cue.Dark;
                }
                if (!String.IsNullOrEmpty(state.Decision)) return Cue.Dismissed;
                return utc < state.SnoozeUntilUtc ? Cue.Snoozed : Cue.Pending;
            }
            if (!o.MorningReminder) return dark ? Cue.Dark : Cue.Day;
            if (!dark)
            {
                state.DayDecision = "light";
                state.DaySnoozeUntilUtc = DateTime.MinValue;
                return Cue.Day;
            }
            if (!String.IsNullOrEmpty(state.DayDecision)) return Cue.LightDismissed;
            return utc < state.DaySnoozeUntilUtc ? Cue.LightSnoozed : Cue.LightPending;
        }

        public static void RecordDecision(DateTime local, Options o, NightState state, bool dark)
        {
            if (Prepare(local, o, state))
            {
                state.Decision = dark ? "dark" : "light";
                state.SnoozeUntilUtc = DateTime.MinValue;
            }
            else
            {
                state.DayDecision = dark ? "dark" : "light";
                state.DaySnoozeUntilUtc = DateTime.MinValue;
            }
        }

        public static void Snooze(DateTime local, DateTime utc, Options o, NightState state)
        {
            if (Prepare(local, o, state))
            {
                state.Decision = "";
                state.SnoozeUntilUtc = utc.AddMinutes(o.SnoozeMinutes);
            }
            else if (o.MorningReminder)
            {
                state.DayDecision = "";
                state.DaySnoozeUntilUtc = utc.AddMinutes(o.SnoozeMinutes);
            }
        }

        public static void Restore(DateTime local, Options o, NightState state)
        {
            if (Prepare(local, o, state))
            {
                state.Decision = "";
                state.SnoozeUntilUtc = DateTime.MinValue;
            }
            else
            {
                state.DayDecision = "";
                state.DaySnoozeUntilUtc = DateTime.MinValue;
            }
        }
    }
}
