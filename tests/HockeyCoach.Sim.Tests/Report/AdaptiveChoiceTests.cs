using System.Text;
using HockeyCoach.Harness.Batch;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Report;

/// <summary>A-T2 item 8: switches, countered time and classification hit rate in the batch report.</summary>
public class AdaptiveChoiceTests
{
    [Fact]
    public void Effective_DropsTheDecisionReplacedByTheAfterTimeoutRound()
    {
        var plans = new List<PlanRecord>
        {
            new(1, 0.0, TransitionInstruction.Rush, "forecheck212"),
            new(1, 50.0, TransitionInstruction.Rush, "forecheck212"),
            new(1, 50.0, TransitionInstruction.Regroup, "trap122"),
            new(2, 0.0, TransitionInstruction.Rush, "forecheck212"),
        };

        IReadOnlyList<PlanRecord> effective = AdaptiveChoice.Effective(plans);

        Assert.Equal(new[] { plans[0], plans[2], plans[3] }, effective);
        Assert.Equal(plans[2], AdaptiveChoice.At(effective, 1, 60.0));
        Assert.Null(AdaptiveChoice.At(effective, 3, 60.0));
    }

    [Fact]
    public void AdaptiveVsRulebased_MeasuresEverySecondAndClassifiesMostTransitionsCorrectly()
    {
        var data = ShiftTestData.Data;
        var stats = new MatchupStats(new Matchup(CoachRegistry.Adaptive, CoachRegistry.RuleBased), data.Tuning.Time.Periods, data.Tuning.Time.PeriodSeconds, data.Tuning.Ai.Rulebased.TransitionBySystem);
        foreach (BatchGame game in BatchMatch.PlayPair(data, stats.Matchup, "forecheck212", true, 0, 42))
        {
            Assert.False(game.Game.Result.Stalled);
            stats.Add(game, data.Rink);
        }

        AdaptiveChoice c = stats.Adaptive;
        double matchSeconds = data.Tuning.Time.Periods * data.Tuning.Time.PeriodSeconds;
        foreach (string slot in new[] { "A", "B" })
        {
            Assert.Equal(2, c.Switches.N[slot]);
            double bySystem = c.SystemSeconds.Sum.Where(e => e.Key.StartsWith(slot + " ", StringComparison.Ordinal)).Sum(e => e.Value);
            Assert.Equal(2 * matchSeconds, bySystem, 6);
            Assert.Equal(2 * matchSeconds, c.Countered.Sum2[slot], 6);
            Assert.InRange(c.Countered.Sum[slot], 0.0, c.Countered.Sum2[slot]);
        }

        int n = c.Classification.N.Where(e => e.Key.StartsWith("A ", StringComparison.Ordinal)).Sum(e => e.Value);
        int hits = c.Classification.Hits.Where(e => e.Key.StartsWith("A ", StringComparison.Ordinal)).Sum(e => e.Value);
        Assert.True(n > 20, "observations " + n);
        Assert.True(hits > n / 2, hits + " / " + n);

        var md = new StringBuilder();
        AdaptiveChoice.Write(md, new[] { stats });
        string text = md.ToString();
        Assert.Contains("## Adaptive system choice", text, StringComparison.Ordinal);
        Assert.Contains("| adaptive vs rulebased | A |", text, StringComparison.Ordinal);
        Assert.DoesNotContain("| B |", text, StringComparison.Ordinal);
    }
}
