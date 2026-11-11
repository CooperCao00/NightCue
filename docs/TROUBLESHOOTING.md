# Troubleshooting / 故障排查

Current version / 当前版本：**1.3.1**。

| Problem / 问题 | What to check / 处理方式 |
| --- | --- |
| Settings will not reopen / 关闭后打不开设置 | Upgrade to 1.3.1 and exit the older running copy first. Left-click now opens/activates Settings, recreates a closed window and restores a minimized window. Right-click retains the quick menu; launching the same EXE again also requests Settings. / 先退出旧实例并升级到 1.3.1。左键打开或激活设置，关闭后重建、最小化后恢复；右键保留快捷菜单，再次运行同一个 EXE 也可请求打开设置。 |
| No visible cue / 看不到提醒 | Drag NightCue out of hidden icons or enable it in Windows taskbar settings; the tray menu has guidance. Check times and the morning-cue switch. The icon updates within about 10 seconds while awake. / 将图标拖到时钟旁或在任务栏设置中显示，核对时段与早晨提醒开关；运行时约 10 秒内刷新。 |
| It stays gray / 一直灰色 | The current period may be waiting, snoozed or dismissed. Restore the corresponding cue while the current theme differs from the offered theme. / 可能正在等待、延后或本时段已处理；在当前主题与提醒目标不同时恢复对应提醒。 |
| No light cue in the morning / 早晨没有太阳 | Enable the after-end-time option and check that both Windows and app themes are dark. An earlier light-theme observation or keep-dark choice completes this daytime period; use Restore light-mode cue to resume. / 开启结束时间后的提醒，确认系统与应用均为深色；此前已是浅色或选择保持深色，会停止本时段提醒，可手动恢复。 |
| Another launch shows an older copy / 新副本打开旧版 | Exit the running copy from its tray menu first. All copies share a single instance. / 先从托盘退出运行中的旧副本。 |
| Unexpected interface language / 语言不符合预期 | Follow Windows uses the display language, not the region or calendar. Choose a language manually; it saves automatically. Restart after changing the Windows display language. Unsupported languages use English. / 默认读取显示语言而非地区或日历；手选后自动保存。改系统语言后重启程序；不支持的语言用英语。 |
| A language change disappears / 语言选择未保留 | Language changes save immediately. If saving fails, the previous language remains active and a local error appears. Check folder access and diagnostics; closing no longer cancels a successfully saved change. / 语言立即保存；失败时恢复原语言并局部提示，请检查目录权限和诊断。关闭不再撤销成功保存的更改。 |
| Squares or missing glyphs / 出现方框或缺字 | Check Windows optional language fonts and the font choices in the language resource. Report the locale and a screenshot; NightCue does not bundle font files. / 检查 Windows 可选语言字体，反馈语言及截图；软件不附带字体文件。 |
| Hotkey does nothing / 快捷键无效 | Check its switch and conflict hint in Settings; another app may own it. The tray menu remains available. / 检查开关与占用提示，仍可使用托盘菜单。 |
| Some apps keep their own theme / 部分应用不变色 | Enable “follow system” in those apps. NightCue does not override fixed app themes. / 将对应应用设为跟随系统。 |
| Theme changes back unexpectedly / 主题自行变回 | Check other theme schedulers. NightCue only changes themes on a theme command or hotkey. / 检查其他工具的定时切换。 |
| Startup fails or settings cannot save / 启动或保存失败 | Use a writable app folder and Open diagnostics. Settings/state loading can recover to defaults with a notice; if reading or backup failed, writes to that file remain blocked until access is repaired and the app restarts. / 使用可写程序目录并打开诊断；加载失败可使用默认值并提示。读取或备份失败时保护原件，修复权限并重启后再试。 |
| It no longer starts at sign-in / 登录不再启动 | Read the startup status in Settings. For another registered location, exit the old copy, run the intended one and click Repair startup path. Use Windows startup settings to check whether Windows disabled it. / 查看注册状态；若指向其他位置，退出旧副本，从目标位置运行并点击“修复启动路径”。使用“Windows 启动设置”检查系统是否禁用。 |
| Startup registration is unreadable / 启动注册信息无法读取 | Startup changes are blocked rather than treating an unknown entry as absent; other settings save independently. Open diagnostics, resolve the access problem and reopen Settings to retry. / 不会把未知状态当作未注册；此时只禁止自启动更改，其他设置仍可独立保存。打开诊断，处理访问问题后重新打开设置重试。 |
| Recovery notice keeps returning / 恢复提示反复出现 | The notice persists until explicitly dismissed; a later recovery can create it again. Dismissing does not repair the cause or delete backups. See below. / 提示需显式关闭，后续发生新的恢复可再次出现；关闭不等于修复原因，也不删除备份。 |
| Build script is blocked / 构建脚本被阻止 | Use an approved PowerShell environment or build the solution in Visual Studio with the 4.8 targeting pack. No global policy change is needed. / 使用获准环境或 Visual Studio，无需修改全局策略。 |

