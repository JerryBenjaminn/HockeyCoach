using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;

namespace HockeyCoach.Harness.Simulation;

/// <summary>
/// One side of a paired series: the roster, a factory for its coach (a fresh coach per match) and a label for output.
/// The roster, the coach and its system travel with the team when it changes venue.
/// </summary>
/// <param name="Label">Name in output, e.g. "A (forecheck212)".</param>
/// <param name="Team">The roster.</param>
/// <param name="NewCoach">Creates the team's coach for one match.</param>
public sealed record PairedTeam(string Label, Team Team, Func<ICoach> NewCoach);

/// <summary>One played match of a paired series.</summary>
/// <param name="Pair">Zero-based pair index.</param>
/// <param name="Seed">Seed of the pair (both matches of a pair use it).</param>
/// <param name="TeamAIsHome">True in the first match of the pair (team A at home), false in the second.</param>
/// <param name="Setup">The match setup (home and away rosters for this match).</param>
/// <param name="Result">The result.</param>
public sealed record PairedGame(int Pair, ulong Seed, bool TeamAIsHome, MatchSetup Setup, MatchResult Result)
{
    /// <summary>Team A's goals.</summary>
    public int TeamAGoals => TeamAIsHome ? Result.HomeGoals : Result.AwayGoals;

    /// <summary>Team B's goals.</summary>
    public int TeamBGoals => TeamAIsHome ? Result.AwayGoals : Result.HomeGoals;

    /// <summary>The venue side of team A in this match.</summary>
    public TeamSide TeamASide => TeamAIsHome ? TeamSide.Home : TeamSide.Away;
}

/// <summary>
/// Paired matches (D-069): every pair plays the same two rosters with the same seed twice, first team A at home, then
/// the teams swap home and away (rosters, coaches and systems travel with the team). This separates home advantage from
/// roster strength and seed luck. Shared by <c>match --count</c> and the upcoming <c>batch</c> command.
/// </summary>
public static class PairedMatches
{
    /// <summary>
    /// Number of pairs for <paramref name="matchCount"/> matches. The count must be a positive even number (each pair is
    /// two matches); otherwise throws <see cref="ArgumentException"/> with a clear message.
    /// </summary>
    public static int PairCount(int matchCount)
    {
        if (matchCount < 2 || matchCount % 2 != 0)
        {
            throw new ArgumentException("Paired matches need an even match count of at least 2 (each pair is two matches with the same seed, sides swapped); got "
                + matchCount + ".");
        }

        return matchCount / 2;
    }

    /// <summary>
    /// Plays <paramref name="pairs"/> pairs. Pair i uses seed <paramref name="firstSeed"/> + i for both matches: first
    /// <paramref name="teamA"/> at home, then <paramref name="teamB"/> at home. Matches are yielded in play order.
    /// </summary>
    public static IEnumerable<PairedGame> Play(
        GameData data,
        PairedTeam teamA,
        PairedTeam teamB,
        ulong firstSeed,
        int pairs,
        int maxStepsPerSegment = MatchCommand.DefaultMaxStepsPerSegment,
        int maxSegments = MatchCommand.DefaultMaxSegments)
    {
        if (pairs < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pairs), "At least one pair is needed.");
        }

        var aHome = new MatchSetup(data.Rink, data.Tuning, teamA.Team, teamB.Team, maxStepsPerSegment, maxSegments);
        var bHome = new MatchSetup(data.Rink, data.Tuning, teamB.Team, teamA.Team, maxStepsPerSegment, maxSegments);
        for (int i = 0; i < pairs; i++)
        {
            ulong seed = firstSeed + (ulong)i;
            yield return new PairedGame(i, seed, true, aHome, MatchSimulator.Run(aHome, teamA.NewCoach(), teamB.NewCoach(), new Pcg32(seed)));
            yield return new PairedGame(i, seed, false, bHome, MatchSimulator.Run(bHome, teamB.NewCoach(), teamA.NewCoach(), new Pcg32(seed)));
        }
    }
}

/// <summary>
/// Results of a paired series split two ways: by venue (home vs away) and by team (A vs B, regardless of venue).
/// </summary>
public sealed class PairedRecord
{
    /// <summary>Matches added.</summary>
    public int Matches { get; private set; }

    /// <summary>Home wins.</summary>
    public int HomeWins { get; private set; }

    /// <summary>Away wins.</summary>
    public int AwayWins { get; private set; }

    /// <summary>Ties.</summary>
    public int Ties => Matches - HomeWins - AwayWins;

    /// <summary>Goals by the home team.</summary>
    public int HomeGoals { get; private set; }

    /// <summary>Goals by the away team.</summary>
    public int AwayGoals { get; private set; }

    /// <summary>Team A wins (any venue).</summary>
    public int TeamAWins { get; private set; }

    /// <summary>Team B wins (any venue).</summary>
    public int TeamBWins { get; private set; }

    /// <summary>Team A goals.</summary>
    public int TeamAGoals { get; private set; }

    /// <summary>Team B goals.</summary>
    public int TeamBGoals { get; private set; }

    /// <summary>Team A wins at home.</summary>
    public int TeamAHomeWins { get; private set; }

    /// <summary>Team A wins away.</summary>
    public int TeamAAwayWins { get; private set; }

    /// <summary>Team B wins at home.</summary>
    public int TeamBHomeWins { get; private set; }

    /// <summary>Team B wins away.</summary>
    public int TeamBAwayWins { get; private set; }

    /// <summary>Adds one match.</summary>
    public void Add(PairedGame game)
    {
        Matches++;
        int home = game.Result.HomeGoals;
        int away = game.Result.AwayGoals;
        HomeGoals += home;
        AwayGoals += away;
        HomeWins += home > away ? 1 : 0;
        AwayWins += away > home ? 1 : 0;
        TeamAGoals += game.TeamAGoals;
        TeamBGoals += game.TeamBGoals;
        if (game.TeamAGoals > game.TeamBGoals)
        {
            TeamAWins++;
            TeamAHomeWins += game.TeamAIsHome ? 1 : 0;
            TeamAAwayWins += game.TeamAIsHome ? 0 : 1;
        }
        else if (game.TeamBGoals > game.TeamAGoals)
        {
            TeamBWins++;
            TeamBHomeWins += game.TeamAIsHome ? 0 : 1;
            TeamBAwayWins += game.TeamAIsHome ? 1 : 0;
        }
    }
}
