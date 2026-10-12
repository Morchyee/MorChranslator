using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using MorChranslator.Base;
using MorChranslator.Base.Outputs;
using MorChranslator.Base.Recognizers;
using MorChranslator.Base.Translators;
using MorChranslator.Capture;
using MorChranslator.Photino.Handlers;
using MorChranslator.Recognizers;
using MorChranslator.Utility;
using Newtonsoft.Json;
using Photino.NET;
using Serilog;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Backends.MiniAudio.Enums;
using SoundFlow.Structs;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace MorChranslator.Photino;

public class Mic
{
    [JsonProperty("name")] public string Name;
    [JsonProperty("default")] public bool Default;
}

public class AppState
{
    [JsonProperty("microphones")] public Mic[] Microphones;
    [JsonProperty("config")] public ConfigObject Config;
    [JsonProperty("status")] public int Status;
    [JsonProperty("app_version")] public string AppVersion = "Developer Build";
    [JsonProperty("server_version")] public string ServerVersion;
    [JsonProperty("is_linux")] public bool IsLinux;
    [JsonProperty("is_appimage")] public bool IsAppimage;
    [JsonProperty("is_muted")] public bool IsMuted;
}

public class RecognitionData
{
    [JsonProperty("transcription")] public string Transcription;
    [JsonProperty("translation")] public string Translation;
    [JsonProperty("final")] public bool Final;
}

public class Manager : IDisposable
{
    private TranslationPipeline? _microphonePipeline;
    private TranslationPipeline? _desktopPipeline;

    private SystemLoopback _loopback;
    private Microphone? _mic;
    private readonly string _modelPath;
    private AppState _appState = new () { Microphones = [] };
    private IErrorHandler _errorHandler;
    private bool _running;
    private readonly object _lifecycleLock = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task _deviceMonitor;
    private bool _disposed;
    private string? _playbackDeviceId;
    private readonly OnConfigUpdate _configUpdated;

    private readonly SubtitleWriter _subtitleWriter = new();
    private Process? _subtitleProcess;
    private readonly bool _noUI;
    private OscWatcher? _oscWatcher;

    private Connector _connector;

    public DeviceInfo[] GetMicrophones() => GetMicrophone().GetCaptureDevices();
    private Microphone GetMicrophone() => _mic ??= new Microphone(_modelPath, _errorHandler);

