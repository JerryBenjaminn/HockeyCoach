using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Checks;

public class OneSidedCheckTests
{
    private static CheckParticipants Blocker(int positioning)
    {
        return new CheckParticipants().With("nearestDefender", TestPlayers.Skater(5, positioning));
    }

    [Fact]
    public void Block_SucceedsAtBaseRate_WhenBlockerEqualsReferenceValue()
    {
        var formula = TestChecks.Formula(referenceValue: 12.0);

        double p = CheckResolver.Probability(formula, TestChecks.Block(), Blocker(12), 0.0);

        Assert.Equal(0.75, p, 12);
    }

    [Fact]
    public void Block_UsesReferenceValueAsAttackerRating_WhenDefenderSideIsPresent()
    {
        var formula = TestChecks.Formula();

        CheckResolver.Ratings(formula, TestChecks.Block(), Blocker(14), out double h, out double d);

        Assert.Equal(10.5, h);
        Assert.Equal(14.0, d);
    }

    [Fact]
    public void Block_BetterBlocker_LowersAttackerSuccess()
    {
        var formula = TestChecks.Formula();

        double weak = CheckResolver.Probability(formula, TestChecks.Block(), Blocker(5), 0.0);
        double strong = CheckResolver.Probability(formula, TestChecks.Block(), Blocker(18), 0.0);

        Assert.True(strong < weak);
    }

    [Fact]
    public void OneSidedAttackerCheck_UsesReferenceValueAsDefenderRating()
    {
        var check = CheckDefinition.OneSided("x", 0.5, CheckSide.Attacker, new[] { new WeightTerm("carrier", StatRef.Of(SkaterStat.Speed), 1.0) });
        var participants = new CheckParticipants().With("carrier", TestPlayers.Skater(1, 16));

        CheckResolver.Ratings(TestChecks.Formula(), check, participants, out double h, out double d);

        Assert.Equal(16.0, h);
        Assert.Equal(10.5, d);
        Assert.True(CheckResolver.Probability(TestChecks.Formula(), check, participants, 0.0) > 0.5);
    }

    [Fact]
    public void OneSidedCheck_MatchesFormula_ForKnownInputs()
    {
        double expected = 1.0 / (1.0 + Math.Exp(-(Math.Log(0.75 / 0.25) + 0.15 * (10.5 - 16.0))));

        double p = CheckResolver.Probability(TestChecks.Formula(), TestChecks.Block(), Blocker(16), 0.0);

        Assert.Equal(expected, p, 12);
    }

    [Fact]
    public void NoCheck_CannotBeResolved()
    {
        var dumpIn = new CheckDefinition("dumpIn", CheckKind.NoCheck, null, null, Array.Empty<WeightTerm>(), Array.Empty<WeightTerm>());

        Assert.Throws<InvalidOperationException>(() =>
            CheckResolver.Resolve(TestChecks.Formula(), dumpIn, new CheckParticipants(), 0.0, new Pcg32(1UL)));
    }
}
