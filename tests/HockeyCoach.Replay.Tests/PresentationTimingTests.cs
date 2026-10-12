using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests;

/// <summary>The pure functions of the presentation time warp (D-088 A, data-schema.md, replay.json, Esitysaika).</summary>
public sealed class PresentationTimingTests
{
    private static readonly TimeCostsData Costs = ReplayJsonReader.Read(new SyntheticMatch()
        .Add("Faceoff", 1, 0, 27, null, SyntheticMatch.OnIce(27, 16, 27, 38), new JObject { ["homeCentreId"] = 1, ["awayCentreId"] = 101 }).Json).Time;

    private static readonly PresentationConfig Config = ReplayConfigFile.Read();

    private static ReplayEvent E(ReplayEventType kind, string? method = null) => new() { Kind = kind, Type = kind.ToString(), Method = method };

    [Theory]
    [InlineData(ReplayEventType.Faceoff, "faceoff")]
    [InlineData(ReplayEventType.Pass, "pass")]
    [InlineData(ReplayEventType.Shot, "shoot")]
    [InlineData(ReplayEventType.DumpIn, "dumpIn")]
    [InlineData(ReplayEventType.Turnover, "skate")]
    [InlineData(ReplayEventType.PuckBattle, "loosePuck")]
    public void ActionCost_UsesTheHeaderCostOfTheEventsAction(ReplayEventType kind, string key)
    {
        Assert.Equal(Costs.Action(key), PresentationTiming.ActionCost(E(kind), E(ReplayEventType.Pass), Costs));
    }

    [Theory]
    [InlineData(ReplayEventType.Stoppage)]
    [InlineData(ReplayEventType.LineChange)]
    [InlineData(ReplayEventType.Timeout)]
    public void ActionCost_IsZero_ForStoppagesChangesAndTimeouts(ReplayEventType kind)
    {
        Assert.Equal(0, PresentationTiming.ActionCost(E(kind), E(ReplayEventType.Pass), Costs));
    }

    [Fact]
    public void ActionCost_PuckBattle_AddsReboundAfterAShot_AndSystemStepAfterABattle()
    {
        double loose = Costs.Action("loosePuck");
        Assert.Equal(loose + Costs.Action("rebound"), PresentationTiming.ActionCost(E(ReplayEventType.PuckBattle), E(ReplayEventType.Shot), Costs));
        Assert.Equal(loose + Costs.Action("systemStep"), PresentationTiming.ActionCost(E(ReplayEventType.PuckBattle), E(ReplayEventType.PuckBattle), Costs));
        Assert.Equal(loose, PresentationTiming.ActionCost(E(ReplayEventType.PuckBattle), null, Costs));
    }

