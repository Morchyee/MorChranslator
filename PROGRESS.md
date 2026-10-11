# Modernization progress — 2026-10-11

## Recovery and ownership

- Branch: `codex/production-reliability`; starting commit: `f0cb8ef`.
- The session began with extensive uncommitted cleanup and product changes. Preserve them. `docs/modernization-baseline-files.txt` lists already modified/deleted tracked paths; `docs/CLEANUP_AUDIT.md` was already untracked. Do not stage these wholesale.
- No existing PROGRESS.md was present. Earlier validation documents are historical evidence, not current test results.
- Commit only explicitly listed session files after each milestone passes validation. No publishing or remote operations.

## Current status

### Completed release packaging — sentence latency build

- Built commit `6469576d48375d9f2b2d670c1fe64b7eeedbbcc3` as `2.0.2-alpha.20261011.2`, using an isolated `git archive` export at `release/.source-6469576-latency`. No uncommitted source, project, frontend, documentation or release-script changes were inputs. All 219 regular exported source files match that commit after Git's configured CRLF normalization. The original 99 changed tracked paths and cleanup audit remain preserved and uncommitted.
- Executed the committed `scripts/build-release.ps1 -Version 2.0.2-alpha.20261011.2` in the export with locked `npm ci` and explicit `SourceRevisionId`. Frontend **26/26 passed**, TypeScript/Vite production build passed, full .NET solution **62 existing warnings / 0 errors**, C# **37/37 passed**, self-contained win-x64 publish passed. This committed snapshot retains legacy projects/dependencies/assets absent from the uncommitted cleanup, explaining differences from the earlier working-tree package and its warning count. No cleanup was silently included.
- Bing provisional sentence regressions passed: early stable-prefix translation before final recognition, same-ID source/translation updates, punctuation/abbreviations/CJK, continuous speech, cancellation/revisions, final priority, preview coalescing, exact-final cache reuse and language-direction invalidation. Actual Groq WAV tests confirm submission at 80,000 samples without waiting for subsequent audio/silence.
- Published native subtitle self-tests passed in **en, zh-CN and ja-JP**, including rendering, update, history and scroll checks.
- **Launch verified:** packaged `DesktopTranslator.exe --start-stopped --settings` started native settings, a WebView2 child and the native subtitle window. Packaged index.html matched the HTTP response and JS/CSS returned HTTP 200. Normal control-pipe exit completed with code zero and no surviving subtitle child. No audio capture was started. The temporary test configuration contained no credentials; existing application files, settings and logs were restored byte-for-byte. The native libraries extracted by this new binary were inspected: ONNX Runtime, Photino, WebView2 loader, miniaudio, WebRTC processing, libuv and ASP.NET hosting support were present.
- **Release directory:** `F:\Program\kikitan-translator\release\DesktopTranslator-2.0.2-alpha.20261011.2-win-x64`. Double-click `DesktopTranslator.exe` in that complete directory. Product version: `2.0.2-alpha.20261011.2+6469576d48375d9f2b2d670c1fe64b7eeedbbcc3`; both executables verified as x64 PE. .NET/native dependencies are bundled; Microsoft Edge WebView2 Runtime remains required.
- **Contents:** 34 files / **248,107,512 bytes / 236.61 MiB**. Includes both executables, frontend/assets, Silero model/icons, license/README, release notes, five technical documents and SHA-256 manifest. Independently verified all **33 manifest entries** and exact coverage. All generated frontend assets, model and icons match the isolated source/build. Forbidden source/config/log files and known credential-pattern scan passed; this is not a universal secret-detection guarantee.
- **Portable ZIP:** `F:\Program\kikitan-translator\release\DesktopTranslator-2.0.2-alpha.20261011.2-win-x64.zip`, **107,549,788 bytes / 102.57 MiB**. SHA-256: `854b11f03edeb6a64da2843772e364fe6022931b2348b3c75c51511c0743e692`, also recorded in `.zip.sha256`. Independently read and hashed all 34 ZIP files: exact match to the final package, sidecar verified.
- The previous `DesktopTranslator-2.0.2-alpha.20261011-win-x64` directory was not overwritten. Its manifest SHA-256 remains `5df551e4c4fcde8affa95ff6e1ccf7e33324158807f3edfedd9d5ca9a508324d`, and all 17 prior manifest entries still match. Source export/archive and smoke-test evidence are ignored artifacts outside the distributable. Source archive SHA-256: `8cfb41b73dbea40458efb10e74c4dc0423956337006cb52e7c664406ac5be62f`.
- **Unverified:** live Bing hypothesis cadence/punctuation, paid-provider acceptance/rate limits, actual microphone/system-audio translation latency, Groq word-cut quality, device recovery, mixed DPI, clean-machine prerequisites, signing and long-running use. Preview prefix semantics, cooperative cancellation and batch accuracy tradeoffs remain documented in `docs/SENTENCE_LATENCY.md`. This package is ready for the user's live latency acceptance test, not production certification.

