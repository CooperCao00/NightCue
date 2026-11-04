# Changelog / 更新记录

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
