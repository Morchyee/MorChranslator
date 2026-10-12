using MorChranslator.Resources;
using MorChranslator.Utility;

internal static class BrandingTests
{
    public static void Register(Action<string, Func<Task>> test, Action<bool, string> check)
    {
        test("MorChranslator retains legacy data, single-instance and pipe identities", () =>
        {
            check(ProductIdentity.Name == "MorChranslator", "Product spelling");
            check(ProductIdentity.DataFolder == "Kikitan Translator", "Existing configuration and subtitle path");
            check(ProductIdentity.HostMutexPrefix == @"Local\DesktopTranslator.Main-", "Old/new host exclusion");
            check(ProductIdentity.SubtitleMutexPrefix == @"Local\DesktopTranslator.Subtitles-", "Old/new subtitle exclusion");
            check(ProductIdentity.SubtitlePipePrefix == "kikitan-desktop-subtitles-", "Subtitle wire transport");
            check(ProductIdentity.ControlPipePrefix == "kikitan-desktop-control-", "Tray command transport");
            check(CredentialProtection.Prefix == "dpapi:v1:", "Existing credential envelope");
            return Task.CompletedTask;
        });
        test("New diagnostics switch and legacy alias are opt-in without changing event semantics", () =>
        {
            var current = Environment.GetEnvironmentVariable(ProductIdentity.DiagnosticsSwitch);
            var legacy = Environment.GetEnvironmentVariable(ProductIdentity.LegacyDiagnosticsSwitch);
            try
            {
                Environment.SetEnvironmentVariable(ProductIdentity.DiagnosticsSwitch, null);
                Environment.SetEnvironmentVariable(ProductIdentity.LegacyDiagnosticsSwitch, null);
                check(!ProductIdentity.DiagnosticsEnabled, "Default off");
                Environment.SetEnvironmentVariable(ProductIdentity.DiagnosticsSwitch, "1");
                check(ProductIdentity.DiagnosticsEnabled, "New switch");
                Environment.SetEnvironmentVariable(ProductIdentity.DiagnosticsSwitch, "0");
                check(!ProductIdentity.DiagnosticsEnabled, "Only explicit 1 enables");
                Environment.SetEnvironmentVariable(ProductIdentity.LegacyDiagnosticsSwitch, "1");
                check(ProductIdentity.DiagnosticsEnabled, "Legacy alias");
            }
            finally
            {
                Environment.SetEnvironmentVariable(ProductIdentity.DiagnosticsSwitch, current);
                Environment.SetEnvironmentVariable(ProductIdentity.LegacyDiagnosticsSwitch, legacy);
            }
            return Task.CompletedTask;
        });
        test("Localized presentation keeps the brand exact and preserves upstream attribution", () =>
        {
            foreach (var locale in DesktopText.Locales)
            {
                check(DesktopText.Get(locale, "common.opening").Contains(ProductIdentity.Name), "Opening brand: " + locale);
                check(DesktopText.Get(locale, "general.autoStartHelp").Contains(ProductIdentity.Name), "Startup brand: " + locale);
                check(DesktopText.Get(locale, "about.originsHelp").Contains("Kikitan Translator"), "Original project credit: " + locale);
            }
            check(typeof(AppConfig).Assembly.GetName().Name == "MorChranslator.Base", "Core assembly name");
            check(typeof(DesktopText).Assembly.GetName().Name == "MorChranslator.Resources", "Resource assembly name");
            return Task.CompletedTask;
        });
    }
}
