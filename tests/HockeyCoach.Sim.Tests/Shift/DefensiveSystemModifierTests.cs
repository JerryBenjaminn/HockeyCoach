using System.Text.Json.Nodes;
using HockeyCoach.AI;
using HockeyCoach.Harness.Data;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Shift;

/// <summary>
/// The defensive-system modifiers of data-schema.md M-9 (D-078 K4): the pure formulas, and whole matches where an
/// extreme modifier value pushes every affected check to the clamp so its scope can be read from the observed probabilities.
/// </summary>
public class DefensiveSystemModifierTests
{
    /// <summary>A logit large enough to push any check to the D-014 clamp.</summary>
    private const double Extreme = 50.0;

    private sealed class Recorder : ICheckObserver
    {
        public List<CheckObservation> Checks { get; } = new();

        public void OnCheck(CheckObservation observation) => Checks.Add(observation);
    }

    // ------------------------------------------------------------------ pure formulas

    [Theory]
    [InlineData(3, 2, 0.25)]
    [InlineData(2, 2, 0.0)]
    [InlineData(2, 3, -0.25)]
    [InlineData(2, 5, -0.75)]
    [InlineData(5, 0, 1.25)]
    [InlineData(1, 5, -1.0)]
    public void StrengthDifference_IsPerPlayerTimesNMinusM(int attackers, int defenders, double expected)
    {
        Assert.Equal(expected, DefensiveSystemModifiers.StrengthDifference(0.25, attackers, defenders), 12);
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(1, -0.25)]
    [InlineData(2, -0.5)]
    [InlineData(3, -0.75)]
    public void Forecheckers_IsPerForecheckerTimesF(int f, double expected)
    {
        Assert.Equal(expected, DefensiveSystemModifiers.Forecheckers(-0.25, f), 12);
    }

    [Fact]
    public void Modifiers_GiveTheSpecExampleProbabilities_AtEqualRatings()
    {
        var formula = ShiftTestData.Data.Tuning.CheckFormula;
        double Entry(int n, int m) => CheckFormula.Probability(formula, 0.6, 10.0, 10.0, DefensiveSystemModifiers.StrengthDifference(0.25, n, m));
        double Breakout(int f) => CheckFormula.Probability(formula, 0.7, 10.0, 10.0, DefensiveSystemModifiers.Forecheckers(-0.25, f));

        // M-9: 3 vs 2, 2 vs 2, 2 vs 3, 2 vs 5 → 66 %, 60 %, 54 %, 41 %; f = 0..3 → 70 %, 64 %, 59 %, 52 %.
        // The spec's percentages are rounded to whole points (f = 1 is 64.5 %), hence the 1-point tolerance.
        double[] entries = { Entry(3, 2), Entry(2, 2), Entry(2, 3), Entry(2, 5) };
        double[] breakouts = { Breakout(0), Breakout(1), Breakout(2), Breakout(3) };
        double[] expectedEntries = { 0.66, 0.60, 0.54, 0.41 };
        double[] expectedBreakouts = { 0.70, 0.64, 0.59, 0.52 };
        for (int i = 0; i < 4; i++)
        {
            Assert.InRange(entries[i], expectedEntries[i] - 0.01, expectedEntries[i] + 0.01);
            Assert.InRange(breakouts[i], expectedBreakouts[i] - 0.01, expectedBreakouts[i] + 0.01);
        }

        Assert.True(entries[0] > entries[1] && entries[1] > entries[2] && entries[2] > entries[3]);
        Assert.True(breakouts[0] > breakouts[1] && breakouts[1] > breakouts[2] && breakouts[2] > breakouts[3]);
    }

    [Fact]
    public void ZeroCoefficients_HaveNoEffect()
    {
        Assert.Equal(0.0, DefensiveSystemModifiers.StrengthDifference(0.0, 1, 5));
        Assert.Equal(0.0, DefensiveSystemModifiers.Forecheckers(0.0, 3));
    }

    // ------------------------------------------------------------------ whole matches

    private static GameData DataWith(double strengthPerPlayer, double perForechecker) => RepositoryTuning.Load(r =>
    {
        r["checks"]!["zoneEntryCarry"]!["modifiers"]!["strengthDifferencePerPlayer"] = strengthPerPlayer;
        r["checks"]!["breakout"]!["modifiers"]!["perForechecker"] = perForechecker;
    });

    private static readonly Lazy<GameData> ExtremeData = new(() => DataWith(Extreme, -Extreme));

    /// <summary>Matches with both transitions and both systems on each side, so own-zone regroups and rushes both happen.</summary>
    private static List<(MatchResult Result, Recorder Checks)> Play(GameData data, int seeds)
    {
        MatchSetup setup = MatchCommand.Setup(data);
        var played = new List<(MatchResult, Recorder)>();
        foreach ((string home, TransitionInstruction homeT, string away, TransitionInstruction awayT) in new[]
        {
            ("forecheck212", TransitionInstruction.Regroup, "trap122", TransitionInstruction.Rush),
            ("trap122", TransitionInstruction.Rush, "forecheck212", TransitionInstruction.Regroup),
        })
        {
            for (ulong seed = 1; seed <= (ulong)seeds; seed++)
            {
                var recorder = new Recorder();
                MatchResult result = MatchSimulator.Run(
                    setup,
                    new RotationCoach(data.Plays, HarnessTeams.System(data, home), homeT),
                    new RotationCoach(data.Plays, HarnessTeams.System(data, away), awayT),
                    new MatchStreams(seed).Simulation(),
                    recorder);
                played.Add((result, recorder));
            }
        }

        return played;
    }

