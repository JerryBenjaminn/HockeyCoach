using System.Text.Json;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Harness.Simulation;

/// <summary>
/// The replay header of the <c>match --json</c> export (D-087, Q-076): top-level fields written before
/// <c>"events"</c>. The event schema is unchanged. Fields:
/// <list type="bullet">
/// <item><c>formatVersion</c>: <see cref="FormatVersion"/>.</item>
/// <item><c>periodSeconds</c> and <c>periodCount</c>: <c>time.periodSeconds</c> and <c>time.periods</c> of the tuning
/// (<c>periodCount</c> so it does not clash with the existing <c>periods</c> snapshot array).</item>
/// <item><c>roster</c>: every player of both teams, Home first, then by id. Each entry is
/// <c>{id, team, position, line, pair, goalie, starter}</c>: <c>team</c> is <c>Home</c> / <c>Away</c> as in the events;
/// <c>position</c> is the slot the player fills in his forward line or defence pair (C, LW, RW, LD, RD; the primary
/// position if he is in no unit) or <c>G</c>; <c>line</c> is the 1-based forward line number and <c>pair</c> the 1-based
/// defence pair number, null when not applicable (goalies have both null); <c>starter</c> is true for the goalie
/// who starts the match (the team's first goalie, as in the simulator) and null for skaters.</item>
/// <item><c>rink</c>: grid <c>length</c> and <c>width</c>, <c>ownGoal</c> and <c>opponentGoal</c>, <c>nodes</c>
/// (<c>{id, x, y, zone}</c> in id order, id = x * width + y), <c>faceoffSpots</c> (<c>{id, x, y, nodeId}</c>),
/// <c>zones</c> (<c>{id, xMin, xMax}</c>) and <c>metres</c> (<see cref="ReplayRinkDimensions"/>, replay only, Q-077).
/// All coordinates are in the home team's frame, the same frame as the event placements: <c>ownGoal</c> is the home
/// team's own goal (the home goalie's node) and <c>opponentGoal</c> the away team's goal; zones are named from the
/// home team's view.</item>
/// </list>
/// </summary>
public static class ReplayHeader
{
    /// <summary>Version of the <c>match --json</c> format; bump when the header or layout changes.</summary>
    public const int FormatVersion = 1;

    /// <summary>Goalie position name in the roster.</summary>
    public const string GoaliePosition = "G";

    /// <summary>Writes the header fields into the open top-level object.</summary>
    public static void Write(Utf8JsonWriter w, MatchSetup setup)
    {
        w.WriteNumber("formatVersion", FormatVersion);
        w.WriteNumber("periodSeconds", setup.Tuning.Time.PeriodSeconds);
        w.WriteNumber("periodCount", setup.Tuning.Time.Periods);
        WriteRoster(w, setup);
        WriteRink(w, setup.Rink);
    }

    private static void WriteRoster(Utf8JsonWriter w, MatchSetup setup)
    {
        w.WriteStartArray("roster");
        foreach ((Team team, TeamSide side) in new[] { (setup.Home, TeamSide.Home), (setup.Away, TeamSide.Away) })
        {
            var entries = new List<(int Id, Action Write)>();
            foreach (Skater skater in team.Skaters)
            {
                entries.Add((skater.Id, () => WriteSkater(w, team, side, skater)));
            }

            for (int i = 0; i < team.Goalies.Count; i++)
            {
                Goalie goalie = team.Goalies[i];
                bool starter = i == 0;
                entries.Add((goalie.Id, () => WritePlayer(w, goalie.Id, side, GoaliePosition, null, null, true, starter)));
            }

            foreach ((int _, Action write) in entries.OrderBy(e => e.Id))
            {
                write();
            }
        }

        w.WriteEndArray();
    }

