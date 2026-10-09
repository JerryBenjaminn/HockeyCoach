using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Checks;

public class ShotResolverTests
{
    [Fact]
    public void GoalProbability_EqualsBaseXg_WhenShooterEqualsGoalie()
    {
        double p = ShotResolver.GoalProbability(TestChecks.Formula(), TestChecks.Shot(), "slot", TestChecks.ShotParticipants(12, 12), 0.0);

        Assert.Equal(0.15, p, 12);
    }

    [Fact]
    public void GoalProbability_IsNotRaisedToCheckMinimum()
    {
        // The long-range base xG 0.005 is below the check clamp 0.02 but within the shot bounds (D-014).
        double p = ShotResolver.GoalProbability(TestChecks.Formula(), TestChecks.Shot(), "longRange", TestChecks.ShotParticipants(10, 10), 0.0);

        Assert.Equal(0.005, p, 12);
    }

    [Fact]
    public void GoalProbability_IsClampedToShotMinimum_NotCheckMinimum()
    {
        double p = ShotResolver.GoalProbability(TestChecks.Formula(), TestChecks.Shot(), "longRange", TestChecks.ShotParticipants(1, 20), -3.0);

        Assert.Equal(0.001, p);
    }

    [Fact]
    public void GoalProbability_IsClampedToShotMaximum_NotCheckMaximum()
    {
        double p = ShotResolver.GoalProbability(TestChecks.Formula(), TestChecks.Shot(), "slot", TestChecks.ShotParticipants(20, 1), 3.0);

        Assert.Equal(0.6, p);
    }

    [Fact]
    public void RegularCheck_StillUsesCheckClamp()
    {
        // Same extreme inputs through the regular check path: clamped to 0.02, not 0.001.
        CheckDefinition check = TestChecks.Shot().ForXgZone("longRange");

        double p = CheckResolver.Probability(TestChecks.Formula(), check, TestChecks.ShotParticipants(1, 20), -3.0);

        Assert.Equal(0.02, p);
    }

    [Fact]
    public void GoalProbability_UsesZoneSpecificShooterWeights()
    {
        var accurate = new CheckParticipants()
            .With("shooter", new HockeyCoach.Sim.Model.Skater(1, "S", HockeyCoach.Sim.Model.Position.Center, HockeyCoach.Sim.Model.SkaterStats.Uniform(10).With(HockeyCoach.Sim.Model.SkaterStat.ShotAccuracy, 18)))
            .With("goalie", TestPlayers.Goalie(2, 10));
        CheckResult slot = ShotResolver.Resolve(TestChecks.Formula(), TestChecks.Shot(), "slot", accurate, 0.0, new Pcg32(1UL));
        CheckResult longRange = ShotResolver.Resolve(TestChecks.Formula(), TestChecks.Shot(), "longRange", accurate, 0.0, new Pcg32(1UL));

        Assert.Equal(0.8 * 18 + 0.2 * 10, slot.AttackerRating, 12);
        Assert.Equal(0.3 * 18 + 0.7 * 10, longRange.AttackerRating, 12);
        Assert.Equal("shot", slot.CheckId);
    }

    [Fact]
    public void ForXgZone_Throws_ForUnknownZone()
    {
        Assert.Throws<KeyNotFoundException>(() => TestChecks.Shot().ForXgZone("crease"));
    }
}