    private static readonly Lazy<List<(MatchResult Result, Recorder Checks)>> ExtremeMatches = new(() => Play(ExtremeData.Value, 3));

    [Fact]
    public void Carry_IsPushedByTheLoggedStrength_EveryCarryEntryCheckInLogOrder()
    {
        var formula = ExtremeData.Value.Tuning.CheckFormula;
        int ahead = 0;
        int behind = 0;
        int even = 0;
        foreach ((MatchResult result, Recorder recorder) in ExtremeMatches.Value)
        {
            List<CheckObservation> checks = recorder.Checks.Where(c => c.CheckId == "zoneEntryCarry").ToList();
            List<ControlledZoneEntryEvent> carries = result.Log.Events.OfType<ControlledZoneEntryEvent>().Where(e => e.Method == ZoneEntryMethod.Carry).ToList();
            Assert.Equal(carries.Count, checks.Count);
            for (int i = 0; i < checks.Count; i++)
            {
                Assert.Equal(carries[i].Outcome == ZoneEntryOutcome.Kept, checks[i].Success);
                int diff = carries[i].Numbers.Attackers - carries[i].Numbers.Defenders;
                if (diff > 0)
                {
                    Assert.Equal(formula.MaxProbability, checks[i].Probability, 12);
                    ahead++;
                }
                else if (diff < 0)
                {
                    Assert.Equal(formula.MinProbability, checks[i].Probability, 12);
                    behind++;
                }
                else
                {
                    Assert.InRange(checks[i].Probability, formula.MinProbability + 1e-9, formula.MaxProbability - 1e-9);
                    even++;
                }

                Assert.Null(checks[i].Forecheckers);
            }
        }

        // Carries with N > M are rare in the repository data; the clamp check above covers them whenever they occur.
        Assert.True(behind > 0 && even > 0, "carries ahead / behind / even: " + ahead + " / " + behind + " / " + even);
    }

    [Fact]
    public void PassEntries_AreNotAffected_Q063()
    {
        var formula = ExtremeData.Value.Tuning.CheckFormula;
        int passEntriesBehind = 0;
        foreach ((MatchResult result, Recorder recorder) in ExtremeMatches.Value)
        {
            passEntriesBehind += result.Log.Events.OfType<ControlledZoneEntryEvent>()
                .Count(e => e.Method == ZoneEntryMethod.Pass && e.Numbers.Attackers < e.Numbers.Defenders);
            Assert.DoesNotContain(recorder.Checks, c => c.CheckId == "pass" && Math.Abs(c.Probability - formula.MinProbability) < 1e-12);
        }

        Assert.True(passEntriesBehind > 0);
    }

    [Fact]
    public void Breakout_IsPushedByTheForecheckerCount_InEveryBreakoutCheck()
    {
        var formula = ExtremeData.Value.Tuning.CheckFormula;
        var seen = new int[4];
        foreach ((MatchResult unused, Recorder recorder) in ExtremeMatches.Value)
        {
            foreach (CheckObservation c in recorder.Checks.Where(c => c.CheckId == "breakout"))
            {
                Assert.NotNull(c.Forecheckers);
                int f = c.Forecheckers!.Value;
                Assert.InRange(f, 0, 3);
                if (f == 0)
                {
                    Assert.InRange(c.Probability, formula.MinProbability + 1e-9, formula.MaxProbability - 1e-9);
                }
                else
                {
                    Assert.Equal(formula.MinProbability, c.Probability, 12);
                }

                seen[f]++;
            }
        }

        Assert.True(seen[0] > 0 && seen[1] > 0 && seen[2] > 0, "breakouts by f: " + string.Join(" / ", seen));
    }

    [Fact]
    public void OtherChecks_CarryNoForecheckerCount()
    {
        foreach ((MatchResult unused, Recorder recorder) in ExtremeMatches.Value)
        {
            Assert.All(recorder.Checks.Where(c => c.CheckId != "breakout"), c => Assert.Null(c.Forecheckers));
        }
    }

    [Fact]
    public void RepositoryValues_ChangeTheMatch_ComparedToZeroCoefficients()
    {
        GameData zero = DataWith(0.0, 0.0);
        MatchSetup setup = MatchCommand.Setup(zero);
        string Run(GameData data) => MatchJson.Write(MatchCommand.Play(data, MatchCommand.Setup(data), "forecheck212", "trap122", 11UL), setup, 11UL);

        Assert.NotEqual(Run(zero), Run(ShiftTestData.Data));
    }

    [Theory]
    [InlineData(42UL)]
    [InlineData(7UL)]
    public void SameSeed_ProducesIdenticalEventLogs_WithTheModifiers(ulong seed)
    {
        GameData data = ShiftTestData.Data;
        MatchSetup setup = MatchCommand.Setup(data);
        string Run() => MatchJson.Write(MatchCommand.Play(data, setup, "forecheck212", "trap122", seed), setup, seed);

        Assert.Equal(Run(), Run());
    }
}
