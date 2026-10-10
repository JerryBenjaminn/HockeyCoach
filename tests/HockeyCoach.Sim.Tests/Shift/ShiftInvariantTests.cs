using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.Sim.Tests.Shift;

/// <summary>Invariants that must hold for every event of every shift (tech-spec.md, Testaus).</summary>
public class ShiftInvariantTests
{
    private const int Seeds = 200;

    [Fact]
    public void EveryEvent_HasTenSkatersTwoGoaliesAndOnePuck_OffTheGoalNodes()
    {
        Rink rink = ShiftTestData.Data.Rink;
        int ownGoal = rink.IdOf(rink.OwnGoal.X, rink.OwnGoal.Y);
        int opponentGoal = rink.IdOf(rink.OpponentGoal.X, rink.OpponentGoal.Y);
        for (ulong seed = 1; seed <= Seeds; seed++)
        {
            ShiftResult result = ShiftTestData.Run(seed);
            foreach (SimEvent e in result.Log.Events)
            {
                PlacementSnapshot p = e.Context.Placement;
                Assert.Equal(5, p.SkaterCount(TeamSide.Home));
                Assert.Equal(5, p.SkaterCount(TeamSide.Away));
                Assert.Equal(2, p.Players.Count(x => x.IsGoalie));
                Assert.Equal(new Strength(5, 5), e.Context.Strength);
                Assert.All(p.Players.Where(x => !x.IsGoalie), x => Assert.True(x.NodeId != ownGoal && x.NodeId != opponentGoal, "skater on a goal node, seed " + seed));
                Assert.True(!p.IsPuckLoose || (p.PuckNodeId != ownGoal && p.PuckNodeId != opponentGoal), "loose puck on a goal node, seed " + seed);
                Assert.True(p.PuckCarrierId == null || p.Find(p.PuckCarrierId.Value)!.IsGoalie == false);
            }
        }
    }

    [Fact]
    public void Shift_StartsWithAFaceoff_AndEndsWithItsOnlyStoppage()
    {
        for (ulong seed = 1; seed <= Seeds; seed++)
        {
            ShiftResult result = ShiftTestData.Run(seed);
            IReadOnlyList<SimEvent> events = result.Log.Events;

            Assert.IsType<FaceoffEvent>(events[0]);
            Assert.NotEqual(ShiftEndReason.StepCap, result.EndReason);
            var stoppage = Assert.IsType<StoppageEvent>(events[events.Count - 1]);
            Assert.Single(events.OfType<StoppageEvent>());
            Assert.Equal(result.EndReason == ShiftEndReason.Goal ? StoppageReason.Goal : StoppageReason.GoalieFreeze, stoppage.Reason);
        }
    }

    [Fact]
    public void Goals_MatchTheLog()
    {
        for (ulong seed = 1; seed <= Seeds; seed++)
        {
            ShiftResult result = ShiftTestData.Run(seed);
            int goals = ShiftTestData.Of<ShotEvent>(result).Count(s => s.Outcome == ShotOutcome.Goal);

            Assert.Equal(goals, result.HomeGoals + result.AwayGoals);
            Assert.Equal(result.EndReason == ShiftEndReason.Goal ? 1 : 0, goals);
            Assert.Equal(ShiftTestData.Of<StoppageEvent>(result).Count(s => s.Reason == StoppageReason.Goal), goals);
        }
    }

    [Fact]
    public void Time_NeverRunsBackwards()
    {
        for (ulong seed = 1; seed <= Seeds; seed++)
        {
            IReadOnlyList<SimEvent> events = ShiftTestData.Run(seed).Log.Events;
            for (int i = 1; i < events.Count; i++)
            {
                Assert.True(events[i].Context.CompareTime(events[i - 1].Context) >= 0);
            }
        }
    }

    [Fact]
    public void ShotEvents_HaveXgInRangeAndASystem()
    {
        for (ulong seed = 1; seed <= Seeds; seed++)
        {
            foreach (ShotEvent shot in ShiftTestData.Of<ShotEvent>(ShiftTestData.Run(seed)))
            {
                Assert.InRange(shot.Xg, 0.0, ShiftTestData.Data.Tuning.Shot.MaxProbability);
                Assert.Null(shot.Speed);
                Assert.NotNull(shot.Context.SystemId);
            }
        }
    }
}
