# Desktop modernization checkpoint — 2026-10-11

This is a tested incremental release checkpoint, not a claim that the entire product is production-ready. Start continuation work with the root `PROGRESS.md`, Git status and recent commits. Historical audits describe earlier snapshots.

## Delivered changes

### Speech reliability (`7627925`)

Previously Groq Whisper finalized speech on the first negative VAD frame. A brief inter-word pause could split an utterance, and two short syllables could both fall below the minimum length and disappear. `SpeechSegmenter` now collects speech using sample counts:

| Rule | Value at 16 kHz | Purpose |
|---|---:|---|
| Pre-roll | 200 ms / 3,200 samples | Include onset audio preceding positive VAD classification |
| End silence | 360 ms / 5,760 samples | Retain brief pauses within an utterance |
| Minimum voiced content | 240 ms / 3,840 samples | Reject short noise without counting padding as speech |
| Maximum segment | 20 seconds / 320,000 total samples | Bound each queued waveform, including pre-roll and silence |

Continuous speech splits at the cap without losing the next sample. Stop discards an incomplete utterance and cancels queued/in-flight work. Language is selected before starting capture, so synchronous capture callbacks cannot precede language initialization. The existing serial HTTP worker and eight-slot speech queue remain in use.

Silero's 480-to-512-sample adapter explicitly clears its stack buffer before padding. Stack allocation does not guarantee initialized contents; padding must contain silence. The ONNX model, threshold and provider selection are unchanged. A future adapter using contiguous native-size model frames should be validated independently against real audio.

This trades a 360 ms silence requirement for coherent utterances. Actual latency also includes VAD, capture packet cadence, network and provider processing. Neither recognition accuracy nor end-to-end latency was measured with live services.

### Setup and keyboard access (`9f02601`)

- Added a compact setup section using the existing typography, borders, color tokens and responsive layout.
- Missing-key guidance follows selected providers and acknowledged saved-key indicators; a draft alone does not mark setup complete.
- Shared Groq credentials satisfy both Groq speech and translation. Transcription-only mode omits translation-key requirements. Gemini's combined pipeline does not request a separate translation key. Legacy microphone mode does not display desktop setup guidance.
- Actions navigate to and focus the appropriate password field, including when already on that settings page.
- Added English, Simplified Chinese and Japanese strings. No new configuration fields or dependencies.
- Alt+1–5 navigates Translation, Speech recognition, Subtitles, General and About. Accessible shortcut metadata and General-page help make this discoverable. Text entry, composition, held/repeated keys and conflicting modifiers are ignored; event listeners are removed on unmount.
- Saving credentials is explicitly described as storage confirmation, not a connectivity or quota check. Guidance disappears when required saved-key indicators are present; it does not claim the service is healthy.

## Current architecture

```text
WASAPI playback / retained microphone capture
    -> mono 16 kHz audio + Silero voice classification
    -> recognizer (Groq: bounded sample-based segmenter -> serial speech HTTP)
    -> source subtitle with stable utterance ID
    -> bounded asynchronous translation queue -> existing ITranslator adapters
    -> matching translation update
    -> current-user named pipe -> native subtitle history / WinForms renderer

Photino settings host -> React settings and redacted configuration state
                     -> persisted DPAPI-protected credentials
                     -> manager lifecycle / child-process supervision / tray controls
```

`SpeechSegmenter` owns only utterance boundaries and buffers; it has no provider, configuration, capture-device or UI dependency. `GroqRecognizer` retains transport and lifecycle ownership. `SetupGuidance` derives requirements from existing state, while the existing credential editor retains save/acknowledgement ownership. Provider interfaces remain suitable extension points; no local-model or fallback provider has been implemented in this checkpoint.

## Executed validation

| Check | Result and limits |
|---|---|
| Baseline C# runner | 20/20 passed |
| Updated C# runner | 24/24 passed; fake capture/HTTP plus isolated Windows credential persistence |
| Baseline frontend tests | 22/22 passed |
| Updated frontend tests | 26/26 passed |
| Full .NET solution after both milestones | Passed, 28 existing warnings / 0 errors |
| TypeScript + Vite production build | Passed; existing stale Browserslist-data warning remains |
| Browser visual review | English dark and Chinese light at 1280×720; Japanese dark at 600×760 |
| Browser interaction | Setup action focuses password input; Alt+4 leaves text editing undisturbed and navigates to General outside input |
| Narrow-layout overflow | 600 px document width; main client/scroll widths both 585 px |
| Git whitespace check | Passed, including pre-existing working-tree changes |
| Self-contained win-x64 release script | Passed, including a repeat of both test suites, required-resource checks and per-asset frontend hash comparison |
| Published subtitle executable | Passed its real WinForms rendering, update, history and scrolling self-test |
| Release-content verification | Passed; no source, config, logs, symbols or known credential patterns found by the existing verifier |
| Independent final SHA-256 verification | All 17 manifest entries match; 18 total files including the manifest itself |

The new speech checks inspect actual multipart WAV payloads emitted by the recognizer: onset samples, the internal pause, sample rate and payload length; silence/noise rejection; continuous speech split without missing samples; incomplete utterance cancellation. The existing queue-pressure and Stop tests still pass with the new silence threshold. Tests make no external provider requests and capture no hardware audio.

