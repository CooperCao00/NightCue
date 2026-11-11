# Architecture / 架构

NightCue is one .NET Framework 4.8 application project with a WinForms tray shell and a WPF settings view. The folders separate responsibilities; they are not separate executables or assemblies. All application types remain in the `NightCue` namespace, preserving XML type names and the existing single-instance behavior.

NightCue 使用一个 .NET Framework 4.8 工程，WinForms 托盘配合 WPF 设置界面。目录按职责划分，没有拆成多个程序或程序集；类型仍位于 `NightCue` 命名空间，保留 XML 类型名称及原有单实例行为。

## Modules / 模块

| Folder | Responsibility / 职责 | Boundary / 边界 |
| --- | --- | --- |
| `Domain` | Options, cue types, night/day state and schedule rules / 配置模型、状态与提醒规则 | Receives local time, UTC and observed theme as inputs; no UI, registry, localization or file access / 无界面、注册表、翻译与文件操作 |
| `Persistence` | XML reading, atomic replacement, recovery and diagnostics / XML 读写、原子替换、恢复与诊断 | Configuration beside the EXE, diagnostics in local application data; no UI or theme decisions / 配置在程序旁，诊断在本地应用数据目录，不参与主题决策 |
| `Platform` | Current-user theme/startup integration, hotkeys, native messages and optional caption styling / Windows 平台交互 | Encapsulates Windows APIs; startup storage is injectable for tests / 封装平台 API，启动存储可替换测试 |
| `Localization` | Embedded catalog, selection, regional/key fallback and display text / 语言目录、回退与显示文案 | Converts typed states to text; Domain does not parse translated strings / 不以翻译文案判断业务状态 |
| `Shell` | Tray lifecycle, menu and icon presentation, settings host and XAML / 托盘与设置界面 | Coordinates the other modules and handles explicit user commands / 协调模块、响应用户操作 |
| `App` | Entry point, single instance, show-settings signal and application startup / 入口、单实例与启动装配 | Retains legacy mutex/event identifiers for interoperability with older local copies / 与旧副本共享单实例标记 |

## Reminder lifecycle / 提醒流程

While the app is awake, the shell evaluates the schedule about every ten seconds and when an action requires a refresh. `Schedule.Evaluate` receives the current local time, UTC time, options, state and observed dark-theme flag. It returns a typed `Cue` and updates only reminder state. The shell saves changed state and selects the corresponding tray icon and localized text.

运行时约每十秒检查一次，用户操作后也会刷新。调度器接收时间、设置、状态及观察到的主题，返回 `Cue` 并更新提醒状态；界面负责保存变化、选择图标和显示翻译。调度器不调用主题切换。

Night identity is the date on which Start occurred. The subsequent daytime period is identified by its End date. The default window therefore has a night from 23:00 to 07:00 and a daytime period from 07:00 to the next 23:00. Choices and snoozes are independent, including when either period crosses midnight. A command initializes the current period before applying a decision or snooze, so crossing a boundary between refresh and click does not lose the action.

夜间标记使用开始时的日期；随后的白天以结束时的日期标记。默认夜间为 23:00–07:00，白天为 07:00–下一次 23:00，两者分别保存选择与延后时间。命令执行前先初始化当前时段，避免刷新与点击之间跨边界而丢失操作。

Observing the target theme completes that period's cue. Returning to the other theme externally does not resume reminders automatically. Restore is explicit. The default-enabled daytime cue can be disabled independently. Only a theme command or the enabled hotkey calls `Theme.Set`; neither clock evaluation nor snooze does so.

观察到目标主题即视为本时段已处理，外部切回另一主题不会自动重新提醒，需要主动恢复。白天提醒默认开启，可单独关闭。只有主题命令与快捷键会调用主题切换，时钟检查和延后不会。

## Persistence and startup / 保存与启动

`settings.xml` and `state.xml` retain their original XML roots and fields. Missing newer fields receive constructor defaults: system language and enabled daytime reminders for options, empty daytime state for saved decisions. Existing night choices remain readable. There is no separate schema-version migration engine in this release.

设置与状态保留原 XML 根节点和旧字段。新增字段缺失时使用默认值：跟随系统语言、开启白天提醒、空白天状态；旧夜间选择仍可读取。本版没有另建带版本号的迁移引擎。

Reads prohibit DTDs/external entities and limit document size. Writes serialize to a temporary file, flush, then replace or move into place. Startup registration has a dedicated write path that snapshots the existing Run command, applies and verifies the requested change, and attempts to restore the previous command on failure. Rollback can fail too; the shell shows failure feedback, reads actual registration again, and disables startup controls with unknown status if that read fails. General option saves do not write startup registration. Windows' separate startup-disable controls remain under user control.

读取禁止 DTD/外部实体并限制文件大小；写入先序列化到临时文件、刷盘，再替换。启动注册使用独立写入流程，保留完整旧命令、应用并验证更改，失败则尝试恢复旧命令；回滚也可能失败，界面显示提示并重新读取实际注册状态。重读失败则禁用启动控件并显示未知状态。普通设置保存不写启动项。Windows 自身的启动禁用状态仍由用户管理。

