# Kikitan Translator 英语→简体中文延迟测试

此诊断测试包的应用源码固定为 **5fc56a08c95dd21c490fa729ec8fb6a9ee373a92**。不是正式发布认证；没有修改识别、翻译、字幕算法或诊断事件定义。无需 .NET SDK、Node 或 Vite，Windows x64 上需有 Microsoft Edge WebView2 Runtime。

## 启动

1. 完整解压 ZIP，不要只复制 EXE。先从其他 Desktop Translator 的**托盘菜单选择退出**；仅关闭/最小化设置窗口并不会结束进程，单实例保护会阻止新实例。
2. 在解压目录打开 PowerShell，运行：

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Start-LatencyDiagnostics.ps1
   ```

   `Bypass` 只作用于此 PowerShell 进程，不修改执行策略；脚本未签名，请先检查脚本。脚本仅在新应用进程环境中设置 `KIKITAN_LATENCY_DIAGNOSTICS=1`，不修改当前 shell、系统或用户环境变量。不自动开始捕获，启动后显示设置。
3. 保留启动器窗口，测试结束后等待它完成日志归档及旧日志归位。直接双击 `DesktopTranslator.exe` 为正常模式，默认不启用诊断（除非你自行在其他环境中持久设置过该变量）。

启动器不读取、复制、重置或导出配置/凭据，应用沿用正常的本机设置，手动更改设置仍会按应用既有逻辑保存。凭据只在本机配置。不要同时启动另一个普通/旧版应用，也不要在测试期间运行共用日志目录的旧命令行程序。

## 配置

- 选择系统音频、语音识别 **Bing**，源语言 **English**，目标语言 **中文 / Chinese（zh）**；这是本项目简体中文翻译目标，不是界面语言切换。
- 选择已有的 DeepL、Google Cloud 或 Groq 翻译提供方，并在本机使用有效凭据。关闭“仅转录”。Bing 识别器没有额外 key 输入，翻译提供方仍可能需要付费账户/key。
- 将 Windows 正在播放的英语音频设为默认输出设备，保持字幕窗口可见，点击开始。一次只测试一个提供方，记录其名称即可，不记录 key。
- 如果现有服务拒绝请求、网络断开或无翻译，记录失败现象和诊断事件；不要绕过限制或反复高频重试。识别正确率和翻译质量需人工判断。

## 三组测试

使用公开、非隐私的固定英语素材；不保存或上传用户音频、原文、译文或包含正文的截图。各组可停止后重新开始，以明确实验边界。

| 组别 | 素材安排 | 建议时长 | 观察 |
|---|---|---|---|
| 短句 | 每句话约 2–5 秒，句间停顿约 1 秒 | 60–90 秒 | 原文出现、句末翻译是否迟到、预览是否复用 |
| 长讲话 | 连续演讲、少停顿，包含自然标点和约 100–300ms 短停顿 | 2–3 分钟 | partial 间隔、预览生成、排队是否增长 |
| 快速对话 | 两人轮流说话、短轮次，避免人为叠加不可辨识声音 | 90–120 秒 | 取消/过期/丢弃、翻译是否追不上对话 |

只记录测试组名、提供方、时间范围以及“原文迟/译文迟/窗口迟”等主观现象。不把主观秒数当作仪器测得的完整音频延迟。汇总每 30 秒及正常退出时输出。

## 查找和安全导出

运行期间日志位于 `%APPDATA%\Kikitan Translator\logs`；正常退出启动器后，本次日志位于其中的 `diagnostic-sessions\<本次会话>`。启动器会显示该目录。旧根目录日志在测试期间暂存于 `diagnostic-preserved\<本次会话>`，退出后归位；不会删除或覆盖旧日志。

**完整日志可能包含其他业务信息，不要直接分享。** 将下面 `$sessionLogs` 填为启动器显示的本次目录，提取仅带 `[LATENCY]` 前缀的 JSON；自动选择全新导出文件，拒绝覆盖：

```powershell
$sessionLogs = '在此填入本次 diagnostic-sessions 会话目录'
$export = Join-Path $PWD ('latency-' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N') + '.jsonl')
$lines = foreach ($file in Get-ChildItem -LiteralPath $sessionLogs -Filter '*.log' -File | Sort-Object Name) {
    foreach ($line in Get-Content -LiteralPath $file.FullName -Encoding UTF8) {
        if ($line -match '\[LATENCY\] (\{.*\})$') { $Matches[1] }
    }
}
if (!$lines) { throw '本会话没有 LATENCY 记录，请检查启动方式和路径。' }
$stream = [IO.File]::Open($export, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
$writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false))
try { foreach ($line in $lines) { $writer.WriteLine($line) } } finally { $writer.Dispose() }
Write-Host $export
```

分享前人工查看 JSON：正常只包含固定事件名、随机字幕 ID、事件/请求序号、时间、计数和 HTTP 状态码；不应出现正文、URL、授权头或 key。只分享该 JSONL 及测试组/提供方/失败现象；不要附 `config.json`、完整日志或 WebView profile。

先检查 `type=health` 的 `recordingDrops`、`sinkFailures`。丢失非零时指标不完整。`summary` 的 count/max 为累计值；p50/p95 为该阶段最近最多 512 个有效时间样本。重点比较 partial cadence、TranslationQueueWait、HttpCompleted/Failed/Timeout、RecognitionToTranslation、TranslatedRecognitionToIpcWrite。完整定义见 `docs/LATENCY_DIAGNOSTICS.md`，其中旧 Debug/Vite 启动说明不适用于此便携包。

**IPC 写入刷新完成不是屏幕渲染确认。** 所有 recognition→translation/IPC 指标从识别回调开始，不包含音频开始前后至识别结果的等待；本包不能测量完整 audio-onset→屏幕字幕延迟。

## 退出及恢复正常模式

先点击停止，再从托盘选择退出，等待 PowerShell 显示会话目录并结束。之后直接运行平时使用的 EXE 即可；启动器没有设置持久环境变量，无需修改系统配置。旧发布目录保持不变。

如果强制关闭启动器/断电，日志仍保留在上述子目录，不会自动丢弃。先完全退出应用，再按会话目录检查 `diagnostic-preserved`：把其中的旧 `.log` 文件移回 logs 根目录，**遇到同名文件不要覆盖**，保留两份并人工处理；当前测试的根目录日志可移至一个全新的 diagnostic-sessions 会话目录。配置不受启动器影响。不要删除这些目录来“恢复”。

尚需人工验证：真实 Bing hypothesis 频率、云端账户/限流、实际音频识别与翻译效果、设备变化、DPI、多小时运行以及启用诊断的实际开销。此包未签名，Windows 可能显示来源提示。
