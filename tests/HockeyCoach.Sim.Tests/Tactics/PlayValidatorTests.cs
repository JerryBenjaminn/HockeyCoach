using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

public class PlayValidatorTests
{
    private static readonly Rink Rink = TestRinks.Standard();
    private static readonly PlaysConfig Limits = new(4, 2);

    private static KeyValuePair<Position, GridPoint> At(Position p, int x, int y) => TestPlays.At(p, x, y);

    /// <summary>Default start: LW (8,0) has the puck, C (8,3), RW (9,4), LD (7,1), RD (7,3).</summary>
    private static KeyValuePair<Position, GridPoint>[] Start(params KeyValuePair<Position, GridPoint>[] overrides)
    {
        var map = new SortedDictionary<Position, GridPoint>
        {
            [Position.LeftWing] = new GridPoint(8, 0),
            [Position.Center] = new GridPoint(8, 3),
            [Position.RightWing] = new GridPoint(9, 4),
            [Position.LeftDefence] = new GridPoint(7, 1),
            [Position.RightDefence] = new GridPoint(7, 3),
        };
        foreach (var o in overrides)
        {
            map[o.Key] = o.Value;
        }

        return map.ToArray();
    }

    private static IReadOnlyList<string> Validate(IReadOnlyList<Beat> beats, KeyValuePair<Position, GridPoint>[]? start = null, Position carrier = Position.LeftWing, PlayType type = PlayType.OffensiveZone, string? spot = null)
    {
        return PlayValidator.Validate(TestPlays.Build(carrier, start ?? Start(), beats, type, spot), Rink, Limits);
    }

    private static Beat B(PlayAction action, params KeyValuePair<Position, GridPoint>[] moves) => new(moves, action);

    [Fact]
    public void SchemaExample_IsValid_AndSoIsItsMirror()
    {
        Assert.Empty(PlayValidator.Validate(TestPlays.PointShotScreen(), Rink, Limits));
        Assert.Empty(PlayValidator.Validate(PlayMirror.Mirror(TestPlays.PointShotScreen(), Rink), Rink, Limits));
    }

