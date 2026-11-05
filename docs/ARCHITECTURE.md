# Architecture / 架构

NightCue is one .NET Framework 4.8 application project with a WinForms tray shell and a WPF settings view. The folders separate responsibilities; they are not separate executables or assemblies. All application types remain in the `NightCue` namespace, preserving XML type names and the existing single-instance behavior.

NightCue 使用一个 .NET Framework 4.8 工程，WinForms 托盘配合 WPF 设置界面。目录按职责划分，没有拆成多个程序或程序集；类型仍位于 `NightCue` 命名空间，保留 XML 类型名称及原有单实例行为。

## Modules / 模块

| Folder | Responsibility / 职责 | Boundary / 边界 |
| --- | --- | --- |
| `Domain` | Options, cue types, night/day state and schedule rules / 配置模型、状态与提醒规则 | Receives local time, UTC and observed theme as inputs; no UI, registry, localization or file access / 无界面、注册表、翻译与文件操作 |
| `Persistence` | XML reading, atomic replacement and error logging / XML 读写、原子替换与日志 | Local files beside the executable; no UI or theme decisions / 不参与主题决策 |
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

Reads prohibit DTDs/external entities and limit document size. Writes serialize to a temporary file, flush, then replace or move into place. Startup settings save snapshots the complete existing Run command, writes and verifies the desired command, then saves options. If saving fails, it restores the exact previous command. Windows' separate startup-disable controls remain under user control.

读取禁止 DTD/外部实体并限制文件大小；写入先序列化到临时文件、刷盘，再替换。保存登录启动设置时保留完整旧命令，写入并验证新命令后保存配置；失败则恢复旧命令。Windows 自身的启动禁用状态仍由用户管理。

## Build and regression checks / 构建与检查

The project explicitly lists C# sources; `build.ps1` reads that same list. XAML retains the logical resource name `NightCue.SettingsView.xaml`, and language XML files use `NightCue.Languages.<code>.xml`. Source packaging uses an explicit allowlist, excluding runtime XML, logs and build output.

工程显式列出 C# 文件，直接构建脚本读取同一列表。XAML 与语言资源保留稳定逻辑名称；源码打包使用明确清单，排除运行配置、日志和构建产物。

Regression checks cover period boundaries, state persistence, legacy XML, malformed input, language resolution and placeholders, light/dark UI renders, cancellation semantics and startup failures through an in-memory store. They do not register startup/hotkeys or change the Windows theme. Actual sign-in, tray interaction and platform coverage still need desktop checks. See [VALIDATION.md](VALIDATION.md).

回归检查覆盖时段边界、旧配置、状态保存、异常输入、语言回退、布局、取消语义与启动失败回滚，不注册启动项/快捷键，也不改系统主题。实际登录、托盘交互和各系统版本仍需桌面验证。
