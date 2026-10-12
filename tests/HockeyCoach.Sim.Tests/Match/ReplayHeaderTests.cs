using System.Text.Json;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tests.Shift;

namespace HockeyCoach.Sim.Tests.Match;

/// <summary>The replay header of the <c>match --json</c> export (D-087, Q-076).</summary>
public class ReplayHeaderTests
{
    private const ulong Seed = 42UL;

    private static MatchSetup Setup => MatchTestData.Setup;

    private static string Json() => MatchJson.Write(MatchTestData.Play(Seed), Setup, Seed);

    private static JsonElement Root(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Header_HasFormatVersionTwoAndPeriodLengthFromTuning()
    {
        JsonElement root = Root(Json());

        Assert.Equal(2, root.GetProperty("formatVersion").GetInt32());
        Assert.Equal(Setup.Tuning.Time.PeriodSeconds, root.GetProperty("periodSeconds").GetDouble());
        Assert.Equal(Setup.Tuning.Time.Periods, root.GetProperty("periodCount").GetInt32());
    }

    [Fact]
    public void Header_KeepsExistingTopLevelFieldsAndPutsHeaderBeforeEvents()
    {
        string[] names = Root(Json()).EnumerateObject().Select(p => p.Name).ToArray();

        Assert.Equal(
            new[] { "seed", "homeGoals", "awayGoals", "stalled", "formatVersion", "periodSeconds", "periodCount", "time", "roster", "rink", "events", "goals", "periods" },
            names);
    }

    [Fact]
    public void Time_ExportsExactlyTheTuningTimeCosts_WithTheTuningStructure()
    {
        JsonElement time = Root(Json()).GetProperty("time");

        Assert.Equal(new[] { "secondsPerAction", "setupSeconds", "regroupSeconds" }, time.EnumerateObject().Select(p => p.Name).ToArray());
        JsonElement actions = time.GetProperty("secondsPerAction");
        Assert.Equal(Setup.Tuning.Time.SecondsPerAction.Keys.ToArray(), actions.EnumerateObject().Select(p => p.Name).ToArray());
        foreach (KeyValuePair<string, double> action in Setup.Tuning.Time.SecondsPerAction)
        {
            Assert.Equal(action.Value, actions.GetProperty(action.Key).GetDouble());
        }

        Assert.Equal(Setup.Tuning.Time.SetupSeconds, time.GetProperty("setupSeconds").GetDouble());
        Assert.Equal(Setup.Tuning.Time.RegroupSeconds, time.GetProperty("regroupSeconds").GetDouble());
    }

    [Fact]
    public void Time_ContainsEveryCostTheReplayUses()
    {
        JsonElement actions = Root(Json()).GetProperty("time").GetProperty("secondsPerAction");

        foreach (string key in new[] { "faceoff", "skate", "pass", "shoot", "dumpIn", "loosePuck", "rebound", "systemStep" })
        {
            Assert.True(actions.TryGetProperty(key, out JsonElement value), key);
            Assert.True(value.GetDouble() >= 0, key);
        }
    }

    [Fact]
    public void Roster_ListsEveryDressedPlayerOfBothTeams_HomeFirstThenById()
    {
        JsonElement[] roster = Root(Json()).GetProperty("roster").EnumerateArray().ToArray();
        int homeCount = Setup.Home.Skaters.Count + Setup.Home.Goalies.Count;
        int awayCount = Setup.Away.Skaters.Count + Setup.Away.Goalies.Count;

        Assert.Equal(homeCount + awayCount, roster.Length);
        Assert.Equal(homeCount, roster.Count(p => p.GetProperty("team").GetString() == "Home"));
        Assert.Equal(awayCount, roster.Count(p => p.GetProperty("team").GetString() == "Away"));
        int[] ids = roster.Select(p => p.GetProperty("id").GetInt32()).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        var expected = roster.OrderBy(p => p.GetProperty("team").GetString() == "Home" ? 0 : 1).ThenBy(p => p.GetProperty("id").GetInt32())
            .Select(p => p.GetProperty("id").GetInt32());
        Assert.Equal(expected, ids);
    }

    [Fact]
    public void Roster_GivesForwardsLineAndSlot_DefencePairAndSlot_GoaliesStarter()
    {
        Dictionary<int, JsonElement> roster = Root(Json()).GetProperty("roster").EnumerateArray().ToDictionary(p => p.GetProperty("id").GetInt32());
        foreach (Team team in new[] { Setup.Home, Setup.Away })
        {
            for (int i = 0; i < team.ForwardLines.Count; i++)
            {
                ForwardLine line = team.ForwardLines[i];
                AssertSkater(roster[line.LeftWing.Id], "LW", i + 1, null);
                AssertSkater(roster[line.Center.Id], "C", i + 1, null);
                AssertSkater(roster[line.RightWing.Id], "RW", i + 1, null);
            }

            for (int i = 0; i < team.DefencePairs.Count; i++)
            {
                DefencePair pair = team.DefencePairs[i];
                AssertSkater(roster[pair.LeftDefence.Id], "LD", null, i + 1);
                AssertSkater(roster[pair.RightDefence.Id], "RD", null, i + 1);
            }

            for (int i = 0; i < team.Goalies.Count; i++)
            {
                JsonElement g = roster[team.Goalies[i].Id];
                Assert.Equal("G", g.GetProperty("position").GetString());
                Assert.True(g.GetProperty("goalie").GetBoolean());
                Assert.Equal(JsonValueKind.Null, g.GetProperty("line").ValueKind);
                Assert.Equal(JsonValueKind.Null, g.GetProperty("pair").ValueKind);
                Assert.Equal(i == 0, g.GetProperty("starter").GetBoolean());
            }
        }
    }

    [Fact]
    public void Roster_ContainsEveryPlacedPlayer_WithMatchingTeamAndGoalieFlag()
    {
        Dictionary<int, JsonElement> roster = Root(Json()).GetProperty("roster").EnumerateArray().ToDictionary(p => p.GetProperty("id").GetInt32());
        MatchResult result = MatchTestData.Play(Seed);

        foreach (SimEvent e in result.Log.Events)
        {
            foreach (PlayerPlacement p in e.Context.Placement.Players)
            {
                Assert.True(roster.TryGetValue(p.PlayerId, out JsonElement entry), "player " + p.PlayerId + " missing from roster");
                Assert.Equal(p.Team.ToString(), entry.GetProperty("team").GetString());
                Assert.Equal(p.IsGoalie, entry.GetProperty("goalie").GetBoolean());
            }
        }
    }

    [Fact]
    public void Rink_NodesCoverTheGridInIdOrder_WithZones()
    {
        Rink rink = Setup.Rink;
        JsonElement json = Root(Json()).GetProperty("rink");
        int width = json.GetProperty("width").GetInt32();
        JsonElement[] nodes = json.GetProperty("nodes").EnumerateArray().ToArray();

        Assert.Equal(rink.Length, json.GetProperty("length").GetInt32());
        Assert.Equal(rink.Width, width);
        Assert.Equal(rink.Length * rink.Width, nodes.Length);
        for (int i = 0; i < nodes.Length; i++)
        {
            int x = nodes[i].GetProperty("x").GetInt32();
            int y = nodes[i].GetProperty("y").GetInt32();
            Assert.Equal(i, nodes[i].GetProperty("id").GetInt32());
            Assert.Equal(x * width + y, nodes[i].GetProperty("id").GetInt32());
            Assert.Equal(JsonNamingPolicy.CamelCase.ConvertName(rink.ZoneOf(i).ToString()), nodes[i].GetProperty("zone").GetString());
        }
    }

    [Fact]
    public void Rink_GoalsZonesAndFaceoffSpotsMatchTheRink()
    {
        Rink rink = Setup.Rink;
        JsonElement json = Root(Json()).GetProperty("rink");
        int width = json.GetProperty("width").GetInt32();

        Assert.Equal(rink.OwnGoal.X, json.GetProperty("ownGoal").GetProperty("x").GetInt32());
        Assert.Equal(rink.OwnGoal.Y, json.GetProperty("ownGoal").GetProperty("y").GetInt32());
        Assert.Equal(rink.OpponentGoal.X, json.GetProperty("opponentGoal").GetProperty("x").GetInt32());
        Assert.Equal(rink.OpponentGoal.Y, json.GetProperty("opponentGoal").GetProperty("y").GetInt32());

        JsonElement[] spots = json.GetProperty("faceoffSpots").EnumerateArray().ToArray();
        Assert.Equal(rink.FaceoffSpots.Count, spots.Length);
        for (int i = 0; i < spots.Length; i++)
        {
            FaceoffSpot spot = rink.FaceoffSpots[i];
            Assert.Equal(spot.Id, spots[i].GetProperty("id").GetString());
            Assert.Equal(spot.Point.X, spots[i].GetProperty("x").GetInt32());
            Assert.Equal(spot.Point.Y, spots[i].GetProperty("y").GetInt32());
            Assert.Equal(spot.Point.X * width + spot.Point.Y, spots[i].GetProperty("nodeId").GetInt32());
        }

        JsonElement[] zones = json.GetProperty("zones").EnumerateArray().ToArray();
        Assert.Equal(new[] { "defensive", "neutral", "offensive" }, zones.Select(z => z.GetProperty("id").GetString()));
        Assert.Equal(rink.Zones.Select(z => (z.XMin, z.XMax)), zones.Select(z => (z.GetProperty("xMin").GetInt32(), z.GetProperty("xMax").GetInt32())));

        JsonElement metres = json.GetProperty("metres");
        Assert.Equal(ReplayRinkDimensions.LengthMetres, metres.GetProperty("length").GetDouble());
        Assert.Equal(ReplayRinkDimensions.WidthMetres, metres.GetProperty("width").GetDouble());
        Assert.Equal(ReplayRinkDimensions.GoalLineFromEndMetres, metres.GetProperty("goalLineFromEnd").GetDouble());
        Assert.Equal(ReplayRinkDimensions.BlueLineFromEndMetres, metres.GetProperty("blueLineFromEnd").GetDouble());
    }

    [Fact]
    public void Rink_HomeGoalieStandsOnOwnGoalNode()
    {
        MatchResult result = MatchTestData.Play(Seed);
        int ownGoal = Setup.Rink.IdOf(Setup.Rink.OwnGoal.X, Setup.Rink.OwnGoal.Y);
        int opponentGoal = Setup.Rink.IdOf(Setup.Rink.OpponentGoal.X, Setup.Rink.OpponentGoal.Y);

        foreach (SimEvent e in result.Log.Events)
        {
            foreach (PlayerPlacement p in e.Context.Placement.Players.Where(p => p.IsGoalie))
            {
                Assert.Equal(p.Team == TeamSide.Home ? ownGoal : opponentGoal, p.NodeId);
            }
        }
    }

    [Fact]
    public void Events_AreUnchanged_EachMatchesEventJsonSerialization()
    {
        MatchResult result = MatchTestData.Play(Seed);
        JsonElement[] events = Root(Json()).GetProperty("events").EnumerateArray().ToArray();

        Assert.Equal(result.Log.Events.Count, events.Length);
        for (int i = 0; i < events.Length; i++)
        {
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream))
            {
                EventJson.WriteEvent(w, result.Log.Events[i]);
            }

            JsonElement expected = JsonDocument.Parse(stream.ToArray()).RootElement;
            Assert.True(JsonElement.DeepEquals(expected, events[i]), "event " + i + " differs");
        }
    }

    [Fact]
    public void MatchCommandJson_IsDeterministic()
    {
        var options = new MatchCommand.Options(Seed, "forecheck212", "trap122", Json: true, Count: 1, HomeCoach: "rulebased", AwayCoach: "random");

        string first = MatchCommand.Run(ShiftTestData.Data, options);
        string second = MatchCommand.Run(ShiftTestData.Data, options);

        Assert.Equal(first, second);
        Assert.Equal(2, Root(first).GetProperty("formatVersion").GetInt32());
    }

    private static void AssertSkater(JsonElement entry, string position, int? line, int? pair)
    {
        Assert.Equal(position, entry.GetProperty("position").GetString());
        Assert.False(entry.GetProperty("goalie").GetBoolean());
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("starter").ValueKind);
        AssertNullable(line, entry.GetProperty("line"));
        AssertNullable(pair, entry.GetProperty("pair"));
    }

    private static void AssertNullable(int? expected, JsonElement actual)
    {
        if (expected.HasValue)
        {
            Assert.Equal(expected.Value, actual.GetInt32());
        }
        else
        {
            Assert.Equal(JsonValueKind.Null, actual.ValueKind);
        }
    }
}
