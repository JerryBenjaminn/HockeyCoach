using HockeyCoach.Harness.Data;

namespace HockeyCoach.Harness.Batch;

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
        int pairs = options.Matches / 2;
        int chunk = Math.Max(1, options.Threads * PairsPerThreadPerChunk);
        var all = new List<MatchupStats>();
        foreach (Matchup matchup in options.Matchups)
        {
            var stats = new MatchupStats(matchup, data.Tuning.Time.Periods, data.Tuning.Time.PeriodSeconds);
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
                        games[k] = BatchMatch.PlayPair(data, matchup, options.RotationSystem, options.MirrorRosters, pair, options.Seed + (ulong)pair);
                    });
                foreach (BatchGame[] pairGames in games)
                {
                    foreach (BatchGame game in pairGames)
                    {
                        stats.Add(game, data.Rink);
                    }
                }
            }

            progress?.Invoke(matchup.Label + ": " + options.Matches + " matches done");
            all.Add(stats);
        }

        return all;
    }
}
