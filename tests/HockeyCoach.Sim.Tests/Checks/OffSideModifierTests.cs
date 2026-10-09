using HockeyCoach.Sim.Checks;

namespace HockeyCoach.Sim.Tests.Checks;

public class OffSideModifierTests
{
    [Fact]
    public void NoOffSidePlayers_AddsNothing()
    {
        Assert.Equal(0.0, OffSideModifier.Contribution(-0.3, false, false));
    }

    [Fact]
    public void OffSideAttacker_LowersM()
    {
        Assert.Equal(-0.3, OffSideModifier.Contribution(-0.3, true, false), 12);
    }

    [Fact]
    public void OffSideDefender_RaisesM()
    {
        Assert.Equal(0.3, OffSideModifier.Contribution(-0.3, false, true), 12);
    }

    [Fact]
    public void BothSidesOffSide_CancelOut()
    {
        Assert.Equal(0.0, OffSideModifier.Contribution(-0.3, true, true), 12);
    }
}
