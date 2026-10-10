using HockeyCoach.Harness.Data;
using HockeyCoach.Harness.Report;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Tests.Events;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Report;

/// <summary>Automatic target comparison (D-071) and the metric-to-statistic mapping (data-schema.md, targets.json).</summary>
public class TargetComparisonTests
{
    private const int Periods = 3;
    private const double PeriodSeconds = 1200.0;

    private static TargetMetric Metric(string name, double? min, double? max, string unit = "count", string scope = "perTeamPerMatch") =>
        new(name, unit, scope, min, max, min.HasValue && max.HasValue ? "approved" : "placeholder", "test");

    private static EventContext Ctx(int period = 1, int home = 5, int away = 5) =>
        new(period, 10.0, new Strength(home, away), EventTestData.FiveOnFive(), null!, null!);

    private static ShotEvent Shot(int shooter, double xg, ShotOutcome outcome, ChanceType type = ChanceType.OffensiveZone, EventContext? ctx = null, bool pressure = false)
    {
        int? stopper = outcome == ShotOutcome.Saved || outcome == ShotOutcome.Blocked ? 16 : null;
        return new ShotEvent(ctx ?? Ctx(), shooter, stopper, xg, null, type, pressure, null, outcome);
    }

    private static int SlotOf(int id) => id < 10 ? 0 : 1;

    private static int SlotOfSide(TeamSide side) => (int)side;

    private static TargetCounts Counts(params SimEvent[] events)
    {
        var counts = new TargetCounts(Periods, PeriodSeconds);
        counts.AddMatch(events, Array.Empty<PeriodSnapshot>(), SlotOf, SlotOfSide);
        return counts;
    }

    private static TargetRow Row(TargetCounts counts, TargetMetric metric) =>
        TargetComparison.Evaluate(new TargetsConfig(new[] { metric }), counts).Single();

    [Theory]
    [InlineData(4.0, TargetStatus.Ok)]
    [InlineData(7.0, TargetStatus.Ok)]
    [InlineData(3.99, TargetStatus.TooLow)]
    [InlineData(7.01, TargetStatus.TooHigh)]
    public void Compare_IncludesBounds(double value, TargetStatus expected)
    {
        Assert.Equal(expected, TargetComparison.Compare(MeasuredValue.Of(value), Metric("goalsPerMatch", 4, 7)));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(4.0, null)]
    [InlineData(null, 7.0)]
    public void Compare_WithoutBothBounds_ShowsValueOnly(double? min, double? max)
    {
        Assert.Equal(TargetStatus.NoRange, TargetComparison.Compare(MeasuredValue.Of(100.0), Metric("x", min, max)));
    }

    [Fact]
    public void Percentile_InterpolatesBetweenRanks()
    {
        double[] sorted = { 1.0, 2.0, 3.0, 4.0, 5.0 };

        Assert.Equal(1.4, TargetComparison.Percentile(sorted, 10.0), 9);
        Assert.Equal(3.0, TargetComparison.Percentile(sorted, 50.0), 9);
        Assert.Equal(4.6, TargetComparison.Percentile(sorted, 90.0), 9);
    }

    [Fact]
    public void ShotXgTypical_ComparesTenthPercentileWithMin_AndNinetiethWithMax()
    {
        TargetCounts counts = Counts(Enumerable.Range(1, 11).Select(i => (SimEvent)Shot(1, i / 100.0, ShotOutcome.Missed)).ToArray());

        TargetRow inside = Row(counts, Metric("shotXgTypical", 0.02, 0.10, "xg", "perShot"));
        TargetRow low = Row(counts, Metric("shotXgTypical", 0.03, 0.10, "xg", "perShot"));
        TargetRow high = Row(counts, Metric("shotXgTypical", 0.02, 0.09, "xg", "perShot"));

        Assert.Equal(new[] { 0.02, 0.06, 0.10 }, inside.Combined!.Values.Select(v => Math.Round(v, 9)));
        Assert.Equal(TargetStatus.Ok, inside.Status);
        Assert.Equal(TargetStatus.TooLow, low.Status);
        Assert.Equal(TargetStatus.TooHigh, high.Status);
    }

