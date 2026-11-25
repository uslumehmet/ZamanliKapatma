namespace ZamanliKapatma.Core;

/// <summary>Zamanlayıcının sonunda yapılacak güç işlemi.</summary>
public enum PowerAction
{
    Shutdown,
    Restart,
    Sleep,
    LogOff,
}

public static class PowerActionText
{
    /// <summary>Listelerde gösterilen ad, örn. "Bilgisayarı kapat".</summary>
    public static string DisplayName(this PowerAction action) => action switch
    {
        PowerAction.Shutdown => "Bilgisayarı kapat",
        PowerAction.Restart => "Yeniden başlat",
        PowerAction.Sleep => "Uyku moduna al",
        PowerAction.LogOff => "Oturumu kapat",
        _ => action.ToString(),
    };

    /// <summary>Durum satırlarında kullanılan kısa ad, örn. "Kapat".</summary>
    public static string ShortName(this PowerAction action) => action switch
    {
        PowerAction.Shutdown => "Kapat",
        PowerAction.Restart => "Yeniden başlat",
        PowerAction.Sleep => "Uyku",
        PowerAction.LogOff => "Oturumu kapat",
        _ => action.ToString(),
    };

    /// <summary>Uyarı penceresindeki cümle, örn. "Bilgisayar kapatılacak".</summary>
    public static string PendingMessage(this PowerAction action) => action switch
    {
        PowerAction.Shutdown => "Bilgisayar kapatılacak",
        PowerAction.Restart => "Bilgisayar yeniden başlatılacak",
        PowerAction.Sleep => "Bilgisayar uyku moduna alınacak",
        PowerAction.LogOff => "Oturum kapatılacak",
        _ => action.ToString(),
    };
}
