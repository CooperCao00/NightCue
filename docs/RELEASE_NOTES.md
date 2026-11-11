# NightCue 1.3.1 · 夜色提醒

Patch release fixing Settings reopening from the tray. Ten languages and the optional morning cue remain available. Default window: **23:00–07:00**. No sounds, banners, flashing or automatic theme changes.

修复托盘重新打开设置的补丁版本，保留十种语言和可选的早晨浅色提醒。默认 **23:00–次日 07:00**；无声音、横幅、闪烁或自动换色。

## Unreleased interaction update / 未发布交互更新

The current working source now saves settings automatically. Switches/language apply immediately; Enter or leaving the section applies both times together. Minute edits apply on Enter or leaving the field, while +/− applies immediately. Hiding/closing attempts valid drafts; incomplete or invalid drafts allow closing with prior values and applied changes are not cancelled. If a valid time/minute draft cannot be written, the window remains open with inline retry feedback, without a dialog. Startup-path repair is now an explicit button action. See the current [README](../README.md) and [Unreleased changelog](../CHANGELOG.md). The 1.3.1 notes below describe the earlier patch preparation, not validation of these new changes.

当前工作源码已改为自动保存：开关/语言即时，时间整组在 Enter 或离开区域时应用，分钟输入在 Enter 或失焦时应用，加减立即；关闭/收起尝试提交有效草稿，未完整或无效时允许关闭并沿用原值，不撤销已生效更改。有效时间/分钟草稿若写盘失败，窗口保留并行内提示供重试，不弹对话框。启动路径通过显式按钮修复。以下 1.3.1 说明保留此前补丁准备的历史，不代表本轮新行为已验证。

## 1.3.1 patch history / 1.3.1 补丁历史

- **Left-click opens Settings directly.** It activates an existing window, recreates one that was closed, and restores a minimized window. / **左键直接打开设置**：已有窗口会激活，关闭后重新创建，最小化后恢复。
- **Right-click keeps the quick menu** for themes, snooze and Exit. / **右键保留快捷菜单**，用于主题切换、延后和退出。
- Three regression cycles cover closing and recreating Settings with a shared icon and restoring the unsaved language choice. See [VALIDATION.md](VALIDATION.md) for actual checks and remaining desktop coverage. / 新增三个窗口生命周期回归用例，实际验证及未覆盖范围见验证记录。

The existing quiet cues, ten-language selection, startup-path repair and MIT license are unchanged. Previous release details are in [CHANGELOG.md](../CHANGELOG.md).

原有安静提醒、十种语言、启动路径修复及 MIT 许可保持不变；此前版本内容见更新记录。

## Downloads / 下载

- `NightCue-v1.3.1-windows-portable.zip`: extract all files to a writable folder and run NightCue.exe. / 完整解压到可写目录后运行。
- `NightCue-source-v1.3.1.zip`: source, documentation and build workflows. / 源码、文档与构建配置。
- `SHA256SUMS.txt`: SHA-256 checksums for both ZIPs. / 两个 ZIP 的校验值。

Requires .NET Framework 4.8 or later. Windows 11 is the local test environment. Windows 10 is a compatibility target; Windows 10 and ARM64 have not been tested on their respective machines. The executable is unsigned. See [compatibility](COMPATIBILITY.md) and the [validation record](VALIDATION.md).

需要 .NET Framework 4.8 或更高版本。当前在 Windows 11 上开发验证；Windows 10 是兼容目标，Windows 10 与 ARM64 尚未真机验证。程序未签名，具体范围见上述文档。

## 1.3.1 historical upgrade instructions / 1.3.1 历史升级说明

Exit the old tray app, replace the program files and preserve `settings.xml` and `state.xml`. Older settings gain the system-language and enabled morning-cue defaults; existing night choices are retained. If you moved the folder, launch the new copy and save with **Run at sign-in** enabled to update the path. Also check Task Manager if Windows has disabled the startup app.

先从托盘退出旧程序，替换程序文件并保留 `settings.xml`、`state.xml`。旧设置默认跟随系统语言、开启早晨提醒，原夜间选择保留。若移动了文件夹，从新位置运行，保持“登录后自动运行”开启并保存，即可更新路径；如 Windows 禁用了启动应用，还需检查任务管理器。

Translations are included and tested for resource completeness and layout; native-speaker review is welcome. A version number in this source tree does not mean a public Release has already been published.

翻译已纳入资源完整性与布局检查，欢迎母语使用者校对。源码中的版本号不代表已经公开发布。
