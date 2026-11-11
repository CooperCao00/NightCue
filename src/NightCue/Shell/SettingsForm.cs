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
    public interface ISettingsActions
    {
        bool ApplyOptions(Options next);
        bool SetStartup(bool enabled);
        StartupRegistration StartupState { get; }
        bool HotkeyEnabled { get; }
        bool HotkeyAvailable { get; }
        string Warning { get; }
        Cue CurrentCue { get; }
        string StatusText();
        void ChangeTheme(bool dark);
        void OpenMenuAt(Point point);
    }

    public class SettingsForm : Form
    {
        readonly ISettingsActions app;
        readonly bool preview;
        readonly string originalLanguage;
        bool previewDark, disposed, changingLanguage, initializing = true, updatingControls, closing;
        Options committed;
        readonly Timer feedbackTimer = new Timer { Interval = 2200 };
        readonly System.Collections.Generic.HashSet<string> saveErrors = new System.Collections.Generic.HashSet<string>();
        string feedbackKey = "AutoSaveHint", timeError = "", minutesError = "";
        bool? lastDark;
        StartupRegistration startupRegistration;
        readonly W.FrameworkElement view, scene;
        readonly C.TextBox start, end, minutes;
        readonly B.ToggleButton startup, hotkey, morning;
        readonly C.TextBlock status, heading, feedback, shortcut;
        readonly C.Button light, dark;
        readonly C.ComboBox language;
        readonly LanguageInfo systemChoice;
        string warning = "";
        public W.FrameworkElement View { get { return view; } }
        public SettingsForm(ISettingsActions owner, Options options, Icon icon) : this(owner, options, icon, false, false) { }

        public SettingsForm(ISettingsActions owner, Options options, Icon icon, bool isPreview, bool darkPreview)
        {
            app = owner;
            preview = isPreview;
            previewDark = darkPreview;
            originalLanguage = options.Language;
            committed = options.Copy();
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
            startupRegistration = preview ? StartupRegistration.Unregistered : app.StartupState;
            SetStartupControl();
            hotkey.IsChecked = options.Hotkey;
            morning.IsChecked = options.MorningReminder;
            startup.Checked += delegate { ChangeStartup(startup.IsChecked == true); };
            startup.Unchecked += delegate { ChangeStartup(startup.IsChecked == true); };
            hotkey.Checked += delegate { ChangeToggle("Hotkey", hotkey.IsChecked == true); };
            hotkey.Unchecked += delegate { ChangeToggle("Hotkey", hotkey.IsChecked == true); };
            morning.Checked += delegate { ChangeToggle("Morning", morning.IsChecked == true); };
            morning.Unchecked += delegate { ChangeToggle("Morning", morning.IsChecked == true); };
            Find<C.Button>("StartupWindowsButton").Click += delegate
            {
                if (startupRegistration == StartupRegistration.DifferentPath) ChangeStartup(true);
                else OpenLocation("ms-settings:startupapps");
            };
            Find<C.Button>("DiagnosticsButton").Click += delegate { OpenLocation(Store.DiagnosticDirectory); };
            Find<C.Button>("RecoveryDismissButton").Click += delegate
            {
                if (preview) { feedback.Text = L.Text("PreviewNotice"); return; }
                Store.AcknowledgeRecovery();
                UpdateRecoveryNotice();
            };
            Activated += delegate
            {
                if (!preview) startupRegistration = app.StartupState;
                SetStartupControl();
                UpdateStartupStatus();
            };

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
                if (initializing || updatingControls || changingLanguage || args.AddedItems.Count == 0) return;
                LanguageInfo selected = args.AddedItems[0] as LanguageInfo;
                if (selected == null || selected.Code == committed.Language) return;
                Options next = committed.Copy();
                next.Language = selected.Code;
                if (!ApplyEdit(next, "Language"))
                {
                    updatingControls = true;
                    try { language.SelectedValue = String.Equals(committed.Language, "system", StringComparison.OrdinalIgnoreCase) ? "system" : L.Resolve(committed.Language, "en"); }
                    finally { updatingControls = false; }
                }
            };
            light.Click += delegate { SetTheme(false); };
            dark.Click += delegate { SetTheme(true); };
            Find<C.Button>("Minus").Click += delegate { Step(-1); };
            Find<C.Button>("Plus").Click += delegate { Step(1); };
            Find<C.Button>("HideButton").Click += delegate { Close(); };
            C.Button more = Find<C.Button>("MoreButton");
            more.Click += delegate
            {
                if (preview) { feedback.Text = L.Text("PreviewNotice"); return; }
                W.Point point = more.PointToScreen(new W.Point(0, more.ActualHeight + 6));
                app.OpenMenuAt(new Point((int)point.X, (int)point.Y));
            };
            start.TextChanged += delegate { timeError = ""; UpdateEndLabel(); RenderEditFeedback(); };
            end.TextChanged += delegate { timeError = ""; UpdateEndLabel(); RenderEditFeedback(); };
            minutes.TextChanged += delegate { minutesError = ""; RenderEditFeedback(); };
            W.FrameworkElement timeGroup = Find<W.FrameworkElement>("TimeGroup");
            timeGroup.LostKeyboardFocus += delegate
            {
                view.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (!disposed && !closing && !timeGroup.IsKeyboardFocusWithin) CommitTimes();
                }));
            };
            minutes.LostKeyboardFocus += delegate { if (!closing) CommitMinutes(); };
            view.PreviewKeyDown += delegate(object sender, System.Windows.Input.KeyEventArgs args)
            {
                if (args.Key == System.Windows.Input.Key.Enter)
                {
                    if (timeGroup.IsKeyboardFocusWithin) { CommitTimes(); args.Handled = true; }
                    else if (minutes.IsKeyboardFocusWithin) { CommitMinutes(); args.Handled = true; }
                }
                if (args.Key == System.Windows.Input.Key.Escape && !language.IsDropDownOpen)
                { Close(); args.Handled = true; }
            };
            FormClosing += delegate(object sender, FormClosingEventArgs args)
            {
                closing = true;
                bool timesAccepted = CommitTimes(), minutesAccepted = CommitMinutes();
                // Incomplete drafts may be dismissed. A valid edit that could not
                // be persisted stays visible so the user can correct or retry it.
                bool saveFailed = (!timesAccepted && timeError.Length == 0 && saveErrors.Contains("Time")) ||
                    (!minutesAccepted && minutesError.Length == 0 && saveErrors.Contains("Minutes"));
                if (saveFailed && args.CloseReason != CloseReason.WindowsShutDown && args.CloseReason != CloseReason.ApplicationExitCall)
                {
                    args.Cancel = true;
                    closing = false;
                }
            };
            Shown += delegate { ApplyChrome(); Find<C.Button>("HideButton").Focus(); };
            feedbackTimer.Tick += delegate { feedbackTimer.Stop(); feedbackKey = "AutoSaveHint"; RenderEditFeedback(); };
            L.Changed += ApplyLanguage;
            initializing = false;
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
                RenderEditFeedback();
                UpdateStartupStatus();
                UpdateRecoveryNotice();
                lastDark = null;
                UpdateEndLabel();
                UpdateStatus();
            }
            finally { changingLanguage = false; }
        }

        void SetStartupControl()
        {
            updatingControls = true;
            try
            {
                startup.IsEnabled = startupRegistration != StartupRegistration.Unavailable;
                startup.IsChecked = startupRegistration == StartupRegistration.CurrentPath || startupRegistration == StartupRegistration.DifferentPath;
            }
            finally { updatingControls = false; }
        }
        void UpdateStartupStatus()
        {
            string key = startupRegistration == StartupRegistration.Unavailable ? "StartupUnknown" :
                startupRegistration == StartupRegistration.CurrentPath ? "StartupRegistered" :
                startupRegistration == StartupRegistration.DifferentPath ? "StartupRepair" : "StartupUnregistered";
            Find<C.TextBlock>("StartupStatus").Text = L.Text(key);
            Find<C.TextBlock>("StartupStatus").ToolTip = saveErrors.Contains("Startup") ? L.Text("StartupUpdateFailed") : null;
            Find<C.TextBlock>("StartupStatus").Foreground = saveErrors.Contains("Startup") ? Brush("#DB866E") : (M.Brush)scene.Resources["Muted"];
            Find<C.Button>("StartupWindowsButton").Content = L.Text(startupRegistration == StartupRegistration.DifferentPath ? "RepairStartup" : "StartupWindows");
        }
        void ChangeStartup(bool enabled)
        {
            if (initializing || updatingControls) return;
            bool success = preview || app.SetStartup(enabled);
            if (success)
            {
                startupRegistration = preview ? (enabled ? StartupRegistration.CurrentPath : StartupRegistration.Unregistered) : app.StartupState;
                saveErrors.Remove("Startup");
                ShowSaved();
            }
            else
            {
                startupRegistration = app.StartupState;
                saveErrors.Add("Startup");
                RenderEditFeedback();
            }
            SetStartupControl();
            UpdateStartupStatus();
        }
        void ChangeToggle(string field, bool enabled)
        {
            if (initializing || updatingControls) return;
            Options next = committed.Copy();
            if (field == "Hotkey") next.Hotkey = enabled;
            else next.MorningReminder = enabled;
            if (!ApplyEdit(next, field))
            {
                updatingControls = true;
                try
                {
                    if (field == "Hotkey") hotkey.IsChecked = committed.Hotkey;
                    else morning.IsChecked = committed.MorningReminder;
                }
                finally { updatingControls = false; }
            }
            UpdateStatus();
        }
        bool ApplyEdit(Options next, string field)
        {
            if (!preview && !app.ApplyOptions(next))
            {
                saveErrors.Add(field);
                RenderEditFeedback();
                return false;
            }
            committed = next.Copy();
            saveErrors.Remove(field);
            if (preview) L.Select(committed.Language);
            ShowSaved();
            UpdateStatus();
            return true;
        }
        void ShowSaved()
        {
            feedbackKey = "Saved";
            feedbackTimer.Stop();
            feedbackTimer.Start();
            RenderEditFeedback();
        }
        void RenderEditFeedback()
        {
            if (initializing) return;
            feedback.Text = saveErrors.Count > 0 ? L.Text(saveErrors.Count == 1 && saveErrors.Contains("Startup") ? "StartupUpdateFailed" : "SaveFailed") : warning.Length > 0 ? warning : L.Text(feedbackKey);
            feedback.Foreground = saveErrors.Count > 0 || warning.Length > 0 ? Brush("#DB866E") : (M.Brush)scene.Resources["Muted"];
            Find<C.TextBlock>("TimeFeedback").Text = L.Text(timeError.Length > 0 ? timeError : saveErrors.Contains("Time") ? "SaveFailed" : "TimeHint");
            Find<C.TextBlock>("TimeFeedback").Foreground = timeError.Length > 0 || saveErrors.Contains("Time") ? Brush("#DB866E") : (M.Brush)scene.Resources["Muted"];
            SetRowFeedback("Minutes", minutesError.Length > 0 ? minutesError : saveErrors.Contains("Minutes") ? "SaveFailedShort" : "");
            foreach (string field in new[] { "Language", "Morning", "Hotkey" })
                SetRowFeedback(field, saveErrors.Contains(field) ? "SaveFailedShort" : "");
        }
        void SetRowFeedback(string field, string key)
        {
            C.TextBlock text = Find<C.TextBlock>(field + "Feedback");
            text.Text = key.Length == 0 ? "" : L.Text(key);
            text.Visibility = key.Length == 0 ? W.Visibility.Collapsed : W.Visibility.Visible;
            text.ToolTip = saveErrors.Contains(field) ? L.Text("SaveFailed") : text.Text;
        }
        void UpdateRecoveryNotice()
        {
            bool recovered = Store.RecoveryNotice;
            C.TextBlock body = Find<C.TextBlock>("HeroBody");
            body.Text = L.Text(recovered ? "RecoveryNotice" : "HeroBody");
            body.FontSize = recovered ? 12 : 13;
            body.LineHeight = recovered ? 19 : 24;
            Find<C.Button>("RecoveryDismissButton").Visibility = recovered ? W.Visibility.Visible : W.Visibility.Collapsed;
        }
        void OpenLocation(string location)
        {
            if (preview) { feedback.Text = L.Text("PreviewNotice"); return; }
            try
            {
                if (location == Store.DiagnosticDirectory) Directory.CreateDirectory(location);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(location) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Store.Log(e);
                feedback.Text = L.Format("StartupOpenFailed", location);
            }
        }

        void SetTheme(bool value)
        {
            if (preview) { previewDark = value; UpdateStatus(); }
            else app.ChangeTheme(value);
        }
        void Step(int delta)
        {
            int value;
            if (!Int32.TryParse(minutes.Text, out value) || value < 1 || value > 120) value = committed.SnoozeMinutes;
            minutes.Text = Math.Max(1, Math.Min(120, value + delta)).ToString();
            CommitMinutes();
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
        bool CommitTimes()
        {
            TimeSpan first, last;
            if (!ParseTime(start.Text, out first)) timeError = "InvalidStart";
            else if (!ParseTime(end.Text, out last)) timeError = "InvalidEnd";
            else if (first == last) timeError = "InvalidTimes";
            else
            {
                timeError = "";
                Options next = committed.Copy();
                next.Start = first.ToString(@"hh\:mm");
                next.End = last.ToString(@"hh\:mm");
                if (next.Start == committed.Start && next.End == committed.End)
                {
                    saveErrors.Remove("Time");
                    start.Text = next.Start; end.Text = next.End;
                    RenderEditFeedback();
                    return true;
                }
                if (!ApplyEdit(next, "Time")) return false;
                start.Text = next.Start; end.Text = next.End;
                return true;
            }
            RenderEditFeedback();
            return false;
        }
        bool CommitMinutes()
        {
            int duration;
            if (!Int32.TryParse(minutes.Text, out duration) || duration < 1 || duration > 120)
            { minutesError = "InvalidSnooze"; RenderEditFeedback(); return false; }
            minutesError = "";
            if (duration == committed.SnoozeMinutes)
            {
                saveErrors.Remove("Minutes");
                minutes.Text = duration.ToString();
                RenderEditFeedback();
                return true;
            }
            Options next = committed.Copy();
            next.SnoozeMinutes = duration;
            if (!ApplyEdit(next, "Minutes")) return false;
            minutes.Text = duration.ToString();
            return true;
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
            ApplyPalette(isDark);
            RenderEditFeedback();
            UpdateStartupStatus();
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
            bool restoreLanguage = disposing && !disposed && preview;
            if (disposing && !disposed) { disposed = true; L.Changed -= ApplyLanguage; feedbackTimer.Stop(); feedbackTimer.Dispose(); }
            base.Dispose(disposing);
            // Preview rendering must not change the global language outside its lifetime.
            if (restoreLanguage) L.Select(originalLanguage);
        }
    }
}