The new frontend checks cover selected/shared credentials, transcription-only and combined providers, action destinations, removal of completed guidance, keyboard guards and listener cleanup. They do not replace native WebView2 or screen-reader acceptance.

One sandboxed C# run passed 23/24 tests because an existing configuration fixture failed to delete its temporary directory. The directory was empty on subsequent inspection; all 24 tests passed with approved execution outside that sandbox. Initial Vite sandbox execution also failed on filesystem EPERM; approved production builds passed. Chrome screenshot tooling stalled, so browser visual verification used the in-app browser instead.

## Size and performance evidence

Main frontend bundle changed from 390.15 to 394.17 kB uncompressed and 127.00 to 128.45 kB gzip. CSS changed from 10.73 to 11.13 kB. These are Vite build reports, not memory or startup benchmarks. No new libraries were added. There are no measured claims for active CPU, cold startup, recognition accuracy, real request throughput or shutdown duration.

## Release and reproducibility

The local release build uses:

```powershell
./scripts/build-release.ps1 -Version 2.0.2-alpha.20261011 -SkipInstall
```

`-SkipInstall` uses the already restored workspace dependencies. On a fresh environment omit it to run the locked npm install. The release script builds the frontend/solution, runs both test suites, publishes self-contained Windows x64 host and subtitle executables, compares every packaged frontend asset with the generated file, runs the native subtitle rendering self-test, inspects contents and writes SHA-256 hashes. A new version directory preserves old packages. No publishing or deployment is performed.

Package: `release/DesktopTranslator-2.0.2-alpha.20261011-win-x64/`; double-click **DesktopTranslator.exe** in the complete directory. It contains **18 files, 245,716,553 bytes (234.33 MiB)**. Contents: host and subtitle executables, frontend HTML/JS/CSS/SVG, icons, Silero ONNX model, two small ONNX publish-support libraries, README, license, four existing technical/manual documents and SHA256SUMS.txt. This new modernization report and `PROGRESS.md` are repository handoff documents. The executable's product version is `2.0.2-alpha.20261011` with source commit `9f02601` in its informational version.

The package requires Windows and WebView2; .NET is self-contained. Code signing, actual packaged Photino startup and clean-machine acceptance have not been performed in this session. Older packages were preserved. No external deployment or publishing occurred.

## Release-readiness assessment and next priorities

**Status: improved alpha, suitable for controlled acceptance testing; not yet validated for a production release.** The code and UI milestones are complete. Hardware/cloud/endurance acceptance and distribution readiness remain incomplete.

| Priority | Remaining work | Verification required |
|---|---|---|
| P0 | Validate real playback, Groq transcription and each configured translation provider | Use an isolated profile; sentence accuracy, direction, no lost source, bad-key/quota/network cases |
| P0 | Establish a supported long-term speech-provider default before commercial distribution | Review provider terms/support and implement an official integration; do not depend on inherited Bing protocol behavior as a guarantee |
| P1 | Capture recovery and lifecycle stress | Unplug last device, switch endpoints, sleep/resume, rapid Stop/Start, settings reopen and clean Exit on actual Windows hardware |
| P1 | Persistence failure semantics | Inject disk-full/access-denied failures; verify failed in-memory edits are rolled back or explicitly shown as unsaved; preserve corrupted subtitle preferences before replacement |
| P1 | Speech backlog freshness and provider rate-limit policy | Test prolonged outages with eight queued utterances; add age/drop policy and speech Retry-After handling without retrying before provider deadlines |
| P1 | Real speech segmentation evaluation | Quiet onsets, sub-240 ms words, pauses near 360 ms, long speech, noise, loopback packet gaps and microphone behavior |
| P1 | Endurance and performance baselines | 4–8 hour runs with CPU/private-memory/handle samples, queue lengths and audio-to-subtitle timestamps |
| P1 | Clean-machine release acceptance | Windows/WebView2 prerequisites, bundled resources, high DPI, signing and archive/distribution checks |
| P2 | Native accessibility and complete first-run experience | Screen reader, high contrast, mixed DPI; decide explicit first-run capture/startup behavior and add audio diagnostics |
| P2 | Provider expansion | Local inference and configurable fallbacks only after explicit user routing/privacy choices and provider conformance tests |
| P2 | Engineering maintenance | Audit nullable/async warnings and SDK/dependency support; resolve retained legacy UI/backend mismatches before expanding microphone controls |
| P3 | Optional product features | Evaluate global hotkeys, diagnostic export, supported update checking and transcript export against demonstrated user need |

Use the existing `MANUAL_TESTS.md` for the broader acceptance matrix. No full architecture rewrite, new provider, global hotkey, update mechanism or diagnostic exporter is claimed here.

## Recovery and scope boundaries

The working tree already contained extensive uncommitted cleanup at session start. Those files are listed in `docs/modernization-baseline-files.txt`; `docs/CLEANUP_AUDIT.md` was already untracked. They were preserved and excluded from milestone commits. Building this exact package requires those working-tree changes as well as this session's commits. Do not assume a fresh checkout of the branch alone reproduces the entire input snapshot. Never stage all changes indiscriminately.

Session rollback can revert the focused milestone commits without discarding the user's baseline. Preserve the progress/report files when handing work to the next agent. The next session should resolve baseline ownership deliberately and then work through one acceptance blocker at a time.