    [Fact]
    public void ShotCounts_FollowTheOutcomeMapping_PerTeamPerMatch()
    {
        TargetCounts counts = Counts(
            Shot(1, 0.1, ShotOutcome.Goal),
            Shot(1, 0.1, ShotOutcome.Saved),
            Shot(1, 0.1, ShotOutcome.Blocked),
            Shot(1, 0.1, ShotOutcome.Missed),
            Shot(11, 0.2, ShotOutcome.Saved));

        Assert.Equal(2.5, Row(counts, Metric("shotAttempts", null, null)).Combined!.Values[0]);
        Assert.Equal(4.0, Row(counts, Metric("shotAttempts", null, null)).Teams![0]!.Values[0]);
        Assert.Equal(1.5, Row(counts, Metric("shotsOnTarget", null, null)).Combined!.Values[0]);
        Assert.Equal(0.5, Row(counts, Metric("shotsBlocked", null, null)).Combined!.Values[0]);
        Assert.Equal(0.5, Row(counts, Metric("shotsMissed", null, null)).Combined!.Values[0]);
        Assert.Equal(0.5, Row(counts, Metric("goalsPerTeamPerMatch", null, null)).Combined!.Values[0]);
        Assert.Equal(1.0, Row(counts, Metric("goalsPerMatch", 4, 7, scope: "perMatch")).Combined!.Values[0]);
        Assert.Equal(TargetStatus.TooLow, Row(counts, Metric("goalsPerMatch", 4, 7, scope: "perMatch")).Status);
    }

    [Fact]
    public void FiveOnFiveMetrics_IgnoreOtherStrengths_AndFullMatchMetricsDoNot()
    {
        TargetCounts counts = Counts(Shot(1, 0.1, ShotOutcome.Saved), Shot(1, 0.3, ShotOutcome.Saved, ctx: Ctx(home: 5, away: 4)));

        Assert.Equal(1.0, Row(counts, Metric("shots5v5", null, null)).Teams![0]!.Values[0]);
        Assert.Equal(2.0, Row(counts, Metric("shotAttempts", null, null)).Teams![0]!.Values[0]);
        Assert.Equal(0.1, Row(counts, Metric("xg5v5", null, null, "xg")).Teams![0]!.Values[0], 9);
        Assert.Equal(0.4, Row(counts, Metric("xg", null, null, "xg")).Teams![0]!.Values[0], 9);
        Assert.False(counts.AllFiveOnFive);
        Assert.Equal(TargetStatus.NotMeasured, Row(counts, Metric("offensiveZoneTime5v5", null, null, "seconds")).Status);
    }

    [Fact]
    public void EventsAfterRegulation_AreIgnored()
    {
        TargetCounts counts = Counts(Shot(1, 0.1, ShotOutcome.Goal), Shot(1, 0.1, ShotOutcome.Goal, ctx: Ctx(period: Periods + 1)));

        Assert.Equal(1, counts.Goals);
    }

    [Fact]
    public void EvenStrengthXgShares_ExcludeFaceoffsFromTheThreeClasses_FaceoffShareUsesAllFour()
    {
        TargetCounts counts = Counts(
            Shot(1, 0.2, ShotOutcome.Missed, ChanceType.Rush),
            Shot(1, 0.1, ShotOutcome.Missed, ChanceType.Turnover),
            Shot(1, 0.1, ShotOutcome.Missed, ChanceType.OffensiveZone),
            Shot(1, 0.1, ShotOutcome.Missed, ChanceType.Faceoff),
            Shot(1, 0.5, ShotOutcome.Missed, ChanceType.Rush, Ctx(home: 5, away: 4)));

        Assert.Equal(0.5, Row(counts, Metric("evenStrengthXgShareRush", null, null, "share", "perTeam")).Combined!.Values[0], 9);
        Assert.Equal(0.25, Row(counts, Metric("evenStrengthXgShareTurnover", null, null, "share", "perTeam")).Combined!.Values[0], 9);
        Assert.Equal(0.25, Row(counts, Metric("evenStrengthXgShareZonePlay", null, null, "share", "perTeam")).Combined!.Values[0], 9);
        Assert.Equal(0.2, Row(counts, Metric("evenStrengthXgShareFaceoff", null, null, "share", "perTeam")).Combined!.Values[0], 9);
    }

