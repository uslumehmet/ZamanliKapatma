using ZamanliKapatma.Core;

namespace ZamanliKapatma.Tests;

public class SchedulerTests
{
    private static readonly DateTime Now = new(2026, 11, 25, 20, 0, 0);

    private static ScheduleSettings After(int minutes, PowerAction action = PowerAction.Shutdown) => new()
    {
        Action = action,
        Mode = ScheduleMode.AfterDuration,
        Duration = TimeSpan.FromMinutes(minutes),
    };

    private static ScheduleSettings At(int hour, int minute, bool daily = false) => new()
    {
        Mode = ScheduleMode.AtTime,
        TimeOfDay = new TimeOnly(hour, minute),
        RepeatDaily = daily,
    };

    [Fact]
    public void NewScheduler_IsIdle()
    {
        var scheduler = new Scheduler();

        Assert.Equal(SchedulerStatus.Idle, scheduler.Status);
        Assert.Null(scheduler.Target);
    }

    [Fact]
    public void Start_AfterDuration_TargetsNowPlusDuration()
    {
        var scheduler = new Scheduler();

        scheduler.Start(After(90), Now);

        Assert.Equal(SchedulerStatus.Running, scheduler.Status);
        Assert.Equal(Now.AddMinutes(90), scheduler.Target);
    }

    [Fact]
    public void Start_WithZeroDuration_Throws()
    {
        var scheduler = new Scheduler();

        Assert.Throws<ArgumentException>(() => scheduler.Start(After(0), Now));
    }

    [Fact]
    public void Start_AtLaterTime_TargetsToday()
    {
        var scheduler = new Scheduler();

        scheduler.Start(At(23, 0), Now);

        Assert.Equal(new DateTime(2026, 11, 25, 23, 0, 0), scheduler.Target);
    }

    [Fact]
    public void Start_AtPassedTime_TargetsTomorrow()
    {
        var scheduler = new Scheduler();

        scheduler.Start(At(19, 30), Now);

        Assert.Equal(new DateTime(2026, 11, 26, 19, 30, 0), scheduler.Target);
    }

    [Fact]
    public void NextOccurrence_AtExactlyThatTime_ReturnsTomorrow()
    {
        Assert.Equal(Now.AddDays(1), Scheduler.NextOccurrence(Now, new TimeOnly(20, 0)));
    }

    [Fact]
    public void Tick_WarnsOneMinuteBefore_ThenExecutesAtTarget()
    {
        var scheduler = new Scheduler();
        scheduler.Start(After(5), Now);

        Assert.Equal(TickResult.None, scheduler.Tick(Now.AddMinutes(3)));
        Assert.Equal(TickResult.Warn, scheduler.Tick(Now.AddMinutes(4)));
        Assert.True(scheduler.IsWarning);
        Assert.Equal(TickResult.None, scheduler.Tick(Now.AddMinutes(4).AddSeconds(30)));
        Assert.Equal(TickResult.Execute, scheduler.Tick(Now.AddMinutes(5)));
    }

    [Fact]
    public void Tick_ShortlyAfterTarget_StillGivesFullWarning()
    {
        var scheduler = new Scheduler();
        scheduler.Start(After(5), Now);
        var wokeUpAt = Now.AddMinutes(6);

        Assert.Equal(TickResult.Warn, scheduler.Tick(wokeUpAt));
        Assert.Equal(wokeUpAt + Scheduler.WarningLead, scheduler.Target);
    }

    [Fact]
    public void Tick_LongAfterTarget_DropsOneTimePlan()
    {
        var scheduler = new Scheduler();
        scheduler.Start(After(5), Now);

        Assert.Equal(TickResult.Missed, scheduler.Tick(Now.AddHours(2)));
        Assert.Equal(SchedulerStatus.Idle, scheduler.Status);
    }

    [Fact]
    public void Tick_LongAfterTarget_MovesDailyPlanToNextOccurrence()
    {
        var scheduler = new Scheduler();
        scheduler.Start(At(21, 0, daily: true), Now);

        Assert.Equal(TickResult.Missed, scheduler.Tick(new DateTime(2026, 11, 26, 8, 0, 0)));
        Assert.Equal(SchedulerStatus.Running, scheduler.Status);
        Assert.Equal(new DateTime(2026, 11, 26, 21, 0, 0), scheduler.Target);
    }

    [Fact]
    public void PauseAndResume_KeepsRemainingTime()
    {
        var scheduler = new Scheduler();
        scheduler.Start(After(60), Now);

        scheduler.Pause(Now.AddMinutes(20));

        Assert.Equal(SchedulerStatus.Paused, scheduler.Status);
        Assert.Equal(TimeSpan.FromMinutes(40), scheduler.PausedRemaining);
        Assert.Equal(TickResult.None, scheduler.Tick(Now.AddHours(5)));

        scheduler.Resume(Now.AddHours(2));

        Assert.Equal(SchedulerStatus.Running, scheduler.Status);
        Assert.Equal(Now.AddHours(2).AddMinutes(40), scheduler.Target);
    }

    [Fact]
    public void Resume_AtTimePlan_UsesNextOccurrence()
    {
        var scheduler = new Scheduler();
        scheduler.Start(At(23, 0), Now);
        scheduler.Pause(Now);

        scheduler.Resume(new DateTime(2026, 11, 25, 23, 30, 0));

        Assert.Equal(new DateTime(2026, 11, 26, 23, 0, 0), scheduler.Target);
    }

