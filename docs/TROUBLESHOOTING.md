# Troubleshooting / 故障排查

Current version / 当前版本：**1.3.1**。

| Problem / 问题 | What to check / 处理方式 |
| --- | --- |
| Settings will not reopen / 关闭后打不开设置 | Upgrade to 1.3.1 and exit the older running copy first. Left-click now opens/activates Settings, recreates a closed window and restores a minimized window. Right-click retains the quick menu; launching the same EXE again also requests Settings. / 先退出旧实例并升级到 1.3.1。左键打开或激活设置，关闭后重建、最小化后恢复；右键保留快捷菜单，再次运行同一个 EXE 也可请求打开设置。 |
| No visible cue / 看不到提醒 | Drag NightCue out of hidden icons or enable it in Windows taskbar settings; the tray menu has guidance. Check times and the morning-cue switch. The icon updates within about 10 seconds while awake. / 将图标拖到时钟旁或在任务栏设置中显示，核对时段与早晨提醒开关；运行时约 10 秒内刷新。 |
| It stays gray / 一直灰色 | The current period may be waiting, snoozed or dismissed. Restore the corresponding cue while the current theme differs from the offered theme. / 可能正在等待、延后或本时段已处理；在当前主题与提醒目标不同时恢复对应提醒。 |
| No light cue in the morning / 早晨没有太阳 | Enable the after-end-time option and check that both Windows and app themes are dark. An earlier light-theme observation or keep-dark choice completes this daytime period; use Restore light-mode cue to resume. / 开启结束时间后的提醒，确认系统与应用均为深色；此前已是浅色或选择保持深色，会停止本时段提醒，可手动恢复。 |
| Another launch shows an older copy / 新副本打开旧版 | Exit the running copy from its tray menu first. All copies share a single instance. / 先从托盘退出运行中的旧副本。 |
| Unexpected interface language / 语言不符合预期 | Follow Windows uses the display language, not the region or calendar. Choose a language manually and save; restart after changing the Windows display language. Unsupported languages use English. / 默认读取显示语言而非地区或日历；可手选并保存。改系统语言后重启程序；不支持的语言用英语。 |
| A language change disappears / 语言选择未保留 | Language changes are previews until saved. Closing without saving restores the previous choice. / 切换语言先预览，需保存；直接关闭会恢复原选择。 |
| Squares or missing glyphs / 出现方框或缺字 | Check Windows optional language fonts and the font choices in the language resource. Report the locale and a screenshot; NightCue does not bundle font files. / 检查 Windows 可选语言字体，反馈语言及截图；软件不附带字体文件。 |
| Hotkey does nothing / 快捷键无效 | Check its switch and conflict hint in Settings; another app may own it. The tray menu remains available. / 检查开关与占用提示，仍可使用托盘菜单。 |
| Some apps keep their own theme / 部分应用不变色 | Enable “follow system” in those apps. NightCue does not override fixed app themes. / 将对应应用设为跟随系统。 |
| Theme changes back unexpectedly / 主题自行变回 | Check other theme schedulers. NightCue only changes themes on a theme command or hotkey. / 检查其他工具的定时切换。 |
| Startup fails or settings cannot save / 启动或保存失败 | Use a writable folder and inspect error.log. With the app stopped, back up and rename malformed XML files to reset them. / 使用可写目录并查看日志；退出后备份并重命名损坏的 XML 可恢复默认。 |
| It no longer starts at sign-in / 登录不再启动 | Exit the old copy, run the new location and save with Run at sign-in enabled. A hint indicates an outdated path. Also check Windows Task Manager → Startup apps: Windows can disable an otherwise valid entry. / 退出旧副本，从新位置运行并保持登录启动开启后保存，按提示修复路径；再检查任务管理器是否禁用了启动应用。 |
| Build script is blocked / 构建脚本被阻止 | Use an approved PowerShell environment or build the solution in Visual Studio with the 4.8 targeting pack. No global policy change is needed. / 使用获准环境或 Visual Studio，无需修改全局策略。 |

## Time and state / 时间与状态

NightCue does not wake a sleeping computer. After resume it evaluates current local time; snooze deadlines use UTC. Changing the time zone changes the local reminder window. Each period has its own identity: the night begins at Start, and the following daytime period begins at End. A non-overnight window works too; its daytime period can cross midnight.

程序不会唤醒休眠电脑。恢复后按当前本地时间判断，延后截止时间使用 UTC；时区变化会影响提醒时段。夜间从开始时间计，随后的白天从结束时间计。不跨午夜的夜间窗口也可使用，其白天时段可能跨午夜。

Observing dark during the night, or light during the daytime period, completes that period's cue. Changing back externally does not automatically restore it. Daytime decisions do not suppress the next night's reminder. The daytime option is enabled by default when upgrading an older settings file.

夜间观察到深色、白天观察到浅色，均视为对应时段已处理。随后在其他地方切回另一主题，不会自动恢复提醒；白天选择不影响下一晚。旧设置升级时默认开启白天提醒。

## Reporting / 反馈

Include the NightCue version, Windows version, language, steps, expected behavior and actual result. Share only relevant log excerpts after removing personal paths. For sign-in problems, distinguish “entry exists”, “Windows allows the entry” and “the app actually starts after sign-in”; a registry check alone does not prove the last step.

请附程序版本、Windows 版本、语言、复现步骤和预期/实际表现，日志去除个人路径。登录问题需区分“注册项存在”“Windows 允许此项启动”和“登录后实际启动”，仅检查注册表不能证明最后一步。