    [Fact]
    public void Shares_AreRatiosOfSums_NotMeansOfTeamShares()
    {
        TargetCounts counts = Counts(
            new PassEvent(Ctx(), 1, 2, true, true),
            new PassEvent(Ctx(), 1, 2, false, true),
            new PassEvent(Ctx(), 1, 2, false, true),
            new PassEvent(Ctx(), 11, 12, true, false));

        Assert.Equal(0.75, Row(counts, Metric("passSuccessAll", null, null, "share", "perTeam")).Combined!.Values[0], 9);
        Assert.Equal(0.0, Row(counts, Metric("passSuccessAll", null, null, "share", "perTeam")).Teams![1]!.Values[0], 9);
        Assert.Equal(0.5, Row(counts, Metric("passesUnderPressureShare", null, null, "share", "perTeam")).Combined!.Values[0], 9);
        Assert.Equal(0.5, Row(counts, Metric("passSuccessUnderPressure", null, null, "share", "perTeam")).Combined!.Values[0], 9);
    }

    [Fact]
    public void Faceoffs_GiveFaceoffsPerMatch_AndRegulationSecondsPerFaceoff()
    {
        var faceoff = new FaceoffEvent(Ctx(), 1, 11, TeamSide.Home, FaceoffLocation.NeutralZone);
        var counts = new TargetCounts(Periods, PeriodSeconds);
        counts.AddMatch(Enumerable.Repeat<SimEvent>(faceoff, 60).ToList(), Array.Empty<PeriodSnapshot>(), SlotOf, SlotOfSide);
        counts.AddMatch(Enumerable.Repeat<SimEvent>(faceoff, 40).ToList(), Array.Empty<PeriodSnapshot>(), SlotOf, SlotOfSide);

        Assert.Equal(50.0, Row(counts, Metric("faceoffsPerMatch", null, null, scope: "perMatch")).Combined!.Values[0], 9);
        Assert.Equal((60.0 + 90.0) / 2.0, Row(counts, Metric("meanTimeBetweenStoppages", null, null, "seconds", "perMatch")).Combined!.Values[0], 9);
    }

    [Fact]
    public void UnknownMetricAndSmartVsRandom_AreNotMeasured()
    {
        TargetCounts counts = Counts(Shot(1, 0.1, ShotOutcome.Goal));

        Assert.Equal(TargetStatus.NotMeasured, Row(counts, Metric("somethingNew", 1, 2)).Status);
        TargetRow smart = Row(counts, Metric("smartVsRandomWinRate", 0.75, 0.85, "share", "perMatchup"));
        Assert.Equal(TargetStatus.NotMeasured, smart.Status);
        Assert.Contains("not measured", TargetComparison.Write(new[] { smart }, new[] { "A", "B" }), StringComparison.Ordinal);
    }

    [Fact]
    public void MatchupRecord_CountsWinsTiesAndLosses_TieIsHalfAWin()
    {
        var record = new MatchupRecord();
        Assert.Null(record.WinRate);

        record.Add(3, 1);
        record.Add(4, 2);
        record.Add(2, 2);
        record.Add(1, 5);

        Assert.Equal(2, record.Wins);
        Assert.Equal(1, record.Ties);
        Assert.Equal(1, record.Losses);
        Assert.Equal(4, record.Matches);
        Assert.Equal((2 + 0.5) / 4.0, record.WinRate!.Value, 9);
    }

