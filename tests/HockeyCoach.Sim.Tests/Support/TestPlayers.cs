using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tests.Support;

/// <summary>Hand-built players, teams and rinks for tests (D-018: no roles.json in M1).</summary>
internal static class TestPlayers
{
    public static Skater Skater(int id, int allStats = 10, Position position = Position.LeftWing)
    {
        return new Skater(id, "Skater" + id, position, SkaterStats.Uniform(allStats));
    }

    public static Goalie Goalie(int id, int allStats = 10)
    {
        return new Goalie(id, "Goalie" + id, GoalieStats.Uniform(allStats));
    }

    /// <summary>
    /// A team with <paramref name="trios"/> forward trios, <paramref name="pairs"/> defence pairs and one goalie,
    /// every player in his primary position. Ids start at <paramref name="firstId"/>.
    /// </summary>
    public static Team Team(string name, int firstId, int allStats = 10, int trios = 1, int pairs = 1)
    {
        int id = firstId;
        var skaters = new List<Skater>();
        var lines = new List<ForwardLine>();
        for (int t = 0; t < trios; t++)
        {
            Skater lw = Skater(id++, allStats, Position.LeftWing);
            Skater c = Skater(id++, allStats, Position.Center);
            Skater rw = Skater(id++, allStats, Position.RightWing);
            skaters.AddRange(new[] { lw, c, rw });
            lines.Add(new ForwardLine("F" + (t + 1), lw, c, rw));
        }

        var defence = new List<DefencePair>();
        for (int p = 0; p < pairs; p++)
        {
            Skater ld = Skater(id++, allStats, Position.LeftDefence);
            Skater rd = Skater(id++, allStats, Position.RightDefence);
            skaters.AddRange(new[] { ld, rd });
            defence.Add(new DefencePair("D" + (p + 1), ld, rd));
        }

        var goalies = new[] { Goalie(id, allStats) };
        return new Team(name, skaters, goalies, lines, defence);
    }

    /// <summary>A plain grid rink with zones split by thirds of the length and a single xG zone "none".</summary>
    public static Rink GridRink(int length, int width)
    {
        var nodes = new List<RinkNode>();
        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < width; y++)
            {
                nodes.Add(new RinkNode(x, y, ZoneOf(x, length), "none", false));
            }
        }

        int third = length / 3;
        var zones = new[]
        {
            new ZoneRange(RinkZone.Defensive, 0, third - 1),
            new ZoneRange(RinkZone.Neutral, third, length - third - 1),
            new ZoneRange(RinkZone.Offensive, length - third, length - 1),
        };
        var spots = new[] { new FaceoffSpot("center", new GridPoint(length / 2, width / 2)) };
        return new Rink(length, width, nodes, zones, new[] { "none" }, new GridPoint(0, width / 2), new GridPoint(length - 1, width / 2), spots);
    }

    private static RinkZone ZoneOf(int x, int length)
    {
        int third = length / 3;
        if (x < third)
        {
            return RinkZone.Defensive;
        }

        return x < length - third ? RinkZone.Neutral : RinkZone.Offensive;
    }
}
