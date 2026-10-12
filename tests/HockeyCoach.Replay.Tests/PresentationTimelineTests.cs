using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests;

/// <summary>The presentation time warp on small synthetic matches (D-088 A, data-schema.md, replay.json, Esitysaika).</summary>
public sealed class PresentationTimelineTests
{
    /// <summary>D-088 A, Q-078: a 45 s shift lasts 15–20 s at 1×.</summary>
    public const double ShiftTargetMin = 15;

    /// <summary>Upper end of the D-088 A shift target.</summary>
    public const double ShiftTargetMax = 20;

    private const int Centre = 27;

    private static readonly PresentationConfig Config = ReplayConfigFile.Read();

    private static TimingSettings T => Config.Timing;

    /// <summary>Stationary skaters: 1 on the centre spot, 2 next to him, 101 on the other side, 102 in the away zone.</summary>
    private static Dictionary<int, int> Still() => SyntheticMatch.OnIce(Centre, 28, 26, 38);

    private static Dictionary<int, int> With(Dictionary<int, int> placement, int id, int node)
    {
        var copy = new Dictionary<int, int>(placement);
        copy[id] = node;
        return copy;
    }

    private static JObject Faceoff() => new() { ["homeCentreId"] = 1, ["awayCentreId"] = 101, ["winner"] = "Home", ["location"] = "NeutralZone" };

    private static JObject Pass(int from, int to) => new() { ["passerId"] = from, ["receiverId"] = to, ["underPressure"] = false, ["succeeded"] = true };

    private static SyntheticMatch Start(Dictionary<int, int>? placement = null) =>
        new SyntheticMatch().Add("Faceoff", 1, 0, Centre, 1, placement ?? Still(), Faceoff());

    [Fact]
    public void Gap_IsSplitIntoDeadTimeOtherActionAndTheWindow_WithTheClockRunningThroughThem()
    {
        PresentationTimeline p = Start()
            .Add("Pass", 1, 10, 28, 2, Still(), Pass(1, 2))
            .Add("Pass", 1, 30, Centre, 1, Still(), Pass(2, 1))
            .Present();

        SegmentTiming first = p.Segments[1];
        Assert.Equal((6.0, 2.0, 2.0), (first.Split.Dead, first.Split.Other, first.Split.Action));
        Assert.Equal(6 / T.DeadCompression, first.Durations.Dead, 12);
        Assert.Equal(2 / T.ActionCompression, first.Durations.Other, 12);
        Assert.Equal(2 / T.ActionCompression, first.Durations.Window, 12);
        Assert.Equal(
            new[] { PresentationPhaseKind.DeadTime, PresentationPhaseKind.OtherAction, PresentationPhaseKind.ActionWindow },
            first.Phases.Select(ph => ph.Kind));
        Assert.Equal(new[] { 0.0, 6.0, 8.0 }, first.Phases.Select(ph => ph.ClockStart));
        Assert.Equal(new[] { 6.0, 8.0, 10.0 }, first.Phases.Select(ph => ph.ClockEnd));
        Assert.Equal(10, p.ClockAt(1, first.EventEnd));
        Assert.Equal(first.WindowStart, first.Phases[2].Start);

        SegmentTiming second = p.Segments[2];
        Assert.Equal((14.0, 4.0, 2.0), (second.Split.Dead, second.Split.Other, second.Split.Action));
        Assert.Equal(14 / T.DeadCompression, second.Durations.Dead, 12);
        Assert.Equal(first.End, second.Start);
        Assert.Equal(30, p.ClockAt(2, second.EventEnd));
    }

    [Fact]
    public void EventAtTheSameSecond_LastsMinEventSeconds_WithTheClockFrozenAtThatSecond()
    {
        PresentationTimeline p = Start()
            .Add("Pass", 1, 10, 28, 2, Still(), Pass(1, 2))
            .Add("Pass", 1, 10, Centre, 1, Still(), Pass(2, 1))
            .Present();

        SegmentTiming s = p.Segments[2];
        Assert.Equal(T.MinEventSeconds, s.Duration, 12);
        PresentationPhase window = Assert.Single(s.Phases);
        Assert.Equal(PresentationPhaseKind.ActionWindow, window.Kind);
        Assert.True(window.IsFrozen);
        Assert.Equal(10, p.ClockAt(2, (s.Start + s.End) / 2));
    }