    [Fact]
    public void SmartVsRandom_WithARecord_IsComparedAndShowsWinsTiesLosses()
    {
        TargetCounts counts = Counts(Shot(1, 0.1, ShotOutcome.Goal));
        var record = new MatchupRecord();
        for (int i = 0; i < 7; i++)
        {
            record.Add(3, 1);
        }

        record.Add(2, 2);
        record.Add(2, 2);
        record.Add(0, 1);
        counts.SmartVsRandom = record;

        TargetRow row = Row(counts, Metric("smartVsRandomWinRate", 0.75, 0.85, "share", "perMatchup"));

        Assert.Equal(0.8, row.Combined!.Values[0], 9); // (7 + 2 / 2) / 10
        Assert.Equal(TargetStatus.Ok, row.Status);
        Assert.Null(row.Teams);
        string text = TargetComparison.Write(new[] { row }, new[] { "A", "B" });
        Assert.Contains("80.0 %  (7 wins / 2 ties / 1 losses)  → OK", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SmartVsRandom_AllTies_IsHalf()
    {
        TargetCounts counts = Counts(Shot(1, 0.1, ShotOutcome.Goal));
        counts.SmartVsRandom = new MatchupRecord();
        counts.SmartVsRandom.Add(1, 1);
        counts.SmartVsRandom.Add(0, 0);

        TargetRow row = Row(counts, Metric("smartVsRandomWinRate", 0.75, 0.85, "share", "perMatchup"));

        Assert.Equal(0.5, row.Combined!.Values[0], 9);
        Assert.Equal(TargetStatus.TooLow, row.Status);
    }

    [Fact]
    public void SmartVsRandom_WithAnEmptyRecord_IsNotMeasured()
    {
        TargetCounts counts = Counts(Shot(1, 0.1, ShotOutcome.Goal));
        counts.SmartVsRandom = new MatchupRecord();

        Assert.Equal(TargetStatus.NotMeasured, Row(counts, Metric("smartVsRandomWinRate", 0.75, 0.85, "share", "perMatchup")).Status);
    }

    [Fact]
    public void PairedMatches_TargetsMatchTheRecordAndTheSnapshots()
    {
        (PairedTeam a, PairedTeam b) = MatchCommand.PairTeams(ShiftTestData.Data, "forecheck212", "trap122");
        List<PairedGame> games = PairedMatches.Play(ShiftTestData.Data, a, b, 3, 1).ToList();
        var counts = new TargetCounts(ShiftTestData.Data.Tuning.Time.Periods, ShiftTestData.Data.Tuning.Time.PeriodSeconds);
        var record = new PairedRecord();
        foreach (PairedGame game in games)
        {
            MatchCommand.AddTargets(counts, game);
            record.Add(game);
        }

        Assert.Equal(record.TeamAGoals, counts.Teams[0].Goals);
        Assert.Equal(record.TeamBGoals, counts.Teams[1].Goals);
        PeriodSnapshot last = games[0].Result.Periods[^1];
        PeriodSnapshot lastSwapped = games[1].Result.Periods[^1];
        Assert.Equal(last.ForwardShifts[0].Sum() + lastSwapped.ForwardShifts[1].Sum(), counts.Teams[0].ForwardShifts);
        Assert.True(counts.Teams[0].ForwardShifts > 0 && counts.Teams[1].PairShifts > 0);
    }

    [Fact]
    public void Averages_PrintEveryTargetMetric()
    {
        string text = MatchCommand.Run(ShiftTestData.Data, new MatchCommand.Options(1, "forecheck212", "trap122", false, 2));

        foreach (TargetMetric metric in ShiftTestData.Data.Targets.Metrics)
        {
            Assert.Contains("  " + metric.Name + " [", text, StringComparison.Ordinal);
        }

        Assert.Contains("goalsPerMatch [4.00, 7.00]: ", text, StringComparison.Ordinal);
    }
}
