using System.Text.Json.Serialization;

namespace ZamanliKapatma.Core;

public enum ScheduleMode
{
    /// <summary>Belirli bir süre sonra (örn. 1 saat 30 dakika sonra).</summary>
    AfterDuration,

    /// <summary>Belirli bir saatte (örn. 23:00).</summary>
    AtTime,
}

/// <summary>Kullanıcının seçtiği plan ayarları.</summary>
public sealed record ScheduleSettings
{
    public PowerAction Action { get; init; } = PowerAction.Shutdown;
    public ScheduleMode Mode { get; init; } = ScheduleMode.AfterDuration;
    public TimeSpan Duration { get; init; } = TimeSpan.FromHours(1);
    public TimeOnly TimeOfDay { get; init; } = new(23, 0);
    public bool RepeatDaily { get; init; }
    public bool ForceCloseApps { get; init; }

    /// <summary>Plan her gün aynı saatte tekrarlanıyor mu?</summary>
    [JsonIgnore]
    public bool IsRecurring => Mode == ScheduleMode.AtTime && RepeatDaily;
}
