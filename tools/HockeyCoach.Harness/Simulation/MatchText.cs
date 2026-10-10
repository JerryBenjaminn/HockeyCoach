using System.Globalization;
using System.Text;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.State;
using static HockeyCoach.Harness.Simulation.MatchCommand;

namespace HockeyCoach.Harness.Simulation;

/// <summary>Readable text of a full match: header, event lines, goal explanations, period summaries and match totals.</summary>
public static class MatchText
{
    private const string Sparkline = " .:-=+*#%@";

    /// <summary>The whole output of one match.</summary>
    public static string Write(MatchResult result, MatchSetup setup, GameData data, MatchCommand.Options options)
    {
        Func<int, TeamSide> teamOf = TeamOf(setup);
        Dictionary<int, string> names = Names(setup);
        var text = new EventText(id => names.TryGetValue(id, out string? n) ? n : "#" + id);
        var sb = new StringBuilder();
        sb.AppendLine("Match, seed " + options.Seed.ToString(CultureInfo.InvariantCulture) + ": Home (" + options.HomeSystem + ") vs Away (" + options.AwaySystem
            + "). Coaches: rotation (trios 1→4, pairs 1→3, least-used play first), transition rush, system-mode defaults.");
        foreach (Team team in new[] { setup.Home, setup.Away })
        {
            sb.AppendLine(team.Name + ": " + string.Join(" | ", team.ForwardLines.Select(l => l.Name + " " + string.Join(",", l.Skaters.Select(s => "#" + s.Id))))
                + " | " + string.Join(" | ", team.DefencePairs.Select(p => p.Name + " " + string.Join(",", p.Skaters.Select(s => "#" + s.Id))))
                + " | G #" + team.Goalies[0].Id);
        }

        sb.AppendLine("Plays: " + string.Join(", ", data.Plays.Select(p => p.Id)));
        sb.AppendLine();
        int goalIndex = 0;
        int period = 0;
        foreach (SimEvent e in result.Log.Events)
        {
            if (e.Context.Period != period)
            {
                if (period > 0)
                {
                    AppendPeriod(sb, result, setup, period, teamOf, names);
                }

                period = e.Context.Period;
                sb.AppendLine("=== Period " + period + " ===");
            }

            sb.AppendLine(text.Line(e));
            if (e is ShotEvent { Outcome: ShotOutcome.Goal } && goalIndex < result.State.GoalNotes.Count)
            {
                sb.AppendLine("    GOAL: " + Explain(result.State.GoalNotes[goalIndex++]));
            }
        }

        if (period > 0)
        {
            AppendPeriod(sb, result, setup, period, teamOf, names);
        }

        sb.AppendLine("=== Match ===");
        sb.AppendLine("Final: Home " + result.HomeGoals + " - " + result.AwayGoals + " Away" + (result.Stalled ? " (STALLED: anti-stall cap reached)" : string.Empty));
        AppendStats(sb, TeamStats.From(result.Log.Events, teamOf), result.Log.Events, result.Periods.Count > 0 ? result.Periods[^1] : null, null, setup);
        return sb.ToString();
    }

    /// <summary>The one-sentence goal explanation (approved milestone 3 plan).</summary>
    public static string Explain(GoalNote note)
    {
        var parts = new List<string> { TypeName(note.ChanceType) };
        if (note.HasEntry)
        {
            parts[0] += " " + note.Entry;
        }

        parts.Add("defence " + (100.0 * note.DefenceOrganization).ToString("0", CultureInfo.InvariantCulture) + " % organized");
        if (note.SecondChance)
        {
            parts.Add("second chance");
        }

        if (note.Crease)
        {
            parts.Add("from the crease");
        }

        if (note.RoyalRoad)
        {
            parts.Add("Royal Road");
        }

        if (note.Screen)
        {
            parts.Add("screened");
        }

        return string.Join(", ", parts) + ".";
    }

