# 延迟诊断：第一里程碑（2026-10-12）

本里程碑只增加可观测性，不改变识别算法、预览策略、翻译优先级、超时、字幕 GUID、IPC JSON 或字幕历史规则。不添加依赖，不更新现有 portable release。

## 开启与关闭

诊断默认关闭。进程首次初始化时读取环境变量，只有精确值 `1` 才开启；不修改配置文件，不提高全局日志级别。

在 PowerShell 中为**包含本次源码的开发构建**开启：

```powershell
$env:KIKITAN_LATENCY_DIAGNOSTICS = '1'
dotnet run --project .\KikitanTranslator.Photino --no-build -- --start-stopped --settings
```

本次验证的是 Debug 开发构建。Debug 设置窗口连接 `http://localhost:1420`，需要在另一终端运行现有前端开发服务器（已安装依赖时：进入 `KikitanTranslator.Photino/UserInterface`，执行 `npm run dev`）。启动前退出其他 Desktop Translator 实例；已有单实例保护会让第二个进程直接退出。现有 portable release **不包含**此功能，给旧 EXE 设置变量不会增加埋点。无需为诊断重新输入或导出凭据。

关闭应用后执行以下命令，再重新启动；运行中的进程不会动态切换：

```powershell
Remove-Item Env:KIKITAN_LATENCY_DIAGNOSTICS -ErrorAction SilentlyContinue
```

## 输出位置和隐私边界

复用 `Logger.Initialize` 的日志：`%APPDATA%\Kikitan Translator\logs\*.log`，以及现有控制台 sink。既有滚动策略是每文件 5,000,000 字节、最多 7 个文件。每条诊断以 `[LATENCY] ` 开头，后面是 JSON。`type=session` 表示已启用，`schema=1`。

诊断 API 只接受固定事件枚举、GUID、请求序号、时间戳、计数和 HTTP 状态码。不会接收或输出音频、原文、译文、URL、授权头、异常消息、API key 或配置。正文由现有业务管道处理，诊断队列只保留数值元数据。**不要分享整个现有日志**：其他既有日志不属于此隐私保证。只提取 `[LATENCY]` 后的 JSON，人工检查后分享：

```powershell
$diagnosticLog = Get-ChildItem (Join-Path $env:APPDATA 'Kikitan Translator\logs') -Filter *.log |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
Get-Content -LiteralPath $diagnosticLog.FullName | ForEach-Object {
    if ($_ -match '\[LATENCY\] (\{.*\})$') { $Matches[1] }
}
```

长测试可能跨多个滚动文件，应按时间选取同一进程的文件。上述命令只输出筛选后的记录，不复制配置，也不打印私人路径字段。

## 关联与时间定义

- `unit`：既有字幕 GUID，未改变生成/更新语义。同一句 partial、final、翻译更新仍共享原 ID。没有识别关联的状态/命令 IPC 使用空 GUID，其 `recognitionMs` 为 null。
- `operation`：每个预览或 final 工作项的进程内递增序号。原文事件为 0；同一 unit 的预览和 final 使用不同序号；重试 HTTP 复用所属 operation。
- `recognitionEvent`：每个已接受识别回调的递增序号，区分同一 unit 下 operation=0 的多个 partial 原文事件。翻译工作项保留触发它的回调序号。`emissionAtMs` 在有真实 OnSubtitle 起点时给出相对时间，否则为 null。
- `preview`：是否预览工作项。区分预览和 final 时结合事件类型，不依赖日志行顺序。
- `atMs`：从诊断初始化起经过的单调时间。Serilog 行首墙钟仅用于定位文件；不可用墙钟相减计算延迟。
- `elapsedMs`：该事件定义的阶段耗时；无可靠阶段起点时为 null。
- `recognitionMs`：从触发当前工作项/原文事件的识别回调进入 `Kikitan.OnRecognition` 起，到事件记录时的时间。
- `firstRecognitionMs`：从同一 unit 的第一个已接受识别回调起，到当前事件记录时的时间。final-only Groq 的起点就是 final 回调；不能覆盖识别前的音频分段或上传等待。
- `value`：事件专用整数，HTTP 结果为状态码、HttpStarted 为从 1 开始的尝试序号、RecognitionPartialCount 为该 unit 的 partial 回调总数；其他默认 0。

识别事件在现有识别锁内记录，回调进入时间在锁前获取，日志可反映回调处理等待；partial gap 的终点是该 gap 事件记录时。异步消费者可能先记录后续事件，再记录生产者的 `IpcQueued`；以 GUID/operation 和实际计时字段关联，不把文件行序当作跨线程全序。

## 可观测阶段

