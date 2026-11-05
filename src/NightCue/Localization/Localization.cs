using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace NightCue
{
    public sealed class LanguageInfo
    {
        public string Code { get; internal set; }
        public string Name { get; internal set; }
        public string Font { get; internal set; }
        public Dictionary<string, string> Strings { get; internal set; }
        public override string ToString() { return Name; }
    }

    // All translations are embedded in the same executable. No download or
    // installation is needed when selecting another language.
    public static class L
    {
        static readonly Dictionary<string, LanguageInfo> catalog = Load();
        static string current = Resolve("system", CultureInfo.CurrentUICulture.Name);
        public static event Action Changed;
        public static string Current { get { return current; } }
        public static LanguageInfo CurrentLanguage { get { return catalog[current]; } }
        public static IEnumerable<LanguageInfo> Languages { get { return catalog.Values; } }
        public static bool IsSupported(string code) { return code != null && catalog.ContainsKey(code); }

        static Dictionary<string, LanguageInfo> Load()
        {
            var result = new Dictionary<string, LanguageInfo>(StringComparer.OrdinalIgnoreCase);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] names = assembly.GetManifestResourceNames();
            Array.Sort(names, StringComparer.Ordinal);
            foreach (string name in names)
            {
                if (!name.StartsWith("NightCue.Languages.", StringComparison.Ordinal) || !name.EndsWith(".xml", StringComparison.Ordinal)) continue;
                var document = new XmlDocument { XmlResolver = null };
                using (Stream stream = assembly.GetManifestResourceStream(name))
                using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                    document.Load(reader);
                XmlElement root = document.DocumentElement;
                var info = new LanguageInfo
                {
                    Code = root.GetAttribute("code"), Name = root.GetAttribute("name"),
                    Font = root.GetAttribute("font"), Strings = new Dictionary<string, string>(StringComparer.Ordinal)
                };
                foreach (XmlNode node in root.SelectNodes("string"))
                    info.Strings.Add(node.Attributes["key"].Value, node.InnerText);
                result.Add(info.Code, info);
            }
            if (!result.ContainsKey("en")) throw new InvalidDataException("The English language resource is missing.");
            return result;
        }

        public static string Resolve(string preference, string systemLanguage)
        {
            string code = String.Equals(preference, "system", StringComparison.OrdinalIgnoreCase) || String.IsNullOrEmpty(preference)
                ? systemLanguage : preference;
            code = (code ?? "en").Replace('_', '-');
            if (catalog.ContainsKey(code)) return catalog[code].Code;
            if (code.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                string value = code.ToLowerInvariant();
                return value.Contains("hant") || value == "zh-tw" || value == "zh-hk" || value == "zh-mo" ? "zh-TW" : "zh-CN";
            }
            // A generic Portuguese fallback uses the available Brazilian translation.
            if (code.StartsWith("pt-", StringComparison.OrdinalIgnoreCase) || String.Equals(code, "pt", StringComparison.OrdinalIgnoreCase)) return "pt-BR";
            int separator = code.IndexOf('-');
            string parent = separator < 0 ? code : code.Substring(0, separator);
            return catalog.ContainsKey(parent) ? catalog[parent].Code : "en";
        }

        public static void Select(string preference)
        {
            string next = Resolve(preference, CultureInfo.CurrentUICulture.Name);
            if (current == next) return;
            current = next;
            if (Changed != null) Changed();
        }

        public static string Text(string key) { return TextFor(current, key); }
        public static string TextFor(string code, string key)
        {
            string value;
            LanguageInfo language;
            if (catalog.TryGetValue(code, out language) && language.Strings.TryGetValue(key, out value)) return value;
            if (catalog["en"].Strings.TryGetValue(key, out value)) return value;
            throw new KeyNotFoundException("Unknown translation key: " + key);
        }
        public static string Format(string key, params object[] values)
        {
            return String.Format(CultureInfo.InvariantCulture, Text(key), values);
        }
        public static string Status(Cue cue, string start, DateTime snoozeUtc, bool night)
        {
            switch (cue)
            {
                case Cue.LightPending: return Text("LightPending");
                case Cue.LightSnoozed: return Format("LightSnoozed", snoozeUtc.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture));
                case Cue.LightDismissed: return Text("LightDismissed");
                case Cue.Pending: return Text("Pending");
                case Cue.Snoozed: return Format("Snoozed", snoozeUtc.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture));
                case Cue.Dark: return Text(night ? "DarkNight" : "DarkDay");
                case Cue.Dismissed: return Text("Dismissed");
                default: return Format("Starts", start);
            }
        }
        public static string Heading(Cue cue)
        {
            switch (cue)
            {
                case Cue.LightPending: return Text("HeadingLightPending");
                case Cue.LightSnoozed:
                case Cue.Snoozed: return Text("HeadingSnoozed");
                case Cue.LightDismissed: return Text("HeadingLightDismissed");
                case Cue.Dark: return Text("HeadingDark");
                case Cue.Dismissed: return Text("HeadingDismissed");
                case Cue.Day: return Text("HeadingDay");
                default: return Text("HeadingPending");
            }
        }
        // NotifyIcon.Text is limited to 63 UTF-16 code units in .NET Framework.
        public static string TrayText(string text)
        {
            if (text.Length <= 63) return text;
            int end = 62;
            if (Char.IsHighSurrogate(text[end - 1])) end--;
            return text.Substring(0, end) + "…";
        }
    }
}
