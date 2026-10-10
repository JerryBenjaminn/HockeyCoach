using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>Invariants of every event of full matches (tech-spec.md, Testaus; data-schema.md O-1, O-2, O-13, D-058).</summary>
public class MatchInvariantTests
{
    private const int Matches = 12;

    [Fact]
    public void EveryEvent_HasFiveDistinctSkatersPerTeamFromOneTrioAndOnePair_AndOnePuck()
    {
        Rink rink = ShiftTestData.Data.Rink;
        int ownGoal = rink.IdOf(rink.OwnGoal.X, rink.OwnGoal.Y);
        int opponentGoal = rink.IdOf(rink.OpponentGoal.X, rink.OpponentGoal.Y);
        foreach (MatchResult result in MatchTestData.Sample(Matches))
        {
            foreach (SimEvent e in result.Log.Events)
            {
                PlacementSnapshot p = e.Context.Placement;
                Assert.Equal(new Strength(5, 5), e.Context.Strength);
                Assert.Equal(2, p.Players.Count(x => x.IsGoalie));
                foreach ((Team team, TeamSide side) in new[] { (MatchTestData.Setup.Home, TeamSide.Home), (MatchTestData.Setup.Away, TeamSide.Away) })
                {
                    List<PlayerPlacement> skaters = p.Players.Where(x => !x.IsGoalie && x.Team == side).ToList();
                    Assert.Equal(5, skaters.Count);
                    Assert.Equal(5, skaters.Select(x => x.NodeId).Distinct().Count());
                    Assert.All(skaters, x => Assert.True(x.NodeId != ownGoal && x.NodeId != opponentGoal));
                    HashSet<int> ids = skaters.Select(x => x.PlayerId).ToHashSet();
                    Assert.Single(team.ForwardLines, l => l.Skaters.All(s => ids.Contains(s.Id)));
                    Assert.Single(team.DefencePairs, d => d.Skaters.All(s => ids.Contains(s.Id)));
                }

                Assert.True(p.PuckCarrierId == null || !p.Find(p.PuckCarrierId.Value)!.IsGoalie);
                Assert.True(!p.IsPuckLoose || (p.PuckNodeId != ownGoal && p.PuckNodeId != opponentGoal));
            }
        }
    }

    [Fact]
    public void Periods_RunToTheBuzzer_AndEndWithExactlyOnePeriodEndStoppage()
    {
        double periodSeconds = ShiftTestData.Data.Tuning.Time.PeriodSeconds;
        foreach (MatchResult result in MatchTestData.Sample(Matches))
        {
            Assert.False(result.Stalled);
            Assert.Equal(3, result.Periods.Count);
            for (int period = 1; period <= 3; period++)
            {
                List<SimEvent> events = result.Log.Events.Where(e => e.Context.Period == period).ToList();
                Assert.All(events, e => Assert.InRange(e.Context.Time, 0.0, periodSeconds));
                var last = Assert.IsType<StoppageEvent>(events[^1]);
                Assert.Equal(StoppageReason.PeriodEnd, last.Reason);
                Assert.Equal(periodSeconds, last.Context.Time);
                Assert.Single(events.OfType<StoppageEvent>(), s => s.Reason == StoppageReason.PeriodEnd);
                Assert.IsType<FaceoffEvent>(events.First(e => e is not LineChangeEvent));
            }
        }
    }

    [Fact]
    public void EveryStoppage_IsFollowedByLineChangesAndAFaceoff()
    {
        foreach (MatchResult result in MatchTestData.Sample(Matches))
        {
            IReadOnlyList<SimEvent> events = result.Log.Events;
            for (int i = 0; i < events.Count - 1; i++)
            {
                if (events[i] is not StoppageEvent stoppage || stoppage.Reason == StoppageReason.PeriodEnd)
                {
                    continue;
                }

                int j = i + 1;
                while (events[j] is LineChangeEvent)
                {
                    Assert.Equal(stoppage.Context.Time, events[j].Context.Time);
                    j++;
                }

                // A faceoff, or the period end when even the faceoff would cross it (O-1).
                Assert.True(events[j] is FaceoffEvent || events[j] is StoppageEvent { Reason: StoppageReason.PeriodEnd }, events[j].GetType().Name);
                Assert.Equal(stoppage.Context.Period, events[j].Context.Period);
            }
        }
    }

