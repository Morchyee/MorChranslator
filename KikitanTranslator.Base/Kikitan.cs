using System.Threading.Channels;
using KikitanTranslator.Base.Diagnostics;
using KikitanTranslator.Base.Outputs;
using KikitanTranslator.Base.Translators;
using KikitanTranslator.Recognizers;
using KikitanTranslator.Utility;
using Serilog;

namespace KikitanTranslator.Base;

public sealed class Kikitan : IDisposable
{
    private readonly IRecognizer _recognizer;
    private readonly ITranslator _translator;
    private readonly IErrorHandler _errorHandler;
    private readonly List<IOutput> _outputs = [];
    private readonly bool _isLoopback;
    private readonly object _recognitionLock = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Channel<PendingTranslation> _translations;
    private readonly SemaphoreSlim _translationAvailable = new(0, 1);
    private readonly StreamingTranslationPreview _previewPolicy = new();
    private PendingTranslation? _pendingPreview;
    private PendingTranslation? _completedPreview;
    private CancellationTokenSource? _activePreview;
    private string _activePreviewText = "", _currentSource = "", _translatedPrefix = "", _previewTranslation = "";
    private readonly Channel<(string Source, string Translation)> _delayed = Channel.CreateBounded<(string, string)>(32);
    private Task? _translationWorker;
    private Task? _outputWorker;
    private volatile bool _running;
    private bool _disposed;
    private bool _started;
    private Guid _utteranceId = Guid.NewGuid();
    private DateTime _lastFailure;
    private readonly LatencyDiagnostics _diagnostics;
    private LatencyTrace _recognitionTrace, _activePreviewTrace;
    private long _firstRecognitionAt, _lastPartialAt;
    private int _partialCount;
    private bool _hasRecognition;
    private volatile bool _previewCancelRequested;

    private sealed record PendingTranslation(Guid Id, string Text, string Source, string Target, bool TranscriptionOnly,
        long EnqueuedAt, bool Preview = false, string? CachedTranslation = null,
        LatencyTrace Trace = default, long DiagnosticQueuedAt = 0);
    public event OnRecognizerStatus? OnRecognizerStatusChanged;
    public event Action<Guid, string, string, bool, bool>? OnSubtitle;