    [Fact]
    public void Toggle_CyclesThroughStates()
    {
        var scheduler = new Scheduler();

        scheduler.Toggle(Now);
        Assert.Equal(SchedulerStatus.Running, scheduler.Status);

        scheduler.Toggle(Now);
        Assert.Equal(SchedulerStatus.Paused, scheduler.Status);

        scheduler.Toggle(Now);
        Assert.Equal(SchedulerStatus.Running, scheduler.Status);
    }

    [Fact]
    public void Snooze_MovesTargetAndResetsWarning()
    {
        var scheduler = new Scheduler();
        scheduler.Start(After(5), Now);
        scheduler.Tick(Now.AddMinutes(4));

        scheduler.Snooze(TimeSpan.FromMinutes(10), Now.AddMinutes(4).AddSeconds(30));

        Assert.False(scheduler.IsWarning);
        Assert.Equal(Now.AddMinutes(14).AddSeconds(30), scheduler.Target);
    }

    [Fact]
    public void MarkCompleted_OneTimePlan_BecomesIdleButKeepsSettings()
    {
        var scheduler = new Scheduler();
        var settings = After(5, PowerAction.Restart);
        scheduler.Start(settings, Now);

        scheduler.MarkCompleted(Now.AddMinutes(5));

        Assert.Equal(SchedulerStatus.Idle, scheduler.Status);
        Assert.Equal(settings, scheduler.Settings);
    }

    [Fact]
    public void MarkCompleted_DailyPlan_SchedulesTomorrow()
    {
        var scheduler = new Scheduler();
        scheduler.Start(At(23, 0, daily: true), Now);

        scheduler.MarkCompleted(new DateTime(2026, 11, 25, 23, 0, 0));

        Assert.Equal(new DateTime(2026, 11, 26, 23, 0, 0), scheduler.Target);
    }

    [Fact]
    public void MarkCompleted_SnoozedDailyPlan_SchedulesTomorrowAtOriginalTime()
    {
        var scheduler = new Scheduler();
        scheduler.Start(At(23, 0, daily: true), Now);
        scheduler.Snooze(TimeSpan.FromMinutes(10), new DateTime(2026, 11, 25, 22, 59, 0));

        scheduler.MarkCompleted(new DateTime(2026, 11, 25, 23, 9, 0));

        Assert.Equal(new DateTime(2026, 11, 26, 23, 0, 0), scheduler.Target);
    }

    [Fact]
    public void FromState_DropsPastOneTimePlan_SoRestartDoesNotLoop()
    {
        var state = new SchedulerState
        {
            Settings = After(5, PowerAction.Restart),
            Status = SchedulerStatus.Running,
            Target = Now.AddMinutes(-1),
        };

        var scheduler = Scheduler.FromState(state, Now);

        Assert.Equal(SchedulerStatus.Idle, scheduler.Status);
    }

    [Fact]
    public void FromState_PastDailyPlan_MovesToNextOccurrence()
    {
        var state = new SchedulerState
        {
            Settings = At(7, 0, daily: true),
            Status = SchedulerStatus.Running,
            Target = new DateTime(2026, 11, 24, 7, 0, 0),
        };

        var scheduler = Scheduler.FromState(state, Now);

        Assert.Equal(SchedulerStatus.Running, scheduler.Status);
        Assert.Equal(new DateTime(2026, 11, 26, 7, 0, 0), scheduler.Target);
    }

    [Fact]
    public void FromState_KeepsFutureTarget()
    {
        var state = new SchedulerState
        {
            Settings = After(60),
            Status = SchedulerStatus.Running,
            Target = Now.AddMinutes(30),
        };

        var scheduler = Scheduler.FromState(state, Now);

        Assert.Equal(SchedulerStatus.Running, scheduler.Status);
        Assert.Equal(Now.AddMinutes(30), scheduler.Target);
    }

    [Fact]
    public void ToStateAndFromState_RoundTripsPausedPlan()
    {
        var original = new Scheduler();
        original.Start(After(60), Now);
        original.Pause(Now.AddMinutes(20));

        var restored = Scheduler.FromState(original.ToState(), Now.AddDays(1));

        Assert.Equal(SchedulerStatus.Paused, restored.Status);
        Assert.Equal(TimeSpan.FromMinutes(40), restored.PausedRemaining);
        Assert.Equal(original.Settings, restored.Settings);
    }

    [Fact]
    public void Changed_IsRaisedOnEveryStateChange()
    {
        var scheduler = new Scheduler();
        int count = 0;
        scheduler.Changed += (_, _) => count++;

        scheduler.Start(After(5), Now);
        scheduler.Pause(Now);
        scheduler.Resume(Now);
        scheduler.Cancel();

        Assert.Equal(4, count);
    }

    [Fact]
    public void Describe_RunningDailyPlan()
    {
        var scheduler = new Scheduler();
        scheduler.Start(At(23, 0, daily: true), Now);

        Assert.Equal("Kapat: 23:00 (3 sa kaldı) · her gün", scheduler.Describe(Now));
    }

    [Fact]
    public void Describe_PlanForTomorrow()
    {
        var scheduler = new Scheduler();
        scheduler.Start(At(7, 30), Now);

        Assert.Equal("Kapat: yarın 07:30 (11 sa 30 dk kaldı)", scheduler.Describe(Now));
    }

    [Fact]
    public void Describe_PausedPlan()
    {
        var scheduler = new Scheduler();
        scheduler.Start(After(60, PowerAction.Restart), Now);
        scheduler.Pause(Now.AddMinutes(15));

        Assert.Equal("Yeniden başlat: devre dışı (45 dk kalmıştı)", scheduler.Describe(Now));
    }

    [Fact]
    public void Describe_Idle()
    {
        Assert.Equal("Planlanmış işlem yok", new Scheduler().Describe(Now));
    }
}
