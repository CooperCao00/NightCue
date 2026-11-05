using System;
using System.Globalization;

namespace NightCue
{
    public class Options
    {
        public string Start = "23:00";
        public string End = "07:00";
        public bool Hotkey = true;
        public int SnoozeMinutes = 15;
        public string Language = "system";
        public bool MorningReminder = true;

        public void Validate()
        {
            TimeSpan start, end;
            if (!TimeSpan.TryParseExact(Start, @"hh\:mm", CultureInfo.InvariantCulture, out start) ||
                !TimeSpan.TryParseExact(End, @"hh\:mm", CultureInfo.InvariantCulture, out end) || start == end)
                throw new ArgumentException("Start and End must be different valid 24-hour times.");
            if (SnoozeMinutes < 1 || SnoozeMinutes > 120)
                throw new ArgumentException("SnoozeMinutes must be between 1 and 120.");
        }
    }
}
