# Troubleshooting / 故障排查

| Problem / 问题 | What to check / 处理方式 |
| --- | --- |
| No visible reminder / 看不到提醒 | Make the icon visible beside the clock; check the start/end times. The cue changes within about 10 seconds while the PC is awake. / 显示托盘图标并核对时段，运行时约 10 秒内刷新。 |
| It stays gray / 一直是灰色 | It may be outside the reminder window, snoozed, or dismissed tonight. Use 恢复今晚提醒 while the theme is light. / 可能尚未到时、延后或今晚已处理；在浅色状态下选择“恢复今晚提醒”。 |
| Another launch shows the old version / 新副本打开旧版 | Exit the running copy from its tray menu, then launch the intended executable. All copies share a single instance. / 先从托盘退出正在运行的副本。 |
| Hotkey does nothing / 快捷键无效 | Check the hotkey switch and conflict hint in Settings; use the tray menu if another app owns the shortcut. / 查看开关和占用提示，仍可使用托盘菜单。 |
| Some apps stay light / 部分应用不变色 | Enable “follow system” in those apps; NightCue does not override their own theme settings. / 将对应应用设为跟随系统。 |
| Theme changes back unexpectedly / 主题自行变回 | Check other theme schedulers. NightCue changes themes only after a click or its hotkey. / 检查其他工具的定时切换。 |
| Startup fails or settings cannot save / 启动失败或保存失败 | Use a writable folder and check error.log. With the app stopped, back up and rename malformed settings.xml/state.xml to reset them. / 使用可写目录；退出后备份并重命名损坏的 XML 可恢复默认值。 |
| It no longer starts at sign-in / 登录不再启动 | Moving the folder invalidates the old path. Disable startup, save, then enable it from the new location. / 路径可能已变化，在新位置关闭并重新启用登录启动。 |
| PowerShell blocks build scripts / 构建脚本被阻止 | Use an approved PowerShell environment or build NightCue.sln in Visual Studio with the 4.8 targeting pack. No machine-wide policy change is required. / 使用获准环境或 Visual Studio，无需修改全局执行策略。 |

The app never wakes a sleeping computer. After resume it evaluates the current local time; snooze deadlines use UTC. Changing time zone changes the local reminder window. A dark theme observed during the window is treated as tonight's completed choice; switching back to light externally does not automatically re-enable the cue.

程序不会唤醒休眠电脑。恢复后按当前本地时间判断提醒时段，延后截止时间按 UTC 保存。更改时区会影响本地提醒时段。夜间观察到深色后，视为今晚已处理；在外部切回浅色不会自动恢复提醒。

When reporting a bug, include the NightCue version, Windows version, steps, expected and actual behavior. Share only the relevant log excerpt after removing personal paths. Use the repository's bug-report form.

反馈问题时请提供程序版本、Windows 版本、复现步骤和预期/实际表现，只附去除个人路径后的相关日志片段。
