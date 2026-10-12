using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using MorChranslator.Base.Diagnostics;
using Serilog;

namespace MorChranslator.Photino;

public sealed class SubtitleWriter : IDisposable
{
    private readonly Channel<(DesktopSubtitleResult Result, LatencyTrace Trace, long QueuedAt)> _messages;
    private readonly Func<DesktopSubtitleResult, CancellationToken, Task> _send;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task _worker;
    public SubtitleWriter() : this(128, SendAsync) { }
    internal SubtitleWriter(int capacity, Func<DesktopSubtitleResult, CancellationToken, Task> send)
    {
        _messages = Channel.CreateBounded<(DesktopSubtitleResult, LatencyTrace, long)>(capacity);
        _send = send;
        _worker = Task.Run(WriteAsync);
    }
    public void Write(DesktopSubtitleResult result)
    {
        var trace = LatencyDiagnostics.Context;
        if (trace.Owner == null && LatencyDiagnostics.Current.Enabled)
            trace = new(LatencyDiagnostics.Current, Guid.Empty, 0, 0, 0);
        var queuedAt = trace.Now;
        if (_messages.Writer.TryWrite((result, trace, queuedAt))) trace.Record(LatencyStage.IpcQueued);
        else
        {
            trace.Record(LatencyStage.IpcQueueDropped);
            if (result.IsFinal) Log.Warning("[SUBTITLE] IPC queue full; subtitle delivery delayed or unavailable");
        }
    }
    public void Command(string command) => Write(new("", "", false, Guid.Empty, Command: command));
    public void Status(string state) => Write(new("", "", false, Guid.Empty, State: state, UiLanguage: MorChranslator.Utility.AppConfig.ConfigObject.UiLanguage));
    public void Error(string error) => Write(new("", "", false, Guid.Empty, Error: error));
    private async Task WriteAsync()
    {
        try
        {
            await foreach (var (result, trace, queuedAt) in _messages.Reader.ReadAllAsync(_lifetime.Token))
            {
                var startedAt = trace.Now;
                trace.Record(LatencyStage.IpcQueueWait, queuedAt);
                try
                {
                    await _send(result, _lifetime.Token);
                    trace.Record(LatencyStage.IpcWriteCompleted, startedAt);
                    if (trace.Unit != Guid.Empty)
                    {
                        if (trace.EmissionAt is { } emittedAt) trace.Record(LatencyStage.IpcEmissionToWrite, emittedAt);
                        trace.Record(result.IsTranslationUpdate ? LatencyStage.TranslatedRecognitionToIpcWrite :
                            LatencyStage.OriginalRecognitionToIpcWrite, trace.RecognitionAt);
                        trace.Record(LatencyStage.FirstRecognitionToIpcWrite, trace.FirstRecognitionAt);
                    }
                }
                catch (Exception e) when (e is TimeoutException or IOException)
                {
                    trace.Record(e is TimeoutException ? LatencyStage.IpcWriteTimeout : LatencyStage.IpcWriteFailed, startedAt);
                    if (result.IsFinal) Log.Warning("[SUBTITLE] Subtitle window is unavailable");
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                { trace.Record(LatencyStage.IpcCancelled, startedAt); throw; }
                catch
                { trace.Record(LatencyStage.IpcWriteFailed, startedAt); throw; } // Preserve existing worker-fault behavior.
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
    }
    private static async Task SendAsync(DesktopSubtitleResult result, CancellationToken token)
    {
        await using var pipe = new NamedPipeClientStream(".", $"kikitan-desktop-subtitles-{Environment.ProcessId}",
            PipeDirection.Out, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(500, token);
        await using var writer = new StreamWriter(pipe, new UTF8Encoding(false));
        await writer.WriteLineAsync(JsonSerializer.Serialize(result).AsMemory(), token);
        // Returning from this method includes DisposeAsync/flush of both writer and pipe.
    }
    public void Dispose()
    {
        _messages.Writer.TryComplete();
        _lifetime.Cancel();
        _worker.GetAwaiter().GetResult();
        _lifetime.Dispose();
    }
}
