using HockeyCoach.AI;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Shift;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>Rule-based and adaptive AIs (data-schema.md AI-1, AI-2 S-L, S-T, A-T, AI-4 triggers).</summary>
public class RuleCoachTests
{
    private static readonly TuningConfig Tuning = ShiftTestData.Data.Tuning;
    private static readonly IReadOnlyList<Play> Library = ShiftTestData.Data.Plays;
    private static readonly IReadOnlyList<DefensiveSystem> Systems = ShiftTestData.Data.Systems;

    private static readonly Team Team = TestPlayers.Team("Home", 1, allStats: 10, trios: 4, pairs: 3);

    private static double Factor => Sim.Checks.StateDynamics.ReductionFactor(Tuning.Energy.EnduranceCostReductionPerPoint, 10, Tuning.CheckFormula.ReferenceValue);

    private static double Drain => (Tuning.Energy.DrainPerSecondOnIce + Tuning.Ai.Estimate.ExtraDrainPerSecondOnIce) * Factor;

    private static double Bench(double e, double t) => 1.0 - ((1.0 - e) * Math.Exp(-Tuning.Energy.BenchRecoveryRate * t));

    private static CoachView ViewAt(double time, int trio, int pair, IReadOnlyList<SimEvent> events, bool canF = true, bool canD = true, int period = 1, int own = 0, int opp = 0, CoachTimeouts? timeouts = null) =>
        TestCoachViews.Make(Team, trio, pair, canF, canD, call: CoachCall.Stoppage, events: events, clock: new CoachClock(period, time, 3, 1200.0), ownGoals: own, opponentGoals: opp, timeouts: timeouts);

    [Fact]
    public void EnergyEstimate_ReplaysLineChangesAndTheClock()
    {
        int[] start = TestCoachViews.Unit(Team, 0, 0);
        int[] trio0 = Team.ForwardLines[0].Skaters.Select(s => s.Id).ToArray();
        int[] trio1 = Team.ForwardLines[1].Skaters.Select(s => s.Id).ToArray();
        var events = new List<SimEvent>
        {
            new FaceoffEvent(TestCoachViews.Context(0.0, start), trio0[1], 902, TeamSide.Home, FaceoffLocation.NeutralZone),
            new LineChangeEvent(TestCoachViews.Context(40.0, start), trio0, trio1),
        };

        IReadOnlyDictionary<int, double> e = new EnergyEstimator(Tuning).Estimate(ViewAt(60.0, 1, 0, events));

        double start0 = Tuning.Energy.Start;
        Assert.Equal(Bench(start0 - (Drain * 40.0), 20.0), e[trio0[0]], 12);
        Assert.Equal(start0 - (Drain * 20.0), e[trio1[0]], 12);
        Assert.Equal(start0 - (Drain * 60.0), e[Team.DefencePairs[0].Skaters[0].Id], 12);
        Assert.Equal(start0, e[Team.ForwardLines[3].Skaters[0].Id], 12);
    }

    [Fact]
    public void EnergyEstimate_StartsEachPeriodFresh_AndAppliesTimeouts()
    {
        int[] start = TestCoachViews.Unit(Team, 0, 0);
        var events = new List<SimEvent>
        {
            new FaceoffEvent(TestCoachViews.Context(0.0, start), start[1], 902, TeamSide.Home, FaceoffLocation.NeutralZone),
            new StoppageEvent(TestCoachViews.Context(30.0, start), StoppageReason.GoalieFreeze),
            new TimeoutEvent(TestCoachViews.Context(30.0, start), TeamSide.Away),
        };

        var estimator = new EnergyEstimator(Tuning);
        double rested = Bench(Tuning.Energy.Start - (Drain * 30.0), Tuning.Time.TimeoutRestSeconds);
        Assert.Equal(rested, estimator.Estimate(ViewAt(30.0, 0, 0, events))[start[0]], 12);
        Assert.All(estimator.Estimate(ViewAt(0.0, 0, 0, events, period: 2)).Values, v => Assert.Equal(Tuning.Energy.Start, v));
    }

