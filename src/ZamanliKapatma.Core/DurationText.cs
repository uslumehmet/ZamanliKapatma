namespace ZamanliKapatma.Core;

public static class DurationText
{
    /// <summary>
    /// Süreyi kısa metne çevirir: "45 sn", "12 dk", "1 sa 12 dk", "2 gün 3 sa" (İngilizcede "45 s", "1 h 12 min").
    /// Yukarı yuvarlar; böylece süre bitmeden "0 dk" gösterilmez.
    /// </summary>
    public static string Format(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        long seconds = (long)Math.Ceiling(value.TotalSeconds);
        if (seconds < 60)
        {
            return Lang.T($"{seconds} sn", $"{seconds} s");
        }

        long totalMinutes = (seconds + 59) / 60;
        long days = totalMinutes / (24 * 60);
        long hours = totalMinutes / 60 % 24;
        long minutes = totalMinutes % 60;

        var parts = new List<string>(3);
        if (days > 0)
        {
            parts.Add(Lang.T($"{days} gün", $"{days} d"));
        }

        if (hours > 0)
        {
            parts.Add(Lang.T($"{hours} sa", $"{hours} h"));
        }

        if (minutes > 0 && days == 0)
        {
            parts.Add(Lang.T($"{minutes} dk", $"{minutes} min"));
        }

        return string.Join(" ", parts);
    }
}
