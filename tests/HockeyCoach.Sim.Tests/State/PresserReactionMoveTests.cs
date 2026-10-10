using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.State;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.State;

/// <summary>
/// D-074 presser reaction delay at the movement level: one mover (the presser) takes at most N steps, the rest of the
/// defence moves as usual (data-schema.md, Puolustusjärjestelmät, Painostajan reaktioviive, points 5 and 6).
/// </summary>
public class PresserReactionMoveTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    private static Dictionary<Position, GridPoint> Formation() => new()
    {
        [Position.LeftWing] = new GridPoint(5, 0),
        [Position.Center] = new GridPoint(5, 1),
        [Position.RightWing] = new GridPoint(5, 4),
        [Position.LeftDefence] = new GridPoint(3, 1),
        [Position.RightDefence] = new GridPoint(3, 3),
    };

    private static ShiftState Build(GridPoint? loose = null)
    {
        Team home = TestPlayers.Team("Home", 1);
        Team away = TestPlayers.Team("Away", 101);
        return new ShiftState(
            Rink,
            new OnIceSkaters(home.ForwardLines[0], home.DefencePairs[0]),
            new OnIceSkaters(away.ForwardLines[0], away.DefencePairs[0]),
            home.Goalies[0].Id,
            away.Goalies[0].Id,
            Formation(),
            Formation(),
            loose ?? new GridPoint(5, 2));
    }

    /// <summary>Every role heads for its own node on the far end row (x = 0), well beyond two steps.</summary>
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

    private static (ShiftState State, SystemTargets Targets, Dictionary<Position, GridPoint> Before) Applied(Position? limited, int limitedSteps)
    {
        ShiftState state = Build();
        state.GivePuckTo(TeamSide.Home, Position.Center);
        var before = new Dictionary<Position, GridPoint>(state.NodesInTeamView(TeamSide.Away));
        SystemTargets targets = state.ApplySystem(TeamSide.Away, Retreat(), 2, false, limited, limitedSteps, out bool unused);
        return (state, targets, before);
    }

    [Fact]
    public void ApplySystem_LimitsThePresserToTheReactionSteps()
    {
        (ShiftState plain, SystemTargets targets, Dictionary<Position, GridPoint> before) = Applied(null, 1);
        Position presser = targets.Roles[SystemRole.F1];
        (ShiftState delayed, _, _) = Applied(presser, 1);

        GridPoint puck = new(5, 3); // home C (5,1) in the away team's view
        Assert.Equal(2, GridPoint.Chebyshev(before[presser], plain.NodesInTeamView(TeamSide.Away)[presser]));
        Assert.Equal(SystemMovement.Advance(before[presser], targets.Targets[presser], puck, 1, Rink), delayed.NodesInTeamView(TeamSide.Away)[presser]);
    }

    [Fact]
    public void Move_LimitsOnlyThePresser_OthersMoveTheFullBeat()
    {
        // Five separate lanes toward the end row: no D-058 conflicts, so only the limited mover differs.
        var targets = new[]
        {
            TestPlays.At(Position.Center, 1, 1), TestPlays.At(Position.LeftWing, 1, 0), TestPlays.At(Position.RightWing, 1, 4),
            TestPlays.At(Position.LeftDefence, 0, 1), TestPlays.At(Position.RightDefence, 0, 3),
        };
        ShiftState full = Build();
        full.Move(TeamSide.Home, targets, 2, null, 1);
        ShiftState delayed = Build();
        delayed.Move(TeamSide.Home, targets, 2, Position.Center, 1);

        Assert.Equal(new GridPoint(3, 1), full.NodeOf(TeamSide.Home, Position.Center));
        Assert.Equal(new GridPoint(4, 1), delayed.NodeOf(TeamSide.Home, Position.Center));
        foreach (Position other in PositionOrder.All.Where(p => p != Position.Center))
        {
            Assert.Equal(full.NodeOf(TeamSide.Home, other), delayed.NodeOf(TeamSide.Home, other));
            Assert.Equal(2, GridPoint.Chebyshev(Formation()[other], delayed.NodeOf(TeamSide.Home, other)));
        }
    }

    [Fact]
    public void ApplySystem_ZeroReactionNodes_PresserStaysPut()
    {
        (ShiftState plain, SystemTargets targets, Dictionary<Position, GridPoint> before) = Applied(null, 0);
        Position presser = targets.Roles[SystemRole.D1];
        (ShiftState delayed, _, _) = Applied(presser, 0);

        Assert.Equal(before[presser], delayed.NodesInTeamView(TeamSide.Away)[presser]);
        Assert.NotEqual(before[presser], plain.NodesInTeamView(TeamSide.Away)[presser]);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void ApplySystem_ReactionNodesAtOrAboveMaxSteps_HasNoEffect(int limitedSteps)
    {
        (ShiftState plain, SystemTargets targets, _) = Applied(null, limitedSteps);
        (ShiftState delayed, _, _) = Applied(targets.Roles[SystemRole.F1], limitedSteps);

        Assert.Equal(plain.NodesInTeamView(TeamSide.Away), delayed.NodesInTeamView(TeamSide.Away));
    }

    [Fact]
    public void Move_CirclingStepCountsAsOneOfTheReactionSteps()
    {
        // data-schema.md, Maalin kierto: (2,2) → (0,2) with the puck below the goal row circles via (1,3).
        ShiftState state = Build(new GridPoint(0, 3));
        state.Place(TeamSide.Home, Position.Center, new GridPoint(2, 2));
        var targets = new[] { TestPlays.At(Position.Center, 0, 2) };

        state.Move(TeamSide.Home, targets, 2, Position.Center, 1);
        Assert.Equal(new GridPoint(1, 3), state.NodeOf(TeamSide.Home, Position.Center));

        state.Place(TeamSide.Home, Position.Center, new GridPoint(2, 2));
        state.Move(TeamSide.Home, targets, 2, null, 1);
        Assert.Equal(new GridPoint(0, 2), state.NodeOf(TeamSide.Home, Position.Center));
    }

    [Fact]
    public void Move_ShortenedPresserEnd_TakesTheNodeALaterMoverWanted()
    {
        // C (5,1) → (2,0): two steps end at (3,0), one step at (4,0). LW (5,0) → (4,0) comes later in the order (D-058).
        var targets = new[] { TestPlays.At(Position.Center, 2, 0), TestPlays.At(Position.LeftWing, 4, 0) };

        ShiftState full = Build();
        full.Move(TeamSide.Home, targets, 2, null, 1);
        Assert.Equal(new GridPoint(3, 0), full.NodeOf(TeamSide.Home, Position.Center));
        Assert.Equal(new GridPoint(4, 0), full.NodeOf(TeamSide.Home, Position.LeftWing));

        ShiftState delayed = Build();
        delayed.Move(TeamSide.Home, targets, 2, Position.Center, 1);
        Assert.Equal(new GridPoint(4, 0), delayed.NodeOf(TeamSide.Home, Position.Center));
        Assert.Equal(new GridPoint(5, 0), delayed.NodeOf(TeamSide.Home, Position.LeftWing));
    }
}
