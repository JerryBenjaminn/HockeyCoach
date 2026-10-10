using System.Text.Json;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.Harness.Simulation;

/// <summary>Writes a shift's event log as JSON: one object per event with the common context and the event's fields.</summary>
public static class EventJson
{
    /// <summary>Serializes the result (indented JSON).</summary>
    public static string Write(ShiftResult result, ulong seed)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("seed", seed);
            writer.WriteString("endReason", result.EndReason.ToString());
            writer.WriteNumber("homeGoals", result.HomeGoals);
            writer.WriteNumber("awayGoals", result.AwayGoals);
            writer.WriteNumber("endTime", result.EndTime);
            writer.WriteStartArray("events");
            foreach (SimEvent e in result.Log.Events)
            {
                WriteEvent(writer, e);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Writes one event object (common context, the event's fields and the placement).</summary>
    public static void WriteEvent(Utf8JsonWriter w, SimEvent e)
    {
        EventContext c = e.Context;
        w.WriteStartObject();
        w.WriteString("type", e.GetType().Name.Replace("Event", string.Empty, StringComparison.Ordinal));
        w.WriteNumber("period", c.Period);
        w.WriteNumber("time", c.Time);
        w.WriteString("strength", c.Strength.ToString());
        w.WriteString("playId", c.PlayId);
        w.WriteString("systemId", c.SystemId);
        e.Accept(new Fields(w));
        w.WriteStartObject("placement");
        if (c.Placement.PuckCarrierId.HasValue)
        {
            w.WriteNumber("puckCarrierId", c.Placement.PuckCarrierId.Value);
        }
        else
        {
            w.WriteNull("puckCarrierId");
        }

        w.WriteNumber("puckNodeId", c.Placement.PuckNodeId);
        w.WriteStartArray("players");
        foreach (PlayerPlacement p in c.Placement.Players)
        {
            w.WriteStartObject();
            w.WriteNumber("id", p.PlayerId);
            w.WriteString("team", p.Team.ToString());
            w.WriteNumber("nodeId", p.NodeId);
            w.WriteBoolean("goalie", p.IsGoalie);
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private sealed class Fields : ISimEventVisitor<bool>
    {
        private readonly Utf8JsonWriter _w;

        public Fields(Utf8JsonWriter w)
        {
            _w = w;
        }

        public bool Visit(FaceoffEvent e)
        {
            _w.WriteNumber("homeCentreId", e.HomeCentreId);
            _w.WriteNumber("awayCentreId", e.AwayCentreId);
            _w.WriteString("winner", e.Winner.ToString());
            _w.WriteString("location", e.Location.ToString());
            return true;
        }

        public bool Visit(ControlledZoneEntryEvent e)
        {
            _w.WriteNumber("carrierId", e.CarrierId);
            _w.WriteString("numbers", e.Numbers.ToString());
            _w.WriteString("method", e.Method.ToString());
            _w.WriteString("outcome", e.Outcome.ToString());
            return true;
        }

        public bool Visit(DumpInEvent e)
        {
            _w.WriteNumber("shooterId", e.ShooterId);
            _w.WriteString("battleOutcome", e.BattleOutcome.ToString());
            return true;
        }

        public bool Visit(PassEvent e)
        {
            _w.WriteNumber("passerId", e.PasserId);
            _w.WriteNumber("receiverId", e.ReceiverId);
            _w.WriteBoolean("underPressure", e.UnderPressure);
            _w.WriteBoolean("succeeded", e.Succeeded);
            return true;
        }

        public bool Visit(ShotEvent e)
        {
            _w.WriteNumber("shooterId", e.ShooterId);
            if (e.StoppedById.HasValue)
            {
                _w.WriteNumber("stoppedById", e.StoppedById.Value);
            }
            else
            {
                _w.WriteNull("stoppedById");
            }

            _w.WriteNumber("xg", e.Xg);
            _w.WriteString("chanceClass", e.ChanceClass?.ToString());
            _w.WriteString("chanceType", e.ChanceType.ToString());
            _w.WriteBoolean("underPressure", e.UnderPressure);
            _w.WriteNull("speed");
            _w.WriteString("outcome", e.Outcome.ToString());
            return true;
        }

        public bool Visit(TurnoverEvent e)
        {
            _w.WriteNumber("loserId", e.LoserId);
            _w.WriteNumber("takerId", e.TakerId);
            _w.WriteNumber("nodeId", e.NodeId);
            return true;
        }

        public bool Visit(PuckBattleEvent e)
        {
            _w.WriteStartArray("attackerIds");
            foreach (int id in e.AttackerIds)
            {
                _w.WriteNumberValue(id);
            }

            _w.WriteEndArray();
            _w.WriteStartArray("defenderIds");
            foreach (int id in e.DefenderIds)
            {
                _w.WriteNumberValue(id);
            }

            _w.WriteEndArray();
            _w.WriteString("outcome", e.Outcome.ToString());
            return true;
        }

        public bool Visit(StoppageEvent e)
        {
            _w.WriteString("reason", e.Reason.ToString());
            return true;
        }

        public bool Visit(LineChangeEvent e)
        {
            _w.WriteStartArray("outgoingIds");
            foreach (int id in e.OutgoingIds)
            {
                _w.WriteNumberValue(id);
            }

            _w.WriteEndArray();
            _w.WriteStartArray("incomingIds");
            foreach (int id in e.IncomingIds)
            {
                _w.WriteNumberValue(id);
            }

            _w.WriteEndArray();
            return true;
        }
    }
}