    private static string TypeName(ChanceType type) => type switch
    {
        ChanceType.Rush => "Rush",
        ChanceType.Turnover => "Off a turnover",
        ChanceType.Faceoff => "Off the faceoff",
        ChanceType.OffensiveZone => "Zone play",
        _ => type.ToString(),
    };

    private static void AppendPeriod(StringBuilder sb, MatchResult result, MatchSetup setup, int period, Func<int, TeamSide> teamOf, Dictionary<int, string> names)
    {
        List<SimEvent> events = result.Log.Events.Where(e => e.Context.Period == period).ToList();
        TeamStats[] stats = TeamStats.From(events, teamOf);
        PeriodSnapshot? snapshot = result.Periods.FirstOrDefault(p => p.Period == period);
        PeriodSnapshot? previous = result.Periods.FirstOrDefault(p => p.Period == period - 1);
        int home = result.Log.Events.OfType<ShotEvent>().Count(s => s.Outcome == ShotOutcome.Goal && s.Context.Period <= period && teamOf(s.ShooterId) == TeamSide.Home);
        int away = result.Log.Events.OfType<ShotEvent>().Count(s => s.Outcome == ShotOutcome.Goal && s.Context.Period <= period && teamOf(s.ShooterId) == TeamSide.Away);
        sb.AppendLine("--- Period " + period + " summary --- Score Home " + home + " - " + away + " Away (period " + stats[0].Goals + "-" + stats[1].Goals + ")");
        AppendStats(sb, stats, events, snapshot, previous, setup);
        string spark = PressureLine(result.State.PressureSamples.Where(p => p.Period == period).ToList());
        sb.AppendLine("  Pressure per 60 s (home / away): " + spark);
        sb.AppendLine();
    }

    private static void AppendStats(StringBuilder sb, TeamStats[] stats, IEnumerable<SimEvent> events, PeriodSnapshot? snapshot, PeriodSnapshot? previous, MatchSetup setup)
    {
        string[] side = { "Home", "Away" };
        for (int t = 0; t < 2; t++)
        {
            TeamStats s = stats[t];
            sb.AppendLine("  " + side[t] + ": shots " + s.Shots + " (on target " + s.OnTarget + ", blocked " + s.Blocked + ", missed " + s.Missed + "), xG " + F2(s.Xg)
                + ", goals " + s.Goals + "; by type " + string.Join(", ", s.ShotsByType.OrderBy(k => k.Key).Select(k =>
                    k.Key + " " + k.Value + "/" + F2(s.XgByType.GetValueOrDefault(k.Key)) + "/" + s.GoalsByType.GetValueOrDefault(k.Key))));
            sb.AppendLine("        passes " + Pct(s.PassesCompleted, s.Passes) + " of " + s.Passes + ", under pressure " + Pct(s.PassesUnderPressureCompleted, s.PassesUnderPressure)
                + " of " + s.PassesUnderPressure + "; entries carry " + s.CarryEntriesKept + "/" + s.CarryEntries + ", pass " + s.PassEntriesKept + "/" + s.PassEntries
                + " (" + string.Join(", ", s.EntryNumbers.Select(e => e.Key + " x" + e.Value)) + "), dumps " + s.Dumps);
            sb.AppendLine("        battles W/NW/L " + s.BattlesWon + "/" + s.BattlesNoWinner + "/" + s.BattlesLost + ", faceoffs " + s.FaceoffsWon + "/" + s.Faceoffs
                + ", takeaways " + s.Takeaways);
            if (snapshot != null)
            {
                double zone = snapshot.OffensiveZoneSeconds[t] - (previous?.OffensiveZoneSeconds[t] ?? 0.0);
                Team team = t == 0 ? setup.Home : setup.Away;
                var units = new List<string>();
                for (int i = 0; i < team.ForwardLines.Count; i++)
                {
                    units.Add(UnitText(team.ForwardLines[i].Name, team.ForwardLines[i].Skaters, snapshot.ForwardSeconds[t][i] - (previous?.ForwardSeconds[t][i] ?? 0.0),
                        snapshot.ForwardShifts[t][i] - (previous?.ForwardShifts[t][i] ?? 0), snapshot));
                }

                for (int i = 0; i < team.DefencePairs.Count; i++)
                {
                    units.Add(UnitText(team.DefencePairs[i].Name, team.DefencePairs[i].Skaters, snapshot.PairSeconds[t][i] - (previous?.PairSeconds[t][i] ?? 0.0),
                        snapshot.PairShifts[t][i] - (previous?.PairShifts[t][i] ?? 0), snapshot));
                }

                sb.AppendLine("        offensive-zone time " + Clock(zone) + "; ice time / shifts / avg shift / energy at end: " + string.Join("; ", units));
                sb.AppendLine("        play uses (familiarity counter): " + string.Join(", ", snapshot.PlayUses[t].Select(u => u.Key + " " + u.Value.ToString("0.#", CultureInfo.InvariantCulture))));
            }
        }

        var stoppages = new SortedDictionary<string, int>(StringComparer.Ordinal);
        double gapSum = 0.0;
        int gaps = 0;
        CountStoppages(events, stoppages, ref gapSum, ref gaps);
        sb.AppendLine("  Stoppages: " + string.Join(", ", stoppages.Select(s => s.Key + " " + s.Value)) + "; mean time between stoppages " + F1(gaps == 0 ? 0 : gapSum / gaps) + " s");
    }

