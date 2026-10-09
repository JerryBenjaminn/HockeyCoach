using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Checks;

public class CheckFormulaTests
{
    [Theory]
    [InlineData(0.5)]
    [InlineData(0.85)]
    [InlineData(0.15)]
    public void Probability_EqualsBaseRate_WhenRatingsAreEqualAndNoModifiers(double p0)
    {
        double p = CheckFormula.Probability(TestChecks.Formula(), p0, 12.0, 12.0, 0.0);

        Assert.Equal(p0, p, 12);
    }

    [Fact]
    public void Probability_MatchesFormula_ForKnownInputs()
    {
        // logit(0.85) + 0.15 * (14 - 10) + 0.3 = 1.734601055 + 0.6 + 0.3
        double expected = 1.0 / (1.0 + Math.Exp(-(Math.Log(0.85 / 0.15) + 0.9)));

        double p = CheckFormula.RawProbability(0.85, 0.15, 14.0, 10.0, 0.3);

        Assert.Equal(expected, p, 12);
    }

    [Fact]
    public void Probability_RisesWithAttackerRating()
    {
        var formula = TestChecks.Formula();
        double previous = 0.0;
        for (int h = 1; h <= 20; h++)
        {
            double p = CheckFormula.RawProbability(0.5, formula.K, h, 10.0, 0.0);
            Assert.True(p > previous);
            previous = p;
        }
    }

    [Fact]
    public void Probability_FallsWithDefenderRating()
    {
        double previous = 1.0;
        for (int d = 1; d <= 20; d++)
        {
            double p = CheckFormula.RawProbability(0.5, 0.15, 10.0, d, 0.0);
            Assert.True(p < previous);
            previous = p;
        }
    }

    [Fact]
    public void PositiveModifier_RaisesProbability_AndNegativeModifierLowersIt()
    {
        double neutral = CheckFormula.RawProbability(0.85, 0.15, 10.0, 10.0, 0.0);
        double up = CheckFormula.RawProbability(0.85, 0.15, 10.0, 10.0, 0.5);
        double down = CheckFormula.RawProbability(0.85, 0.15, 10.0, 10.0, -0.9);

        Assert.True(up > neutral);
        Assert.True(down < neutral);
    }

    [Fact]
    public void ZeroSensitivity_MakesStatsIrrelevant()
    {
        double p = CheckFormula.RawProbability(0.6, 0.0, 20.0, 1.0, 0.0);

        Assert.Equal(0.6, p, 12);
    }

    [Fact]
    public void Probability_IsClampedToMaximum_WhenAttackerIsFarBetter()
    {
        double p = CheckFormula.Probability(TestChecks.Formula(), 0.85, 20.0, 1.0, 2.0);

        Assert.Equal(0.98, p);
    }

    [Fact]
    public void Probability_IsClampedToMinimum_WhenDefenderIsFarBetter()
    {
        double p = CheckFormula.Probability(TestChecks.Formula(), 0.15, 1.0, 20.0, -2.0);

        Assert.Equal(0.02, p);
    }

    [Fact]
    public void Probability_UsesClampBoundsFromConfig()
    {
        double p = CheckFormula.Probability(TestChecks.Formula(min: 0.1, max: 0.6), 0.85, 10.0, 10.0, 0.0);

        Assert.Equal(0.6, p);
    }
}