## Editing and automatic saving / 编辑与自动保存

Switches and language save on change. Both time fields are one edit: Enter or leaving the section validates and applies them together, so moving between Start and End does not commit half an edit. Minute entry applies on Enter or leaving the field; +/− applies immediately. The hide/close action also attempts to commit valid drafts. Incomplete or invalid drafts allow closing while retaining the last valid configuration; earlier successful changes are never cancelled. If a valid time or minute draft cannot be written, the window stays open with an inline error for retry, without a dialog.

开关与语言更改即保存。开始和结束时间视为一次编辑，按 Enter 或离开整个区域时一起校验应用，在两个时间框之间移动不会只保存一半。分钟输入在 Enter 或失焦时应用，加减立即应用；收起/关闭也尝试提交有效草稿。未填完整或无效的草稿允许关闭并沿用原有效配置，已成功的更改不撤销。有效的时间或分钟草稿若写盘失败，窗口保留并显示行内提示以便重试，不弹对话框。

If writing the settings file fails, the affected row shows an error and the previous valid setting stays active. Reminder/hotkey switches and language roll back; numeric drafts remain visible for correction. If the hotkey conflicts with another app, its conflict status stays visible. Unrelated edits do not re-register the hotkey; only changing its switch does so.

设置文件写入失败时对应行显示错误，原有效设置继续运行。提醒/快捷键开关和语言恢复原值，数字保留草稿便于修正。快捷键被占用会明确提示；修改无关设置不会重新注册快捷键，只有其开关变化才会重新处理。

## Startup status and recovery / 启动状态与恢复

The status area reports registration, not Windows' final startup decision. **Off** can be changed with the switch immediately; **registered for this copy** confirms the command path; **another location** exposes **Repair startup path**. Click that button to register the current path; after success it becomes **Windows startup settings** again. **Unreadable** blocks startup changes to protect an unknown entry, without blocking unrelated settings. On failure, feedback is shown and registration is read again. Rollback is attempted but can also fail; the displayed state comes from the new read. If unreadable, startup controls are disabled and status is unknown. Changing language, times or reminder options does not rewrite the Run entry. NightCue does not change Windows' startup-disable controls. Test actual sign-out/sign-in or reboot separately.

状态区报告注册情况，不能代表 Windows 最终会启动应用。关闭时直接开启开关即可应用；“当前程序已注册”表示路径相符；“其他位置”显示“修复启动路径”，点击登记当前路径后恢复为“Windows 启动设置”入口；“无法读取”仅阻止自启动更改，保护未知注册内容。启动更改失败会显示提示并重新读取实际注册状态。程序尝试回滚，但回滚也可能失败，因此以重读结果为准；无法读取时禁用启动控件并显示未知状态。其他设置独立保存，修改语言、时间或提醒选项不会重写 Run 项。系统禁用状态仍由用户管理，真实注销登录或重启需另行验证。

`settings.xml` and `state.xml` stay beside the EXE. **Open diagnostics** opens `%LOCALAPPDATA%\NightCue\Diagnostics`, which contains:

