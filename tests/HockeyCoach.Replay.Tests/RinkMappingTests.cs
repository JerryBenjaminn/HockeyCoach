using HockeyCoach.Replay.Core;
using HockeyCoach.Replay.Tests.Support;

namespace HockeyCoach.Replay.Tests;

public sealed class RinkMappingTests
{
    private const double Length = 60.96;
    private const double Width = 25.91;
    private const double GoalLine = 3.35;
    private const double BlueLine = 22.86;

    private static RinkMapping Mapping() => new(new SyntheticMatch().Root["rink"]!.ToObject<RinkData>()!);

    [Fact]
    public void GoalLineRows_LieOnTheGoalLines()
    {
        RinkMapping m = Mapping();
        Assert.Equal(GoalLine, m.RowX(1), 9);
        Assert.Equal(Length - GoalLine, m.RowX(9), 9);
        Assert.Equal(m.HomeGoalLineX, m.RowX(1), 9);
        Assert.Equal(m.AwayGoalLineX, m.RowX(9), 9);
    }

    [Fact]
    public void EndRows_LieHalfwayBetweenEndBoardsAndGoalLine()
    {
        RinkMapping m = Mapping();
        Assert.Equal(GoalLine / 2, m.RowX(0), 9);
        Assert.Equal(Length - (GoalLine / 2), m.RowX(10), 9);
    }

    [Fact]
    public void RowsBetweenGoalLines_AreEvenlySpaced_AndSymmetric()
    {
        RinkMapping m = Mapping();
        double spacing = (Length - (2 * GoalLine)) / 8;
        Assert.Equal(spacing, m.RowSpacing, 9);
        for (int x = 1; x < 9; x++)
        {
            Assert.Equal(spacing, m.RowX(x + 1) - m.RowX(x), 9);
        }

        for (int x = 0; x <= 10; x++)
        {
            Assert.Equal(Length, m.RowX(x) + m.RowX(10 - x), 9);
        }

        Assert.Equal(m.CentreLineX, m.RowX(5), 9);
    }

    [Fact]
    public void Lanes_AreEqualWidth_WithNodesAtLaneCentres()
    {
        RinkMapping m = Mapping();
        double lane = Width / 5;
        Assert.Equal(lane, m.LaneWidth, 9);
        for (int y = 0; y < 5; y++)
        {
            Assert.Equal((y + 0.5) * lane, m.LaneY(y), 9);
        }

        Assert.Equal(Width / 2, m.LaneY(2), 9);
    }

    [Fact]
    public void NodePosition_UsesIdEqualsXTimesWidthPlusY()
    {
        RinkMapping m = Mapping();
        Assert.Equal(m.GridPosition(9, 2), m.NodePosition(47));
        Assert.Equal(m.GridPosition(1, 2), m.NodePosition(7));
        Assert.Equal(m.GridPosition(0, 0), m.NodePosition(0));
        Assert.Equal(m.GridPosition(10, 4), m.NodePosition(54));
        Assert.Equal(47, m.NodeId(9, 2));
    }

    [Fact]
    public void Nets_AreOnTheGoalLines_HomeAttacksPlusX()
    {
        RinkMapping m = Mapping();
        Assert.Equal(new RinkPoint(GoalLine, Width / 2).X, m.HomeNet.X, 9);
        Assert.Equal(Width / 2, m.HomeNet.Y, 9);
        Assert.Equal(Length - GoalLine, m.AwayNet.X, 9);
        Assert.Equal(m.HomeNet, m.NetOf(ReplayTeam.Home));
        Assert.Equal(m.AwayNet, m.NetOf(ReplayTeam.Away));
        Assert.Equal(7, m.NetNodeOf(ReplayTeam.Home));
        Assert.Equal(47, m.NetNodeOf(ReplayTeam.Away));
        Assert.True(m.AwayNet.X > m.HomeNet.X);
    }

    [Fact]
    public void Lines_ComeFromTheHeaderMetres()
    {
        RinkMapping m = Mapping();
        Assert.Equal(BlueLine, m.HomeBlueLineX, 9);
        Assert.Equal(Length - BlueLine, m.AwayBlueLineX, 9);
        Assert.Equal(Length / 2, m.CentreLineX, 9);
        Assert.Equal(Length, m.LengthMetres);
        Assert.Equal(Width, m.WidthMetres);
    }

    [Fact]
    public void FaceoffSpots_AreMappedFromTheirNodes()
    {
        RinkMapping m = Mapping();
        Assert.Equal(3, m.FaceoffSpots.Count);
        RinkSpot centre = m.FaceoffSpotAt(27)!;
        Assert.Equal("center", centre.Id);
        Assert.Equal(new RinkPoint(m.CentreLineX, Width / 2).Y, centre.Position.Y, 9);
        Assert.Equal(m.CentreLineX, centre.Position.X, 9);
        Assert.Null(m.FaceoffSpotAt(28));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(55)]
    public void NodePosition_Throws_ForNodesOutsideTheGrid(int node)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Mapping().NodePosition(node));
    }

    [Fact]
    public void PuckPath_MovesAtConstantSpeed_ThroughTheViaPoint()
    {
        var a = new RinkPoint(0, 0);
        var via = new RinkPoint(3, 0);
        var end = new RinkPoint(3, 1);
        var path = new PuckPath(0, a, 1, via, 2, end, null);
        Assert.Equal(4, path.Length, 9);
        Assert.Equal(a, path.PositionAt(0));
        Assert.Equal(via, path.PositionAt(0.75));
        Assert.Equal(1.5, path.PositionAt(0.375).X, 9);
        Assert.Equal(0.5, path.PositionAt(0.875).Y, 9);
        Assert.Equal(end, path.PositionAt(1));
        Assert.Equal(end, path.PositionAt(2));
        Assert.Equal(a, path.PositionAt(-1));
    }
}
