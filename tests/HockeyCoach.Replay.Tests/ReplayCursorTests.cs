using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests;

public sealed class ReplayCursorTests
{
    /// <summary>
    /// 0: faceoff at 0 s (zero length); 1: pass 1 → 2, 0–4 s; 2: turnover at 4 s (zero length); 3: away goal, 4–10 s;
    /// 4: stoppage at 10 s (zero length); 5: faceoff at 12 s.
    /// </summary>
    private static ReplayTimeline Timeline() => new SyntheticMatch()
        .Add("Faceoff", 1, 0, 27, 1, SyntheticMatch.OnIce(27, 16, 27, 38), new JObject { ["homeCentreId"] = 1, ["awayCentreId"] = 101, ["winner"] = "Home", ["location"] = "NeutralZone" })
        .Add("Pass", 1, 4, 37, 2, SyntheticMatch.OnIce(31, 37, 27, 38), new JObject { ["passerId"] = 1, ["receiverId"] = 2, ["underPressure"] = false, ["succeeded"] = true })
        .Add("Turnover", 1, 4, 37, 102, SyntheticMatch.OnIce(31, 37, 27, 37), new JObject { ["loserId"] = 2, ["takerId"] = 102, ["nodeId"] = 37 })
        .Add("Shot", 1, 10, 12, 101, SyntheticMatch.OnIce(31, 37, 12, 37), new JObject { ["shooterId"] = 101, ["stoppedById"] = null, ["outcome"] = "Goal" })
        .Add("Stoppage", 1, 10, 12, 101, SyntheticMatch.OnIce(31, 37, 12, 37), new JObject { ["reason"] = "Goal" })
        .Add("Faceoff", 1, 12, 27, 1, SyntheticMatch.OnIce(27, 16, 27, 38), new JObject { ["homeCentreId"] = 1, ["awayCentreId"] = 101, ["winner"] = "Home", ["location"] = "NeutralZone" })
        .Build();

    [Fact]
    public void NewCursor_IsPausedAtTheStart_At1x()
    {
        var c = new ReplayCursor(Timeline());
        Assert.False(c.IsPlaying);
        Assert.Equal(0, c.Clock);
        Assert.Equal(0, c.SegmentIndex);
        Assert.Equal(1, c.Speed);
        Assert.Equal(1, c.Sample().Fraction);
    }

    [Fact]
    public void Advance_DoesNothing_WhilePaused()
    {
        var c = new ReplayCursor(Timeline());
        c.Advance(3);
        Assert.Equal(0, c.Clock);
    }

    [Fact]
    public void Advance_MovesTheClockBySpeedTimesRealSeconds_AndInterpolates()
    {
        ReplayTimeline t = Timeline();
        var c = new ReplayCursor(t) { Speed = 2 };
        c.Play();
        c.Advance(1);
        ReplaySample s = c.Sample();
        Assert.Equal(2, s.Clock);
        Assert.Equal(1, s.SegmentIndex);
        Assert.Equal(0.5, s.Fraction);
        RinkPoint passer = s.Players.Single(p => p.Id == 1).Position;
        Assert.Equal(RinkPoint.Lerp(t.Mapping.NodePosition(27), t.Mapping.NodePosition(31), 0.5), passer);
        Assert.Equal(RinkPoint.Lerp(t.Mapping.NodePosition(27), t.Mapping.NodePosition(37), 0.5), s.Puck);
        Assert.Null(s.PuckCarrierId);
    }

    [Fact]
    public void ReachingAnEventTime_PassesThroughZeroLengthSegmentsAtThatSecond()
    {
        var c = new ReplayCursor(Timeline());
        c.Play();
        c.Advance(4);
        ReplaySample s = c.Sample();
        Assert.Equal(2, s.SegmentIndex);
        Assert.Equal(102, s.PuckCarrierId);
    }

    [Fact]
    public void Score_ChangesWhenTheGoalSegmentCompletes()
    {
        ReplayTimeline t = Timeline();
        var c = new ReplayCursor(t);
        c.SeekClock(7);
        ReplaySample mid = c.Sample();
        Assert.Equal(3, mid.SegmentIndex);
        Assert.Equal((0, 0), (mid.HomeScore, mid.AwayScore));
        c.SeekClock(10);
        ReplaySample after = c.Sample();
        Assert.Equal(4, after.SegmentIndex);
        Assert.Equal((0, 1), (after.HomeScore, after.AwayScore));
        Assert.Equal(t.Mapping.NodePosition(12), after.Puck);
    }

