# 云词岛 · Cloud Lyric Island

**陈小铭开发的 Windows 网易云音乐歌词悬浮窗。**

作者：[B 站 · 陈小铭](https://space.bilibili.com/2121431899) · 许可证：[MIT](LICENSE) · [使用声明](DISCLAIMER.md)

适合在桌面或窗口化游戏中看歌词、控制音乐。官方网易云音乐在后台运行，云词岛负责显示歌词、搜索和播放控制。

## 下载与启动

到 [Releases](https://github.com/cxm133777/CloudLyricIsland/releases) 下载 `CloudLyricIsland-v1.0.0-win-x64.zip`，**完整解压**后双击 `云词岛.exe`。请保留同目录的 XAML、脚本和 assets 文件，不要只移动 EXE。

运行环境：Windows 10 / 11 x64，系统 .NET Framework 4.x 和 Windows PowerShell 5.1。无需 Node、Python 或 .NET SDK，不要求管理员权限。目前精确控制适配在网易云 Windows 3.1.41.205529 上验证，未来客户端版本可能需要更新适配。

先启动官方网易云并登录、播放音乐。云词岛启动时会自动连接精确控制；如果客户端尚未开启控制接口，会在后台自动重启网易云一次并继续连接，无需手动点击。也可在搜索窗口的“连接选项”查看状态或手动重试。控制通道只监听本机 `127.0.0.1:9223`，详见[使用声明](DISCLAIMER.md)。

## 功能

- 原文、翻译、前后句歌词，以及本地 LRC 导入和时间偏移。
- 可拖动的播放进度，自动跟随客户端的进度跳转。
- 带专辑封面的搜索结果，双击后台播放，不主动弹出客户端。
- 读取本地“我喜欢的音乐”，查看红心状态并播放收藏。
- 上一首、下一首、暂停、网易云独立音量、全局快捷键和鼠标穿透。
- 自定义字体、颜色、布局、透明度、圆角、封面取色和样式方案。
- 圆角深色搜索 / 自定义窗口、细滚动条；辅助窗口始终在歌词岛上方。

## 操作

悬浮窗顶部可拖动，放大镜打开搜索，滑杆图标打开设置，锁图标切换鼠标穿透。托盘可以显示、解锁、搜索和退出。搜索 / 设置窗口的红黄绿按钮分别用于关闭、最小化、放大还原，右下角可缩放，Esc 关闭面板。

默认快捷键均带 `Ctrl+Alt`：

| 按键 | 功能 |
| --- | --- |
| F5 / F7 | 上一首 / 下一首 |
| F6 | 播放 / 暂停 |
| F3 / F4 | 网易云音量 − / ＋ |
| F8 | 游戏模式 / 解锁鼠标穿透 |
| F9 | 喜欢歌曲，需要配置官方客户端全局快捷键联动 |
| F10 | 显示 / 隐藏 |
| F11 | 自定义 |

快捷键都可以修改。喜欢联动建议在网易云中设置一个未占用组合，再在云词岛填写同一个组合并启用。状态来自本地缓存，可能延迟。

## 限制与数据

独占全屏可能遮挡普通桌面悬浮窗，建议使用无边框窗口或窗口化。本软件不注入游戏。搜索、播放、会员和版权权限沿用官方客户端；本软件不提供音频下载。

未连接精确控制时可使用 SMTC 或手动校准后备方案；部分网易云版本的 SMTC 没有有效时间轴。客户端升级可能影响内部控制和本地缓存结构。

设置、样式和缓存保存在 `%LOCALAPPDATA%\CloudLyricIsland`，不随发行包发布。不读取或导出登录凭据，不含独立遥测。网络请求及本机调试通道说明见 [DISCLAIMER.md](DISCLAIMER.md)。

## 构建与打包

在 Windows PowerShell 中运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\package.ps1
```

脚本使用系统自带的 Framework C# 编译器，仅对当前子进程使用执行策略 Bypass。输出位于 `dist`，包含运行 ZIP 和 SHA-256 校验文件。`BuildIcons.ps1` 可以从图案原稿重建多尺寸 ICO。

源码入口为 `App.cs`，播放和搜索为 `Features.cs`，本机控制适配为 `Control.cs` / `ClientRuntime.js`，本地歌单为 `Library.cs`。参考项目说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

## 作者与许可

Copyright © 2026 **陈小铭**。项目以 MIT 许可证发布，修改和再分发时须保留 LICENSE 中的版权及许可声明。歌曲、歌词、封面及第三方商标不属于本项目许可范围。

[关注作者 B 站主页](https://space.bilibili.com/2121431899)。本项目为独立第三方工具，与网易云音乐、网易及 Apple 无隶属、授权或合作关系。
