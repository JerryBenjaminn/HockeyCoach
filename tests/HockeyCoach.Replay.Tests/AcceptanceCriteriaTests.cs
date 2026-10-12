using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.State;

namespace HockeyCoach.Replay.Tests;

/// <summary>
/// The acceptance criteria 1–9 of the Unity replay, stage 1 (D-087, <c>docs/handoff.md</c>), on the seed 42 match. Since
/// D-088 the cursor plays presentation time (<see cref="PresentationTimeline"/>) and the clock shows the true match time.
/// </summary>
[Collection(Seed42Collection.Name)]
public sealed class AcceptanceCriteriaTests
{
    private const int OnIcePlayers = 12;
    private readonly Seed42Match _m;

    public AcceptanceCriteriaTests(Seed42Match m)
    {
        _m = m;
    }

    private ReplayTimeline Timeline => _m.Timeline;

    private PresentationTimeline Presentation => _m.Presentation;

    private IReadOnlyList<TimelineSegment> Segments => _m.Timeline.Segments;

    [Fact]
    public void Seed42_TimelineHasOneSegmentPerEvent_AndMatchesTheSimulatorLog()
    {
        Assert.Equal(_m.Result.Log.Count, Segments.Count);
        Assert.Equal(_m.Match.Events.Count, Segments.Count);
        Assert.False(_m.Match.Stalled);
    }

    [Fact]
    public void Criterion1_All12OnIcePlayersArePresent_InEverySegment()
    {
        foreach (TimelineSegment s in Segments)
        {
            Assert.Equal(OnIcePlayers, s.Players.Count);
            Assert.Equal(OnIcePlayers, s.Players.Select(p => p.Id).Distinct().Count());
            Assert.Equal(2, s.Players.Count(p => p.IsGoalie));
            Assert.Equal(6, s.Players.Count(p => p.Team == ReplayTeam.Home));
        }
    }

    [Fact]
    public void Criterion1_PlayersStandOnTheirPlacementNodes_OutsideLineChanges()
    {
        foreach (TimelineSegment s in Segments.Where(s => s.Type != ReplayEventType.LineChange))
        {
            var placed = s.Event.Placement.Players.ToDictionary(p => p.Id, p => p.NodeId);
            Assert.Equal(placed.Keys.OrderBy(i => i), s.Players.Select(p => p.Id));
            foreach (SegmentPlayer p in s.Players)
            {
                Assert.Equal(placed[p.Id], p.NodeId);
                Assert.Equal(Timeline.Mapping.NodePosition(p.NodeId), p.To);
            }
        }
    }

    [Fact]
    public void Criterion1_EverySampleOfTheWholeMatchShows12Players()
    {
        var cursor = new ReplayCursor(Presentation) { Speed = 8 };
        cursor.Play();
        int samples = 0;
        while (cursor.IsPlaying)
        {
            cursor.Advance(0.5);
            Assert.Equal(OnIcePlayers, cursor.Sample().Players.Count);
            samples++;
        }

        Assert.True(samples > 0);
    }

    [Fact]
    public void Criterion2_ScoreAndGoalTimes_EqualTheMatchResultAndJsonGoals()
    {
        Assert.Equal(_m.Result.HomeGoals, Timeline.HomeScore);
        Assert.Equal(_m.Result.AwayGoals, Timeline.AwayScore);
        Assert.Equal(_m.Match.HomeGoals, Timeline.HomeScore);
        Assert.Equal(_m.Match.AwayGoals, Timeline.AwayScore);
        IReadOnlyList<GoalNote> notes = _m.Result.State.GoalNotes;
        Assert.True(notes.Count >= 2, "seed 42 should have goals for the check to mean something");
        Assert.Equal(notes.Count, Timeline.Goals.Count);
        Assert.Equal(_m.Match.Goals.Count, Timeline.Goals.Count);
        for (int i = 0; i < notes.Count; i++)
        {
            TimelineGoal g = Timeline.Goals[i];
            Assert.Equal(notes[i].Period, g.Period);
            Assert.Equal(notes[i].Time, g.Time);
            Assert.Equal(notes[i].Team == TeamSide.Home ? ReplayTeam.Home : ReplayTeam.Away, g.Team);
            Assert.Equal(notes[i].ShooterId, g.ShooterId);
            Assert.Equal(_m.Match.Goals[i].Period, g.Period);
            Assert.Equal(_m.Match.Goals[i].Time, g.Time);
            Assert.Equal(_m.Match.Goals[i].Team, g.Team.ToString());
        }
    }

