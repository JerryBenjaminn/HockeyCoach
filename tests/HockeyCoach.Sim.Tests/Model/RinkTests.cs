using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Model;

public class RinkTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 4, 4)]
    [InlineData(1, 0, 5)]
    [InlineData(5, 2, 27)]
    [InlineData(9, 2, 47)]
    [InlineData(10, 4, 54)]
    public void IdOf_IsXTimesWidthPlusY_On11x5Grid(int x, int y, int expectedId)
    {
        Rink rink = TestPlayers.GridRink(11, 5);

        Assert.Equal(expectedId, rink.IdOf(x, y));
        Assert.Equal(x, rink.XOf(expectedId));
        Assert.Equal(y, rink.YOf(expectedId));
        Assert.Same(rink.GetNode(x, y), rink.GetNode(expectedId));
    }

    [Fact]
    public void IdOf_UsesWidthFromData_OnLargerGrid()
    {
        Rink rink = TestPlayers.GridRink(11, 7);

        Assert.Equal(77, rink.NodeCount);
        Assert.Equal(3 * 7 + 2, rink.IdOf(3, 2));
    }

    [Theory]
    [InlineData(0, 0, 10, 4)]
    [InlineData(5, 2, 5, 2)]
    [InlineData(1, 2, 9, 2)]
    [InlineData(8, 1, 2, 3)]
    public void Flip_Rotates180Degrees_On11x5Grid(int x, int y, int flippedX, int flippedY)
    {
        Rink rink = TestPlayers.GridRink(11, 5);

        int flipped = rink.Flip(rink.IdOf(x, y));

        Assert.Equal(flippedX, rink.XOf(flipped));
        Assert.Equal(flippedY, rink.YOf(flipped));
    }

    [Fact]
    public void Flip_TwiceReturnsOriginal_ForEveryNode()
    {
        Rink rink = TestPlayers.GridRink(11, 5);

        for (int id = 0; id < rink.NodeCount; id++)
        {
            Assert.Equal(id, rink.Flip(rink.Flip(id)));
        }
    }

    [Fact]
    public void Constructor_Throws_WhenNodeIsMissing()
    {
        var nodes = new[] { new RinkNode(0, 0, "none", false) };

        Assert.Throws<ArgumentException>(() => Build(1, 2, nodes));
    }

    [Fact]
    public void Constructor_Throws_WhenNodeIsDuplicated()
    {
        var nodes = new[]
        {
            new RinkNode(0, 0, "none", false),
            new RinkNode(0, 0, "none", false),
        };

        Assert.Throws<ArgumentException>(() => Build(1, 2, nodes));
    }

    [Fact]
    public void Constructor_Throws_WhenNodeIsOutsideGrid()
    {
        var nodes = new[]
        {
            new RinkNode(0, 0, "none", false),
            new RinkNode(0, 2, "none", false),
        };

        Assert.Throws<ArgumentException>(() => Build(1, 2, nodes));
    }

    private static Rink Build(int length, int width, IReadOnlyList<RinkNode> nodes)
    {
        return new Rink(
            length,
            width,
            nodes,
            new[] { new ZoneRange(RinkZone.Defensive, 0, length - 1) },
            new[] { "none" },
            new GridPoint(0, 0),
            new GridPoint(length - 1, 0),
            Array.Empty<FaceoffSpot>());
    }
}