    [Fact]
    public void ActionCost_ZoneEntry_IsSkateWhenCarried_AndZeroWhenPassed()
    {
        Assert.Equal(Costs.Action("skate"), PresentationTiming.ActionCost(E(ReplayEventType.ControlledZoneEntry, "Carry"), null, Costs));
        Assert.Equal(0, PresentationTiming.ActionCost(E(ReplayEventType.ControlledZoneEntry, "Pass"), null, Costs));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5.9, 0)]
    [InlineData(6, 6)]
    [InlineData(13.9, 6)]
    [InlineData(14, 14)]
    [InlineData(40, 14)]
    public void DeadSeconds_IsSetupPlusRegroup_SetupOrNothing(double remainder, double expected)
    {
        Assert.Equal(6, Costs.SetupSeconds);
        Assert.Equal(8, Costs.RegroupSeconds);
        Assert.Equal(expected, PresentationTiming.DeadSeconds(remainder, Costs.SetupSeconds, Costs.RegroupSeconds));
    }

    [Fact]
    public void SplitGame_CapsTheActionAtTheGap()
    {
        GameSplit s = PresentationTiming.SplitGame(1, 2, Costs);
        Assert.Equal((0.0, 0.0, 1.0), (s.Dead, s.Other, s.Action));
    }

    [Fact]
    public void Split_CompressesDeadAndActionSeparately()
    {
        GapDurations d = PresentationTiming.Split(20, 2, Costs, Config.Timing);
        Assert.Equal(14 / Config.Timing.DeadCompression, d.Dead, 12);
        Assert.Equal(4 / Config.Timing.ActionCompression, d.Other, 12);
        Assert.Equal(2 / Config.Timing.ActionCompression, d.Window, 12);
    }

    [Fact]
    public void WithWindowAtLeast_StretchesAllPhasesInTheSameRatio()
    {
        var d = new GapDurations(1, 2, 0.5);
        GapDurations s = d.WithWindowAtLeast(1.5);
        Assert.Equal((3.0, 6.0, 1.5), (s.Dead, s.Other, s.Window));
        Assert.Equal(d, d.WithWindowAtLeast(0.5));
    }

    [Fact]
    public void WithWindowAtLeast_GivesAnEmptyWindowTheFloor()
    {
        GapDurations s = new GapDurations(1, 2, 0).WithWindowAtLeast(0.7);
        Assert.Equal((1.0, 2.0, 0.7), (s.Dead, s.Other, s.Window));
    }

    [Fact]
    public void WithTotalAtLeast_StretchesAllPhasesInTheSameRatio_OrFillsAnEmptyGapWithTheWindow()
    {
        GapDurations s = new GapDurations(1, 1, 2).WithTotalAtLeast(8);
        Assert.Equal((2.0, 2.0, 4.0), (s.Dead, s.Other, s.Window));
        GapDurations empty = new GapDurations(0, 0, 0).WithTotalAtLeast(0.4);
        Assert.Equal((0.0, 0.0, 0.4), (empty.Dead, empty.Other, empty.Window));
        Assert.Equal(new GapDurations(1, 1, 2), new GapDurations(1, 1, 2).WithTotalAtLeast(4));
    }

    [Fact]
    public void ApproachSeconds_IsDisplacementOverSpeed_Clamped()
    {
        TimingSettings t = Config.Timing;
        Assert.Equal(t.FaceoffApproachMinSeconds, PresentationTiming.ApproachSeconds(0, t));
        Assert.Equal(t.FaceoffApproachMaxSeconds, PresentationTiming.ApproachSeconds(1000, t));
        double inside = (t.FaceoffApproachMinSeconds + t.FaceoffApproachMaxSeconds) / 2;
        Assert.Equal(inside, PresentationTiming.ApproachSeconds(inside * t.FaceoffApproachSpeed, t), 12);
    }

    [Fact]
    public void GoalWindow_DividesBySlowMoRate_WithAMinimum()
    {
        GoalSettings g = Config.Goal;
        Assert.Equal(g.SlowMoMinSeconds, PresentationTiming.GoalWindow(0, g));
        double long_ = g.SlowMoMinSeconds * 2;
        Assert.Equal(long_ / g.SlowMoRate, PresentationTiming.GoalWindow(long_, g), 12);
    }

    [Fact]
    public void Phase_ClockRunsLinearly_AndFrozenPhasesHoldTheClock()
    {
        var running = new PresentationPhase(PresentationPhaseKind.OtherAction, 0, 10, 12, 100, 106);
        Assert.Equal(100, running.ClockAt(9));
        Assert.Equal(103, running.ClockAt(11), 12);
        Assert.Equal(106, running.ClockAt(12));
        Assert.Equal(11, running.TimeAt(103), 12);
        var frozen = new PresentationPhase(PresentationPhaseKind.StoppageHold, 0, 12, 13, 106, 106);
        Assert.True(frozen.IsFrozen);
        Assert.Equal(106, frozen.ClockAt(12.5));
        Assert.Equal(12, frozen.TimeAt(106));
    }

    [Fact]
    public void Actors_AreTheNamedPlayersAndTheCarriersBeforeAndAfter()
    {
        var e = new ReplayEvent
        {
            Kind = ReplayEventType.PuckBattle,
            AttackerIds = new List<int> { 5, 2 },
            DefenderIds = new List<int> { 101 },
            Placement = new PlacementData { PuckCarrierId = 7 },
        };
        var previous = new ReplayEvent { Kind = ReplayEventType.Pass, PasserId = 99, Placement = new PlacementData { PuckCarrierId = 8 } };
        Assert.Equal(new[] { 2, 5, 7, 8, 101 }, PresentationTimeline.Actors(e, previous));
    }

    [Fact]
    public void Gate_IsOnTheBenchSideOfTheCentreLine()
    {
        RinkMapping map = new SyntheticMatch().Add("Faceoff", 1, 0, 27, null, SyntheticMatch.OnIce(27, 16, 27, 38)).Build().Mapping;
        RinkPoint home = PresentationTimeline.Gate(map, Config.Bench, ReplayTeam.Home);
        RinkPoint away = PresentationTimeline.Gate(map, Config.Bench, ReplayTeam.Away);
        Assert.Equal(map.CentreLineX + Config.Bench.HomeGateFromCentreMetres, home.X, 12);
        Assert.Equal(map.CentreLineX + Config.Bench.AwayGateFromCentreMetres, away.X, 12);
        Assert.Equal(Config.Bench.GateYMetres, home.Y);
    }

    [Fact]
    public void NetInside_IsBehindTheGoalLine_AndMissPointBesideTheNet()
    {
        RinkMapping map = new SyntheticMatch().Add("Faceoff", 1, 0, 27, null, SyntheticMatch.OnIce(27, 16, 27, 38)).Build().Mapping;
        RinkPoint homeNet = PuckGeometry.NetInside(map, ReplayTeam.Home, 0.5);
        Assert.Equal(map.HomeGoalLineX - 0.5, homeNet.X, 12);
        RinkPoint awayNet = PuckGeometry.NetInside(map, ReplayTeam.Away, 0.5);
        Assert.Equal(map.AwayGoalLineX + 0.5, awayNet.X, 12);
        double offset = (PuckGeometry.NetWidthMetres / 2) + 1.5;

        // Puck node 43 is on lane 3 (> centre lane 2): the far side; node 41 lane 1: the y = 0 side.
        Assert.Equal(map.AwayNet.Y + offset, PuckGeometry.MissPoint(map, ReplayTeam.Away, 43, 27, 1.5).Y, 12);
        Assert.Equal(map.AwayNet.Y - offset, PuckGeometry.MissPoint(map, ReplayTeam.Away, 41, 27, 1.5).Y, 12);

        // Centre lane puck: the shooter's side; both on the centre lane: the y = 0 side.
        Assert.Equal(map.AwayNet.Y + offset, PuckGeometry.MissPoint(map, ReplayTeam.Away, 42, 39, 1.5).Y, 12);
        Assert.Equal(map.AwayNet.Y - offset, PuckGeometry.MissPoint(map, ReplayTeam.Away, 42, 27, 1.5).Y, 12);
        Assert.Equal(map.AwayGoalLineX, PuckGeometry.MissPoint(map, ReplayTeam.Away, 42, 27, 1.5).X, 12);
    }
}