    [Fact]
    public void Criterion2_CursorShowsTheScoreChange_AtTheGoalTime()
    {
        var cursor = new ReplayCursor(Presentation);
        int home = 0;
        int away = 0;
        foreach (TimelineGoal goal in Timeline.Goals)
        {
            cursor.SeekClock(goal.Clock - 0.5);
            ReplaySample before = cursor.Sample();
            Assert.Equal((home, away), (before.HomeScore, before.AwayScore));
            if (goal.Team == ReplayTeam.Home)
            {
                home++;
            }
            else
            {
                away++;
            }

            cursor.SeekClock(goal.Clock);
            ReplaySample after = cursor.Sample();
            Assert.Equal((home, away), (after.HomeScore, after.AwayScore));
            Assert.Equal(goal.Period, after.Period);
            Assert.Equal(goal.Time, after.PeriodTime, 9);
        }
    }

    [Fact]
    public void Criterion3_PassesAndShots_StartAtTheActorAndEndAtThePlacementPuckNode_ExceptGoalsInTheNet()
    {
        // D-088 E, Q-079: passes and shots end at the placement, goals in the net. The grid path still names the
        // placement node; the drawn flight of a goal ends inside the net (PuckMotionTests, Criterion3).
        RinkMapping map = Timeline.Mapping;
        int checkedSegments = 0;
        for (int i = 1; i < Segments.Count; i++)
        {
            TimelineSegment s = Segments[i];
            int? actor = s.Type == ReplayEventType.Pass ? s.Event.PasserId : s.Type == ReplayEventType.Shot ? s.Event.ShooterId : null;
            if (actor == null)
            {
                continue;
            }

            SegmentPlayer before = Segments[i - 1].Players.Single(p => p.Id == actor.Value);
            Assert.Equal(before.NodeId, s.Puck.StartNodeId);
            Assert.Equal(before.To, s.Puck.Start);
            Assert.Equal(s.Event.Placement.PuckNodeId, s.Puck.EndNodeId);
            Assert.Equal(Timeline.Mapping.NodePosition(s.Event.Placement.PuckNodeId), s.Puck.End);
            Assert.Equal(s.Puck.Start, s.Puck.PositionAt(0));
            Assert.Equal(s.Puck.End, s.Puck.PositionAt(1));
            PuckPlan flight = Presentation.Motion.Puck.Plans[i];
            if (s.Event.Outcome == "Goal" && s.Type == ReplayEventType.Shot)
            {
                ReplayTeam conceding = s.ScoringTeam == ReplayTeam.Home ? ReplayTeam.Away : ReplayTeam.Home;
                Assert.Equal(PuckGeometry.NetInside(map, conceding, _m.Config.Goal.NetInsideMetres), flight.EndPoint);
                Assert.True(flight.InNet);
            }
            else if (s.Event.Placement.PuckCarrierId == null)
            {
                Assert.Equal(map.NodePosition(s.Event.Placement.PuckNodeId), flight.EndPoint);
            }
            else
            {
                Assert.Equal(s.Event.Placement.PuckCarrierId, flight.EndCarrierId);
            }

            checkedSegments++;
        }

        Assert.Equal(_m.Match.Events.Count(e => e.Type is "Pass" or "Shot"), checkedSegments);
    }

    [Fact]
    public void Criterion4_FaceoffsHappenAtFaceoffSpots_UnderBothCentres()
    {
        var spotNodes = _m.Match.Rink.FaceoffSpots.Select(s => s.NodeId).ToHashSet();
        List<TimelineSegment> faceoffs = Segments.Where(s => s.Type == ReplayEventType.Faceoff).ToList();
        Assert.NotEmpty(faceoffs);
        foreach (TimelineSegment s in faceoffs)
        {
            Assert.NotNull(s.FaceoffSpot);
            Assert.Contains(s.Puck.StartNodeId, spotNodes);
            Assert.Equal(s.FaceoffSpot!.NodeId, s.Puck.StartNodeId);
            Assert.Equal(s.FaceoffSpot.Position, s.Puck.Start);
            Assert.Equal(s.FaceoffSpot.NodeId, s.Players.Single(p => p.Id == s.Event.HomeCentreId).NodeId);
            Assert.Equal(s.FaceoffSpot.NodeId, s.Players.Single(p => p.Id == s.Event.AwayCentreId).NodeId);
        }
    }

