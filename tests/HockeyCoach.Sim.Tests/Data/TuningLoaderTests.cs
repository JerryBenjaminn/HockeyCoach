using System.Text.Json.Nodes;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Data;

public class TuningLoaderTests
{
    private static LoadResult<TuningConfig> Load(Action<JsonObject>? edit = null, IReadOnlyList<string>? xgZones = null)
    {
        string json = edit == null ? Fixtures.Read("tuning-small.json") : Fixtures.Edit("tuning-small.json", edit);
        return TuningLoader.Parse(json, xgZones ?? Fixtures.SmallRinkXgZones);
    }

    private static JsonObject Check(JsonObject root, string id) => root["checks"]![id]!.AsObject();

    [Fact]
    public void ValidFixture_LoadsTypedConfig()
    {
        LoadResult<TuningConfig> result = Load();

        Assert.Empty(result.Errors);
        TuningConfig tuning = result.Value!;
        Assert.Equal(0.15, tuning.CheckFormula.K);
        Assert.Equal(10.5, tuning.CheckFormula.ReferenceValue);
        Assert.Equal(-0.3, tuning.Positions.OffSideCheckModifier);
        Assert.Equal(new[] { "A", "B" }, tuning.Stats.Grades.Select(g => g.Name));
        Assert.Equal(new[] { "block", "breakout", "dumpIn", "pass", "rebound" }, tuning.Checks.Keys);
    }

    [Fact]
    public void TwoSidedCheck_MapsWeightsInRoleAndStatOrder()
    {
        CheckDefinition pass = Load().Value!.GetCheck("pass");

        Assert.Equal(CheckKind.TwoSided, pass.Kind);
        Assert.Equal(0.85, pass.P0);
        Assert.Equal(new[] { "passer", "receiver" }, pass.Attacker.Select(t => t.Participant));
        Assert.Equal(StatRef.Of(SkaterStat.Passing), pass.Attacker[0].Stat);
        Assert.Equal(new[] { StatRef.Of(SkaterStat.Awareness), StatRef.Of(SkaterStat.Positioning) }, pass.Defender.Select(t => t.Stat));
        Assert.Equal(-0.4, pass.Modifiers.Get("crossIce"));
        Assert.Equal(0.0, pass.Modifiers.GetTable("laneDefenderDistance", 5));
    }

    [Fact]
    public void OneSidedCheck_MapsSideAndGoalieStats()
    {
        CheckDefinition rebound = Load().Value!.GetCheck("rebound");

        Assert.Equal(CheckKind.OneSided, rebound.Kind);
        Assert.Equal(CheckSide.Defender, rebound.Side);
        Assert.Empty(rebound.Attacker);
        Assert.Equal(StatRef.Of(GoalieStat.ReboundControl), rebound.Defender.Single().Stat);
        Assert.Equal(0.6, rebound.GetParameter("controlledHoldShare"));
    }

    [Fact]
    public void NoCheck_KeepsItsNumbersAsParameters()
    {
        CheckDefinition dumpIn = Load().Value!.GetCheck("dumpIn");

        Assert.Equal(CheckKind.NoCheck, dumpIn.Kind);
        Assert.Null(dumpIn.P0);
        Assert.Equal(0.03, dumpIn.GetParameter("goaliePuckHandlingPerPoint"));
    }

    [Fact]
    public void Shot_MapsZonesAndOwnBounds()
    {
        ShotConfig shot = Load().Value!.Shot;

        Assert.Equal(0.001, shot.MinProbability);
        Assert.Equal(0.6, shot.MaxProbability);
        Assert.Equal(0.55, shot.OnTargetShare);
        CheckDefinition slot = shot.ForXgZone("slot");
        Assert.Equal(0.15, slot.P0);
        Assert.Equal(0.8, slot.Attacker.Single(t => t.Stat.Equals(StatRef.Of(SkaterStat.ShotAccuracy))).Weight);
        Assert.Equal(StatRef.Of(GoalieStat.Positioning), shot.Defender.Single(t => t.Weight == 0.6).Stat);
    }

    [Fact]
    public void LaterSections_DoNotFailLoading_ButUnknownSectionsDo()
    {
        LoadResult<TuningConfig> unknown = Load(r => r["tiem"] = new JsonObject());

        Assert.Contains("tiem: unknown key", unknown.Errors);
    }

