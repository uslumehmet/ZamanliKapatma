namespace ZamanliKapatma.Core;

public enum SchedulerStatus
{
    /// <summary>Planlanmış işlem yok.</summary>
    Idle,

    /// <summary>Geri sayım devam ediyor.</summary>
    Running,

    /// <summary>Plan devre dışı bırakıldı; kalan süre saklanıyor.</summary>
    Paused,
}

public enum TickResult
{
    None,

    /// <summary>İşleme <see cref="Scheduler.WarningLead"/> kaldı; kullanıcı uyarılmalı.</summary>
    Warn,

    /// <summary>Zaman geldi; işlem şimdi yapılmalı.</summary>
    Execute,

    /// <summary>Bilgisayar kapalıyken/uykudayken zaman geçti; işlem yapılmadı.</summary>
    Missed,
}

/// <summary>
/// Zamanlanmış güç işleminin durum makinesi. Arayüzden bağımsızdır ve şimdiki zaman
/// her metoda parametre olarak verildiği için tamamen birim test edilebilir.
/// </summary>
public sealed class Scheduler
{
    /// <summary>İşlemden ne kadar önce uyarı gösterileceği.</summary>
    public static readonly TimeSpan WarningLead = TimeSpan.FromSeconds(60);

    /// <summary>Bu süreden fazla gecikmiş işlemler "kaçırıldı" sayılır ve yapılmaz.</summary>
    public static readonly TimeSpan MissedTolerance = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(1);

    private bool _warned;

    public ScheduleSettings Settings { get; private set; } = new();
    public SchedulerStatus Status { get; private set; } = SchedulerStatus.Idle;
    public DateTime? Target { get; private set; }
    public TimeSpan? PausedRemaining { get; private set; }

    /// <summary>Son dakika uyarısı verildi ve işlem bekleniyor.</summary>
    public bool IsWarning => Status == SchedulerStatus.Running && _warned;

    /// <summary>Durum her değiştiğinde tetiklenir (kaydetmek ve arayüzü yenilemek için).</summary>
    public event EventHandler? Changed;

    public void Start(ScheduleSettings settings, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Mode == ScheduleMode.AfterDuration && settings.Duration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Süre sıfırdan büyük olmalı.", nameof(settings));
        }