    [Fact]
    public void Criterion5_StoppedShots_GoThroughTheStopper()
    {
        List<TimelineSegment> stopped = Segments.Where(s => s.Type == ReplayEventType.Shot && s.Event.StoppedById.HasValue).ToList();
        Assert.Contains(stopped, s => s.Event.Outcome == "Blocked");
        foreach (TimelineSegment s in stopped)
        {
            SegmentPlayer stopper = s.Players.Single(p => p.Id == s.Event.StoppedById);
            Assert.Equal(stopper.NodeId, s.Puck.ViaNodeId);
            Assert.Equal(stopper.To, s.Puck.Via);
            Assert.NotEqual(Timeline.FindPlayer(s.Event.ShooterId!.Value)!.Team, stopper.Team.ToString());
        }
    }

    [Fact]
    public void Criterion5_GoalsGoIntoTheNetOfTheConcedingTeam()
    {
        List<TimelineSegment> goals = Segments.Where(s => s.Type == ReplayEventType.Shot && s.Event.Outcome == "Goal").ToList();
        Assert.Equal(Timeline.Goals.Count, goals.Count);
        Assert.Contains(goals, s => s.ScoringTeam == ReplayTeam.Home);
        Assert.Contains(goals, s => s.ScoringTeam == ReplayTeam.Away);
        RinkMapping map = Timeline.Mapping;
        foreach (TimelineSegment s in goals)
        {
            ReplayTeam scorer = s.ScoringTeam!.Value;
            ReplayTeam conceding = scorer == ReplayTeam.Home ? ReplayTeam.Away : ReplayTeam.Home;
            Assert.Equal(map.NetOf(conceding), s.Puck.Via);
            Assert.Equal(map.NetNodeOf(conceding), s.Puck.ViaNodeId);

            // The conceding team's goalie stands in that net; the home team scores at the +x end.
            SegmentPlayer goalie = s.Players.Single(p => p.IsGoalie && p.Team == conceding);
            Assert.Equal(map.NetNodeOf(conceding), goalie.NodeId);
            double goalLine = scorer == ReplayTeam.Home ? map.AwayGoalLineX : map.HomeGoalLineX;
            Assert.Equal(goalLine, s.Puck.Via!.Value.X, 9);
            Assert.Equal(map.WidthMetres / 2, s.Puck.Via.Value.Y, 9);
        }
    }

    [Fact]
    public void Criterion6_LineChanges_RemoveOutgoingAndAddIncoming()
    {
        int changes = 0;
        for (int i = 1; i < Segments.Count; i++)
        {
            TimelineSegment s = Segments[i];
            if (s.Type != ReplayEventType.LineChange)
            {
                Assert.Empty(s.Leaving);
                Assert.Empty(s.Entering);
                continue;
            }

            var before = Segments[i - 1].Players.Select(p => p.Id).ToHashSet();
            var expected = before.Except(s.Event.OutgoingIds!).Union(s.Event.IncomingIds!).OrderBy(id => id);
            Assert.Equal(expected, s.Players.Select(p => p.Id));
            Assert.Equal(s.Event.OutgoingIds!.OrderBy(id => id), s.Leaving);
            Assert.Equal(s.Event.IncomingIds!.OrderBy(id => id), s.Entering);
            changes++;
        }

        Assert.Equal(_m.Match.Events.Count(e => e.Type == "LineChange"), changes);
    }

    [Fact]
    public void Criterion6_AfterEachGroupOfChanges_OnIceSetEqualsThePlacement()
    {
        for (int i = 0; i < Segments.Count; i++)
        {
            bool lastOfGroup = i == Segments.Count - 1 || Segments[i + 1].Type != ReplayEventType.LineChange || Segments[i + 1].EndClock != Segments[i].EndClock;
            if (Segments[i].Type == ReplayEventType.LineChange && lastOfGroup)
            {
                Assert.Equal(Segments[i].Event.Placement.Players.Select(p => p.Id).OrderBy(id => id), Segments[i].Players.Select(p => p.Id));
            }
        }
    }

    [Fact]
    public void Criterion7_ClockNeverGoesBackwards_InSegments()
    {
        Assert.Equal(0, Timeline.StartClock);
        for (int i = 0; i < Segments.Count; i++)
        {
            Assert.True(Segments[i].Duration >= 0);
            if (i > 0)
            {
                Assert.Equal(Segments[i - 1].EndClock, Segments[i].StartClock);
            }
        }
    }

    [Fact]
    public void Criterion7_ClockNeverGoesBackwards_WhilePlaying()
    {
        var cursor = new ReplayCursor(Presentation) { Speed = 4 };
        cursor.Play();
        ReplaySample last = cursor.Sample();
        while (cursor.IsPlaying)
        {
            cursor.Advance(1.0 / 30);
            ReplaySample now = cursor.Sample();
            Assert.True(now.Time >= last.Time);
            Assert.True(now.Clock >= last.Clock);
            Assert.True(now.SegmentIndex >= last.SegmentIndex);
            Assert.True(now.Period > last.Period || (now.Period == last.Period && now.PeriodTime >= last.PeriodTime));
            Assert.True(now.HomeScore >= last.HomeScore && now.AwayScore >= last.AwayScore);
            last = now;
        }

        Assert.Equal(Segments.Count - 1, last.SegmentIndex);
        Assert.Equal(Timeline.EndClock, last.Clock);
    }

