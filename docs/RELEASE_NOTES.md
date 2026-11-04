# NightCue 1.2.0 · 夜色提醒

A quiet Windows tray reminder for switching themes when you are ready. Default reminder window: **23:00–07:00**. No sounds, banners or automatic theme changes.

安静的 Windows 夜间主题提醒。默认 **23:00–次日 07:00** 提醒；等你关灯或有空时，再手动切换深色。无声音、横幅或自动换色。

- Manual light/dark switching, configurable snooze, keep-light-tonight choice and optional sign-in startup.
- Simplified Chinese UI; English and Chinese documentation.
- MIT license, credited to NightCue contributors.
- This release adds open-source project documentation, build/package automation and locale/XML regression checks.

## Downloads / 下载

- `NightCue-v1.2.0-windows-portable.zip`: extract all files to a writable folder and run NightCue.exe. / 完整解压到可写目录后运行。
- `NightCue-source-v1.2.0.zip`: complete source project, documentation and workflows. / 完整工程、文档和自动构建配置。
- `SHA256SUMS.txt`: SHA-256 checksums for both ZIPs. / 两个 ZIP 的校验值。

Requires .NET Framework 4.8 or later. Windows 11 is the manually tested environment; Windows 10 and ARM64 are not yet manually verified. The executable is unsigned. See the README for remaining limitations.

需要 .NET Framework 4.8 或更高版本。已手动验证 Windows 11；Windows 10、ARM64 尚未手动验证。程序未签名，其他限制见 README。

Upgrade: exit the old tray app before extracting the new files into its folder; preserve settings.xml and state.xml. Keep sign-in startup pointed at the same location, or disable it before moving and re-enable it afterwards.

升级前先从托盘退出旧版，再覆盖程序文件，保留 settings.xml、state.xml；移动位置前关闭登录启动，移动后重新开启。
