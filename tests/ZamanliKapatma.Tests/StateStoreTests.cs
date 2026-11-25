using ZamanliKapatma.Core;

namespace ZamanliKapatma.Tests;

public sealed class StateStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ZamanliKapatma.Tests", Guid.NewGuid().ToString("N"));

    private string StatePath => Path.Combine(_directory, "state.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenFileIsMissing_ReturnsNull()
    {
        Assert.Null(new StateStore(StatePath).Load());
    }

    [Fact]
    public void SaveThenLoad_RoundTripsState()
    {
        var store = new StateStore(StatePath);
        var state = new SchedulerState
        {
            Settings = new ScheduleSettings
            {
                Action = PowerAction.Restart,
                Mode = ScheduleMode.AtTime,
                TimeOfDay = new TimeOnly(6, 45),
                RepeatDaily = true,
                ForceCloseApps = true,
            },
            Status = SchedulerStatus.Running,
            Target = new DateTime(2026, 11, 26, 6, 45, 0),
        };

        store.Save(state);
        var loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Equal(state.Settings, loaded.Settings);
        Assert.Equal(state.Status, loaded.Status);
        Assert.Equal(state.Target, loaded.Target);
        Assert.Null(loaded.PausedRemaining);
    }

    [Fact]
    public void Load_WhenFileIsCorrupt_ReturnsNull()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(StatePath, "{ bu geçerli json değil");

        Assert.Null(new StateStore(StatePath).Load());
    }
}