| File | Purpose / 用途 |
| --- | --- |
| `diagnostics.log`, `error.log` | Diagnostic events and errors; each capped at 128 KiB with one `.1` rotated copy / 诊断事件与错误，各上限 128 KiB，各保留一个 `.1` 轮转副本 |
| `recovery.pending` | Persistent recovery notice awaiting acknowledgement / 等待用户确认的持久恢复提示 |
| `<original>.corrupt-<UTC timestamp>-<unique ID>.bak` | Original configuration/state XML that could not be parsed or failed validation / 无法解析或校验无效的配置、状态原件备份 |

On XML or validation errors, NightCue attempts to copy the original before writing defaults. If the backup succeeds but saving defaults fails, it leaves the original in place and uses defaults in memory. If reading, file access or backup fails, it uses defaults in memory and blocks this process from saving that original. Resolve the access problem and restart before trying again. The left-panel recovery notice remains available until **Dismiss recovery notice**; dismissing removes the marker, not the backups or logs. To recover earlier values, exit NightCue first and inspect the relevant backup and diagnostic entry before restoring it.

XML 解析或校验失败时，先尝试复制原件，再写默认值；备份成功但保存失败，则保留原件、使用内存默认值。读取、访问或备份失败时，同样仅用内存默认值，并禁止本进程保存对应原件。处理权限后重启重试。左栏提示需通过“关闭恢复提示”显式确认，确认只清除标记，保留备份和日志。如需找回原设置，先退出程序，再核对备份及对应诊断记录后恢复。

Logs and backups are local and may contain full paths, commands or settings. Share only the relevant redacted excerpt. They are outside the portable app folder and are not removed by deleting that folder.

日志和备份只保存在本机，可能包含完整路径、启动命令或设置；分享前检查，只提供去除个人信息的相关内容。它们位于便携程序目录之外，删除程序文件夹不会同时删除诊断文件。

The diagnostics folder is shared by copies running as the same Windows user. If that folder cannot be written, the recovery notice remains in memory for this process but cannot survive restart. Backups are not count-limited; review and remove obsolete backups yourself when no longer needed. The marker is shared too: acknowledgement clears that user's pending marker, not a separate marker for each app location.

同一 Windows 用户的程序副本共用诊断目录。目录不可写时，本进程仍显示内存中的恢复提示，但无法跨重启保留。备份数量没有自动上限，不再需要时可自行整理删除。提示标记同样共享，确认会清除此用户的待确认标记，而非按每个程序位置分别保存。
## Time and state / 时间与状态

NightCue does not wake a sleeping computer. After resume it evaluates current local time; snooze deadlines use UTC. Changing the time zone changes the local reminder window. Each period has its own identity: the night begins at Start, and the following daytime period begins at End. A non-overnight window works too; its daytime period can cross midnight.

程序不会唤醒休眠电脑。恢复后按当前本地时间判断，延后截止时间使用 UTC；时区变化会影响提醒时段。夜间从开始时间计，随后的白天从结束时间计。不跨午夜的夜间窗口也可使用，其白天时段可能跨午夜。

Observing dark during the night, or light during the daytime period, completes that period's cue. Changing back externally does not automatically restore it. Daytime decisions do not suppress the next night's reminder. The daytime option is enabled by default when upgrading an older settings file.

夜间观察到深色、白天观察到浅色，均视为对应时段已处理。随后在其他地方切回另一主题，不会自动恢复提醒；白天选择不影响下一晚。旧设置升级时默认开启白天提醒。

## Reporting / 反馈

Include the NightCue version, Windows version, language, steps, expected behavior and actual result. Share only relevant log excerpts after removing personal paths. For sign-in problems, distinguish “entry exists”, “Windows allows the entry” and “the app actually starts after sign-in”; a registry check alone does not prove the last step.

请附程序版本、Windows 版本、语言、复现步骤和预期/实际表现，日志去除个人路径。登录问题需区分“注册项存在”“Windows 允许此项启动”和“登录后实际启动”，仅检查注册表不能证明最后一步。