| 事件 | 边界与解读 |
|---|---|
| RecognitionFirstPartial / RecognitionPartial | 第一次及后续 partial 回调；包括内容相同的重复 hypothesis，不等同于服务内部推理步骤 |
| RecognitionRevision | 后续非 Gemini partial 与已保存的原文不同；只记录发生次数，不记录差异内容 |
| RecognitionPartialGap | 从上次 partial 回调进入起至本次 gap 记录的时间 |
| RecognitionFinal / RecognitionPartialCount | final 回调；前者 elapsedMs 从本 unit 首次回调开始，后者 value 是 partial 数 |
| PreviewCandidate / PreviewScheduled | 稳定前缀策略产生候选，以及填入一个可替换预览槽；两者可能几乎同时 |
| PreviewCoalesced / PreviewDiscardedForFinal | 未执行的旧槽被更新候选替换，或被 final 清除；不计为 HTTP 错误 |
| PreviewCancelForFinal / PreviewCancelForRevision | 请求取消 active preview 的原因；不意味着提供方已经响应取消，重复取消请求可能多于实际取消数 |
| FinalQueued | final 入队尝试；容量淘汰或入队拒绝另记 TranslationQueueDropped |
| TranslationQueueWait | 工作项入队/填槽至 worker 取出的等待，不包含 HTTP 执行 |
| PreviewStarted / FinalStarted | worker 开始该工作项；cache hit / transcription-only 也经过这里，不能当作实际 HTTP 请求数 |
| PreviewCompleted / FinalCompleted | 翻译调用返回有效结果或 transcription-only 工作完成；elapsedMs 是本次工作执行时间，包含 adapter、重试和等待，排除排队 |
| PreviewFailed / FinalFailed | 空翻译、异常失败；不记录异常详情；原文保留逻辑不变 |
| TranslationCacheHit / TranslationBypassed | final 复用已完成预览，或仅转录模式；均不新增 HTTP 请求 |
| PreviewCancelled / PreviewTimeout | preview 因明确取消请求结束，或两秒 preview budget 结束；前者与取消原因事件配对 |
| TranslationCancelled / TranslationTimeout | 停止时的执行取消，或非停止的 OperationCanceledException（通常来自内部/transport timeout）；后者不是云端确认的超时原因 |
| PreviewStale | 请求开始前或返回后不再适用；已返回结果被丢弃，不会成为字幕翻译 |
| TranslationExpired | 既有桌面十秒排队年龄策略跳过工作项；独立于容量丢弃 |
| TranslationQueueDropped | final 队列的 DropOldest 淘汰或入队拒绝；不是诊断记录丢失 |
| HttpStarted / HttpCompleted / HttpFailed | 每次 `HttpClient.SendAsync` 尝试，默认 completion 包含响应体缓冲；elapsedMs 排除重试退避及 adapter JSON 解析；非 2xx 单独记失败 |
| HttpCancelled / HttpTimeout | 调用方 token 取消，或未由调用方 token 导致的 transport/budget 取消；不读取异常正文 |
| HttpRetry / HttpCooldown | 安排既有退避重试，或设置/命中既有 Retry-After 冷却；冷却命中不会产生 HttpStarted |
| OriginalEmitted / TranslationEmitted | 同步调用既有 OnSubtitle 事件前；成功/失败 final 翻译更新均可能发出 TranslationEmitted，不代表已经写入 IPC |
| CombinedTranslationEmitted | Gemini 自带组合结果进入管道；其识别与翻译计算无法在这里拆分，且不经过独立翻译 HTTP 埋点 |
| IpcQueued / IpcQueueDropped | IPC 入队成功或容量拒绝；包括已开启诊断时的无识别关联控制消息 |
| IpcQueueWait | 从调用 Write 的入队时间到 IPC worker 开始处理 |
| IpcWriteCompleted | 命名管道连接、写行及 StreamWriter/pipe DisposeAsync 刷新完成；**不是 UI 渲染确认** |
| IpcWriteFailed / IpcWriteTimeout / IpcCancelled | IO 错误、连接超时、应用停止取消；没有成功写入保证 |
| IpcEmissionToWrite | OnSubtitle 发出至 IPC 成功写入/刷新，包含写入排队、连接和写入 |
| RecognitionToTranslation / FirstRecognitionToTranslation | 有效翻译更新发出时，分别从触发工作项的回调、本 unit 首次回调计时；cache hit 也记录；失败空更新不记录 |
| OriginalRecognitionToIpcWrite / TranslatedRecognitionToIpcWrite | 原文或翻译更新实际成功写入时，从其对应触发回调计时 |
| FirstRecognitionToIpcWrite | 本 unit 首次回调到各次成功 IPC 写入；混合原文及翻译更新，分析时结合 event 的 operation/preview |

