using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

public class SystemMovementTests
{
    private static readonly Rink Rink = TestRinks.Standard();
    private static readonly GridPoint LeftPuck = new(5, 1);
    private static readonly GridPoint RightPuck = new(5, 3);

    [Fact]
    public void Advance_MovesDiagonallyThenStraight_UpToMaxSteps()
    {
        Assert.Equal(new GridPoint(6, 2), SystemMovement.Advance(new GridPoint(4, 0), new GridPoint(8, 2), LeftPuck, 2, Rink));
        Assert.Equal(new GridPoint(7, 2), SystemMovement.Advance(new GridPoint(4, 0), new GridPoint(8, 2), LeftPuck, 3, Rink));
    }

    [Fact]
    public void Advance_StopsAtTheTarget()
    {
        Assert.Equal(new GridPoint(5, 1), SystemMovement.Advance(new GridPoint(4, 1), new GridPoint(5, 1), LeftPuck, 2, Rink));
    }

    [Fact]
    public void DiagonalStepOntoGoal_BecomesStraightStepInX()
    {
        // (8,1) → target (10,3): diagonal step would be (9,2), the goal node.
        Assert.Equal(new GridPoint(9, 1), SystemMovement.Step(new GridPoint(8, 1), new GridPoint(10, 3), LeftPuck, Rink));
    }

    [Theory]
    [InlineData(true, 3)]
    [InlineData(false, 1)]
    public void StraightStepOntoGoal_SidestepsTowardThePuckSide(bool puckRight, int expectedY)
    {
        // (8,2) → target (10,2): straight step would be (9,2), the goal node.
        GridPoint next = SystemMovement.Step(new GridPoint(8, 2), new GridPoint(10, 2), puckRight ? RightPuck : LeftPuck, Rink);

        Assert.Equal(new GridPoint(9, expectedY), next);
    }

    [Theory]
    // data-schema.md, Maalin kierto examples (own goal (1, 2)).
    [InlineData(2, 2, 0, 2, 0, 3, 1, 3)] // puck below the goal row → +1 side
    [InlineData(2, 2, 0, 2, 0, 2, 1, 1)] // puck on the goal row, defender on it too → −1 (left)
    [InlineData(0, 1, 2, 3, 3, 4, 0, 2)] // replacement (1,3) too far → (0, s) to (0, 2)
    [InlineData(1, 1, 1, 3, 3, 4, 2, 2)] // sign dx = 0 → via the net front
    [InlineData(2, 3, 0, 2, 0, 2, 1, 3)] // puck on the goal row → defender's own side (+1)
    public void Step_CirclesTheGoal_PerD053(int x0, int y0, int tx, int ty, int px, int py, int ex, int ey)
    {
        GridPoint next = SystemMovement.Step(new GridPoint(x0, y0), new GridPoint(tx, ty), new GridPoint(px, py), Rink);

        Assert.Equal(new GridPoint(ex, ey), next);
    }

    [Fact]
    public void Advance_ContinuesTheCircleOnTheNextStep()
    {
        Assert.Equal(new GridPoint(1, 3), SystemMovement.Advance(new GridPoint(0, 1), new GridPoint(2, 3), new GridPoint(3, 4), 2, Rink));
        Assert.Equal(new GridPoint(1, 3), SystemMovement.Advance(new GridPoint(1, 1), new GridPoint(1, 3), new GridPoint(3, 4), 2, Rink));
    }

    [Fact]
    public void Advance_NeverEntersAGoalNode()
    {
        for (int x = 0; x < Rink.Length; x++)
        {
            for (int y = 0; y < Rink.Width; y++)
            {
                var from = new GridPoint(x, y);
                if (Rink.IsGoalNode(from))
                {
                    continue;
                }

                foreach (var target in new[] { new GridPoint(10, 2), new GridPoint(0, 2), new GridPoint(10, 0), new GridPoint(8, 2) })
                {
                    GridPoint current = from;
                    for (int i = 0; i < 4; i++)
                    {
                        current = SystemMovement.Step(current, target, LeftPuck, Rink);
                        Assert.False(Rink.IsGoalNode(current), from + " → " + target);
                    }
                }
            }
        }
    }
}

public class SystemMovementReachTests
{
    [Fact]
    public void Advance_ReachesEveryNonGoalTarget_FromEveryNonGoalNode()
    {
        var rink = TestRinks.Standard();
        var nodes = new List<GridPoint>();
        for (int x = 0; x < rink.Length; x++)
        {
            for (int y = 0; y < rink.Width; y++)
            {
                if (!rink.IsGoalNode(new GridPoint(x, y)))
                {
                    nodes.Add(new GridPoint(x, y));
                }
            }
        }

        foreach (GridPoint puck in new[] { new GridPoint(5, 1), new GridPoint(5, 2), new GridPoint(5, 3) })
        {
            foreach (GridPoint from in nodes)
            {
                foreach (GridPoint target in nodes)
                {
                    Assert.Equal(target, SystemMovement.Advance(from, target, puck, 30, rink));
                }
            }
        }
    }
}
