using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

/// <summary>D-058: one skater per team per node.</summary>
public class OccupancyResolverTests
{
    private static readonly Rink Rink = TestRinks.Standard();
    private static readonly GridPoint Puck = new(5, 0);

    private static GridPoint P(int x, int y) => new(x, y);

    private static Dictionary<Position, GridPoint> Team(GridPoint lw, GridPoint c, GridPoint rw, GridPoint ld, GridPoint rd) => new()
    {
        { Position.LeftWing, lw }, { Position.Center, c }, { Position.RightWing, rw }, { Position.LeftDefence, ld }, { Position.RightDefence, rd },
    };

    private static OccupancyMove Move(Position position, GridPoint from, GridPoint to) =>
        new(position, SystemMovement.Path(from, to, Puck, 30, Rink));

    [Fact]
    public void Resolve_KeepsIdealEnds_WhenNothingCollides()
    {
        var current = Team(P(5, 0), P(5, 2), P(5, 4), P(3, 1), P(3, 3));

        var result = OccupancyResolver.Resolve(current, new[] { Move(Position.Center, P(5, 2), P(7, 2)) }, Rink);

        Assert.Equal(P(7, 2), result[Position.Center]);
        Assert.Equal(P(5, 0), result[Position.LeftWing]);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void Resolve_BacksUpAlongThePath_WhenAStationaryTeammateHoldsTheEnd()
    {
        var current = Team(P(7, 2), P(4, 2), P(5, 4), P(3, 1), P(3, 3));

        var result = OccupancyResolver.Resolve(current, new[] { Move(Position.Center, P(4, 2), P(7, 2)) }, Rink);

        Assert.Equal(P(6, 2), result[Position.Center]);
        Assert.Equal(P(7, 2), result[Position.LeftWing]);
    }

    [Fact]
    public void Resolve_LaterMoverBacksUp_WhenAnEarlierMoverTookTheEnd()
    {
        var current = Team(P(5, 0), P(4, 2), P(5, 4), P(3, 1), P(3, 3));
        var moves = new[] { Move(Position.LeftWing, P(5, 0), P(7, 2)), Move(Position.Center, P(4, 2), P(7, 2)) };

        var result = OccupancyResolver.Resolve(current, moves, Rink);

        Assert.Equal(P(7, 2), result[Position.LeftWing]);
        Assert.Equal(P(6, 2), result[Position.Center]);
    }

    [Fact]
    public void Resolve_AMoverMayTakeTheNodeALaterMoverLeaves()
    {
        var current = Team(P(5, 0), P(5, 1), P(5, 4), P(3, 1), P(3, 3));
        var moves = new[] { Move(Position.LeftWing, P(5, 0), P(5, 1)), Move(Position.Center, P(5, 1), P(6, 1)) };

        var result = OccupancyResolver.Resolve(current, moves, Rink);

        Assert.Equal(P(5, 1), result[Position.LeftWing]);
        Assert.Equal(P(6, 1), result[Position.Center]);
    }

    [Fact]
    public void Resolve_GoesToTheNearestFreeNode_WhenTheWholePathIsTaken()
    {
        // C stays at (4, 2) whose path is just itself; LW, moving first, takes (4, 2).
        var current = Team(P(4, 1), P(4, 2), P(5, 4), P(3, 1), P(3, 3));
        var moves = new[] { Move(Position.LeftWing, P(4, 1), P(4, 2)), new OccupancyMove(Position.Center, new[] { P(4, 2) }) };

        var result = OccupancyResolver.Resolve(current, moves, Rink);

        // Ring 1 around (4, 2) minus (3, 1) and (3, 3): (3, 2) is closest to the own goal (1, 2).
        Assert.Equal(P(4, 2), result[Position.LeftWing]);
        Assert.Equal(P(3, 2), result[Position.Center]);
    }

    [Fact]
    public void NearestFree_PrefersOwnGoal_ThenMiddleLane_ThenSmallerY_AndSkipsGoalNodes()
    {
        // Around the net front (2, 2): (1, 2) is the goal; (1, 1) and (1, 3) tie on goal distance and lane; smaller y wins.
        Assert.Equal(P(1, 1), OccupancyResolver.NearestFree(P(2, 2), new HashSet<GridPoint>(), Rink));
        Assert.Equal(P(1, 3), OccupancyResolver.NearestFree(P(2, 2), new HashSet<GridPoint> { P(1, 1) }, Rink));

        // Ring 1 fully taken: ring 2.
        var ring1 = new HashSet<GridPoint> { P(4, 0), P(4, 1), P(5, 1), P(6, 0), P(6, 1) };
        GridPoint ring2 = OccupancyResolver.NearestFree(P(5, 0), ring1, Rink);
        Assert.Equal(2, GridPoint.Chebyshev(ring2, P(5, 0)));
        Assert.Equal(P(3, 2), ring2);
    }

    [Fact]
    public void Resolve_NeverLeavesTwoTeammatesOnOneNode_ForAnyTargets()
    {
        // Every forward and defenceman aims at the same node from a spread formation; all five end on distinct non-goal nodes.
        foreach (GridPoint target in new[] { P(2, 2), P(8, 2), P(0, 0), P(10, 4), P(5, 2) })
        {
            var current = Team(P(5, 0), P(5, 2), P(5, 4), P(3, 1), P(3, 3));
            var moves = PositionOrder.All.Select(p => Move(p, current[p], target)).ToList();

            var result = OccupancyResolver.Resolve(current, moves, Rink);

            Assert.Equal(5, result.Values.Distinct().Count());
            Assert.All(result.Values, n => Assert.False(Rink.IsGoalNode(n)));
            Assert.Equal(target, result[Position.Center]);
        }
    }

    [Fact]
    public void AttackOrder_PutsTheCarrierFirst_ThenPositionOrder()
    {
        var movers = new HashSet<Position> { Position.RightDefence, Position.LeftWing, Position.Center };

        Assert.Equal(new[] { Position.RightDefence, Position.Center, Position.LeftWing }, OccupancyResolver.AttackOrder(Position.RightDefence, movers));
        Assert.Equal(new[] { Position.Center, Position.LeftWing, Position.RightDefence }, OccupancyResolver.AttackOrder(Position.RightWing, movers));
        Assert.Equal(new[] { Position.Center, Position.LeftWing, Position.RightDefence }, OccupancyResolver.AttackOrder(null, movers));
    }

    [Fact]
    public void SystemOrder_IsF1D1D2F2F3()
    {
        Assert.Equal(new[] { SystemRole.F1, SystemRole.D1, SystemRole.D2, SystemRole.F2, SystemRole.F3 }, OccupancyResolver.SystemOrder);
    }

    [Fact]
    public void Path_StartsAtTheCurrentNode_AndEndsWhereAdvanceEnds()
    {
        IReadOnlyList<GridPoint> path = SystemMovement.Path(P(4, 0), P(8, 2), Puck, 3, Rink);

        Assert.Equal(new[] { P(4, 0), P(5, 1), P(6, 2), P(7, 2) }, path);
        Assert.Equal(SystemMovement.Advance(P(4, 0), P(8, 2), Puck, 3, Rink), path[^1]);
    }
}
