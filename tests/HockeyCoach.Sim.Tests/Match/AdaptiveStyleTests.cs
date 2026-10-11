using HockeyCoach.AI;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;
using HockeyCoach.Sim.Tests.Shift;
using HockeyCoach.Sim.Tests.Support;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>Adaptive own system from the opponent's transition style (data-schema.md AI-2, A-T2 item 9).</summary>
public class AdaptiveStyleTests
{
    private const string Forecheck = "forecheck212";
    private const string Trap = "trap122";
    private const string SomePlay = "somePlay";
    private const int Opp1 = 901;
    private const int Opp2 = 902;

    private static readonly TuningConfig Tuning = ShiftTestData.Data.Tuning;
    private static readonly IReadOnlyList<Play> Library = ShiftTestData.Data.Plays;
    private static readonly IReadOnlyList<DefensiveSystem> Systems = ShiftTestData.Data.Systems;
    private static readonly Team Team = TestPlayers.Team("Home", 1, allStats: 10, trios: 4, pairs: 3);
    private static readonly int[] Own = TestCoachViews.Unit(Team, 0, 0);
    private static readonly SortedSet<int> OwnSet = new(Team.Skaters.Select(s => s.Id));

    private static AiAdaptiveConfig Adaptive => Tuning.Ai.Adaptive;

    private static string NormalSystem => Tuning.Ai.Rulebased.Modes["normal"].System;

    private static EventContext Ctx(string? play = null, string? system = Forecheck, int period = 1, double time = 10.0) =>
        TestCoachViews.Context(time, Own, period, play, system);

    // Opponent (away) events: the own team defends with `system`.
    private static SimEvent OppPass(string? play = null, string? system = Forecheck) => new PassEvent(Ctx(play, system), Opp1, Opp2, false, true);

    private static SimEvent OppEntry(string? play = null, string? system = Forecheck) =>
        new ControlledZoneEntryEvent(Ctx(play, system), Opp1, new EntryNumbers(2, 2), ZoneEntryMethod.Carry, ZoneEntryOutcome.Kept);

    // Own events: the opponent defends (system = opponent's).
    private static SimEvent OwnPass(bool succeeded = true, string? play = null) => new PassEvent(Ctx(play, Trap), Own[0], Own[1], false, succeeded);

    private static SimEvent Turnover(int loser, int taker, string? play = null, string? system = Forecheck) => new TurnoverEvent(Ctx(play, system), loser, taker, 0);

    private static SimEvent OwnLosesToOpponent() => Turnover(Own[0], Opp1, system: Trap);

    private static SimEvent Faceoff(bool ownWins) => new FaceoffEvent(Ctx(), Own[0], Opp1, ownWins ? TeamSide.Home : TeamSide.Away, FaceoffLocation.NeutralZone);

    private static SimEvent Stoppage() => new StoppageEvent(Ctx(), StoppageReason.Icing);

    private static IReadOnlyList<TransitionObservation> Classify(params SimEvent[] events) => TransitionStyle.Classify(events, OwnSet);

    private static TransitionStyleClass Single(params SimEvent[] events)
    {
        TransitionObservation o = Assert.Single(Classify(events));
        return o.Style;
    }

    [Fact]
    public void Classify_TakeawayThenOpponentEntryWithoutPlay_IsRush()
    {
        Assert.Equal(TransitionStyleClass.Rush, Single(Faceoff(true), OwnLosesToOpponent(), OppEntry()));
        TransitionObservation o = Classify(Faceoff(true), OwnLosesToOpponent(), OppEntry(system: Trap))[0];
        Assert.Equal(Trap, o.SystemId);
        Assert.Equal(2, o.EventIndex);
    }

    [Fact]
    public void Classify_TakeawayThenOpponentPassWithPlay_IsRegroup()
    {
        Assert.Equal(TransitionStyleClass.Regroup, Single(Faceoff(true), OwnLosesToOpponent(), OppPass(SomePlay)));
    }

    [Fact]
    public void Classify_FirstActionTurnover_IsNotClassified()
    {
        Assert.Empty(Classify(Faceoff(true), OwnLosesToOpponent(), Turnover(Opp1, Own[0], system: Forecheck), OwnPass()));
    }

    [Fact]
    public void Classify_PossessionFromAFaceoff_IsNotClassified()
    {
        Assert.Empty(Classify(Faceoff(false), OppPass(), OppEntry()));
        Assert.Empty(Classify(Faceoff(false), OppPass(SomePlay), OppEntry(SomePlay)));
    }

    [Fact]
    public void Classify_OwnFailedPassThenOpponentAction_StartsANewClassifiedPossession()
    {
        Assert.Equal(TransitionStyleClass.Rush, Single(Faceoff(true), OwnPass(succeeded: false), OppEntry()));
        Assert.Equal(TransitionStyleClass.Regroup, Single(Faceoff(true), OwnPass(succeeded: false), OppPass(SomePlay)));
    }