    private static string UnitText(string name, IReadOnlyList<Skater> skaters, double seconds, int shifts, PeriodSnapshot snapshot)
    {
        double energy = skaters.Average(s => snapshot.Energy[s.Id]);
        return name + " " + Clock(seconds) + "/" + shifts + "/" + F1(shifts == 0 ? 0 : seconds / shifts) + "s/" + F2(energy);
    }

    /// <summary>Counts stoppages by reason and the time from each period start or stoppage to the next stoppage.</summary>
    public static void CountStoppages(IEnumerable<SimEvent> events, SortedDictionary<string, int> byReason, ref double gapSum, ref int gaps)
    {
        int period = 0;
        double last = 0.0;
        foreach (StoppageEvent stoppage in events.OfType<StoppageEvent>())
        {
            if (stoppage.Context.Period != period)
            {
                period = stoppage.Context.Period;
                last = 0.0;
            }

            string key = stoppage.Reason.ToString();
            byReason[key] = byReason.GetValueOrDefault(key) + 1;
            gapSum += stoppage.Context.Time - last;
            gaps++;
            last = stoppage.Context.Time;
        }
    }

    private static string PressureLine(List<PressureSample> samples)
    {
        string Row(Func<PressureSample, double> value) => new(samples.Select(s => Sparkline[Math.Min(Sparkline.Length - 1, (int)(value(s) * Sparkline.Length))]).ToArray());
        return "[" + Row(s => s.Home) + "] / [" + Row(s => s.Away) + "]";
    }

    /// <summary>mm:ss.</summary>
    public static string Clock(double seconds)
    {
        int whole = (int)Math.Round(seconds);
        return (whole / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (whole % 60).ToString("00", CultureInfo.InvariantCulture);
    }

    private static Dictionary<int, string> Names(MatchSetup setup)
    {
        var names = new Dictionary<int, string>();
        foreach ((Team team, string prefix) in new[] { (setup.Home, "H"), (setup.Away, "A") })
        {
            foreach (ForwardLine line in team.ForwardLines)
            {
                foreach (Skater s in line.Skaters)
                {
                    names[s.Id] = prefix + "-" + line.Name + "-" + Positions.ToName(line.SlotOf(s)!.Value);
                }
            }

            foreach (DefencePair pair in team.DefencePairs)
            {
                foreach (Skater s in pair.Skaters)
                {
                    names[s.Id] = prefix + "-" + pair.Name + "-" + Positions.ToName(pair.SlotOf(s)!.Value);
                }
            }

            names[team.Goalies[0].Id] = prefix + "-G";
        }

        return names;
    }
}
