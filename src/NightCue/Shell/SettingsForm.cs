using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using W = System.Windows;
using C = System.Windows.Controls;
using M = System.Windows.Media;
using B = System.Windows.Controls.Primitives;

namespace NightCue
{
    public class SettingsForm : Form
    {
        readonly TrayApp app;
        readonly bool preview;
        readonly string originalLanguage;
        bool previewDark, saved, disposed, changingLanguage;
        bool? lastDark;
        readonly W.FrameworkElement view, scene;
        readonly C.TextBox start, end, minutes;
        readonly B.ToggleButton startup, hotkey, morning;
        readonly C.TextBlock status, heading, feedback, shortcut;
        readonly C.Button light, dark;
        readonly C.ComboBox language;
        readonly LanguageInfo systemChoice;
        string warning = "";
        public W.FrameworkElement View { get { return view; } }
        public SettingsForm(TrayApp owner, Options options, Icon icon) : this(owner, options, icon, false, false) { }

        public SettingsForm(TrayApp owner, Options options, Icon icon, bool isPreview, bool darkPreview)
        {
            app = owner;
            preview = isPreview;
            previewDark = darkPreview;
            originalLanguage = options.Language;
            L.Select(options.Language);
            Icon = icon;
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            float dpi;
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero)) dpi = graphics.DpiX / 96f;
            float scale = Math.Min(dpi, Math.Min((Screen.PrimaryScreen.WorkingArea.Width - 80f) / 1000f, (Screen.PrimaryScreen.WorkingArea.Height - 80f) / 790f));
            ClientSize = new Size((int)(1000 * scale), (int)(790 * scale));
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NightCue.SettingsView.xaml"))
            {
                if (stream == null) throw new FileNotFoundException(L.Text("MissingResource"));
                view = (W.FrameworkElement)System.Windows.Markup.XamlReader.Load(stream);
            }
            scene = (W.FrameworkElement)view.FindName("Scene");
            Controls.Add(new ElementHost { Dock = DockStyle.Fill, Child = view });
            start = Find<C.TextBox>("StartTime"); end = Find<C.TextBox>("EndTime"); minutes = Find<C.TextBox>("Minutes");
            startup = Find<B.ToggleButton>("StartupToggle"); hotkey = Find<B.ToggleButton>("HotkeyToggle");
            morning = Find<B.ToggleButton>("MorningToggle");
            status = Find<C.TextBlock>("StatusText"); heading = Find<C.TextBlock>("StatusHeading");
            feedback = Find<C.TextBlock>("Feedback"); shortcut = Find<C.TextBlock>("ShortcutHint");
            light = Find<C.Button>("LightButton"); dark = Find<C.Button>("DarkButton");
            language = Find<C.ComboBox>("LanguageChoice");
            start.Text = options.Start; end.Text = options.End; minutes.Text = options.SnoozeMinutes.ToString();
            startup.IsChecked = !preview && Startup.Registered; hotkey.IsChecked = options.Hotkey;
            morning.IsChecked = options.MorningReminder;

