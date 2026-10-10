using System.Globalization;
using System.Text;
using System.Text.Json;
using HockeyCoach.AI;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.State;

namespace HockeyCoach.Harness.Simulation;

/// <summary>
/// <c>match</c>: plays a full match with the default <see cref="RotationCoach"/> on both sides and prints it as text
/// (events, goal explanations, period and match summaries) or JSON. With <c>--count N</c> it plays N matches (seeds
/// seed … seed + N − 1) and prints only the averages and the target comparison; raw match data is never written to the
/// repository (CLAUDE.md rule 5). The full <c>batch</c> report comes in milestone 4.
/// </summary>
public static class MatchCommand
{
    /// <summary>Default anti-stall cap per segment (D-043, a caller parameter).</summary>
    public const int DefaultMaxStepsPerSegment = 5000;

    /// <summary>Default anti-stall cap on segments per match (D-043).</summary>
    public const int DefaultMaxSegments = 2000;

    /// <summary>Options of the command.</summary>
    /// <param name="Seed">Random seed (first seed with --count).</param>
    /// <param name="HomeSystem">Home defensive system id.</param>
    /// <param name="AwaySystem">Away defensive system id.</param>
    /// <param name="Json">Print JSON instead of text (single match).</param>
    /// <param name="Count">Number of matches; above 1 prints averages only.</param>
    public sealed record Options(ulong Seed, string HomeSystem, string AwaySystem, bool Json, int Count);

    /// <summary>The match setup used by the command: harness teams (home ids from 1, away from 101) with the given caps.</summary>
    public static MatchSetup Setup(GameData data, int maxStepsPerSegment = DefaultMaxStepsPerSegment, int maxSegments = DefaultMaxSegments)
    {
        Team home = HarnessTeams.BuildMatchTeam("Home", 1, 1UL);
        Team away = HarnessTeams.BuildMatchTeam("Away", 101, 2UL);
        return new MatchSetup(data.Rink, data.Tuning, home, away, maxStepsPerSegment, maxSegments);
    }

    /// <summary>Plays one match with rotation coaches.</summary>
    public static MatchResult Play(GameData data, MatchSetup setup, string homeSystem, string awaySystem, ulong seed)
    {
        var homeCoach = new RotationCoach(data.Plays, HarnessTeams.System(data, homeSystem));
        var awayCoach = new RotationCoach(data.Plays, HarnessTeams.System(data, awaySystem));
        return MatchSimulator.Run(setup, homeCoach, awayCoach, new Pcg32(seed));
    }

    /// <summary>Runs the command and returns the printed output.</summary>
    public static string Run(GameData data, Options options)
    {
        MatchSetup setup = Setup(data);
        if (options.Count > 1)
        {
            return Averages(data, setup, options);
        }

        MatchResult result = Play(data, setup, options.HomeSystem, options.AwaySystem, options.Seed);
        return options.Json ? MatchJson.Write(result, setup, options.Seed) : MatchText.Write(result, setup, data, options);
    }

