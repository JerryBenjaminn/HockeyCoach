using HockeyCoach.AI;
using HockeyCoach.Harness.Batch;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Report;

/// <summary>D-078 K3: <c>--rotation-transition</c>, the tactic matrix and the rush-vs-regroup split by opponent system.</summary>
public class TacticMatrixTests
{
    private static BatchOptions MatrixOptions(int threads, int matches = 2) =>
        BatchOptions.Parse(new[] { "batch", "--tactic-matrix", "--matches", matches.ToString(System.Globalization.CultureInfo.InvariantCulture), "--seed", "7", "--threads", threads.ToString(System.Globalization.CultureInfo.InvariantCulture) }, out _)!;

    [Fact]
    public void Parse_ReadsRotationTransition_AndRejectsUnknownValues()
    {
        BatchOptions? o = BatchOptions.Parse(new[] { "batch", "--rotation-transition", "regroup" }, out string? error);
        Assert.Null(error);
        Assert.Equal(TransitionInstruction.Regroup, o!.RotationTransition);
        Assert.False(o.TacticMatrix);
        Assert.Equal(BatchOptions.DefaultMatches, o.Matches);

        Assert.Equal(TransitionInstruction.Rush, BatchOptions.Parse(new[] { "batch" }, out _)!.RotationTransition);
        Assert.Null(BatchOptions.Parse(new[] { "batch", "--rotation-transition", "dump" }, out error));
        Assert.Contains("--rotation-transition", error);
    }

    [Fact]
    public void Parse_TacticMatrix_DefaultsTo2000MatchesPerCell_AndMirrorsRosters()
    {
        BatchOptions o = BatchOptions.Parse(new[] { "batch", "--tactic-matrix" }, out string? error)!;

        Assert.Null(error);
        Assert.True(o.TacticMatrix);
        Assert.True(o.MirrorRosters);
        Assert.Equal(BatchOptions.DefaultMatrixMatches, o.Matches);
        Assert.Equal(2000, o.Matches);
        Assert.Equal(10, BatchOptions.Parse(new[] { "batch", "--tactic-matrix", "--matches", "10" }, out _)!.Matches);
    }

    [Theory]
    [InlineData(TransitionInstruction.Rush)]
    [InlineData(TransitionInstruction.Regroup)]
    public void RotationCoach_PlaysTheFixedTransition_AtEveryStoppage(TransitionInstruction transition)
    {
        var data = ShiftTestData.Data;
        var home = new RecordingCoach(CoachRegistry.Factory(CoachRegistry.Rotation, data, "forecheck212", transition)(new Pcg32(1UL, 1UL)));
        var away = new RecordingCoach(new RotationCoach(data.Plays, HarnessTeams.System(data, "trap122")));
        (Sim.Model.Team a, Sim.Model.Team b) = HarnessTeams.PairRosters(3UL, true);
        var setup = new MatchSetup(data.Rink, data.Tuning, a, b, MatchCommand.DefaultMaxStepsPerSegment, MatchCommand.DefaultMaxSegments);
        var streams = new MatchStreams(3UL);

        MatchSimulator.Run(setup, home, away, streams.Simulation());

        Assert.NotEmpty(home.Plans);
        Assert.All(home.Plans, p => Assert.Equal(transition, p.Transition));
        Assert.All(away.Plans, p => Assert.Equal(TransitionInstruction.Rush, p.Transition));
    }

