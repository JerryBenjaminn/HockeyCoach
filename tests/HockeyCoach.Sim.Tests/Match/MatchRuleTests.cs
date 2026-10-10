using HockeyCoach.AI;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Shift;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>Match rules: period boundary (O-1), faceoff spots (O-3), the default coach (O-4), transitions (O-7), familiarity (O-11).</summary>
public class MatchRuleTests
{
    private static ShiftSetup At(double startTime, TransitionInstruction transition = TransitionInstruction.Rush)
    {
        ShiftSetup s = ShiftTestData.Setup();
        TeamShiftSetup Plan(TeamShiftSetup t) => new(t.Skaters, t.Goalie, new ShiftPlan(t.Plan.Plays, t.Plan.System, transition, SystemModeInstructions.Default));
        return new ShiftSetup(s.Rink, s.Tuning, Plan(s.Home), Plan(s.Away), "center", 1, startTime, s.MaxSteps);
    }

    [Fact]
    public void Segment_EndsThePeriodBeforeASequenceThatWouldCrossIt()
    {
        ShiftResult result = ShiftTestData.Run(1, At(1199.0));

        var stoppage = Assert.IsType<StoppageEvent>(Assert.Single(result.Log.Events));
        Assert.Equal(StoppageReason.PeriodEnd, stoppage.Reason);
        Assert.Equal(1200.0, stoppage.Context.Time);
        Assert.Equal(ShiftEndReason.PeriodEnd, result.EndReason);
    }

    [Fact]
    public void Segment_RunsASequenceThatEndsExactlyAtTheBuzzer()
    {
        ShiftResult result = ShiftTestData.Run(1, At(1198.0));

        Assert.IsType<FaceoffEvent>(result.Log.Events[0]);
        Assert.Equal(1200.0, result.Log.Events[0].Context.Time);
        Assert.Equal(StoppageReason.PeriodEnd, Assert.IsType<StoppageEvent>(result.Log.Events[^1]).Reason);
    }

    [Fact]
    public void Regroup_SpendsRegroupTime_BeforeAnythingElseAfterAnOwnOrNeutralZoneTakeaway()
    {
        ShiftSetup setup = At(0.0, TransitionInstruction.Regroup);
        Rink rink = ShiftTestData.Data.Rink;
        double regroup = ShiftTestData.Data.Tuning.Time.RegroupSeconds;
        int seen = 0;
        for (ulong seed = 1; seed <= 200; seed++)
        {
            IReadOnlyList<SimEvent> events = ShiftTestData.Run(seed, setup).Log.Events;
            for (int i = 0; i < events.Count - 1; i++)
            {
                if (events[i] is not TurnoverEvent turnover || events[i + 1] is StoppageEvent { Reason: StoppageReason.PeriodEnd })
                {
                    continue;
                }

                PlayerPlacement taker = turnover.Context.Placement.Find(turnover.TakerId)!;
                GridPoint view = TeamFrame.ToTeamView(new GridPoint(rink.XOf(turnover.NodeId), rink.YOf(turnover.NodeId)), taker.Team, rink);
                if (rink.ZoneAtX(view.X) == RinkZone.Offensive)
                {
                    continue;
                }

                Assert.True(events[i + 1].Context.Time - turnover.Context.Time >= regroup, "regroup skipped at seed " + seed);
                seen++;
            }
        }

        Assert.True(seen > 0);
    }

    [Fact]
    public void Rush_PlaysWithoutAPlay_RightAfterATakeaway()
    {
        int seen = 0;
        for (ulong seed = 1; seed <= 100; seed++)
        {
            IReadOnlyList<SimEvent> events = ShiftTestData.Run(seed, At(0.0)).Log.Events;
            for (int i = 0; i < events.Count - 1; i++)
            {
                // A play needs a setup first (setupSeconds), so anything sooner after a takeaway is the built-in rush.
                bool soon = events[i + 1].Context.Time - events[i].Context.Time < ShiftTestData.Data.Tuning.Time.SetupSeconds;
                if (events[i] is TurnoverEvent && soon && events[i + 1] is PassEvent or ShotEvent or ControlledZoneEntryEvent)
                {
                    Assert.Null(events[i + 1].Context.PlayId);
                    seen++;
                }
            }
        }

        Assert.True(seen > 0);
    }

