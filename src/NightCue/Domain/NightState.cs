using System;

namespace NightCue
{
    // Keep the original type and XML fields for portable configuration compatibility.
    public class NightState
    {
        public string Night = "";
        public string Decision = "";
        public DateTime SnoozeUntilUtc = DateTime.MinValue;
        public string Day = "";
        public string DayDecision = "";
        public DateTime DaySnoozeUntilUtc = DateTime.MinValue;
    }
}
