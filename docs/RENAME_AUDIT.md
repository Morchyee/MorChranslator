# MorChranslator 重命名剩余命中审计

范围：当前源码、项目、测试、脚本、CI 和文档；排除 Git 对象、旧发行包、生成缓存、node_modules、ignored 隔离源码及备份。原有发行文件 255/255 SHA-256 未变。审计报告自身含旧名称作为证据，不作为当前产品品牌。

| 文件 | 行号 | 每组保留原因 |
|---|---|---|
| `MorChranslator.Photino/DesktopControlServer.cs` | 21 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `MorChranslator.Photino/SubtitleWriter.cs` | 74 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `MorChranslator.Photino/UserInterface/src/i18n/locales/en.json` | 95 | 上游项目来源与必要署名 |
| `MorChranslator.Photino/UserInterface/src/i18n/locales/ja-JP.json` | 95 | 上游项目来源与必要署名 |
| `MorChranslator.Photino/UserInterface/src/i18n/locales/zh-CN.json` | 95 | 上游项目来源与必要署名 |
| `MorChranslator.Subtitles/SubtitleAppearanceDialog.cs` | 179 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `MorChranslator.Subtitles/SubtitleWindow.cs` | 181, 198, 401 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `PROGRESS.md` | 18, 19, 38, 44, 45, 54, 56, 59, 63, 64, 73, 74, 76, 77, 95, 99, 112, 118, 119, 125, 126, 136, 137 | 历史记录、已有用户审计或历史包名，保留事实 |
| `README.md` | 3, 94 | 上游项目来源与必要署名 |
| `README.md` | 32 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `docs/DIAGNOSTIC_TESTING_ZH.md` | 40 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `docs/LATENCY_DIAGNOSTICS.md` | 26, 122 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `docs/PERFORMANCE.md` | 36 | 历史记录、已有用户审计或历史包名，保留事实 |
| `docs/UI_DESIGN.md` | 134 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `docs/modernization-baseline-files.txt` | 2, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 94 | 历史记录、已有用户审计或历史包名，保留事实 |
| `scripts/Start-LatencyDiagnostics.ps1` | 13, 18, 30 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `tests/MorChranslator.Tests/DiagnosticsTests.cs` | 291 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `docs/CLEANUP_AUDIT.md` | 15, 16, 34, 38, 64, 65, 66, 67, 68, 69, 71, 72, 75, 87, 98, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168 | 历史记录、已有用户审计或历史包名，保留事实 |
| `MorChranslator.Resources/ProductIdentity.cs` | 8, 9, 10, 11, 12, 14 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `tests/MorChranslator.Tests/BrandingTests.cs` | 11, 12, 13, 14, 15 | 旧数据路径、锁、IPC、自动化/诊断别名及其回归断言 |
| `tests/MorChranslator.Tests/BrandingTests.cs` | 48 | 上游项目来源与必要署名 |
| `MorChranslator.Photino/UserInterface/tests/branding.test.mjs` | 13, 18 | 断言旧品牌不得作为当前产品名称出现 |
| `MorChranslator.Photino/UserInterface/tests/branding.test.mjs` | 17 | 上游项目来源与必要署名 |
| `docs/RENAMING.md` | 9, 10, 11, 12, 13, 14, 17 | 兼容标识及保留原因说明 |

当前产品标题、EXE、项目/程序集/命名空间和新包名不保留旧品牌。文件路径改名清单以本次 Git 提交的 `--name-status -M` 为准；99 项原有改动/删除沿新路径保留，未纳入品牌提交。