    [Fact]
    public void RuleLines_Pick_FollowsThresholdMarginAndTies()
    {
        var lines = new RuleLines(Tuning);
        double threshold = Tuning.Ai.Rulebased.ChangeEnergyThreshold;
        double margin = Tuning.Ai.Rulebased.FreshnessMargin;

        Assert.Equal(0, lines.Pick(-1, new[] { 1.0, 1.0, 1.0, 1.0 }));
        Assert.Equal(1, lines.Pick(-1, new[] { 0.9, 1.0, 1.0, 1.0 }));

        // Fresh enough and no better bench unit by the margin: stay.
        Assert.Equal(2, lines.Pick(2, new[] { 1.0, 1.0, threshold + 0.01, 1.0 }.Select((v, i) => i == 2 ? 1.0 - (margin / 2) : 1.0).ToArray()));

        // Tired below the threshold: change to the best bench unit; ties go to the next in rotation order.
        Assert.Equal(3, lines.Pick(2, new[] { 0.9, 0.9, threshold - 0.01, 0.9 }));
        Assert.Equal(0, lines.Pick(3, new[] { 0.9, 0.9, 0.9, threshold - 0.01 }));

        // A bench unit fresher by the margin: change even above the threshold.
        Assert.Equal(1, lines.Pick(0, new[] { 0.9, 0.9 + margin, 0.9, 0.9 }));
    }

    [Fact]
    public void RuleLines_KeepIneligibleUnits_AndStartWithUnitsZero()
    {
        var lines = new RuleLines(Tuning);
        Assert.Equal(0, lines.AtStoppage(ViewAt(0.0, -1, -1, Array.Empty<SimEvent>())).ForwardIndex);
        Assert.Equal(0, lines.AtStoppage(ViewAt(0.0, -1, -1, Array.Empty<SimEvent>())).PairIndex);

        int[] start = TestCoachViews.Unit(Team, 2, 1);
        var events = new List<SimEvent> { new FaceoffEvent(TestCoachViews.Context(0.0, start), start[1], 902, TeamSide.Home, FaceoffLocation.NeutralZone) };
        var choice = lines.OnTheFly(ViewAt(300.0, 2, 1, events, canF: false, canD: true));
        Assert.Equal(2, choice.ForwardIndex);
        Assert.NotEqual(1, choice.PairIndex);
    }

    [Theory]
    [InlineData(3, 100.0, 1, 0, GameMode.Protect)]
    [InlineData(3, 100.0, 0, 2, GameMode.Chase)]
    [InlineData(3, 100.0, 1, 1, GameMode.Normal)]
    [InlineData(2, 100.0, 1, 0, GameMode.Normal)]
    [InlineData(3, 600.0, 1, 0, GameMode.Normal)]
    public void RuleTactics_Mode_DependsOnLateGameScore(int period, double time, int own, int opp, GameMode expected)
    {
        var tactics = new RuleTactics(Library, Systems, Tuning);
        double clock = 1200.0 - time;
        Assert.Equal(expected, tactics.Mode(ViewAt(clock, 0, 0, Array.Empty<SimEvent>(), period: period, own: own, opp: opp)));
    }

    [Fact]
    public void RuleTactics_Plan_UsesTheModeSettings()
    {
        var tactics = new RuleTactics(Library, Systems, Tuning);
        ShiftPlan protect = tactics.Plan(ViewAt(1150.0, 0, 0, Array.Empty<SimEvent>(), period: 3, own: 2, opp: 0));
        AiModeConfig settings = Tuning.Ai.Rulebased.Modes["protect"];
        Assert.Equal(settings.System, protect.System.Id);
        Assert.Equal(settings.Transition == "regroup" ? TransitionInstruction.Regroup : TransitionInstruction.Rush, protect.Transition);
        Assert.Equal(settings.LooseChasers, protect.Instructions.LooseChasers);
        Assert.Equal(settings.Pinch, protect.Instructions.Pinch);
        Assert.Equal(settings.NetFrontAfterShot, protect.Instructions.NetFrontAfterShot);
        Assert.Equal(Library.Count, protect.Plays.Count);
    }

