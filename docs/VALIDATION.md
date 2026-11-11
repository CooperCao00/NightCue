# Validation record / 验证记录

## Unreleased automatic settings checks — 2026-11-07 / 设置自动保存

These checks cover the autosave working-tree changes on top of 1.3.1. No new release package or GitHub release was published. The startup section below records the earlier baseline and the user's successful sign-in test.
本节验证 1.3.1 之上的自动保存修改，尚未发布新版本。下方自启动章节保留此前的检查基线及用户实际登录成功的反馈。

| Check / 检查 | Result / 结果 |
| --- | --- |
| Framework build and regression runner | Passed: 265 checks, including independent switches/language, time-pair validation, minute entry, failed-save rollback, closing retry, persistence across reopening, unavailable startup state and hotkey-conflict feedback / 构建及 265 项检查通过，覆盖独立提交、时间校验、失败回退、关窗失败重试及重开保留 |
| Language and failure layouts | Ten languages in light/dark mode, inline-error layouts and recovery-notice renders passed text-height/separation checks. Chinese, German and Russian renders were visually inspected / 十语言浅深色、错误与恢复提示布局通过检查；目视检查中、德、俄渲染 |
| MSBuild Release | Rebuilt using .NET Framework 4.8 reference assemblies without warnings/errors; generated regression runner passed all 265 checks / 使用 4.8 参考程序集构建，无警告或错误，生成的测试程序 265 项通过 |
| Isolated real tray integration | `--tray-smoke` passed 9 checks: protected-state handling; failed settings writes preserve disk and active options; accepted options are copied; a language-refresh exception after saving does not falsely report an uncommitted change. Hotkeys were disabled; real startup/theme were not modified / 独立目录中的真实托盘集成 9 项通过，覆盖文件写入失败及保存后刷新异常，不修改用户启动项和主题 |
| Local executable update | Backed up the installed app and replaced it with the tested build. Verified executable hash, background process, unchanged settings bytes, startup command and theme values before UI testing / 备份后替换本地程序，校验构建哈希及后台运行，界面测试前确认设置文件、启动项和主题未变 |
| Live settings UI (Terra) | Passed: morning switch and English selection saved immediately; closing and reopening kept English; entering 16 minutes and pressing Enter saved without closing; an incomplete start time showed inline validation without changing the active period. Original values were restored and the window hidden / 实测开关与语言即改即存、收起重开保留语言、分钟 Enter 提交不关窗、无效时间就地提示，最后恢复原值并收起 |
| Final installed-state verification | Settings file matched the pre-test backup byte for byte; startup command and both Windows theme values were unchanged. The running background executable matched the tested build / 实测后配置文件与原备份字节一致，启动项及两项主题值未变，后台运行程序与测试构建一致 |

The ordinary runner uses a fake settings-action host for the production form, plus preview renders; it does not exercise the user's registry. The tray integration mode uses an isolated configuration directory and must be explicitly requested. The real startup toggle and real Windows hotkey-conflict cases remain distinct from those simulations.
普通测试使用模拟设置动作接口驱动正式界面，并进行预览渲染，不更改用户注册表。托盘集成测试需显式运行并使用隔离目录。模拟启动开关和快捷键冲突不能代替 Windows 实机验收。

## Unreleased startup reliability checks — 2026-11-07 / 自启动可靠性补强

These checks cover the current working-tree changes on top of 1.3.1; no new release has been published.
本节针对 1.3.1 之上的当前修改，不代表已发布新版本。

