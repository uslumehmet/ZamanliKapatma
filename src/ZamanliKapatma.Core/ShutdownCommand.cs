namespace ZamanliKapatma.Core;

/// <summary>Windows'un yerleşik shutdown.exe aracı için komut satırı argümanları.</summary>
public static class ShutdownCommand
{
    public static string GetArguments(PowerAction action, bool forceCloseApps)
    {
        string arguments = action switch
        {
            PowerAction.Shutdown => "/s /t 0",
            PowerAction.Restart => "/r /t 0",
            PowerAction.LogOff => "/l",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Bu işlem shutdown.exe ile yapılmaz."),
        };

        return forceCloseApps ? arguments + " /f" : arguments;
    }
}
