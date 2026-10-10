# 参考项目及第三方内容说明

作者：[陈小铭](https://space.bilibili.com/2121431899)。本项目的代码以 MIT 许可证发布，作者及许可声明见 LICENSE。

以下公开项目与文档提供了接入思路参考。本项目独立编写实现，没有将这些项目的源码或二进制依赖打包发布：

- [MusicFloat](https://github.com/Adudumax/MusicFloat)：SMTC、全局快捷键和桌面悬浮窗思路。
- [MyLyrics](https://github.com/ZiweiZhou99/MyLyrics)：本地播放列表与歌曲 ID / 歌词匹配思路。
- [cloudmusic-desktop-mcp](https://github.com/Seraph310/cloudmusic-desktop-mcp)：本机调试通道与客户端播放接入思路。
- [Microsoft GSMTC 文档](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager)：Windows 媒体会话。
- [LRCLIB](https://lrclib.net/)：时间轴歌词的可选回退来源。

应用使用 Windows 提供的 .NET Framework / WPF、Windows PowerShell、SMTC、Core Audio、winsqlite3 和 Win32 接口；不再分发上述系统组件。网易云客户端需用户自行安装。

云朵声波应用图案使用 imagegen 生成，提示词记录在 assets/设计说明.md，图标转换脚本由本项目提供。按钮图形以 WPF 几何路径实现，未打包 Apple 字体或图标素材。

运行时请求的歌曲、歌词、翻译、专辑封面及相关商标归各自权利人所有，不属于本项目的 MIT 授权内容。发行包不包含账号数据、歌单数据库、歌词缓存、音乐文件、用户桌面壁纸或截图。