    [Fact]
    public void Goals_MatchTheLog_AndNoEventFollowsAGoalBeforeTheFaceoff()
    {
        Func<int, TeamSide> teamOf = MatchTestData.TeamOf;
        foreach (MatchResult result in MatchTestData.Sample(Matches))
        {
            List<ShotEvent> goals = result.Log.Events.OfType<ShotEvent>().Where(s => s.Outcome == ShotOutcome.Goal).ToList();
            Assert.Equal(result.HomeGoals, goals.Count(g => teamOf(g.ShooterId) == TeamSide.Home));
            Assert.Equal(result.AwayGoals, goals.Count(g => teamOf(g.ShooterId) == TeamSide.Away));
            Assert.Equal(goals.Count, result.Log.Events.OfType<StoppageEvent>().Count(s => s.Reason == StoppageReason.Goal));
            Assert.Equal(goals.Count, result.State.GoalNotes.Count);
            IReadOnlyList<SimEvent> events = result.Log.Events;
            for (int i = 0; i < events.Count - 1; i++)
            {
                if (events[i] is ShotEvent { Outcome: ShotOutcome.Goal })
                {
                    Assert.Equal(StoppageReason.Goal, Assert.IsType<StoppageEvent>(events[i + 1]).Reason);
                }
            }
        }
    }

    [Fact]
    public void LineChanges_SwapTheUnitOnTheIce_AndIceTimeAddsUpToThePeriods()
    {
        Func<int, TeamSide> teamOf = MatchTestData.TeamOf;
        foreach (MatchResult result in MatchTestData.Sample(Matches))
        {
            List<LineChangeEvent> changes = result.Log.Events.OfType<LineChangeEvent>().ToList();
            Assert.NotEmpty(changes);
            foreach (LineChangeEvent change in changes)
            {
                HashSet<int> onIce = change.Context.Placement.Players.Select(p => p.PlayerId).ToHashSet();
                Assert.All(change.IncomingIds, id => Assert.Contains(id, onIce));
                Assert.All(change.OutgoingIds, id => Assert.DoesNotContain(id, onIce));
                Assert.Equal(teamOf(change.OutgoingIds[0]), teamOf(change.IncomingIds[0]));
            }

            PeriodSnapshot final = result.Periods[^1];
            for (int t = 0; t < 2; t++)
            {
                Assert.Equal(3 * 1200.0, final.ForwardSeconds[t].Sum(), 6);
                Assert.Equal(3 * 1200.0, final.PairSeconds[t].Sum(), 6);
            }

            Assert.All(final.Energy.Values, e => Assert.InRange(e, 0.0, 1.0));
        }
    }

    [Fact]
    public void Matches_ProduceEveryChanceTypeAndStoppageReason_AndBothKindsOfChanges()
    {
        var types = new HashSet<ChanceType>();
        var reasons = new HashSet<StoppageReason>();
        bool onTheFly = false;
        foreach (MatchResult result in MatchTestData.Sample(Matches))
        {
            types.UnionWith(result.Log.Events.OfType<ShotEvent>().Select(s => s.ChanceType));
            reasons.UnionWith(result.Log.Events.OfType<StoppageEvent>().Select(s => s.Reason));
            IReadOnlyList<SimEvent> events = result.Log.Events;
            for (int i = 1; i < events.Count; i++)
            {
                onTheFly |= events[i] is LineChangeEvent && events[i - 1] is not StoppageEvent && events[i - 1] is not LineChangeEvent;
            }
        }

        Assert.Superset(new HashSet<ChanceType> { ChanceType.Rush, ChanceType.Turnover, ChanceType.OffensiveZone, ChanceType.Faceoff }, types);
        Assert.Superset(new HashSet<StoppageReason> { StoppageReason.Goal, StoppageReason.GoalieFreeze, StoppageReason.OutOfPlay, StoppageReason.PeriodEnd }, reasons);
        Assert.True(onTheFly);
    }

    [Fact]
    public void Energy_DrainsOnTheIce_AndThePressureStateIsSampledEveryMinute()
    {
        MatchResult result = MatchTestData.Play(1);

        Assert.Contains(result.Periods[0].Energy.Values, e => e < 1.0);
        Assert.Equal(3 * 20, result.State.PressureSamples.Count);
        Assert.All(result.State.PressureSamples, s => Assert.InRange(s.Home, 0.0, 1.0));
        Assert.Contains(result.State.PressureSamples, s => s.Home > 0.0 || s.Away > 0.0);
    }

    [Fact]
    public void MatchText_ExplainsEveryGoal_AndSummarisesEveryPeriod()
    {
        var options = new MatchCommand.Options(1, "forecheck212", "trap122", false, 1);
        string text = MatchCommand.Run(ShiftTestData.Data, options);
        MatchResult result = MatchTestData.Play(1);

        Assert.Equal(result.HomeGoals + result.AwayGoals, text.Split("    GOAL: ").Length - 1);
        Assert.Equal(3, text.Split("summary ---").Length - 1);
        Assert.Contains("=== Match ===", text, StringComparison.Ordinal);
        Assert.Contains("Final: Home " + result.HomeGoals + " - " + result.AwayGoals + " Away", text, StringComparison.Ordinal);
    }
}