    [Fact]
    public void Classify_BattleLineChangeAndTimeout_NeitherEndNorStartAPossession()
    {
        SimEvent battle = new PuckBattleEvent(Ctx(), new[] { Opp1 }, new[] { Own[0] }, BattleOutcome.Win);
        SimEvent change = new LineChangeEvent(Ctx(), new[] { Opp1 }, new[] { Opp2 });
        SimEvent timeout = new TimeoutEvent(Ctx(), TeamSide.Away);

        // Between the takeaway and the first action: still the first action.
        Assert.Equal(TransitionStyleClass.Rush, Single(Faceoff(true), OwnLosesToOpponent(), battle, change, timeout, OppEntry()));

        // After a faceoff possession: they do not start a transition possession.
        Assert.Empty(Classify(Faceoff(false), battle, change, timeout, OppPass()));
    }

    [Fact]
    public void Classify_StoppageAndPeriodStart_ResetTheState()
    {
        // A stoppage ends the possession; a takeaway after the next faceoff starts a new one.
        Assert.Equal(TransitionStyleClass.Rush, Single(Faceoff(true), OwnLosesToOpponent(), Stoppage(), Faceoff(true), OwnLosesToOpponent(), OppEntry()));

        // Takeaway, stoppage, opponent faceoff win: nothing is classified.
        Assert.Empty(Classify(Faceoff(true), OwnLosesToOpponent(), Stoppage(), Faceoff(false), OppPass()));

        // A new period resets the state like a stoppage: the opponent's pending first action is gone.
        SimEvent nextPeriodFaceoff = new FaceoffEvent(Ctx(period: 2, time: 0.0), Own[0], Opp1, TeamSide.Away, FaceoffLocation.NeutralZone);
        SimEvent nextPeriodPass = new PassEvent(Ctx(period: 2, time: 1.0), Opp1, Opp2, false, true);
        Assert.Empty(Classify(Faceoff(true), OwnLosesToOpponent(), nextPeriodFaceoff, nextPeriodPass));
    }

    [Fact]
    public void Classify_OnlyTheFirstActionOfAPossession()
    {
        Assert.Equal(TransitionStyleClass.Rush, Single(Faceoff(true), OwnLosesToOpponent(), OppPass(), OppPass(SomePlay), OppEntry(SomePlay)));
    }

    [Fact]
    public void Classify_OwnTransitions_AreNeverObservations()
    {
        SimEvent oppLoses = Turnover(Opp1, Own[0], system: Trap);
        Assert.Empty(Classify(Faceoff(false), oppLoses, OwnPass(), OwnPass(play: SomePlay)));
        Assert.Empty(Classify(Faceoff(false), OppPass(), OwnPass()));
    }

    [Fact]
    public void Classify_EmptySystemId_GivesNoObservation()
    {
        Assert.Empty(Classify(Faceoff(true), OwnLosesToOpponent(), OppEntry(system: null)));
    }

    private static TransitionObservation Obs(int index, bool rush, string system = Forecheck) =>
        new(index, rush ? TransitionStyleClass.Rush : TransitionStyleClass.Regroup, system);

    [Fact]
    public void Window_CountsOnlyTheCurrentSystem_AndOnlyTheLastWindowEvents()
    {
        int w = Adaptive.StyleWindowEvents;
        var observations = new List<TransitionObservation>();
        for (int i = 0; i < w + 3; i++)
        {
            observations.Add(Obs(2 * i, rush: i < 3, Forecheck));
            observations.Add(Obs((2 * i) + 1, rush: true, Trap));
        }

        IReadOnlyList<TransitionObservation> window = AdaptiveTactics.StyleWindow(observations, Forecheck, 0, w);
        Assert.Equal(w, window.Count);
        Assert.All(window, o => Assert.Equal(Forecheck, o.SystemId));
        Assert.All(window, o => Assert.Equal(TransitionStyleClass.Regroup, o.Style));
        Assert.Equal(2 * (w + 2), window[^1].EventIndex);
        Assert.True(window.Zip(window.Skip(1)).All(p => p.First.EventIndex < p.Second.EventIndex));

        // Observations before the moment the system was chosen do not count.
        Assert.Empty(AdaptiveTactics.StyleWindow(observations, Forecheck, 1000, w));
        Assert.Equal(2, AdaptiveTactics.StyleWindow(observations, Forecheck, 2 * (w + 1), w).Count);
    }

    private static IReadOnlyList<TransitionObservation> Window(int rushes, int regroups, string system = Forecheck) =>
        Enumerable.Range(0, rushes + regroups).Select(i => Obs(i, i < rushes, system)).ToList();

