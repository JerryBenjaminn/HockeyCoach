using System.Text.Json.Nodes;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Data;

/// <summary>Milestone 3 typed sections (data-schema.md O-15, O-16): energy, organization, familiarity, transitions, pressure.</summary>
public class TuningStateSectionsLoaderTests
{
    private static LoadResult<TuningConfig> Load(Action<JsonObject>? edit = null)
    {
        string json = edit == null ? Fixtures.Read("tuning-small.json") : Fixtures.Edit("tuning-small.json", edit);
        return TuningLoader.Parse(json, Fixtures.SmallRinkXgZones);
    }

    [Fact]
    public void Fixture_MapsEveryStateSection()
    {
        LoadResult<TuningConfig> result = Load();
        Assert.Empty(result.Errors);
        TuningConfig t = result.Value!;

        Assert.Equal(1.0, t.Energy.Start);
        Assert.Equal(0.0055, t.Energy.DrainPerSecondOnIce);
        Assert.Equal(0.011, t.Energy.BenchRecoveryRate);
        Assert.Equal(-1.2, t.Energy.CheckModifierAtZero);
        Assert.Equal(0.35, t.Organization.DropIn(RinkZone.Defensive));
        Assert.Equal(0.5, t.Organization.DropIn(RinkZone.Neutral));
        Assert.Equal(0.65, t.Organization.DropIn(RinkZone.Offensive));
        Assert.Equal(0.04, t.Organization.RecoveryPerSecond);
        Assert.Equal(0.5, t.Organization.RecoveryWeights[SkaterStat.Speed]);
        Assert.Equal(0.8, t.Organization.OrganizedThreshold);
        Assert.Equal(2.0, t.Familiarity.FreeUses);
        Assert.Equal(0.5, t.Familiarity.IntermissionMultiplier);
        Assert.Equal(4, t.Transitions.RushMaxActions);
        Assert.Equal(0.05, t.Transitions.RushShotMinBaseXg);
        Assert.Equal(0.01, t.Pressure.DecayPerSecond);
        Assert.Equal(0.5, t.Pressure.KeepOnStoppage);
        Assert.Equal(20.0, t.Time.StoppageChangeMinSeconds);
    }

    [Theory]
    [InlineData("energy")]
    [InlineData("organization")]
    [InlineData("familiarity")]
    [InlineData("transitions")]
    public void MissingStateSection_IsReported(string section)
    {
        Assert.Contains(section + ": missing", Load(r => r.Remove(section)).Errors);
    }

    [Theory]
    [InlineData("energy", "drainPerSecondOnIce")]
    [InlineData("energy", "benchRecoveryRate")]
    [InlineData("organization", "recoveryPerSecond")]
    [InlineData("organization", "dropFactorOnShotOrDump")]
    [InlineData("organization", "dropPerCommittedPlayer")]
    [InlineData("familiarity", "freeUses")]
    [InlineData("familiarity", "intermissionMultiplier")]
    [InlineData("pressure", "decayPerSecond")]
    [InlineData("time", "stoppageChangeMinSeconds")]
    public void RequiredKey_IsReported(string section, string key)
    {
        Assert.Contains(section + "." + key + ": missing", Load(r => r[section]!.AsObject().Remove(key)).Errors);
    }

    [Fact]
    public void OldNames_AreRejectedWithTheirReplacement()
    {
        Assert.Contains(Load(r => r["energy"]!["benchRecoveryPerSecond"] = 0.01).Errors, e => e.StartsWith("energy.benchRecoveryPerSecond: renamed", StringComparison.Ordinal));
        Assert.Contains(Load(r => r["organization"]!["recoveryPerEvent"] = 0.15).Errors, e => e.StartsWith("organization.recoveryPerEvent: renamed", StringComparison.Ordinal));
        Assert.Contains(Load(r => r["chanceTypes"]!["rushOrganizationBelow"] = 0.8).Errors, e => e.StartsWith("chanceTypes.rushOrganizationBelow: renamed or removed", StringComparison.Ordinal));
    }

    [Fact]
    public void OrganizationModifier_IsRequiredOnPresentChecks()
    {
        Assert.Contains("checks.pass.modifiers.organization: missing (O-6)", Load(r => r["checks"]!["pass"]!["modifiers"]!.AsObject().Remove("organization")).Errors);
        Assert.Contains("checks.shot.modifiers.organization: missing (O-6)", Load(r => r["checks"]!["shot"]!["modifiers"]!.AsObject().Remove("organization")).Errors);
    }

    [Theory]
    [InlineData("energy", "start", 0.0)]
    [InlineData("energy", "drainPerSecondOnIce", 1.5)]
    [InlineData("energy", "costPerAction", -0.1)]
    [InlineData("energy", "enduranceCostReductionPerPoint", 0.2)]
    [InlineData("energy", "benchRecoveryRate", 0.0)]
    [InlineData("energy", "checkModifierAtZero", 0.1)]
    [InlineData("organization", "dropFactorOnShotOrDump", 1.5)]
    [InlineData("organization", "recoveryPerSecond", 0.0)]
    [InlineData("organization", "recoveryPerStatPoint", 0.2)]
    [InlineData("organization", "organizedThreshold", 0.0)]
    [InlineData("pressure", "keepOnStoppage", 1.5)]
    [InlineData("pressure", "decayPerSecond", -0.1)]
    [InlineData("pressure", "mentalToughnessReductionPerPoint", 0.2)]
    [InlineData("familiarity", "intermissionMultiplier", 2.0)]
    [InlineData("familiarity", "penaltyPerRepeat", -1.0)]
    [InlineData("transitions", "rushShotMinBaseXg", 1.5)]
    [InlineData("transitions", "rushMaxActions", 0.0)]
    public void OutOfRangeValue_IsRejected(string section, string key, double value)
    {
        LoadResult<TuningConfig> result = Load(r => r[section]![key] = key == "rushMaxActions" ? JsonValue.Create((int)value) : JsonValue.Create(value));

        Assert.Contains(result.Errors, e => e.StartsWith(section + "." + key, StringComparison.Ordinal));
    }

    [Fact]
    public void DropOnTurnover_NeedsExactlyTheThreeZones_AndWeightsSumToOne()
    {
        Assert.Contains("organization.dropOnTurnover.slot: unknown key", Load(r => r["organization"]!["dropOnTurnover"]!["slot"] = 0.1).Errors);
        Assert.Contains("organization.dropOnTurnover.neutral: missing", Load(r => r["organization"]!["dropOnTurnover"]!.AsObject().Remove("neutral")).Errors);
        Assert.Contains(Load(r => r["organization"]!["recoveryWeights"]!["speed"] = 0.7).Errors, e => e.StartsWith("organization.recoveryWeights: weights must sum to 1", StringComparison.Ordinal));
        Assert.Contains("organization.recoveryWeights.wisdom: unknown skater stat", Load(r => r["organization"]!["recoveryWeights"]!["wisdom"] = 0.0).Errors);
    }

    [Fact]
    public void RepositoryTuning_LoadsTheMilestone3Values()
    {
        TuningConfig t = GameData.Load(Path.Combine(Fixtures.RepoRoot(), "data")).Tuning;

        Assert.Equal(4, t.Transitions.RushMaxActions);
        Assert.Equal(0.01, t.Pressure.DecayPerSecond);
        Assert.Equal(20.0, t.Time.StoppageChangeMinSeconds);
        Assert.Equal(8.0, t.ChanceTypes.TurnoverWindowSeconds);
    }
}