    private static void WriteSkater(Utf8JsonWriter w, Team team, TeamSide side, Skater skater)
    {
        Position position = skater.PrimaryPosition;
        int? line = null;
        int? pair = null;
        for (int i = 0; i < team.ForwardLines.Count && line == null; i++)
        {
            Position? slot = team.ForwardLines[i].SlotOf(skater);
            if (slot.HasValue)
            {
                position = slot.Value;
                line = i + 1;
            }
        }

        for (int i = 0; i < team.DefencePairs.Count && line == null && pair == null; i++)
        {
            Position? slot = team.DefencePairs[i].SlotOf(skater);
            if (slot.HasValue)
            {
                position = slot.Value;
                pair = i + 1;
            }
        }

        WritePlayer(w, skater.Id, side, Positions.ToName(position), line, pair, false, null);
    }

    private static void WritePlayer(Utf8JsonWriter w, int id, TeamSide side, string position, int? line, int? pair, bool goalie, bool? starter)
    {
        w.WriteStartObject();
        w.WriteNumber("id", id);
        w.WriteString("team", side.ToString());
        w.WriteString("position", position);
        WriteNullable(w, "line", line);
        WriteNullable(w, "pair", pair);
        w.WriteBoolean("goalie", goalie);
        if (starter.HasValue)
        {
            w.WriteBoolean("starter", starter.Value);
        }
        else
        {
            w.WriteNull("starter");
        }

        w.WriteEndObject();
    }

    private static void WriteNullable(Utf8JsonWriter w, string name, int? value)
    {
        if (value.HasValue)
        {
            w.WriteNumber(name, value.Value);
        }
        else
        {
            w.WriteNull(name);
        }
    }

    private static void WriteRink(Utf8JsonWriter w, Rink rink)
    {
        w.WriteStartObject("rink");
        w.WriteNumber("length", rink.Length);
        w.WriteNumber("width", rink.Width);
        WritePoint(w, "ownGoal", rink.OwnGoal);
        WritePoint(w, "opponentGoal", rink.OpponentGoal);
        w.WriteStartArray("nodes");
        for (int id = 0; id < rink.NodeCount; id++)
        {
            w.WriteStartObject();
            w.WriteNumber("id", id);
            w.WriteNumber("x", rink.XOf(id));
            w.WriteNumber("y", rink.YOf(id));
            w.WriteString("zone", ZoneName(rink.ZoneOf(id)));
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteStartArray("faceoffSpots");
        foreach (FaceoffSpot spot in rink.FaceoffSpots)
        {
            w.WriteStartObject();
            w.WriteString("id", spot.Id);
            w.WriteNumber("x", spot.Point.X);
            w.WriteNumber("y", spot.Point.Y);
            w.WriteNumber("nodeId", rink.IdOf(spot.Point.X, spot.Point.Y));
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteStartArray("zones");
        foreach (ZoneRange zone in rink.Zones)
        {
            w.WriteStartObject();
            w.WriteString("id", ZoneName(zone.Zone));
            w.WriteNumber("xMin", zone.XMin);
            w.WriteNumber("xMax", zone.XMax);
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteStartObject("metres");
        w.WriteNumber("length", ReplayRinkDimensions.LengthMetres);
        w.WriteNumber("width", ReplayRinkDimensions.WidthMetres);
        w.WriteNumber("goalLineFromEnd", ReplayRinkDimensions.GoalLineFromEndMetres);
        w.WriteNumber("blueLineFromEnd", ReplayRinkDimensions.BlueLineFromEndMetres);
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static void WritePoint(Utf8JsonWriter w, string name, GridPoint point)
    {
        w.WriteStartObject(name);
        w.WriteNumber("x", point.X);
        w.WriteNumber("y", point.Y);
        w.WriteEndObject();
    }

    /// <summary>The <c>rink.json</c> zone id (defensive, neutral, offensive).</summary>
    private static string ZoneName(RinkZone zone) => JsonNamingPolicy.CamelCase.ConvertName(zone.ToString());
}
