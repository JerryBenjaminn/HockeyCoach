using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Tactics;

public class FrameAndGeometryTests
{
    private static readonly Rink Rink = TestRinks.Standard();

    [Fact]
    public void HomeTeam_UsesItsOwnViewUnchanged()
    {
        var point = new GridPoint(8, 1);

        Assert.Equal(point, TeamFrame.ToRink(point, TeamSide.Home, Rink));
        Assert.Equal(point, TeamFrame.ToTeamView(point, TeamSide.Home, Rink));
    }

    [Fact]
    public void AwayTeam_IsRotated180Degrees()
    {
        // Away team's attacking net front (8, 2) is the home team's own net front (2, 2).
        Assert.Equal(new GridPoint(2, 2), TeamFrame.ToRink(new GridPoint(8, 2), TeamSide.Away, Rink));
        Assert.Equal(new GridPoint(2, 3), TeamFrame.ToRink(new GridPoint(8, 1), TeamSide.Away, Rink));
        Assert.Equal(Rink.OwnGoal, TeamFrame.ToTeamView(Rink.OpponentGoal, TeamSide.Away, Rink));
    }

    [Fact]
    public void Rotation_IsItsOwnInverse_ForEveryNode()
    {
        for (int x = 0; x < Rink.Length; x++)
        {
            for (int y = 0; y < Rink.Width; y++)
            {
                var point = new GridPoint(x, y);
                Assert.Equal(point, TeamFrame.ToTeamView(TeamFrame.ToRink(point, TeamSide.Away, Rink), TeamSide.Away, Rink));
                Assert.Equal(point, Rink.MirrorY(Rink.MirrorY(point)));
            }
        }
    }

    [Theory]
    [InlineData(0, Lane.Left)]
    [InlineData(1, Lane.Left)]
    [InlineData(2, Lane.Middle)]
    [InlineData(3, Lane.Right)]
    [InlineData(4, Lane.Right)]
    public void LaneOf_SplitsAtTheMiddleLane(int y, Lane expected)
    {
        Assert.Equal(expected, Rink.LaneOf(y));
    }

    [Fact]
    public void NetFront_IsOneStepFromTheGoalTowardTheCentreLine()
    {
        Assert.Equal(5, Rink.CentreLineX);
        Assert.Equal(new GridPoint(8, 2), Rink.NetFrontOf(Rink.OpponentGoal));
        Assert.Equal(new GridPoint(2, 2), Rink.NetFrontOf(Rink.OwnGoal));
        Assert.True(Rink.IsGoalNode(new GridPoint(9, 2)));
        Assert.False(Rink.IsGoalNode(new GridPoint(8, 2)));
    }

    [Theory]
    [InlineData(0, 0, 3, 1, 3, 4)]
    [InlineData(5, 2, 5, 2, 0, 0)]
    [InlineData(8, 1, 6, 3, 2, 4)]
    public void Distances_AreChebyshevAndManhattan(int ax, int ay, int bx, int by, int chebyshev, int manhattan)
    {
        Assert.Equal(chebyshev, GridPoint.Chebyshev(new GridPoint(ax, ay), new GridPoint(bx, by)));
        Assert.Equal(manhattan, GridPoint.Manhattan(new GridPoint(ax, ay), new GridPoint(bx, by)));
    }
}
