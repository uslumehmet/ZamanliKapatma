using System.Globalization;

namespace ZamanliKapatma.Core;

public static class ScheduleText
{
    public static string Clock(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string Clock(DateTime time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>"23:00", "yarın 07:30" veya "28.11.2026 09:00" (İngilizcede "tomorrow 07:30", "2026-11-28 09:00").</summary>
    public static string When(DateTime target, DateTime now)
    {
        if (target.Date == now.Date)
        {
            return Clock(target);
        }

        if (target.Date == now.Date.AddDays(1))
        {
            return Lang.T("yarın ", "tomorrow ") + Clock(target);
        }

        return target.ToString(Lang.IsEnglish ? "yyyy-MM-dd HH:mm" : "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>Ayarların tek satırlık özeti, örn. "Kapat · 1 sa sonra" veya "Kapat · her gün 23:00".</summary>
    public static string Summary(ScheduleSettings settings)
    {
        string name = settings.Action.ShortName();
        if (settings.Mode == ScheduleMode.AfterDuration)
        {
            return Lang.T(
                $"{name} · {DurationText.Format(settings.Duration)} sonra",
                $"{name} · in {DurationText.Format(settings.Duration)}");
        }

        string daily = settings.RepeatDaily ? Lang.T("her gün ", "daily ") : string.Empty;
        return $"{name} · {daily}{Clock(settings.TimeOfDay)}";
    }
}
