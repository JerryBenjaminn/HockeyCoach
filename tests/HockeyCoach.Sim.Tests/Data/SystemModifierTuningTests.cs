using System.Text.Json.Nodes;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Data;

/// <summary>
/// The defensive-system modifiers of data-schema.md M-9 (D-078 K4): <c>checks.zoneEntryCarry.modifiers.strengthDifferencePerPlayer</c>
/// and <c>checks.breakout.modifiers.perForechecker</c> are required, signed and listed as placeholders.
/// </summary>
public class SystemModifierTuningTests
{
    private const string StrengthPath = "checks.zoneEntryCarry.modifiers.strengthDifferencePerPlayer";
    private const string ForecheckerPath = "checks.breakout.modifiers.perForechecker";

    private static JsonObject Modifiers(JsonObject root, string check) => root["checks"]![check]!["modifiers"]!.AsObject();

    [Fact]
    public void RepositoryTuning_HasTheSpecPlaceholderValues_AndListsThemAsPlaceholders()
    {
        TuningConfig t = Shift.ShiftTestData.Data.Tuning;

        Assert.Equal(0.25, t.GetCheck("zoneEntryCarry").Modifiers.Get(TuningValidator.StrengthDifferencePerPlayerModifier));
        Assert.Equal(-0.25, t.GetCheck("breakout").Modifiers.Get(TuningValidator.PerForecheckerModifier));
        var placeholders = RepositoryTuning.Read()["_placeholders"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        Assert.Contains(StrengthPath, placeholders);
        Assert.Contains(ForecheckerPath, placeholders);
    }

    [Fact]
    public void StrengthDifferencePerPlayer_BelowZero_IsRejected()
    {
        LoadResult<TuningConfig> result = RepositoryTuning.Parse(r => Modifiers(r, "zoneEntryCarry")["strengthDifferencePerPlayer"] = -0.1);

        Assert.Contains(StrengthPath + ": must be >= 0, was -0.1", result.Errors);
    }

    [Fact]
    public void PerForechecker_AboveZero_IsRejected()
    {
        LoadResult<TuningConfig> result = RepositoryTuning.Parse(r => Modifiers(r, "breakout")["perForechecker"] = 0.1);

        Assert.Contains(ForecheckerPath + ": must be <= 0, was 0.1", result.Errors);
    }

    [Fact]
    public void ZeroValues_AreAccepted()
    {
        LoadResult<TuningConfig> result = RepositoryTuning.Parse(r =>
        {
            Modifiers(r, "zoneEntryCarry")["strengthDifferencePerPlayer"] = 0.0;
            Modifiers(r, "breakout")["perForechecker"] = 0.0;
        });

        Assert.Empty(result.Errors);
        Assert.Empty(ShiftTuningRequirements.Validate(result.Value!));
    }

    [Theory]
    [InlineData("zoneEntryCarry", "strengthDifferencePerPlayer", StrengthPath)]
    [InlineData("breakout", "perForechecker", ForecheckerPath)]
    public void MissingModifier_IsRejectedWhenLoadingTheData(string check, string key, string path)
    {
        LoadResult<TuningConfig> parsed = RepositoryTuning.Parse(r => Modifiers(r, check).Remove(key));
        Assert.Empty(parsed.Errors);
        Assert.Contains(path + ": missing (used by the shift simulation)", ShiftTuningRequirements.Validate(parsed.Value!));

        DataLoadException ex = Assert.Throws<DataLoadException>(() => RepositoryTuning.Load(r => Modifiers(r, check).Remove(key)));
        Assert.Contains(path + ": missing (used by the shift simulation)", ex.Errors);
    }
}