    [Fact]
    public void FaceoffSpots_FollowO3()
    {
        Rink rink = ShiftTestData.Data.Rink;

        // Home shooter at (8, 1) in its view: the away goalie holds; away's own-zone left in away view = home view (8, 3).
        Assert.Equal("offensiveRight", FaceoffSpots.GoalieFreeze(rink, TeamSide.Home, new GridPoint(8, 3)));
        Assert.Equal("offensiveLeft", FaceoffSpots.GoalieFreeze(rink, TeamSide.Home, new GridPoint(8, 1)));
        Assert.Equal("offensiveRight", FaceoffSpots.GoalieFreeze(rink, TeamSide.Home, new GridPoint(8, 2))); // middle lane → the goalie team's left

        // Away shooter at its (8, 1) = home view (2, 3): home goalie, home view y 3 > 2 → defensiveRight.
        Assert.Equal("defensiveRight", FaceoffSpots.GoalieFreeze(rink, TeamSide.Away, new GridPoint(2, 3)));

        // Out of play (Q-040 default): (7, 2) → offensiveLeft; (7, 4) → offensiveRight; away at its (7, 2) = home (3, 2) → defensiveRight (away's left).
        Assert.Equal("offensiveLeft", FaceoffSpots.OutOfPlay(rink, TeamSide.Home, new GridPoint(7, 2)));
        Assert.Equal("offensiveRight", FaceoffSpots.OutOfPlay(rink, TeamSide.Home, new GridPoint(7, 4)));
        Assert.Equal("defensiveRight", FaceoffSpots.OutOfPlay(rink, TeamSide.Away, new GridPoint(3, 2)));
    }

    [Fact]
    public void RotationCoach_ChangesEveryEligibleUnitInOrder_AndPutsTheLeastUsedPlayFirst()
    {
        Team team = TestPlayers.Team("Home", 1, trios: 4, pairs: 3);
        IReadOnlyList<Play> library = ShiftTestData.Data.Plays;
        var coach = new RotationCoach(library, ShiftTestData.Data.Systems[0]);
        var uses = new Dictionary<string, double> { { library[0].Id, 3 }, { library[2].Id, 0.5 } };
        CoachView View(int f, int p, bool cf, bool cd) => new(TeamSide.Home, team, 1, 0, 0, 0, f, p, cf, cd, uses, new Dictionary<int, double>());

        CoachDecision start = coach.AtStoppage(View(-1, -1, true, true));
        Assert.Equal(0, start.Units.ForwardIndex);
        Assert.Equal(0, start.Units.PairIndex);
        LineChoice next = coach.OnTheFly(View(3, 2, true, false));
        Assert.Equal(0, next.ForwardIndex);
        Assert.Equal(2, next.PairIndex);
        Assert.Equal(TransitionInstruction.Rush, start.Plan.Transition);
        Assert.Equal(library[1].Id, start.Plan.Plays[0].Id);
        Assert.Equal(library[0].Id, start.Plan.Plays[^1].Id);
        Assert.Equal(library[2].Id, start.Plan.Plays[^2].Id);
    }

    [Fact]
    public void Familiarity_IsHalvedAtEveryIntermission()
    {
        MatchResult result = MatchTestData.Play(3);
        double multiplier = ShiftTestData.Data.Tuning.Familiarity.IntermissionMultiplier;

        for (int t = 0; t < 2; t++)
        {
            foreach (KeyValuePair<string, double> use in result.Periods[0].PlayUses[t])
            {
                double second = result.Periods[1].PlayUses[t][use.Key];
                Assert.True(second >= use.Value * multiplier);
                Assert.Equal(0.0, (second - (use.Value * multiplier)) % 1.0, 9);
            }
        }
    }
}