            systemChoice = new LanguageInfo { Code = "system", Name = L.Text("SystemDefault") };
            language.DisplayMemberPath = "Name";
            language.SelectedValuePath = "Code";
            language.Items.Add(systemChoice);
            foreach (LanguageInfo info in L.Languages) if (info.Code == "en") language.Items.Add(info);
            foreach (LanguageInfo info in L.Languages) if (info.Code != "en") language.Items.Add(info);
            language.SelectedValue = String.IsNullOrEmpty(options.Language) || String.Equals(options.Language, "system", StringComparison.OrdinalIgnoreCase)
                ? "system" : L.Resolve(options.Language, "en");
            language.SelectionChanged += delegate(object sender, C.SelectionChangedEventArgs args)
            {
                if (changingLanguage || args.AddedItems.Count == 0) return;
                LanguageInfo selected = args.AddedItems[0] as LanguageInfo;
                if (selected == null) return;
                L.Select(selected.Code);
                feedback.Text = L.Text("LanguageHint");
            };
            light.Click += delegate { SetTheme(false); };
            dark.Click += delegate { SetTheme(true); };
            Find<C.Button>("Minus").Click += delegate { Step(-1); };
            Find<C.Button>("Plus").Click += delegate { Step(1); };
            Find<C.Button>("SaveButton").Click += delegate { Save(); };
            C.Button more = Find<C.Button>("MoreButton");
            more.Click += delegate
            {
                if (preview) { feedback.Text = L.Text("PreviewNotice"); return; }
                W.Point point = more.PointToScreen(new W.Point(0, more.ActualHeight + 6));
                app.OpenMenuAt(new Point((int)point.X, (int)point.Y));
            };
            start.TextChanged += delegate { UpdateEndLabel(); feedback.Text = warning; };
            end.TextChanged += delegate { UpdateEndLabel(); feedback.Text = warning; };
            minutes.TextChanged += delegate { feedback.Text = warning; };
            view.PreviewKeyDown += delegate(object sender, System.Windows.Input.KeyEventArgs args)
            {
                // The first Escape closes an open language list; the next closes settings.
                if (args.Key == System.Windows.Input.Key.Escape && !language.IsDropDownOpen)
                { Close(); args.Handled = true; }
            };
            Shown += delegate { ApplyChrome(); Find<C.Button>("SaveButton").Focus(); };
            L.Changed += ApplyLanguage;
            ApplyLanguage();
        }

        T Find<T>(string name) where T : W.FrameworkElement { return (T)view.FindName(name); }
        static M.SolidColorBrush Brush(string color) { return new M.SolidColorBrush((M.Color)M.ColorConverter.ConvertFromString(color)); }

        void ApplyLanguage()
        {
            changingLanguage = true;
            try
            {
                foreach (LanguageInfo info in L.Languages)
                    if (info.Code == "en") foreach (string key in info.Strings.Keys) scene.Resources["T." + key] = L.Text(key);
                scene.Resources["UiFont"] = new M.FontFamily(L.CurrentLanguage.Font + ", Global User Interface");
                view.Language = System.Windows.Markup.XmlLanguage.GetLanguage(L.Current);
                Text = L.Text("SettingsTitle");
                systemChoice.Name = L.Text("SystemDefault");
                language.Items.Refresh();
                feedback.Text = !preview && Startup.NeedsRepair ? L.Text("StartupRepair") : L.Text("TrayHint");
                lastDark = null;
                UpdateEndLabel();
                UpdateStatus();
            }
            finally { changingLanguage = false; }
        }

        void SetTheme(bool value)
        {
            if (preview) { previewDark = value; UpdateStatus(); }
            else app.ChangeTheme(value);
        }
        void Step(int delta)
        {
            int value;
            if (!Int32.TryParse(minutes.Text, out value)) value = 15;
            minutes.Text = Math.Max(1, Math.Min(120, value + delta)).ToString();
        }
        static bool ParseTime(string text, out TimeSpan value)
        {
            return TimeSpan.TryParseExact(text.Trim().Replace('：', ':'), new string[] { @"h\:mm", @"hh\:mm" }, System.Globalization.CultureInfo.InvariantCulture, out value) && value.TotalHours < 24;
        }
        void UpdateEndLabel()
        {
            TimeSpan first, last;
            Find<C.TextBlock>("EndLabel").Text = L.Text(ParseTime(start.Text, out first) && ParseTime(end.Text, out last) && last < first ? "EndNext" : "End");
        }
        void Save()
        {
            TimeSpan first, last; int duration;
            if (!ParseTime(start.Text, out first)) { feedback.Text = L.Text("InvalidStart"); start.Focus(); return; }
            if (!ParseTime(end.Text, out last)) { feedback.Text = L.Text("InvalidEnd"); end.Focus(); return; }
            if (first == last) { feedback.Text = L.Text("InvalidTimes"); end.Focus(); return; }
            if (!Int32.TryParse(minutes.Text, out duration) || duration < 1 || duration > 120)
            { feedback.Text = L.Text("InvalidSnooze"); minutes.Focus(); return; }
            var next = new Options
            {
                Start = first.ToString(@"hh\:mm"), End = last.ToString(@"hh\:mm"),
                SnoozeMinutes = duration, Hotkey = hotkey.IsChecked == true, MorningReminder = morning.IsChecked == true,
                Language = language.SelectedItem == null ? "system" : ((LanguageInfo)language.SelectedItem).Code
            };
            if (preview) { feedback.Text = L.Text("PreviewValid"); return; }
            if (app.ApplyOptions(next, startup.IsChecked == true)) { saved = true; Close(); }
        }

        void ApplyPalette(bool isDark)
        {
            if (lastDark == isDark) return;
            lastDark = isDark;
            string[] names = { "Page", "Card", "Ink", "Muted", "Line", "Field", "Accent", "Tint", "Hover", "Track" };
            string[] colors = isDark
                ? new string[] { "#181C2A", "#222738", "#EDF0FA", "#989FB8", "#30364B", "#1B2030", "#A394F3", "#302D49", "#38334E", "#495168" }
                : new string[] { "#F6F7FB", "#FFFFFF", "#242737", "#777E92", "#E7E9F1", "#F7F8FC", "#7868DE", "#F0EDFF", "#EEEAFB", "#DADDE8" };
            for (int i = 0; i < names.Length; i++) scene.Resources[names[i]] = Brush(colors[i]);
            BackColor = isDark ? Color.FromArgb(24, 28, 42) : Color.FromArgb(246, 247, 251);
            C.Button selected = isDark ? dark : light, other = isDark ? light : dark;
            selected.SetResourceReference(C.Button.BackgroundProperty, "Tint"); selected.SetResourceReference(C.Button.BorderBrushProperty, "Accent");
            other.SetResourceReference(C.Button.BackgroundProperty, "Card"); other.SetResourceReference(C.Button.BorderBrushProperty, "Line");
            Find<C.TextBlock>("LightCaption").Text = L.Text(isDark ? "LightIdle" : "LightActive");
            Find<C.TextBlock>("DarkCaption").Text = L.Text(isDark ? "DarkActive" : "DarkIdle");
            ApplyChrome();
        }
        void ApplyChrome()
        {
            if (IsHandleCreated) WindowChrome.Apply(Handle, lastDark == true, BackColor);
        }
        public void UpdateStatus()
        {
            bool isDark = preview ? previewDark : Theme.IsDark();
            Cue current = preview ? (isDark ? Cue.Dark : Cue.Pending) : app.CurrentCue;
            status.Text = preview ? L.Status(current, start.Text, DateTime.MinValue, true) : app.StatusText();
            warning = preview ? "" : app.Warning;
            if (warning.Length > 0) feedback.Text = warning;
            ApplyPalette(isDark);
            heading.Text = L.Heading(current);
            M.SolidColorBrush accent = Brush(isDark ? "#B5B3F1" : "#E8C58D");
            heading.Foreground = accent; Find<System.Windows.Shapes.Ellipse>("StatusDot").Fill = accent;
            bool enabled = preview ? hotkey.IsChecked == true : app.HotkeyEnabled;
            bool available = preview || app.HotkeyAvailable;
            shortcut.Text = !enabled ? L.Text("HotkeyOff") : available ? "Ctrl + Alt + Shift + D" : L.Text("HotkeyConflictShort");
            shortcut.ToolTip = enabled && !available ? L.Text("HotkeyConflict") : shortcut.Text;
        }
        protected override void Dispose(bool disposing)
        {
            bool restoreLanguage = disposing && !disposed && !saved;
            if (disposing && !disposed) { disposed = true; L.Changed -= ApplyLanguage; }
            base.Dispose(disposing);
            // Preview and real settings follow the same cancellation semantics.
            if (restoreLanguage) L.Select(originalLanguage);
        }
    }
}
