using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
        bool previewDark;
        bool? lastDark;
        readonly W.FrameworkElement view, scene;
        readonly C.TextBox start, end, minutes;
        readonly B.ToggleButton startup, hotkey;
        readonly C.TextBlock status, heading, feedback, shortcut;
        readonly C.Button light, dark;
        string warning = "";
        public W.FrameworkElement View
        {
            get
            {
                return view;
            }
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int value, int size);
        public SettingsForm(TrayApp owner, Options o, Icon icon) : this(owner, o, icon, false, false)
        {
        }

        public SettingsForm(TrayApp owner, Options o, Icon icon, bool isPreview, bool darkPreview)
        {
            app = owner;
            preview = isPreview;
            previewDark = darkPreview;
            Text = "夜色提醒 · 设置";
            Icon = icon;
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            float dpi;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                dpi = g.DpiX / 96f;
            float scale = Math.Min(dpi, Math.Min((Screen.PrimaryScreen.WorkingArea.Width - 80f) / 940f, (Screen.PrimaryScreen.WorkingArea.Height - 80f) / 688f));
            ClientSize = new Size((int)(940 * scale), (int)(688 * scale));
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("NightCue.SettingsView.xaml"))
            {
                if (s == null)
                    throw new FileNotFoundException("找不到界面资源。");
                view = (W.FrameworkElement)System.Windows.Markup.XamlReader.Load(s);
            }

            scene = (W.FrameworkElement)view.FindName("Scene");
            ElementHost host = new ElementHost
            {
                Dock = DockStyle.Fill,
                Child = view
            };
            Controls.Add(host);
            start = Find<C.TextBox>("StartTime");
            end = Find<C.TextBox>("EndTime");
            minutes = Find<C.TextBox>("Minutes");
            startup = Find<B.ToggleButton>("StartupToggle");
            hotkey = Find<B.ToggleButton>("HotkeyToggle");
            status = Find<C.TextBlock>("StatusText");
            heading = Find<C.TextBlock>("StatusHeading");
            feedback = Find<C.TextBlock>("Feedback");
            shortcut = Find<C.TextBlock>("ShortcutHint");
            light = Find<C.Button>("LightButton");
            dark = Find<C.Button>("DarkButton");
            start.Text = o.Start;
            end.Text = o.End;
            minutes.Text = o.SnoozeMinutes.ToString();
            startup.IsChecked = preview || Startup.Enabled;
            hotkey.IsChecked = o.Hotkey;
            light.Click += delegate
            {
                SetTheme(false);
            };
            dark.Click += delegate
            {
                SetTheme(true);
            };
            Find<C.Button>("Minus").Click += delegate
            {
                Step(-1);
            };
            Find<C.Button>("Plus").Click += delegate
            {
                Step(1);
            };
            Find<C.Button>("SaveButton").Click += delegate
            {
                Save();
            };
            C.Button more = Find<C.Button>("MoreButton");
            more.Click += delegate
            {
                if (preview)
                {
                    feedback.Text = "界面预览，不会改变系统设置。";
                    return;
                }

                W.Point p = more.PointToScreen(new W.Point(0, more.ActualHeight + 6));
                app.OpenMenuAt(new Point((int)p.X, (int)p.Y));
            };
            start.TextChanged += delegate
            {
                UpdateEndLabel();
                feedback.Text = warning;
            };
            end.TextChanged += delegate
            {
                UpdateEndLabel();
                feedback.Text = warning;
            };
            minutes.TextChanged += delegate
            {
                feedback.Text = warning;
            };
            view.PreviewKeyDown += delegate (object sender, System.Windows.Input.KeyEventArgs e)
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    Close();
                    e.Handled = true;
                }
            };
            UpdateEndLabel();
            ApplyPalette(preview ? previewDark : Theme.IsDark());
            Shown += delegate
            {
                ApplyChrome();
                Find<C.Button>("SaveButton").Focus();
            };
            if (preview)
                UpdateStatus(darkPreview ? "当前为深色，今晚不再提醒" : "可以切换深色了，等你方便时", "Ctrl + Alt + Shift + D", "");
        }

        T Find<T>(string name)
            where T : W.FrameworkElement
        {
            return (T)view.FindName(name);
        }

        static M.SolidColorBrush Brush(string color)
        {
            return new M.SolidColorBrush((M.Color)M.ColorConverter.ConvertFromString(color));
        }

        void SetTheme(bool value)
        {
            if (preview)
            {
                previewDark = value;
                UpdateStatus(value ? "当前为深色，今晚不再提醒" : "可以切换深色了，等你方便时", "Ctrl + Alt + Shift + D", "");
            }
            else
                app.ChangeTheme(value);
        }

        void Step(int delta)
        {
            int n;
            if (!Int32.TryParse(minutes.Text, out n))
                n = 15;
            minutes.Text = Math.Max(1, Math.Min(120, n + delta)).ToString();
        }

        static bool ParseTime(string text, out TimeSpan value)
        {
            return TimeSpan.TryParseExact(text.Trim().Replace('：', ':'), new string[] { @"h\:mm", @"hh\:mm" }, null, out value) && value.TotalHours < 24;
        }

        void UpdateEndLabel()
        {
            TimeSpan s, e;
            Find<C.TextBlock>("EndLabel").Text = ParseTime(start.Text, out s) && ParseTime(end.Text, out e) && e < s ? "次日结束" : "结束";
        }

        void Save()
        {
            TimeSpan s, e;
            int n;
            if (!ParseTime(start.Text, out s))
            {
                feedback.Text = "开始时间请填写为 23:00 这样的格式。";
                start.Focus();
                return;
            }

            if (!ParseTime(end.Text, out e))
            {
                feedback.Text = "结束时间请填写为 07:00 这样的格式。";
                end.Focus();
                return;
            }

            if (s == e)
            {
                feedback.Text = "开始与结束时间不能相同。";
                end.Focus();
                return;
            }

            if (!Int32.TryParse(minutes.Text, out n) || n < 1 || n > 120)
            {
                feedback.Text = "延后时间可以设置为 1–120 分钟。";
                minutes.Focus();
                return;
            }

            Options next = new Options
            {
                Start = s.ToString(@"hh\:mm"),
                End = e.ToString(@"hh\:mm"),
                SnoozeMinutes = n,
                Hotkey = hotkey.IsChecked == true
            };
            if (preview)
            {
                feedback.Text = "设置格式正确。预览模式不会保存或更改系统。";
                return;
            }

            if (app.ApplyOptions(next, startup.IsChecked == true))
                Close();
        }

        void ApplyPalette(bool isDark)
        {
            if (lastDark == isDark)
                return;
            lastDark = isDark;
            string[] names =
            {
                "Page",
                "Card",
                "Ink",
                "Muted",
                "Line",
                "Field",
                "Accent",
                "Tint",
                "Hover",
                "Track"
            };
            string[] colors = isDark ? new string[]
            {
                "#181C2A",
                "#222738",
                "#EDF0FA",
                "#989FB8",
                "#30364B",
                "#1B2030",
                "#A394F3",
                "#302D49",
                "#38334E",
                "#495168"
            }

            : new string[]
            {
                "#F6F7FB",
                "#FFFFFF",
                "#242737",
                "#777E92",
                "#E7E9F1",
                "#F7F8FC",
                "#7868DE",
                "#F0EDFF",
                "#EEEAFB",
                "#DADDE8"
            };
            for (int i = 0; i < names.Length; i++)
                scene.Resources[names[i]] = Brush(colors[i]);
            BackColor = isDark ? Color.FromArgb(24, 28, 42) : Color.FromArgb(246, 247, 251);
            C.Button selected = isDark ? dark : light, other = isDark ? light : dark;
            selected.SetResourceReference(C.Button.BackgroundProperty, "Tint");
            selected.SetResourceReference(C.Button.BorderBrushProperty, "Accent");
            other.SetResourceReference(C.Button.BackgroundProperty, "Card");
            other.SetResourceReference(C.Button.BorderBrushProperty, "Line");
            Find<C.TextBlock>("LightCaption").Text = isDark ? "明亮、清晰" : "正在使用  ·  浅色";
            Find<C.TextBlock>("DarkCaption").Text = isDark ? "正在使用  ·  深色" : "柔和、沉静";
            ApplyChrome();
        }

        void ApplyChrome()
        {
            if (!IsHandleCreated)
                return;
            int darkValue = lastDark == true ? 1 : 0, round = 2;
            DwmSetWindowAttribute(Handle, 20, ref darkValue, 4);
            DwmSetWindowAttribute(Handle, 33, ref round, 4);
            Color c = BackColor;
            int caption = c.R | (c.G << 8) | (c.B << 16);
            DwmSetWindowAttribute(Handle, 35, ref caption, 4);
        }

        public void UpdateStatus(string text, string keys, string issue)
        {
            status.Text = text;
            warning = issue;
            if (issue.Length > 0)
                feedback.Text = issue;
            bool isDark = preview ? previewDark : Theme.IsDark();
            ApplyPalette(isDark);
            heading.Text = text.Contains("延后") ? "稍后再见" : isDark ? "夜间深色已开启" : text.Contains("不再提醒") ? "今晚安心专注" : text.Contains("开始") ? "安静等待中" : "等你方便时";
            M.SolidColorBrush accent = Brush(isDark ? "#B5B3F1" : "#E8C58D");
            heading.Foreground = accent;
            Find<System.Windows.Shapes.Ellipse>("StatusDot").Fill = accent;
            shortcut.Text = keys.StartsWith("Ctrl") ? "Ctrl + Alt + Shift + D" : keys.Contains("占用") ? "快捷键已被占用" : "快捷键已关闭";
            shortcut.ToolTip = keys;
        }
    }

    public class QuietMenuRenderer : ToolStripProfessionalRenderer
    {
        readonly bool dark;
        public QuietMenuRenderer(bool value) : base(new MenuColors(value))
        {
            dark = value;
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? (dark ? Color.FromArgb(231, 233, 244) : Color.FromArgb(40, 44, 60)) : (dark ? Color.FromArgb(145, 154, 179) : Color.FromArgb(122, 129, 148));
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled)
                return;
            Rectangle r = new Rectangle(3, 1, e.Item.Width - 6, e.Item.Height - 2);
            using (var p = new System.Drawing.Drawing2D.GraphicsPath())
            {
                p.AddArc(r.Left, r.Top, 12, 12, 180, 90);
                p.AddArc(r.Right - 12, r.Top, 12, 12, 270, 90);
                p.AddArc(r.Right - 12, r.Bottom - 12, 12, 12, 0, 90);
                p.AddArc(r.Left, r.Bottom - 12, 12, 12, 90, 90);
                p.CloseFigure();
                using (var b = new SolidBrush(dark ? Color.FromArgb(57, 51, 83) : Color.FromArgb(240, 237, 255)))
                    e.Graphics.FillPath(b, p);
            }
        }

        class MenuColors : ProfessionalColorTable
        {
            readonly bool dark;
            public MenuColors(bool value)
            {
                dark = value;
                UseSystemColors = false;
            }

            public override Color ToolStripDropDownBackground
            {
                get
                {
                    return dark ? Color.FromArgb(34, 39, 56) : Color.White;
                }
            }

            public override Color MenuBorder
            {
                get
                {
                    return dark ? Color.FromArgb(58, 64, 87) : Color.FromArgb(224, 227, 237);
                }
            }

            public override Color SeparatorDark
            {
                get
                {
                    return MenuBorder;
                }
            }

            public override Color SeparatorLight
            {
                get
                {
                    return ToolStripDropDownBackground;
                }
            }
        }
    }
}