Baseline exploration: .NET 9 core with WASAPI/Silero, Bing/Groq/Gemini recognizers, three official translation adapters, Photino/React settings, WinForms subtitles and tray, DPAPI configuration, bounded queues, release scripts and executable regression runner already exist. This session builds on that architecture rather than replacing working components.

Baseline: frontend 22/22 and C# 20/20 tests passed; TypeScript/Vite production build passed after sandbox EPERM retry. Full .NET solution built with 29 warnings, 0 errors.

## Milestones

1. **Completed: speech segmentation reliability.** Added 200 ms bounded pre-roll, 360 ms silence hangover, 240 ms minimum voiced content and an exact 20-second segment cap. Preserve brief pauses and onset audio; Stop discards incomplete speech. Clear Silero stack padding explicitly and set recognition language before starting capture.
2. **Completed: actionable setup guidance.** Localized missing-key guidance respects selected speech/translation providers, shared Groq keys, transcription-only and combined Gemini operation. Actions navigate and focus the key field. Alt+1–5 switches settings sections outside text entry; listeners are removed on unmount. No new dependencies or configuration migration.
3. **Completed: validation and delivery.** Self-contained package, native rendering test, content verification, technical changelog and prioritized release assessment. Hardware/cloud/soak checks remain separate.

**Current milestone completed: sentence translation latency (continuation session).** Branch `codex/sentence-translation-latency`, starting at `97ff441`. Initial C# baseline: 24/24 passed; final: 37/37 passed. The 99 pre-existing tracked changes and untracked cleanup audit remain untouched. No unfinished implementation is required to build or use this milestone.

### Latency session results and recovery

