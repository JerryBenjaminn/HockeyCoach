using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>Paired matches (D-069): same seed and rosters twice per pair, sides swapped; count handling.</summary>
public class PairedMatchesTests
{
    private static List<PairedGame> PlayPairs(ulong firstSeed, int pairs)
    {
        (PairedTeam a, PairedTeam b) = MatchCommand.PairTeams(ShiftTestData.Data, "forecheck212", "trap122");
        return PairedMatches.Play(ShiftTestData.Data, a, b, firstSeed, pairs).ToList();
    }

    [Fact]
    public void Pairs_UseTheSameSeedTwice_AndSwapTheRosters()
    {
        List<PairedGame> games = PlayPairs(5, 2);

        Assert.Equal(4, games.Count);
        Assert.Equal(new ulong[] { 5, 5, 6, 6 }, games.Select(g => g.Seed));
        Assert.Equal(new[] { 0, 0, 1, 1 }, games.Select(g => g.Pair));
        Assert.Equal(new[] { true, false, true, false }, games.Select(g => g.TeamAIsHome));
        Assert.Same(games[0].Setup.Home, games[1].Setup.Away);
        Assert.Same(games[0].Setup.Away, games[1].Setup.Home);
        Assert.Equal("A", games[0].Setup.Home.Name);
        Assert.Equal("B", games[1].Setup.Home.Name);
    }

    [Fact]
    public void Pairs_SystemsTravelWithTheTeam()
    {
        List<PairedGame> games = PlayPairs(3, 1);

        Assert.Equal(new[] { "forecheck212", "trap122" }, SystemsByTeam(games[0]));
        Assert.Equal(new[] { "forecheck212", "trap122" }, SystemsByTeam(games[1]));
    }

    [Fact]
    public void Pairs_FirstMatch_EqualsASingleMatchWithTheSameSeedAndRosters()
    {
        PairedGame first = PlayPairs(9, 1)[0];
        MatchResult single = MatchCommand.Play(ShiftTestData.Data, first.Setup, "forecheck212", "trap122", 9);

        Assert.Equal(MatchJson.Write(single, first.Setup, 9), MatchJson.Write(first.Result, first.Setup, 9));
    }

    [Fact]
    public void Pairs_AreDeterministic()
    {
        string Logs() => string.Join("\n", PlayPairs(11, 1).Select(g => MatchJson.Write(g.Result, g.Setup, g.Seed)));

        Assert.Equal(Logs(), Logs());
    }

    [Fact]
    public void PairedRecord_SplitsVenueFromTeam()
    {
        List<PairedGame> games = PlayPairs(1, 2);
        var record = new PairedRecord();
        games.ForEach(record.Add);

        Assert.Equal(4, record.Matches);
        Assert.Equal(games.Sum(g => g.Result.HomeGoals), record.HomeGoals);
        Assert.Equal(games.Sum(g => g.Result.AwayGoals), record.AwayGoals);
        Assert.Equal(record.HomeGoals + record.AwayGoals, record.TeamAGoals + record.TeamBGoals);
        Assert.Equal(games.Sum(g => g.TeamAIsHome ? g.Result.HomeGoals : g.Result.AwayGoals), record.TeamAGoals);
        Assert.Equal(record.HomeWins + record.AwayWins, record.TeamAWins + record.TeamBWins);
        Assert.Equal(record.TeamAWins, record.TeamAHomeWins + record.TeamAAwayWins);
        Assert.Equal(record.TeamBWins, record.TeamBHomeWins + record.TeamBAwayWins);
        Assert.Equal(record.HomeWins, record.TeamAHomeWins + record.TeamBHomeWins);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(200, 100)]
    public void PairCount_HalvesAnEvenCount(int count, int pairs)
    {
        Assert.Equal(pairs, PairedMatches.PairCount(count));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void PairCount_RejectsOddOrTooSmallCounts(int count)
    {
        Assert.Throws<ArgumentException>(() => PairedMatches.PairCount(count));
    }

    [Fact]
    public void Parse_RejectsAnOddCountAboveOne_WithAClearError()
    {
        MatchCommand.Options? options = MatchCommand.Parse(new[] { "match", "--count", "3" }, out string? error);

        Assert.Null(options);
        Assert.Contains("even", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("4")]
    public void Parse_AcceptsOneOrAnEvenCount(string count)
    {
        MatchCommand.Options? options = MatchCommand.Parse(new[] { "match", "--count", count }, out string? error);

        Assert.NotNull(options);
        Assert.Null(error);
    }

    [Fact]
    public void Averages_ReportVenueAndTeamSplit_AndFullMatchShotsAndFaceoffs()
    {
        string text = MatchCommand.Run(ShiftTestData.Data, new MatchCommand.Options(1, "forecheck212", "trap122", false, 2));

        Assert.Contains("2 matches in 1 pairs", text, StringComparison.Ordinal);
        Assert.Contains("By venue: home wins ", text, StringComparison.Ordinal);
        Assert.Contains("By team: A wins ", text, StringComparison.Ordinal);
        Assert.Contains("shot attempts", text, StringComparison.Ordinal);
        Assert.Contains("shots on target", text, StringComparison.Ordinal);
        Assert.Contains("Faceoffs per match: ", text, StringComparison.Ordinal);
    }

    private static string[] SystemsByTeam(PairedGame game)
    {
        // Team A's and team B's system ids regardless of venue: a shot's context carries the defending team's system.
        Func<int, TeamSide> teamOf = MatchCommand.TeamOf(game.Setup);
        var bySide = new string?[2];
        foreach (ShotEvent shot in game.Result.Log.Events.OfType<ShotEvent>())
        {
            int defender = 1 - (int)teamOf(shot.ShooterId);
            bySide[defender] ??= shot.Context.SystemId;
        }

        int a = (int)game.TeamASide;
        return new[] { bySide[a]!, bySide[1 - a]! };
    }
}