| Check / 检查 | Result / 结果 |
| --- | --- |
| Framework build and regression runner | Passed: 232 checks, including startup registration states, overlong-command rejection, configuration recovery, byte-preserving backups, blocked writes, persistent notice in a fresh child process and bounded logs / 构建及 232 项检查通过 |
| Language and recovery layouts | Ten languages in light/dark mode plus ten recovery-notice renders; text-height and recovery-button/status-card separation checks passed; all startup-status strings checked / 十语言浅深色及十张恢复提示渲染通过，检查文字高度与控件不重叠 |
| MSBuild Release | Passed using .NET Framework 4.8 reference assemblies, no warnings or errors; generated tests passed 232 checks / 使用 4.8 参考程序集，零警告零错误，生成的测试程序 232 项通过 |
| Isolated tray integration | `--tray-smoke` passed 3 checks with a locked state file: tray text updates, warning stays visible, original bytes remain intact. Startup registration and theme values were unchanged / 隔离托盘测试通过，状态文件占用时仍更新提示并保留原件，启动项与主题未变 |
| Installed background launch | Updated local app was launched with `--background`; process remained running without a settings window. `startup-attempt` and `tray-initialized` appeared in diagnostics. Existing settings, startup command and theme values remained unchanged / 本地程序后台运行及两条启动日志确认，用户设置、启动项和主题未变 |
| Live settings UI (Terra) | Confirmed the current-copy registration notice and both new buttons. Clicking Open diagnostics produced a File Explorer window, but its path could not be read after the UI tool stalled. Windows startup settings was not clicked. No toggles, theme or saved settings changed / 确认状态文案和两按钮；点击诊断后出现资源管理器，工具阻塞导致未确认路径；Windows 启动设置按钮未点击，未改开关、主题或保存设置 |
| Actual Windows sign-in / reboot | Passed by user confirmation on 2026-11-07 after enabling Run at sign-in and saving. The user clarified that this step had previously been omitted; latest success was not independently observed by the agent / 2026-11-07 用户确认开启并保存后已能开机自启动；此前未完成此步骤，最新成功依据用户实机反馈 |

### Latest result — 2026-11-07 / 最新验收结论

The user subsequently clarified that Run at sign-in had not previously been enabled and saved, and confirmed startup now works after completing that step. The original report is closed based on this user confirmation. The existing HKCU Run mechanism can remain; the evidence does not establish a missing installer or Windows recognition failure as the cause. Disabling startup, Task Manager overrides and moved-folder scenarios remain separate regression checks. Historical observations below remain unchanged as observations of the earlier test.

用户后续明确说明：此前未开启「登录后自动运行」并保存，完成后现已可以开机自启动。原问题按用户实机确认关闭，现有 HKCU Run 机制可保留。没有证据将缺少安装流程或系统识别失败确定为根因。关闭启动、任务管理器禁用、目录移动等场景仍另行回归；以下保留早先那次测试的历史观察。

### Reboot investigation — 2026-11-07 / 真实重启排查

Windows Explorer started at 18:09:53 China Standard Time. At the 18:15 check, NightCue was absent and its latest diagnostic entries were from before reboot. Windows Shell-Core recorded Run-list enumeration and other startup launches between 18:10 and 18:12, but no NightCue execution entry was found. The current-user Run value was present in both registry views and matched the existing executable. No NightCue-specific crash or application-block event was found in the checked Application, Shell-Core, Application-Experience, CodeIntegrity, AppLocker and Defender event logs. Absence of such events is evidence, not proof that no blocking mechanism exists.

The Windows Settings > Apps > Startup list was inspected visually from top to bottom (Terra, screenshot-only after accessibility text was unavailable); NightCue, nightcue.exe and 夜色提醒 were not present. No startup switches or security settings were changed, and NightCue was not manually launched during this investigation. At that point the cause was undetermined; the later user confirmation above supersedes the open status. A registered Run command alone is not proof of successful startup.

Windows 桌面进程在北京时间 18:09:53 启动；18:15 检查没有 NightCue 进程，诊断记录均早于重启。系统记录显示 18:10–18:12 已处理 Run 列表及其他启动应用，但未找到 NightCue 执行记录。两种注册表视图均有正确的当前用户启动项，目标文件存在。已查事件日志中没有 NightCue 崩溃或应用阻止记录，但日志缺失不能排除所有拦截原因。

使用 Terra 通过纯截图从头到尾检查“设置 → 应用 → 启动”，未发现 NightCue、nightcue.exe 或“夜色提醒”。本次排查没有修改启动开关、安全设置，也没有手动打开 NightCue。当时原因尚未确定；后续用户确认已解决，状态以上方最新结论为准。仅凭 Run 项存在不能证明自启动成功。

