using ZamanliKapatma.Core;

namespace ZamanliKapatma.Tests;

public class DurationTextTests
{
    [Theory]
    [InlineData(0, "0 sn")]
    [InlineData(45, "45 sn")]
    [InlineData(60, "1 dk")]
    [InlineData(90, "2 dk")]
    [InlineData(3600, "1 sa")]
    [InlineData(4320, "1 sa 12 dk")]
    [InlineData(90000, "1 gün 1 sa")]
    public void Format_ReturnsShortText(int seconds, string expected)
    {
        Assert.Equal(expected, DurationText.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Format_NegativeValue_IsTreatedAsZero()
    {
        Assert.Equal("0 sn", DurationText.Format(TimeSpan.FromSeconds(-5)));
    }
}

public class ScheduleTextTests
{
    private static readonly DateTime Now = new(2026, 11, 25, 20, 0, 0);

    [Fact]
    public void When_Today_ShowsClockOnly()
    {
        Assert.Equal("23:15", ScheduleText.When(new DateTime(2026, 11, 25, 23, 15, 0), Now));
    }

    [Fact]
    public void When_Tomorrow_ShowsTomorrow()
    {
        Assert.Equal("yarın 06:05", ScheduleText.When(new DateTime(2026, 11, 26, 6, 5, 0), Now));
    }

    [Fact]
    public void When_Later_ShowsFullDate()
    {
        Assert.Equal("28.11.2026 09:00", ScheduleText.When(new DateTime(2026, 11, 28, 9, 0, 0), Now));
    }

    [Fact]
    public void Summary_AfterDuration()
    {
        var settings = new ScheduleSettings { Mode = ScheduleMode.AfterDuration, Duration = TimeSpan.FromMinutes(90) };

        Assert.Equal("Kapat · 1 sa 30 dk sonra", ScheduleText.Summary(settings));
    }

    [Fact]
    public void Summary_DailyAtTime()
    {
        var settings = new ScheduleSettings
        {
            Action = PowerAction.Restart,
            Mode = ScheduleMode.AtTime,
            TimeOfDay = new TimeOnly(4, 0),
            RepeatDaily = true,
        };

        Assert.Equal("Yeniden başlat · her gün 04:00", ScheduleText.Summary(settings));
    }
}

public class ShutdownCommandTests
{
    [Theory]
    [InlineData(PowerAction.Shutdown, false, "/s /t 0")]
    [InlineData(PowerAction.Restart, false, "/r /t 0")]
    [InlineData(PowerAction.LogOff, false, "/l")]
    [InlineData(PowerAction.Shutdown, true, "/s /t 0 /f")]
    [InlineData(PowerAction.Restart, true, "/r /t 0 /f")]
    public void GetArguments_ReturnsShutdownExeArguments(PowerAction action, bool force, string expected)
    {
        Assert.Equal(expected, ShutdownCommand.GetArguments(action, force));
    }

    [Fact]
    public void GetArguments_Sleep_IsNotHandledByShutdownExe()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShutdownCommand.GetArguments(PowerAction.Sleep, false));
    }
}
