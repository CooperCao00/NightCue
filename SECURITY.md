# Security policy / 安全政策

## Supported versions

Security fixes target the latest published stable release. Older versions do not have a separate backport commitment. This source tree prepares version 1.3.1; publication is a separate step.

## Report a vulnerability

Use **Security → Advisories → Report a vulnerability** on the canonical GitHub repository if private vulnerability reporting is enabled. Maintainers should enable this before public launch; see [the publishing guide](docs/PUBLISHING.md).

If that option is unavailable, open a minimal issue asking maintainers to enable a private reporting channel. Do not include an exploit, sensitive logs, or vulnerability details in that public request. No security mailbox or guaranteed response time is currently designated.

Privately include the affected version, Windows version, reproduction steps, expected impact, and suggested mitigation if known. Remove usernames and unrelated information from logs.

## Scope

NightCue runs as the current user. It changes current-user theme settings only on request and adds a current-user startup entry only when enabled. It makes no network requests, collects no telemetry, and does not download updates. XML configuration rejects DTDs/external entities and limits document size. Diagnostics and recovery files are stored locally in `%LOCALAPPDATA%\NightCue\Diagnostics`, while settings/state remain beside the executable. Logs may contain paths and startup commands; recovery backups preserve configuration content. They are not uploaded automatically. Inspect and redact them before sharing. Acknowledging a recovery notice deletes only its marker; backups and logs remain. The folder and recovery marker are shared by copies under the same Windows user. Logs rotate, but backup count is not automatically limited. The app does not override Windows' startup-disable decision, and unreadable startup registration blocks startup changes only; unrelated settings do not rewrite the startup entry.

These controls do not provide a security boundary against other programs already running as the same user.

## 中文

安全修复面向最新已发布的稳定版，不承诺向旧版单独回移。当前源码准备的版本是 1.3.1，公开发布是另一步骤。

请优先使用仓库 **Security → Advisories → Report a vulnerability** 私密报告。若该入口未启用，可提交仅请求开放私密渠道的 issue，不要公开漏洞细节、利用步骤或敏感日志。当前未指定安全邮箱，也未承诺固定响应时限。

私密报告请包含受影响版本、Windows 版本、复现方式、影响和可能的缓解办法，日志应去除个人信息。程序只以当前用户身份运行，不联网、不收集遥测；不防御已经以同一用户身份运行的其他程序。

诊断与恢复文件保存在 `%LOCALAPPDATA%\NightCue\Diagnostics`，设置/状态仍在程序旁。日志可能包含路径、启动命令，恢复备份包含配置内容，不会自动上传。分享前请检查并去除个人信息。确认恢复提示只清除标记，日志与备份保留；删除程序目录也不会删除这些诊断文件。同一 Windows 用户的副本共用目录与提示标记；日志会轮转，备份数量没有自动上限。程序不覆盖 Windows 的启动禁用决定，启动注册信息不可读时只阻止自启动更改，其他设置保存不改启动项。
