using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tests.Support;

/// <summary>Hand-built check definitions for tests. Values mirror docs/stats-and-checks.md; they are test inputs, not tuning.</summary>
internal static class TestChecks
{
    public static CheckFormulaConfig Formula(double k = 0.15, double min = 0.02, double max = 0.98, double referenceValue = 10.5)
    {
        return new CheckFormulaConfig(k, min, max, referenceValue);
    }

    /// <summary>Pass: passer passing 0.6 + receiver hands 0.4 vs nearest defender awareness 0.5 + positioning 0.5.</summary>
    public static CheckDefinition Pass(double p0 = 0.85)
    {
        return CheckDefinition.TwoSided(
            "pass",
            p0,
            new[]
            {
                new WeightTerm("passer", StatRef.Of(SkaterStat.Passing), 0.6),
                new WeightTerm("receiver", StatRef.Of(SkaterStat.Hands), 0.4),
            },
            new[]
            {
                new WeightTerm("nearestDefender", StatRef.Of(SkaterStat.Awareness), 0.5),
                new WeightTerm("nearestDefender", StatRef.Of(SkaterStat.Positioning), 0.5),
            });
    }

    /// <summary>Breakout: carrier passing/awareness vs forecheckers speed/awareness (mean over forecheckers).</summary>
    public static CheckDefinition Breakout(double p0 = 0.7)
    {
        return CheckDefinition.TwoSided(
            "breakout",
            p0,
            new[]
            {
                new WeightTerm("carrier", StatRef.Of(SkaterStat.Passing), 0.5),
                new WeightTerm("carrier", StatRef.Of(SkaterStat.Awareness), 0.5),
            },
            new[]
            {
                new WeightTerm("forecheckers", StatRef.Of(SkaterStat.Speed), 0.5),
                new WeightTerm("forecheckers", StatRef.Of(SkaterStat.Awareness), 0.5),
            });
    }

    /// <summary>Block: one-sided, defender side nearestDefender positioning 1.0. Success = shot gets through.</summary>
    public static CheckDefinition Block(double p0 = 0.75)
    {
        return CheckDefinition.OneSided("block", p0, CheckSide.Defender, new[] { new WeightTerm("nearestDefender", StatRef.Of(SkaterStat.Positioning), 1.0) });
    }

    /// <summary>Shot config with two xG zones, shot bounds 0.001..0.6.</summary>
    public static ShotConfig Shot(double slotXg = 0.15, double longRangeXg = 0.005, double min = 0.001, double max = 0.6)
    {
        var slot = new[]
        {
            new WeightTerm("shooter", StatRef.Of(SkaterStat.ShotAccuracy), 0.8),
            new WeightTerm("shooter", StatRef.Of(SkaterStat.ShotPower), 0.2),
        };
        var longRange = new[]
        {
            new WeightTerm("shooter", StatRef.Of(SkaterStat.ShotAccuracy), 0.3),
            new WeightTerm("shooter", StatRef.Of(SkaterStat.ShotPower), 0.7),
        };
        var goalie = new[]
        {
            new WeightTerm("goalie", StatRef.Of(GoalieStat.Positioning), 0.6),
            new WeightTerm("goalie", StatRef.Of(GoalieStat.Reflexes), 0.4),
        };
        return new ShotConfig(
            new Dictionary<string, IReadOnlyList<WeightTerm>> { ["slot"] = slot, ["longRange"] = longRange },
            goalie,
            new Dictionary<string, double> { ["slot"] = slotXg, ["longRange"] = longRangeXg },
            0.55,
            min,
            max);
    }

    public static CheckParticipants PassParticipants(int passer, int receiver, int defender)
    {
        return new CheckParticipants()
            .With("passer", TestPlayers.Skater(1, passer))
            .With("receiver", TestPlayers.Skater(2, receiver))
            .With("nearestDefender", TestPlayers.Skater(3, defender));
    }

    public static CheckParticipants ShotParticipants(int shooter, int goalie)
    {
        return new CheckParticipants()
            .With("shooter", TestPlayers.Skater(1, shooter))
            .With("goalie", TestPlayers.Goalie(2, goalie));
    }
}
