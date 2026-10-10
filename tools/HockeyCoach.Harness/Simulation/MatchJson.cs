using System.Text.Json;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.State;

namespace HockeyCoach.Harness.Simulation;

/// <summary>Writes a match as JSON: score, the event log (same event objects as <c>shift --json</c>), goal notes and period snapshots.</summary>
public static class MatchJson
{
    /// <summary>Serializes the match (indented JSON).</summary>
    public static string Write(MatchResult result, MatchSetup setup, ulong seed)
    {
        Func<int, TeamSide> teamOf = MatchCommand.TeamOf(setup);
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteNumber("seed", seed);
            w.WriteNumber("homeGoals", result.HomeGoals);
            w.WriteNumber("awayGoals", result.AwayGoals);
            w.WriteBoolean("stalled", result.Stalled);
            w.WriteStartArray("events");
            foreach (SimEvent e in result.Log.Events)
            {
                EventJson.WriteEvent(w, e);
            }

            w.WriteEndArray();
            w.WriteStartArray("goals");
            foreach (GoalNote note in result.State.GoalNotes)
            {
                w.WriteStartObject();
                w.WriteNumber("period", note.Period);
                w.WriteNumber("time", note.Time);
                w.WriteString("team", note.Team.ToString());
                w.WriteNumber("shooterId", note.ShooterId);
                w.WriteString("chanceType", note.ChanceType.ToString());
                w.WriteNumber("defenceOrganization", note.DefenceOrganization);
                w.WriteString("entry", note.HasEntry ? note.Entry.ToString() : null);
                w.WriteBoolean("royalRoad", note.RoyalRoad);
                w.WriteBoolean("screen", note.Screen);
                w.WriteBoolean("crease", note.Crease);
                w.WriteBoolean("secondChance", note.SecondChance);
                w.WriteString("explanation", MatchText.Explain(note));
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteStartArray("periods");
            foreach (PeriodSnapshot period in result.Periods)
            {
                List<SimEvent> events = result.Log.Events.Where(e => e.Context.Period == period.Period).ToList();
                TeamStats[] stats = TeamStats.From(events, teamOf);
                w.WriteStartObject();
                w.WriteNumber("period", period.Period);
                for (int t = 0; t < 2; t++)
                {
                    w.WriteStartObject(t == 0 ? "home" : "away");
                    TeamStats s = stats[t];
                    w.WriteNumber("goals", s.Goals);
                    w.WriteNumber("shots", s.Shots);
                    w.WriteNumber("onTarget", s.OnTarget);
                    w.WriteNumber("blocked", s.Blocked);
                    w.WriteNumber("missed", s.Missed);
                    w.WriteNumber("xg", s.Xg);
                    w.WriteNumber("passes", s.Passes);
                    w.WriteNumber("passesCompleted", s.PassesCompleted);
                    w.WriteNumber("passesUnderPressure", s.PassesUnderPressure);
                    w.WriteNumber("passesUnderPressureCompleted", s.PassesUnderPressureCompleted);
                    w.WriteNumber("offensiveZoneSecondsCumulative", period.OffensiveZoneSeconds[t]);
                    w.WriteEndObject();
                }

                w.WriteStartArray("pressure");
                foreach (PressureSample sample in result.State.PressureSamples.Where(p => p.Period == period.Period))
                {
                    w.WriteStartObject();
                    w.WriteNumber("time", sample.Time);
                    w.WriteNumber("home", sample.Home);
                    w.WriteNumber("away", sample.Away);
                    w.WriteEndObject();
                }

                w.WriteEndArray();
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
