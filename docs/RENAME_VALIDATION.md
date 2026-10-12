# MorChranslator 重命名验证记录（2026-10-12）

## 版本与来源

- 分支：`codex/MorChranslator-rename`。起点：`7276dd5`。
- 品牌代码：`a656505`；完整源码旧 UI 路径/资源修正：`9c2d97f`；严格发行扫描兼容文档修正：`e84dd7f`。
- 新包应用源码：`e84dd7fb2d486d5944b7e81639fc960bff82d365`。230 个源码文件通过 git archive 独立导出，构建后 SHA-256 全部匹配。未混入工作区 99 项原有改动。
- 版本：`2.0.2-alpha.20261012.rename.1`。EXE 的 ProductName 为 `MorChranslator`，InformationalVersion 包含上述完整提交；主进程和字幕进程均为 Windows x64 自包含应用。

## 实际执行

工具：.NET SDK 9.0.318、Node 24.15.0、npm 11.12.1；锁定 npm ci 527 项，无版本升级。

| 范围 | 命令/检查 | 结果 |
|---|---|---|
| 保留用户改动的工作区 | `dotnet build MorChranslator.sln -m:1 -p:SkipNpmBuild=true -p:UseSharedCompilation=false -v:q` | 成功，36 警告/0 错误，包含 NuGet 漏洞源不可达 |
| 工作区 | `dotnet run --project tests/MorChranslator.Tests --no-build --no-restore` | 56/56，含全部 16 项延迟诊断及 3 项新兼容/品牌测试 |
| 工作区最终前端 | `npm test`、`npm run build --prefix MorChranslator.Photino/UserInterface` | 27/27；TypeScript/Vite 生产构建成功 |
| 干净品牌候选 | 完整七项目构建、上述自定义运行器和前端测试 | 72 警告/0 错误，56/56，27/27 |
| 最终提交隔离快照 | `pwsh -NoProfile -File ./scripts/build-release.ps1 -Version 2.0.2-alpha.20261012.rename.1 -SkipInstall` | 前端 27/27、生产构建、全解决方案 68 警告/0 错误、C# 56/56、自包含 publish、原生自测、资源哈希及凭据模式检查成功 |
| 最终包 | 字幕 `--self-test en` / `zh-CN` / `ja-JP` | 全部通过，包括渲染、更新、历史、滚动 |
| 最终包 | `MorChranslator.exe --start-stopped --settings` | 实际设置标题、字幕标题、WebView2、HTTP index/JS/CSS/SVG、原生依赖加载成功 |
| 最终包 | 普通重复启动、旧版主/字幕 EXE 启动、诊断启动器拒绝已运行实例 | 均通过，不出现两个实例 |
| 最终包 | Windows PowerShell 5.1 启动器，普通模式与诊断模式，控制管道正常退出 | 默认关闭/子进程启用通过；两个模式退出码 0，无已记录的残留进程；退出汇总/health 有效且 recordingDrops/sinkFailures 为 0 |
| 数据安全 | 启动测试前后应用文件 SHA-256、环境变量 | 所有原有配置/日志逐字节恢复，父进程环境未改变 |
| 业务边界 | 19 个捕获、识别、翻译、协调、字幕模型/DTO 文件 | 除标识符替换外与原工作区完全一致；协议/事件/GUID 未改 |
| 工作安全 | 原始用户差异重放、清理审计、旧包 SHA-256 | 99 项原差异沿新路径完整保留；审计字节一致；255/255 旧发布文件未变 |

原生依赖实际提取检查覆盖 ONNX Runtime、Photino、WebView2 loader、miniaudio、WebRTC、libuv、ASP.NET hosting。未做真实云服务调用或音频捕获。

## 发现并处理的问题

- 沙箱 Vite realpath EPERM：授权范围内重试构建成功；不修改安全设置。
- 离线 npm 缓存缺 zwitch：按原锁文件正常安装成功；无依赖升级。
- 第一轮隔离构建发现保留的旧 UI 导入路径漏改：修正并提交，用户的对应删除不恢复。
- 第一次发布内容检查把文档中的 DPAPI 标记示例识别为凭据：去掉文字示例；扫描器及加密代码不变。失败 staging 留存于 ignored 证据目录。
- 首次启动检查发现用户旧版正在运行：未终止它，用户确认从托盘退出后再验证成功。
- Browserslist 数据较旧、既有 nullable/async/平台警告仍存在。NU1900 意味着本次漏洞源检查不可用，不是依赖安全认证。

## 未验证与边界

真实 WASAPI/麦克风硬件捕获、Bing/Groq/翻译云服务的实时成功率和英中质量/延迟、网络故障、设备切换、DPI、休眠、多小时运行、签名与干净机器部署需要人工验收。现有假识别器/HTTP 和真实本机管道回归只证明确定性逻辑，不能替代云端或硬件测试。IPC 写入不代表接收端已绘制。

构建、源文件清单、失败尝试、启动与完整性证据位于 `release/audit-work/MorChranslator-rename`，不在发行包内。原根目录不移动。没有推送、GitHub Release、历史重写或旧包覆盖。文件改名清单见 RENAME_FILES.md；剩余命中见 RENAME_AUDIT.md。
