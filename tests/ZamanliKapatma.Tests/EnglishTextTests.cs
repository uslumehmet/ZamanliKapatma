using ZamanliKapatma.Core;

namespace ZamanliKapatma.Tests;

/// <summary>İngilizce arayüz metinleri. Lang.Use yalnızca bu testin akışını etkiler; diğer testler Türkçe kalır.</summary>
public class EnglishTextTests
{
    private static readonly DateTime Now = new(2026, 11, 25, 20, 0, 0);

    [Theory]
    [InlineData(45, "45 s")]
    [InlineData(60, "1 min")]
    [InlineData(4320, "1 h 12 min")]
    [InlineData(90000, "1 d 1 h")]
    public void Duration_InEnglish(int seconds, string expected)
    {
        using var _ = Lang.Use(AppLanguage.English);

        Assert.Equal(expected, DurationText.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void When_InEnglish()
    {
        using var _ = Lang.Use(AppLanguage.English);

        Assert.Equal("tomorrow 06:05", ScheduleText.When(new DateTime(2026, 11, 26, 6, 5, 0), Now));
        Assert.Equal("2026-11-28 09:00", ScheduleText.When(new DateTime(2026, 11, 28, 9, 0, 0), Now));
    }

    [Fact]
    public void Summary_InEnglish()
    {
        using var _ = Lang.Use(AppLanguage.English);

        Assert.Equal(
            "Shut down · in 1 h",
            ScheduleText.Summary(new ScheduleSettings { Action = PowerAction.Shutdown, Mode = ScheduleMode.AfterDuration, Duration = TimeSpan.FromHours(1) }));
        Assert.Equal(
            "Restart · daily 23:00",
            ScheduleText.Summary(new ScheduleSettings { Action = PowerAction.Restart, Mode = ScheduleMode.AtTime, TimeOfDay = new TimeOnly(23, 0), RepeatDaily = true }));
    }

    [Fact]
    public void Describe_InEnglish()
    {
        using var _ = Lang.Use(AppLanguage.English);
        var scheduler = new Scheduler();

        Assert.Equal("Nothing scheduled", scheduler.Describe(Now));

        scheduler.Start(new ScheduleSettings { Mode = ScheduleMode.AfterDuration, Duration = TimeSpan.FromMinutes(90) }, Now);
        Assert.Equal("Shut down: 21:30 (1 h 30 min left)", scheduler.Describe(Now));
    }

    [Fact]
    public void PowerActionNames_InEnglish()
    {
        using var _ = Lang.Use(AppLanguage.English);

        Assert.Equal("Sleep", PowerAction.Sleep.DisplayName());
        Assert.Equal("The computer will restart", PowerAction.Restart.PendingMessage());
    }
}
