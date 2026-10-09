using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tests.Support;

/// <summary>Hand-built check definitions for tests. Values mirror docs/stats-and-checks.md; they are test inputs, not tuning.</summary>
internal static class TestChecks
{
    public static CheckFormulaConfig Formula(double k = 0.15, double min = 0.02, double max = 0.98)
    {
        return new CheckFormulaConfig(k, min, max);
    }

    /// <summary>Pass: passer passing 0.6 + receiver hands 0.4 vs nearest defender awareness 0.5 + positioning 0.5.</summary>
    public static CheckDefinition Pass(double p0 = 0.85)
    {
        return new CheckDefinition(
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

    /// <summary>Goal check against the goalie: shooter accuracy/power vs goalie positioning 0.6 + reflexes 0.4.</summary>
    public static CheckDefinition SlotShot(double p0 = 0.15)
    {
        return new CheckDefinition(
            "shot.slot",
            p0,
            new[]
            {
                new WeightTerm("shooter", StatRef.Of(SkaterStat.ShotAccuracy), 0.8),
                new WeightTerm("shooter", StatRef.Of(SkaterStat.ShotPower), 0.2),
            },
            new[]
            {
                new WeightTerm("goalie", StatRef.Of(GoalieStat.Positioning), 0.6),
                new WeightTerm("goalie", StatRef.Of(GoalieStat.Reflexes), 0.4),
            });
    }

    public static CheckParticipants PassParticipants(int passer, int receiver, int defender)
    {
        return new CheckParticipants()
            .With("passer", TestPlayers.Skater(1, passer))
            .With("receiver", TestPlayers.Skater(2, receiver))
            .With("nearestDefender", TestPlayers.Skater(3, defender));
    }
}
