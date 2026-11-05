# Compatibility / 兼容性

NightCue targets **.NET Framework 4.8** and is built as **AnyCPU**. It uses Windows desktop components, without third-party runtime packages. A compatibility target describes the design intent; it does not substitute for a test on that operating system.

NightCue 目标框架为 **.NET Framework 4.8**，构建为 **AnyCPU**，使用 Windows 桌面组件，无第三方运行时包依赖。“兼容目标”表示设计范围，不等于已在对应系统验证。

| Environment / 环境 | Status / 状态 |
| --- | --- |
| Windows 11 | Local build, automated regression and selected UI checks; exact scope in [VALIDATION.md](VALIDATION.md) / 本地构建、自动回归及部分界面操作已验证 |
| Windows 10 with .NET Framework 4.8 | Compatibility target; no Windows 10 machine or VM test yet / 兼容目标，尚无 Windows 10 真机或虚拟机验证 |
| Windows ARM64 | Untested; AnyCPU does not establish ARM64 compatibility / 未验证，不能仅凭 AnyCPU 宣称兼容 |
| Older Windows, Windows Server, Wine | Outside the current verification scope / 未纳入当前验证范围 |
| macOS / Linux | No native build; Windows APIs and desktop components are required / 无原生版本，依赖 Windows API 与桌面组件 |

## Framework and window appearance / 框架与窗口外观

Microsoft lists .NET Framework 4.8 as included with Windows 10 22H2 and the original Windows 11 release; later Windows 11 releases include 4.8.1. NightCue's 4.8 target avoids requiring a separate modern .NET desktop runtime. This framework information supports the compatibility target, not a claim that NightCue has passed every OS test. [Microsoft system requirements](https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements).

微软列出的 Windows 10 22H2 和初版 Windows 11 自带 .NET Framework 4.8，后续 Windows 11 版本自带 4.8.1。此依据支持框架选择，但不能替代 NightCue 的实际系统测试，也不代表对操作系统支持生命周期作出承诺。

Windows 11 can use optional dark-caption, corner-preference and caption-color attributes. NightCue only attempts these on build 22000 or later. Windows 10 retains its native title bar and corners; the settings content and tray menu still use NightCue's own light/dark palettes. Microsoft's attribute documentation marks these options as supported from Windows 11 build 22000. [DWM window attributes](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute).

Windows 11 可使用深色标题栏、圆角偏好与标题栏颜色等可选属性；NightCue 仅在 build 22000 起尝试。Windows 10 保留原生标题栏和边角，设置内容与托盘菜单仍可切换自身浅深色配色。此降级避免把可选的 Windows 11 外观功能作为运行前提。

## Remaining desktop checks / 尚需验证

- Windows 10 actual startup, tray behavior, themes, languages and layout. / Windows 10 实际启动、托盘、主题、语言与布局。
- Startup enabled and disabled across a real sign-out/sign-in or reboot, including Task Manager overrides. / 实际注销登录或重启后的启动开关效果，以及任务管理器禁用状态。
- ARM64 execution, comprehensive screen-reader and high-contrast use, and mixed-DPI multi-monitor behavior. / ARM64、完整读屏/高对比度与混合 DPI 多屏行为。
- Native-speaker translation review and missing optional language fonts. / 母语翻译校对与可选字体缺失场景。

The app only changes the current user's Windows/app theme preferences. Individual applications must support following those preferences; fixed themes and web-page themes are not forced. Windows controls whether a tray icon is hidden or visible. Neither limitation is solved by packaging a separate EXE for each language or Windows version.

程序只修改当前用户的系统/应用主题偏好；应用需支持跟随该偏好，固定主题与网页不会被强制覆盖。托盘是否折叠由 Windows 管理。按语言或系统版本分别打包不能消除这些限制。
