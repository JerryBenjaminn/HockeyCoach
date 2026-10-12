using Newtonsoft.Json.Linq;

namespace HockeyCoach.Replay.Tests.Support;

/// <summary>
/// Builds small match JSON documents in the <c>match --json</c> format for unit tests: an 11 × 5 grid like
/// <c>data/rink.json</c> (home goal (1, 2) = node 7, away goal (9, 2) = node 47, centre spot node 27) and NHL metres.
/// Players: home skaters 1 and 2, home goalie 3, away skaters 101 and 102, away goalie 103; bench players 4 and 104.
/// </summary>
public sealed class SyntheticMatch
{
    public const int PeriodSeconds = 1200;

    private readonly JArray _events = new();

    public JObject Root { get; } = new()
    {
        ["seed"] = 1,
        ["homeGoals"] = 0,
        ["awayGoals"] = 0,
        ["stalled"] = false,
        ["formatVersion"] = 1,
        ["periodSeconds"] = PeriodSeconds,
        ["periodCount"] = 3,
    };

    public SyntheticMatch()
    {
        Root["roster"] = new JArray(
            Player(1, "Home", "C"), Player(2, "Home", "LD"), Player(3, "Home", "G"), Player(4, "Home", "LW"),
            Player(101, "Away", "C"), Player(102, "Away", "LD"), Player(103, "Away", "G"), Player(104, "Away", "LW"));
        Root["rink"] = Rink();
        Root["events"] = _events;
        Root["goals"] = new JArray();
    }

    /// <summary>The standard on-ice placement: skaters at the given nodes, goalies in their nets.</summary>
    public static Dictionary<int, int> OnIce(int home1, int home2, int away1, int away2) =>
        new() { [1] = home1, [2] = home2, [3] = 7, [101] = away1, [102] = away2, [103] = 47 };

    public SyntheticMatch Add(string type, int period, double time, int puckNode, int? carrier, Dictionary<int, int> players, JObject? fields = null)
    {
        var e = new JObject
        {
            ["type"] = type,
            ["period"] = period,
            ["time"] = time,
            ["strength"] = "5v5",
            ["playId"] = null,
            ["systemId"] = null,
        };
        if (fields != null)
        {
            foreach (KeyValuePair<string, JToken?> f in fields)
            {
                e[f.Key] = f.Value;
            }
        }

        var list = new JArray();
        foreach (KeyValuePair<int, int> p in players.OrderBy(p => p.Key))
        {
            list.Add(new JObject
            {
                ["id"] = p.Key,
                ["team"] = p.Key > 100 ? "Away" : "Home",
                ["nodeId"] = p.Value,
                ["goalie"] = p.Key % 100 == 3,
            });
        }

        e["placement"] = new JObject { ["puckCarrierId"] = carrier, ["puckNodeId"] = puckNode, ["players"] = list };
        _events.Add(e);
        return this;
    }

    public string Json => Root.ToString();

    public Core.ReplayTimeline Build() => Core.TimelineBuilder.Build(Core.ReplayJsonReader.Read(Json));

    private static JObject Player(int id, string team, string position) => new()
    {
        ["id"] = id,
        ["team"] = team,
        ["position"] = position,
        ["line"] = null,
        ["pair"] = null,
        ["goalie"] = position == "G",
        ["starter"] = position == "G" ? true : null,
    };

    private static JObject Rink()
    {
        var nodes = new JArray();
        for (int x = 0; x < 11; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                nodes.Add(new JObject { ["id"] = (x * 5) + y, ["x"] = x, ["y"] = y, ["zone"] = x <= 3 ? "defensive" : x <= 6 ? "neutral" : "offensive" });
            }
        }

        return new JObject
        {
            ["length"] = 11,
            ["width"] = 5,
            ["ownGoal"] = new JObject { ["x"] = 1, ["y"] = 2 },
            ["opponentGoal"] = new JObject { ["x"] = 9, ["y"] = 2 },
            ["nodes"] = nodes,
            ["faceoffSpots"] = new JArray(
                new JObject { ["id"] = "defensiveLeft", ["x"] = 2, ["y"] = 1, ["nodeId"] = 11 },
                new JObject { ["id"] = "center", ["x"] = 5, ["y"] = 2, ["nodeId"] = 27 },
                new JObject { ["id"] = "offensiveRight", ["x"] = 8, ["y"] = 3, ["nodeId"] = 43 }),
            ["zones"] = new JArray(
                new JObject { ["id"] = "defensive", ["xMin"] = 0, ["xMax"] = 3 },
                new JObject { ["id"] = "neutral", ["xMin"] = 4, ["xMax"] = 6 },
                new JObject { ["id"] = "offensive", ["xMin"] = 7, ["xMax"] = 10 }),
            ["metres"] = new JObject { ["length"] = 60.96, ["width"] = 25.91, ["goalLineFromEnd"] = 3.35, ["blueLineFromEnd"] = 22.86 },
        };
    }
}
