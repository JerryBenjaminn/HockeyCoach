using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.State;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.State;

public class ShiftStateTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    private static Dictionary<Position, GridPoint> Nodes(params (Position P, int X, int Y)[] nodes) => nodes.ToDictionary(n => n.P, n => new GridPoint(n.X, n.Y));

    /// <summary>Both teams in the same own-view formation: forwards at x = 5, defence at x = 3.</summary>
    private static Dictionary<Position, GridPoint> Formation() => Nodes(
        (Position.LeftWing, 5, 0), (Position.Center, 5, 1), (Position.RightWing, 5, 4), (Position.LeftDefence, 3, 1), (Position.RightDefence, 3, 3));

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

    [Fact]
    public void AwayNodes_AreRotatedIntoTheHomeView()
    {
        ShiftState state = Build();

        Assert.Equal(new GridPoint(5, 0), state.NodeOf(TeamSide.Home, Position.LeftWing));
        Assert.Equal(new GridPoint(5, 4), state.NodeOf(TeamSide.Away, Position.LeftWing));
        Assert.Equal(new GridPoint(7, 1), state.NodeOf(TeamSide.Away, Position.RightDefence));
        Assert.Equal(Formation(), state.NodesInTeamView(TeamSide.Away));
    }

    [Fact]
    public void Snapshot_HasTenSkatersTwoGoaliesAndOnePuck()
    {
        ShiftState state = Build();
        state.GivePuckTo(TeamSide.Away, Position.Center);

        PlacementSnapshot snapshot = state.Snapshot();

        Assert.Equal(5, snapshot.SkaterCount(TeamSide.Home));
        Assert.Equal(5, snapshot.SkaterCount(TeamSide.Away));
        Assert.Equal(12, snapshot.Players.Count);
        Assert.Equal(state.PlayerId(TeamSide.Away, Position.Center), snapshot.PuckCarrierId);
        Assert.Equal(Rink.IdOf(5, 3), snapshot.PuckNodeId);
        Assert.Contains(snapshot.Players, p => p.IsGoalie && p.Team == TeamSide.Home && p.NodeId == Rink.IdOf(1, 2));
        Assert.Contains(snapshot.Players, p => p.IsGoalie && p.Team == TeamSide.Away && p.NodeId == Rink.IdOf(9, 2));
        Assert.Equal(new Strength(5, 5), state.Strength);
    }

    [Fact]
    public void LoosePuck_HasNoCarrier()
    {
        ShiftState state = Build(new GridPoint(6, 1));

        Assert.False(state.HasCarrier);
        Assert.Equal(PuckState.Loose, state.PuckState);
        Assert.True(state.Snapshot().IsPuckLoose);
        Assert.Equal(Rink.IdOf(6, 1), state.Snapshot().PuckNodeId);
    }

    [Fact]
    public void GoalNodes_AreRejectedForSkatersAndLoosePuck()
    {
        ShiftState state = Build();

        Assert.Throws<ArgumentException>(() => state.Place(TeamSide.Home, Position.Center, new GridPoint(9, 2)));
        Assert.Throws<ArgumentException>(() => state.SetLoosePuck(new GridPoint(1, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.Place(TeamSide.Home, Position.Center, new GridPoint(11, 2)));
    }

    [Fact]
    public void ApplySystem_MovesTheAwayDefenceInItsOwnView()
    {
        ShiftState state = Build();
        state.GivePuckTo(TeamSide.Home, Position.LeftWing); // home (5,0) = away view (5,4), right side for the away team

        SystemTargets targets = state.ApplySystem(TeamSide.Away, TestPlays.Trap122(), 2);

        Assert.True(targets.Mirrored);
        Assert.Equal(1, targets.RuleIndex);
        IReadOnlyDictionary<Position, GridPoint> awayView = state.NodesInTeamView(TeamSide.Away);
        foreach (KeyValuePair<Position, GridPoint> target in targets.Targets)
        {
            Assert.False(Rink.IsGoalNode(target.Value));
            Assert.True(GridPoint.Chebyshev(Formation()[target.Key], awayView[target.Key]) <= 2);
        }

        // F1 targets puckOffset [-1, 0] of the mirrored puck (5,0) → (4,0), mirrored back → (4,4).
        Assert.Equal(new GridPoint(4, 4), targets.Targets[targets.Roles[SystemRole.F1]]);
        Assert.Equal(Position.RightWing, targets.Roles[SystemRole.F1]);
        Assert.Equal(new GridPoint(4, 4), awayView[Position.RightWing]);
    }

    [Fact]
    public void Place_RejectsATeammatesNode_ButAllowsAnOpponentsNode()
    {
        ShiftState state = Build();
        GridPoint homeLw = state.NodeOf(TeamSide.Home, Position.LeftWing);
        GridPoint awayC = state.NodeOf(TeamSide.Away, Position.Center); // (5,3), free for home

        Assert.Throws<InvalidOperationException>(() => state.Place(TeamSide.Home, Position.Center, homeLw));
        state.Place(TeamSide.Home, Position.Center, awayC);

        Assert.Equal(awayC, state.NodeOf(TeamSide.Home, Position.Center));
    }

    [Fact]
    public void Constructor_RejectsTwoTeammatesOnOneNode()
    {
        Team home = TestPlayers.Team("Home", 1);
        Team away = TestPlayers.Team("Away", 101);
        Dictionary<Position, GridPoint> stacked = Formation();
        stacked[Position.Center] = stacked[Position.LeftWing];

        Assert.Throws<ArgumentException>(() => new ShiftState(
            Rink,
            new OnIceSkaters(home.ForwardLines[0], home.DefencePairs[0]),
            new OnIceSkaters(away.ForwardLines[0], away.DefencePairs[0]),
            home.Goalies[0].Id,
            away.Goalies[0].Id,
            stacked,
            Formation(),
            new GridPoint(5, 2)));
    }

    [Fact]
    public void Move_ResolvesTeammatesAimingAtOneNode()
    {
        ShiftState state = Build();
        var targets = new List<KeyValuePair<Position, GridPoint>>
        {
            new(Position.Center, new GridPoint(7, 2)),
            new(Position.LeftWing, new GridPoint(7, 2)),
        };

        state.Move(TeamSide.Home, targets, 30);

        Assert.Equal(new GridPoint(7, 2), state.NodeOf(TeamSide.Home, Position.Center));
        Assert.Equal(new GridPoint(6, 1), state.NodeOf(TeamSide.Home, Position.LeftWing)); // backs up its path (5,0) → (6,1) → (7,2)
        Assert.Equal(5, state.NodesInTeamView(TeamSide.Home).Values.Distinct().Count());
    }

    [Fact]
    public void ApplySystem_KeepsTheDefenceOnDistinctNodes_WhenTargetsMeet()
    {
        ShiftState state = Build();
        state.GivePuckTo(TeamSide.Home, Position.LeftDefence); // home (3,1) = away view (7,3)
        // Every role aims at the puck carrier.
        var onPuck = TestPlays.Targets(SystemTarget.PuckOffset(0, 0), SystemTarget.PuckOffset(0, 0), SystemTarget.PuckOffset(0, 0), SystemTarget.PuckOffset(0, 0), SystemTarget.PuckOffset(0, 0));
        var system = new DefensiveSystem("swarm", "Swarm", false, new[] { new SystemRule(SystemCondition.Always, onPuck) });

        state.ApplySystem(TeamSide.Away, system, 30);

        IReadOnlyDictionary<Position, GridPoint> awayView = state.NodesInTeamView(TeamSide.Away);
        Assert.Equal(5, awayView.Values.Distinct().Count());
        Assert.Contains(new GridPoint(7, 3), awayView.Values);
    }
}
