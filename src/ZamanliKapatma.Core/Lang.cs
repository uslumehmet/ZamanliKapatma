using System.Globalization;

namespace ZamanliKapatma.Core;

/// <summary>Arayüz dili.</summary>
public enum AppLanguage
{
    Turkish,
    English,
}

/// <summary>
/// Basit iki dilli metin seçici. Her metin kodda Türkçe ve İngilizce hâliyle yan yana durur:
/// <c>Lang.T("Ayarlar", "Settings")</c>.
/// </summary>
public static class Lang
{
    private static readonly AsyncLocal<AppLanguage?> Scoped = new();
    private static AppLanguage _current = AppLanguage.Turkish;

    public static AppLanguage Current
    {
        get => Scoped.Value ?? _current;
        set => _current = value;
    }

    public static bool IsEnglish => Current == AppLanguage.English;

    public static string T(string turkish, string english) => IsEnglish ? english : turkish;

    /// <summary>Windows Türkçe ise Türkçe, değilse İngilizce.</summary>
    public static AppLanguage Detect() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr" ? AppLanguage.Turkish : AppLanguage.English;

    /// <summary>Yalnızca o anki akış için dili geçici olarak değiştirir (testlerde paralel çalışmayı bozmaz).</summary>
    public static IDisposable Use(AppLanguage language)
    {
        var previous = Scoped.Value;
        Scoped.Value = language;
        return new Restore(previous);
    }

    private sealed class Restore(AppLanguage? previous) : IDisposable
    {
        public void Dispose() => Scoped.Value = previous;
    }
}
