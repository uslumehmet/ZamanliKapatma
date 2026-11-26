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
        PowerAction.Shutdown => Lang.T("Bilgisayarı kapat", "Shut down"),
        PowerAction.Restart => Lang.T("Yeniden başlat", "Restart"),
        PowerAction.Sleep => Lang.T("Uyku moduna al", "Sleep"),
        PowerAction.LogOff => Lang.T("Oturumu kapat", "Sign out"),
        _ => action.ToString(),
    };

    /// <summary>Durum satırlarında kullanılan kısa ad, örn. "Kapat".</summary>
    public static string ShortName(this PowerAction action) => action switch
    {
        PowerAction.Shutdown => Lang.T("Kapat", "Shut down"),
        PowerAction.Restart => Lang.T("Yeniden başlat", "Restart"),
        PowerAction.Sleep => Lang.T("Uyku", "Sleep"),
        PowerAction.LogOff => Lang.T("Oturumu kapat", "Sign out"),
        _ => action.ToString(),
    };

    /// <summary>Uyarı penceresindeki cümle, örn. "Bilgisayar kapatılacak".</summary>
    public static string PendingMessage(this PowerAction action) => action switch
    {
        PowerAction.Shutdown => Lang.T("Bilgisayar kapatılacak", "The computer will shut down"),
        PowerAction.Restart => Lang.T("Bilgisayar yeniden başlatılacak", "The computer will restart"),
        PowerAction.Sleep => Lang.T("Bilgisayar uyku moduna alınacak", "The computer will go to sleep"),
        PowerAction.LogOff => Lang.T("Oturum kapatılacak", "You will be signed out"),
        _ => action.ToString(),
    };
}