    public Kikitan(IRecognizer recognizer, ITranslator translator, IErrorHandler errorHandler, bool loopback,
        LatencyDiagnostics? diagnostics = null)
    {
        _recognizer = recognizer;
        _translator = translator;
        _errorHandler = errorHandler;
        _isLoopback = loopback;
        _diagnostics = diagnostics ?? LatencyDiagnostics.Current;
        // Desktop captions favor recent speech during provider stalls. Legacy chat output retains FIFO behavior.
        _translations = Channel.CreateBounded<PendingTranslation>(new BoundedChannelOptions(loopback ? 3 : 32)
        {
            FullMode = loopback ? BoundedChannelFullMode.DropOldest : BoundedChannelFullMode.Wait,
            SingleReader = true
        }, dropped => dropped.Trace.Record(LatencyStage.TranslationQueueDropped));
        recognizer.OnRecognitionReceived += OnRecognition;
        recognizer.OnRecognizerStatusChanged += OnRecognizerStatus;
    }
    public void AddOutput(IOutput output) => _outputs.Add(output);
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started) return; // A stopped pipeline is disposed and replaced by Manager.
        _started = true;
        _running = true;
        _translationWorker = TranslateQueuedAsync(_lifetime.Token);
        _outputWorker = SendDelayedAsync(_lifetime.Token);
        _recognizer.Start(AppConfig.ConfigObject.SourceLanguage, _errorHandler);
    }
    private void OnRecognition(string text, bool final)
    {
        var receivedAt = _diagnostics.Now;
        lock (_recognitionLock)
        {
            if (!_running || string.IsNullOrWhiteSpace(text)) return;
            var id = _utteranceId;
            if (_diagnostics.Enabled)
            {
                if (!_hasRecognition) { _firstRecognitionAt = receivedAt; _hasRecognition = true; }
                _recognitionTrace = _diagnostics.Recognition(id, receivedAt, _firstRecognitionAt);
                if (final)
                {
                    _recognitionTrace.Record(LatencyStage.RecognitionFinal, _firstRecognitionAt);
                    _recognitionTrace.Record(LatencyStage.RecognitionPartialCount, value: _partialCount);
                }
                else
                {
                    _recognitionTrace.Record(_partialCount == 0 ? LatencyStage.RecognitionFirstPartial : LatencyStage.RecognitionPartial);
                    if (_partialCount > 0 && AppConfig.ConfigObject.Recognizer != 2 &&
                        !text.AsSpan().Trim().SequenceEqual(_currentSource.AsSpan()))
                        _recognitionTrace.Record(LatencyStage.RecognitionRevision);
                    if (_partialCount > 0) _recognitionTrace.Record(LatencyStage.RecognitionPartialGap, _lastPartialAt);
                    _lastPartialAt = receivedAt; _partialCount++;
                }
            }
            if (AppConfig.ConfigObject.Recognizer == 2)
            {
                var parts = text.Split('|', 2);
                if (parts.Length > 1) _recognitionTrace.Record(LatencyStage.CombinedTranslationEmitted);
                Publish(id, parts[0], parts.Length > 1 ? parts[1] : "", final, trace: _recognitionTrace);
            }
            else
            {
                _currentSource = text.Trim();
                if (_activePreview != null && (final || !StreamingTranslationPreview.IsCompatible(_currentSource, _activePreviewText)))
                {
                    _previewCancelRequested = true;
                    _activePreviewTrace.Record(final ? LatencyStage.PreviewCancelForFinal : LatencyStage.PreviewCancelForRevision);
                    _activePreview.Cancel();
                }
                if (_translatedPrefix.Length > 0 && (!StreamingTranslationPreview.IsCompatible(_currentSource, _translatedPrefix) ||
                    _completedPreview?.Source != AppConfig.ConfigObject.SourceLanguage ||
                    _completedPreview?.Target != AppConfig.ConfigObject.TargetLanguage || AppConfig.ConfigObject.SpeechToTextOnly))
                    _translatedPrefix = _previewTranslation = "";
                // Commit source immediately; translation later updates exactly this ID.
                EmitSubtitle(id, text, _previewTranslation, final, false, _recognitionTrace);
                foreach (var output in _outputs.Where(o => !o.IsDelayed())) SendOutput(output, text, "", false);
                if (final)
                {
                    _pendingPreview?.Trace.Record(LatencyStage.PreviewDiscardedForFinal);
                    _pendingPreview = null;
                    var cached = _currentSource == _translatedPrefix ? _previewTranslation : null;
                    var item = CreateTranslation(id, text, false, cached);
                    item.Trace.Record(LatencyStage.FinalQueued);
                    if (!_translations.Writer.TryWrite(item))
                    {
                        item.Trace.Record(LatencyStage.TranslationQueueDropped);
                        ReportFailure("Translation queue is full. Original subtitles are retained.");
                    }
                    else SignalTranslation();
                }
                else if (_isLoopback && !AppConfig.ConfigObject.SpeechToTextOnly)
                {
                    var preview = _previewPolicy.Observe(text);
                    if (preview != null)
                    {
                        // One replaceable slot, separate from the final-result queue.
                        _pendingPreview?.Trace.Record(LatencyStage.PreviewCoalesced);
                        _pendingPreview = CreateTranslation(id, preview, true);
                        _pendingPreview.Trace.Record(LatencyStage.PreviewCandidate);
                        _pendingPreview.Trace.Record(LatencyStage.PreviewScheduled);
                        SignalTranslation();
                    }
                }
            }
            if (final)
            {
                _utteranceId = Guid.NewGuid();
                _previewPolicy.Reset();
                _completedPreview = null;
                _translatedPrefix = _previewTranslation = _currentSource = "";
                _hasRecognition = false; _partialCount = 0;
            }
        }
    }

    private PendingTranslation CreateTranslation(Guid id, string text, bool preview, string? cached = null) =>
        new(id, text, AppConfig.ConfigObject.SourceLanguage, AppConfig.ConfigObject.TargetLanguage,
            AppConfig.ConfigObject.SpeechToTextOnly, System.Diagnostics.Stopwatch.GetTimestamp(), preview, cached,
            _diagnostics.Request(_recognitionTrace, preview), _diagnostics.Now);

    // Producers and candidate access are serialized by _recognitionLock. A single
    // wake-up suffices because the worker drains final work before the preview slot.
    private void SignalTranslation()
    {
        if (_translationAvailable.CurrentCount == 0) _translationAvailable.Release();
    }
    private bool TryTakeTranslation(out PendingTranslation item)
    {
        lock (_recognitionLock)
        {
            if (_translations.Reader.TryRead(out var final)) { item = final; return true; }
            if (_pendingPreview != null) { item = _pendingPreview; _pendingPreview = null; return true; }
            item = null!;
            return false;
        }
    }
    private bool IsCurrentPreview(PendingTranslation item) => _running && item.Id == _utteranceId &&
        item.Source == AppConfig.ConfigObject.SourceLanguage && item.Target == AppConfig.ConfigObject.TargetLanguage &&
        !AppConfig.ConfigObject.SpeechToTextOnly &&
        StreamingTranslationPreview.IsCompatible(_currentSource, item.Text);

    private async Task TranslateQueuedAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await _translationAvailable.WaitAsync(token);
                while (TryTakeTranslation(out var item))
                {
                    item.Trace.Record(LatencyStage.TranslationQueueWait, item.DiagnosticQueuedAt);
                    if (token.IsCancellationRequested) { item.Trace.Record(LatencyStage.TranslationCancelled); break; }
                    // Never spend another provider request on desktop speech that is already stale.
                    if (_isLoopback && System.Diagnostics.Stopwatch.GetElapsedTime(item.EnqueuedAt) > TimeSpan.FromSeconds(10))
                    {
                        item.Trace.Record(LatencyStage.TranslationExpired, item.DiagnosticQueuedAt);
                        continue;
                    }
                    await TranslateOneAsync(item, token);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    private async Task TranslateOneAsync(PendingTranslation item, CancellationToken token)
    {
        string translated = "";
        CancellationTokenSource? previewBudget = null;
        var startedAt = item.Trace.Now;
        using var diagnosticScope = _diagnostics.Enter(item.Trace);
        try
        {
            if (item.Preview)
            {
                lock (_recognitionLock)
                {
                    if (!IsCurrentPreview(item)) { item.Trace.Record(LatencyStage.PreviewStale); return; }
                    previewBudget = CancellationTokenSource.CreateLinkedTokenSource(token);
                    previewBudget.CancelAfter(TimeSpan.FromSeconds(2));
                    _activePreview = previewBudget;
                    _activePreviewText = item.Text;
                    _activePreviewTrace = item.Trace; _previewCancelRequested = false;
                }
            }
            var requestToken = previewBudget?.Token ?? token;
            item.Trace.Record(item.Preview ? LatencyStage.PreviewStarted : LatencyStage.FinalStarted);
            if (item.TranscriptionOnly) item.Trace.Record(LatencyStage.TranslationBypassed);
            else if (item.CachedTranslation != null) item.Trace.Record(LatencyStage.TranslationCacheHit);
            if (!item.TranscriptionOnly)
                translated = item.CachedTranslation ?? await _translator.TranslateAsync(item.Text, item.Source, item.Target, requestToken) ?? "";
            if (!item.TranscriptionOnly && string.IsNullOrWhiteSpace(translated))
            {
                item.Trace.Record(item.Preview ? LatencyStage.PreviewFailed : LatencyStage.FinalFailed, startedAt);
                ReportFailure("Translation provider returned no text. Original subtitles are retained.");
            }
            else item.Trace.Record(item.Preview ? LatencyStage.PreviewCompleted : LatencyStage.FinalCompleted, startedAt);
            lock (_recognitionLock)
            {
                if (!_running) { item.Trace.Record(LatencyStage.TranslationCancelled); return; }
                if (item.Preview)
                {
                    if (previewBudget!.IsCancellationRequested || !IsCurrentPreview(item) || string.IsNullOrWhiteSpace(translated))
                    {
                        item.Trace.Record(LatencyStage.PreviewStale);
                        return;
                    }
                    _translatedPrefix = item.Text;
                    _previewTranslation = translated;
                    _completedPreview = item;
                    // Keep the newest original text; a preview may cover only
                    // its stable leading sentences. Final STT remains authoritative.
                    EmitSubtitle(item.Id, _currentSource, translated, false, true, item.Trace);
                }
                else Publish(item.Id, item.Text, translated, true, true, item.Trace);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        { item.Trace.Record(LatencyStage.TranslationCancelled, startedAt); throw; }
        catch (OperationCanceledException) when (previewBudget?.IsCancellationRequested == true)
        { item.Trace.Record(_previewCancelRequested ? LatencyStage.PreviewCancelled : LatencyStage.PreviewTimeout, startedAt); }
        catch (Exception e)
        {
            item.Trace.Record(e is OperationCanceledException ? LatencyStage.TranslationTimeout :
                item.Preview ? LatencyStage.PreviewFailed : LatencyStage.FinalFailed, startedAt);
            // Do not log provider exceptions: some include request URLs or credentials.
            Log.Warning("[TRANSLATION] Failed with {ErrorType}", e.GetType().Name);
            ReportFailure(e is InvalidOperationException ? e.Message :
                "Translation failed or timed out. Check provider credentials, languages and network. Original subtitles are retained.");
            if (!item.Preview)
                lock (_recognitionLock) { if (_running) Publish(item.Id, item.Text, "", true, true, item.Trace); }
        }
        finally
        {
            if (previewBudget != null)
            {
                lock (_recognitionLock) { if (ReferenceEquals(_activePreview, previewBudget)) _activePreview = null; }
                previewBudget.Dispose();
            }
        }
    }
    private void EmitSubtitle(Guid id, string text, string translated, bool final, bool translationUpdate, LatencyTrace trace)
    {
        trace = trace with { EmissionAt = trace.Now };
        using var scope = _diagnostics.Enter(trace);
        trace.Record(translationUpdate ? LatencyStage.TranslationEmitted : LatencyStage.OriginalEmitted);
        if (translationUpdate && !string.IsNullOrWhiteSpace(translated))
        {
            trace.Record(LatencyStage.RecognitionToTranslation, trace.RecognitionAt);
            trace.Record(LatencyStage.FirstRecognitionToTranslation, trace.FirstRecognitionAt);
        }
        OnSubtitle?.Invoke(id, text, translated, final, translationUpdate);
    }
    private void Publish(Guid id, string text, string translated, bool final, bool translationUpdate = false, LatencyTrace trace = default)
    {
        EmitSubtitle(id, text, translated, final, translationUpdate, trace);
        foreach (var output in _outputs.Where(o => !o.IsDelayed())) SendOutput(output, text, translated, final);
        if (final && _outputs.Any(o => o.IsDelayed()) && !_delayed.Writer.TryWrite((text, translated)))
            ReportFailure("Delayed output queue is full.");
    }
    private static void SendOutput(IOutput output, string source, string translation, bool final)
    {
        try { output.Send(source, translation, final); }
        catch (Exception e) { Log.Warning("[OUTPUT] Delivery failed: {ErrorType}", e.GetType().Name); }
    }
    private void ReportFailure(string message)
    {
        if (DateTime.UtcNow - _lastFailure < TimeSpan.FromSeconds(15)) return;
        _lastFailure = DateTime.UtcNow;
        _errorHandler.OnError(message);
    }
    private async Task SendDelayedAsync(CancellationToken token)
    {
        try
        {
            await foreach (var item in _delayed.Reader.ReadAllAsync(token))
            {
                if (token.IsCancellationRequested) break;
                foreach (var output in _outputs.Where(o => o.IsDelayed())) SendOutput(output, item.Source, item.Translation, true);
                var length = Math.Max(item.Source.Length, item.Translation.Length);
                await Task.Delay((int)Math.Clamp((long)length * AppConfig.ConfigObject.ChatboxWaitPerCharMs, 0, 60000), token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception e) { Log.Warning("[OUTPUT] Failed with {ErrorType}", e.GetType().Name); }
    }
    private void OnRecognizerStatus(RecognizerStatus status) => OnRecognizerStatusChanged?.Invoke(status);
    public void ManualTranslate(string text) => OnRecognition(text, true);
    public void Stop()
    {
        _running = false;
        _lifetime.Cancel(); // Cancel HTTP before waiting for recognizer callbacks.
        _recognizer.Stop();
        Task.WhenAll(_translationWorker ?? Task.CompletedTask, _outputWorker ?? Task.CompletedTask).GetAwaiter().GetResult();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _running = false;
        _lifetime.Cancel();
        _recognizer.OnRecognitionReceived -= OnRecognition;
        _recognizer.OnRecognizerStatusChanged -= OnRecognizerStatus;
        try { _recognizer.Dispose(); }
        finally
        {
            Task.WhenAll(_translationWorker ?? Task.CompletedTask, _outputWorker ?? Task.CompletedTask).GetAwaiter().GetResult();
            lock (_recognitionLock) { }
            _translator.Dispose();
            _translationAvailable.Dispose();
            _lifetime.Dispose();
        }
    }
}
