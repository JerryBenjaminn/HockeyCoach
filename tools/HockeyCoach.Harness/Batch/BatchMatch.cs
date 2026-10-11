using HockeyCoach.Harness.Data;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Harness.Batch;

/// <summary>A plan decision recorded for the report (rush vs regroup): when it was made and what it said.</summary>
/// <param name="Period">Period.</param>
/// <param name="Time">Period time of the decision.</param>
/// <param name="Transition">Transition instruction.</param>
/// <param name="SystemId">Own defensive system.</param>
public sealed record PlanRecord(int Period, double Time, TransitionInstruction Transition, string SystemId);

/// <summary>Records every stoppage decision of the wrapped coach (report only; passes decisions through unchanged).</summary>
public sealed class RecordingCoach : ICoach
{
    private readonly ICoach _inner;

    /// <summary>Wraps <paramref name="inner"/>.</summary>
    public RecordingCoach(ICoach inner)
    {
        _inner = inner;
    }

    /// <summary>Decisions in order; at a timeout the after-timeout round comes later with the same time.</summary>
    public List<PlanRecord> Plans { get; } = new();

    /// <inheritdoc />
    public CoachDecision AtStoppage(CoachView view)
    {
        CoachDecision decision = _inner.AtStoppage(view);
        Plans.Add(new PlanRecord(view.Period, view.Time, decision.Plan.Transition, decision.Plan.System.Id));
        return decision;
    }

    /// <inheritdoc />
    public LineChoice OnTheFly(CoachView view) => _inner.OnTheFly(view);
}

/// <summary>Collects check observations of one match.</summary>
public sealed class CheckCollector : ICheckObserver
{
    /// <summary>Observations in order.</summary>
    public List<CheckObservation> Checks { get; } = new();

    /// <inheritdoc />
    public void OnCheck(CheckObservation observation) => Checks.Add(observation);
}

/// <summary>One played batch match with what the report needs beyond the log.</summary>
/// <param name="Game">The paired game (pair, seed, venue, setup, result).</param>
/// <param name="Plans">Recorded plans per venue side (home, away).</param>
/// <param name="Checks">Every resolved check.</param>
public sealed record BatchGame(PairedGame Game, IReadOnlyList<PlanRecord>[] Plans, IReadOnlyList<CheckObservation> Checks);

/// <summary>Plays one pair of a batch matchup (same rosters and seed twice, sides swapped; Q-057 c, D-069).</summary>
public static class BatchMatch
{
    /// <summary>Plays pair <paramref name="pair"/> with seed <paramref name="seed"/>: team A at home first, then team B.</summary>
    public static BatchGame[] PlayPair(GameData data, Matchup matchup, string rotationSystem, bool mirror, int pair, ulong seed)
    {
        Func<IRandom, ICoach> a = CoachRegistry.Factory(matchup.A, data, rotationSystem);
        Func<IRandom, ICoach> b = CoachRegistry.Factory(matchup.B, data, rotationSystem);
        (Team rosterA, Team rosterB) = HarnessTeams.PairRosters(seed, mirror);
        return new[]
        {
            Play(data, rosterA, rosterB, a, b, pair, seed, true),
            Play(data, rosterB, rosterA, b, a, pair, seed, false),
        };
    }

    private static BatchGame Play(GameData data, Team home, Team away, Func<IRandom, ICoach> homeCoach, Func<IRandom, ICoach> awayCoach, int pair, ulong seed, bool teamAIsHome)
    {
        var setup = new MatchSetup(data.Rink, data.Tuning, home, away, MatchCommand.DefaultMaxStepsPerSegment, MatchCommand.DefaultMaxSegments);
        var streams = new MatchStreams(seed);
        var homeRecorder = new RecordingCoach(homeCoach(streams.HomeCoach()));
        var awayRecorder = new RecordingCoach(awayCoach(streams.AwayCoach()));
        var checks = new CheckCollector();
        MatchResult result = MatchSimulator.Run(setup, homeRecorder, awayRecorder, streams.Simulation(), checks);
        return new BatchGame(new PairedGame(pair, seed, teamAIsHome, setup, result), new IReadOnlyList<PlanRecord>[] { homeRecorder.Plans, awayRecorder.Plans }, checks.Checks);
    }
}
