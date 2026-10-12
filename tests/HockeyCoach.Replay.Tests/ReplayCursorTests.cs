using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests;

/// <summary>The playback cursor on presentation time (D-088 A).</summary>
public sealed class ReplayCursorTests
{
    /// <summary>
    /// 0: faceoff at 0 s; 1: pass 1 → 2, 0–4 s; 2: turnover at 4 s; 3: away goal, 4–10 s; 4: stoppage at 10 s;
    /// 5: faceoff at 12 s.
    /// </summary>
    private static PresentationTimeline Timeline() => new SyntheticMatch()
        .Add("Faceoff", 1, 0, 27, 1, SyntheticMatch.OnIce(27, 16, 27, 38), new JObject { ["homeCentreId"] = 1, ["awayCentreId"] = 101, ["winner"] = "Home", ["location"] = "NeutralZone" })
        .Add("Pass", 1, 4, 37, 2, SyntheticMatch.OnIce(31, 37, 27, 38), new JObject { ["passerId"] = 1, ["receiverId"] = 2, ["underPressure"] = false, ["succeeded"] = true })
        .Add("Turnover", 1, 4, 37, 102, SyntheticMatch.OnIce(31, 37, 27, 37), new JObject { ["loserId"] = 2, ["takerId"] = 102, ["nodeId"] = 37 })
        .Add("Shot", 1, 10, 12, 101, SyntheticMatch.OnIce(31, 37, 12, 37), new JObject { ["shooterId"] = 101, ["stoppedById"] = null, ["outcome"] = "Goal" })
        .Add("Stoppage", 1, 10, 12, 101, SyntheticMatch.OnIce(31, 37, 12, 37), new JObject { ["reason"] = "Goal" })
        .Add("Faceoff", 1, 12, 27, 1, SyntheticMatch.OnIce(27, 16, 27, 38), new JObject { ["homeCentreId"] = 1, ["awayCentreId"] = 101, ["winner"] = "Home", ["location"] = "NeutralZone" })
        .Present();

    [Fact]
    public void NewCursor_IsPausedAtTheStart_At1x()
    {
        var c = new ReplayCursor(Timeline());
        Assert.False(c.IsPlaying);
        Assert.Equal(0, c.Time);
        Assert.Equal(0, c.Clock);
        Assert.Equal(0, c.SegmentIndex);
        Assert.Equal(1, c.Speed);
    }

    [Fact]
    public void Advance_DoesNothing_WhilePaused()
    {
        var c = new ReplayCursor(Timeline());
        c.Advance(3);
        Assert.Equal(0, c.Time);
    }

    [Fact]
    public void Advance_MovesThePresentationTimeBySpeedTimesRealSeconds()
    {
        PresentationTimeline p = Timeline();
        var c = new ReplayCursor(p) { Speed = 2 };
        c.Play();
        c.Advance(0.75);
        Assert.Equal(1.5, c.Time);
        Assert.Equal(p.ClockAt(1.5), c.Clock);
        Assert.Equal(1.5, c.Sample().Time);
    }

    [Fact]
    public void Sample_InterpolatesOverTheSegmentsMotion_AndTheClockFollowsTheWarp()
    {
        PresentationTimeline p = Timeline();
        SegmentTiming pass = p.Segments[1];
        var c = new ReplayCursor(p);
        c.SeekTime((pass.Start + pass.EventEnd) / 2);
        ReplaySample s = c.Sample();
        Assert.Equal(1, s.SegmentIndex);
        Assert.Equal(0.5, s.Fraction, 12);
        MotionSample motion = p.Motion.Sample(1, c.Time);
        Assert.Equal(motion.Players.Select(x => (x.Id, x.Position)), s.Players.Select(x => (x.Id, x.Position)));
        Assert.Null(s.PuckCarrierId);
        Assert.InRange(s.Clock, 0, 4);
        Assert.Equal(pass.ClockAt(c.Time), s.Clock);
    }

    [Fact]
    public void ReachingTheEndOfAWindow_ShowsTheEventsClockAndState()
    {
        PresentationTimeline p = Timeline();
        var c = new ReplayCursor(p);
        c.Play();
        c.Advance(p.Segments[2].EventEnd);
        ReplaySample s = c.Sample();
        Assert.Equal(2, s.SegmentIndex);
        Assert.Equal(4, s.Clock);
        Assert.Equal(102, s.PuckCarrierId);
    }

    [Fact]
    public void Score_ChangesWhenTheGoalWindowEnds()
    {
        PresentationTimeline p = Timeline();
        var c = new ReplayCursor(p);
        c.SeekClock(7);
        ReplaySample mid = c.Sample();
        Assert.Equal(3, mid.SegmentIndex);
        Assert.Equal(7, mid.Clock, 9);
        Assert.Equal((0, 0), (mid.HomeScore, mid.AwayScore));
        c.SeekClock(10);
        ReplaySample after = c.Sample();
        Assert.Equal(4, after.SegmentIndex);
        Assert.Equal((0, 1), (after.HomeScore, after.AwayScore));
        Assert.Equal(p.Timeline.Mapping.NodePosition(12), after.Puck);
    }

    [Fact]
    public void StepNext_AndStepPrevious_LandAtTheEndOfEachEventsWindow()
    {
        PresentationTimeline p = Timeline();
        var c = new ReplayCursor(p);
        c.StepNext();
        Assert.Equal((1, 4.0, p.Segments[1].EventEnd), (c.SegmentIndex, c.Clock, c.Time));
        c.StepNext();
        Assert.Equal((2, 4.0), (c.SegmentIndex, c.Clock));
        c.StepNext();
        Assert.Equal((3, 10.0), (c.SegmentIndex, c.Clock));
        Assert.True(c.Time < p.Segments[3].End, "the goal hold follows the end of the window");
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
    public void SeekClockAndSeekTime_ClampToTheMatch()
    {
        PresentationTimeline p = Timeline();
        var c = new ReplayCursor(p);
        c.SeekClock(100);
        Assert.Equal(12, c.Clock);
        Assert.True(c.IsAtEnd);
        c.SeekClock(-5);
        Assert.Equal(0, c.Clock);
        Assert.Equal(0, c.SegmentIndex);
        c.SeekTime(1e9);
        Assert.Equal(p.Duration, c.Time);
        Assert.True(c.IsAtEnd);
        c.SeekTime(-1);
        Assert.Equal(0, c.Time);
        Assert.Throws<ArgumentOutOfRangeException>(() => c.SeekTime(double.NaN));
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
        Assert.Equal(1, c.Time);
        c.TogglePlay();
        c.Advance(1);
        Assert.Equal(2, c.Time);
    }

    [Fact]
    public void WallSeconds_IsThePresentationDurationOverSpeed()
    {
        PresentationTimeline p = Timeline();
        Assert.Equal(p.Duration / 8, p.WallSecondsAt(8));
        var c = new ReplayCursor(p) { Speed = 4 };
        Assert.Equal(p.Duration / 4, c.RemainingWallSeconds);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.WallSecondsAt(0));
    }

    [Fact]
    public void Clock_NeverGoesBackwards_AndShowsTheTrueMatchTime()
    {
        var c = new ReplayCursor(Timeline());
        c.Play();
        double last = 0;
        while (c.IsPlaying)
        {
            c.Advance(1.0 / 60);
            Assert.True(c.Clock >= last);
            last = c.Clock;
        }

        Assert.Equal(12, last);
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
