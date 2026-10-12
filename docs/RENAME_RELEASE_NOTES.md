# MorChranslator 2.0.2-alpha.20261012.rename.1

应用源码 / Application source: `e84dd7fb2d486d5944b7e81639fc960bff82d365`.

## 中文

- Windows x64 实时语音识别与双语字幕翻译器。统一项目、程序集、命名空间、EXE、设置、字幕窗口、托盘及文档为 **MorChranslator**，替换带旧品牌文字的图标。
- 保留已有系统音频/麦克风路径、Bing/Groq 识别、官方翻译适配器、句子预览、稳定字幕 GUID 和默认关闭的延迟诊断；本次不优化或修改这些算法。
- 旧配置、DPAPI 凭据与字幕外观原地兼容；新旧版本共用单实例锁及 IPC。旧教程截图保留于历史源码，但不打入新包。原许可证与上游署名保留。
- 安装：完整解压 ZIP，先从旧版托盘退出，再运行 `MorChranslator.exe`。无需 .NET SDK、Node 或 Vite；需 Windows 10/11 x64、WebView2 Runtime，使用云服务还需网络及相应账户/凭据。
- 诊断：执行随包 `Start-LatencyDiagnostics.ps1`；仅为本次应用子进程启用诊断，不改系统环境。参见 `DIAGNOSTIC_TESTING_ZH.md`，分享时只导出 LATENCY JSON。
- 已验证：隔离提交快照前端 27/27、C# 回归 56/56、完整解决方案构建、win-x64 自包含发布、en/zh-CN/ja-JP 原生自测、实际设置/WebView2/字幕启动、新旧单实例兼容、默认关闭与显式开启诊断、正常退出及用户文件恢复。
- 限制：真实音频、云端服务可用性/限流、英中质量/延迟、设备恢复、混合 DPI、多小时稳定性及干净机器安装仍需手测。IPC 写入完成不是屏幕绘制确认。构建有既有编译警告及 NuGet 漏洞源不可达警告，前端浏览器数据库较旧；未签名，不是生产认证。
- 本包来自独立已提交源码，不混入原有 99 项未提交用户清理；因此保留部分旧实现/依赖。工作区已另行测试，原有改动沿改名路径完整保留。没有推送或创建 GitHub Release。

## English

MorChranslator is a Windows real-time speech recognition and bilingual subtitle translator. This milestone unifies the product, projects, assemblies, namespaces, executables, windows, tray, icons and documentation. It preserves recognition/translation algorithms, sentence-preview behavior, subtitle IDs, IPC semantics, existing settings and DPAPI credentials. Upstream license and attribution remain intact.

Extract the complete portable ZIP, exit older instances from the tray, then run `MorChranslator.exe`. Windows x64 and WebView2 Runtime are required; .NET is bundled. Cloud services require connectivity and appropriate accounts. The optional PowerShell diagnostics launcher affects only its application child; diagnostics remain off by default.

Verified on the isolated committed snapshot: 27 frontend tests, 56 custom C# regressions, complete solution build, self-contained publish, native subtitle self-tests in three locales, actual settings/WebView2/subtitle startup, old/new single-instance exclusion and graceful shutdown. Existing user files were restored byte-for-byte. Live audio/cloud quality and latency, device recovery, mixed DPI, clean-machine setup and soak behavior remain unverified. IPC completion is not rendering confirmation. Existing warnings remain, the vulnerability feed was unreachable, and this package is unsigned.

The package deliberately excludes the original 99 uncommitted user changes; the renamed working tree was tested separately. No GitHub push or external release was performed.