Configuration and state remain portable files beside the executable. `Store.ReadRecovering` falls back for the affected file rather than aborting background startup. XML/validation failures first create a recovery backup; only after successful backup may defaults replace the original. Read/access or backup failures protect that original from subsequent saves for the current process. A failed default write leaves the original and uses in-memory defaults.

配置与状态仍随程序保存在旁边。`Store.ReadRecovering` 对受影响文件回退，避免后台启动因配置加载失败直接退出。解析/校验失败先备份，成功后才可覆盖为默认；读取/访问或备份失败时，本进程禁止后续保存原件。默认值写入失败则保留原件并使用内存默认值。

Diagnostics live in `%LOCALAPPDATA%\NightCue\Diagnostics`: `diagnostics.log` and `error.log` each rotate at 128 KiB with one `.1` file. Backups use `<original>.corrupt-<UTC yyyyMMddTHHmmssfffZ>-<Guid N>.bak`. `recovery.pending` persists an outstanding recovery notice; `Store.RecoveryNotice` exposes it, and `Store.AcknowledgeRecovery()` clears the marker after an explicit user action without deleting backups. The shell presents this independently of temporary input-validation feedback. All copies under one Windows user share this diagnostics directory and marker. If the directory is unwritable, the process retains an in-memory notice only; backup count is not automatically limited.

诊断目录为 `%LOCALAPPDATA%\NightCue\Diagnostics`，两个日志各 128 KiB 并保留一个 `.1` 副本。备份文件名含原名、UTC 时间和唯一标识。`recovery.pending` 记录未确认提示，`RecoveryNotice` 提供状态，用户显式确认后由 `AcknowledgeRecovery()` 清除标记，备份保留；界面将恢复提示与临时输入校验文案分开显示。同一 Windows 用户的副本共用目录及标记；目录不可写时仅在本进程内存中提示，备份数量不自动限制。

Startup status distinguishes absent, current-path, other-path and unreadable registration. An unreadable value blocks startup writes instead of being treated as disabled; unrelated option saves remain independent. The Windows startup-settings button delegates enable/disable decisions to Windows; registration status is not a claim that Windows' separate startup approval is enabled, or that sign-in has been tested.

启动状态区分未注册、当前路径、其他路径与不可读；不可读时阻止自启动写入，不将未知状态当作关闭；其他选项独立保存。按钮打开 Windows 启动设置，由用户处理系统禁用；路径注册不代表系统已允许，也不代表登录实测通过。

## Automatic settings application / 设置自动应用

The settings shell submits switches and language immediately. Time inputs form one draft group, validated and saved together on Enter or when focus leaves the section. Minute entry commits on Enter or loss of focus; step buttons commit immediately. Closing or hiding attempts to submit valid drafts. Incomplete or invalid drafts do not block closing and retain previous values; already committed changes are not rolled back. A persistence failure for a valid time or minute draft prevents the window from closing and retains inline feedback for retry, without a dialog. Failed settings-file writes leave the previous valid model active, restore reminder/hotkey switches and language controls and retain numeric drafts with local feedback.

设置界面对开关和语言立即提交；两个时间作为一组草稿，在 Enter 或焦点离开区域时一起校验保存。分钟输入在 Enter 或失焦时提交，加减按钮立即提交。关闭/收起尝试提交有效草稿，未填完整或无效时允许关闭并沿用原值，不回滚已提交更改。有效的时间或分钟草稿写盘失败时阻止关闭，保留行内提示供重试，不弹对话框。设置文件写入失败保持原有效模型，提醒/快捷键开关及语言恢复，数字草稿保留并局部提示。

Startup registration is changed only by its switch or the explicit repair action. The repair button is shown for a different registered path and returns to the Windows startup-settings action after success. Hotkey registration is updated only when its own option changes, preserving a visible conflict indication across unrelated saves. Theme commands retain their existing explicit-action behavior.

只有启动开关或显式修复操作才改启动注册；注册位置不同时显示修复按钮，成功后恢复系统启动设置入口。快捷键仅其选项改变时重新注册，无关保存不会掩盖占用提示；主题命令继续要求用户主动操作。

## Build and regression checks / 构建与检查

The project explicitly lists C# sources; `build.ps1` reads that same list. XAML retains the logical resource name `NightCue.SettingsView.xaml`, and language XML files use `NightCue.Languages.<code>.xml`. Source packaging uses an explicit allowlist, excluding runtime XML, logs and build output.

工程显式列出 C# 文件，直接构建脚本读取同一列表。XAML 与语言资源保留稳定逻辑名称；源码打包使用明确清单，排除运行配置、日志和构建产物。

Regression checks cover period boundaries, state persistence, legacy XML, malformed input, language resolution and placeholders, light/dark UI renders, settings application semantics and startup failures through an in-memory store. They do not register startup/hotkeys or change the Windows theme. Actual sign-in, tray interaction and platform coverage still need desktop checks. See [VALIDATION.md](VALIDATION.md).

回归检查覆盖时段边界、旧配置、状态保存、异常输入、语言回退、布局、设置应用语义与启动失败回滚，不注册启动项/快捷键，也不改系统主题。实际登录、托盘交互和各系统版本仍需桌面验证。
