# Localization / 国际化与翻译

NightCue ships every translation in one executable. Translations need no separate download or locale-specific release. The application is offline and uses installed Windows fonts instead of bundling font files.

NightCue 将全部翻译嵌入同一个 EXE，不按语言分别打包，也不下载语言或字体包；字形由本机 Windows 字体提供。

## Included languages / 内置语言

| Code | Language / 语言 | Preferred UI font / 首选字体 |
| --- | --- | --- |
| `en` | English | Segoe UI |
| `zh-CN` | 简体中文 | Microsoft YaHei UI |
| `zh-TW` | 繁體中文 | Microsoft JhengHei UI |
| `ja` | 日本語 | Yu Gothic UI |
| `ko` | 한국어 | Malgun Gothic |
| `de` | Deutsch | Segoe UI |
| `fr` | Français | Segoe UI |
| `es` | Español | Segoe UI |
| `pt-BR` | Português (Brasil) | Segoe UI |
| `ru` | Русский | Segoe UI |

The selector shows language names in their own language. The translations are included and checked for keys, placeholders and layout; this is not a claim of complete native-speaker review. Corrections are welcome.

语言列表使用各语言自己的名称。资源已检查键名、占位符和布局，不代表全部经过母语使用者审校；欢迎纠正措辞。

## Selection and fallback / 选择与回退

1. A saved manual selection takes priority. `Language = "system"` (the default, including older XML files) reads `CurrentUICulture`, the Windows display-language context, at startup.
2. An exact supported locale is used directly. Traditional Chinese script or Taiwan/Hong Kong/Macao selects `zh-TW`; other Chinese variants select `zh-CN`. Portuguese variants currently use `pt-BR`.
3. Other regional variants use their available base language, such as `de-CH → de` or `fr-CA → fr`. Unsupported languages fall back to `en`.
4. A missing translation key falls back to English. Unknown keys absent from English are programming errors. Release checks require all included languages to contain the English key set.

手动选择优先；默认 `system` 读取 Windows 显示语言，而非地区、时区或日历。繁体及台港澳使用繁中，其他中文变体使用简中；葡萄牙语暂用巴西葡语，其余地区变体回退到已有基础语言，不支持时使用英语。缺少词条时回退英语，但发布检查要求内置资源键名齐全。

Selecting a language previews it immediately in the settings and tray text. **Save & hide** persists it. Closing without saving restores the previously saved preference. When **Follow Windows** is saved, restart NightCue after changing the Windows display language. OS-owned dialog buttons and window-system menus may continue to follow Windows rather than the app selection.

选择语言会立即预览设置和托盘文案，保存后才持久化；关闭未保存的设置会恢复原偏好。选择跟随系统时，修改系统显示语言后需重启 NightCue。系统原生对话框按钮、窗口系统菜单仍可能使用 Windows 自身语言。

## Fonts and layout / 字体与布局

The WPF view uses the locale's preferred font followed by `Global User Interface`, and sets its `Language` property to the resolved locale. WPF provides composite-font fallback; the Windows installation determines which optional fonts are available. The project does not redistribute Microsoft font files. [Microsoft WPF globalization guidance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/wpf-globalization-and-localization-overview).

WPF 视图按语言指定首选字体，再使用 `Global User Interface` 回退，并设置对应的 `Language`。可用字形仍取决于 Windows 已安装字体，项目不分发微软字体文件。新增语言应测试长文案、换行、下拉列表和数字输入。

All current languages use left-to-right layout. Arabic, Hebrew and other right-to-left languages are not yet included. Adding one requires explicit layout and keyboard-navigation work, not just another resource file.

当前十种语言均使用从左到右布局。阿拉伯语、希伯来语等尚未纳入；增加时需要处理布局方向与键盘导航，不能只添加翻译文件。

## Contributing a translation / 添加或修改翻译

1. Start from [en.xml](../src/NightCue/Localization/Languages/en.xml). Add or edit an XML file in `src/NightCue/Localization/Languages/`, with a unique locale `code`, native `name` and suitable Windows `font`.
2. Keep every key and each format placeholder such as `{0}` unchanged. Translate text only. Escape XML characters such as `&amp;`; preserve intentional line breaks. Use concise, natural wording and avoid changing the reminder's meaning.
3. Resources are embedded automatically by both build paths as `NightCue.Languages.<code>.xml`. Add new files to `scripts/source-files.txt`; update the expected language count, fallback cases and UI checks in the test runner as needed.
4. Run `build.ps1`. Inspect both light and dark render outputs, then try the real language list, keyboard navigation, validation messages and save/cancel behavior. Test missing optional fonts on a suitable machine where possible.
5. Update the language lists in both READMEs and this document. Mention whether a native speaker reviewed the translation.

以英语 XML 为模板，保留键名和 `{0}` 等占位符，填写原生语言名及合适字体。两种构建会自动嵌入语言文件，但仍需更新源码打包清单和相关测试。完成资源检查后，实际检查浅深色布局、下拉列表、键盘操作、错误提示及保存/取消，并同步中英文文档。

Time inputs retain 24-hour `HH:mm` format in every language. State identities use the invariant Gregorian date, and snooze deadlines use UTC. Translation must not change these persistence formats.

所有语言的时间输入保持 24 小时 `HH:mm`；状态标记使用固定公历日期，延后截止时间使用 UTC。翻译不能改变这些存储格式。