    [Fact]
    public void RuleTactics_Playbook_UsesTheLastObservedOpponentSystemAndFamiliarity()
    {
        var tactics = new RuleTactics(Library, Systems, Tuning);
        int[] own = TestCoachViews.Unit(Team, 0, 0);
        string system = Tuning.Ai.Rulebased.PlayBonusBySystem.Keys.First();
        KeyValuePair<string, double> top = Tuning.Ai.Rulebased.PlayBonusBySystem[system].OrderByDescending(b => b.Value).First();

        // Own pass against the system: the opponent defended with it.
        var events = new List<SimEvent> { new PassEvent(TestCoachViews.Context(10.0, own, systemId: system), own[0], own[1], false, true) };
        IReadOnlyList<Play> plays = tactics.Plan(ViewAt(20.0, 0, 0, events)).Plays;
        Assert.Equal(top.Key, plays[0].Id);

        // Without observations it is the rotation order (least used first, ties in load order).
        IReadOnlyList<Play> plain = tactics.Plan(ViewAt(20.0, 0, 0, Array.Empty<SimEvent>())).Plays;
        Assert.Equal(Library.Select(p => p.Id), plain.Select(p => p.Id));

        // An opponent's own pass does not show the opponent's system.
        var theirs = new List<SimEvent> { new PassEvent(TestCoachViews.Context(10.0, own, systemId: system), 901, 902, false, true) };
        Assert.Null(PublicLog.LastOpponentSystem(ViewAt(20.0, 0, 0, theirs)));
    }

    [Fact]
    public void RuleTactics_Timeout_TiredStuckUnit_OrLateDeficit()
    {
        var tactics = new RuleTactics(Library, Systems, Tuning);
        var can = new CoachTimeouts(1, 1, false, true);
        int[] start = TestCoachViews.Unit(Team, 0, 0);
        var events = new List<SimEvent> { new FaceoffEvent(TestCoachViews.Context(0.0, start), start[1], 902, TeamSide.Home, FaceoffLocation.NeutralZone) };

        // Long shift: estimate far below tiredEnergy.
        double seconds = (Tuning.Energy.Start - Tuning.Ai.Rulebased.Timeout.TiredEnergy + 0.05) / Drain;
        Assert.True(tactics.Timeout(ViewAt(seconds, 0, 0, events, canF: false, canD: true, timeouts: can)));
        Assert.False(tactics.Timeout(ViewAt(seconds, 0, 0, events, canF: true, canD: true, timeouts: can)));
        Assert.False(tactics.Timeout(ViewAt(seconds, 0, 0, events, canF: false, canD: true, timeouts: new CoachTimeouts(0, 1, false, false))));

        double late = 1200.0 - Tuning.Ai.Rulebased.Timeout.LateGameSeconds + 1.0;
        Assert.True(tactics.Timeout(ViewAt(late, 0, 0, Array.Empty<SimEvent>(), period: 3, own: 0, opp: 1, timeouts: can)));
        Assert.False(tactics.Timeout(ViewAt(late, 0, 0, Array.Empty<SimEvent>(), period: 3, own: 0, opp: Tuning.Ai.Rulebased.Timeout.MaxDeficitGoals + 1, timeouts: can)));
        Assert.False(tactics.Timeout(ViewAt(late, 0, 0, Array.Empty<SimEvent>(), period: 2, own: 0, opp: 1, timeouts: can)));
        Assert.False(tactics.Timeout(ViewAt(late, 0, 0, Array.Empty<SimEvent>(), period: 3, own: 1, opp: 1, timeouts: can)));
    }

