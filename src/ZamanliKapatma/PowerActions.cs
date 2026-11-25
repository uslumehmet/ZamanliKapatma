using System.Diagnostics;
using ZamanliKapatma.Core;

namespace ZamanliKapatma;

/// <summary>Güç işlemlerini gerçekten uygular. Yönetici izni gerektirmez.</summary>
internal static class PowerActions
{
    public static void Execute(PowerAction action, bool forceCloseApps)
    {
        if (action == PowerAction.Sleep)
        {
            Application.SetSuspendState(PowerState.Suspend, force: false, disableWakeEvent: false);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "shutdown.exe"),
            Arguments = ShutdownCommand.GetArguments(action, forceCloseApps),
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
    }
}
