using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>Shared match setup (repository data, harness teams, rotation coaches) and a small cache of played matches.</summary>
internal static class MatchTestData
{
    public static readonly MatchSetup Setup = MatchCommand.Setup(ShiftTestData.Data);

    private static readonly Dictionary<(ulong, string, string), MatchResult> Cache = new();

    public static MatchResult Play(ulong seed, string home = "forecheck212", string away = "trap122")
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue((seed, home, away), out MatchResult? result))
            {
                result = MatchCommand.Play(ShiftTestData.Data, Setup, home, away, seed);
                Cache[(seed, home, away)] = result;
            }

            return result;
        }
    }

    public static Func<int, TeamSide> TeamOf => MatchCommand.TeamOf(Setup);

    public static IEnumerable<MatchResult> Sample(int count)
    {
        for (ulong seed = 1; seed <= (ulong)count; seed++)
        {
            yield return Play(seed);
        }
    }
}