    public Manager(bool noUI, Connector connector)
    {
        _noUI = noUI;
        _appState.Config = AppConfig.ConfigObject;

        _errorHandler = new ErrorHandler(connector, error => _subtitleWriter.Error(error));
        
        #if DEBUG
        _loopback = new(Path.Combine(AppContext.BaseDirectory, "wwwroot", "silero_vad.onnx"));
        _modelPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "silero_vad.onnx");
        #else
        _appState.AppVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split("+")[0];
        _loopback = new(Path.Combine(AppContext.BaseDirectory, "wwwroot", "silero_vad.onnx"));
        _modelPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "silero_vad.onnx");
        #endif
        
        _appState.IsLinux = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        _appState.IsAppimage = _appState.IsLinux && Environment.GetEnvironmentVariable("MorChranslator_NOT_APPIMAGE") == null;
        _connector = connector;

        _configUpdated = () =>
        {
            _appState.Config = AppConfig.ConfigObject;
            SendUpdateToUI();
        };
        AppConfig.OnUpdate += _configUpdated;
        _deviceMonitor = Task.Run(MonitorDevicesAsync);
    }

    private async Task MonitorDevicesAsync()
    {
        MiniAudioEngine? engine = null;
        try
        {
            while (!_lifetime.IsCancellationRequested)
            {
                await Task.Delay(2000, _lifetime.Token);
                try
                {
                    if (AppConfig.ConfigObject.DesktopTranslation && OperatingSystem.IsWindows())
                    {
                        lock (_lifecycleLock)
                        {
                            if (_disposed) return;
                            if (!_noUI && (_subtitleProcess == null || _subtitleProcess.HasExited)) StartSubtitleWindow();
                            if (_running)
                            {
                                using var devices = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                                using var device = devices.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia);
                                if (_playbackDeviceId != device.ID || (_appState.Status == 2 && !_loopback.IsRunning))
                                {
                                    Log.Information("[AUDIO] Playback device changed or capture stopped; restarting capture pipeline");
                                    RestartCore();
                                }
                            }
                        }
                        continue;
                    }
                    engine ??= new MiniAudioEngine(backendPriority: [MiniAudioBackend.Wasapi, MiniAudioBackend.Oss]);
                    engine.UpdateAudioDevicesInfo();
                    var mics = engine.CaptureDevices.Select(d => new Mic { Name = d.Name, Default = d.IsDefault }).ToArray();
                    if (!_appState.Microphones.Select(m => m.Name).SequenceEqual(mics.Select(m => m.Name)))
                    {
                        _appState.Microphones = mics;
                        SendUpdateToUI();
                    }
                    if (_appState.Config.Microphone.Length > 0 && mics.Length > 0 && !mics.Any(m => m.Name == _appState.Config.Microphone))
                    {
                        AppConfig.ConfigObject.Microphone = (mics.FirstOrDefault(m => m.Default) ?? mics[0]).Name;
                        SendMicChanged();
                        RestartIfRunning();
                    }
                }
                catch (Exception e) { Log.Warning("[AUDIO] Device recovery failed: {ErrorType}. Retry available from tray.", e.GetType().Name); }
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        finally { engine?.Dispose(); }
    }

    public void Start()
    {
        lock (_lifecycleLock)
        {
            if (_disposed) return;
            if (_running)
            {
                if (_appState.Status == 0) RestartCore(); // Recover a pipeline whose recognizer failed after startup.
                return;
            }

            StartCore();
        }
    }

    private void StartCore()
    {
        Log.Information($"[APP] Start requested: running={_running}, recognizer={AppConfig.ConfigObject.Recognizer}, translator={AppConfig.ConfigObject.Translator}, desktop={AppConfig.ConfigObject.DesktopTranslation}, chatbox={AppConfig.ConfigObject.SendToChatbox}");
        if (AppConfig.LoadError != null) { _errorHandler.OnError(AppConfig.LoadError); return; }
        var desktopMode = AppConfig.ConfigObject.DesktopTranslation && !_appState.IsLinux;

        if (desktopMode)
        {
            try
            {
                StartDesktopCore();
            }
            catch (Exception e)
            {
                Log.Error("[APP] Desktop pipeline failed to start: {ErrorType}", e.GetType().Name);
                _desktopPipeline?.Dispose();
                _desktopPipeline = null;
                _errorHandler.OnError(e is InvalidOperationException ? e.Message : "Desktop pipeline could not start. Check the playback device, speech credentials and network.");
            }

            _running = _desktopPipeline != null;
            Log.Information("[APP] Startup complete.");
            SendUpdateToUI();
            return;
        }

        _oscWatcher = new OscWatcher();
        _oscWatcher.MuteStatusChanged += muted =>
        {
            _appState.IsMuted = muted;
            SendUpdateToUI();
        };
        _oscWatcher.Start();

        IRecognizer rMic;
        if (AppConfig.ConfigObject.Recognizer == 0) rMic = new Bing(GetMicrophone());
        else if (AppConfig.ConfigObject.Recognizer == 1) rMic = new GroqRecognizer(GetMicrophone());
        else rMic = new Gemini(GetMicrophone());

        _microphonePipeline = new TranslationPipeline(rMic, CreateTranslator(), _errorHandler, false);
        _microphonePipeline.AddOutput(new Custom(SendRecognitionData, false));
        if (AppConfig.ConfigObject.SendToChatbox)
        {
            var chatbox = new Chatbox();
            
            _microphonePipeline.AddOutput(new Custom((r, t, f) =>
            {
                if (AppConfig.ConfigObject.DisableWhenMuted && _appState.IsMuted)
                {
                    if (f) Log.Debug("[OSC]  Final chatbox output skipped: VRChat microphone is muted");
                    return;
                }
                
                chatbox.Send(r, t, f);
            }, true));
        }
        
        if (AppConfig.ConfigObject.SendUserData)
        {
            _microphonePipeline.AddOutput(new OSC("/microphone"));
        }
        
        _microphonePipeline.OnRecognizerStatusChanged += s =>
        {
            _appState.Status = (int)s;
            
            SendUpdateToUI();
        };
        
        try
        {
            _microphonePipeline.Start();
            if (rMic.Status() == RecognizerStatus.NotStarted)
            {
                _microphonePipeline.Dispose();
                _microphonePipeline = null;
            }

            _running = _microphonePipeline != null;
            if (!_running)
            {
                _oscWatcher.Dispose();
                _oscWatcher = null;
            }
            Log.Information($"[APP] Startup complete.");
            SendUpdateToUI();
        }
        catch
        {
            StopCore();
            throw;
        }
    }

    private void StartDesktopCore()
    {
        if (!Languages.SourceLanguages.ContainsKey(AppConfig.ConfigObject.SourceLanguage) ||
            !Languages.TargetLanguages.ContainsKey(AppConfig.ConfigObject.TargetLanguage))
            throw new InvalidOperationException("Select supported source and target languages in Settings.");
        using (var devices = new NAudio.CoreAudioApi.MMDeviceEnumerator())
        using (var device = devices.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia))
            _playbackDeviceId = device.ID;
        var provider = AppConfig.ConfigObject.DesktopTranslationProvider;
        var key = provider switch
        {
            "google" => AppConfig.ConfigObject.GoogleCloudApiKey,
            "deepl" => AppConfig.ConfigObject.DeepLApiKey,
            "groq" => AppConfig.ConfigObject.GroqApiKey,
            _ => throw new InvalidOperationException($"Unknown desktop translation provider: {provider}")
        };
        if (!AppConfig.ConfigObject.SpeechToTextOnly && string.IsNullOrWhiteSpace(key))
            Log.Warning("[SUBTITLE] {Provider} API key is not configured; recognized text will still be shown", provider);

        if (!_noUI) StartSubtitleWindow();

        IRecognizer rDesktop;
        if (AppConfig.ConfigObject.Recognizer == 0) rDesktop = new Bing(_loopback);
        else if (AppConfig.ConfigObject.Recognizer == 1) rDesktop = new GroqRecognizer(_loopback);
        else rDesktop = new Gemini(_loopback);

        _desktopPipeline = new TranslationPipeline(rDesktop, CreateTranslationProvider(provider), _errorHandler, true);
        Log.Information($"[APP] Desktop pipeline starting: recognizer={rDesktop.GetType().Name}, subtitleWindow={_subtitleProcess != null}");
        _desktopPipeline.OnRecognizerStatusChanged += s =>
        {
            _appState.Status = (int)s;
            SendUpdateToUI();
        };
        if (!_noUI)
        {
            _desktopPipeline.OnSubtitle += (id, recognized, translated, final, update) =>
            {
                if (string.IsNullOrWhiteSpace(recognized)) return;
                _subtitleWriter.Write(new DesktopSubtitleResult(recognized, translated, final, id, update));
            };
        }

        _desktopPipeline.Start();
        if (rDesktop.Status() == RecognizerStatus.NotStarted)
        {
            _desktopPipeline.Dispose();
            _desktopPipeline = null;
        }
    }

    public void StartSubtitleWindow()
    {
        lock (_lifecycleLock)
        {
            if (_disposed || _subtitleProcess is { HasExited: false }) return;
            var path = Path.Combine(AppContext.BaseDirectory, "MorChranslator.Subtitles.exe");
            if (!File.Exists(path)) throw new InvalidOperationException("Subtitle executable is missing. Extract the complete release folder.");
            var startInfo = new ProcessStartInfo(path) { UseShellExecute = false };
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add(AppConfig.ConfigObject.UiLanguage);
            _subtitleProcess?.Dispose();
            _subtitleProcess = Process.Start(startInfo);
        }
    }

    public void Stop()
    {
        lock (_lifecycleLock) StopCore();
    }

    public void RestartIfRunning()
    {
        lock (_lifecycleLock)
        {
            if (!_disposed && _running) RestartCore();
        }
    }

    private void RestartCore()
    {
        Log.Information("[APP] Restart requested");
        _appState.Status = 1;
        StopCore();
        StartCore();
    }

    private void StopCore()
    {
        Log.Information("[APP] Stopping...");
        _running = false;
        var desktop = _desktopPipeline;
        var microphone = _microphonePipeline;
        _desktopPipeline = null;
        _microphonePipeline = null;
        foreach (var component in new IDisposable?[] { microphone, desktop, _oscWatcher })
        {
            try { component?.Dispose(); }
            catch (Exception e) { Log.Warning("[APP] Cleanup failed: {ErrorType}", e.GetType().Name); }
        }
        _oscWatcher = null;
        _appState.Status = 0;
        SendUpdateToUI();
    }

    private static ITranslator CreateTranslator() => AppConfig.ConfigObject.Translator switch
    {
        0 => new GoogleCloudTranslator(),
        1 => new GroqTranslator(),
        _ => new GeminiStub()
    };

    private static ITranslator CreateTranslationProvider(string provider) => provider switch
    {
        "google" => new GoogleCloudTranslator(),
        "deepl" => new DeepLTranslator(),
        "groq" => new GroqTranslator(),
        _ => throw new InvalidOperationException($"Unknown desktop translation provider: {provider}")
    };

    public void ManualTranslate(string text) => _microphonePipeline?.ManualTranslate(text);

    public void SendUpdateToUI()
    {
        _subtitleWriter.Status(_appState.Status switch { 2 => "Listening", 1 => "Connecting", _ => "Stopped" });
        var state = Newtonsoft.Json.Linq.JObject.FromObject(_appState);
        state["config"] = AppConfig.PublicConfig();
        state["configuration_error"] = AppConfig.LoadError;
        _connector.Send(JsonConvert.SerializeObject(new Message { Method = "state", Data = state.ToString(Formatting.None) }));
    }

    public void ShowSubtitles()
    {
        lock (_lifecycleLock) { if (!_disposed) StartSubtitleWindow(); }
        _subtitleWriter.Command("show");
    }
    public void ShowSubtitleAppearance()
    {
        lock (_lifecycleLock) { if (!_disposed) StartSubtitleWindow(); }
        _subtitleWriter.Command("appearance");
    }
    public void Dispose()
    {
        lock (_lifecycleLock)
        {
            if (_disposed) return;
            _disposed = true;
            _lifetime.Cancel();
            AppConfig.OnUpdate -= _configUpdated;
            StopCore();
        }
        _deviceMonitor.GetAwaiter().GetResult();
        _loopback.Stop(); _mic?.Dispose();
        _subtitleWriter.Command("close");
        if (_subtitleProcess != null)
        {
            try { if (!_subtitleProcess.HasExited && !_subtitleProcess.WaitForExit(1500)) _subtitleProcess.Kill(); }
            catch (InvalidOperationException) { }
            _subtitleProcess.Dispose();
        }
        _subtitleWriter.Dispose();
        _lifetime.Dispose();
    }

    private void SendRecognitionData(string recognized, string translated, bool final)
    {
        _connector.Send(
            JsonConvert.SerializeObject(new Message
            {
                Method = "recognition",
                Data = JsonConvert.SerializeObject(new RecognitionData { Transcription = recognized, Translation = translated, Final = final })
            }));
    }

    private void SendMicChanged()
    {
        _connector.Send(
            JsonConvert.SerializeObject(new Message
            {
                Method = "mic_changed",
                Data = ""
            }));
    }
        
}