    [Fact]
    public void NoBeats_And_TooManyBeats_AreRejected()
    {
        Assert.Contains(Validate(Array.Empty<Beat>()), e => e.StartsWith("beats: must have 1..4 beats", StringComparison.Ordinal));
        Beat pass = B(PlayAction.Pass(Position.LeftWing, Position.LeftDefence));
        Beat back = B(PlayAction.Pass(Position.LeftDefence, Position.LeftWing));
        Assert.Contains(Validate(new[] { pass, back, pass, back, pass }), e => e.StartsWith("beats: must have 1..4 beats", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingStartPosition_IsRejected()
    {
        var start = Start().Where(p => p.Key != Position.RightDefence).ToArray();

        Assert.Contains(Validate(new[] { B(PlayAction.Shoot(Position.LeftWing)) }, start), e => e.StartsWith("start.positions.RD: missing", StringComparison.Ordinal));
    }

    [Fact]
    public void GoalNode_IsRejectedForStartMoveSkateAndDump()
    {
        Assert.Contains(Validate(new[] { B(PlayAction.Shoot(Position.LeftWing)) }, Start(At(Position.RightWing, 9, 2))), e => e.StartsWith("start.positions.RW: (9,2) is a goal node", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.Shoot(Position.LeftWing), At(Position.Center, 9, 2)) }), e => e.StartsWith("beats[0].moves.C: (9,2) is a goal node", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.Skate(Position.LeftWing, new GridPoint(9, 2))) }, Start(At(Position.LeftWing, 8, 1))), e => e.StartsWith("beats[0].action.to: (9,2) is a goal node", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.Dump(Position.LeftWing, new GridPoint(9, 2))) }), e => e.StartsWith("beats[0].action.to: (9,2) is a goal node", StringComparison.Ordinal));
    }

    [Fact]
    public void NodeOutsideGrid_IsRejected()
    {
        Assert.Contains(Validate(new[] { B(PlayAction.Shoot(Position.LeftWing), At(Position.Center, 8, 5)) }), e => e.StartsWith("beats[0].moves.C: (8,5) is outside", StringComparison.Ordinal));
    }

    [Fact]
    public void TwoPlayersOnOneNode_IsRejectedAtStartAndAfterABeat()
    {
        Assert.Contains(Validate(new[] { B(PlayAction.Shoot(Position.LeftWing)) }, Start(At(Position.Center, 8, 0))), e => e.StartsWith("start.positions: C and LW are both at (8,0)", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.Shoot(Position.LeftWing), At(Position.Center, 7, 3)) }), e => e.StartsWith("beats[0]: C and RD are both at (7,3)", StringComparison.Ordinal));
    }

    [Fact]
    public void MoveOrSkateLongerThanMaxNodesPerBeat_IsRejected()
    {
        Assert.Contains(Validate(new[] { B(PlayAction.Shoot(Position.LeftWing), At(Position.LeftDefence, 4, 1)) }), e => e.StartsWith("beats[0].moves.LD: (7,1) → (4,1) is 3 nodes", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.Skate(Position.LeftWing, new GridPoint(5, 0))) }), e => e.StartsWith("beats[0].action.to: (8,0) → (5,0) is 3 nodes", StringComparison.Ordinal));
    }

    [Fact]
    public void MovingThePuckCarrier_IsRejected()
    {
        Assert.Contains(Validate(new[] { B(PlayAction.Shoot(Position.LeftWing), At(Position.LeftWing, 8, 1)) }), e => e.StartsWith("beats[0].moves.LW: the puck carrier moves only with skate", StringComparison.Ordinal));
    }

    [Fact]
    public void MovingTheDriveNetPlayer_And_MovingToNetFront_AreRejected()
    {
        Assert.Contains(Validate(new[] { B(PlayAction.DriveNet(Position.RightWing), At(Position.RightWing, 9, 3)) }), e => e.StartsWith("beats[0].moves.RW: the driveNet player", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.Shoot(Position.LeftWing), At(Position.Center, 8, 2)) }), e => e.StartsWith("beats[0].moves.C: a move must not end at the net front", StringComparison.Ordinal));
    }

    [Fact]
    public void ActionByNonCarrier_IsRejected()
    {
        Assert.Contains(Validate(new[] { B(PlayAction.Shoot(Position.Center)) }), e => e.StartsWith("beats[0].action.by: C is not the puck carrier", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.Pass(Position.Center, Position.LeftWing)) }), e => e.StartsWith("beats[0].action.from: C is not the puck carrier", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.Pass(Position.LeftWing, Position.LeftWing)) }), e => e == "beats[0].action.to: must differ from from");
    }

    [Fact]
    public void CarrierFollowsPasses()
    {
        var beats = new[]
        {
            B(PlayAction.Pass(Position.LeftWing, Position.LeftDefence)),
            B(PlayAction.Shoot(Position.LeftDefence), At(Position.LeftWing, 9, 1)),
        };

        Assert.Empty(Validate(beats));
    }

    [Fact]
    public void DriveNet_RejectsCarrierPlayerAlreadyThereAndTooFar()
    {
        Assert.Contains(Validate(new[] { B(PlayAction.DriveNet(Position.LeftWing)) }), e => e.StartsWith("beats[0].action.by: LW has the puck", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.DriveNet(Position.Center)) }, Start(At(Position.Center, 8, 2))), e => e.StartsWith("beats[0].action.by: C is already at the net front", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.DriveNet(Position.Center)) }, Start(At(Position.Center, 5, 3))), e => e.StartsWith("beats[0].action.by: C at (5,3) is more than plays.maxNodesPerBeat", StringComparison.Ordinal));
    }

    [Fact]
    public void Dump_RequiresOffensiveTargetAndCarrierNotBehindCentre()
    {
        Assert.Contains(Validate(new[] { B(PlayAction.Dump(Position.LeftWing, new GridPoint(6, 0))) }), e => e.StartsWith("beats[0].action.to: (6,0) is not in the offensive zone", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { B(PlayAction.Dump(Position.LeftWing, new GridPoint(10, 0))) }, Start(At(Position.LeftWing, 4, 0))), e => e.StartsWith("beats[0].action.by: LW at (4,0) is behind the centre line", StringComparison.Ordinal));
        Assert.Empty(Validate(new[] { B(PlayAction.Dump(Position.LeftWing, new GridPoint(10, 0))) }, Start(At(Position.LeftWing, 5, 0))));
    }

    [Fact]
    public void BeatsAfterShootOrDump_AreRejected()
    {
        var beats = new[] { B(PlayAction.Shoot(Position.LeftWing)), B(PlayAction.Shoot(Position.LeftWing)) };

        Assert.Contains(Validate(beats), e => e == "beats[1]: no beats may follow shoot");
    }

    [Fact]
    public void FaceoffSpot_IsRequiredForFaceoffPlaysOnly_AndMustExist()
    {
        Beat shoot = B(PlayAction.Shoot(Position.LeftWing));
        Assert.Contains(Validate(new[] { shoot }, type: PlayType.Faceoff), e => e == "faceoffSpot: missing (faceoff play)");
        Assert.Contains(Validate(new[] { shoot }, type: PlayType.Faceoff, spot: "nowhere"), e => e.StartsWith("faceoffSpot: nowhere is not a faceoff spot", StringComparison.Ordinal));
        Assert.Contains(Validate(new[] { shoot }, spot: "offensiveLeft"), e => e == "faceoffSpot: only faceoff plays have a faceoff spot");
        Assert.Empty(Validate(new[] { shoot }, type: PlayType.Faceoff, spot: "offensiveLeft"));
    }
}

public class PassThroughGoalTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    private static Play Play(int px, int py, int rx, int ry)
    {
        return TestPlays.Build(
            Position.Center,
            new[]
            {
                TestPlays.At(Position.Center, px, py), TestPlays.At(Position.LeftWing, rx, ry), TestPlays.At(Position.RightWing, 7, 4),
                TestPlays.At(Position.LeftDefence, 6, 0), TestPlays.At(Position.RightDefence, 6, 4),
            },
            new[] { new Beat(TestPlays.NoMoves, PlayAction.Pass(Position.Center, Position.LeftWing)) });
    }

    [Fact]
    public void PassStraightThroughTheGoal_IsRejected()
    {
        Assert.Contains(PlayValidator.Validate(Play(10, 2, 8, 2), Rink, new PlaysConfig(4, 2)), e => e.Contains("goes straight through a goal (D-051)", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(10, 1, 8, 3)] // diagonal from behind the net: allowed
    [InlineData(10, 2, 9, 3)]
    [InlineData(8, 2, 7, 2)]  // same side of the goal
    public void OtherPassesAroundTheGoal_AreAllowed(int px, int py, int rx, int ry)
    {
        Assert.Empty(PlayValidator.Validate(Play(px, py, rx, ry), Rink, new PlaysConfig(4, 2)));
    }
}
