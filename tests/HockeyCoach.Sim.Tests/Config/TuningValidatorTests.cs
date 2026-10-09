using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Config;

public class TuningValidatorTests
{
    [Fact]
    public void Formula_IsValid_WithDocumentedDefaults()
    {
        Assert.Empty(TuningValidator.Validate(TestChecks.Formula()));
    }

    [Theory]
    [InlineData(-0.1, 0.02, 0.98)]
    [InlineData(double.NaN, 0.02, 0.98)]
    [InlineData(0.15, 0.0, 0.98)]
    [InlineData(0.15, 0.02, 1.0)]
    [InlineData(0.15, 0.6, 0.4)]
    public void Formula_IsInvalid_WithBadValues(double k, double min, double max)
    {
        Assert.NotEmpty(TuningValidator.Validate(new CheckFormulaConfig(k, min, max)));
    }

    [Fact]
    public void Check_IsValid_WhenEachSideSumsToOne()
    {
        Assert.Empty(TuningValidator.Validate(TestChecks.Pass()));
        Assert.Empty(TuningValidator.Validate(TestChecks.SlotShot()));
    }

    [Fact]
    public void Check_IsInvalid_WhenAttackerWeightsDoNotSumToOne()
    {
        var check = new CheckDefinition(
            "pass",
            0.85,
            new[] { new WeightTerm("passer", StatRef.Of(SkaterStat.Passing), 0.6) },
            TestChecks.Pass().Defender);

        IReadOnlyList<string> errors = TuningValidator.Validate(check);

        Assert.Single(errors);
        Assert.Contains("checks.pass.attacker", errors[0]);
    }

    [Fact]
    public void Check_IsInvalid_WhenDefenderSideIsEmpty()
    {
        var check = new CheckDefinition("x", 0.5, TestChecks.Pass().Attacker, Array.Empty<WeightTerm>());

        IReadOnlyList<string> errors = TuningValidator.Validate(check);

        Assert.Single(errors);
        Assert.Contains("checks.x.defender", errors[0]);
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
    public void Check_IsInvalid_WhenWeightIsNegative()
    {
        var check = new CheckDefinition(
            "x",
            0.5,
            new[]
            {
                new WeightTerm("a", StatRef.Of(SkaterStat.Speed), 1.5),
                new WeightTerm("a", StatRef.Of(SkaterStat.Hands), -0.5),
            },
            TestChecks.Pass().Defender);

        Assert.Contains(TuningValidator.Validate(check), e => e.Contains("weight must be", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_IsInvalid_WhenSameStatIsListedTwiceForParticipant()
    {
        var check = new CheckDefinition(
            "x",
            0.5,
            new[]
            {
                new WeightTerm("a", StatRef.Of(SkaterStat.Speed), 0.5),
                new WeightTerm("a", StatRef.Of(SkaterStat.Speed), 0.5),
            },
            TestChecks.Pass().Defender);

        Assert.Contains(TuningValidator.Validate(check), e => e.Contains("listed twice", StringComparison.Ordinal));
    }

    [Fact]
    public void Checks_AreInvalid_WhenIdsRepeat()
    {
        Assert.Contains(
            TuningValidator.Validate(new[] { TestChecks.Pass(), TestChecks.Pass() }),
            e => e.Contains("duplicate check id", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_ToleratesFloatingPointRounding_InWeightSum()
    {
        var check = new CheckDefinition(
            "x",
            0.5,
            new[]
            {
                new WeightTerm("a", StatRef.Of(SkaterStat.Strength), 0.5),
                new WeightTerm("a", StatRef.Of(SkaterStat.Hands), 0.3),
                new WeightTerm("a", StatRef.Of(SkaterStat.Speed), 0.2),
            },
            TestChecks.Pass().Defender);

        Assert.Empty(TuningValidator.Validate(check));
    }
}