    [Fact]
    public void Adaptive_DistributionHasThePrior_AndSystemFollowsEntryShareWithHysteresis()
    {
        var tactics = new AdaptiveTactics(Library, Systems, Tuning);
        AiAdaptiveConfig a = Tuning.Ai.Adaptive;
        int[] own = TestCoachViews.Unit(Team, 0, 0);

        IReadOnlyDictionary<string, double> uniform = tactics.OpponentSystemDistribution(ViewAt(10.0, 0, 0, Array.Empty<SimEvent>()));
        Assert.All(uniform.Values, p => Assert.Equal(1.0 / Systems.Count, p, 12));

        string s0 = Systems.Select(s => s.Id).OrderBy(x => x, StringComparer.Ordinal).First();
        var seen = Enumerable.Range(0, 10).Select(i => (SimEvent)new PassEvent(TestCoachViews.Context(i, own, systemId: s0), own[0], own[1], false, true)).ToList();
        IReadOnlyDictionary<string, double> p = tactics.OpponentSystemDistribution(ViewAt(20.0, 0, 0, seen));
        Assert.Equal((10 + (a.PriorWeight / Systems.Count)) / (10 + a.PriorWeight), p[s0], 12);

        // Many opponent carries → passive; many dumps → aggressive; in between → previous choice.
        List<SimEvent> Entries(int carries, int dumps) =>
            Enumerable.Range(0, carries).Select(i => (SimEvent)new ControlledZoneEntryEvent(TestCoachViews.Context(i, own), 901, new EntryNumbers(2, 2), ZoneEntryMethod.Carry, ZoneEntryOutcome.Kept))
                .Concat(Enumerable.Range(0, dumps).Select(i => (SimEvent)new DumpInEvent(TestCoachViews.Context(100 + i, own), 902, BattleOutcome.NoWinner)))
                .ToList();

        Assert.Equal(a.PassiveSystem, tactics.Plan(ViewAt(300.0, 0, 0, Entries(40, 0))).System.Id);
        Assert.Equal(a.PassiveSystem, tactics.Plan(ViewAt(300.0, 0, 0, Entries(5, 5))).System.Id);
        Assert.Equal(a.AggressiveSystem, tactics.Plan(ViewAt(300.0, 0, 0, Entries(0, 40))).System.Id);
        Assert.Equal(a.AggressiveSystem, tactics.Plan(ViewAt(300.0, 0, 0, Entries(5, 5))).System.Id);
        Assert.Equal(Tuning.Ai.Rulebased.Modes["normal"].System, new AdaptiveTactics(Library, Systems, Tuning).Plan(ViewAt(300.0, 0, 0, Entries(5, 5))).System.Id);

        // Events outside the window are ignored: q falls back to the prior.
        double q = tactics.ControlledEntryShare(ViewAt(a.WindowSeconds + 500.0, 0, 0, Entries(40, 0), period: 1));
        Assert.Equal(AdaptiveTactics.PriorEntryShare, q, 12);
    }

    [Theory]
    [InlineData("rulebased", "random")]
    [InlineData("adaptive", "rulebased")]
    [InlineData("randomTactics", "randomLines")]
    [InlineData("rulebased", "rotation")]
    public void AllCoaches_PlayDeterministicMatchesWithoutStalls(string home, string away)
    {
        var data = ShiftTestData.Data;
        string Log(ulong seed) => MatchJson.Write(MatchCommand.Play(data, MatchTestData.Setup, home, "forecheck212", away, "trap122", seed), MatchTestData.Setup, seed);

        Assert.Equal(Log(21UL), Log(21UL));
        for (ulong seed = 1; seed <= 6; seed++)
        {
            Assert.False(MatchCommand.Play(data, MatchTestData.Setup, home, "forecheck212", away, "trap122", seed).Stalled);
        }
    }

    [Fact]
    public void Registry_KnowsEveryAiOfAi2()
    {
        Assert.Equal(new[] { "adaptive", "random", "randomLines", "randomTactics", "rotation", "rulebased" }, CoachRegistry.Names);
    }
}
