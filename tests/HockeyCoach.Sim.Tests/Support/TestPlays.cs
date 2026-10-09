using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Tests.Support;

/// <summary>Hand-built plays and systems for tests (examples of docs/data-schema.md).</summary>
internal static class TestPlays
{
    public static KeyValuePair<Position, GridPoint> At(Position position, int x, int y) => new(position, new GridPoint(x, y));

    public static KeyValuePair<Position, GridPoint>[] NoMoves => Array.Empty<KeyValuePair<Position, GridPoint>>();

    /// <summary>The data-schema.md example <c>pointShotScreen</c>.</summary>
    public static Play PointShotScreen()
    {
        return new Play(
            "pointShotScreen",
            "Point shot with screen",
            PlayType.OffensiveZone,
            null,
            Position.LeftWing,
            new[]
            {
                At(Position.LeftWing, 8, 0), At(Position.Center, 8, 3), At(Position.RightWing, 9, 4),
                At(Position.LeftDefence, 7, 1), At(Position.RightDefence, 7, 3),
            },
            new[]
            {
                new Beat(new[] { At(Position.Center, 9, 3) }, PlayAction.Pass(Position.LeftWing, Position.LeftDefence)),
                new Beat(new[] { At(Position.LeftWing, 9, 1) }, PlayAction.DriveNet(Position.RightWing)),
                new Beat(NoMoves, PlayAction.Shoot(Position.LeftDefence)),
            });
    }

    /// <summary>A one-beat play with the given start nodes, carrier and beats.</summary>
    public static Play Build(
        Position carrier,
        IEnumerable<KeyValuePair<Position, GridPoint>> start,
        IReadOnlyList<Beat> beats,
        PlayType type = PlayType.OffensiveZone,
        string? faceoffSpot = null,
        string id = "testPlay")
    {
        return new Play(id, "Test play", type, faceoffSpot!, carrier, start, beats);
    }

    /// <summary>The data-schema.md example <c>trap122</c>.</summary>
    public static DefensiveSystem Trap122(bool mirrorY = true)
    {
        return new DefensiveSystem(
            "trap122",
            "1-2-2 trap",
            mirrorY,
            new[]
            {
                new SystemRule(
                    new SystemCondition(new[] { RinkZone.Offensive }, null, null, null),
                    Targets(SystemTarget.PuckOffset(0, 0), SystemTarget.Node(new GridPoint(6, 1)), SystemTarget.Node(new GridPoint(6, 3)), SystemTarget.Node(new GridPoint(4, 1)), SystemTarget.Node(new GridPoint(4, 3)))),
                new SystemRule(
                    SystemCondition.Always,
                    Targets(SystemTarget.PuckOffset(-1, 0), SystemTarget.Node(new GridPoint(3, 1)), SystemTarget.Node(new GridPoint(3, 3)), SystemTarget.Node(new GridPoint(2, 1)), SystemTarget.Node(new GridPoint(2, 2)))),
            });
    }

    public static KeyValuePair<SystemRole, SystemTarget>[] Targets(SystemTarget f1, SystemTarget f2, SystemTarget f3, SystemTarget d1, SystemTarget d2)
    {
        return new[]
        {
            new KeyValuePair<SystemRole, SystemTarget>(SystemRole.F1, f1),
            new KeyValuePair<SystemRole, SystemTarget>(SystemRole.F2, f2),
            new KeyValuePair<SystemRole, SystemTarget>(SystemRole.F3, f3),
            new KeyValuePair<SystemRole, SystemTarget>(SystemRole.D1, d1),
            new KeyValuePair<SystemRole, SystemTarget>(SystemRole.D2, d2),
        };
    }
}
