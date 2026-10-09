using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Checks;

public class CheckResolverTests
{
    [Fact]
    public void Pass_SucceedsAtBaseRate_WhenRatingsAreEqual()
    {
        double p = CheckResolver.Probability(TestChecks.Formula(), TestChecks.Pass(), TestChecks.PassParticipants(12, 12, 12), 0.0);

        Assert.Equal(0.85, p, 12);
    }

    [Fact]
    public void StatWeighting_UsesEachParticipantsOwnStats()
    {
        var passer = new Skater(1, "P", Position.Center, SkaterStats.Uniform(1).With(SkaterStat.Passing, 20));
        var receiver = new Skater(2, "R", Position.RightWing, SkaterStats.Uniform(1).With(SkaterStat.Hands, 10));
        var participants = new CheckParticipants().With("passer", passer).With("receiver", receiver);

        double h = StatWeighting.Rating(TestChecks.Pass().Attacker, participants);

        Assert.Equal(0.6 * 20 + 0.4 * 10, h, 12);
    }

    [Fact]
    public void StatWeighting_ReadsGoalieStats_ForGoalieParticipant()
    {
        var goalie = new Goalie(9, "G", new GoalieStats(reflexes: 15, positioning: 10, mobility: 1, reboundControl: 1, puckHandling: 1, mentalToughness: 1));
        var participants = new CheckParticipants().With("goalie", goalie);

        double d = StatWeighting.Rating(TestChecks.Shot().Defender, participants);

        Assert.Equal(0.6 * 10 + 0.4 * 15, d, 12);
    }

    [Fact]
    public void Pass_IsMoreLikely_WithBetterPasser()
    {
        var formula = TestChecks.Formula();
        double weak = CheckResolver.Probability(formula, TestChecks.Pass(), TestChecks.PassParticipants(8, 10, 10), 0.0);
        double strong = CheckResolver.Probability(formula, TestChecks.Pass(), TestChecks.PassParticipants(16, 10, 10), 0.0);

        Assert.True(strong > weak);
    }

    [Fact]
    public void Pass_IsLessLikely_WithBetterDefender()
    {
        var formula = TestChecks.Formula();
        double weakDefender = CheckResolver.Probability(formula, TestChecks.Pass(), TestChecks.PassParticipants(10, 10, 6), 0.0);
        double strongDefender = CheckResolver.Probability(formula, TestChecks.Pass(), TestChecks.PassParticipants(10, 10, 18), 0.0);

        Assert.True(strongDefender < weakDefender);
    }

    [Fact]
    public void Resolve_ReportsInputsAndClampedProbability()
    {
        CheckResult result = CheckResolver.Resolve(TestChecks.Formula(), TestChecks.Pass(), TestChecks.PassParticipants(20, 20, 1), 1.0, new Pcg32(1UL));

        Assert.Equal("pass", result.CheckId);
        Assert.Equal(20.0, result.AttackerRating, 12);
        Assert.Equal(1.0, result.DefenderRating, 12);
        Assert.Equal(1.0, result.Modifier);
        Assert.Equal(0.98, result.Probability);
    }

    [Fact]
    public void Resolve_ConsumesExactlyOneChanceDraw()
    {
        var a = new Pcg32(77UL);
        var b = new Pcg32(77UL);

        CheckResolver.Resolve(TestChecks.Formula(), TestChecks.Pass(), TestChecks.PassParticipants(10, 10, 10), 0.0, a);
        b.Chance(0.5);

        Assert.Equal(b.NextUInt(), a.NextUInt());
    }

    [Fact]
    public void Resolve_SuccessRateConvergesToProbability()
    {
        var rng = new Pcg32(2024UL);
        var formula = TestChecks.Formula();
        CheckDefinition pass = TestChecks.Pass();
        CheckParticipants participants = TestChecks.PassParticipants(10, 10, 10);
        const int n = 50_000;

        int successes = 0;
        for (int i = 0; i < n; i++)
        {
            if (CheckResolver.Resolve(formula, pass, participants, 0.0, rng).Success)
            {
                successes++;
            }
        }

        // 0.85 ± ~5 standard deviations (sd = 0.0016).
        Assert.InRange(successes / (double)n, 0.842, 0.858);
    }

    [Fact]
    public void Resolve_Throws_WhenParticipantIsMissing()
    {
        var participants = new CheckParticipants().With("passer", TestPlayers.Skater(1));

        Assert.Throws<ArgumentException>(() =>
            CheckResolver.Resolve(TestChecks.Formula(), TestChecks.Pass(), participants, 0.0, new Pcg32(1UL)));
    }
}
