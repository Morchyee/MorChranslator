# MorChranslator 品牌与兼容性

产品名称严格为 **MorChranslator**。主程序 `MorChranslator.exe`，字幕进程 `MorChranslator.Subtitles.exe`，解决方案 `MorChranslator.sln`，测试项目 `tests/MorChranslator.Tests`。所有 C# 命名空间和程序集使用该前缀；核心识别/翻译协调类只改名为 `TranslationPipeline`，算法不变。

## 保留标识及原因

| 标识 | 保留原因 |
|---|---|
| `%APPDATA%\Kikitan Translator` | 原地读取配置、DPAPI 密钥、字幕外观和日志；不复制或改写用户数据目录，不分裂两份设置 |
| `Local\DesktopTranslator.Main-` / `.Subtitles-` | 新旧版本共用单实例保护，避免并行捕获、配置写入及字幕冲突 |
| `kikitan-desktop-control-` / `kikitan-desktop-subtitles-` | 保留按父进程 ID 命名的本用户 IPC；JSON、事件、GUID 完全不变 |
| `KIKITAN_LATENCY_DIAGNOSTICS` | 兼容旧启动方法；新方法使用 `MorChranslator_LATENCY_DIAGNOSTICS`，任一精确为 `1` 即开启，默认关闭 |
| `DESKTOP_TRANSLATOR_TEST_ARTIFACTS` | 保留既有原生自测截图自动化接口，不属于界面品牌 |
| `Local\DesktopTranslator.DiagnosticsLauncher-` | 新旧诊断启动器互斥，防止同时移动共用日志 |
| 上游项目名、上游 URL、LICENSE.md | 原作者署名、许可、来源说明，不冒充原创 |
| PROGRESS.md 既有章节、modernization-baseline-files.txt、CLEANUP_AUDIT.md、历史版本路径 | 历史证据和原有用户审计，保持事实可追溯，不篡改历史 |
| 本地根目录 `F:\Program\kikitan-translator` | 保持工作区、外部编辑器和自动化有效；所有相对开发/构建路径已更新 |
| Git 历史、旧发布包、隔离备份和生成缓存 | 不改写历史、不破坏回滚；不进入新包 |
| 提交历史仍保留的旧 UI 教程截图 | 工作区原有删除状态保持不变；Vite 仅打包显式引用的资源，旧截图不进入新发行包 |

兼容常量集中在 `MorChranslator.Resources/ProductIdentity.cs`。管道及启动器保留同样的字面量；回归测试刻意验证原始值。配置字段、既有 DPAPI 凭据封装及加密参数未变；原字幕历史为内存会话状态，不新增磁盘迁移。

## 开发与验证

```powershell
npm ci --prefix MorChranslator.Photino/UserInterface
npm test --prefix MorChranslator.Photino/UserInterface
dotnet build MorChranslator.sln -m:1
dotnet run --project tests/MorChranslator.Tests --no-build --no-restore
./scripts/build-release.ps1 -Version 2.0.2-alpha.20261012.rename.1
```

打包目录及 ZIP 使用 `MorChranslator-<version>-win-x64`。不要只复制 EXE。先退出旧版本；窗口关闭只是最小化/隐藏，须从托盘退出。设置窗口仍需 WebView2 Runtime；发布包自带 .NET。诊断开关只改变可观测性，IPC 写完成不是屏幕绘制完成。

重命名不代表云服务或真实音频已验证。完整自动测试、原生自测、打包与启动结果及仍需手测项目见 PROGRESS.md 和本次交付验证记录。禁止推送或创建外部 Release，除非用户另行授权。
