# Sentence translation latency — 2026-10-11

## Scope and active route

This continuation follows `97ff441` on `codex/sentence-translation-latency`. It changes only translation scheduling, a small hypothesis policy, the batch segment cap and regression tests. The existing cleanup working tree, settings, credentials, provider endpoints, frontend, audio capture and subtitle transport are preserved.

The saved routing fields select desktop Bing (`Recognizer = 0`) -> DeepL, English -> Chinese, with translation enabled. Only routing fields were inspected; no credentials were printed, decrypted, modified or used. This describes the local configuration at inspection time, not every user's configuration.

## Exact delay sources before this change

| Stage | Existing implementation and consequence |
| --- | --- |
| Bing recognition | `Recognizers/Bing.cs` already streams audio and raises nonfinal text from `speech.hypothesis`, and final text from `speech.phrase`. It does not wait for the local Groq segmenter. The service chooses when a phrase becomes final. |
| Translation admission | `MorChranslator.OnRecognition` published nonfinal original text but enqueued translation **only when final was true**. Thus even a completed sentence in a continuing Bing hypothesis waited for the provider's final phrase. This is the primary code-level gate on the active route. |
| Groq recognition | `GroqRecognizer.cs` serially POSTs complete WAV files for transcription. Its segmenter waited for 360 ms of classified silence or 20 seconds of continuous buffered audio. It emits only completed transcription responses; this integration has no streaming partial results. A short pause under 360 ms deliberately joins speech rather than submitting a fragment. |
| Recognition backlog | Groq permits eight queued segments plus one in flight and has a 25-second HTTP timeout. Slow transcription can accumulate delay after segmentation. Shorter segments do not eliminate that backlog. |
| Translation backlog | Desktop translation permits three pending finals plus one request in flight, drops the oldest queued final when full, and skips queued items older than ten seconds. These existing freshness policies do not preempt an already running final request. |
| Network | Existing translation transport has a 20-second request budget, bounded retries and provider cooldown handling. Credentials, network failures and rate limits can dominate latency after a sentence is admitted. |
| Subtitle delivery | `SubtitleWriter` uses a bounded 128-message channel and a new named-pipe connection per update with a 500 ms connection timeout. Native history applies updates by stable ID; rendering does not wait for sentence completion. A stalled subtitle process can still create transport delay. |

Gemini's existing combined source/translation streaming route bypasses the external translator and remains unchanged. Streaming capability here means behavior present in this repository; this work does not certify the long-term support or availability of the Bing service integration.

## Implemented behavior

- Desktop external translation can begin from a stable leading prefix seen in two successive hypotheses. Sentence punctuation (`.`, `?`, `!`, `。`, `？`, `！`) triggers a preview. Conservative exclusions avoid common abbreviations, initials and decimals; closing quotes stay with the sentence.
- Continuous text without punctuation can advance after twelve additional whitespace-delimited words or twenty-four additional CJK characters. English prefixes ending inside a word are trimmed. This is a heuristic, not full linguistic sentence segmentation or a time-based stability guarantee.
- One coalesced preview slot is separate from the unchanged bounded final queue. Queued finals take priority; previews cannot evict them. Requests use the existing provider and carry source/target snapshots.
- A preview has a two-second cancellation budget. A final recognition or incompatible source revision cancels the active preview. Old results are checked against utterance ID, source prefix, language direction and cancellation state before publication. Actual interruption requires the provider to honor cancellation; the worker remains serial.
- Original text appears immediately and continues growing. Preview translation covers its stable leading text and updates the same subtitle ID. Subsequent source updates retain that translation unless the source or language direction invalidates it. Final recognition remains authoritative and is translated in full. An exact final matching a completed preview with the same direction reuses it.
- Provisional translations go only to the desktop subtitle event. Chatbox/OSC and other legacy output final behavior, transcription-only operation and Gemini's combined path remain intact.
- Groq's exact maximum buffered segment is now five seconds rather than twenty. The 200 ms pre-roll, 360 ms silence hangover and 240 ms minimum voiced content remain. Tests verify sample continuity across hard cuts and packet sizes. This also affects the retained microphone Groq path using the same segmenter.

