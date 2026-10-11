using System.Text;
using HockeyCoach.Harness.Batch;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Report;

/// <summary>D-078 K4 reporting: zone entries by strength and breakouts by forechecker count (M-9, Kalibrointi).</summary>
public class CounterplayTests
{
    private static BatchOptions MatrixOptions(int matches = 2) =>
        BatchOptions.Parse(new[] { "batch", "--tactic-matrix", "--matches", matches.ToString(System.Globalization.CultureInfo.InvariantCulture), "--seed", "5", "--threads", "2" }, out _)!;

    [Fact]
    public void Keys_HaveSlotRowAndSystem()
    {
        Assert.Equal("A carry 2v3 vs trap122", Counterplay.EntryKey("A", ZoneEntryMethod.Carry, new EntryNumbers(2, 3), "trap122"));
        Assert.Equal("B pass 3v2 vs -", Counterplay.EntryKey("B", ZoneEntryMethod.Pass, new EntryNumbers(3, 2), "-"));
        Assert.Equal("B f2 vs forecheck212", Counterplay.BreakoutKey("B", 2, "forecheck212"));
    }

    [Fact]
    public void Group_FiltersBySlotAndSystem_AndSumsTheRest()
    {
        var tally = new Tally();
        tally.Add("A carry 2v3 vs trap122", true);
        tally.Add("A carry 2v3 vs trap122", false);
        tally.Add("A carry 2v3 vs forecheck212", true);
        tally.Add("B carry 2v3 vs trap122", true);
        tally.Add("A pass 3v2 vs trap122", false);

        Assert.Equal(new Rate(2, 1), Counterplay.Group(tally, "A", "trap122")["carry 2v3"]);
        Assert.Equal(new Rate(3, 2), Counterplay.Group(tally, "A", null)["carry 2v3"]);
        Assert.Equal(new Rate(4, 3), Counterplay.Group(tally, null, null)["carry 2v3"]);
        Assert.Equal(new Rate(1, 0), Counterplay.Group(tally, null, null)["pass 3v2"]);
        Assert.Equal(new[] { "forecheck212", "trap122" }, Counterplay.Systems(tally));
    }

    [Fact]
    public void EntryRows_PutCarriesFirst_ThenTheBestStrength()
    {
        string[] rows = { "pass 2v2", "carry 2v5", "carry 2v2", "carry 3v2", "carry 1v1", "carry 2v3", "pass 3v2" };

        Assert.Equal(
            new[] { "carry 3v2", "carry 2v2", "carry 1v1", "carry 2v3", "carry 2v5", "pass 3v2", "pass 2v2" },
            Counterplay.OrderEntryRows(rows));
    }

    [Fact]
    public void WriteTable_ShowsSuccessAndAttempts_OrADashWithoutAttempts()
    {
        var md = new StringBuilder();
        var columns = new[]
        {
            new RateColumn("x", new Dictionary<string, Rate> { ["f1"] = new Rate(4, 3) }),
            new RateColumn("y", new Dictionary<string, Rate>()),
        };

        Counterplay.WriteTable(md, "f", new[] { "f1" }, columns);

        Assert.Contains("| f1 | 75.0 % (4) | - |", md.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MatrixStats_CountEveryEntryEventAndBreakoutCheck_AndTheReportHasTheSections()
    {
        BatchOptions options = MatrixOptions();
        IReadOnlyList<TacticCell> cells = TacticMatrix.Run(ShiftTestData.Data, options);
        TacticMatrixFiles files = TacticMatrix.Write(ShiftTestData.Data, options, cells, new[] { "Command: test" });

        foreach (TacticCell c in cells)
        {
            Assert.Equal(c.Stats.Teams.Sum(t => t.CarryEntries + t.PassEntries), c.Stats.Entries.N.Values.Sum());
            Assert.Equal(c.Stats.Teams.Sum(t => t.CarryEntriesKept + t.PassEntriesKept), c.Stats.Entries.Hits.Values.Sum());
            Assert.Equal(c.Stats.Checks.N["breakout"], c.Stats.Breakouts.N.Values.Sum());
            Assert.All(c.Stats.Entries.N.Keys, k => Assert.Matches("^[AB] (carry|pass) \\d+v\\d+ vs (forecheck212|trap122|-)$", k));
            Assert.All(c.Stats.Breakouts.N.Keys, k => Assert.Matches("^[AB] f[0-3] vs (forecheck212|trap122)$", k));

            // B always plays the cell's system, so A's breakouts are all against it.
            Assert.All(c.Stats.Breakouts.N.Keys.Where(k => k.StartsWith("A ", StringComparison.Ordinal)), k => Assert.EndsWith(" vs " + c.OpponentSystem, k, StringComparison.Ordinal));
        }

        Assert.Contains("## Team A zone entries by strength", files.Markdown, StringComparison.Ordinal);
        Assert.Contains("## Team A breakouts by forecheckers", files.Markdown, StringComparison.Ordinal);
        Assert.Contains("| regroup vs trap122 |", files.Markdown, StringComparison.Ordinal);
    }
}
