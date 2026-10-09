using HockeyCoach.Sim.Events;

namespace HockeyCoach.Sim.Tests.Events;

public class EventLogTests
{
    private static StoppageEvent At(int period, double time)
    {
        return new StoppageEvent(EventTestData.Context(period, time), StoppageReason.GoalieFreeze);
    }

    [Fact]
    public void Append_KeepsEventsInOrder()
    {
        var log = new EventLog();
        SimEvent first = At(1, 3.0);
        SimEvent second = At(1, 3.0);
        SimEvent third = At(2, 0.0);

        log.Append(first);
        log.Append(second);
        log.Append(third);

        Assert.Equal(new[] { first, second, third }, log.Events);
        Assert.Equal(3, log.Count);
    }

    [Fact]
    public void Append_RejectsEarlierTimeInSamePeriod()
    {
        var log = new EventLog();
        log.Append(At(1, 10.0));

        Assert.Throws<ArgumentException>(() => log.Append(At(1, 9.5)));
        Assert.Equal(1, log.Count);
    }

    [Fact]
    public void Append_RejectsEarlierPeriod()
    {
        var log = new EventLog();
        log.Append(At(2, 1.0));

        Assert.Throws<ArgumentException>(() => log.Append(At(1, 500.0)));
    }

    [Fact]
    public void Append_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new EventLog().Append(null!));
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(1, -1.0)]
    [InlineData(1, double.NaN)]
    [InlineData(1, double.PositiveInfinity)]
    public void Context_RejectsInvalidPeriodOrTime(int period, double time)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EventTestData.Context(period, time));
    }

    [Fact]
    public void Context_CarriesPlayAndSystemIds()
    {
        EventContext context = EventTestData.Context(playId: "pointShotScreen", systemId: "trap122");
        EventContext outsidePlay = EventTestData.Context();

        Assert.Equal("pointShotScreen", context.PlayId);
        Assert.Equal("trap122", context.SystemId);
        Assert.Null(outsidePlay.PlayId);
        Assert.Null(outsidePlay.SystemId);
    }
}