    [Fact]
    public void Decide_FollowsTheThresholdsAndHysteresis()
    {
        int min = Adaptive.StyleMinEvents;
        Assert.Equal(4, min);
        Assert.Equal(Trap, Adaptive.PassiveSystem);
        Assert.Equal(Forecheck, Adaptive.AggressiveSystem);

        // n = min − 1: c stays even when all are rushes.
        Assert.Equal(Forecheck, AdaptiveTactics.DecideSystem(Window(min - 1, 0), Forecheck, Adaptive));
        Assert.Equal(Trap, AdaptiveTactics.DecideSystem(Window(0, min - 1, Trap), Trap, Adaptive));

        Assert.Equal(Trap, AdaptiveTactics.DecideSystem(Window(3, 1), Forecheck, Adaptive));
        Assert.Equal(Forecheck, AdaptiveTactics.DecideSystem(Window(1, 3, Trap), Trap, Adaptive));
        Assert.Equal(Forecheck, AdaptiveTactics.DecideSystem(Window(2, 2), Forecheck, Adaptive));
        Assert.Equal(Trap, AdaptiveTactics.DecideSystem(Window(2, 2, Trap), Trap, Adaptive));
    }

    private static CoachView View(IReadOnlyList<SimEvent> events, double time = 300.0, int period = 1, int own = 0, int opp = 0) =>
        TestCoachViews.Make(Team, 0, 0, true, true, call: CoachCall.Stoppage, events: events, clock: new CoachClock(period, time, 3, 1200.0), ownGoals: own, opponentGoals: opp);

    /// <summary>A log with the given opponent transitions against <paramref name="system"/>, each after an own turnover.</summary>
    private static List<SimEvent> Transitions(IEnumerable<SimEvent> prefix, string system, params bool[] rushes)
    {
        var log = new List<SimEvent>(prefix);
        if (log.Count == 0)
        {
            log.Add(Faceoff(true));
        }

        foreach (bool rush in rushes)
        {
            log.Add(OwnLosesToOpponent());
            log.Add(rush ? OppEntry(null, system) : OppPass(SomePlay, system));
        }

        return log;
    }

    [Fact]
    public void Plan_EmptyLog_UsesModesNormalSystem()
    {
        var tactics = new AdaptiveTactics(Library, Systems, Tuning);
        Assert.Equal(NormalSystem, tactics.Plan(View(Array.Empty<SimEvent>())).System.Id);
        Assert.Equal(NormalSystem, tactics.CurrentSystem);
    }

    [Fact]
    public void Plan_AfterASwitch_TheWindowIsEmpty_AndATimeoutRoundKeepsTheChoice()
    {
        Assert.Equal(Forecheck, NormalSystem);
        var tactics = new AdaptiveTactics(Library, Systems, Tuning);

        // Regroups against trap long ago, then rushes against forecheck: switch to trap.
        List<SimEvent> log = Transitions(Array.Empty<SimEvent>(), Trap, false, false, false, false);
        log = Transitions(log, Forecheck, true, true, true, true);
        Assert.Equal(Trap, tactics.Plan(View(log)).System.Id);

        // Same stoppage, after-timeout round: same choice (the old regroups against trap do not count).
        Assert.Equal(Trap, tactics.Plan(View(log)).System.Id);

        // min − 1 new regroups against trap: c stays.
        log = Transitions(log, Trap, Enumerable.Repeat(false, Adaptive.StyleMinEvents - 1).ToArray());
        Assert.Equal(Trap, tactics.Plan(View(log)).System.Id);

        // One more: switch back to forecheck.
        log = Transitions(log, Trap, false);
        Assert.Equal(Forecheck, tactics.Plan(View(log)).System.Id);
    }

    [Fact]
    public void Plan_ProtectAndChase_UseModesSystem_AndKeepTheNormalChoice()
    {
        var tactics = new AdaptiveTactics(Library, Systems, Tuning);
        List<SimEvent> log = Transitions(Array.Empty<SimEvent>(), Forecheck, true, true, true, true);
        Assert.Equal(Trap, tactics.Plan(View(log)).System.Id);

        // Late in the last period: regroups against trap would switch to forecheck in normal mode.
        log = Transitions(log, Trap, false, false, false, false);
        double late = 1200.0 - 60.0;
        Assert.Equal(Tuning.Ai.Rulebased.Modes["protect"].System, tactics.Plan(View(log, late, 3, own: 2)).System.Id);
        Assert.Equal(Tuning.Ai.Rulebased.Modes["chase"].System, tactics.Plan(View(log, late, 3, opp: 2)).System.Id);
        Assert.Equal(Trap, tactics.CurrentSystem);

        // Back in normal: decides from the remembered c (trap) and the regroups against it.
        Assert.Equal(Forecheck, tactics.Plan(View(log, late, 3)).System.Id);
    }

    [Fact]
    public void Plan_IsDeterministic_SameLogSameDecision()
    {
        List<SimEvent> log = Transitions(Array.Empty<SimEvent>(), Forecheck, true, false, true, true, false, true);
        string[] first = Enumerable.Range(0, 3).Select(_ => new AdaptiveTactics(Library, Systems, Tuning).Plan(View(log)).System.Id).ToArray();
        Assert.All(first, s => Assert.Equal(first[0], s));
        Assert.Equal(
            Classify(log.ToArray()).Select(o => (o.EventIndex, o.Style, o.SystemId)),
            Classify(log.ToArray()).Select(o => (o.EventIndex, o.Style, o.SystemId)));
    }
}