    [Fact]
    public void Criterion8_SameCalls_GiveIdenticalSamples()
    {
        PresentationTimeline second = PresentationTimeline.Build(TimelineBuilder.Build(ReplayJsonReader.Read(_m.Json)), ReplayConfigFile.Read());
        var a = new ReplayCursor(Presentation);
        var b = new ReplayCursor(second);
        var script = new Action<ReplayCursor>[]
        {
            c => c.Play(),
            c => c.Advance(0.37),
            c => c.Speed = 8,
            c => c.Advance(1.0 / 60),
            c => c.Advance(2.5),
            c => c.StepNext(),
            c => c.StepNext(),
            c => c.StepPrevious(),
            c => c.Speed = 2,
            c => c.Advance(0.1),
            c => c.Pause(),
            c => c.Advance(5),
            c => c.SeekEvent(400),
            c => c.TogglePlay(),
            c => c.Advance(3.3),
            c => c.SeekClock(1999.25),
            c => c.SeekTime(812.3),
            c => c.Advance(0.75),
            c => c.SeekEvent(Segments.Count - 2),
            c => c.Speed = 64,
            c => c.Advance(10),
        };
        foreach (Action<ReplayCursor> step in script)
        {
            step(a);
            step(b);
            AssertSame(a.Sample(), b.Sample());
            Assert.Equal(a.IsPlaying, b.IsPlaying);
        }

        for (int i = 0; i < 200; i++)
        {
            a.StepPrevious();
            b.StepPrevious();
            AssertSame(a.Sample(), b.Sample());
        }
    }

    [Fact]
    public void Criterion9_WholeMatchAt8x_TakesTheShiftTargetTimesTheMatchOverEight()
    {
        const double speed = 8;
        const double frame = 1.0 / 60;
        double matchSeconds = _m.Match.PeriodCount * _m.Match.PeriodSeconds;
        Assert.Equal(matchSeconds, Timeline.EndClock);
        Assert.Equal(Presentation.Duration / speed, Presentation.WallSecondsAt(speed), 9);

        var cursor = new ReplayCursor(Presentation) { Speed = speed };
        Assert.Equal(Presentation.Duration / speed, cursor.RemainingWallSeconds, 9);
        cursor.Play();
        int frames = 0;
        while (cursor.IsPlaying)
        {
            cursor.Advance(frame);
            frames++;
        }

        double wall = frames * frame;
        Assert.True(cursor.IsAtEnd);
        Assert.Equal(Timeline.EndClock, cursor.Clock);
        Assert.InRange(wall, Presentation.Duration / speed, (Presentation.Duration / speed) + frame);

        // D-088 A, Q-078: a 45 s shift lasts 15–20 s at 1×, so the whole match at 8× lasts
        // matchSeconds / 45 × (15–20 s) / 8 = 2.5–3.3 min (data-schema.md, Mitoitus: 2.6 min on seed 42).
        Assert.InRange(wall, matchSeconds / 45 * PresentationTimelineTests.ShiftTargetMin / speed, matchSeconds / 45 * PresentationTimelineTests.ShiftTargetMax / speed);
    }

    private static void AssertSame(ReplaySample a, ReplaySample b)
    {
        Assert.Equal(a.SegmentIndex, b.SegmentIndex);
        Assert.Equal(a.Fraction, b.Fraction);
        Assert.Equal(a.Clock, b.Clock);
        Assert.Equal(a.Period, b.Period);
        Assert.Equal(a.PeriodTime, b.PeriodTime);
        Assert.Equal(a.HomeScore, b.HomeScore);
        Assert.Equal(a.AwayScore, b.AwayScore);
        Assert.Equal(a.Puck, b.Puck);
        Assert.Equal(a.PuckCarrierId, b.PuckCarrierId);
        Assert.Equal(a.Players.Count, b.Players.Count);
        for (int i = 0; i < a.Players.Count; i++)
        {
            Assert.Equal(a.Players[i].Id, b.Players[i].Id);
            Assert.Equal(a.Players[i].Team, b.Players[i].Team);
            Assert.Equal(a.Players[i].Position, b.Players[i].Position);
        }
    }
}
