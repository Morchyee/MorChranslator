using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Serilog;

namespace KikitanTranslator.Base.Diagnostics;

// Fixed event names and numeric metadata only. Never accept provider text, URLs or exceptions.
public enum LatencyStage
{
    RecognitionFirstPartial, RecognitionPartial, RecognitionRevision, RecognitionPartialGap, RecognitionFinal, RecognitionPartialCount,
    PreviewCandidate, PreviewScheduled, PreviewCoalesced, PreviewDiscardedForFinal,
    PreviewCancelForFinal, PreviewCancelForRevision, PreviewStarted, PreviewCompleted, PreviewFailed,
    PreviewCancelled, PreviewTimeout, PreviewStale, FinalQueued, FinalStarted, FinalCompleted, FinalFailed,
    TranslationQueueWait, TranslationExpired, TranslationQueueDropped, TranslationCancelled,
    TranslationCacheHit, TranslationBypassed, TranslationTimeout,
    HttpStarted, HttpCompleted, HttpFailed, HttpCancelled, HttpTimeout, HttpRetry, HttpCooldown,
    OriginalEmitted, TranslationEmitted, CombinedTranslationEmitted,
    IpcQueued, IpcQueueDropped, IpcQueueWait, IpcWriteCompleted, IpcWriteFailed, IpcWriteTimeout,
    IpcCancelled, IpcEmissionToWrite, RecognitionToTranslation, FirstRecognitionToTranslation,
    OriginalRecognitionToIpcWrite, TranslatedRecognitionToIpcWrite, FirstRecognitionToIpcWrite
}

public readonly record struct LatencyTrace(LatencyDiagnostics? Owner, Guid Unit, long Operation,
    long RecognitionAt, long FirstRecognitionAt, bool Preview = false, long? EmissionAt = null, long RecognitionEvent = 0)
{
    public long Now => Owner?.Now ?? 0;
    public void Record(LatencyStage stage, long? startedAt = null, long value = 0) => Owner?.Record(this, stage, startedAt, value);
}

/// <summary>Opt-in, bounded, nonblocking producer; all formatting and logging happens on one worker.</summary>
public sealed class LatencyDiagnostics : IDisposable
{
    private static readonly AsyncLocal<LatencyTrace> Ambient = new();
    private static readonly Lazy<LatencyDiagnostics> Default = new(() => new(
        Environment.GetEnvironmentVariable("KIKITAN_LATENCY_DIAGNOSTICS") == "1",
        line => Log.Information("[LATENCY] {Diagnostic}", line)));
    public static LatencyDiagnostics Current => Default.Value;
    public static LatencyTrace Context => Ambient.Value;

    private readonly Func<long> _clock;
    private readonly double _millisecondsPerTick;
    private readonly long _origin;
    private readonly Action<string> _sink;
    private readonly Channel<Entry>? _events;
    private readonly Task _worker = Task.CompletedTask;
    private long _operation, _recognitionEvent, _dropped, _sinkFailures;
    private int _disposed;
    public bool Enabled { get; }
    public long Now => Enabled ? _clock() : 0;
    private readonly record struct Entry(LatencyTrace Trace, LatencyStage Stage, long At, long? StartedAt, long Value);

