using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Shift;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

/// <summary>M-7 presser (D-072, Q-054 recommendations 1–4 and 6).</summary>
public class PresserSelectorTests
{
    private static readonly Rink Rink = ShiftTestData.Data.Rink;

    private static DefensiveSystem System(string id) => HarnessTeams.System(ShiftTestData.Data, id);

    private static Dictionary<Position, GridPoint> Skaters(
        (int X, int Y) c, (int X, int Y) lw, (int X, int Y) rw, (int X, int Y) ld, (int X, int Y) rd) => new()
    {
        [Position.Center] = new GridPoint(c.X, c.Y),
        [Position.LeftWing] = new GridPoint(lw.X, lw.Y),
        [Position.RightWing] = new GridPoint(rw.X, rw.Y),
        [Position.LeftDefence] = new GridPoint(ld.X, ld.Y),
        [Position.RightDefence] = new GridPoint(rd.X, rd.Y),
    };

    private static Position Presser(DefensiveSystem system, GridPoint puck, Dictionary<Position, GridPoint> skaters, bool pinched = false)
    {
        Assert.True(PresserSelector.FromSystem(system, Rink, puck, PuckState.Controlled, skaters, pinched, out Position presser));
        return presser;
    }

    [Fact]
    public void Forecheck212_NeutralZone_PicksF1OverF2()
    {
        // Puck (5, 1): neutral-zone rule, F1 [0, 0] and F2 [-1, 1]. LW is the closest forward (F1), C the next (F2).
        var skaters = Skaters(c: (4, 2), lw: (6, 1), rw: (5, 3), ld: (3, 1), rd: (3, 3));

        IReadOnlyDictionary<SystemRole, Position> roles = RoleAssigner.Assign(skaters, new GridPoint(5, 1), Rink);
        Assert.Equal(Position.LeftWing, roles[SystemRole.F1]);
        Assert.Equal(Position.Center, roles[SystemRole.F2]);

        Assert.Equal(Position.LeftWing, Presser(System("forecheck212"), new GridPoint(5, 1), skaters));
    }

    [Fact]
    public void Forecheck212_NeutralZoneCentreLane_PicksF1OverF2WithOffsetMinusOneZero()
    {
        // Puck (5, 2): F2 [-1, 0] has Chebyshev 1, F1 [0, 0] has 0.
        var skaters = Skaters(c: (6, 2), lw: (4, 1), rw: (4, 3), ld: (3, 1), rd: (3, 3));

        Assert.Equal(Position.Center, Presser(System("forecheck212"), new GridPoint(5, 2), skaters));
    }

    [Fact]
    public void Trap122_OffensiveZone_PicksF1WithOffsetMinusOneOne()
    {
        var skaters = Skaters(c: (6, 1), lw: (7, 2), rw: (6, 3), ld: (4, 1), rd: (4, 3));

        // Puck (8, 1): generic offensive-zone rule, F1 [-1, 1] is the only puckOffset.
        Assert.Equal(Position.LeftWing, Presser(System("trap122"), new GridPoint(8, 1), skaters));
    }

    [Fact]
    public void Trap122_MirroredPuck_StillPicksF1()
    {
        var skaters = Skaters(c: (6, 1), lw: (6, 3), rw: (7, 2), ld: (4, 1), rd: (4, 3));

        // Puck (8, 3) on the right: mirrorY applies, F1 [-1, -1]; RW is F1.
        Assert.Equal(Position.RightWing, Presser(System("trap122"), new GridPoint(8, 3), skaters));
    }

    [Fact]
    public void Presser_IsF1_EvenWhenAnotherDefenderStandsOnThePuck()
    {
        // LD on the puck node (5, 1); F1 (LW) is a node away. Only F1 presses.
        var skaters = Skaters(c: (4, 3), lw: (6, 1), rw: (5, 4), ld: (5, 1), rd: (3, 3));

        Assert.Equal(Position.LeftWing, Presser(System("forecheck212"), new GridPoint(5, 1), skaters));
    }

    [Fact]
    public void Pinch_D1WinsOverAnOffsetF1()
    {
        // Trap122, puck (8, 0): F1 [-1, 1]; pinch-D1 has length 0 and wins.
        var skaters = Skaters(c: (6, 1), lw: (7, 1), rw: (6, 3), ld: (6, 0), rd: (4, 3));

        Assert.Equal(Position.LeftDefence, Presser(System("trap122"), new GridPoint(8, 0), skaters, pinched: true));
        Assert.Equal(Position.LeftWing, Presser(System("trap122"), new GridPoint(8, 0), skaters, pinched: false));
    }