    [Fact]
    public void MatchStart_FaceoffAtClockZero_LastsMinEventSeconds()
    {
        PresentationTimeline p = Start().Add("Pass", 1, 10, 28, 2, Still(), Pass(1, 2)).Present();
        SegmentTiming s = p.Segments[0];
        Assert.Equal(0, s.Start);
        Assert.Equal(Math.Max(T.MinEventSeconds, Config.Puck.FaceoffDrawDelaySeconds), s.Duration, 12);
        Assert.True((s.Flags & SegmentTimingFlags.PeriodStart) != 0);
        Assert.Equal(0, s.ApproachSeconds);
    }

    [Fact]
    public void PuckFloor_StretchesEveryPhaseOfTheGapInTheSameRatio()
    {
        // Receiver 2 stands at node 2 (row 0): a pass of about 29 m at longPassSpeed outlasts the 2 s / 3 window.
        Dictionary<int, int> far = With(Still(), 2, 2);
        PresentationTimeline p = Start(far).Add("Pass", 1, 10, 2, 2, far, Pass(1, 2)).Present();
        RinkMapping map = p.Timeline.Mapping;

        SegmentTiming s = p.Segments[1];
        double distance = RinkPoint.Distance(map.NodePosition(Centre), map.NodePosition(2));
        Assert.True(distance >= Config.Puck.LongPassMinMetres);
        double floor = distance / Config.Puck.LongPassSpeed;
        Assert.Equal(floor, s.PuckFloorSeconds, 12);
        Assert.Equal(floor, s.Durations.Window, 12);
        GapDurations plain = PresentationTiming.Split(10, 2, p.Timeline.Match.Time, T);
        double ratio = floor / plain.Window;
        Assert.True(ratio > 1);
        Assert.Equal(plain.Dead * ratio, s.Durations.Dead, 12);
        Assert.Equal(plain.Other * ratio, s.Durations.Other, 12);
    }

    [Fact]
    public void SkaterFloor_StretchesTheGap_ToTheFarthestActorOverTopSpeed()
    {
        // The receiver 2 skates four rows (node 28 → 48) during a 4 s gap.
        PresentationTimeline p = Start().Add("Pass", 1, 4, 48, 2, With(Still(), 2, 48), Pass(1, 2)).Present();
        RinkMapping map = p.Timeline.Mapping;

        SegmentTiming s = p.Segments[1];
        double floor = RinkPoint.Distance(map.NodePosition(28), map.NodePosition(48)) / Config.Skaters.TopSpeed;
        Assert.Equal(floor, s.SkaterFloorSeconds, 12);
        Assert.Equal(floor, s.Durations.Total, 9);
    }

    [Fact]
    public void SkaterFloor_IgnoresPlayersWhoAreNotActors()
    {
        // 102 (no actor) skates across the rink; the gap keeps its unstretched length.
        PresentationTimeline p = Start().Add("Pass", 1, 4, 28, 2, With(Still(), 102, 2), Pass(1, 2)).Present();
        SegmentTiming s = p.Segments[1];
        Assert.Equal(0, s.SkaterFloorSeconds);
        Assert.Equal(PresentationTiming.Split(4, 2, p.Timeline.Match.Time, T).Total, s.Durations.Total, 12);
    }

    [Fact]
    public void Goal_WindowIsSlowed_ThenHeldFrozen_AndTheScoreChangesAtTheEndOfTheWindow()
    {
        Dictionary<int, int> afterGoal = Still();
        PresentationTimeline p = Start()
            .Add("Pass", 1, 10, 28, 2, Still(), Pass(1, 2))
            .Add("Shot", 1, 11, 47, null, afterGoal, new JObject { ["shooterId"] = 2, ["stoppedById"] = null, ["outcome"] = "Goal" })
            .Present();

        SegmentTiming s = p.Segments[2];
        Assert.True((s.Flags & SegmentTimingFlags.Goal) != 0);
        double unslowed = Math.Max(1 / T.ActionCompression, s.PuckFloorSeconds);
        Assert.Equal(PresentationTiming.GoalWindow(unslowed, Config.Goal), s.Durations.Window, 12);
        Assert.True(s.Durations.Window >= Config.Goal.SlowMoMinSeconds);
        PresentationPhase hold = s.Phases[^1];
        Assert.Equal(PresentationPhaseKind.GoalHold, hold.Kind);
        Assert.Equal(Config.Goal.HoldSeconds, hold.Duration, 12);
        Assert.True(hold.IsFrozen);
        Assert.Equal(11, hold.ClockStart);

        var c = new ReplayCursor(p);
        c.SeekTime(s.EventEnd - 1e-6);
        Assert.Equal((0, 0), (c.Sample().HomeScore, c.Sample().AwayScore));
        c.SeekTime(s.EventEnd);
        Assert.Equal((1, 0), (c.Sample().HomeScore, c.Sample().AwayScore));
        Assert.Equal(11, c.Clock);
        c.SeekTime(s.End - 1e-6);
        Assert.Equal(11, c.Clock);
    }

