namespace ZamanliKapatma.Core;

/// <summary>Uygulama kapanıp açıldığında planın kaldığı yerden devam etmesi için diske yazılan durum.</summary>
public sealed class SchedulerState
{
    public ScheduleSettings Settings { get; set; } = new();
    public SchedulerStatus Status { get; set; }
    public DateTime? Target { get; set; }
    public TimeSpan? PausedRemaining { get; set; }
}
