using HockeyCoach.Harness.Batch;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Report;

/// <summary>The batch command: options, thread-count-invariant output, skill-vs-luck math and report sections.</summary>
public class BatchTests
{
    private static BatchOptions Options(int threads, string matchups = "rulebased-random,random-random", int matches = 4) =>
        new(matches, BatchOptions.ParseMatchups(matchups, out _)!, 11UL, threads, "unused", "forecheck212", false);

    private static BatchReportFiles Report(BatchOptions options) =>
        BatchReport.Write(ShiftTestData.Data, options, BatchRunner.Run(ShiftTestData.Data, options), new[] { "Command: test" });

    [Fact]
    public void Output_DoesNotDependOnTheThreadCount()
    {
        BatchReportFiles one = Report(Options(1));
        BatchReportFiles four = Report(Options(4));

        Assert.Equal(one.Markdown, four.Markdown);
        Assert.Equal(one.SummaryCsv, four.SummaryCsv);
        Assert.Equal(one.MatchesCsv, four.MatchesCsv);
        Assert.Equal(one.DetailsCsv, four.DetailsCsv);
    }

    [Fact]
    public void AllMatchups_AreEveryPairOfTheSixAis_MirrorsIncluded()
    {
        IReadOnlyList<Matchup> all = BatchOptions.AllMatchups();

        Assert.Equal(21, all.Count);
        Assert.Equal(new Matchup("rulebased", "rulebased"), all[0]);
        Assert.Equal(new Matchup("rulebased", "random"), all[2]);
        Assert.Equal(6, all.Count(m => m.A == m.B));
        Assert.Equal(all.Count, all.Select(m => string.Join("/", new[] { m.A, m.B }.OrderBy(x => x, StringComparer.Ordinal))).Distinct().Count());
    }

    [Fact]
    public void Parse_ReadsOptions_AndRejectsOddMatchCountsAndUnknownAis()
    {
        BatchOptions? o = BatchOptions.Parse(new[] { "batch", "--matches", "10", "--matchups", "rulebased-random", "--seed", "5", "--threads", "2", "--mirror" }, out string? error);
        Assert.Null(error);
        Assert.Equal(10, o!.Matches);
        Assert.Equal(new[] { new Matchup("rulebased", "random") }, o.Matchups);
        Assert.Equal(5UL, o.Seed);
        Assert.Equal(2, o.Threads);
        Assert.True(o.MirrorRosters);

        Assert.Null(BatchOptions.Parse(new[] { "batch", "--matches", "3" }, out error));
        Assert.Contains("even", error);
        Assert.Null(BatchOptions.Parse(new[] { "batch", "--matchups", "genius-random" }, out error));
        Assert.Contains("Bad matchup", error);
    }

    [Fact]
    public void SkillLuck_ComputesRecordCiVarianceSplitAndXgWinner()
    {
        var lines = new List<MatchLine>
        {
            new(0, 1, true, 3, 1, 2.0, 1.0, 30, 20, 0, 0, false, 100),
            new(0, 1, false, 2, 2, 1.5, 1.5, 25, 25, 0, 0, false, 100),
            new(1, 2, true, 0, 1, 2.5, 1.0, 35, 15, 0, 0, false, 100),
            new(1, 2, false, 4, 0, 3.0, 0.5, 40, 10, 0, 0, false, 100),
        };

        SkillLuck s = SkillLuck.From(lines);

        Assert.Equal((2, 1, 1), (s.Wins, s.Ties, s.Losses));
        Assert.Equal(0.625, s.PointsShare, 12);

        // Pair means 0.75 and 0.5: sd = 0.1767767, se = 0.125.
        Assert.Equal(0.625 - (SkillLuck.Z95 * 0.125), s.CiLow, 9);
        Assert.Equal(0.625 + (SkillLuck.Z95 * 0.125), s.CiHigh, 9);
        Assert.Equal(0.25, s.UpsetRate, 12);
        Assert.Equal(0.0, s.SweepShare, 12);
        Assert.Equal(0.5, s.SplitShare, 12);

        double[] gd = { 2, 0, -1, 4 };
        double[] xgd = { 1, 0, 1.5, 2.5 };
        double[] luck = gd.Zip(xgd, (g, x) => g - x).ToArray();
        Assert.Equal(SkillLuck.Variance(gd), s.VarGoalDiff, 12);
        Assert.Equal(1.0, s.ChanceShare + s.FinishingShare + s.CovarianceShare, 9);
        Assert.Equal(SkillLuck.Variance(xgd) / SkillLuck.Variance(gd), s.ChanceShare, 12);
        Assert.Equal(SkillLuck.Variance(luck) / SkillLuck.Variance(gd), s.FinishingShare, 12);

        // xG decided in 3 matches: the xG winner won 2 (matches 1 and 4), lost 1.
        Assert.Equal(3, s.XgDecided);
        Assert.Equal(2.0 / 3.0, s.XgWinnerWins, 12);
        Assert.Equal(1.0 / 4.0, s.HomeWinShare, 12);
        Assert.Equal(2.0 / 4.0, s.AwayWinShare, 12);
    }

    [Fact]
    public void Report_HasEverySection_AndShiftTimesMatchIceTime()
    {
        BatchOptions options = Options(2, "rulebased-random,rulebased-rulebased,random-random", 2);
        IReadOnlyList<MatchupStats> stats = BatchRunner.Run(ShiftTestData.Data, options);
        BatchReportFiles files = BatchReport.Write(ShiftTestData.Data, options, stats, new[] { "Command: test" });

        foreach (string section in new[]
        {
            "## Skill vs luck", "**Primary (D-077 item 4): rulebased vs random:**", "## Points-share matrix", "## Ablation", "## Targets on rulebased vs rulebased",
            "### Fatigue", "### Play × system", "### Rush vs regroup", "### Checks", "### Pressure shares", "### xG by chance type and class", "### Goal variance",
            "smartVsRandomWinRate", "xG diff / 60 min",
        })
        {
            Assert.Contains(section, files.Markdown, StringComparison.Ordinal);
        }

        Assert.Equal(1 + 3, files.SummaryCsv.Trim().Split('\n').Length);
        Assert.Equal(1 + 6, files.MatchesCsv.Trim().Split('\n').Length);

        // Shift seconds from the public log add up to the snapshot ice time (per unit type and team).
        foreach (MatchupStats s in stats)
        {
            foreach (string slot in new[] { "A", "B" })
            {
                double trioShifts = s.Shifts.Sum[slot + " trio"];
                double trioIce = s.IceTime.Sum.Where(e => e.Key.StartsWith(slot + " F", StringComparison.Ordinal)).Sum(e => e.Value);
                Assert.Equal(trioIce, trioShifts, 6);
            }

            Assert.All(s.Transitions.N.Keys, k => Assert.Matches("^(rush|regroup) vs (forecheck212|trap122)$", k));
            Assert.Equal(s.Lines.Count * 3 * 1200.0 * 2, s.TransitionSeconds.Sum.Values.Sum(), 6);
        }
    }
}