    [Fact]
    public void StoppageSeries_HoldsTheStoppage_ChangesInstantly_HoldsTheTimeout_ThenApproachesFrozen()
    {
        Dictionary<int, int> changed = Still();
        changed.Remove(1);
        changed[4] = Centre;
        PresentationTimeline p = Start()
            .Add("Pass", 1, 10, 28, 2, Still(), Pass(1, 2))
            .Add("Stoppage", 1, 12, 28, null, Still(), new JObject { ["reason"] = "Offside" })
            .Add("LineChange", 1, 12, 28, null, changed, new JObject { ["team"] = "Home", ["outgoingIds"] = new JArray(1), ["incomingIds"] = new JArray(4) })
            .Add("Timeout", 1, 12, 28, null, changed, new JObject { ["team"] = "Away" })
            .Add("Faceoff", 1, 14, Centre, 4, changed, new JObject { ["homeCentreId"] = 4, ["awayCentreId"] = 101, ["winner"] = "Home", ["location"] = "NeutralZone" })
            .Present();

        SegmentTiming stoppage = p.Segments[2];
        PresentationPhase hold = stoppage.Phases[^1];
        Assert.Equal(PresentationPhaseKind.StoppageHold, hold.Kind);
        Assert.Equal(T.StoppageHoldSeconds, hold.Duration, 12);
        Assert.True(hold.IsFrozen);
        Assert.False(stoppage.UsesAnticipation);

        SegmentTiming change = p.Segments[3];
        Assert.Equal(0, change.Duration);
        Assert.True((change.Flags & SegmentTimingFlags.InstantLineChange) != 0);
        Assert.True((change.Flags & SegmentTimingFlags.StoppageSeries) != 0);

        SegmentTiming timeout = p.Segments[4];
        PresentationPhase rest = Assert.Single(timeout.Phases);
        Assert.Equal(PresentationPhaseKind.TimeoutHold, rest.Kind);
        Assert.Equal(T.TimeoutHoldSeconds, timeout.Duration, 12);
        Assert.Equal(12, rest.ClockStart);
        Assert.True(rest.IsFrozen);

        SegmentTiming faceoff = p.Segments[5];
        Assert.True((faceoff.Flags & SegmentTimingFlags.FaceoffApproach) != 0);
        PresentationPhase approach = faceoff.Phases[0];
        Assert.Equal(PresentationPhaseKind.FaceoffApproach, approach.Kind);
        Assert.True(approach.IsFrozen);
        Assert.Equal(12, approach.ClockStart);

        // Outgoing 1 skates from the centre spot to the home gate, incoming 4 from the gate to the centre spot.
        RinkMapping map = p.Timeline.Mapping;
        double toGate = RinkPoint.Distance(map.NodePosition(Centre), PresentationTimeline.Gate(map, Config.Bench, ReplayTeam.Home));
        Assert.Equal(PresentationTiming.ApproachSeconds(toGate, T), faceoff.ApproachSeconds, 12);
        Assert.Equal(12, faceoff.ClockStart);
        Assert.Equal(14, p.ClockAt(5, faceoff.EventEnd));
        Assert.Equal(0, faceoff.SkaterFloorSeconds);
        Assert.False(faceoff.UsesAnticipation);
    }