The MSBuild check initially picked the machine's AMD64 WPF runtime reference and reported architecture warnings. Repeating with the actual .NET 4.8 reference-assembly directory resolved those warnings; this was a local build invocation issue.
首次 MSBuild 调用使用了本机 AMD64 WPF 运行时引用而出现架构警告；改用正确的 .NET 4.8 参考程序集目录后通过。

To reproduce the opt-in tray integration check after building, run `artifacts/tests/NightCue.Tests.exe --tray-smoke <new-empty-test-directory>`. It briefly creates a tray icon and uses isolated settings with hotkeys disabled. The ordinary build/test runner does not run this mode.
隔离托盘验证需显式运行上述参数，并使用新的空测试目录；会短暂创建托盘图标，不注册快捷键。常规构建测试不会自动进入此模式。


Release preparation: **1.3.1, 2026-11-06**. This record describes checks actually performed; it is not certification of every Windows configuration. Later changes require the affected checks to be rerun.

本记录仅描述实际执行的检查，不代表覆盖所有 Windows 配置。后续修改需重跑受影响检查。

## Current patch checks / 本次补丁检查

| Check / 检查 | Result / 结果 |
| --- | --- |
| Settings lifecycle regression | 191 checks passed, including three preview-window close/recreate cycles using a shared tray icon; each creates a handle, changes language, closes and checks disposal plus language restoration / 191 项通过；新增三次共享图标的预览设置关闭重建循环，创建句柄、切换语言并关闭，验证释放及语言恢复 |
| 1.3.1 direct build | Passed with 191 regression checks / 构建及 191 项回归检查通过 |
| 1.3.1 solution and extracted-source package | Framework, MSBuild Release and source ZIP extracted to a new path all passed 191 checks; MSBuild had 0 warnings/errors / 两种构建及源码 ZIP 新路径解压构建均通过 191 项检查，MSBuild 零警告零错误 |
| Running application reopened by launching the same EXE | Passed on 1.3.1: close/relaunch, minimize/relaunch and a second close/relaunch all displayed settings again / 1.3.1 实际程序关闭重开、最小化恢复及再次关闭重开均显示设置窗口 |
| Actual tray left/right click | Not exercised: the UI test tool cannot access the taskbar in this session / 未实测：本次界面工具无法访问任务栏 |

The tests below are historical 1.3.0 evidence. They must not be read as completed 1.3.1 checks. No successful end-to-end tray click is claimed for this patch.

以下是 1.3.0 的历史验证，不能作为 1.3.1 已完成检查。本补丁尚不能宣称实际托盘点击流程通过。

## 1.3.0 automated checks / 1.3.0 自动检查

| Check / 检查 | Local result / 本地结果 |
| --- | --- |
| Framework compiler build (`build.ps1`, PowerShell 7) | Passed, warnings treated as errors / 通过，编译警告视为错误 |
| Regression runner | 188 checks passed: night/day boundaries and restart persistence, legacy XML/defaults, input/XML limits, language fallback/resources, UI rendering and startup rollback / 188 项通过 |
| Ten languages, light/dark render | Twenty renders loaded and localized successfully; automated text-height checks passed / 十种语言二十张浅深色渲染及文本高度检查通过 |
| Live language preview and cancellation | Automated view update, localized validation and restoration of the original unsaved language passed / 即时更新、翻译错误提示及关闭未保存语言的恢复通过 |
| Startup transaction | In-memory store tests passed for path repair, removal, write failure, verification failure and exact rollback / 内存存储模拟的路径修复、移除、写入/验证失败及完整回滚通过 |
| Solution Release build | Windows MSBuild rebuild passed with 0 warnings and 0 errors; its test executable passed all 188 checks / 解决方案重构建零警告零错误，生成的测试程序 188 项通过 |
| Source ZIP unpacked to a new path | Passed in a clean path containing spaces and Chinese characters; all 188 checks passed / 在含空格和中文的新路径解压，构建及 188 项检查通过 |
| Final documentation links, metadata and archive contents | Passed; source has 68 allowlisted files, portable has 5 files; both include MIT and exclude settings, state, logs and private issue notes / 通过；源码清单 68 个文件，便携包 5 个文件，均含 MIT 且无用户配置、状态、日志或私有问题记录 |
| GitHub-hosted workflow | Not run for this release yet / 本版尚未执行远端流程 |