    [Fact]
    public void Pinch_F1WithZeroOffsetWinsOverD1ByRoleOrder()
    {
        var skaters = Skaters(c: (6, 1), lw: (7, 1), rw: (6, 3), ld: (6, 0), rd: (4, 3));

        Assert.Equal(Position.LeftWing, Presser(System("forecheck212"), new GridPoint(8, 0), skaters, pinched: true));
    }

    private static DefensiveSystem OneRule(SystemTarget f1, SystemTarget f2, SystemTarget f3, SystemTarget d1, SystemTarget d2) =>
        new("custom", "Custom", false, new[] { new SystemRule(SystemCondition.Always, TestPlays.Targets(f1, f2, f3, d1, d2)) });

    // F1 at (5, 1) next to the puck (6, 1); C (F2) at (4, 1); RW (F3) at (3, 1); LD (D1) at (4, 2); RD (D2) at (2, 3).
    private static readonly Dictionary<Position, GridPoint> Spread = Skaters(c: (4, 1), lw: (5, 1), rw: (3, 1), ld: (4, 2), rd: (2, 3));

    [Fact]
    public void EqualLengths_GoByRoleOrder_D2BeforeF2()
    {
        DefensiveSystem system = OneRule(
            SystemTarget.Node(new GridPoint(4, 0)),
            SystemTarget.PuckOffset(-1, 0),
            SystemTarget.Node(new GridPoint(4, 3)),
            SystemTarget.Node(new GridPoint(3, 1)),
            SystemTarget.PuckOffset(0, 1));

        Assert.Equal(Position.RightDefence, Presser(system, new GridPoint(6, 1), Spread));
    }

    [Fact]
    public void EqualChebyshev_GoesByManhattan_BeforeRoleOrder()
    {
        DefensiveSystem system = OneRule(
            SystemTarget.PuckOffset(-1, 1),
            SystemTarget.Node(new GridPoint(4, 0)),
            SystemTarget.PuckOffset(-1, 0),
            SystemTarget.Node(new GridPoint(3, 1)),
            SystemTarget.Node(new GridPoint(2, 2)));

        Assert.Equal(Position.RightWing, Presser(system, new GridPoint(6, 1), Spread));
    }

    [Fact]
    public void ShorterOffset_Wins_OverEarlierRole()
    {
        DefensiveSystem system = OneRule(
            SystemTarget.PuckOffset(-2, 0),
            SystemTarget.Node(new GridPoint(4, 0)),
            SystemTarget.PuckOffset(0, 1),
            SystemTarget.Node(new GridPoint(3, 1)),
            SystemTarget.Node(new GridPoint(2, 2)));

        Assert.Equal(Position.RightWing, Presser(system, new GridPoint(6, 1), Spread));
    }

    [Fact]
    public void NoPuckOffset_NoPresser()
    {
        DefensiveSystem system = OneRule(
            SystemTarget.Node(new GridPoint(5, 1)),
            SystemTarget.Node(new GridPoint(4, 0)),
            SystemTarget.Node(new GridPoint(4, 3)),
            SystemTarget.Node(new GridPoint(3, 1)),
            SystemTarget.Node(new GridPoint(2, 2)));

        Assert.False(PresserSelector.FromSystem(system, Rink, new GridPoint(6, 1), PuckState.Controlled, Spread, false, out _));
        Assert.True(PresserSelector.FromSystem(system, Rink, new GridPoint(6, 1), PuckState.Controlled, Spread, true, out Position pinch));
        Assert.Equal(Position.LeftDefence, pinch);
    }

    [Fact]
    public void Nearest_IsSmallestChebyshev()
    {
        var defenders = Skaters(c: (3, 3), lw: (6, 2), rw: (2, 2), ld: (5, 4), rd: (8, 2));

        Assert.True(PresserSelector.Nearest(defenders, new GridPoint(5, 2), out Position presser));
        Assert.Equal(Position.LeftWing, presser);
    }

    [Fact]
    public void Nearest_BreaksChebyshevTieByManhattan()
    {
        // LW (6, 3) and RD (6, 2): both Chebyshev 1 from (5, 2); RD has Manhattan 1.
        var defenders = Skaters(c: (2, 2), lw: (6, 3), rw: (2, 0), ld: (8, 0), rd: (6, 2));

        Assert.True(PresserSelector.Nearest(defenders, new GridPoint(5, 2), out Position presser));
        Assert.Equal(Position.RightDefence, presser);
    }

    [Fact]
    public void Nearest_FullTie_GoesByPositionOrder()
    {
        // RW (4, 2) and LD (6, 2) both at Chebyshev 1, Manhattan 1; RW comes first in C, LW, RW, LD, RD.
        var defenders = Skaters(c: (2, 0), lw: (8, 4), rw: (4, 2), ld: (6, 2), rd: (8, 0));

        Assert.True(PresserSelector.Nearest(defenders, new GridPoint(5, 2), out Position presser));
        Assert.Equal(Position.RightWing, presser);
    }
}
