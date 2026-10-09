using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

public class SystemEvaluationTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    private static Dictionary<Position, GridPoint> Skaters(int cx, int cy, int lwx, int lwy, int rwx, int rwy, int ldx, int ldy, int rdx, int rdy) => new()
    {
        [Position.Center] = new GridPoint(cx, cy),
        [Position.LeftWing] = new GridPoint(lwx, lwy),
        [Position.RightWing] = new GridPoint(rwx, rwy),
        [Position.LeftDefence] = new GridPoint(ldx, ldy),
        [Position.RightDefence] = new GridPoint(rdx, rdy),
    };

    [Fact]
    public void Roles_FollowDistanceToThePuck()
    {
        var skaters = Skaters(5, 2, 7, 1, 4, 3, 3, 1, 2, 3);

        IReadOnlyDictionary<SystemRole, Position> roles = RoleAssigner.Assign(skaters, new GridPoint(8, 1), Rink);

        Assert.Equal(Position.LeftWing, roles[SystemRole.F1]);
        Assert.Equal(Position.Center, roles[SystemRole.F2]);
        Assert.Equal(Position.RightWing, roles[SystemRole.F3]);
        Assert.Equal(Position.LeftDefence, roles[SystemRole.D1]);
        Assert.Equal(Position.RightDefence, roles[SystemRole.D2]);
    }

    [Fact]
    public void Roles_BreakChebyshevTiesByManhattan()
    {
        // LW (6,1) and RW (6,3) are both 2 Chebyshev from the puck (8,2); C (6,0) too, but with Manhattan 4.
        var skaters = Skaters(6, 0, 6, 1, 6, 4, 3, 1, 3, 3);

        IReadOnlyDictionary<SystemRole, Position> roles = RoleAssigner.Assign(skaters, new GridPoint(8, 2), Rink);

        Assert.Equal(Position.LeftWing, roles[SystemRole.F1]);
    }

    [Theory]
    [InlineData(1, Position.LeftWing, Position.LeftDefence)]   // puck on the left: LW and LD first
    [InlineData(2, Position.LeftWing, Position.LeftDefence)]   // middle lane counts as left
    [InlineData(3, Position.RightWing, Position.RightDefence)] // puck on the right: RW and RD first
    public void Roles_FullTie_GoesToCentreThenPuckSideWingerAndPuckSideDefence(int puckY, Position f2, Position d1)
    {
        // Every forward 2 away (Chebyshev and Manhattan) from the puck at (5, y): C, LW, RW all tie.
        var skaters = new Dictionary<Position, GridPoint>
        {
            [Position.Center] = new GridPoint(3, puckY),
            [Position.LeftWing] = new GridPoint(7, puckY),
            [Position.RightWing] = new GridPoint(5, puckY == 2 ? 0 : (puckY == 1 ? 3 : 1)),
            [Position.LeftDefence] = new GridPoint(2, puckY),
            [Position.RightDefence] = new GridPoint(8, puckY),
        };

        IReadOnlyDictionary<SystemRole, Position> roles = RoleAssigner.Assign(skaters, new GridPoint(5, puckY), Rink);

        Assert.Equal(Position.Center, roles[SystemRole.F1]);
        Assert.Equal(f2, roles[SystemRole.F2]);
        Assert.Equal(d1, roles[SystemRole.D1]);
    }

    [Fact]
    public void Resolve_UsesFirstMatchingRule()
    {
        var skaters = Skaters(5, 2, 7, 1, 4, 3, 3, 1, 2, 3);

        SystemTargets targets = SystemTargetResolver.Resolve(TestPlays.Trap122(), Rink, new GridPoint(8, 1), PuckState.Controlled, skaters);

        Assert.Equal(0, targets.RuleIndex);
        Assert.False(targets.Mirrored);
        Assert.Equal(new GridPoint(8, 1), targets.Targets[Position.LeftWing]); // F1 puckOffset [0, 0]
        Assert.Equal(new GridPoint(6, 1), targets.Targets[Position.Center]);   // F2
        Assert.Equal(new GridPoint(6, 3), targets.Targets[Position.RightWing]); // F3
        Assert.Equal(new GridPoint(4, 1), targets.Targets[Position.LeftDefence]);
        Assert.Equal(new GridPoint(4, 3), targets.Targets[Position.RightDefence]);
    }

    [Fact]
    public void Resolve_FallsBackToTheLastRule()
    {
        var skaters = Skaters(5, 2, 7, 1, 4, 3, 3, 1, 2, 3);

        SystemTargets targets = SystemTargetResolver.Resolve(TestPlays.Trap122(), Rink, new GridPoint(5, 1), PuckState.Loose, skaters);

        Assert.Equal(1, targets.RuleIndex);
        Assert.Equal(new GridPoint(4, 1), targets.Targets[targets.Roles[SystemRole.F1]]); // puckOffset [-1, 0]
    }

    [Fact]
    public void Resolve_MirrorsForPuckOnTheRight_AndMapsTargetsBack()
    {
        DefensiveSystem system = new(
            "side",
            "Side",
            true,
            new[]
            {
                new SystemRule(
                    SystemCondition.Always,
                    TestPlays.Targets(SystemTarget.PuckOffset(-1, 1), SystemTarget.Node(new GridPoint(6, 0)), SystemTarget.Node(new GridPoint(6, 1)), SystemTarget.Node(new GridPoint(4, 1)), SystemTarget.Node(new GridPoint(3, 2))))
            });
        var skaters = Skaters(5, 2, 4, 1, 7, 3, 3, 1, 2, 3);

        SystemTargets left = SystemTargetResolver.Resolve(system, Rink, new GridPoint(8, 1), PuckState.Controlled, skaters);
        SystemTargets right = SystemTargetResolver.Resolve(system, Rink, new GridPoint(8, 3), PuckState.Controlled, skaters);

        Assert.False(left.Mirrored);
        Assert.True(right.Mirrored);
        Assert.Equal(new GridPoint(7, 2), left.Targets[left.Roles[SystemRole.F1]]);
        Assert.Equal(new GridPoint(7, 2), right.Targets[right.Roles[SystemRole.F1]]); // offset [-1, 1] → [-1, -1] from (8, 3)
        Assert.Equal(new GridPoint(6, 4), right.Targets[right.Roles[SystemRole.F2]]);
        Assert.Equal(new GridPoint(3, 2), right.Targets[right.Roles[SystemRole.D2]]);
    }

    [Fact]
    public void PuckOffset_IsClampedPerAxis()
    {
        GridPoint node = SystemTargetResolver.TargetNode(SystemTarget.PuckOffset(2, -3), new GridPoint(9, 1), Rink);

        Assert.Equal(new GridPoint(10, 0), node);
    }

    [Theory]
    [InlineData(2, 2, -1, 0, 2, 2)] // (1,2) own goal → own net front (2,2)
    [InlineData(10, 2, -1, 0, 8, 2)] // (9,2) opponent goal → net front (8,2)
    [InlineData(0, 2, 1, 0, 2, 2)]
    public void PuckOffset_OnAGoalNode_MovesToTheNetFront(int px, int py, int dx, int dy, int ex, int ey)
    {
        Assert.Equal(new GridPoint(ex, ey), SystemTargetResolver.TargetNode(SystemTarget.PuckOffset(dx, dy), new GridPoint(px, py), Rink));
    }

    [Fact]
    public void Resolve_IsDeterministic()
    {
        var skaters = Skaters(5, 2, 7, 1, 4, 3, 3, 1, 2, 3);

        SystemTargets a = SystemTargetResolver.Resolve(TestPlays.Trap122(), Rink, new GridPoint(6, 3), PuckState.Controlled, skaters);
        SystemTargets b = SystemTargetResolver.Resolve(TestPlays.Trap122(), Rink, new GridPoint(6, 3), PuckState.Controlled, skaters);

        Assert.Equal(a.Targets, b.Targets);
        Assert.Equal(a.Roles, b.Roles);
    }
}
