using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.State;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.State;

/// <summary>
/// D-085 back-check at the movement level (data-schema.md, Paluukarvaus suorahyökkäyksessä): a defender behind the puck
/// ignores his system target and skates in his own lane toward his own goal, at most k steps, to the puck's row (D-086).
/// Nodes in these tests are in the defending team's own view unless noted (own goal (1, 2), so "toward the own goal" is x − 1).
/// </summary>
public class BackcheckMoveTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    /// <summary>Every role heads for the far end row of the defending team's view (x = 10), away from its own goal.</summary>
    private static DefensiveSystem Away() => new(
        "away",
        "Test system pulling everyone toward the opponent's end",
        false,
        new[]
        {
            new SystemRule(
                SystemCondition.Always,
                TestPlays.Targets(
                    SystemTarget.Node(new GridPoint(10, 0)),
                    SystemTarget.Node(new GridPoint(10, 1)),
                    SystemTarget.Node(new GridPoint(10, 3)),
                    SystemTarget.Node(new GridPoint(10, 4)),
                    SystemTarget.Node(new GridPoint(6, 2)))),
        });

    /// <summary>Every role heads for the defending team's own end row (x = 0).</summary>
    private static DefensiveSystem Retreat() => new(
        "retreat",
        "Retreat test system",
        false,
        new[]
        {
            new SystemRule(
                SystemCondition.Always,
                TestPlays.Targets(
                    SystemTarget.Node(new GridPoint(0, 0)),
                    SystemTarget.Node(new GridPoint(0, 1)),
                    SystemTarget.Node(new GridPoint(0, 2)),
                    SystemTarget.Node(new GridPoint(0, 3)),
                    SystemTarget.Node(new GridPoint(0, 4)))),
        });

    /// <summary>
    /// A state where <paramref name="defending"/> stands on <paramref name="defence"/> (its own view) and the other team's
    /// C carries the puck on <paramref name="puck"/> (defending team's view).
    /// </summary>
    private static ShiftState Build(TeamSide defending, IReadOnlyDictionary<Position, GridPoint> defence, GridPoint puck)
    {
        Team home = TestPlayers.Team("Home", 1);
        Team away = TestPlayers.Team("Away", 101);
        var carrierNode = new GridPoint(Rink.Length - 1 - puck.X, Rink.Width - 1 - puck.Y);
        var attack = new Dictionary<Position, GridPoint> { [Position.Center] = carrierNode };
        var spare = new[] { new GridPoint(0, 0), new GridPoint(0, 1), new GridPoint(0, 3), new GridPoint(0, 4), new GridPoint(10, 0) };
        int next = 0;
        foreach (Position position in new[] { Position.LeftWing, Position.RightWing, Position.LeftDefence, Position.RightDefence })
        {
            while (spare[next].Equals(carrierNode))
            {
                next++;
            }

            attack[position] = spare[next++];
        }

        bool homeDefends = defending == TeamSide.Home;
        var state = new ShiftState(
            Rink,
            new OnIceSkaters(home.ForwardLines[0], home.DefencePairs[0]),
            new OnIceSkaters(away.ForwardLines[0], away.DefencePairs[0]),
            home.Goalies[0].Id,
            away.Goalies[0].Id,
            homeDefends ? defence : attack,
            homeDefends ? attack : defence,
            new GridPoint(5, 2));
        state.GivePuckTo(TeamSides.Opponent(defending), Position.Center);
        Assert.Equal(puck, TeamFrame.ToTeamView(state.PuckNode, defending, Rink));
        return state;
    }

    private static IReadOnlyDictionary<Position, GridPoint> Apply(
        ShiftState state, TeamSide defending, DefensiveSystem system, int backcheck, Position? limited = null, int limitedSteps = 2)
    {
        state.ApplySystem(defending, system, 2, false, limited, limitedSteps, backcheck, out bool unused);
        return state.NodesInTeamView(defending);
    }

    /// <summary>
    /// Puck (5, 2): LW gap 1, C gap 2, RW gap 4 behind it; LD ahead of it (behind his own goal), RD level with it. The
    /// system moves of LD and RD never end where a back-checker ends, so no D-058 conflict mixes the cases.
    /// </summary>
    private static Dictionary<Position, GridPoint> GapFormation() => new()
    {
        [Position.LeftWing] = new GridPoint(6, 0),
        [Position.Center] = new GridPoint(7, 1),
        [Position.RightWing] = new GridPoint(9, 4),
        [Position.LeftDefence] = new GridPoint(0, 0),
        [Position.RightDefence] = new GridPoint(5, 0),
    };

    private static readonly GridPoint GapPuck = new(5, 2);

    [Fact]
    public void BehindThePuck_SkatesStraightTowardTheOwnGoal_AtMostKSteps_ToThePuckRow()
    {
        IReadOnlyDictionary<Position, GridPoint> after = Apply(Build(TeamSide.Away, GapFormation(), GapPuck), TeamSide.Away, Away(), 2);

        // D-086: x_t = x_k. Puck at x = 5.
        Assert.Equal(new GridPoint(5, 0), after[Position.LeftWing]);  // gap 1 → one step, level with the puck
        Assert.Equal(new GridPoint(5, 1), after[Position.Center]);    // gap 2 → level with the puck
        Assert.Equal(new GridPoint(7, 4), after[Position.RightWing]); // gap 4 → still two rows behind it
    }

    [Fact]
    public void BackChecker_NeverPassesThePuckRow()
    {
        // k = 4: gap 1 would reach x = 2 in four straight steps, but stops on x_t = x_k = 5 (D-086).
        IReadOnlyDictionary<Position, GridPoint> after = Apply(Build(TeamSide.Away, GapFormation(), GapPuck), TeamSide.Away, Away(), 4);

        Assert.Equal(new GridPoint(5, 0), after[Position.LeftWing]);
        Assert.Equal(new GridPoint(5, 1), after[Position.Center]);
        Assert.Equal(new GridPoint(5, 4), after[Position.RightWing]);
    }

    [Fact]
    public void LevelAndAheadDefenders_FollowTheSystem_AsWithZero()
    {
        IReadOnlyDictionary<Position, GridPoint> on = Apply(Build(TeamSide.Away, GapFormation(), GapPuck), TeamSide.Away, Retreat(), 2);
        IReadOnlyDictionary<Position, GridPoint> off = Apply(Build(TeamSide.Away, GapFormation(), GapPuck), TeamSide.Away, Retreat(), 0);

        Assert.Equal(off[Position.LeftDefence], on[Position.LeftDefence]);
        Assert.Equal(off[Position.RightDefence], on[Position.RightDefence]);
        Assert.NotEqual(GapFormation()[Position.RightDefence], on[Position.RightDefence]);
    }

    [Fact]
    public void ZeroSteps_IsTheSystemMoveForEveryone()
    {
        ShiftState with = Build(TeamSide.Away, GapFormation(), GapPuck);
        ShiftState without = Build(TeamSide.Away, GapFormation(), GapPuck);
        with.ApplySystem(TeamSide.Away, Away(), 2, false, null, 2, 0, out bool unused1);
        without.ApplySystem(TeamSide.Away, Away(), 2, false, null, 2, out bool unused2);

        Assert.Equal(without.NodesInTeamView(TeamSide.Away), with.NodesInTeamView(TeamSide.Away));

        // The system really pulls them away from the own goal, so the back-check differs from it.
        Assert.NotEqual(Apply(Build(TeamSide.Away, GapFormation(), GapPuck), TeamSide.Away, Away(), 2), with.NodesInTeamView(TeamSide.Away));
    }

    [Fact]
    public void Target_IsThePuckRow_InTheOwnLane()
    {
        // D-086: x_t = x_k, no net-front cap (previously min(x_k + 1, opponentGoal.x − 1)).
        Assert.Equal(new GridPoint(5, 3), Backcheck.Target(new GridPoint(8, 3), new GridPoint(5, 1)));
        Assert.Equal(new GridPoint(0, 3), Backcheck.Target(new GridPoint(6, 3), new GridPoint(0, 2)));
        Assert.Equal(new GridPoint(1, 0), Backcheck.Target(new GridPoint(6, 0), new GridPoint(1, 0)));
    }

    [Fact]
    public void PuckBehindTheOwnGoal_BackCheckersNeverStandOnAGoalNode_NorPassThePuckRow()
    {
        var defence = new Dictionary<Position, GridPoint>
        {
            [Position.LeftWing] = new GridPoint(4, 0),
            [Position.Center] = new GridPoint(4, 2),
            [Position.RightWing] = new GridPoint(4, 4),
            [Position.LeftDefence] = new GridPoint(3, 1),
            [Position.RightDefence] = new GridPoint(3, 3),
        };
        var puck = new GridPoint(0, 1);
        IReadOnlyDictionary<Position, GridPoint> after = Apply(Build(TeamSide.Away, defence, puck), TeamSide.Away, Away(), 4);
        foreach (GridPoint node in after.Values)
        {
            Assert.False(Rink.IsGoalNode(node));
            Assert.True(node.X >= puck.X);
        }

        Assert.Equal(after.Count, after.Values.Distinct().Count());
    }

    [Fact]
    public void TargetOnTheGoalNode_SkatesStraight_ThenTheLastStepCirclesTheGoal_D053()
    {
        // Puck (1, 0) beside the own goal (1, 2): the middle-lane target (1, 2) is the goal node. Straight to (2, 2), then
        // the step onto the goal circles to the puck's side (1, 1), which is on row x_t, so the path ends there.
        var puck = new GridPoint(1, 0);
        Assert.Equal(new[] { new GridPoint(3, 2), new GridPoint(2, 2), new GridPoint(1, 1) }, Backcheck.Path(new GridPoint(3, 2), puck, 4, Rink));
        Assert.Equal(new[] { new GridPoint(3, 2), new GridPoint(2, 2) }, Backcheck.Path(new GridPoint(3, 2), puck, 1, Rink));
    }

    [Fact]
    public void Target_KeepsTheRow_WhenTheDefenderIsAlreadyPastIt()
    {
        // Q-074: the step is never away from the own goal. With x_t = x_k only a defender not behind the puck is past it.
        Assert.Equal(new GridPoint(1, 3), Backcheck.Target(new GridPoint(1, 3), new GridPoint(2, 1)));
        Assert.True(Backcheck.IsBehindPuck(new GridPoint(1, 3), new GridPoint(0, 1)));
        Assert.False(Backcheck.IsBehindPuck(new GridPoint(5, 3), new GridPoint(5, 1)));
    }

    [Fact]
    public void FromBehindTheOpponentGoal_CirclesItPerD053_AsOneOfTheKSteps()
    {
        // (10, 2) is behind the attacking team's goal (9, 2) in the defending view; puck on the goal row → circle side −1.
        var defence = GapFormation();
        defence[Position.RightWing] = new GridPoint(10, 2);
        IReadOnlyDictionary<Position, GridPoint> after = Apply(Build(TeamSide.Away, defence, GapPuck), TeamSide.Away, Away(), 2);

        IReadOnlyList<GridPoint> path = SystemMovement.Path(new GridPoint(10, 2), new GridPoint(5, 2), GapPuck, 2, Rink);
        Assert.Equal(new GridPoint(9, 1), path[1]);
        Assert.All(path, p => Assert.False(Rink.IsGoalNode(p)));
        Assert.Equal(path[path.Count - 1], after[Position.RightWing]);
        Assert.Equal(new GridPoint(8, 2), after[Position.RightWing]);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    [InlineData(3, 2)]
    public void DelayedPresserBehindThePuck_TakesMinOfKAndReactionNodes(int reactionNodes, int expectedSteps)
    {
        IReadOnlyDictionary<Position, GridPoint> after =
            Apply(Build(TeamSide.Away, GapFormation(), GapPuck), TeamSide.Away, Away(), 2, Position.RightWing, reactionNodes);

        Assert.Equal(new GridPoint(9 - expectedSteps, 4), after[Position.RightWing]);
        Assert.Equal(new GridPoint(5, 0), after[Position.LeftWing]);
    }

    [Fact]
    public void TwoBackCheckersInOneLane_NeverShareANode_D058()
    {
        // k = 4: C (6, 1) and LW (8, 1) both aim at (5, 1); the later one in role order backs up along its own path.
        var defence = new Dictionary<Position, GridPoint>
        {
            [Position.LeftWing] = new GridPoint(8, 1),
            [Position.Center] = new GridPoint(6, 1),
            [Position.RightWing] = new GridPoint(9, 4),
            [Position.LeftDefence] = new GridPoint(0, 0),
            [Position.RightDefence] = new GridPoint(0, 4),
        };
        IReadOnlyDictionary<Position, GridPoint> after = Apply(Build(TeamSide.Away, defence, GapPuck), TeamSide.Away, Away(), 4);

        Assert.Equal(after.Count, after.Values.Distinct().Count());
        var ends = new[] { after[Position.LeftWing], after[Position.Center] }.OrderBy(p => p.X).ToArray();
        Assert.Equal(new[] { new GridPoint(5, 1), new GridPoint(6, 1) }, ends);
    }

    [Fact]
    public void HomeAway_AndLeftRight_GiveMirrorImages()
    {
        // All five behind the puck (4, 1), in separate lanes: only the back-check moves them.
        var defence = new Dictionary<Position, GridPoint>
        {
            [Position.LeftWing] = new GridPoint(5, 0),
            [Position.Center] = new GridPoint(6, 1),
            [Position.RightWing] = new GridPoint(8, 2),
            [Position.LeftDefence] = new GridPoint(7, 3),
            [Position.RightDefence] = new GridPoint(10, 4),
        };
        var puck = new GridPoint(4, 1);
        Dictionary<Position, GridPoint> mirrored = defence.ToDictionary(e => e.Key, e => new GridPoint(e.Value.X, Rink.Width - 1 - e.Value.Y));
        var mirroredPuck = new GridPoint(puck.X, Rink.Width - 1 - puck.Y);

        IReadOnlyDictionary<Position, GridPoint> away = Apply(Build(TeamSide.Away, defence, puck), TeamSide.Away, Away(), 2);
        IReadOnlyDictionary<Position, GridPoint> home = Apply(Build(TeamSide.Home, defence, puck), TeamSide.Home, Away(), 2);
        IReadOnlyDictionary<Position, GridPoint> awayMirror = Apply(Build(TeamSide.Away, mirrored, mirroredPuck), TeamSide.Away, Away(), 2);

        Assert.Equal(away, home);
        foreach (Position position in PositionOrder.All)
        {
            Assert.Equal(new GridPoint(away[position].X, Rink.Width - 1 - away[position].Y), awayMirror[position]);
        }

        Assert.Equal(new GridPoint(4, 0), away[Position.LeftWing]);
        Assert.Equal(new GridPoint(8, 4), away[Position.RightDefence]);
    }
}
