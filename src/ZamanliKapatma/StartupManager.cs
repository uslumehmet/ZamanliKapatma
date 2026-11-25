using Microsoft.Win32;

namespace ZamanliKapatma;

/// <summary>"Windows açılışında başlat" ayarını kullanıcının Run kayıt anahtarıyla yönetir.</summary>
internal static class StartupManager
{
    /// <summary>Otomatik başlatmada pencere açılmadan doğrudan tepsiye gitmek için.</summary>
    public const string TrayArgument = "--tray";

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ZamanliKapatma";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(ValueName) is string;
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
        {
            key.SetValue(ValueName, BuildCommand());
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    /// <summary>Uygulama başka klasöre taşındıysa kayıttaki yolu günceller.</summary>
    public static void RefreshPathIfEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key?.GetValue(ValueName) is not string current)
        {
            return;
        }

        string command = BuildCommand();
        if (!string.Equals(current, command, StringComparison.OrdinalIgnoreCase))
        {
            key.SetValue(ValueName, command);
        }
    }

    private static string BuildCommand()
    {
        string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
        return $"\"{exePath}\" {TrayArgument}";
    }
}
