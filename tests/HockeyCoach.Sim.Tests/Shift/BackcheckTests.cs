using HockeyCoach.AI;
using HockeyCoach.Harness.Data;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Shift;

/// <summary>
/// D-085 back-check in whole shifts and matches (data-schema.md, Paluukarvaus suorahyökkäyksessä): it applies only to
/// the defence move of the opponent's rush actions, k = 0 is the old system movement, and the log stays deterministic.
/// </summary>
public class BackcheckTests
{
    private sealed class Trace : IDefenceMoveTrace
    {
        public List<DefenceMoveSample> Samples { get; } = new();

        public void OnDefenceMove(DefenceMoveSample sample) => Samples.Add(sample);
    }

    private static GameData DataWith(int k) => RepositoryTuning.Load(r => r["transitions"]!["backcheckNodesPerBeat"] = k);

    private static readonly Lazy<GameData> Zero = new(() => DataWith(0));

    /// <summary>Back-check on with k = 2 (D-086); the repository value is 0 since D-087.</summary>
    private static readonly Lazy<GameData> Two = new(() => DataWith(2));

    private static List<(ShiftSetup Setup, DefenceMoveSample Sample)> RushMoves(GameData data, ulong seeds)
    {
        var moves = new List<(ShiftSetup, DefenceMoveSample)>();
        foreach ((string home, string away) in new[] { ("forecheck212", "trap122"), ("trap122", "forecheck212") })
        {
            ShiftSetup setup = HarnessTeams.Shift(data, home, away, 2000);
            for (ulong seed = 1; seed <= seeds; seed++)
            {
                var trace = new Trace();
                ShiftSimulator.Run(setup, new Pcg32(seed), trace);
                moves.AddRange(trace.Samples.Where(s => s.Kind == DefenceMoveKind.Action && s.Rush).Select(s => (setup, s)));
            }
        }

        return moves;
    }

    /// <summary>The system move recomputed from the positions before it, without any back-check (the pre-D-085 rule).</summary>
    private static IReadOnlyDictionary<Position, GridPoint> SystemOnly(ShiftSetup setup, DefenceMoveSample s)
    {
        Rink rink = setup.Rink;
        int maxSteps = setup.Tuning.Plays.MaxNodesPerBeat;
        int reaction = setup.Tuning.Pressure.PresserReactionNodes;
        DefensiveSystem system = (s.Defending == TeamSide.Home ? setup.Home : setup.Away).Plan.System;
        SystemTargets targets = SystemTargetResolver.Resolve(system, rink, s.Puck, PuckState.Controlled, s.Before);
        var moves = new List<OccupancyMove>();
        foreach (SystemRole role in OccupancyResolver.SystemOrder)
        {
            Position position = targets.Roles[role];
            int steps = position == s.DelayedPresser ? Math.Min(maxSteps, reaction) : maxSteps;
            moves.Add(new OccupancyMove(position, SystemMovement.Path(s.Before[position], targets.Targets[position], s.Puck, steps, rink)));
        }

        return OccupancyResolver.Resolve(s.Before, moves, rink);
    }

    private static bool AnyBehind(DefenceMoveSample s) => s.Before.Values.Any(p => Backcheck.IsBehindPuck(p, s.Puck));

    [Fact]
    public void RushMoves_WithNobodyBehindThePuck_AreTheSystemMove()
    {
        List<(ShiftSetup Setup, DefenceMoveSample Sample)> moves = RushMoves(ShiftTestData.Data, 60);
        Assert.Contains(moves, m => AnyBehind(m.Sample));
        int checkedMoves = 0;
        foreach ((ShiftSetup setup, DefenceMoveSample sample) in moves.Where(m => !AnyBehind(m.Sample)))
        {
            Assert.Equal(SystemOnly(setup, sample), sample.After);
            checkedMoves++;
        }

        Assert.True(checkedMoves > 0);
    }

    [Fact]
    public void RushMoves_BackCheckersMoveStraightTowardTheOwnGoal_AndNeverPastThePuckRow()
    {
        int k = Two.Value.Tuning.Transitions.BackcheckNodesPerBeat;
        Assert.Equal(2, k);
        int backCheckers = 0;
        foreach ((ShiftSetup setup, DefenceMoveSample s) in RushMoves(Two.Value, 60))
        {
            foreach (KeyValuePair<Position, GridPoint> before in s.Before.Where(e => Backcheck.IsBehindPuck(e.Value, s.Puck)))
            {
                GridPoint after = s.After[before.Key];
                GridPoint target = Backcheck.Target(before.Value, s.Puck);
                IReadOnlyList<GridPoint> path = Backcheck.Path(before.Value, s.Puck, k, setup.Rink);
                if (!path.Contains(after))
                {
                    // D-058: only a teammate on every node of his own path pushes him to the nearest free node.
                    Assert.All(path, p => Assert.Contains(s.After, e => e.Key != before.Key && e.Value.Equals(p)));
                    continue;
                }

                Assert.True(after.X >= target.X, "passed x_t: " + before.Value + " → " + after + ", puck " + s.Puck);
                Assert.True(before.Value.X - after.X <= k);
                backCheckers++;
            }

            Assert.All(s.After.Values, p => Assert.False(setup.Rink.IsGoalNode(p)));
        }

        Assert.True(backCheckers > 0);
    }

    [Fact]
    public void ZeroSteps_RushMovesAreExactlyTheSystemMove()
    {
        List<(ShiftSetup Setup, DefenceMoveSample Sample)> moves = RushMoves(Zero.Value, 40);
        Assert.Contains(moves, m => AnyBehind(m.Sample));
        foreach ((ShiftSetup setup, DefenceMoveSample sample) in moves)
        {
            Assert.Equal(SystemOnly(setup, sample), sample.After);
        }
    }

    [Fact]
    public void BackCheck_ChangesSomeRushMoves()
    {
        Assert.Contains(RushMoves(Two.Value, 60), m => !SystemOnly(m.Setup, m.Sample).SequenceEqual(m.Sample.After));
    }

    /// <summary>
    /// Both teams on <c>regroup</c>: no rush ever happens, so plays, faceoff plays, setups, regroups (with K1), system
    /// steps, loose pucks and second chances give the same log whatever k is.
    /// </summary>
    [Theory]
    [InlineData(3UL)]
    [InlineData(17UL)]
    public void WithoutRushes_TheLogDoesNotDependOnK(ulong seed)
    {
        string Run(GameData data)
        {
            MatchSetup setup = MatchCommand.Setup(data);
            MatchResult result = MatchSimulator.Run(
                setup,
                new RotationCoach(data.Plays, HarnessTeams.System(data, "forecheck212"), TransitionInstruction.Regroup),
                new RotationCoach(data.Plays, HarnessTeams.System(data, "trap122"), TransitionInstruction.Regroup),
                new MatchStreams(seed).Simulation());
            return MatchJson.Write(result, setup, seed);
        }

        string two = Run(Two.Value);
        Assert.Equal(Run(Zero.Value), two);
        Assert.Equal(Run(DataWith(4)), two);
    }

    [Fact]
    public void WithRushes_KChangesTheLog_AndTheSameSeedGivesTheSameLog()
    {
        string Run(GameData data) => MatchJson.Write(MatchCommand.Play(data, MatchCommand.Setup(data), "forecheck212", "trap122", 11UL), MatchCommand.Setup(data), 11UL);

        string two = Run(Two.Value);
        Assert.Equal(two, Run(Two.Value));
        Assert.Equal(Run(Zero.Value), Run(Zero.Value));
        Assert.NotEqual(Run(Zero.Value), two);
    }
}