- Saved configuration selects desktop Bing (recognizer 0), DeepL, English -> Chinese. Only routing fields were read; credentials were not printed, decrypted, changed or used.
- Cause confirmed: Bing already emits `speech.hypothesis` as nonfinal and `speech.phrase` as final, but `Kikitan.OnRecognition` previously gated translation on finals. Groq uploads whole WAV segments and emits no partials; its previous cap was 20 seconds. Gemini already has a combined streaming source/translation path.
- Implemented desktop-only previews from prefixes stable across successive hypotheses, choosing sentence punctuation or growth of 12 words / 24 CJK characters. One subtitle ID and authoritative final updates are preserved. One coalesced preview slot is separate from the unchanged final queue; queued finals take priority. Two-second cooperative cancellation bounds preview requests; finals and incompatible source revisions cancel active previews. Exact matching finals can reuse completed previews, with language-direction validation. Legacy delayed outputs and Gemini retain their existing behavior.
- Groq's hard buffered-audio segment limit is now five seconds. Pre-roll, minimum speech, 360 ms pause tolerance, serial transport and cancellation remain intact. This improves batch submission latency; it does not introduce streaming STT. More requests and mid-word hard cuts remain a tradeoff.
- Verification actually executed: full solution build with `--no-restore -m:1 -p:SkipNpmBuild=true -p:UseSharedCompilation=false -v:q` passed twice, 28 warnings / 0 errors; C# runner passed 36/36 then 37/37 after the language-direction regression fix; native subtitle `--self-test` passed rendering, update, history and scroll checks; `git diff --check` passed. No live audio/provider calls or end-to-end latency measurements. Frontend unchanged and not rebuilt in this continuation.
- Exact session files: `KikitanTranslator.Base/Kikitan.cs`, `KikitanTranslator.Base/StreamingTranslationPreview.cs`, `KikitanTranslator.Base/Properties/AssemblyInfo.cs`, `KikitanTranslator.Base/Recognizers/SpeechSegmenter.cs`, `tests/DesktopTranslator.Tests/Program.cs`, `tests/DesktopTranslator.Tests/LatencyTests.cs`, `docs/SENTENCE_LATENCY.md`, `PROGRESS.md`.
- Detailed cause analysis, changelog, test evidence, limits and prioritized architectural handoff: `docs/SENTENCE_LATENCY.md`. No broad provider/audio/IPC rewrite, configuration migration or dependency addition. The earlier portable release below does not contain this continuation's changes; the new `2.0.2-alpha.20261011.2` release above does.
- Known unfinished architecture: no linguistic/time-based stability guarantee; a corrected prefix shorter than the furthest scheduled preview can wait for final; preview translation may cover only a leading part of displayed source; cancellation depends on provider cooperation; Groq has no overlap/stitching and slow batch/translation queues can still delay output. No unsupported performance claim is made.

## Verification strategy

- Deterministic fake-capture and fake-HTTP checks exercise audio boundaries, request count, WAV samples, queue bounds and cancellation; no user audio or credentials.
- Existing C# and frontend regressions plus TypeScript/Vite and full solution build.
- Run native subtitle self-test and verify a new release directory if budget permits; never overwrite older releases.

## Files modified in earlier modernization session

- `PROGRESS.md`, `docs/modernization-baseline-files.txt`.
- `KikitanTranslator.Base/Recognizers/SpeechSegmenter.cs`, `GroqRecognizer.cs`, `KikitanTranslator.Base/Utility/Silero.cs`, `tests/DesktopTranslator.Tests/Program.cs`.
- UI `SetupGuidance.tsx`, `useSettingsShortcuts.ts`, `DesktopSettings.tsx`, `desktop.css`, three locale catalogs and `tests/setup-guidance.test.mjs`.
- `docs/MODERNIZATION_2026-10-11.md`: detailed changelog, current architecture, validation, tradeoffs and prioritized next work.

## Milestone 1 validation

- `dotnet build kikitan-translator.sln -m:1 --no-restore -p:SkipNpmBuild=true -p:UseSharedCompilation=false -v:q`: passed, 28 warnings / 0 errors.
- `dotnet run --project tests/DesktopTranslator.Tests --no-build --no-restore`: 24/24 passed with approved execution. An earlier sandbox run passed all speech tests but failed temporary-directory cleanup in an existing configuration test (23/24); the directory was empty on inspection. No application persistence code changed to hide this failure.
- Four new integration tests inspect actual multipart WAV payloads through fake HTTP; existing queue/cancellation checks updated for the silence threshold.
- Focused `git diff --check`: passed.

## Milestone 2 validation

- `npm test --prefix KikitanTranslator.Photino/UserInterface`: 26/26 passed, including four setup/shortcut regressions.
- `npm run build --prefix KikitanTranslator.Photino/UserInterface`: passed TypeScript and Vite. Main JS 394.17 kB (128.45 kB gzip), versus baseline 390.15 kB (127.00 kB gzip); no runtime performance claim. Existing outdated Browserslist-data warning remains.
- Full solution build after locale changes: passed, 28 warnings / 0 errors; C# 24/24 passed.
- Browser preview: visually inspected English dark and Chinese light at 1280x720, Japanese dark at 600x760. Verified setup action focuses the password field; Alt+4 preserves the page when editing that field and navigates to General outside it. Native Photino interaction is still unverified.
- Chrome screenshot tooling stalled; terminated that tool execution and completed visual/interaction checks in Codex's in-app browser using development-only fixtures.
- Whole working-tree `git diff --check`: passed. Baseline files remain uncommitted.

