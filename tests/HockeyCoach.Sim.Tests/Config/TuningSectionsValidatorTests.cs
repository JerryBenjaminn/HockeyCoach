using HockeyCoach.Sim.Config;

namespace HockeyCoach.Sim.Tests.Config;

public class TuningSectionsValidatorTests
{
    private static readonly KeyValuePair<string, double>[] ActionTimes = TimeConfig.PlayActionKeys
        .Select(k => new KeyValuePair<string, double>(k, 2.0))
        .ToArray();

    private static TimeConfig Time(int periods = 3, double periodSeconds = 1200, double forward = 45, double defence = 50, double setup = 6, double regroup = 8, IEnumerable<KeyValuePair<string, double>>? actions = null)
    {
        return new TimeConfig(periods, periodSeconds, forward, defence, actions ?? ActionTimes, setup, regroup);
    }

    [Fact]
    public void Time_IsValid_WithDocumentedShape()
    {
        Assert.Empty(TuningValidator.Validate(Time()));
    }

    [Theory]
    [InlineData(0, 1200, 45, 50, 6, 8)]
    [InlineData(3, 0, 45, 50, 6, 8)]
    [InlineData(3, 1200, -1, 50, 6, 8)]
    [InlineData(3, 1200, 45, double.NaN, 6, 8)]
    [InlineData(3, 1200, 45, 50, -1, 8)]
    [InlineData(3, 1200, 45, 50, 6, double.PositiveInfinity)]
    public void Time_IsInvalid_WithBadValues(int periods, double periodSeconds, double forward, double defence, double setup, double regroup)
    {
        Assert.NotEmpty(TuningValidator.Validate(Time(periods, periodSeconds, forward, defence, setup, regroup)));
    }

    [Fact]
    public void Time_RejectsNegativeActionTime()
    {
        var actions = ActionTimes.Select(a => a.Key == "pass" ? new KeyValuePair<string, double>("pass", -0.5) : a);

        Assert.Contains("time.secondsPerAction.pass: must be a finite number >= 0, was -0.5", TuningValidator.Validate(Time(actions: actions)));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(4, 0)]
    public void Plays_RequireAtLeastOneBeatAndNode(int maxBeats, int maxNodes)
    {
        Assert.NotEmpty(TuningValidator.Validate(new PlaysConfig(maxBeats, maxNodes)));
    }

    [Fact]
    public void Plays_IsValid_WithDocumentedValues()
    {
        Assert.Empty(TuningValidator.Validate(new PlaysConfig(4, 2)));
    }

    [Theory]
    [InlineData(-0.1, 8)]
    [InlineData(1.1, 8)]
    [InlineData(0.8, -1)]
    public void ChanceTypes_IsInvalid_WithBadValues(double organization, double window)
    {
        Assert.NotEmpty(TuningValidator.Validate(new ChanceTypesConfig(organization, window)));
    }

    [Fact]
    public void ChanceClasses_MustBeOrderedTopGoodModerate()
    {
        Assert.Empty(TuningValidator.Validate(new ChanceClassesConfig(0.15, 0.07, 0.03)));
        Assert.Contains(
            "chanceClasses: thresholds must satisfy topMinXg >= goodMinXg >= moderateMinXg",
            TuningValidator.Validate(new ChanceClassesConfig(0.05, 0.07, 0.03)));
        Assert.NotEmpty(TuningValidator.Validate(new ChanceClassesConfig(1.5, 0.07, 0.03)));
    }
}