        Settings = settings;
        SetRunning(ComputeTarget(settings, now));
    }

    /// <summary>Planı devre dışı bırakır. Süreli planlarda kalan süre saklanır.</summary>
    public void Pause(DateTime now)
    {
        if (Status != SchedulerStatus.Running || Target is not { } target)
        {
            return;
        }

        PausedRemaining = Settings.Mode == ScheduleMode.AfterDuration
            ? Max(target - now, WarningLead)
            : null;
        Target = null;
        Status = SchedulerStatus.Paused;
        _warned = false;
        OnChanged();
    }

    /// <summary>Devre dışı bırakılan planı yeniden etkinleştirir.</summary>
    public void Resume(DateTime now)
    {
        if (Status != SchedulerStatus.Paused)
        {
            return;
        }

        var target = PausedRemaining is { } remaining
            ? now + remaining
            : ComputeTarget(Settings, now);
        SetRunning(target);
    }

    /// <summary>Çalışıyorsa devre dışı bırakır, devre dışıysa etkinleştirir, plan yoksa son ayarlarla başlatır.</summary>
    public void Toggle(DateTime now)
    {
        switch (Status)
        {
            case SchedulerStatus.Running:
                Pause(now);
                break;
            case SchedulerStatus.Paused:
                Resume(now);
                break;
            default:
                Start(Settings, now);
                break;
        }
    }

    public void Cancel()
    {
        if (Status == SchedulerStatus.Idle)
        {
            return;
        }

        Status = SchedulerStatus.Idle;
        Target = null;
        PausedRemaining = null;
        _warned = false;
        OnChanged();
    }

    /// <summary>İşlemi şimdiden itibaren <paramref name="delay"/> kadar erteler.</summary>
    public void Snooze(TimeSpan delay, DateTime now)
    {
        if (Status != SchedulerStatus.Running)
        {
            return;
        }

        SetRunning(now + delay);
    }

    /// <summary>
    /// Sıradaki işlemi atlar: her gün tekrarlanan planlar bir sonraki güne geçer,
    /// tek seferlik planlar iptal edilir.
    /// </summary>
    public void Skip(DateTime now)
    {
        if (Status != SchedulerStatus.Running || Target is not { } target)
        {
            return;
        }

        if (Settings.IsRecurring)
        {
            SetRunning(NextOccurrence(Max(now, target), Settings.TimeOfDay));
        }
        else
        {
            Cancel();
        }
    }

    /// <summary>
    /// İşlem yapılmadan hemen önce çağrılır. Böylece bilgisayar yeniden açıldığında
    /// aynı işlem tekrar yapılmaz.
    /// </summary>
    public void MarkCompleted(DateTime now) => Skip(now);

    /// <summary>Her saniye çağrılır ve arayüzün ne yapması gerektiğini söyler.</summary>
    public TickResult Tick(DateTime now)
    {
        if (Status != SchedulerStatus.Running || Target is not { } target)
        {
            return TickResult.None;
        }

        if (now - target > MissedTolerance)
        {
            Skip(now);
            return TickResult.Missed;
        }

        if (!_warned)
        {
            if (now < target - WarningLead)
            {
                return TickResult.None;
            }

            _warned = true;
            if (target < now)
            {
                // Bilgisayar uykudan geç uyandı: kullanıcıya yine de tam uyarı süresi tanı.
                Target = now + WarningLead;
            }

            OnChanged();
            return TickResult.Warn;
        }

        return now >= target ? TickResult.Execute : TickResult.None;
    }

    /// <summary>Kalan süre; plan yoksa null.</summary>
    public TimeSpan? Remaining(DateTime now) => Status switch
    {
        SchedulerStatus.Running when Target is { } target => Max(target - now, TimeSpan.Zero),
        SchedulerStatus.Paused => PausedRemaining,
        _ => null,
    };

    /// <summary>Durumun kısa açıklaması, örn. "Kapat: 23:00 (1 sa 12 dk kaldı)".</summary>
    public string Describe(DateTime now)
    {
        string name = Settings.Action.ShortName();
        switch (Status)
        {
            case SchedulerStatus.Running when Target is { } target:
                string text = $"{name}: {ScheduleText.When(target, now)} ({DurationText.Format(target - now)} kaldı)";
                return Settings.IsRecurring ? text + " · her gün" : text;

            case SchedulerStatus.Paused when PausedRemaining is { } remaining:
                return $"{name}: devre dışı ({DurationText.Format(remaining)} kalmıştı)";

            case SchedulerStatus.Paused:
                string daily = Settings.RepeatDaily ? "her gün " : string.Empty;
                return $"{name}: devre dışı ({daily}{ScheduleText.Clock(Settings.TimeOfDay)})";

            default:
                return "Planlanmış işlem yok";
        }
    }

    public SchedulerState ToState() => new()
    {
        Settings = Settings,
        Status = Status,
        Target = Target,
        PausedRemaining = PausedRemaining,
    };

    /// <summary>
    /// Diskten okunan durumu geri yükler. Zamanı geçmiş tek seferlik planlar bilerek
    /// atılır: aksi halde örneğin "yeniden başlat" planı açılışta tekrar çalışıp
    /// sonsuz bir yeniden başlatma döngüsüne yol açabilirdi.
    /// </summary>
    public static Scheduler FromState(SchedulerState? state, DateTime now)
    {
        var scheduler = new Scheduler();
        if (state is null)
        {
            return scheduler;
        }

        var settings = state.Settings ?? new ScheduleSettings();
        if (settings.Duration <= TimeSpan.Zero)
        {
            settings = settings with { Duration = DefaultDuration };
        }

        scheduler.Settings = settings;

        switch (state.Status)
        {
            case SchedulerStatus.Running when state.Target is { } target:
                if (target > now)
                {
                    scheduler.Status = SchedulerStatus.Running;
                    scheduler.Target = target;
                }
                else if (settings.IsRecurring)
                {
                    scheduler.Status = SchedulerStatus.Running;
                    scheduler.Target = NextOccurrence(now, settings.TimeOfDay);
                }

                break;

            case SchedulerStatus.Paused:
                scheduler.Status = SchedulerStatus.Paused;
                scheduler.PausedRemaining = state.PausedRemaining is { } remaining && remaining > TimeSpan.Zero
                    ? remaining
                    : null;
                break;
        }

        return scheduler;
    }

    public static DateTime ComputeTarget(ScheduleSettings settings, DateTime now) =>
        settings.Mode == ScheduleMode.AfterDuration
            ? now + settings.Duration
            : NextOccurrence(now, settings.TimeOfDay);

    /// <summary><paramref name="after"/> anından kesinlikle sonra gelen ilk <paramref name="time"/>.</summary>
    public static DateTime NextOccurrence(DateTime after, TimeOnly time)
    {
        var candidate = after.Date + time.ToTimeSpan();
        return candidate > after ? candidate : candidate.AddDays(1);
    }

    private void SetRunning(DateTime target)
    {
        Target = target;
        PausedRemaining = null;
        Status = SchedulerStatus.Running;
        _warned = false;
        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
