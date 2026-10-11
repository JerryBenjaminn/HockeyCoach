using System.Globalization;
using HockeyCoach.Harness.Simulation;

namespace HockeyCoach.Harness.Batch;

/// <summary>One AI pair of a batch: coach A (first) against coach B.</summary>
/// <param name="A">Coach name of team A.</param>
/// <param name="B">Coach name of team B.</param>
public sealed record Matchup(string A, string B)
{
    /// <summary>"a vs b".</summary>
    public string Label => A + " vs " + B;

    /// <summary>"a-b" for file and CSV keys.</summary>
    public string Key => A + "-" + B;
}

/// <summary>Options of <c>batch</c>.</summary>
/// <param name="Matches">Matches per AI pair (even: played in pairs, Q-057 b).</param>
/// <param name="Matchups">The AI pairs in report order.</param>
/// <param name="Seed">Seed of the first pair; pair i of every matchup uses Seed + i (same seeds and rosters across matchups).</param>
/// <param name="Threads">Worker threads; the output does not depend on it.</param>
/// <param name="OutDirectory">Directory for the report files (reports/ is git-ignored).</param>
/// <param name="RotationSystem">Defensive system of the rotation coach.</param>
/// <param name="MirrorRosters">Both teams get the same roster stats in every pair (Q-057 c).</param>
public sealed record BatchOptions(int Matches, IReadOnlyList<Matchup> Matchups, ulong Seed, int Threads, string OutDirectory, string RotationSystem, bool MirrorRosters)
{
    /// <summary>AIs of <c>--matchups all</c>, in report order: the primary pair first (D-077 item 4).</summary>
    public static readonly IReadOnlyList<string> AllCoaches = new[]
    {
        CoachRegistry.RuleBased, CoachRegistry.Adaptive, CoachRegistry.Random, CoachRegistry.RandomTactics, CoachRegistry.RandomLines, CoachRegistry.Rotation,
    };

    /// <summary>Every unordered pair of <see cref="AllCoaches"/>, mirrors (X vs X) included, in report order.</summary>
    public static IReadOnlyList<Matchup> AllMatchups()
    {
        var list = new List<Matchup>();
        for (int i = 0; i < AllCoaches.Count; i++)
        {
            for (int j = i; j < AllCoaches.Count; j++)
            {
                list.Add(new Matchup(AllCoaches[i], AllCoaches[j]));
            }
        }

        return list;
    }

    /// <summary>Parses <c>batch</c> arguments; returns null and an error message on bad input.</summary>
    public static BatchOptions? Parse(string[] args, out string? error)
    {
        error = null;
        int matches = 1000;
        string matchups = "all";
        ulong seed = 1;
        int threads = Environment.ProcessorCount;
        string output = "reports";
        string rotationSystem = "forecheck212";
        bool mirror = false;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--matches" when i + 1 < args.Length && int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out int m) && m > 0:
                    matches = m;
                    i++;
                    break;
                case "--matchups" when i + 1 < args.Length:
                    matchups = args[++i];
                    break;
                case "--seed" when i + 1 < args.Length && ulong.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out ulong s):
                    seed = s;
                    i++;
                    break;
                case "--threads" when i + 1 < args.Length && int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out int t) && t > 0:
                    threads = t;
                    i++;
                    break;
                case "--out" when i + 1 < args.Length:
                    output = args[++i];
                    break;
                case "--rotation-system" when i + 1 < args.Length:
                    rotationSystem = args[++i];
                    break;
                case "--mirror":
                    mirror = true;
                    break;
                case "--data":
                    i++;
                    break;
                default:
                    error = "Unknown or incomplete option " + args[i];
                    return null;
            }
        }

        if (matches % 2 != 0)
        {
            error = "--matches must be even: matches are played in pairs with the same seed and sides swapped (got " + matches + ")";
            return null;
        }

        IReadOnlyList<Matchup>? list = ParseMatchups(matchups, out error);
        return list == null ? null : new BatchOptions(matches, list, seed, threads, output, rotationSystem, mirror);
    }

    /// <summary><c>all</c> or a comma-separated list of <c>a-b</c> pairs, e.g. <c>rulebased-random,rulebased-rulebased</c>.</summary>
    public static IReadOnlyList<Matchup>? ParseMatchups(string text, out string? error)
    {
        error = null;
        if (text == "all")
        {
            return AllMatchups();
        }

        var list = new List<Matchup>();
        foreach (string part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] names = part.Split('-');
            if (names.Length != 2 || !CoachRegistry.Names.Contains(names[0]) || !CoachRegistry.Names.Contains(names[1]))
            {
                error = "Bad matchup " + part + " (expected a-b with coaches " + string.Join(", ", CoachRegistry.Names) + ", or all)";
                return null;
            }

            list.Add(new Matchup(names[0], names[1]));
        }

        if (list.Count == 0)
        {
            error = "No matchups given.";
            return null;
        }

        return list;
    }
}
