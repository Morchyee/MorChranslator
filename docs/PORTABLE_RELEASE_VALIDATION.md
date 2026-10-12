# Windows x64 portable validation — 2.0.2-alpha.20261012.1

Application/tag source: `0cf59343b4ec1928a7186f22fe2f57652240028a`.

These checks were executed against a clean isolated committed worktree, not the main working directory with its 99 pre-existing tracked modifications/deletions. Build evidence and test logs remain in ignored `release/audit-work/MorChranslator-publish-20261012/` and are not release assets.

## Executed checks

- Toolchain: .NET SDK 9.0.318, Node 24.15.0, npm 11.12.1; locked `npm ci` (no upgrades). Official portable gh 2.83.2, official checksum verified, existing Git authentication scoped to the child process.
- `pwsh -NoProfile -File scripts/test-package-portable.ps1`: single `MorChranslator/` root, nested/Unicode/hidden/empty files, exact extraction hashes, overwrite refusal and invalid-input rejection passed.
- `scripts/build-release.ps1 -Version 2.0.2-alpha.20261012.1`: frontend **27/27**, TypeScript/Vite production build, full seven-project solution **62 warnings / 0 errors**, custom executable C# regressions **56/56**, self-contained win-x64 publish, packaged native subtitle self-test and frontend/resource hash checks passed. SourceRevisionId was explicitly pinned to the commit above for both EXEs.
- Equivalent individual commands: `npm test --prefix MorChranslator.Photino/UserInterface`; `npm run build --prefix MorChranslator.Photino/UserInterface`; `dotnet build MorChranslator.sln --nologo -m:1 -p:UseSharedCompilation=false`; `dotnet run --project tests/MorChranslator.Tests --no-build --no-restore`. The last command is the custom runner; `dotnet test` discovery is not the regression gate.
- Fresh ZIP extraction: native subtitle tests **en / zh-CN / ja-JP**, real settings/WebView2/native subtitle startup, packaged HTTP index/JS/CSS/SVG and native dependencies passed. Original/new host and subtitle EXEs share single-instance exclusion. Normal launch defaults to diagnostics off. Shipping PowerShell 5.1 diagnostic launcher enables only its child and rejects an existing instance.
- Normal and diagnostic sessions exited through the control pipe with code **0**, zero recorded owned-process survivors. Diagnostics exit summaries/health show no recording or sink loss in this startup-only smoke. All pre-existing app files/config/logs were restored byte-for-byte; parent environment unchanged. No audio or external provider request was started.
- Source/privacy: **235 source files** and **366 outgoing history blobs** scanned with known credential formats; no matches. Current source excludes personal IDE state; the original IDE file already exists in the shared public ancestor and history was not rewritten. All 235 source hashes verified after build; only npm's rewrite of auxiliary `yarn.lock` was restored from the pinned blob, with no index content change. Main working tree **99 tracked edits/deletions + cleanup audit** byte-identical, **283** prior release files unchanged.
- Package: **24 files**, **23/23 SHA256SUMS entries**, **246,935,157 bytes** unpacked. Both executables have ProductName MorChranslator, x64 PE format and the exact source revision in ProductVersion. No application configuration, logs, source, debug symbols, node_modules or known credential patterns in the package. Every ZIP entry and extracted file matches the package hash; exactly one top-level product folder.
- ZIP: `MorChranslator-2.0.2-alpha.20261012.1-win-x64.zip`, **106,380,254 bytes**. SHA256: `c8caddb4aa1437d34a6488df1efd3a904f1d38de4bf00251b7768b62e6fc12cc`.

## Limits

## GitHub delivery

- Repository: https://github.com/Morchyee/MorChranslator . Pre-release: https://github.com/Morchyee/MorChranslator/releases/tag/v2.0.2-alpha.20261012.1 . Release ID 410726048. ZIP and `.zip.sha256` are Release assets, never Git blobs.
- Source and annotated tag pushed atomically without force. Remote README commits remain ancestors; no unrelated experimental branch or uncommitted cleanup was merged. Older draft releases remain unchanged.
- GitHub's server SHA256 matched both local assets before the draft was made public. Anonymous HTTP verification then read public main/tag, received HTTP 200 for the bilingual Release page, downloaded both complete assets and confirmed exact local/server/downloaded size and SHA256. Draft lookup by public tag returned the expected 404 before publication; authenticated ID-based inspection verified the uploads. This was an API visibility issue, not a failed application test.

## Limits

Existing nullable/async/platform warnings remain. Browserslist's database is 22 months old; dependencies were not upgraded. No NU1900 vulnerability-feed warning occurred in this build; this is not a comprehensive dependency-security assessment. Pattern scans cannot prove absence of every arbitrary secret.

Live English→Chinese audio quality/latency, Bing/cloud availability and quotas, network/device recovery, mixed DPI, sleep/resume, clean-machine WebView2 setup and multi-hour stability remain manual. The package is unsigned. IPC completion is not confirmed on-screen rendering; no reliable audio-onset-to-screen measurement has been added.
