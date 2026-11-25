namespace ZamanliKapatma;

internal static class Program
{
    private const string InstanceMutexName = @"Local\ZamanliKapatma.Instance";
    internal const string ShowEventName = @"Local\ZamanliKapatma.Show";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, InstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            // Uygulama zaten çalışıyor: yeni kopya açmak yerine mevcut pencereyi öne getir.
            SignalRunningInstance();
            return;
        }

        bool startHidden = args.Contains(StartupManager.TrayArgument, StringComparer.OrdinalIgnoreCase);

        ApplicationConfiguration.Initialize();
        using var context = new TrayApplicationContext(startHidden);
        Application.Run(context);
    }

    private static void SignalRunningInstance()
    {
        try
        {
            using var showEvent = EventWaitHandle.OpenExisting(ShowEventName);
            showEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // İlk kopya henüz hazır değil; yapılacak bir şey yok.
        }
    }
}