    public LatencyDiagnostics(bool enabled, Action<string> sink, Func<long>? clock = null,
        long frequency = 0, int capacity = 2048)
    {
        Enabled = enabled;
        _sink = sink;
        _clock = clock ?? Stopwatch.GetTimestamp;
        _millisecondsPerTick = 1000d / (frequency > 0 ? frequency : Stopwatch.Frequency);
        if (!enabled) return;
        _origin = _clock();
        _events = Channel.CreateBounded<Entry>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true, FullMode = BoundedChannelFullMode.Wait, AllowSynchronousContinuations = false
        });
        // Do not inherit a caller's request context into this long-lived worker.
        using (ExecutionContext.SuppressFlow()) _worker = Task.Run(ConsumeAsync);
    }

    public LatencyTrace Recognition(Guid unit, long at, long firstAt) => Enabled
        ? new(this, unit, 0, at, firstAt, RecognitionEvent: Interlocked.Increment(ref _recognitionEvent)) : default;
    public LatencyTrace Request(LatencyTrace recognition, bool preview) => Enabled
        ? recognition with { Operation = Interlocked.Increment(ref _operation), Preview = preview } : default;

    public Scope Enter(LatencyTrace trace) => new(trace);
    public readonly struct Scope : IDisposable
    {
        private readonly LatencyTrace _previous;
        private readonly bool _enabled;
        internal Scope(LatencyTrace trace)
        {
            _enabled = trace.Owner?.Enabled == true;
            _previous = _enabled ? Ambient.Value : default;
            if (_enabled) Ambient.Value = trace;
        }
        public void Dispose() { if (_enabled) Ambient.Value = _previous; }
    }

    public void Record(LatencyTrace trace, LatencyStage stage, long? startedAt = null, long value = 0)
    {
        if (!Enabled || Volatile.Read(ref _disposed) != 0) return;
        if (!_events!.Writer.TryWrite(new(trace, stage, _clock(), startedAt, value)))
            Interlocked.Increment(ref _dropped);
    }

    private sealed class Metric
    {
        public long Count, ValueTotal;
        public double Maximum;
        public readonly double[] Samples = new double[512];
        public int SampleCount, Next;
        public void Add(double? elapsed, long value)
        {
            Count++; ValueTotal += value;
            if (elapsed is not { } ms) return;
            Maximum = Math.Max(Maximum, ms);
            Samples[Next] = ms; Next = (Next + 1) % Samples.Length;
            SampleCount = Math.Min(Samples.Length, SampleCount + 1);
        }
    }

    private async Task ConsumeAsync()
    {
        var metrics = new Dictionary<LatencyStage, Metric>();
        Write(new { type = "session", schema = 1, clock = "monotonic", percentileWindow = 512 });
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        var tick = timer.WaitForNextTickAsync().AsTask();
        var ready = _events!.Reader.WaitToReadAsync().AsTask();
        try
        {
            while (true)
            {
                await Task.WhenAny(ready, tick);
                if (tick.IsCompleted)
                {
                    if (!await tick) break;
                    Report(metrics);
                    tick = timer.WaitForNextTickAsync().AsTask();
                }
                if (!ready.IsCompleted) continue;
                if (!await ready) break;
                // Bound a batch so sustained producers cannot indefinitely postpone summaries.
                for (var i = 0; i < 256 && _events.Reader.TryRead(out var entry); i++)
                {
                    double? elapsed = entry.StartedAt is { } start ? Milliseconds(entry.At - start) : null;
                    if (!metrics.TryGetValue(entry.Stage, out var metric)) metrics[entry.Stage] = metric = new();
                    metric.Add(elapsed, entry.Value);
                    Write(new { type = "event", stage = entry.Stage.ToString(), unit = entry.Trace.Unit,
                        operation = entry.Trace.Operation, recognitionEvent = entry.Trace.RecognitionEvent, preview = entry.Trace.Preview,
                        emissionAtMs = entry.Trace.EmissionAt is { } emittedAt ? (double?)Milliseconds(emittedAt - _origin) : null,
                        atMs = Milliseconds(entry.At - _origin), elapsedMs = elapsed,
                        recognitionMs = entry.Trace.Unit == Guid.Empty ? (double?)null : Milliseconds(entry.At - entry.Trace.RecognitionAt),
                        firstRecognitionMs = entry.Trace.Unit == Guid.Empty ? (double?)null : Milliseconds(entry.At - entry.Trace.FirstRecognitionAt), value = entry.Value });
                }
                ready = _events.Reader.WaitToReadAsync().AsTask();
            }
        }
        finally { Report(metrics); }
    }

    private double Milliseconds(long ticks) => Math.Max(0, ticks * _millisecondsPerTick);
    private void Report(Dictionary<LatencyStage, Metric> metrics)
    {
        foreach (var (stage, metric) in metrics)
        {
            var sorted = metric.Samples.Take(metric.SampleCount).Order().ToArray();
            double? Percentile(double p) => sorted.Length == 0 ? null : sorted[(int)Math.Ceiling(sorted.Length * p) - 1];
            Write(new { type = "summary", stage = stage.ToString(), count = metric.Count, valueTotal = metric.ValueTotal,
                sampleCount = sorted.Length, p50Ms = Percentile(.50), p95Ms = Percentile(.95),
                maxMs = sorted.Length == 0 ? (double?)null : metric.Maximum });
        }
        Write(new { type = "health", recordingDrops = Interlocked.Read(ref _dropped), sinkFailures = Interlocked.Read(ref _sinkFailures) });
    }
    private void Write<T>(T payload)
    {
        try { _sink(JsonSerializer.Serialize(payload)); }
        catch { Interlocked.Increment(ref _sinkFailures); } // Diagnostics must never fail the pipeline.
    }

    public static void Initialize()
    {
        if (Current.Enabled) AppDomain.CurrentDomain.ProcessExit += (_, _) => Current.Dispose();
    }
    public Task CompleteAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0) _events?.Writer.TryComplete();
        return _worker;
    }
    public void Dispose()
    {
        // A stalled log sink must not hold process shutdown indefinitely. Tests can await CompleteAsync.
        try { CompleteAsync().Wait(TimeSpan.FromSeconds(1)); } catch { }
    }
}
