using System.Collections.Concurrent;
using System.Net;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Channels;
using KikitanTranslator.Base;
using KikitanTranslator.Base.Diagnostics;
using KikitanTranslator.Base.Translators;
using KikitanTranslator.Photino;
using KikitanTranslator.Utility;

internal static class DiagnosticsTests
{
    public static void Register(Action<string, Func<Task>> test, Action<bool, string> assert)
    {
        void Check(bool condition, string message = "Diagnostics assertion failed") => assert(condition, message);

        test("Disabled diagnostics never reads the clock, formats output or enters ambient context", async () =>
        {
            using var d = new LatencyDiagnostics(false, _ => throw new Exception("sink invoked"),
                () => throw new Exception("clock invoked"));
            var trace = d.Request(d.Recognition(Guid.NewGuid(), 1, 1), true);
            using (d.Enter(trace))
            {
                trace.Record(LatencyStage.PreviewStarted);
                Check(d.Now == 0 && LatencyDiagnostics.Context.Owner == null);
            }
            await d.CompleteAsync();
        });

        test("Diagnostics reports exact queue/request durations and window percentiles", async () =>
        {
            using var log = new Probe();
            var trace = log.Diagnostics.Request(log.Diagnostics.Recognition(Guid.NewGuid(), 0, 0), false);
            log.Clock.Advance(25); trace.Record(LatencyStage.TranslationQueueWait, 0);
            foreach (var ms in new[] { 10, 20, 30, 100 })
            {
                var start = trace.Now; log.Clock.Advance(ms); trace.Record(LatencyStage.FinalCompleted, start);
            }
            await log.Diagnostics.CompleteAsync();
            Check(log.Event(LatencyStage.TranslationQueueWait).GetProperty("elapsedMs").GetDouble() == 25);
            var summary = log.Summary(LatencyStage.FinalCompleted);
            Check(summary.GetProperty("count").GetInt64() == 4 && summary.GetProperty("p50Ms").GetDouble() == 20);
            Check(summary.GetProperty("p95Ms").GetDouble() == 100 && summary.GetProperty("maxMs").GetDouble() == 100);
        });

        test("Concurrent diagnostic recording retains correlation and counts without shared-state corruption", async () =>
        {
            using var log = new Probe();
            var traces = Enumerable.Range(0, 4).Select(_ => log.Diagnostics.Request(
                log.Diagnostics.Recognition(Guid.NewGuid(), 0, 0), true)).ToArray();
            await Task.WhenAll(traces.Select(trace => Task.Run(() =>
            {
                using var scope = log.Diagnostics.Enter(trace);
                for (var i = 0; i < 100; i++) LatencyDiagnostics.Context.Record(LatencyStage.PreviewCandidate);
            })));
            await log.Diagnostics.CompleteAsync();
            Check(log.Events(LatencyStage.PreviewCandidate).Count() == 400);
            foreach (var trace in traces) Check(log.Events(LatencyStage.PreviewCandidate)
                .Count(e => e.GetProperty("unit").GetGuid() == trace.Unit && e.GetProperty("operation").GetInt64() == trace.Operation) == 100);
            Check(log.Summary(LatencyStage.PreviewCandidate).GetProperty("count").GetInt64() == 400);
        });

        test("Percentile retention is bounded while lifetime maximum and counts remain cumulative", async () =>
        {
            using var log = new Probe();
            var trace = log.Diagnostics.Recognition(Guid.NewGuid(), 0, 0);
            log.Clock.Advance(1000); trace.Record(LatencyStage.HttpCompleted, 0);
            for (var i = 0; i < 512; i++)
            {
                var start = trace.Now; log.Clock.Advance(1); trace.Record(LatencyStage.HttpCompleted, start);
            }
            await log.Diagnostics.CompleteAsync();
            var summary = log.Summary(LatencyStage.HttpCompleted);
            Check(summary.GetProperty("sampleCount").GetInt32() == 512 && summary.GetProperty("count").GetInt64() == 513);
            Check(summary.GetProperty("p95Ms").GetDouble() == 1 && summary.GetProperty("maxMs").GetDouble() == 1000);
        });

        test("A saturated diagnostic sink drops records without blocking producers and reports losses", async () =>
        {
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var lines = new ConcurrentQueue<string>();
            using var d = new LatencyDiagnostics(true, line =>
            {
                if (!entered.IsSet) { entered.Set(); release.Wait(TimeSpan.FromSeconds(5)); }
                lines.Enqueue(line);
            }, capacity: 1);
            var trace = d.Recognition(Guid.NewGuid(), d.Now, d.Now);
            trace.Record(LatencyStage.RecognitionFinal);
            try
            {
                Check(entered.Wait(TimeSpan.FromSeconds(2)));
                await Task.Run(() => { for (var i = 0; i < 100; i++) trace.Record(LatencyStage.OriginalEmitted); })
                    .WaitAsync(TimeSpan.FromSeconds(1));
            }
            finally { release.Set(); }
            await d.CompleteAsync();
            var health = lines.Select(Parse).Last(e => e.GetProperty("type").GetString() == "health");
            Check(health.GetProperty("recordingDrops").GetInt64() >= 99);
        });

        test("Diagnostic sink failures are isolated and accounted separately", async () =>
        {
            var lines = new ConcurrentQueue<string>(); var calls = 0;
            using var d = new LatencyDiagnostics(true, line =>
            {
                if (Interlocked.Increment(ref calls) == 1) throw new IOException("synthetic private sink detail");
                lines.Enqueue(line);
            });
            d.Recognition(Guid.NewGuid(), d.Now, d.Now).Record(LatencyStage.RecognitionFinal);
            await d.CompleteAsync();
            Check(lines.Select(Parse).Last().GetProperty("sinkFailures").GetInt64() == 1);
            Check(lines.All(line => !line.Contains("private sink detail")));
        });

        test("Recognition, provider HTTP and flushed subtitle writes share IDs without leaking content", async () =>
        {
            using var log = new Probe();
            var delivered = new ConcurrentQueue<DesktopSubtitleResult>();
            using var writer = new SubtitleWriter(128, (result, _) => { delivered.Enqueue(result); return Task.CompletedTask; });
            AppConfig.ConfigObject = new() { Recognizer = 0, SourceLanguage = "en", TargetLanguage = "zh", DeepLApiKey = "synthetic-secret:fx" };
            using var http = new HttpClient(new FakeHttp((request, _) =>
            {
                Check(request.Headers.Authorization?.Parameter == "synthetic-secret:fx");
                log.Clock.Advance(40);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent("{\"translations\":[{\"text\":\"隐私测试译文\"}]}") });
            }));
            var recognizer = new FakeRecognizer();
            using var pipeline = new Kikitan(recognizer, new DeepLTranslator(http), new Errors(), true, log.Diagnostics);
            pipeline.OnSubtitle += (id, source, translated, final, update) => writer.Write(new(source, translated, final, id, update));
            pipeline.Start();
            recognizer.Emit("Private sentence.", false);
            await Wait(() => log.Events(LatencyStage.IpcWriteCompleted).Count() == 1);
            log.Clock.Advance(100);
            recognizer.Emit("Private sentence.", false);
            await Wait(() => log.Events(LatencyStage.TranslatedRecognitionToIpcWrite).Any());
            var preview = log.Event(LatencyStage.PreviewCompleted);
            var unit = preview.GetProperty("unit").GetGuid(); var operation = preview.GetProperty("operation").GetInt64();
            var recognitionEvent = preview.GetProperty("recognitionEvent").GetInt64();
            Check(operation > 0 && log.Event(LatencyStage.HttpCompleted).GetProperty("elapsedMs").GetDouble() == 40);
            foreach (var stage in new[] { LatencyStage.PreviewCandidate, LatencyStage.PreviewScheduled, LatencyStage.PreviewStarted,
                LatencyStage.HttpStarted, LatencyStage.HttpCompleted, LatencyStage.TranslationEmitted, LatencyStage.TranslatedRecognitionToIpcWrite })
            {
                var e = log.Event(stage);
                Check(e.GetProperty("unit").GetGuid() == unit && e.GetProperty("operation").GetInt64() == operation, stage.ToString());
                Check(e.GetProperty("recognitionEvent").GetInt64() == recognitionEvent, stage.ToString());
            }
            Check(log.Event(LatencyStage.RecognitionPartialGap).GetProperty("elapsedMs").GetDouble() == 100);
            Check(log.Event(LatencyStage.RecognitionToTranslation).GetProperty("elapsedMs").GetDouble() == 40);
            Check(log.Event(LatencyStage.FirstRecognitionToTranslation).GetProperty("elapsedMs").GetDouble() == 140);
            Check(log.Events(LatencyStage.OriginalEmitted).Select(e => e.GetProperty("recognitionEvent").GetInt64()).Distinct().Count() == 2);
            recognizer.Emit("Private sentence.", true);
            await Wait(() => delivered.Any(e => e.IsFinal && e.IsTranslationUpdate));
            pipeline.Stop();
            await Wait(() => log.Events(LatencyStage.IpcWriteCompleted).Count() == 5);
            await log.Diagnostics.CompleteAsync();
            Check(log.Event(LatencyStage.RecognitionPartialCount).GetProperty("value").GetInt64() == 2);
            Check(log.Events(LatencyStage.TranslationCacheHit).Count() == 1 && log.Events(LatencyStage.HttpStarted).Count() == 1);
            Check(delivered.All(e => e.Id == unit));
            var output = string.Join('\n', log.Lines);
            foreach (var forbidden in new[] { "Private sentence", "隐私测试译文", "synthetic-secret", "Authorization", "api-free.deepl.com" })
                Check(!output.Contains(forbidden, StringComparison.OrdinalIgnoreCase), "Sensitive metadata leaked");
        });

        test("Final recognition cancels preview diagnostics and preserves final precedence", async () =>
        {
            using var log = new Probe();
            using var f = new Fixture(log);
            f.Recognizer.Emit("First sentence.", false); f.Recognizer.Emit("First sentence. More", false);
            var preview = await f.Translator.Next();
            log.Clock.Advance(30); f.Recognizer.Emit("First sentence. More complete.", true);
            var final = await f.Translator.Next();
            Check(preview.Token.IsCancellationRequested);
            log.Clock.Advance(20); final.Completion.SetResult("final result");
            await Wait(() => f.Subtitles.Any(e => e.IsTranslationUpdate));
            f.Pipeline.Stop(); await log.Diagnostics.CompleteAsync();
            Check(log.Events(LatencyStage.PreviewCancelForFinal).Count() == 1 && log.Events(LatencyStage.PreviewCancelled).Count() == 1);
            Check(!log.Events(LatencyStage.PreviewTimeout).Any() && !log.Events(LatencyStage.PreviewCompleted).Any());
            Check(log.Event(LatencyStage.PreviewCancelled).GetProperty("unit").GetGuid() == log.Event(LatencyStage.FinalStarted).GetProperty("unit").GetGuid());
            Check(log.Event(LatencyStage.PreviewStarted).GetProperty("operation").GetInt64() != log.Event(LatencyStage.FinalStarted).GetProperty("operation").GetInt64());
            Check(f.Subtitles.Where(e => e.IsTranslationUpdate).All(e => e.IsFinal));
            Check(log.Events(LatencyStage.RecognitionRevision).Count() == 1);
        });

        test("Queued preview coalescing and final discard are observed without changing final priority", async () =>
        {
            using var log = new Probe(); using var f = new Fixture(log);
            f.Recognizer.Emit("blocking final", true); var blocking = await f.Translator.Next();
            f.Recognizer.Emit("First sentence.", false); f.Recognizer.Emit("First sentence. More", false);
            f.Recognizer.Emit("First sentence. Second sentence.", false);
            f.Recognizer.Emit("First sentence. Second sentence. More", false);
            f.Recognizer.Emit("First sentence. Second sentence.", true);
            blocking.Completion.SetResult("first result");
            var final = await f.Translator.Next(); Check(final.Text == "First sentence. Second sentence.");
            final.Completion.SetResult("final result");
            await Wait(() => f.Subtitles.Count(e => e.IsTranslationUpdate) == 2);
            f.Pipeline.Stop(); await log.Diagnostics.CompleteAsync();
            Check(log.Events(LatencyStage.PreviewScheduled).Count() == 2);
            Check(log.Events(LatencyStage.PreviewCoalesced).Count() == 1 && log.Events(LatencyStage.PreviewDiscardedForFinal).Count() == 1);
            Check(!log.Events(LatencyStage.PreviewStarted).Any() && log.Events(LatencyStage.FinalStarted).Count() == 2);
        });

        test("Final queue wait, capacity eviction and provider failure are distinct diagnostic outcomes", async () =>
        {
            using var log = new Probe(); using var f = new Fixture(log);
            f.Recognizer.Emit("first", true); var first = await f.Translator.Next();
            f.Recognizer.Emit("evicted", true); f.Recognizer.Emit("third", true); f.Recognizer.Emit("fourth", true); f.Recognizer.Emit("fifth", true);
            log.Clock.Advance(80); first.Completion.SetException(new IOException("private provider body"));
            var third = await f.Translator.Next(); Check(third.Text == "third"); third.Completion.SetResult("third result");
            (await f.Translator.Next()).Completion.SetResult("fourth result");
            (await f.Translator.Next()).Completion.SetResult("fifth result");
            await Wait(() => f.Subtitles.Count(e => e.IsTranslationUpdate) == 4);
            f.Pipeline.Stop(); await log.Diagnostics.CompleteAsync();
            Check(log.Events(LatencyStage.TranslationQueueDropped).Count() == 1 && log.Events(LatencyStage.FinalFailed).Count() == 1);
            Check(log.Events(LatencyStage.TranslationQueueWait).Count(e => e.GetProperty("elapsedMs").GetDouble() == 80) == 3);
            Check(log.Events(LatencyStage.OriginalEmitted).Count() == 5 && !string.Join('\n', log.Lines).Contains("private provider body"));
        });

        test("Revision cancellation and stale noncooperative preview results have separate diagnostics", async () =>
        {
            using var log = new Probe(); using var f = new Fixture(log, ignoreCancellation: true);
            f.Recognizer.Emit("Old sentence.", false); f.Recognizer.Emit("Old sentence. More", false);
            var preview = await f.Translator.Next();
            f.Recognizer.Emit("Corrected sentence", false);
            preview.Completion.SetResult("obsolete translation");
            await Wait(() => log.Events(LatencyStage.PreviewStale).Any());
            f.Pipeline.Stop(); await log.Diagnostics.CompleteAsync();
            Check(log.Events(LatencyStage.PreviewCancelForRevision).Count() == 1);
            Check(f.Subtitles.All(e => !e.IsTranslationUpdate));
        });

        test("HTTP failure, external cancellation and transport timeout record only safe status metadata", async () =>
        {
            foreach (var mode in new[] { 0, 1, 2 })
            {
                using var log = new Probe(); using var cancel = new CancellationTokenSource();
                using var client = new HttpClient(new FakeHttp((_, token) =>
                {
                    log.Clock.Advance(15);
                    if (mode == 1) { cancel.Cancel(); token.ThrowIfCancellationRequested(); }
                    if (mode == 2) throw new TaskCanceledException("secret timeout url");
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("private body") });
                }));
                var trace = log.Diagnostics.Request(log.Diagnostics.Recognition(Guid.NewGuid(), 0, 0), false);
                using (log.Diagnostics.Enter(trace))
                {
                    try { await TranslationHttp.SendAsync(client, () => new(HttpMethod.Post, "https://example.invalid"), cancel.Token); }
                    catch (Exception e) when (e is InvalidOperationException or OperationCanceledException) { }
                }
                await log.Diagnostics.CompleteAsync();
                var stage = mode == 0 ? LatencyStage.HttpFailed : mode == 1 ? LatencyStage.HttpCancelled : LatencyStage.HttpTimeout;
                Check(log.Event(stage).GetProperty("elapsedMs").GetDouble() == 15);
                Check(!string.Join('\n', log.Lines).Contains("private body") && !string.Join('\n', log.Lines).Contains("secret timeout"));
            }
        });

        test("IPC queue overflow, queue waiting, write failure and timeout remain distinct", async () =>
        {
            using var log = new Probe();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = 0;
            using var writer = new SubtitleWriter(1, async (_, token) =>
            {
                if (Interlocked.Increment(ref calls) == 1) { started.SetResult(); await release.Task.WaitAsync(token); throw new IOException("private pipe error"); }
                throw new TimeoutException("private pipe timeout");
            });
            var trace = log.Diagnostics.Recognition(Guid.NewGuid(), 0, 0);
            using (log.Diagnostics.Enter(trace)) writer.Write(new("source", "", false, trace.Unit));
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            using (log.Diagnostics.Enter(trace))
            {
                writer.Write(new("source", "", false, trace.Unit));
                writer.Write(new("source", "", false, trace.Unit));
            }
            log.Clock.Advance(60); release.SetResult();
            await Wait(() => log.Events(LatencyStage.IpcWriteTimeout).Any());
            await log.Diagnostics.CompleteAsync();
            Check(log.Events(LatencyStage.IpcQueued).Count() == 2 && log.Events(LatencyStage.IpcQueueDropped).Count() == 1);
            Check(log.Event(LatencyStage.IpcWriteFailed).GetProperty("elapsedMs").GetDouble() == 60);
            Check(log.Events(LatencyStage.IpcQueueWait).Last().GetProperty("elapsedMs").GetDouble() == 60);
            Check(!log.Events(LatencyStage.IpcWriteCompleted).Any());
        });

        test("Real local IPC flush preserves the subtitle protocol and records write completion only", async () =>
        {
            using var log = new Probe();
            using var timeout = new CancellationTokenSource(3000);
            await using var server = new NamedPipeServerStream($"kikitan-desktop-subtitles-{Environment.ProcessId}",
                PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            using var writer = new SubtitleWriter();
            var trace = log.Diagnostics.Request(log.Diagnostics.Recognition(Guid.NewGuid(), 0, 0), false);
            using (log.Diagnostics.Enter(trace)) writer.Write(new("Synthetic source", "测试译文", true, trace.Unit, true));
            await server.WaitForConnectionAsync(timeout.Token);
            using var reader = new StreamReader(server);
            var line = await reader.ReadLineAsync(timeout.Token);
            var result = JsonSerializer.Deserialize<DesktopSubtitleResult>(line!);
            await Wait(() => log.Events(LatencyStage.IpcWriteCompleted).Any());
            await log.Diagnostics.CompleteAsync();
            Check(result == new DesktopSubtitleResult("Synthetic source", "测试译文", true, trace.Unit, true));
            Check(log.Event(LatencyStage.IpcWriteCompleted).GetProperty("operation").GetInt64() == trace.Operation);
            Check(!log.Events(LatencyStage.IpcEmissionToWrite).Any(), "Missing emission timestamp must not be invented");
            Check(!line!.Contains("RecognitionAt") && !line.Contains("Operation"));
        });

        test("IPC cancellation is distinct from pipe failure and successful writing", async () =>
        {
            using var log = new Probe();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var writer = new SubtitleWriter(1, async (_, token) => { started.SetResult(); await Task.Delay(Timeout.Infinite, token); });
            try
            {
                var trace = log.Diagnostics.Recognition(Guid.NewGuid(), 0, 0);
                using (log.Diagnostics.Enter(trace)) writer.Write(new("source", "", false, trace.Unit));
                await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            }
            finally { writer.Dispose(); }
            await log.Diagnostics.CompleteAsync();
            Check(log.Events(LatencyStage.IpcCancelled).Count() == 1);
            Check(!log.Events(LatencyStage.IpcWriteFailed).Any() && !log.Events(LatencyStage.IpcWriteCompleted).Any());
        });

        test("HTTP retry and provider cooldown diagnostics do not add requests or expose response content", async () =>
        {
            using var log = new Probe(); var calls = 0;
            using var client = new HttpClient(new FakeHttp((_, _) =>
            {
                var response = new HttpResponseMessage(++calls == 1 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK)
                    { Content = new StringContent("private response") };
                response.Headers.RetryAfter = new(TimeSpan.FromSeconds(.2));
                return Task.FromResult(response);
            }));
            var trace = log.Diagnostics.Request(log.Diagnostics.Recognition(Guid.NewGuid(), 0, 0), false);
            using (log.Diagnostics.Enter(trace))
                await TranslationHttp.SendAsync(client, () => new(HttpMethod.Post, "https://example.invalid"), CancellationToken.None);
            var cooldownCalls = 0;
            using var cooling = new HttpClient(new FakeHttp((_, _) =>
            {
                cooldownCalls++;
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new(TimeSpan.FromSeconds(10));
                return Task.FromResult(response);
            }));
            using (log.Diagnostics.Enter(trace))
                for (var i = 0; i < 2; i++)
                    try { await TranslationHttp.SendAsync(cooling, () => new(HttpMethod.Post, "https://example.invalid"), CancellationToken.None); }
                    catch (InvalidOperationException) { }
            await log.Diagnostics.CompleteAsync();
            Check(calls == 2 && cooldownCalls == 1);
            Check(log.Events(LatencyStage.HttpRetry).Count() == 1 && log.Events(LatencyStage.HttpCooldown).Count() == 2);
            Check(log.Events(LatencyStage.HttpCompleted).Count() == 1 && log.Events(LatencyStage.HttpFailed).Count() == 2);
            Check(!string.Join('\n', log.Lines).Contains("private response"));
        });
    }

    private static async Task Wait(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(3000);
        while (!predicate()) await Task.Delay(5, timeout.Token);
    }
    private static JsonElement Parse(string line) { using var doc = JsonDocument.Parse(line); return doc.RootElement.Clone(); }
    private sealed class Clock
    {
        private long _ticks;
        public long Now() => Interlocked.Read(ref _ticks);
        public void Advance(long milliseconds) => Interlocked.Add(ref _ticks, milliseconds);
    }
    private sealed class Probe : IDisposable
    {
        public readonly Clock Clock = new();
        public readonly ConcurrentQueue<string> Lines = new();
        private readonly ConcurrentQueue<JsonElement> _entries = new();
        public readonly LatencyDiagnostics Diagnostics;
        public Probe() => Diagnostics = new(true, line => { Lines.Enqueue(line); _entries.Enqueue(Parse(line)); }, Clock.Now, 1000);
        public IEnumerable<JsonElement> Events(LatencyStage stage) => _entries.Where(e => e.GetProperty("type").GetString() == "event" && e.GetProperty("stage").GetString() == stage.ToString());
        public JsonElement Event(LatencyStage stage) => Events(stage).Single();
        public JsonElement Summary(LatencyStage stage) => _entries.Last(e => e.GetProperty("type").GetString() == "summary" && e.GetProperty("stage").GetString() == stage.ToString());
        public void Dispose() => Diagnostics.Dispose();
    }
    private sealed record Request(string Text, CancellationToken Token)
    {
        public readonly TaskCompletionSource<string?> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private sealed class Translator(bool ignoreCancellation) : ITranslator
    {
        private readonly Channel<Request> _requests = Channel.CreateUnbounded<Request>();
        private readonly ConcurrentQueue<Request> _all = new();
        public Task<Request> Next() => _requests.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        public string? Translate(string text, string source, string target) => throw new NotSupportedException();
        public async Task<string?> TranslateAsync(string text, string source, string target, CancellationToken cancellationToken)
        {
            var request = new Request(text, cancellationToken); _all.Enqueue(request); _requests.Writer.TryWrite(request);
            return ignoreCancellation ? await request.Completion.Task : await request.Completion.Task.WaitAsync(cancellationToken);
        }
        public void Dispose() { foreach (var request in _all) request.Completion.TrySetResult(""); }
    }
    private sealed class Fixture : IDisposable
    {
        public readonly FakeRecognizer Recognizer = new();
        public readonly Translator Translator;
        public readonly Kikitan Pipeline;
        public readonly ConcurrentQueue<DesktopSubtitleResult> Subtitles = new();
        public Fixture(Probe log, bool ignoreCancellation = false)
        {
            AppConfig.ConfigObject = new() { Recognizer = 0, SourceLanguage = "en", TargetLanguage = "zh" };
            Translator = new(ignoreCancellation);
            Pipeline = new(Recognizer, Translator, new Errors(), true, log.Diagnostics);
            Pipeline.OnSubtitle += (id, source, translated, final, update) => Subtitles.Enqueue(new(source, translated, final, id, update));
            Pipeline.Start();
        }
        public void Dispose() { Translator.Dispose(); Pipeline.Dispose(); }
    }
}