The local solution check used Microsoft's .NET Framework 4.8 reference assemblies 1.0.3 and explicit framework-reference paths in a normal Windows process environment. A restricted process environment failed inside MSBuild; the successful run had no warnings or errors. Reference assemblies are temporary development inputs and are not bundled with the application.

本地解决方案检查使用微软 .NET Framework 4.8 参考程序集 1.0.3 及显式框架路径，在正常 Windows 进程环境下成功。受限进程环境中的 MSBuild 内部失败不作为通过依据。参考程序集仅用于开发验证，不随程序分发。

Automated checks do not start the tray application, register startup/hotkeys or change the user's theme. They render settings in preview mode and simulate startup storage. These results do not prove actual sign-in startup or end-to-end tray behavior.

自动检查不启动托盘程序、不注册启动项或快捷键、不改变用户主题；界面为预览渲染，启动存储为模拟。因此不能由此宣称实际登录启动或完整托盘流程通过。

## 1.3.0 UI checks on Windows 11 / 1.3.0 Windows 11 界面检查

The real preview window was exercised without saving configuration or changing Windows settings:

- Opened the language list and observed English, Simplified Chinese and German selection with immediate translated text.
- Checked light and dark preview appearances and selected-theme styling.
- Toggled the after-end-time light-cue option off and on.
- Used Tab from the language selector to the morning option and then More.
- Closed the language list with Escape. A second Escape was followed by the preview window disappearing; no timing guarantee is inferred from the observation.

已操作真实预览窗口：展开语言列表，在英语、简中、德语间切换并看到即时翻译；检查浅深色外观及选中态；关闭/开启早晨提醒；用 Tab 从语言列表到早晨开关再到更多；Esc 收起列表，再次 Esc 后观察到窗口关闭。未保存配置、未改变 Windows 设置。

The dropdown was visible in its own window capture. Automation could not directly click an item outside the main window's capture boundary; keyboard selection worked. This limitation is recorded as a test-tool boundary, not an application defect. The current form fits its observed desktop working area; arbitrary small screens and mixed-DPI displays remain unverified.

下拉列表在独立截图中可见；自动化工具不能直接点击主窗口捕获范围外的条目，键盘选择可用。这是本次工具操作边界，不能据此认定软件有缺陷。窗口适应了观察到的桌面工作区，任意小屏与混合 DPI 尚未验证。

## Not yet verified / 尚未验证

- Full current-release tray/menu workflow, real system theme switching, hotkey conflicts and external theme changes.
- Startup-disabled behavior, Task Manager overrides and moved-folder regression; the enabled-and-saved case is now confirmed by the user as recorded above.
- Windows 10, Windows ARM64, comprehensive screen-reader/high-contrast behavior and mixed-DPI multi-monitor use.
- A fresh Visual Studio IDE session, GitHub-hosted CI and Windows PowerShell 5.1 execution. Local policy prevented the earlier PowerShell 5.1 attempt; PowerShell 7 was used without changing machine policy.
- Native-speaker review of every included translation.

本版完整托盘/实际主题切换/快捷键流程、关闭自启动后的真实登录、任务管理器禁用及移动目录场景、Windows 10、ARM64、读屏/高对比度及混合 DPI 多屏仍待验证。全新 Visual Studio IDE、远端 CI 与 PowerShell 5.1 执行也未在本次环境完成；没有为验证修改全局策略。所有翻译尚待全面母语校对。

Run `build.ps1` to reproduce compiler, metadata, link and regression checks. Run `scripts/package.ps1` for source/portable archives and checksums, then rebuild the extracted source. See [PUBLISHING.md](PUBLISHING.md) for the remaining desktop and release steps.
