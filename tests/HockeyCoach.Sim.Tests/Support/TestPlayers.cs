using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tests.Support;

/// <summary>Hand-built players and teams for tests (D-018: no roles.json in M1).</summary>
internal static class TestPlayers
{
    public static Skater Skater(int id, int allStats = 10, Position position = Position.Winger)
    {
        return new Skater(id, "Skater" + id, position, SkaterStats.Uniform(allStats));
    }

    public static Goalie Goalie(int id, int allStats = 10)
    {
        return new Goalie(id, "Goalie" + id, GoalieStats.Uniform(allStats));
    }

    /// <summary>A team with one line of 3 forwards + 2 defensemen and one goalie. Ids start at <paramref name="firstId"/>.</summary>
    public static Team Team(string name, int firstId, int allStats = 10)
    {
        var skaters = new[]
        {
            Skater(firstId, allStats, Position.Center),
            Skater(firstId + 1, allStats, Position.Winger),
            Skater(firstId + 2, allStats, Position.Winger),
            Skater(firstId + 3, allStats, Position.Defenseman),
            Skater(firstId + 4, allStats, Position.Defenseman),
        };
        var goalies = new[] { Goalie(firstId + 5, allStats) };
        var lines = new[] { new Line("L1", skaters) };
        return new Team(name, skaters, goalies, lines);
    }

    /// <summary>A plain grid rink with zones split by thirds of the length; xG zone is "none".</summary>
    public static Rink GridRink(int length, int width)
    {
        var nodes = new List<RinkNode>();
        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < width; y++)
            {
                RinkZone zone = x * 3 < length ? RinkZone.Defensive : (x * 3 < length * 2 ? RinkZone.Neutral : RinkZone.Offensive);
                nodes.Add(new RinkNode(x, y, zone, "none", false));
            }
        }

        return new Rink(length, width, nodes);
    }
}
