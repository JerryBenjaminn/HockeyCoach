using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Config;

public class TuningValidatorTests
{
    private static readonly WeightTerm[] NoTerms = Array.Empty<WeightTerm>();

    private static WeightTerm Term(string role, SkaterStat stat, double weight)
    {
        return new WeightTerm(role, StatRef.Of(stat), weight);
    }

    private static StatsConfig Stats(params StatGrade[] grades)
    {
        return new StatsConfig(1, 20, grades.Length > 0 ? grades : new[]
        {
            new StatGrade("A", 17, 20), new StatGrade("B", 13, 16), new StatGrade("C", 9, 12), new StatGrade("D", 5, 8), new StatGrade("E", 1, 4),
        });
    }

    [Fact]
    public void Formula_IsValid_WithDocumentedDefaults()
    {
        Assert.Empty(TuningValidator.Validate(TestChecks.Formula(), Stats()));
    }

    [Theory]
    [InlineData(-0.1, 0.02, 0.98, 10.5)]
    [InlineData(double.NaN, 0.02, 0.98, 10.5)]
    [InlineData(0.15, 0.0, 0.98, 10.5)]
    [InlineData(0.15, 0.02, 1.0, 10.5)]
    [InlineData(0.15, 0.6, 0.4, 10.5)]
    [InlineData(0.15, 0.02, 0.98, 0.5)]
    [InlineData(0.15, 0.02, 0.98, 21.0)]
    public void Formula_IsInvalid_WithBadValues(double k, double min, double max, double referenceValue)
    {
        Assert.NotEmpty(TuningValidator.Validate(new CheckFormulaConfig(k, min, max, referenceValue), Stats()));
    }

    [Fact]
    public void Stats_AreValid_WhenGradesCoverScaleWithoutGaps()
    {
        Assert.Empty(TuningValidator.Validate(Stats()));
    }

    [Fact]
    public void Stats_AreInvalid_WhenGradesLeaveAGap()
    {
        IReadOnlyList<string> errors = TuningValidator.Validate(Stats(new StatGrade("A", 11, 20), new StatGrade("B", 1, 9)));

        Assert.Contains(errors, e => e.StartsWith("stats.grades.A", StringComparison.Ordinal));
    }

    [Fact]
    public void Stats_AreInvalid_WhenGradesOverlap()
    {
        Assert.NotEmpty(TuningValidator.Validate(Stats(new StatGrade("A", 10, 20), new StatGrade("B", 1, 10))));
    }

    [Fact]
    public void Stats_AreInvalid_WhenGradesDoNotReachMax()
    {
        Assert.Contains(TuningValidator.Validate(Stats(new StatGrade("A", 1, 19))), e => e.StartsWith("stats.grades:", StringComparison.Ordinal));
    }

    [Fact]
    public void TwoSidedCheck_IsValid_WhenEachSideSumsToOne()
    {
        Assert.Empty(TuningValidator.Validate(TestChecks.Pass()));
        Assert.Empty(TuningValidator.Validate(TestChecks.Breakout()));
    }

    [Fact]
    public void TwoSidedCheck_IsInvalid_WhenAttackerWeightsDoNotSumToOne()
    {
        var check = CheckDefinition.TwoSided("pass", 0.85, new[] { Term("passer", SkaterStat.Passing, 0.6) }, TestChecks.Pass().Defender);

        IReadOnlyList<string> errors = TuningValidator.Validate(check);

        Assert.Equal("checks.pass.attacker: weights must sum to 1, sum was 0.6", Assert.Single(errors));
    }

    [Fact]
    public void TwoSidedCheck_IsInvalid_WhenDefenderSideIsMissing()
    {
        var check = CheckDefinition.TwoSided("deke", 0.5, TestChecks.Pass().Attacker, NoTerms);

        Assert.Equal("checks.deke.defender: side is missing or empty", Assert.Single(TuningValidator.Validate(check)));
    }

    [Fact]
    public void TwoSidedCheck_IsInvalid_WhenItHasASide()
    {
        var check = new CheckDefinition("pass", CheckKind.TwoSided, CheckSide.Attacker, 0.85, TestChecks.Pass().Attacker, TestChecks.Pass().Defender);

        Assert.Contains(TuningValidator.Validate(check), e => e.StartsWith("checks.pass.side", StringComparison.Ordinal));
    }

    [Fact]
    public void OneSidedCheck_IsValid_WhenPresentSideSumsToOneAndOtherSideIsAbsent()
    {
        Assert.Empty(TuningValidator.Validate(TestChecks.Block()));
    }

    [Fact]
    public void OneSidedCheck_IsInvalid_WithoutSide()
    {
        var check = new CheckDefinition("block", CheckKind.OneSided, null, 0.75, NoTerms, TestChecks.Block().Defender);

        Assert.Contains(TuningValidator.Validate(check), e => e.StartsWith("checks.block.side", StringComparison.Ordinal));
    }

    [Fact]
    public void OneSidedCheck_IsInvalid_WhenOtherSideIsWritten()
    {
        var check = new CheckDefinition("block", CheckKind.OneSided, CheckSide.Defender, 0.75, TestChecks.Pass().Attacker, TestChecks.Block().Defender);

        Assert.Contains(TuningValidator.Validate(check), e => e.StartsWith("checks.block.attacker: must be absent", StringComparison.Ordinal));
    }