The Groq change trades more frequent requests and possible cuts through a word for earlier batch submission. There is no acoustic overlap or cross-request text stitching yet. The cap is on buffered audio and can include pre-roll/silence, so it is not a guarantee of five seconds of voiced content. A preview can repeat leading context in later requests, increasing translation usage. Punctuation revisions can still change provisional meaning. No setting migration or new dependency is required.

## Verification

Baseline: C# runner 24/24 passed before editing.

- `dotnet build MorChranslator.sln --no-restore -m:1 -p:SkipNpmBuild=true -p:UseSharedCompilation=false -v:q`: passed, 28 existing warnings and zero errors on the first complete build; repeated after the final direction-cache regression fix.
- `dotnet run --project tests/MorChranslator.Tests --no-build --no-restore`: 37/37 passed after the final fix (13 new tests plus the existing 24, including the updated Groq cap regression).
- Native subtitle executable `--self-test`: passed rendering, update, history and scroll checks.
- `git diff --check`: passed. Existing user changes are excluded from the focused commit.

Tests use controlled recognition callbacks, explicitly released fake translation requests, fake HTTP inspecting actual WAV payloads and exact sample counts. Waits have failure deadlines; tests do not assert real network wall-clock speed. Coverage includes punctuation/quotes/abbreviations/CJK, unpunctuated continuous text, source revisions, final priority, coalescing, cancellation, exact-final cache reuse and direction changes, unchanged legacy/Gemini/transcription-only paths, a 60 ms pause, the exact 360 ms silence boundary and uninterrupted speech with multiple capture packet sizes. One WAV test waits for the first request at exactly 80,000 samples before sending any further audio, proving batch submission does not require a later pause.

Live microphone/system audio, Bing hypothesis cadence/punctuation, DeepL acceptance, provider charges/rate limits, word quality at Groq cuts and end-to-end latency remain unverified. Frontend files did not change; frontend checks were not repeated in this continuation. The older `release/MorChranslator-2.0.2-alpha.20261011-win-x64` package is historical and does **not** contain these changes. No new portable package or deployment was performed.

## Prioritized architectural continuation

1. Measure with authorized real audio/services before tuning thresholds: capture time, hypothesis arrival, stable-prefix admission, queue age, request duration and subtitle delivery. Use monotonic timestamps and sanitized diagnostics without raw speech or credentials by default. Compare sentence latency and transcription quality across silence and continuous speech.
2. Establish a supported streaming STT integration with explicit recognizer capabilities and structured hypothesis/revision/utterance events. Today `Recognizer == 2` distinguishes Gemini, and other recognizers share `(text, final)` events. Do not infer true streaming support for Groq's current whole-file endpoint or bypass provider restrictions.
3. Introduce committed-prefix/draft-tail semantics and a subtle provisional indicator in subtitles. Current preview translation may cover only the stable prefix while the original includes an untranslated tail. Use timestamped sentence IDs and revision generations to avoid retranslating growing cumulative prefixes and to revisit corrected sentences of equal/shorter length. The current policy only advances beyond its furthest scheduled length until final/reset; a revised shorter sentence may wait for final.
4. For batch Groq, evaluate word-safe endpointing, small acoustic overlap and timestamp-based deduplication, preserving onset and every sample. Benchmark increased request frequency, accuracy/context loss, serial queue age and service limits before choosing a configurable cap. Avoid an unfinished overlap/stitch rewrite.
5. Add explicit preview request/character budgets and provider capability-aware scheduling. Two seconds bounds cooperative preview occupancy, not provider billing or total request count. A noncooperative provider can still delay finals/shutdown; final transport can still occupy its existing longer budget. Design bounded lifecycle handling before introducing concurrency or detached tasks.
6. Profile subtitle IPC backlog and coalesce updates per utterance if measured stalls justify it. Preserve final entries and source text; avoid replacing transport without deterministic ordering/cancellation tests.

Each item should be a separate validated milestone. Preserve the 99 pre-existing tracked changes and untracked cleanup audit; focused commits rely on that local baseline. Resume from `PROGRESS.md`, current Git status and recent commits, not from historical release claims.
