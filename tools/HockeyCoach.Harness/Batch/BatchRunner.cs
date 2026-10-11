using HockeyCoach.Harness.Data;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Random;

namespace HockeyCoach.Harness.Batch;

/// <summary>One matchup to play: its labels and the coach factories of team A and team B.</summary>
/// <param name="Matchup">Labels of team A and team B.</param>
/// <param name="A">Coach factory of team A.</param>
/// <param name="B">Coach factory of team B.</param>
public sealed record BatchEntry(Matchup Matchup, Func<IRandom, ICoach> A, Func<IRandom, ICoach> B);

/// <summary>
/// Plays every matchup of a batch: pairs run in parallel in chunks, then each chunk is added to the statistics in pair
/// order, so the result does not depend on the thread count. Raw logs are dropped after their chunk (CLAUDE.md rule 5).
/// </summary>
public static class BatchRunner
{
    /// <summary>Pairs per worker thread in one chunk (memory bound only; does not affect results).</summary>
    public const int PairsPerThreadPerChunk = 2;

    /// <summary>Runs the batch and returns one statistics object per matchup, in matchup order.</summary>
    /// <param name="data">Loaded data.</param>
    /// <param name="options">Batch options.</param>
    /// <param name="progress">Optional progress line callback (stderr in the CLI).</param>
    public static IReadOnlyList<MatchupStats> Run(GameData data, BatchOptions options, Action<string>? progress = null)
    {
        var entries = options.Matchups
            .Select(m => new BatchEntry(
                m,
                CoachRegistry.Factory(m.A, data, options.RotationSystem, options.RotationTransition),
                CoachRegistry.Factory(m.B, data, options.RotationSystem, options.RotationTransition)))
            .ToList();
        return Run(data, options, entries, progress);
    }

    /// <summary>Runs the given matchups with the pair count, seeds, rosters and threads of <paramref name="options"/>.</summary>
    public static IReadOnlyList<MatchupStats> Run(GameData data, BatchOptions options, IReadOnlyList<BatchEntry> entries, Action<string>? progress = null)
    {
        int pairs = options.Matches / 2;
        int chunk = Math.Max(1, options.Threads * PairsPerThreadPerChunk);
        var all = new List<MatchupStats>();
        foreach (BatchEntry entry in entries)
        {
            var stats = new MatchupStats(entry.Matchup, data.Tuning.Time.Periods, data.Tuning.Time.PeriodSeconds, data.Tuning.Ai.Rulebased.TransitionBySystem);
            for (int first = 0; first < pairs; first += chunk)
            {
                int count = Math.Min(chunk, pairs - first);
                var games = new BatchGame[count][];
                Parallel.For(
                    0,
                    count,
                    new ParallelOptions { MaxDegreeOfParallelism = options.Threads },
                    k =>
                    {
                        int pair = first + k;
                        games[k] = BatchMatch.PlayPair(data, entry.A, entry.B, options.MirrorRosters, pair, options.Seed + (ulong)pair);
                    });
                foreach (BatchGame[] pairGames in games)
                {
                    foreach (BatchGame game in pairGames)
                    {
                        stats.Add(game, data.Rink);
                    }
                }
            }

            progress?.Invoke(entry.Matchup.Label + ": " + options.Matches + " matches done");
            all.Add(stats);
        }

        return all;
    }
}
