using System.Text.Json.Nodes;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Data;

/// <summary>tuning.json <c>ai</c> and the timeout keys of <c>time</c> (data-schema.md AI-3, AI-4, AI-6).</summary>
public class AiTuningLoaderTests
{
    private static LoadResult<TuningConfig> Load(Action<JsonObject>? edit = null)
    {
        string json = edit == null ? Fixtures.Read("tuning-small.json") : Fixtures.Edit("tuning-small.json", edit);
        return TuningLoader.Parse(json, Fixtures.SmallRinkXgZones);
    }

    private static JsonObject Ai(JsonObject root) => root["ai"]!.AsObject();

    private static JsonObject Rule(JsonObject root) => Ai(root)["rulebased"]!.AsObject();

    private static void AssertError(Action<JsonObject> edit, string expectedStart)
    {
        LoadResult<TuningConfig> result = Load(edit);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.StartsWith(expectedStart, StringComparison.Ordinal));
    }

    [Fact]
    public void ValidFixture_MapsTheAiSectionAndTimeoutKeys()
    {
        TuningConfig tuning = Load().Value!;
        AiConfig ai = tuning.Ai;

        Assert.Equal(1, tuning.Time.TimeoutsPerTeamPerMatch);
        Assert.Equal(30.0, tuning.Time.TimeoutRestSeconds);
        Assert.Equal(0.5, ai.Random.OnTheFlyChangeProbability);
        Assert.Equal(0.02, ai.Random.TimeoutProbabilityPerStoppage);
        Assert.Equal(0.0011, ai.Estimate.ExtraDrainPerSecondOnIce);
        Assert.Equal(0.7, ai.Rulebased.ChangeEnergyThreshold);
        Assert.Equal(300.0, ai.Rulebased.LateGameSeconds);
        Assert.Equal(new[] { "chase", "normal", "protect" }, ai.Rulebased.Modes.Keys);
        AiModeConfig chase = ai.Rulebased.Modes["chase"];
        Assert.Equal("forecheck212", chase.System);
        Assert.Equal("rush", chase.Transition);
        Assert.True(chase.NetFrontAfterShot);
        Assert.Equal(2, chase.LooseChasers);
        Assert.True(chase.Pinch);
        Assert.Equal(0.2, ai.Rulebased.PlayBonusBySystem["trap122"]["dumpAndChase"]);
        Assert.Equal(2, ai.Rulebased.Timeout.MaxDeficitGoals);
        Assert.Equal("trap122", ai.Adaptive.PassiveSystem);
        Assert.Equal(0.4, ai.Adaptive.ControlledEntryShareForAggressive);
    }

    [Fact]
    public void RepositoryData_LoadsTheAiSection_WithTheDocumentedPlaceholders()
    {
        string dataDir = Path.Combine(Fixtures.RepoRoot(), "data");
        TuningConfig tuning = GameData.Load(dataDir).Tuning;
        Assert.Equal(0.5, tuning.Ai.Random.OnTheFlyChangeProbability);
        Assert.Equal(-0.1, tuning.Ai.Rulebased.PlayBonusBySystem["forecheck212"]["breakoutUpWall"]);

        JsonArray placeholders = JsonNode.Parse(File.ReadAllText(Path.Combine(dataDir, "tuning.json")))!["_placeholders"]!.AsArray();
        string[] paths = placeholders.Select(p => p!.GetValue<string>()).ToArray();
        Assert.Contains("ai.rulebased.modes", paths);
        Assert.Contains("ai.rulebased.playBonusBySystem", paths);
        Assert.Contains("ai.random.onTheFlyChangeProbability", paths);
        Assert.DoesNotContain("ai.adaptive.passiveSystem", paths);
        Assert.DoesNotContain("ai.adaptive.aggressiveSystem", paths);
        Assert.DoesNotContain("time.timeoutsPerTeamPerMatch", paths);
        Assert.DoesNotContain("time.timeoutRestSeconds", paths);
    }

    [Fact]
    public void MissingAiSection_IsRejected()
    {
        AssertError(root => root.Remove("ai"), "ai: missing");
    }

    [Fact]
    public void MissingTimeoutKeys_AreRejected()
    {
        AssertError(root => root["time"]!.AsObject().Remove("timeoutsPerTeamPerMatch"), "time.timeoutsPerTeamPerMatch: missing");
        AssertError(root => root["time"]!.AsObject().Remove("timeoutRestSeconds"), "time.timeoutRestSeconds: missing");
    }

    [Fact]
    public void BadTimeoutValues_AreRejected()
    {
        AssertError(root => root["time"]!["timeoutsPerTeamPerMatch"] = -1, "time.timeoutsPerTeamPerMatch:");
        AssertError(root => root["time"]!["timeoutsPerTeamPerMatch"] = 1.5, "time.timeoutsPerTeamPerMatch: expected an integer");
        AssertError(root => root["time"]!["timeoutRestSeconds"] = 0, "time.timeoutRestSeconds:");
    }

    [Fact]
    public void UnknownAndMissingAiKeys_AreRejected()
    {
        AssertError(root => Ai(root)["random"]!["extra"] = 1, "ai.random.extra: unknown key");
        AssertError(root => Ai(root).Remove("estimate"), "ai.estimate: missing");
        AssertError(root => Rule(root)["timeout"]!.AsObject().Remove("tiredEnergy"), "ai.rulebased.timeout.tiredEnergy: missing");
        AssertError(root => Rule(root)["modes"]!.AsObject().Remove("protect"), "ai.rulebased.modes.protect: missing");
        AssertError(root => Rule(root)["modes"]!["calm"] = Rule(root)["modes"]!["normal"]!.DeepClone(), "ai.rulebased.modes.calm: unknown mode");
        AssertError(root => Rule(root)["modes"]!["normal"]!["speed"] = 1, "ai.rulebased.modes.normal.speed: unknown key");
    }

    [Theory]
    [InlineData("onTheFlyChangeProbability", 1.5)]
    [InlineData("timeoutProbabilityPerStoppage", -0.1)]
    public void RandomProbabilities_OutsideUnitInterval_AreRejected(string key, double value)
    {
        AssertError(root => Ai(root)["random"]![key] = value, "ai.random." + key + ": must be in [0, 1]");
    }

    [Fact]
    public void RulebasedValueRanges_AreChecked()
    {
        AssertError(root => Ai(root)["estimate"]!["extraDrainPerSecondOnIce"] = -0.1, "ai.estimate.extraDrainPerSecondOnIce:");
        AssertError(root => Rule(root)["changeEnergyThreshold"] = 1.1, "ai.rulebased.changeEnergyThreshold:");
        AssertError(root => Rule(root)["freshnessMargin"] = -0.1, "ai.rulebased.freshnessMargin:");
        AssertError(root => Rule(root)["lateGameSeconds"] = 1201, "ai.rulebased.lateGameSeconds:");
        AssertError(root => Rule(root)["lateGameSeconds"] = 0, "ai.rulebased.lateGameSeconds:");
        AssertError(root => Rule(root)["protectLeadGoals"] = 0, "ai.rulebased.protectLeadGoals:");
        AssertError(root => Rule(root)["chaseDeficitGoals"] = 1.5, "ai.rulebased.chaseDeficitGoals: expected an integer");
        AssertError(root => Rule(root)["modes"]!["normal"]!["transition"] = "dump", "ai.rulebased.modes.normal.transition:");
        AssertError(root => Rule(root)["modes"]!["chase"]!["looseChasers"] = 3, "ai.rulebased.modes.chase.looseChasers:");
        AssertError(root => Rule(root)["modes"]!["chase"]!["pinch"] = 1, "ai.rulebased.modes.chase.pinch: expected true or false");
        AssertError(root => Rule(root)["playBonusBySystem"]!["trap122"]!["dumpAndChase"] = 2.5, "ai.rulebased.playBonusBySystem.trap122.dumpAndChase:");
        AssertError(root => Rule(root)["timeout"]!["minDeficitGoals"] = 0, "ai.rulebased.timeout.minDeficitGoals:");
        AssertError(root => Rule(root)["timeout"]!["maxDeficitGoals"] = 0, "ai.rulebased.timeout.maxDeficitGoals:");
        AssertError(root => Rule(root)["timeout"]!["lateGameSeconds"] = 5000, "ai.rulebased.timeout.lateGameSeconds:");
    }

    [Fact]
    public void AdaptiveValueRanges_AreChecked()
    {
        AssertError(root => Ai(root)["adaptive"]!["windowSeconds"] = 0, "ai.adaptive.windowSeconds:");
        AssertError(root => Ai(root)["adaptive"]!["priorWeight"] = -1, "ai.adaptive.priorWeight:");
        AssertError(root => Ai(root)["adaptive"]!["aggressiveSystem"] = "trap122", "ai.adaptive.aggressiveSystem: must differ");
        AssertError(root => Ai(root)["adaptive"]!["controlledEntryShareForAggressive"] = 0.6, "ai.adaptive.controlledEntryShareForAggressive: must be below");
        AssertError(root => Ai(root)["adaptive"]!["controlledEntryShareForPassive"] = 1.2, "ai.adaptive.controlledEntryShareForPassive:");
    }

    [Fact]
    public void References_AreCheckedAgainstLoadedSystemsAndPlays()
    {
        AiConfig ai = Load(root =>
        {
            Rule(root)["modes"]!["protect"]!["system"] = "nope";
            Rule(root)["playBonusBySystem"]!["trap122"]!["ghostPlay"] = 0.1;
            Rule(root)["playBonusBySystem"]!["zone"] = new JsonObject();
            Ai(root)["adaptive"]!["passiveSystem"] = "box";
        }).Value!.Ai;

        IReadOnlyList<string> errors = AiConfigValidator.ValidateReferences(ai, new[] { "forecheck212", "trap122" }, new[] { "breakoutReverse", "dumpAndChase" });

        Assert.Contains(errors, e => e.StartsWith("ai.rulebased.modes.protect.system: unknown system nope", StringComparison.Ordinal));
        Assert.Contains("ai.rulebased.playBonusBySystem.trap122.ghostPlay: unknown play ghostPlay", errors);
        Assert.Contains("ai.rulebased.playBonusBySystem.zone: unknown system zone", errors);
        Assert.Contains(errors, e => e.StartsWith("ai.adaptive.passiveSystem: unknown system box", StringComparison.Ordinal));
        Assert.Empty(AiConfigValidator.ValidateReferences(Load().Value!.Ai, new[] { "forecheck212", "trap122" }, new[] { "breakoutReverse", "dumpAndChase" }));
    }
}
