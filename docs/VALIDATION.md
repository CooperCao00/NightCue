# Validation record / 验证记录

Release preparation: 1.2.0, 2026-11-04.

This file records checks actually performed; it is not a certification of every Windows configuration.

本文件仅记录实际执行的检查，不代表已覆盖所有 Windows 配置。

## Automated checks / 自动检查

| Check / 检查 | Local result / 本地结果 |
| --- | --- |
| Framework compiler build (`build.ps1`, PowerShell 7) | Passed, compiler warnings treated as errors / 通过，编译警告视为错误 |
| Regression runner | 37 checks passed, including schedule boundaries, restart persistence, culture changes, XML limits and light/dark UI rendering / 37 项通过 |
| Solution Release build | Passed using Windows MSBuild and Microsoft's .NET Framework 4.8 reference assemblies 1.0.3, with explicit TargetFrameworkRootPath and FrameworkPathOverride; final build had no warnings or errors / 通过，无警告或错误 |
| Source ZIP unpacked to a new path containing spaces and Chinese characters | Build and all 37 checks passed / 重新构建及 37 项检查通过 |
| Documentation links and version metadata | Passed / 通过 |
| GitHub workflow, Dependabot and issue-form YAML | Five files parsed locally; hosted execution awaits repository publication / 5 个文件本地解析通过，远端运行待上传后验证 |
| Source and portable archives | Source uses a 39-file allowlist; both include MIT license; no user settings/state/log files / 源码清单 39 个文件，两包均含许可且不含个人配置与日志 |

The light/dark render outputs were visually reviewed. The checks do not launch the tray application, register startup/hotkeys, or change the user's theme. No Visual Studio installation was required for the local solution check; Microsoft reference files were downloaded to a temporary working folder and are not bundled in either release archive.

浅深色渲染结果已检查。自动检查不启动托盘程序，不注册登录启动/快捷键，不修改用户主题。本地工程验证使用临时下载的微软参考程序集，没有安装 Visual Studio，也没有将这些参考文件放入发布包。

Run `build.ps1` to reproduce compiler, metadata, documentation link and regression checks; run `scripts/package.ps1` to create clean archives and checksums. GitHub-hosted CI, a fresh Visual Studio IDE session and Windows PowerShell 5.1 execution have not been verified in this environment. Local script-policy restrictions prevented the PowerShell 5.1 run; PowerShell 7 was used without changing machine policy.

可运行 `build.ps1` 复现构建与回归检查，使用 `scripts/package.ps1` 打包。当前环境未验证 GitHub 托管 CI、全新 Visual Studio IDE 会话和 Windows PowerShell 5.1 执行；本机策略限制了 5.1 脚本运行，已使用 PowerShell 7，未更改系统策略。

## Desktop coverage / 桌面验证范围

The existing interface was manually exercised on Windows 11 during 1.1.0 development: light/dark appearance, theme controls, hotkey, snooze and sign-in startup. Version 1.2.0 preserves that interface and adds automated resource-loading checks. A full new manual desktop pass and GitHub-hosted CI run remain release-maintainer checks.

1.1.0 开发期间已在 Windows 11 上人工检查浅深色界面、主题操作、快捷键、延后及登录启动。1.2.0 保留该界面并加入资源加载检查；完整的新一轮桌面验证和 GitHub 托管 CI 运行仍需发布维护者完成。

Windows 10, Windows ARM64, comprehensive screen-reader/high-contrast behavior and mixed-DPI multi-monitor operation are not yet manually verified.
