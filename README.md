# NightCue · 夜色提醒

**A quiet reminder to switch themes when you turn off the lights.**

English · [简体中文](README.zh-CN.md) · [MIT license](LICENSE)

NightCue is a small Windows tray app for people whose room lighting does not follow a fixed schedule. At night, an amber moon waits beside the clock. Switch to dark mode when you are ready, snooze the cue, or keep the light theme for tonight. After the end time, a gold sun can offer light mode just as quietly.

There are no sounds, notification banners, flashing icons, or automatic theme changes. The app does not try to infer whether you are focused or whether your room lights are on.

![NightCue settings in light mode](docs/images/light-en.png)

<details>
<summary>Dark appearance</summary>

![NightCue settings in dark mode](docs/images/dark-en.png)

</details>

## Features

- Configurable reminder window, **23:00–07:00** by default; supports crossing midnight.
- A steady amber moon for the night cue and a gold sun for the light-mode cue after the end time.
- Switch both Windows and app themes manually, using the tray menu or **Ctrl + Alt + Shift + D**.
- Snooze for **15 minutes** by default, adjustable from 1 to 120 minutes.
- Keep light mode tonight or keep dark mode until the next start time. Night and daytime choices and snoozes are independent and survive restarts.
- Optional light-mode reminder after the end time, enabled by default. Neither cue changes the theme automatically.
- Optional sign-in startup, local settings, and light/dark settings-window styling.

One executable includes **English, Simplified Chinese, Traditional Chinese, Japanese, Korean, German, French, Spanish, Brazilian Portuguese and Russian**. It follows the Windows display language by default, with English as fallback. Choosing a language applies and saves it immediately. Closing Settings does not undo a saved language change. No separate language downloads are needed. [Language and font behavior](docs/LOCALIZATION.md).

The default morning cue appears from **07:00 until the next 23:00** if the theme is still dark. You can switch to light, snooze, keep dark for this period, or disable this cue in Settings.

## Get started

The automatic-saving changes described here are in the current working source and have not been packaged as a new release.

Download `NightCue-v1.3.1-windows-portable.zip` from this repository's **Releases** section once a release has been published. If none is available, build from source below.

1. Extract the entire ZIP to a folder you can write to. Keep this folder in a stable location if you enable sign-in startup.
2. Run `NightCue.exe`. No administrator access is needed.
3. Make NightCue visible next to the clock using Windows taskbar settings or by dragging it out of the hidden-icons menu. The tray menu includes visibility guidance; Windows controls pinning.
4. Left-click the tray icon to open **Settings**. Changes save automatically; select **Hide to tray** when finished.

Switches and language save immediately. For the two times, press Enter or leave the time section to apply them together. Minutes apply on Enter or leaving the field; the +/− buttons apply immediately. Closing tries to submit valid drafts. Incomplete or invalid input allows closing and keeps the last valid values. If a valid time or minute draft cannot be written, the window stays open with an inline error so you can retry; no dialog appears. A settings-file write failure keeps the previous setting active and is shown beside the affected control. Automatic saving does not change the manual-only theme behavior.

Left-click opens or activates Settings, recreates it after closing, and restores it when minimized. Right-click opens the quick menu for theme switching, snooze and Exit. This reopening behavior is fixed in **1.3.1**.

| Tray icon | Meaning |
| --- | --- |
| Amber moon | A night-theme choice is pending |
| Gold sun | A light-theme choice is pending after the end time |
| Gray ring | Waiting, snoozed, or dismissed for the current period |
| Blue moon | Dark mode, with no active cue |

“15 minutes” means the delay after you explicitly choose **Remind me in 15 min**. It is not a recurring pop-up. [English quick start](docs/QUICKSTART.en.txt) · [中文使用说明](docs/使用说明.txt).

## Requirements and limitations

