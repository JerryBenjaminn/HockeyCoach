using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>Q-057 (c): rosters derived from the pair seed, and the mirror-roster option.</summary>
public class PairRosterTests
{
    private static string Stats(Team team) =>
        string.Join(";", team.Skaters.Select(s => string.Join(",", Enum.GetValues<SkaterStat>().Select(st => s.Stats[st])))
            .Concat(team.Goalies.Select(g => string.Join(",", Enum.GetValues<GoalieStat>().Select(st => g.Stats[st])))));

    [Fact]
    public void Rosters_AreReproducible_DifferBetweenPairs_AndBetweenTeams()
    {
        (Team a1, Team b1) = HarnessTeams.PairRosters(7UL, false);
        (Team a2, Team b2) = HarnessTeams.PairRosters(7UL, false);
        (Team a3, _) = HarnessTeams.PairRosters(8UL, false);

        Assert.Equal(Stats(a1), Stats(a2));
        Assert.Equal(Stats(b1), Stats(b2));
        Assert.NotEqual(Stats(a1), Stats(b1));
        Assert.NotEqual(Stats(a1), Stats(a3));
        Assert.Equal(1, a1.Skaters[0].Id);
        Assert.Equal(101, b1.Skaters[0].Id);
    }

    [Fact]
    public void Mirror_GivesBothTeamsTheSameStats()
    {
        (Team a, Team b) = HarnessTeams.PairRosters(7UL, true);

        Assert.Equal(Stats(a), Stats(b));
        Assert.Equal(Stats(HarnessTeams.PairRosters(7UL, false).A), Stats(a));
        Assert.NotEqual(a.Skaters[0].Id, b.Skaters[0].Id);
    }

    [Fact]
    public void PairedMatches_UseOneRosterPairPerPair()
    {
        (PairedTeam a, PairedTeam b) = MatchCommand.PairTeams(ShiftTestData.Data, "forecheck212", "trap122");
        List<PairedGame> games = PairedMatches.Play(ShiftTestData.Data, a, b, 5UL, 2).ToList();

        Assert.Same(games[0].Setup.Home, games[1].Setup.Away);
        Assert.Equal(Stats(HarnessTeams.PairRosters(5UL, false).A), Stats(games[0].Setup.Home));
        Assert.Equal(Stats(HarnessTeams.PairRosters(6UL, false).B), Stats(games[2].Setup.Away));
        Assert.NotEqual(Stats(games[0].Setup.Home), Stats(games[2].Setup.Home));

        List<PairedGame> mirrored = PairedMatches.Play(ShiftTestData.Data, a, b, 5UL, 1, mirror: true).ToList();
        Assert.Equal(Stats(mirrored[0].Setup.Home), Stats(mirrored[0].Setup.Away));
    }

    [Fact]
    public void Parse_AcceptsMirror()
    {
        Assert.True(MatchCommand.Parse(new[] { "match", "--mirror", "--count", "2" }, out _)!.Mirror);
    }
}
