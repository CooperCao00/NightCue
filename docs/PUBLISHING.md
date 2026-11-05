# Publishing NightCue / 发布指南

This repository is prepared for publication. Repository creation, account-specific settings and publishing a GitHub Release are separate maintainer actions. No repository owner or download URL is hard-coded.

本工程已准备发布所需文件；创建 GitHub 仓库、配置仓库权限和正式发布 Release 需要维护者在自己的账号下完成。工程没有写入虚构的仓库地址。

## First publication / 首次发布

1. Create a GitHub repository named NightCue (or your preferred name). Upload/commit the **contents** of the source folder at the repository root, including hidden `.github`, `.gitignore`, `.gitattributes` and `.editorconfig` files. Do not add a second generated license or README. GitHub Desktop or Git preserves hidden files more reliably than dragging files in a browser.
2. Set the repository description, for example: “A quiet Windows tray reminder to switch themes when you turn off the lights.” Suggested topics: `windows`, `dark-mode`, `tray-app`, `csharp`, `wpf`, `winforms`.
3. Enable Issues and GitHub Actions. Enable **private vulnerability reporting** under the repository security settings before inviting security reports. Consider branch protection once the first CI run provides a required check name.
4. Confirm the **Windows build** workflow completes successfully. Download its release-packages artifact and inspect the files. Workflow artifacts are not public Releases.
5. Perform the manual checks below on a Windows desktop. Create a **draft** GitHub Release tagged `v1.3.1` at the verified commit. Attach the two ZIPs and `SHA256SUMS.txt` from that same build. Use [RELEASE_NOTES.md](RELEASE_NOTES.md) as the description.
6. Review the draft and publish. After publishing, confirm that the README's Releases instructions lead to the expected files. No authentication tokens or local user data belong in the repository or archives.

中文要点：把源码文件夹的内容（含隐藏文件）放在仓库根目录；启用 Issues、Actions 和私密漏洞报告；等待 Windows 构建通过；先创建草稿 Release，附同一次构建产出的两个 ZIP 和校验文件，人工检查后再发布。可使用 GitHub Desktop 避免漏传隐藏文件。

## Local packaging / 本地打包

```powershell
.\scripts\package.ps1
Get-FileHash .\artifacts\packages\NightCue-v1.3.1-windows-portable.zip -Algorithm SHA256
```

The packaging script builds and tests in a fresh staging directory, uses an explicit source allowlist (`scripts/source-files.txt`), includes the license and help in the portable ZIP, checks for accidental runtime data, and writes checksums. It never starts the application or publishes anything. Add new source files to the allowlist before packaging.

打包脚本每次在新的暂存目录中构建测试，使用明确的源码清单，生成校验值，不启动应用或上传文件。新增源码后请同步 `scripts/source-files.txt`。构建输出包含编译时间等信息，不宣称不同机器生成的 ZIP 或 EXE 字节完全一致。

## Manual desktop checks / 人工验证

- Verify left-click opens Settings after closing and restores a minimized window; right-click must retain the quick menu. Repeat with the same EXE already running.
- Pin the tray icon. Confirm the cue becomes amber at the configured start and returns quietly after snoozing; no sound, banner or focus change.
- Test an overnight window, keep-light choice, restart, restore and end time. At End, confirm the optional gold-sun cue appears while dark; test snooze, keep-dark and restore independently of the next night. Neither boundary may change the theme.
- Check both theme buttons, tray commands, hotkey enable/disable/conflict and external theme changes.
- Check the settings window in light/dark mode, long translated strings, the language dropdown, keyboard navigation and relevant DPI settings. Verify manual-language save, unsaved cancellation and Follow Windows on a different display language.
- Test startup enabled/disabled with an actual sign-out/sign-in or reboot, not only a registry read. Test moving the folder and saving to repair the path; check Task Manager overrides and uninstall. Restore personal settings changed during testing.
- Exit the old version before upgrade; preserve XML files. Test a clean unzip in a writable path, including a path with spaces and non-ASCII characters.

人工验证包括关闭设置后左键重开、最小化恢复、右键菜单及重复启动，再检查夜间/早晨独立提醒、延后、跨午夜、重启、手动切换、快捷键、十种语言的浅深色布局、下拉操作、保存/取消、缩放、迁移后的启动路径、实际注销登录或重启及卸载。注册项存在不能替代登录验证。升级前退出旧程序并保留 XML；测试纯净解压路径及包含中文/空格的路径。自动测试不代替这些桌面检查。

## Validation record / 验证记录

See [VALIDATION.md](VALIDATION.md) for what was actually checked locally. Do not claim that GitHub CI or an untested Windows version passed until you have evidence. Record later platform results in release notes.

## Subsequent releases / 后续版本

Update `VERSION`, `Properties/AssemblyInfo.cs`, the manifest, README filenames, quick-start version and changelog together. Run checks, build and package. Publish from a known commit, keep tag and package versions equal, and attach a new checksum file for each release. Never silently replace published binaries.