    [Fact]
    public void GoalPuck_ReachesTheNetBeforeTheEnd()
    {
        ReplayTimeline t = Timeline();
        TimelineSegment goal = t.Segments[3];
        double toNet = RinkPoint.Distance(goal.Puck.Start, t.Mapping.HomeNet) / goal.Puck.Length;
        var c = new ReplayCursor(t);
        c.SeekClock(goal.StartClock + (toNet * goal.Duration));
        ReplaySample s = c.Sample();
        Assert.Equal(t.Mapping.HomeNet.X, s.Puck.X, 9);
        Assert.Equal(t.Mapping.HomeNet.Y, s.Puck.Y, 9);
    }

    [Fact]
    public void StepNext_AndStepPrevious_LandAtTheEndOfEachEvent()
    {
        ReplayTimeline t = Timeline();
        var c = new ReplayCursor(t);
        c.StepNext();
        Assert.Equal((1, 4.0), (c.SegmentIndex, c.Clock));
        c.StepNext();
        Assert.Equal((2, 4.0), (c.SegmentIndex, c.Clock));
        c.StepNext();
        Assert.Equal((3, 10.0), (c.SegmentIndex, c.Clock));
        c.StepPrevious();
        Assert.Equal((2, 4.0), (c.SegmentIndex, c.Clock));
        Assert.Equal(1, c.Sample().Fraction);
        c.SeekEvent(0);
        c.StepPrevious();
        Assert.Equal((0, 0.0), (c.SegmentIndex, c.Clock));
        c.SeekEvent(5);
        c.StepNext();
        Assert.Equal((5, 12.0), (c.SegmentIndex, c.Clock));
        Assert.True(c.IsAtEnd);
    }

    [Fact]
    public void SeekEvent_Throws_OutsideTheTimeline()
    {
        var c = new ReplayCursor(Timeline());
        Assert.Throws<ArgumentOutOfRangeException>(() => c.SeekEvent(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => c.SeekEvent(-1));
    }

    [Fact]
    public void SeekClock_ClampsToTheTimeline()
    {
        var c = new ReplayCursor(Timeline());
        c.SeekClock(100);
        Assert.Equal(12, c.Clock);
        Assert.True(c.IsAtEnd);
        c.SeekClock(-5);
        Assert.Equal(0, c.Clock);
        Assert.Equal(0, c.SegmentIndex);
    }

    [Fact]
    public void Playing_StopsAtTheEnd_AndPlayDoesNotRestart()
    {
        var c = new ReplayCursor(Timeline()) { Speed = 8 };
        c.Play();
        c.Advance(100);
        Assert.False(c.IsPlaying);
        Assert.True(c.IsAtEnd);
        Assert.Equal(12, c.Clock);
        c.Play();
        Assert.False(c.IsPlaying);
        Assert.Equal(0, c.RemainingWallSeconds);
    }

    [Fact]
    public void PauseAndToggle_ControlPlayback()
    {
        var c = new ReplayCursor(Timeline());
        c.TogglePlay();
        Assert.True(c.IsPlaying);
        c.Advance(1);
        c.Pause();
        c.Advance(1);
        Assert.Equal(1, c.Clock);
        c.TogglePlay();
        c.Advance(1);
        Assert.Equal(2, c.Clock);
    }

    [Fact]
    public void WallSeconds_IsDurationOverSpeed()
    {
        ReplayTimeline t = Timeline();
        Assert.Equal(12, t.WallSecondsAt(1));
        Assert.Equal(1.5, t.WallSecondsAt(8));
        var c = new ReplayCursor(t) { Speed = 4 };
        Assert.Equal(3, c.RemainingWallSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Speed_MustBePositiveAndFinite(double speed)
    {
        var c = new ReplayCursor(Timeline());
        Assert.Throws<ArgumentOutOfRangeException>(() => c.Speed = speed);
    }

    [Fact]
    public void Advance_RejectsNegativeTime()
    {
        var c = new ReplayCursor(Timeline());
        Assert.Throws<ArgumentOutOfRangeException>(() => c.Advance(-0.1));
    }
}
