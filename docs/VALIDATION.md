# Validation record / 验证记录

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
- Real sign-out/sign-in or reboot startup enabled/disabled behavior, and Task Manager startup overrides.
- Windows 10, Windows ARM64, comprehensive screen-reader/high-contrast behavior and mixed-DPI multi-monitor use.
- A fresh Visual Studio IDE session, GitHub-hosted CI and Windows PowerShell 5.1 execution. Local policy prevented the earlier PowerShell 5.1 attempt; PowerShell 7 was used without changing machine policy.
- Native-speaker review of every included translation.

本版完整托盘/实际主题切换/快捷键流程、真实登录或重启、任务管理器禁用状态、Windows 10、ARM64、读屏/高对比度及混合 DPI 多屏仍待验证。全新 Visual Studio IDE、远端 CI 与 PowerShell 5.1 执行也未在本次环境完成；没有为验证修改全局策略。所有翻译尚待全面母语校对。

Run `build.ps1` to reproduce compiler, metadata, link and regression checks. Run `scripts/package.ps1` for source/portable archives and checksums, then rebuild the extracted source. See [PUBLISHING.md](PUBLISHING.md) for the remaining desktop and release steps.
