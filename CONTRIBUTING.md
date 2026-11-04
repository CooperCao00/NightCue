# Contributing / 贡献指南

English and Chinese contributions are welcome. Keep the core promise intact: a reminder may change a tray indicator; a theme change requires a user's action. Do not add unsolicited notifications, activity monitoring, or telemetry.

欢迎中文或英文贡献。核心约定：提醒只改变托盘标记；切换主题需要用户操作。请勿引入主动弹窗、活动监控或遥测。

## Development / 开发

Requirements: Windows, .NET Framework 4.8, PowerShell. No NuGet restore is required.

```powershell
.\build.ps1
```

For IDE development use Visual Studio with .NET desktop development and the .NET Framework 4.8 targeting pack. In its Developer PowerShell:

```powershell
msbuild NightCue.sln /t:Rebuild /p:Configuration=Release /m
.\tests\NightCue.Tests\bin\Release\NightCue.Tests.exe
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
```

`build.ps1` and the solution must include the same source files and embedded XAML. The XAML is loaded at runtime from the resource **NightCue.SettingsView.xaml**; keep its build action as EmbeddedResource, not Page. Run tests after changes. For UI changes, also inspect the app manually on Windows and attach screenshots with personal information removed.

两种构建方式应包含相同源码和资源。XAML 的构建类型是 EmbeddedResource，不能改成 Page。修改后运行测试；涉及界面时，还需在 Windows 上人工检查，并提供去除个人信息的截图。

## Source map / 源码导览

| Path | Responsibility |
| --- | --- |
| `src/NightCue/NightCue.cs` | Scheduling, local persistence, Windows integration and tray lifecycle |
| `src/NightCue/SettingsForm.cs` | WPF/WinForms host, input validation, appearance |
| `src/NightCue/SettingsView.xaml` | Settings layout and original vector illustrations |
| `src/NightCue/Properties/AssemblyInfo.cs` | Assembly identity and version |
| `tests/NightCue.Tests/Program.cs` | Dependency-free regression runner and UI resource smoke checks |
| `scripts/check.ps1` | Metadata, project inclusion, and documentation link checks |
| `scripts/package.ps1` | Build, test, package and SHA-256 generation |

Use four spaces in C#, two in XAML/YAML, and follow `.editorconfig`. The direct build uses the .NET Framework compiler, so keep C# syntax compatible with C# 5. Prefer small changes with regression tests for behavior changes. Documentation-only changes need link checks, not new behavior tests.

源码中的单实例名称保留了早期版本的标识，便于与旧版本互斥；它们不是作者署名。不要随意改动，否则新旧副本可能同时运行。

## Before a pull request / 提交前

- Describe the problem and the resulting behavior; reference an issue if one exists.
- Run `.\build.ps1`; explain any checks you could not run.
- Update both READMEs when user-visible behavior or setup instructions change.
- Never commit settings, state, logs, generated executables, local paths, secrets, or build outputs.
- Keep dependencies minimal. Discuss large changes or new dependencies in an issue first.

请说明解决的问题、修改后的行为和验证结果；影响用户的变化应同步中英文文档。不要提交个人设置、日志、程序产物、凭据或本机路径。较大的改动、新依赖，建议先开 issue 讨论。

## Release changes / 版本维护

Keep `VERSION`, assembly versions, the application manifest, README download filenames and `CHANGELOG.md` consistent. Run `.\scripts\package.ps1`; see [PUBLISHING.md](docs/PUBLISHING.md) for the release checklist. A version number in the source does not imply that a GitHub Release already exists.

## Contribution license / 贡献许可

By submitting a contribution, you agree that it may be distributed under the project's [MIT license](LICENSE). Only submit material you are entitled to contribute. No separate CLA is required.

提交贡献即表示同意按本项目 MIT 许可分发该贡献；请确保有权提供相关内容，无需另签 CLA。