    [Fact]
    public void PeriodEnd_AddsTheBreak_AndTheNextPeriodStartsAtItsPositionsWithoutApproach()
    {
        PresentationTimeline p = Start()
            .Add("Pass", 1, 10, 28, 2, Still(), Pass(1, 2))
            .Add("Stoppage", 1, 1200, 28, null, Still(), new JObject { ["reason"] = "PeriodEnd" })
            .Add("LineChange", 2, 0, Centre, null, Still(), new JObject { ["team"] = "Home", ["outgoingIds"] = new JArray(), ["incomingIds"] = new JArray() })
            .Add("Faceoff", 2, 2, Centre, 1, Still(), Faceoff())
            .Present();

        SegmentTiming end = p.Segments[2];
        Assert.Equal(
            new[] { PresentationPhaseKind.StoppageHold, PresentationPhaseKind.PeriodBreak },
            end.Phases.Skip(end.Phases.Count - 2).Select(ph => ph.Kind));
        Assert.Equal(T.PeriodBreakSeconds, end.Phases[^1].Duration, 12);

        SegmentTiming opening = p.Segments[3];
        Assert.Equal(0, opening.Duration);
        Assert.True((opening.Flags & SegmentTimingFlags.PeriodStart) != 0);
        Assert.True((opening.Flags & SegmentTimingFlags.InstantLineChange) != 0);

        SegmentTiming faceoff = p.Segments[4];
        Assert.Equal(0, faceoff.ApproachSeconds);
        Assert.Equal(1200, faceoff.ClockStart);
        Assert.Equal(2 / T.ActionCompression, faceoff.Durations.Window, 12);
    }

    [Fact]
    public void PeriodFirstEvent_GapIsItsPeriodTime_AndTheClockJumpsForwardToThePeriodStart()
    {
        PresentationTimeline p = Start()
            .Add("Stoppage", 1, 1100, Centre, null, Still(), new JObject { ["reason"] = "PeriodEnd" })
            .Add("Faceoff", 2, 2, Centre, 1, Still(), Faceoff())
            .Present();

        SegmentTiming s = p.Segments[2];
        Assert.Equal(1200, s.ClockStart);
        Assert.Equal(2.0, s.Split.Action);
        Assert.Equal(1200, p.ClockAt(2, s.Start + 1e-9), 6);
        Assert.Equal(1100, p.ClockAt(1, s.Start));
    }

    [Fact]
    public void FlyingLineChange_LastsAtLeastLineChangeSeconds()
    {
        Dictionary<int, int> changed = Still();
        changed.Remove(1);
        changed[4] = Centre;
        PresentationTimeline p = Start()
            .Add("Pass", 1, 10, 28, 2, Still(), Pass(1, 2))
            .Add("LineChange", 1, 10, 28, 2, changed, new JObject { ["team"] = "Home", ["outgoingIds"] = new JArray(1), ["incomingIds"] = new JArray(4) })
            .Present();

        SegmentTiming s = p.Segments[2];
        Assert.True((s.Flags & SegmentTimingFlags.FlyingLineChange) != 0);
        Assert.Equal(T.LineChangeSeconds, s.Duration, 12);
    }

    [Fact]
    public void Cursor_SegmentAfterSeekTime_EqualsTheSegmentReachedByPlaying()
    {
        PresentationTimeline p = Start()
            .Add("Pass", 1, 10, 28, 2, Still(), Pass(1, 2))
            .Add("Shot", 1, 11, 47, null, Still(), new JObject { ["shooterId"] = 2, ["stoppedById"] = null, ["outcome"] = "Goal" })
            .Add("Stoppage", 1, 11, 47, null, Still(), new JObject { ["reason"] = "Goal" })
            .Add("LineChange", 1, 11, 47, null, Still(), new JObject { ["team"] = "Home", ["outgoingIds"] = new JArray(), ["incomingIds"] = new JArray() })
            .Add("Faceoff", 1, 13, Centre, 1, Still(), Faceoff())
            .Present();

        var played = new ReplayCursor(p);
        played.Play();
        while (played.IsPlaying)
        {
            played.Advance(1.0 / 50);
            var seeker = new ReplayCursor(p);
            seeker.SeekTime(played.Time);
            Assert.Equal(played.SegmentIndex, seeker.SegmentIndex);
            Assert.Equal(played.Clock, seeker.Clock);
            Assert.Equal(p.SegmentIndexAt(played.Time), played.SegmentIndex);
        }
    }

    [Fact]
    public void Build_RejectsBenchGatesOutsideTheRink()
    {
        JObject root = JObject.Parse(ReplayConfigFile.Text);
        root["bench"]!["awayGateFromCentreMetres"] = 40;
        PresentationConfig config = PresentationConfigReader.Read(root.ToString());
        ReplayTimeline t = Start().Build();
        Assert.Throws<PresentationConfigException>(() => PresentationTimeline.Build(t, config));
    }
}
