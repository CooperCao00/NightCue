# Changelog / 更新记录

## 1.3.1 — 2026-11-06

### Fixed

- Left-clicking the tray icon now directly opens or activates Settings. A closed settings window is recreated, and an existing minimized window is restored. Right-click retains the quick menu.
- Added three close/recreate regression cycles using the shared tray icon, checking disposal and restoration of the unsaved language choice.

中文：修复关闭设置后无法通过托盘重新打开的问题。左键直接打开或激活设置，关闭后重建，最小化后恢复；右键保留快捷菜单。补充三个窗口生命周期回归用例。

## 1.3.0 — 2026-11-05

Release preparation; this record does not imply publication on GitHub.

### Added

- Ten languages in one executable, Windows display-language detection, regional fallback, immediate language preview and a saved manual override.
- Locale-aware UI fonts with WPF font fallback; English fallback for unavailable translations.
- Optional light-mode cue after the end time, enabled by default: a steady gold sun, snooze, keep-dark choice and restore action. Day and night decisions are stored independently.
- Settings hint and tray-menu guidance for keeping the tray icon visible.
- Localization, architecture and compatibility documentation.

### Fixed

- Moving an app no longer makes an existing startup registration appear off. Settings identifies an outdated path and repairs it when saved with startup enabled.
- A failed settings save restores the exact previous startup command, including its old location.
- Snooze actions initialize the current period even when clicked across a schedule boundary; keep-theme actions preserve the meaning of the displayed menu command.
- Windows 10 skips window-caption attributes documented only for Windows 11; application content still uses its light/dark palette.

### Maintenance

- Split the application into Domain, Persistence, Platform, Shell, Localization and App folders while retaining a single project, namespace, executable and compatible XML fields.
- Expanded regression coverage for translation resources, live language preview, UI rendering, daytime reminders, legacy XML and startup transaction failures.
- Preserved manual-only theme changes, the 23:00–07:00 default and existing single-instance identifiers.

中文：同一个程序内置十种语言，默认跟随系统，支持即时预览与保存固定；新增可关闭的早晨浅色轻提醒，白天与夜间分别记忆选择。修复目录迁移后的启动路径提示和保存失败回滚；按职责拆分源码，保留旧 XML 兼容。主题始终由用户主动切换。

## 1.2.0 — 2026-11-04

Prepared for the first public release; this record does not imply publication on GitHub.

### Added

- MIT license, credited to NightCue contributors.
- English and Simplified Chinese READMEs, quick starts, contributor and community guidance, security reporting instructions and a publishing checklist.
- GitHub issue forms, pull request template, Windows build/test/package workflow, and Dependabot updates for Actions.
- Source and portable release packaging with SHA-256 checksums; consistent assembly and manifest versions.
- Regression coverage for non-Gregorian system culture, null state, XML handling, option boundaries and light/dark UI resource loading.

### Fixed

- Night identifiers now use the Gregorian invariant format regardless of the system calendar.
- Null decisions in saved state no longer break reminder evaluation.
- Configuration loading explicitly rejects DTDs, external entities and oversized documents.

### Maintenance

- Consistent C# formatting, project inclusion checks and local documentation link validation.
- Preserved the 23:00–07:00 default, manual-only theme changes and existing single-instance identifiers.

中文：补齐 MIT 许可、双语文档、协作规范、反馈模板、Windows 自动构建及发布打包；修复地区日历影响夜间标记和空状态问题，限制 XML 解析，增加回归检查。默认提醒时段和手动切换逻辑不变。

## 1.1.0 — 2026-11-03

Local development version: redesigned the settings window with WPF, light/dark palettes, vector artwork and a themed tray menu; packaged the original Visual Studio source project.

## 1.0.0 — 2026-11-03

Local development version: introduced the quiet tray reminder, overnight schedule, snooze, keep-light decision, theme hotkey, optional sign-in startup and local persistence.