    [Fact]
    public void MissingKind_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "pass").Remove("kind"));

        Assert.Contains("checks.pass.kind: missing", result.Errors);
    }

    [Fact]
    public void UnknownKind_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "pass")["kind"] = "threeSided");

        Assert.Contains(result.Errors, e => e.StartsWith("checks.pass.kind: unknown kind threeSided", StringComparison.Ordinal));
    }

    [Fact]
    public void TwoSidedWithoutDefender_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "pass").Remove("defender"));

        Assert.Contains("checks.pass.defender: missing (twoSided check)", result.Errors);
    }

    [Fact]
    public void TwoSidedWithSide_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "pass")["side"] = "attacker");

        Assert.Contains("checks.pass.side: only oneSided checks have a side", result.Errors);
    }

    [Fact]
    public void OneSidedWithoutSide_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "block").Remove("side"));

        Assert.Contains("checks.block.side: missing (oneSided check needs side attacker or defender)", result.Errors);
    }

    [Fact]
    public void OneSidedWithOtherSideWritten_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "block")["attacker"] = JsonNode.Parse("{\"shooter\": {\"shotPower\": 1.0}}"));

        Assert.Contains("checks.block.attacker: must not be written in a oneSided check with side defender", result.Errors);
    }

    [Fact]
    public void OneSidedWithPresentSideMissing_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "block").Remove("defender"));

        Assert.Contains("checks.block.defender: missing (oneSided check with side defender)", result.Errors);
    }

    [Fact]
    public void NoCheckWithP0_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "dumpIn")["p0"] = 0.5);

        Assert.Contains("checks.dumpIn.p0: noCheck must not have p0", result.Errors);
    }

    [Fact]
    public void WeightsNotSummingToOne_AreReportedWithPath()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "pass")["attacker"]!["receiver"]!["hands"] = 0.3);

        Assert.Contains(result.Errors, e => e.StartsWith("checks.pass.attacker: weights must sum to 1, sum was 0.8999", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownStatName_IsReportedWithPath()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "pass")["attacker"]!["passer"] = JsonNode.Parse("{\"pasing\": 0.6}"));

        Assert.Contains("checks.pass.attacker.passer.pasing: unknown skater stat", result.Errors);
    }

    [Fact]
    public void SkaterStatOnGoalieRole_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "rebound")["defender"] = JsonNode.Parse("{\"goalie\": {\"strength\": 1.0}}"));

        Assert.Contains("checks.rebound.defender.goalie.strength: unknown goalie stat", result.Errors);
    }

    [Fact]
    public void UnknownRole_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "pass")["defender"] = JsonNode.Parse("{\"winger\": {\"speed\": 1.0}}"));

        Assert.Contains(result.Errors, e => e.StartsWith("checks.pass.defender.winger: unknown participant role", StringComparison.Ordinal));
    }

    [Fact]
    public void ShareOutsideUnitInterval_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "rebound")["controlledHoldShare"] = 1.5);

        Assert.Contains("checks.rebound.controlledHoldShare: share must be in (0, 1), was 1.5", result.Errors);
    }

    [Fact]
    public void NonNumericExtraCheckField_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "pass")["note"] = "text");

        Assert.Contains("checks.pass.note: unknown key (extra check fields must be numbers)", result.Errors);
    }

    [Fact]
    public void ShotZonesNotMatchingRink_AreReported()
    {
        LoadResult<TuningConfig> result = Load(xgZones: new[] { "slot", "longRange", "crease" });

        Assert.Contains("checks.shot.baseXg: missing xG zone crease (rink.json xgZones)", result.Errors);
        Assert.Contains("checks.shot.attackerByXgZone: missing xG zone crease (rink.json xgZones)", result.Errors);
    }

    [Fact]
    public void ShotWithP0_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "shot")["p0"] = 0.1);

        Assert.Contains(result.Errors, e => e.StartsWith("checks.shot.p0: shot has no p0", StringComparison.Ordinal));
    }

    [Fact]
    public void ShotBoundsReversed_AreReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "shot")["minProbability"] = 0.7);

        Assert.Contains("checks.shot: minProbability (0.7) must be less than maxProbability (0.6)", result.Errors);
    }

    [Fact]
    public void MissingShot_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => r["checks"]!.AsObject().Remove("shot"));

        Assert.Contains("checks.shot: missing", result.Errors);
    }

    [Fact]
    public void ReferenceValueOutsideStatScale_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => r["checkFormula"]!["referenceValue"] = 25);

        Assert.Contains(result.Errors, e => e.StartsWith("checkFormula.referenceValue: must be within stats.min..stats.max", StringComparison.Ordinal));
    }

    [Fact]
    public void GradeGap_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => r["stats"]!["grades"]!["A"] = new JsonArray(12, 20));

        Assert.Contains(result.Errors, e => e.StartsWith("stats.grades.A: starts at 12", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingOffSideModifier_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => r["positions"]!.AsObject().Remove("offSideCheckModifier"));

        Assert.Contains("positions.offSideCheckModifier: missing", result.Errors);
    }

    [Fact]
    public void DuplicateKey_IsReported()
    {
        string json = Fixtures.Read("tuning-small.json").Replace("\"k\": 0.15,", "\"k\": 0.15, \"k\": 0.2,", StringComparison.Ordinal);

        LoadResult<TuningConfig> result = TuningLoader.Parse(json, Fixtures.SmallRinkXgZones);

        Assert.Contains("checkFormula.k: duplicate key", result.Errors);
    }

    [Fact]
    public void BaseXgAboveShotMaximum_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => Check(r, "shot")["baseXg"]!["slot"] = 0.7);

        Assert.Contains(result.Errors, e => e.StartsWith("checks.shot.baseXg.slot: must not exceed checks.shot.maxProbability", StringComparison.Ordinal));
    }

    [Fact]
    public void AllowedTopLevelSections_MatchDataSchema()
    {
        // docs/data-schema.md, tuning.json: "Sallitut ylimmän tason avaimet" (D-030).
        string[] allowed =
        {
            "schemaVersion", "stats", "checkFormula", "checks", "positions", "time", "energy", "organization",
            "pressure", "form", "chemistry", "familiarity", "plays", "chanceTypes", "chanceClasses", "loosePuckSpots", "transitions",
        };

        LoadResult<TuningConfig> result = Load(r =>
        {
            foreach (string section in allowed.Where(s => r[s] == null))
            {
                r[section] = new JsonObject();
            }
        });

        Assert.Empty(result.Errors);
    }
}
