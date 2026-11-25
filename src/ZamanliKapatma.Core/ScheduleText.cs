using System.Globalization;

namespace ZamanliKapatma.Core;

public static class ScheduleText
{
    public static string Clock(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string Clock(DateTime time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>"23:00", "yarın 07:30" veya "28.11.2026 09:00".</summary>
    public static string When(DateTime target, DateTime now)
    {
        if (target.Date == now.Date)
        {
            return Clock(target);
        }

        if (target.Date == now.Date.AddDays(1))
        {
            return "yarın " + Clock(target);
        }

        return target.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>Ayarların tek satırlık özeti, örn. "Kapat · 1 sa sonra" veya "Kapat · her gün 23:00".</summary>
    public static string Summary(ScheduleSettings settings)
    {
        string name = settings.Action.ShortName();
        if (settings.Mode == ScheduleMode.AfterDuration)
        {
            return $"{name} · {DurationText.Format(settings.Duration)} sonra";
        }

        string daily = settings.RepeatDaily ? "her gün " : string.Empty;
        return $"{name} · {daily}{Clock(settings.TimeOfDay)}";
    }
}