    [Fact]
    public void OneSidedCheck_IsInvalid_WhenPresentSideIsMissing()
    {
        var check = new CheckDefinition("block", CheckKind.OneSided, CheckSide.Defender, 0.75, NoTerms, NoTerms);

        Assert.Contains(TuningValidator.Validate(check), e => e.StartsWith("checks.block.defender: side is missing", StringComparison.Ordinal));
    }

    [Fact]
    public void NoCheck_IsValid_WithOnlyParameters()
    {
        var check = new CheckDefinition("dumpIn", CheckKind.NoCheck, null, null, NoTerms, NoTerms, null, new Dictionary<string, double> { ["goaliePuckHandlingPerPoint"] = 0.03 });

        Assert.Empty(TuningValidator.Validate(check));
    }

    [Fact]
    public void NoCheck_IsInvalid_WithP0OrSides()
    {
        var check = new CheckDefinition("dumpIn", CheckKind.NoCheck, null, 0.5, TestChecks.Pass().Attacker, NoTerms);

        IReadOnlyList<string> errors = TuningValidator.Validate(check);

        Assert.Contains("checks.dumpIn.p0: noCheck must not have p0", errors);
        Assert.Contains("checks.dumpIn.attacker: noCheck must not have an attacker side", errors);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Check_IsInvalid_WhenBaseRateIsOutsideOpenUnitInterval(double p0)
    {
        Assert.Contains(TuningValidator.Validate(TestChecks.Pass(p0)), e => e.StartsWith("checks.pass.p0", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_IsInvalid_WhenShareIsOutsideOpenUnitInterval()
    {
        var check = new CheckDefinition("loosePuck", CheckKind.TwoSided, null, 0.5, TestChecks.Pass().Attacker, TestChecks.Pass().Defender, null, new Dictionary<string, double> { ["noWinnerShare"] = 1.2 });

        Assert.Contains(TuningValidator.Validate(check), e => e.StartsWith("checks.loosePuck.noWinnerShare", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_IsInvalid_WithUnknownRole()
    {
        var check = CheckDefinition.TwoSided("x", 0.5, new[] { Term("striker", SkaterStat.Speed, 1.0) }, TestChecks.Pass().Defender);

        Assert.Contains("checks.x.attacker.striker: unknown participant role", TuningValidator.Validate(check));
    }

    [Fact]
    public void Check_IsInvalid_WhenGoalieRoleReadsSkaterStat()
    {
        var check = CheckDefinition.OneSided("rebound", 0.15, CheckSide.Defender, new[] { Term("goalie", SkaterStat.Positioning, 1.0) });

        Assert.Contains(TuningValidator.Validate(check), e => e.Contains("reads goalie stats", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_IsInvalid_WhenWeightIsNegative()
    {
        var check = CheckDefinition.TwoSided(
            "x", 0.5, new[] { Term("carrier", SkaterStat.Speed, 1.5), Term("carrier", SkaterStat.Hands, -0.5) }, TestChecks.Pass().Defender);

        Assert.Contains(TuningValidator.Validate(check), e => e.Contains("weight must be", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_IsInvalid_WhenSameStatIsListedTwiceForParticipant()
    {
        var check = CheckDefinition.TwoSided(
            "x", 0.5, new[] { Term("carrier", SkaterStat.Speed, 0.5), Term("carrier", SkaterStat.Speed, 0.5) }, TestChecks.Pass().Defender);

        Assert.Contains(TuningValidator.Validate(check), e => e.Contains("listed twice", StringComparison.Ordinal));
    }

    [Fact]
    public void Checks_AreInvalid_WhenIdsRepeat()
    {
        Assert.Contains(TuningValidator.Validate(new[] { TestChecks.Pass(), TestChecks.Pass() }), e => e.Contains("duplicate check id", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_ToleratesFloatingPointRounding_InWeightSum()
    {
        var check = CheckDefinition.TwoSided(
            "x",
            0.5,
            new[] { Term("participant", SkaterStat.Strength, 0.5), Term("participant", SkaterStat.Hands, 0.3), Term("participant", SkaterStat.Speed, 0.2) },
            TestChecks.Pass().Defender);

        Assert.Empty(TuningValidator.Validate(check));
    }

    [Fact]
    public void Shot_IsValid_WhenZonesMatchRink()
    {
        Assert.Empty(TuningValidator.Validate(TestChecks.Shot(), new[] { "longRange", "slot" }));
    }

    [Fact]
    public void Shot_IsInvalid_WhenZonesDoNotMatchRink()
    {
        IReadOnlyList<string> errors = TuningValidator.Validate(TestChecks.Shot(), new[] { "slot", "crease" });

        Assert.Contains("checks.shot.baseXg: missing xG zone crease (rink.json xgZones)", errors);
        Assert.Contains("checks.shot.attackerByXgZone.longRange: not an xG zone in rink.json", errors);
    }

    [Fact]
    public void Shot_IsInvalid_WhenOwnBoundsAreReversed()
    {
        Assert.Contains(
            TuningValidator.Validate(TestChecks.Shot(min: 0.5, max: 0.1), new[] { "longRange", "slot" }),
            e => e.StartsWith("checks.shot: minProbability", StringComparison.Ordinal));
    }
}