    /// <summary>Parses <c>match</c> arguments; returns null and an error message on bad input.</summary>
    public static Options? Parse(string[] args, out string? error)
    {
        error = null;
        ulong seed = 42;
        string home = "forecheck212";
        string away = "trap122";
        bool json = false;
        int count = 1;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed" when i + 1 < args.Length && ulong.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out ulong s):
                    seed = s;
                    i++;
                    break;
                case "--home-system" when i + 1 < args.Length:
                    home = args[++i];
                    break;
                case "--away-system" when i + 1 < args.Length:
                    away = args[++i];
                    break;
                case "--count" when i + 1 < args.Length && int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out int c) && c > 0:
                    count = c;
                    i++;
                    break;
                case "--json":
                    json = true;
                    break;
                case "--data":
                    i++;
                    break;
                default:
                    error = "Unknown or incomplete option " + args[i];
                    return null;
            }
        }

        return new Options(seed, home, away, json, count);
    }

    /// <summary>Player id → team side for a setup.</summary>
    public static Func<int, TeamSide> TeamOf(MatchSetup setup)
    {
        var map = new Dictionary<int, TeamSide>();
        foreach ((Team team, TeamSide side) in new[] { (setup.Home, TeamSide.Home), (setup.Away, TeamSide.Away) })
        {
            foreach (Skater s in team.Skaters)
            {
                map[s.Id] = side;
            }

            foreach (Goalie g in team.Goalies)
            {
                map[g.Id] = side;
            }
        }

        return id => map[id];
    }

    private static string Averages(GameData data, MatchSetup setup, Options options)
    {
        Func<int, TeamSide> teamOf = TeamOf(setup);
        var total = new TeamStats();
        var perSide = new[] { new TeamStats(), new TeamStats() };
        int stalled = 0;
        int homeWins = 0;
        int awayWins = 0;
        double zoneSeconds = 0.0;
        var stoppages = new SortedDictionary<string, int>(StringComparer.Ordinal);
        double stoppageGaps = 0.0;
        int gaps = 0;
        int events = 0;
        for (int i = 0; i < options.Count; i++)
        {
            MatchResult result = Play(data, setup, options.HomeSystem, options.AwaySystem, options.Seed + (ulong)i);
            stalled += result.Stalled ? 1 : 0;
            homeWins += result.HomeGoals > result.AwayGoals ? 1 : 0;
            awayWins += result.AwayGoals > result.HomeGoals ? 1 : 0;
            events += result.Log.Count;
            TeamStats[] stats = TeamStats.From(result.Log.Events, teamOf);
            for (int t = 0; t < 2; t++)
            {
                perSide[t].Add(stats[t]);
                total.Add(stats[t]);
                zoneSeconds += result.State.OffensiveZoneSeconds((TeamSide)t);
            }

            MatchText.CountStoppages(result.Log.Events, stoppages, ref stoppageGaps, ref gaps);
        }

        int n = options.Count;
        double teamMatches = 2.0 * n;
        var sb = new StringBuilder();
        sb.AppendLine("Match averages: " + n + " matches, seeds " + options.Seed + "–" + (options.Seed + (ulong)n - 1) + ", Home (" + options.HomeSystem + ") vs Away ("
            + options.AwaySystem + "), rotation coaches, transition rush.");
        sb.AppendLine("Stalled matches: " + stalled + ". Results: home " + homeWins + ", away " + awayWins + ", ties " + (n - homeWins - awayWins) + ". Events per match " + F1(events / (double)n) + ".");
        sb.AppendLine();
        double goals = total.Goals / (double)n;
        sb.AppendLine("Goals per match (both teams combined): " + F2(goals) + "   [target goalsPerMatch " + TargetText(data, "goalsPerMatch", goals) + "]");
        sb.AppendLine("Per team per match: shot attempts " + F1(total.Shots / teamMatches) + ", on target " + F1(total.OnTarget / teamMatches)
            + ", blocked " + F1(total.Blocked / teamMatches) + ", missed " + F1(total.Missed / teamMatches) + ", xG " + F2(total.Xg / teamMatches)
            + ", goals " + F2(total.Goals / teamMatches));
        sb.AppendLine("Home / away goals per match: " + F2(perSide[0].Goals / (double)n) + " / " + F2(perSide[1].Goals / (double)n));
        sb.AppendLine("Passes: " + Pct(total.PassesCompleted, total.Passes) + " complete (" + F1(total.Passes / teamMatches) + " per team per match), under pressure "
            + Pct(total.PassesUnderPressureCompleted, total.PassesUnderPressure) + " (" + Pct(total.PassesUnderPressure, total.Passes) + " of passes)");
        sb.AppendLine("Shots under pressure: " + Pct(total.ShotsUnderPressure, total.Shots));
        sb.AppendLine("Chance types (share of attempts / share of xG / goals per team per match):");
        foreach (ChanceType type in new[] { ChanceType.Rush, ChanceType.Turnover, ChanceType.OffensiveZone, ChanceType.Faceoff })
        {
            sb.AppendLine("  " + type.ToString().PadRight(14) + Pct(total.ShotsByType.GetValueOrDefault(type), total.Shots).PadLeft(7) + " / "
                + Pct(total.XgByType.GetValueOrDefault(type), total.Xg).PadLeft(7) + " / " + F2(total.GoalsByType.GetValueOrDefault(type) / teamMatches));
        }

        sb.AppendLine("Chance classes per team per match: top " + F2(total.ShotsByClass.GetValueOrDefault(ChanceClass.Top) / teamMatches)
            + ", good " + F2(total.ShotsByClass.GetValueOrDefault(ChanceClass.Good) / teamMatches)
            + ", moderate " + F2(total.ShotsByClass.GetValueOrDefault(ChanceClass.Moderate) / teamMatches));
        int battles = total.BattlesWon + total.BattlesNoWinner + total.BattlesLost;
        sb.AppendLine("Puck battles (win / no winner / loss): " + Pct(total.BattlesWon, battles) + " / " + Pct(total.BattlesNoWinner, battles) + " / " + Pct(total.BattlesLost, battles));
        sb.AppendLine("Zone entries kept: carry " + Pct(total.CarryEntriesKept, total.CarryEntries) + " (" + F1(total.CarryEntries / teamMatches) + " per team per match), pass "
            + Pct(total.PassEntriesKept, total.PassEntries) + " (" + F1(total.PassEntries / teamMatches) + "); dumps " + F1(total.Dumps / teamMatches));
        sb.AppendLine("Entry numbers: " + string.Join(", ", total.EntryNumbers.Select(e => e.Key + " " + Pct(e.Value, total.CarryEntries + total.PassEntries))));
        sb.AppendLine("Offensive-zone time per team per match: " + MatchText.Clock(zoneSeconds / teamMatches));
        sb.AppendLine("Stoppages per match: " + string.Join(", ", stoppages.Select(s => s.Key + " " + F1(s.Value / (double)n))) + "; mean time between stoppages "
            + F1(gaps == 0 ? 0.0 : stoppageGaps / gaps) + " s");
        sb.AppendLine();
        sb.AppendLine("Targets with a range in data/targets.json:");
        foreach (TargetMetric metric in data.Targets.Metrics.Where(m => m.Min.HasValue && m.Max.HasValue))
        {
            string value = metric.Name == "goalsPerMatch" ? F2(goals) : "not measured by match --count";
            sb.AppendLine("  " + metric.Name + " [" + metric.Min + ", " + metric.Max + "]: " + value
                + (metric.Name == "goalsPerMatch" ? " → " + Status(goals, metric) : string.Empty));
        }

        sb.AppendLine("Other metrics have no range yet (placeholder in targets.json).");
        return sb.ToString();
    }

    private static string TargetText(GameData data, string name, double value)
    {
        TargetMetric? metric = data.Targets.Metrics.FirstOrDefault(m => m.Name == name);
        return metric == null || !metric.Min.HasValue || !metric.Max.HasValue ? "no range" : metric.Min + "–" + metric.Max + ": " + Status(value, metric);
    }

    private static string Status(double value, TargetMetric metric)
    {
        return value < metric.Min!.Value ? "TOO LOW" : (value > metric.Max!.Value ? "TOO HIGH" : "OK");
    }

    internal static string F1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    internal static string F2(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);

    internal static string Pct(double part, double whole) => whole <= 0 ? "-" : (100.0 * part / whole).ToString("0.0", CultureInfo.InvariantCulture) + " %";
}