    [Fact]
    public void Matrix_HasEveryTransitionAgainstEverySystem_WithFixedTransitionsAndMirroredRosters()
    {
        BatchOptions options = MatrixOptions(2);
        IReadOnlyList<TacticCell> cells = TacticMatrix.Run(ShiftTestData.Data, options);

        Assert.Equal(
            new[] { "rush/forecheck212", "rush/trap122", "regroup/forecheck212", "regroup/trap122" },
            cells.Select(c => CoachRegistry.TransitionName(c.Transition) + "/" + c.OpponentSystem));
        foreach (TacticCell c in cells)
        {
            Assert.Equal(2, c.Stats.Lines.Count);

            // Team A plays its row's transition against the column's system; team B plays rush against A's forecheck212.
            var expected = new SortedSet<string>(StringComparer.Ordinal)
            {
                CoachRegistry.TransitionName(c.Transition) + " vs " + c.OpponentSystem,
                "rush vs forecheck212",
            };
            Assert.Equal(expected, c.Stats.Transitions.N.Keys.ToHashSet());
            Assert.Equal("rotation/forecheck212/" + CoachRegistry.TransitionName(c.Transition), c.Stats.Matchup.A);
            Assert.Equal("rotation/" + c.OpponentSystem + "/rush", c.Stats.Matchup.B);
        }
    }

    [Fact]
    public void Matrix_OutputDoesNotDependOnTheThreadCount()
    {
        TacticMatrixFiles one = TacticMatrix.Write(ShiftTestData.Data, MatrixOptions(1, 4), TacticMatrix.Run(ShiftTestData.Data, MatrixOptions(1, 4)), new[] { "Command: test" });
        TacticMatrixFiles three = TacticMatrix.Write(ShiftTestData.Data, MatrixOptions(3, 4), TacticMatrix.Run(ShiftTestData.Data, MatrixOptions(3, 4)), new[] { "Command: test" });

        Assert.Equal(one.Markdown, three.Markdown);
        Assert.Equal(one.Csv, three.Csv);
        foreach (string section in new[] { "## Matrix", "## Cells", "## Rush minus regroup per opponent system", "B plays forecheck212", "B plays trap122" })
        {
            Assert.Contains(section, one.Markdown, StringComparison.Ordinal);
        }

        Assert.Equal(1 + 4, one.Csv.Trim().Split('\n').Length);
    }

    [Fact]
    public void XgPer60_ScalesTeamAXgByRegulationLength()
    {
        var stats = new MatchupStats(new Matchup("a", "b"), 3, 1200.0);
        stats.Lines.Add(new MatchLine(0, 1, true, 1, 0, 3.0, 1.0, 0, 0, 0, 0, false, 0));
        stats.Lines.Add(new MatchLine(0, 1, false, 0, 0, 1.0, 2.0, 0, 0, 0, 0, false, 0));
        var cell = new TacticCell(TransitionInstruction.Rush, "trap122", stats);

        (double f, double a, double d) = TacticMatrix.XgPer60(cell, 3600.0);

        Assert.Equal(2.0, f, 12);
        Assert.Equal(1.5, a, 12);
        Assert.Equal(0.5, d, 12);
        Assert.Equal(0.75, cell.Luck.PointsShare, 12);
    }

    [Fact]
    public void OpponentSystemAt_IsTheLastDecisionOfThePeriodAtOrBeforeTheTime()
    {
        var plans = new List<PlanRecord>
        {
            new(1, 0.0, TransitionInstruction.Rush, "forecheck212"),
            new(1, 50.0, TransitionInstruction.Rush, "trap122"),
            new(2, 0.0, TransitionInstruction.Rush, "forecheck212"),
        };

        Assert.Equal("forecheck212", MatchupStats.OpponentSystemAt(plans, 1, 49.9));
        Assert.Equal("trap122", MatchupStats.OpponentSystemAt(plans, 1, 50.0));
        Assert.Equal("trap122", MatchupStats.OpponentSystemAt(plans, 1, 900.0));
        Assert.Equal("forecheck212", MatchupStats.OpponentSystemAt(plans, 2, 10.0));
        Assert.Equal("-", MatchupStats.OpponentSystemAt(plans, 3, 10.0));
    }

    [Theory]
    [InlineData(0.474, "+0.47")]
    [InlineData(-0.1, "-0.10")]
    [InlineData(0.001, "0.00")]
    [InlineData(-0.001, "0.00")]
    public void Signed_ShowsTheSignOfNonZeroValues(double value, string expected)
    {
        Assert.Equal(expected, BatchReport.Signed(value));
    }
}