所有 `recognition...` 指标都是**识别事件之后的局部管道延迟**。Bing 在 hypothesis/phrase 解析后同步通知本管道；Groq 在整段 WAV 上传并解析后只通知 final。这些数据不能回答语音开始至识别回调之间耗时多少。ManualTranslate 复用 final 回调路径，也会产生识别入口指标；做音频实验时不要混入手动翻译。

## 汇总、丢失与开销

- 单个进程使用有界 2048 条诊断 Channel。生产者只读单调时钟、递增序号并 TryWrite，不等待日志 IO。没有音频回调新增埋点；AsyncLocal 只在开启时携带元数据。关闭时不创建 collector worker/channel、不读取诊断时钟、不格式化输出。
- 一个后台消费者进行 JSON 格式化和既有 Serilog 写入。每 30 秒和正常退出时输出 `type=summary`，按 stage 汇总。`count`、`valueTotal` 与 `maxMs` 是本进程已消费记录的累计值；`sampleCount`、p50Ms、p95Ms 是该 stage **最近最多 512 个有 elapsedMs 样本**的精确 nearest-rank 分位数，不是全程分位数。无阶段耗时的事件分位数为 null。RecognitionPartialCount 应看 valueTotal/count，不把 partial 数当毫秒。
- `type=health` 的 `recordingDrops` 是诊断 Channel 满导致的元数据损失；`sinkFailures` 是输出失败。均与业务队列丢弃/请求失败分开。发生这些情况时汇总不完整，不能声称没有业务故障。
- 各事件量纲不同、不同 operation 可交错；不要相加各 stage 的 count 当作请求总数。PreviewCompleted 表示调用已返回，之后仍可能 PreviewStale；CancellationRequested 不等于取消完成。
- 退出最多等候诊断 worker 一秒，异常终止/强制退出/日志 IO 持续阻塞可能丢失尾部记录。既有管道停止时未执行的排队项不保证各有 terminal 事件，不能用 queued 减 completed 推断永久泄漏。
- 启用后每事件日志仍有 CPU/磁盘开销，且会更快触发既有日志滚动。这里只验证了有界、非阻塞和禁用路径；没有宣称实际音频延迟、CPU 或内存改善。建议短时复现后关闭。

## 安全的真实英语→中文采样

1. 使用本次 Debug 开发构建和前端开发服务器，设置环境变量并启动应用。确认出现 `[LATENCY]` session JSON。保持原有凭据，只在本机设置中选择系统音频、Bing 或 Groq、English→Chinese 和现有翻译提供方。
2. 播放无隐私的固定英语测试素材：短句后长停顿、约 100–300ms 短停顿、连续讲话各一段。每组运行至少 30 秒，主观记录“原文迟/译文迟/字幕窗迟”和测试组名称，不抄录内容、不上传音频。
3. 先看 recordingDrops/sinkFailures，再看 partial cadence、Final/Preview queue wait、HttpCompleted/Failed/Timeout、RecognitionToTranslation 和 TranslatedRecognitionToIpcWrite。HTTP 耗时高与排队高是不同原因；Groq 首次 final 以前的等待仍不可见。
4. 停止并正常退出，筛选本次进程的 `[LATENCY]` JSON；只分享该元数据及测试组、识别器/翻译提供方名称、成功/失败现象。不要附完整日志、配置、截图中正文或 key。
5. 移除环境变量并重启应用恢复默认关闭。此里程碑没有生成新发布包，旧 portable release 保持原样。

## 验证及后续边界

自定义可执行测试 runner 使用假时钟、假识别器/翻译器、假 HTTP 和受控 IPC transport。另有本地真实命名管道测试，验证协议、Unicode 和 flush 完成；没有调用真实云服务、采集用户音频或证明 UI 已绘制。

```powershell
dotnet build kikitan-translator.sln --no-restore -m:1 -p:SkipNpmBuild=true -p:UseSharedCompilation=false -v:q
dotnet run --project tests/DesktopTranslator.Tests --no-build --no-restore
```

本次不修改 frontend，故不重跑 frontend tests，不发布/打包。最终实际结果与警告见根目录 PROGRESS.md。

下一步先收集以上真实诊断样本，再决定是否需要 receiver 端 ACK/绘制时间，以及音频段/识别单元的明确关联。当前缺少音频 onset→识别单元映射、Bing 原生流时间对齐、Groq 分段/上传队列计时和 UI render ACK；不可用估计补齐。十秒 expiration 分支及真实两秒/二十秒预算触发尚未以虚拟调度器覆盖；不要为测量而先改调度/识别行为。本里程碑到此结束。
