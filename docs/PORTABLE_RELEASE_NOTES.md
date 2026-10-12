# MorChranslator — Windows x64 portable alpha

## 简体中文

Windows 桌面实时语音识别与双语字幕翻译器，支持系统音频、Bing/Groq 识别、已集成的官方翻译适配器、句子级预览和稳定字幕条目。产品及便携 EXE 名称统一为 **MorChranslator**。本次交付调整便携 ZIP 结构；没有修改识别、翻译调度、字幕 GUID、IPC 或延迟诊断事件含义。

下载 ZIP 和对应 SHA256 校验文件，校验后完整解压。ZIP 内只有一个 `MorChranslator/` 顶层文件夹。先从旧版托盘退出，然后双击文件夹内的 `MorChranslator.exe`。无需安装 .NET SDK、Node 或 Vite；需要 Windows 10/11 x64、Microsoft Edge WebView2 Runtime，云服务还需网络及有效账户/凭据。不要仅复制 EXE。

旧配置、加密凭据和字幕外观继续使用原有用户目录；新旧版本共用单实例锁。诊断默认关闭，可运行随包的 `Start-LatencyDiagnostics.ps1`，参见 `DIAGNOSTIC_TESTING_ZH.md`。分享时仅导出 LATENCY 记录，IPC 写入成功不表示已经绘制到屏幕。

这是未签名的 Pre-release。真实英中音频质量/延迟、云端可用性/限流、设备恢复、混合 DPI、长时间运行和干净机器安装仍需手动验证。Bing 服务可用性不保证。部分旧实现/依赖保留在已提交源码中，主工作区的未提交清理不属于此发行包。上游许可证与署名保留。

## English

MorChranslator is a Windows real-time speech recognition and bilingual subtitle translator with system audio, Bing/Groq recognition, integrated official translation adapters, sentence previews and stable subtitle entries. Product/executable branding is unified. This delivery fixes the portable ZIP layout without changing recognition, translation scheduling, subtitle IDs, IPC or diagnostic event semantics.

Download the ZIP and matching SHA256 file, verify the checksum and extract everything. The archive contains one `MorChranslator/` folder. Exit older instances from the tray and run `MorChranslator.exe` inside it. Windows 10/11 x64 and WebView2 Runtime are required; .NET is bundled. Node, Vite and the .NET SDK are unnecessary for use. Cloud services require connectivity and valid credentials.

Existing configuration/credentials remain compatible and old/new versions share instance locks. Diagnostics are off by default; use the optional launcher and Chinese testing guide. IPC write completion is not confirmed on-screen rendering.

Unsigned pre-release: live audio quality/latency, provider availability/rate limits, hardware recovery, mixed DPI, long sessions and clean-machine setup require manual acceptance. Bing availability is not guaranteed. Uncommitted working-tree cleanup is excluded. Upstream attribution/license are preserved.
