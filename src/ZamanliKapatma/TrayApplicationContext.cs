using System.Media;
using ZamanliKapatma.Core;

namespace ZamanliKapatma;

/// <summary>
/// Bildirim alanındaki (gizli simgeler) simgeyi, sağ tık menüsünü ve
/// her saniye çalışan zamanlayıcı döngüsünü yönetir.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const string AppName = "Zamanlı Kapatma";
    private const int MaxTooltipLength = 127;
    private static readonly int[] QuickMinutes = [15, 30, 60, 120, 180];
    private static readonly TimeSpan SnoozeDelay = TimeSpan.FromMinutes(10);

    private readonly WindowsFormsSynchronizationContext _ui;
    private readonly StateStore _store = new();
    private readonly Scheduler _scheduler;

    private readonly Icon _idleIcon = IconFactory.CreateClockIcon(Color.FromArgb(107, 114, 128));
    private readonly Icon _runningIcon = IconFactory.CreateClockIcon(Color.FromArgb(37, 99, 235));
    private readonly Icon _pausedIcon = IconFactory.CreateClockIcon(Color.FromArgb(217, 119, 6));
    private readonly Icon _warningIcon = IconFactory.CreateClockIcon(Color.FromArgb(220, 38, 38));

    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _menu;
    private readonly Font _boldMenuFont;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _cancelItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly EventWaitHandle _showEvent;
    private readonly RegisteredWaitHandle _showWait;

    private MainForm? _mainForm;
    private WarningForm? _warningForm;
    private static readonly string HideHintMarkerPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ZamanliKapatma",
        "tray-hint-shown");

    private bool _executing;

    public TrayApplicationContext(bool startHidden)
    {
        _ui = new WindowsFormsSynchronizationContext();
        var savedState = _store.Load();
        bool isFirstRun = savedState is null;
        _scheduler = Scheduler.FromState(savedState, DateTime.Now);
        _store.Save(_scheduler.ToState());

        _menu = new ContextMenuStrip();
        _boldMenuFont = new Font(_menu.Font, FontStyle.Bold);

        _statusItem = new ToolStripMenuItem { Enabled = false };
        _toggleItem = new ToolStripMenuItem(string.Empty, null, (_, _) => _scheduler.Toggle(DateTime.Now));
        _cancelItem = new ToolStripMenuItem("Planı iptal et", null, (_, _) => _scheduler.Cancel());
        _startupItem = new ToolStripMenuItem("Windows ile başlat", null, (_, _) => ToggleStartup());
        var openItem = new ToolStripMenuItem("Zamanla...", null, (_, _) => ShowMainWindow()) { Font = _boldMenuFont };
        var exitItem = new ToolStripMenuItem("Çıkış", null, (_, _) => ExitApplication());

        _menu.Items.AddRange(new ToolStripItem[]
        {
            _statusItem,
            new ToolStripSeparator(),
            _toggleItem,
            CreateQuickMenu(),
            openItem,
            _cancelItem,
            new ToolStripSeparator(),
            _startupItem,
            exitItem,
        });
        _menu.Opening += (_, _) =>
        {
            _startupItem.Checked = StartupManager.IsEnabled();
            RefreshUi(DateTime.Now);
        };

        _trayIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = AppName,
            ContextMenuStrip = _menu,
            Visible = true,
        };
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        };

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) => OnTick();

        // İkinci kez açılmaya çalışıldığında ilk kopyanın penceresini göster.
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ShowEventName);
        _showWait = ThreadPool.RegisterWaitForSingleObject(
            _showEvent,
            (_, _) => _ui.Post(_ => ShowMainWindow(), null),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        if (isFirstRun)
        {
            // İlk açılışta otomatik başlatmayı aç: bilgisayar yeniden açıldığında simge tepside hazır olsun.
            TryEnableStartup();
        }

        TryRefreshStartupPath();

        _scheduler.Changed += (_, _) =>
        {
            _store.Save(_scheduler.ToState());
            RefreshUi(DateTime.Now);
        };

        _timer.Start();
        RefreshUi(DateTime.Now);

        if (!startHidden)
        {
            ShowMainWindow();
        }
    }

    private ToolStripMenuItem CreateQuickMenu()
    {
        var quickMenu = new ToolStripMenuItem("Hızlı zamanla");
        foreach (var action in new[] { PowerAction.Shutdown, PowerAction.Restart, PowerAction.Sleep })
        {
            var actionItem = new ToolStripMenuItem(action.DisplayName());
            foreach (int minutes in QuickMinutes)
            {
                var duration = TimeSpan.FromMinutes(minutes);
                string label = minutes < 60 ? $"{minutes} dakika sonra" : $"{minutes / 60} saat sonra";
                actionItem.DropDownItems.Add(label, null, (_, _) => StartQuick(action, duration));
            }

            quickMenu.DropDownItems.Add(actionItem);
        }

        return quickMenu;
    }

    private void StartQuick(PowerAction action, TimeSpan duration)
    {
        var settings = _scheduler.Settings with
        {
            Action = action,
            Mode = ScheduleMode.AfterDuration,
            Duration = duration,
        };
        _scheduler.Start(settings, DateTime.Now);

        if (_mainForm is { IsDisposed: false })
        {
            _mainForm.LoadSettings(settings);
        }

        _trayIcon.ShowBalloonTip(3000, AppName, _scheduler.Describe(DateTime.Now), ToolTipIcon.Info);
    }

    private void OnTick()
    {
        var now = DateTime.Now;
        switch (_scheduler.Tick(now))
        {
            case TickResult.Warn:
                ShowWarning();
                break;

            case TickResult.Execute:
                ExecuteNow();
                return;

            case TickResult.Missed:
                string next = _scheduler.Status == SchedulerStatus.Running
                    ? " Sıradaki: " + _scheduler.Describe(now)
                    : string.Empty;
                _trayIcon.ShowBalloonTip(
                    8000,
                    AppName,
                    "Planlanan zaman bilgisayar kapalı ya da uykudayken geçti; işlem yapılmadı." + next,
                    ToolTipIcon.Warning);
                break;
        }

        RefreshUi(now);
    }

    private void ShowWarning()
    {
        if (_warningForm is { IsDisposed: false })
        {
            return;
        }

        var form = new WarningForm(
            _scheduler,
            _warningIcon,
            SnoozeDelay,
            executeNow: ExecuteNow,
            snooze: () => _scheduler.Snooze(SnoozeDelay, DateTime.Now),
            skip: () => _scheduler.Skip(DateTime.Now));
        form.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_warningForm, form))
            {
                _warningForm = null;
            }
        };

        _warningForm = form;
        form.Show();
        form.Activate();
        SystemSounds.Exclamation.Play();
    }

    private void CloseWarning()
    {
        var form = _warningForm;
        _warningForm = null;
        if (form is { IsDisposed: false })
        {
            form.Close();
        }
    }

    private void ExecuteNow()
    {
        if (_executing)
        {
            return;
        }

        _executing = true;
        try
        {
            var settings = _scheduler.Settings;
            CloseWarning();

            // Önce planı "tamamlandı" olarak kaydet: bilgisayar yeniden açıldığında işlem tekrarlanmasın.
            _scheduler.MarkCompleted(DateTime.Now);
            PowerActions.Execute(settings.Action, settings.ForceCloseApps);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            _trayIcon.ShowBalloonTip(8000, AppName, "İşlem gerçekleştirilemedi: " + ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            _executing = false;
        }
    }

    private void RefreshUi(DateTime now)
    {
        string status = _scheduler.Describe(now);
        _statusItem.Text = status;
        _toggleItem.Text = _scheduler.Status switch
        {
            SchedulerStatus.Running => "Devre dışı bırak",
            SchedulerStatus.Paused => "Etkinleştir",
            _ => $"Etkinleştir ({ScheduleText.Summary(_scheduler.Settings)})",
        };
        _cancelItem.Enabled = _scheduler.Status != SchedulerStatus.Idle;

        Icon icon = _scheduler.Status switch
        {
            SchedulerStatus.Running => _scheduler.IsWarning ? _warningIcon : _runningIcon,
            SchedulerStatus.Paused => _pausedIcon,
            _ => _idleIcon,
        };
        if (!ReferenceEquals(_trayIcon.Icon, icon))
        {
            _trayIcon.Icon = icon;
        }

        string tooltip = $"{AppName}\n{status}";
        if (tooltip.Length > MaxTooltipLength)
        {
            tooltip = tooltip[..MaxTooltipLength];
        }

        if (_trayIcon.Text != tooltip)
        {
            _trayIcon.Text = tooltip;
        }

        if (_mainForm is { IsDisposed: false, Visible: true })
        {
            _mainForm.RefreshStatus(now);
        }

        if (_warningForm is { IsDisposed: false } warning)
        {
            if (_scheduler.IsWarning)
            {
                warning.UpdateCountdown(now);
            }
            else
            {
                CloseWarning();
            }
        }
    }

    private void ShowMainWindow()
    {
        if (_mainForm is null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_scheduler, _runningIcon);
            _mainForm.HiddenToTray += (_, _) => ShowHideHintOnce();
        }

        if (!_mainForm.Visible)
        {
            _mainForm.Show();
        }

        if (_mainForm.WindowState == FormWindowState.Minimized)
        {
            _mainForm.WindowState = FormWindowState.Normal;
        }

        _mainForm.Activate();
    }

    private void ShowHideHintOnce()
    {
        // Bu ipucu yalnızca bir kez gösterilir; her açılışta tekrar çıkması rahatsız edici olur.
        if (File.Exists(HideHintMarkerPath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(HideHintMarkerPath)!);
            File.WriteAllText(HideHintMarkerPath, string.Empty);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // İşaret dosyası yazılamazsa ipucu bir sonraki oturumda yeniden görünebilir; kritik değil.
        }

        _trayIcon.ShowBalloonTip(
            5000,
            AppName,
            "Uygulama arka planda çalışmaya devam ediyor. Simgeye görev çubuğundaki gizli simgeler (^) bölümünden ulaşabilirsiniz.",
            ToolTipIcon.Info);
    }

    private void ToggleStartup()
    {
        try
        {
            StartupManager.SetEnabled(!StartupManager.IsEnabled());
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            _trayIcon.ShowBalloonTip(5000, AppName, "Otomatik başlatma ayarı değiştirilemedi: " + ex.Message, ToolTipIcon.Error);
        }

        if (_mainForm is { IsDisposed: false })
        {
            _mainForm.RefreshStartupCheck();
        }
    }

    private static void TryEnableStartup()
    {
        try
        {
            StartupManager.SetEnabled(true);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            // Kritik değil: kullanıcı menüden "Windows ile başlat"ı yine açabilir.
        }
    }

    private static void TryRefreshStartupPath()
    {
        try
        {
            StartupManager.RefreshPathIfEnabled();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            // Kritik değil: kayıt güncellenemezse eski yol kullanılmaya devam eder.
        }
    }

    private void ExitApplication()
    {
        if (_scheduler.Status == SchedulerStatus.Running)
        {
            var answer = MessageBox.Show(
                "Planlanmış bir işlem var. Uygulama kapalıyken bu işlem gerçekleşmez.\n\nYine de çıkılsın mı?",
                AppName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                return;
            }
        }

        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _trayIcon.Visible = false;
        CloseWarning();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _showWait.Unregister(null);
            _showEvent.Dispose();
            _timer.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _menu.Dispose();
            _boldMenuFont.Dispose();
            _warningForm?.Dispose();
            _mainForm?.Dispose();
            _idleIcon.Dispose();
            _runningIcon.Dispose();
            _pausedIcon.Dispose();
            _warningIcon.Dispose();
            _ui.Dispose();
        }

        base.Dispose(disposing);
    }
}
