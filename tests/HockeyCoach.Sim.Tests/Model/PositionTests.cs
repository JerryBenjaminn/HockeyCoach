using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tests.Model;

public class PositionTests
{
    [Theory]
    [InlineData("C", Position.Center)]
    [InlineData("LW", Position.LeftWing)]
    [InlineData("RW", Position.RightWing)]
    [InlineData("LD", Position.LeftDefence)]
    [InlineData("RD", Position.RightDefence)]
    public void Positions_RoundTripDataNames(string name, Position position)
    {
        Assert.True(Positions.TryParse(name, out Position parsed));
        Assert.Equal(position, parsed);
        Assert.Equal(name, Positions.ToName(position));
    }

    [Theory]
    [InlineData("c")]
    [InlineData("D")]
    [InlineData("")]
    public void Positions_RejectUnknownNames(string name)
    {
        Assert.False(Positions.TryParse(name, out _));
    }

    [Theory]
    [InlineData(Position.LeftWing, Position.RightWing, true)]
    [InlineData(Position.RightWing, Position.LeftWing, true)]
    [InlineData(Position.LeftDefence, Position.RightDefence, true)]
    [InlineData(Position.RightDefence, Position.LeftDefence, true)]
    [InlineData(Position.LeftWing, Position.LeftWing, false)]
    [InlineData(Position.Center, Position.LeftWing, false)]
    [InlineData(Position.LeftWing, Position.Center, false)]
    [InlineData(Position.LeftWing, Position.LeftDefence, false)]
    public void IsOffSide_OnlyForOppositeSideOfSameUnit(Position primary, Position played, bool expected)
    {
        Assert.Equal(expected, Positions.IsOffSide(primary, played));
    }
}