## Milestone 3 validation and deliverable

- `./scripts/build-release.ps1 -Version 2.0.2-alpha.20261011 -SkipInstall`: passed with approved execution. Repeated frontend 26/26 and C# 24/24 regressions, built/published self-contained win-x64 executables, verified frontend hashes and required assets, ran native subtitle self-test and wrote SHA-256 manifest.
- Native result: `PASS subtitle window rendering, update, history and scroll checks`.
- Package: `F:\Program\kikitan-translator\release\DesktopTranslator-2.0.2-alpha.20261011-win-x64`.
- Double-click: `DesktopTranslator.exe` in that complete directory. WebView2 remains required.
- Final contents: **18 files / 245,716,553 bytes / 234.33 MiB**, including host, subtitle executable, current frontend, Silero model, icons, ONNX support libraries, README/license/docs and manifest. No previous package overwritten.
- Product version inspected: `2.0.2-alpha.20261011+9f026014284e20881c0d10e5ddac72f2e4ad2bf2`.
- Final independent manifest check: all 17 entries match SHA-256; verifier re-run passed on all 18 files (manifest excluded from hashing itself).
- Known credential-pattern/content verifier passed; this is not proof against all possible secrets. Real packaged Photino startup, live providers/audio, mixed DPI, signing and clean-machine tests were not executed this session.
- Exact set of pre-existing modified/deleted tracked files matches `docs/modernization-baseline-files.txt` after implementation commits. No baseline file was staged in those commits.

## Architectural decisions and remaining risks

- Retain current providers, configuration format and product identity. Do not add heavyweight dependencies or silently route user audio/text to new providers.
- Speech smoothing trades a bounded silence delay for coherent utterances. Explicit sample-count bounds avoid wall-clock timing dependence.
- Existing Bing integration, real hardware/device changes, paid-provider acceptance, mixed DPI and long-running use remain release blockers until manually verified.
- Existing uncommitted baseline must remain available for a full build; focused session commits alone do not capture that baseline.

## Exact next steps

1. Begin next session with `git status --short`, `git log -5 --oneline`, this file and `docs/SENTENCE_LATENCY.md`. The latency milestone follows `97ff441`; earlier milestones are `7627925` and `9f02601`. No in-progress rewrite needs recovery.
2. Preserve the 99 pre-existing tracked changes and pre-existing untracked `docs/CLEANUP_AUDIT.md`. They are not covered by this session's focused commits; do not discard or stage them blindly.
3. Measure real Bing partial cadence, punctuation and sentence-to-subtitle latency using authorized audio/services; check preview usefulness and rate limits. Test continuous speech and word quality at Groq's five-second cuts. Add sanitized monotonic stage timings before tuning heuristic thresholds.
4. Follow the prioritized handoff in `docs/SENTENCE_LATENCY.md`: supported streaming recognizer capabilities/revision events, committed-prefix/draft-tail subtitle semantics, word-safe batch overlap/deduplication, request budgets and measured IPC coalescing. Complete each as a separate tested milestone. Keep existing lifecycle/network limitations visible.
5. Use the new `2.0.2-alpha.20261011.2` portable directory/ZIP above for live latency acceptance. Packaged startup, settings/subtitle windows, frontend serving and graceful exit have passed; actual Start/Stop with audio, repeated settings reopening, first-run behavior and the hardware/provider matrix in `docs/MANUAL_TESTS.md` still need manual testing. Real providers/audio, mixed DPI, device recovery, signing and 4–8 hour endurance remain unverified release gates. Also retain the broader backlog in `docs/MODERNIZATION_2026-10-11.md`.
