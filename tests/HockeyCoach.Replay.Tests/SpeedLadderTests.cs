using HockeyCoach.Replay.Core;

namespace HockeyCoach.Replay.Tests;

public sealed class SpeedLadderTests
{
    private static SpeedLadder Ladder(int index = 0) => new(new double[] { 1, 2, 4, 8, 16 }, index);

    [Fact]
    public void Current_IsTheInitialSpeed()
    {
        Assert.Equal(4, Ladder(2).Current);
    }

    [Fact]
    public void Faster_StepsUp_AndStopsAtTheTop()
    {
        SpeedLadder ladder = Ladder(3);
        Assert.Equal(16, ladder.Faster());
        Assert.Equal(16, ladder.Faster());
        Assert.Equal(4, ladder.Index);
    }

    [Fact]
    public void Slower_StepsDown_AndStopsAtTheBottom()
    {
        SpeedLadder ladder = Ladder(1);
        Assert.Equal(1, ladder.Slower());
        Assert.Equal(1, ladder.Slower());
        Assert.Equal(0, ladder.Index);
    }

    [Theory]
    [InlineData(new double[0], 0)]
    [InlineData(new double[] { 1, 0.5 }, 0)]
    [InlineData(new double[] { 1, 1 }, 0)]
    [InlineData(new double[] { 0, 1 }, 0)]
    [InlineData(new double[] { 1, 2 }, 2)]
    [InlineData(new double[] { 1, 2 }, -1)]
    public void Constructor_Throws_ForInvalidLadder(double[] speeds, int index)
    {
        Assert.Throws<ArgumentException>(() => new SpeedLadder(speeds, index));
    }
}
