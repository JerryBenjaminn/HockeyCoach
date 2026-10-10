using System.Text.Json.Nodes;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Data;

/// <summary>Typed tuning sections of milestone 2: time, plays, chanceTypes, chanceClasses.</summary>
public class TuningSectionsLoaderTests
{
    private static LoadResult<TuningConfig> Load(Action<JsonObject>? edit = null)
    {
        string json = edit == null ? Fixtures.Read("tuning-small.json") : Fixtures.Edit("tuning-small.json", edit);
        return TuningLoader.Parse(json, Fixtures.SmallRinkXgZones);
    }

    [Fact]
    public void ValidFixture_MapsTimeSection()
    {
        TimeConfig time = Load().Value!.Time;

        Assert.Equal(3, time.Periods);
        Assert.Equal(1200.0, time.PeriodSeconds);
        Assert.Equal(45.0, time.ForwardShiftSeconds);
        Assert.Equal(50.0, time.DefenceShiftSeconds);
        Assert.Equal(6.0, time.SetupSeconds);
        Assert.Equal(8.0, time.RegroupSeconds);
        Assert.Equal(3.0, time.GetSecondsPerAction("skate"));
        Assert.Equal(time.SecondsPerAction.Keys.OrderBy(k => k, StringComparer.Ordinal), time.SecondsPerAction.Keys);
        Assert.Throws<KeyNotFoundException>(() => time.GetSecondsPerAction("teleport"));
    }

    [Fact]
    public void ValidFixture_MapsPlaysAndChanceSections()
    {
        TuningConfig tuning = Load().Value!;

        Assert.Equal(4, tuning.Plays.MaxBeats);
        Assert.Equal(2, tuning.Plays.MaxNodesPerBeat);
        Assert.Equal(8.0, tuning.ChanceTypes.TurnoverWindowSeconds);
        Assert.Equal(0.15, tuning.ChanceClasses.TopMinXg);
        Assert.Equal(0.07, tuning.ChanceClasses.GoodMinXg);
        Assert.Equal(0.03, tuning.ChanceClasses.ModerateMinXg);
    }

    [Theory]
    [InlineData("time")]
    [InlineData("plays")]
    [InlineData("chanceTypes")]
    [InlineData("chanceClasses")]
    public void MissingSection_IsReported(string section)
    {
        LoadResult<TuningConfig> result = Load(r => r.Remove(section));

        Assert.Contains(section + ": missing", result.Errors);
    }

    [Fact]
    public void UnknownKeyInMappedSection_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => r["plays"]!["maxBeat"] = 4);

        Assert.Contains("plays.maxBeat: unknown key", result.Errors);
    }

    [Fact]
    public void MissingPlayActionTime_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => r["time"]!["secondsPerAction"]!.AsObject().Remove("driveNet"));

        Assert.Contains("time.secondsPerAction.driveNet: missing (used by the play actions)", result.Errors);
    }

    [Fact]
    public void NonIntegerMaxBeats_IsReported()
    {
        LoadResult<TuningConfig> result = Load(r => r["plays"]!["maxBeats"] = 2.5);

        Assert.Contains("plays.maxBeats: expected an integer, got a number", result.Errors);
    }

    [Fact]
    public void RepositoryTuning_HasAllPlayActionTimes()
    {
        TuningConfig tuning = GameData.Load(Path.Combine(Fixtures.RepoRoot(), "data")).Tuning;

        Assert.All(TimeConfig.PlayActionKeys, key => Assert.True(tuning.Time.GetSecondsPerAction(key) >= 0.0));
    }
}

public class TuningLoosePuckAndPressureTests
{
    private static LoadResult<TuningConfig> Load(Action<System.Text.Json.Nodes.JsonObject> edit)
    {
        return TuningLoader.Parse(Fixtures.Edit("tuning-small.json", edit), Fixtures.SmallRinkXgZones);
    }

    [Fact]
    public void LoosePuckSpotsAndPressure_AreMapped()
    {
        TuningConfig tuning = TuningLoader.Parse(Fixtures.Read("tuning-small.json"), Fixtures.SmallRinkXgZones).Value!;

        Assert.Equal(1, tuning.Pressure.UnderPressureNodes);
        Assert.Equal(LoosePuckRule.NetFront, tuning.LoosePuckSpots.ReboundSlot);
        Assert.Equal(LoosePuckRule.ShooterSideCorner, tuning.LoosePuckSpots.ReboundCorner);
        Assert.Equal(LoosePuckRule.EndRowShooterLane, tuning.LoosePuckSpots.MissedShot);
        Assert.Equal(LoosePuckRule.BlockerNode, tuning.LoosePuckSpots.BlockedShot);
        Assert.Equal(LoosePuckRule.LaneDefenderNode, tuning.LoosePuckSpots.FailedPass);
    }

    [Fact]
    public void UnknownMissingAndBadRules_AreRejected()
    {
        Assert.Contains("loosePuckSpots.offside: unknown key", Load(r => r["loosePuckSpots"]!["offside"] = "netFront").Errors);
        Assert.Contains("loosePuckSpots.failedPass: missing", Load(r => r["loosePuckSpots"]!.AsObject().Remove("failedPass")).Errors);
        Assert.Contains(Load(r => r["loosePuckSpots"]!["missedShot"] = "corner").Errors, e => e.StartsWith("loosePuckSpots.missedShot: unknown rule corner", StringComparison.Ordinal));
        Assert.Contains(Load(r => r["loosePuckSpots"]!["missedShot"] = "blockerNode").Errors, e => e.StartsWith("loosePuckSpots.missedShot: rule BlockerNode has no reference node", StringComparison.Ordinal));
    }

    [Fact]
    public void PressureSection_RequiresEveryKey()
    {
        Assert.Contains("pressure.underPressureNodes: missing", Load(r => r["pressure"]!.AsObject().Remove("underPressureNodes")).Errors);
        Assert.Contains("pressure.radius: unknown key", Load(r => r["pressure"]!["radius"] = 1).Errors);
        Assert.Contains("pressure.underPressureNodes: must be >= 0, was -1", Load(r => r["pressure"]!["underPressureNodes"] = -1).Errors);
    }

    [Fact]
    public void RepositoryTuning_RenamedPassPressureModifier()
    {
        TuningConfig tuning = GameData.Load(Path.Combine(Fixtures.RepoRoot(), "data")).Tuning;

        Assert.Contains("underPressure", tuning.GetCheck("pass").Modifiers.Scalars.Keys);
        Assert.DoesNotContain("pressure", tuning.GetCheck("pass").Modifiers.Scalars.Keys);
    }
}