- Windows 11 is the local development and test environment. Windows 10 with .NET Framework 4.8 is a compatibility target, but has not been tested on a Windows 10 machine. Windows ARM64 is also unverified. See the [compatibility matrix](docs/COMPATIBILITY.md) and [validation record](docs/VALIDATION.md) for the exact scope.
- Requires **.NET Framework 4.8 or later** with the Windows desktop components. The app is AnyCPU and has no third-party runtime packages.
- Apps and websites must support following the Windows theme. Fixed app themes are not overridden. Mixed Windows/app themes are treated as not fully dark.
- Disable competing theme schedules in PowerToys Light Switch or Auto Dark Mode to avoid conflicting changes.
- The current executable is unsigned; Windows may display a reputation warning. Verify the release checksum or build from source if needed.
- High-contrast mode, screen-reader support, and mixed-DPI multi-monitor layouts have not been comprehensively tested.

## Build and test

On Windows with .NET Framework 4.8 installed, open PowerShell in the repository root:

```powershell
.\build.ps1
```

The script compiles with the Windows .NET Framework compiler, checks repository metadata, and runs the regression suite without starting the tray app or modifying Windows settings. PowerShell 7 is locally verified. The scripts use PowerShell 5.1-compatible syntax; local execution policies still apply. If scripts are blocked, use your approved development environment or Visual Studio.

Outputs are `artifacts/app/NightCue.exe` and `artifacts/tests/NightCue.Tests.exe`. To use Visual Studio, open `NightCue.sln` with the **.NET desktop development** workload and **.NET Framework 4.8 targeting pack**, build Release, and run the test executable. [Contributor guide](CONTRIBUTING.md) covers both build paths.

```powershell
# Build, test, and create source + portable ZIPs and SHA-256 checksums
.\scripts\package.ps1
```

GitHub Actions checks both build paths on Windows and uploads packages as workflow artifacts. These are build artifacts; a maintainer publishes Releases separately. [Publishing guide / 发布指南](docs/PUBLISHING.md).

## Data, startup, and uninstall

NightCue makes no network requests and includes no analytics or automatic updater. `settings.xml` and `state.xml` remain beside the executable. Diagnostics and recovery files are stored in `%LOCALAPPDATA%\NightCue\Diagnostics`; **Open diagnostics** opens that folder. Logs and recovery backups can contain local paths or configuration data: review them before sharing. Keep the application in a writable folder.

Settings shows whether startup is off, registered for this copy, registered elsewhere, or unreadable. The startup switch applies immediately. On failure, feedback is shown and registration is read again; if it remains unreadable, startup controls are disabled and the status is unknown. For another registered location, click **Repair startup path**; after repair the button returns to **Windows startup settings**. Unreadable registration blocks startup changes, while other settings can still save without touching the startup entry. NightCue does not change Windows' separate startup-disable decision. A registered path alone does not prove that sign-in startup works.

If settings or reminder state cannot be loaded, NightCue uses defaults for the affected file and shows a persistent recovery notice in the left panel. For corrupt or invalid files it attempts to preserve the original in diagnostics before saving defaults. If reading or backup fails, it keeps defaults in memory and protects the original from writes for that process. **Dismiss recovery notice** clears the notice, not the backup or the underlying access problem. See [recovery details](docs/TROUBLESHOOTING.md).

Choosing a theme writes only the current user's `AppsUseLightTheme` and `SystemUsesLightTheme` values under `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`. Enabling sign-in startup adds a `NightCue` value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. It does not install a service or scheduled task.

To uninstall, turn off **Run at sign-in**, confirm success, choose **Exit** from the tray menu, then delete the extracted folder. Your current Windows theme remains as chosen. When upgrading, exit the old app and preserve `settings.xml` and `state.xml`. If you moved the folder, run the new copy and click **Repair startup path** when an old location is registered. If Windows has disabled the startup app, use **Windows startup settings** or Task Manager to enable it yourself. Actual sign-in behavior still needs to be checked on your machine. [Troubleshooting](docs/TROUBLESHOOTING.md).

## Contributing

Bug reports, translations, accessibility improvements, and focused pull requests are welcome in English or Chinese. Start with the [architecture guide](docs/ARCHITECTURE.md), [translation guide](docs/LOCALIZATION.md), [CONTRIBUTING.md](CONTRIBUTING.md), [the code of conduct](CODE_OF_CONDUCT.md), and [the changelog](CHANGELOG.md). For security concerns, see [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE) · Copyright (c) 2026 NightCue contributors. Project code, original artwork, and documentation are covered by this license. Windows and .NET remain subject to their respective licenses.
