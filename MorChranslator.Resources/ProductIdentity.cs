namespace MorChranslator.Resources;

/// <summary>Branding is independent of persisted data and transport identities.</summary>
public static class ProductIdentity
{
    public const string Name = "MorChranslator";
    // Retain the exact legacy identities: old and new versions share settings and cannot run together.
    public const string DataFolder = "Kikitan Translator";
    public const string HostMutexPrefix = @"Local\DesktopTranslator.Main-";
    public const string SubtitleMutexPrefix = @"Local\DesktopTranslator.Subtitles-";
    public const string SubtitlePipePrefix = "kikitan-desktop-subtitles-";
    public const string ControlPipePrefix = "kikitan-desktop-control-";
    public const string DiagnosticsSwitch = "MorChranslator_LATENCY_DIAGNOSTICS";
    public const string LegacyDiagnosticsSwitch = "KIKITAN_LATENCY_DIAGNOSTICS";
    public static bool DiagnosticsEnabled => Environment.GetEnvironmentVariable(DiagnosticsSwitch) == "1"
        || Environment.GetEnvironmentVariable(LegacyDiagnosticsSwitch) == "1";
}
