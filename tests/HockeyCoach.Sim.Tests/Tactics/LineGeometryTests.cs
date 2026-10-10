using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

public class LineGeometryTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    private static GridPoint P(int x, int y) => new(x, y);

    private static HashSet<GridPoint> Set(params GridPoint[] points) => new(points);

    [Fact]
    public void Nodes_MatchTheDocumentedExamples()
    {
        Assert.Equal(Set(P(8, 1), P(8, 2), P(8, 3)), new HashSet<GridPoint>(LineGeometry.Nodes(P(8, 1), P(8, 3))));
        Assert.Equal(Set(P(7, 1), P(8, 1), P(8, 2), P(9, 2)), new HashSet<GridPoint>(LineGeometry.Nodes(P(7, 1), P(9, 2))));
        Assert.Equal(Set(P(10, 1), P(9, 2), P(8, 3)), new HashSet<GridPoint>(LineGeometry.Nodes(P(10, 1), P(8, 3))));
        Assert.Equal(new[] { P(4, 4) }, LineGeometry.Nodes(P(4, 4), P(4, 4)));
    }

    [Fact]
    public void Nodes_AreTheSameInBothDirections_MirroredAndRotated()
    {
        for (int ax = 0; ax < Rink.Length; ax++)
        {
            for (int ay = 0; ay < Rink.Width; ay++)
            {
                foreach (GridPoint b in new[] { P(0, 0), P(10, 4), P(6, 1), P(3, 3), P(9, 0) })
                {
                    GridPoint a = P(ax, ay);
                    var forward = new HashSet<GridPoint>(LineGeometry.Nodes(a, b));
                    Assert.Equal(forward, new HashSet<GridPoint>(LineGeometry.Nodes(b, a)));
                    Assert.Equal(forward.Select(Rink.MirrorY).ToHashSet(), new HashSet<GridPoint>(LineGeometry.Nodes(Rink.MirrorY(a), Rink.MirrorY(b))));
                    Assert.Equal(forward.Select(Rink.Rotate).ToHashSet(), new HashSet<GridPoint>(LineGeometry.Nodes(Rink.Rotate(a), Rink.Rotate(b))));
                }
            }
        }
    }

    [Fact]
    public void LineDefender_IsClosestToTheLine_ThenToTheReference()
    {
        IReadOnlyList<GridPoint> lane = LineGeometry.Nodes(P(5, 0), P(5, 4));
        var defenders = new Dictionary<Position, GridPoint>
        {
            [Position.Center] = P(7, 2),
            [Position.LeftWing] = P(6, 3),   // 1 from the lane, Chebyshev 1 to the receiver (5,4)
            [Position.RightWing] = P(6, 1),  // 1 from the lane, Chebyshev 3 to the receiver
            [Position.LeftDefence] = P(2, 0),
            [Position.RightDefence] = P(2, 4),
        };

        Assert.True(LineGeometry.LineDefender(defenders, lane, P(5, 4), out Position defender, out int distance));
        Assert.Equal(Position.LeftWing, defender);
        Assert.Equal(1, distance);
    }

    [Fact]
    public void Closest_HonoursTheMaximumDistance()
    {
        var defenders = new Dictionary<Position, GridPoint> { [Position.Center] = P(5, 4) };

        Assert.False(LineGeometry.Closest(defenders, new[] { P(8, 0) }, P(8, 0), 1, out _, out _));
    }

    [Theory]
    [InlineData(1, 3, true)]
    [InlineData(0, 4, true)]
    [InlineData(2, 3, false)] // from the middle lane
    [InlineData(1, 0, false)]
    public void CrossIce_NeedsOppositeSidesOfTheMiddleLane(int y1, int y2, bool expected)
    {
        Assert.Equal(expected, LineGeometry.IsCrossIce(P(8, y1), P(8, y2), Rink));
    }
}
