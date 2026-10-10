using System.Globalization;
using System.Text;
using HockeyCoach.Harness.Data;
using HockeyCoach.Sim.Events;

namespace HockeyCoach.Harness.Report;

/// <summary>Comparison status of one metric (D-071).</summary>
public enum TargetStatus
{
    /// <summary>Range filled and the value is inside it (bounds included).</summary>
    Ok,

    /// <summary>Value below <c>min</c>.</summary>
    TooLow,

    /// <summary>Value above <c>max</c>.</summary>
    TooHigh,

    /// <summary>Lowest value below <c>min</c> and highest value above <c>max</c> (only for a spread such as <c>shotXgTypical</c>).</summary>
    TooLowAndTooHigh,

    /// <summary>No full range (min or max null): value only.</summary>
    NoRange,

    /// <summary>The harness cannot compute the metric yet, or there were no events to compute it from.</summary>
    NotMeasured,
}

/// <summary>
/// A measured metric value. <see cref="Low"/> is compared with <c>min</c> and <see cref="High"/> with <c>max</c>; for a
/// single value both are the same. <see cref="Values"/> are the shown numbers (one, or the 10th, 50th and 90th
/// percentile for <c>shotXgTypical</c>, Q-053).
/// </summary>
/// <param name="Values">Values to show.</param>
/// <param name="Low">Value compared with min.</param>
/// <param name="High">Value compared with max.</param>
public sealed record MeasuredValue(IReadOnlyList<double> Values, double Low, double High)
{
    /// <summary>A single value.</summary>
    public static MeasuredValue Of(double value) => new(new[] { value }, value, value);
}

/// <summary>One row of the target comparison.</summary>
/// <param name="Metric">The target metric.</param>
/// <param name="Combined">Value for both teams together (scope rule), or null when not measured.</param>
/// <param name="Teams">Per-team values (slot 0 and 1), or null for perMatch metrics or when not measured.</param>
/// <param name="Status">Comparison result.</param>
/// <param name="Note">Why the metric is not measured, or null.</param>
public sealed record TargetRow(TargetMetric Metric, MeasuredValue? Combined, MeasuredValue?[]? Teams, TargetStatus Status, string? Note);

/// <summary>
/// Automatic target comparison (D-071, docs/data-schema.md targets.json "Automaattinen vertailu" and "Mittarit ja
/// niiden laskenta"): every metric with both min and max gets OK / TOO LOW / TOO HIGH, others show the value only.
/// </summary>
public static class TargetComparison
{
    /// <summary>Percentiles shown and compared for <c>shotXgTypical</c> (Q-053, designer recommendation 5).</summary>
    public static readonly double[] ShotXgPercentiles = { 10.0, 50.0, 90.0 };

    private const string NoMatchup = "needs the smart vs random AI matchup, not played by this command";

    private static readonly SortedDictionary<string, Func<TeamTargetCounts, double>[]> Ratios = new(StringComparer.Ordinal)
    {
        ["goalsPerTeamPerMatch"] = PerMatch(t => t.Goals),
        ["shotAttempts"] = PerMatch(t => t.Shots),
        ["shotsOnTarget"] = PerMatch(t => t.OnTarget),
        ["shotsBlocked"] = PerMatch(t => t.Blocked),
        ["shotsMissed"] = PerMatch(t => t.Missed),
        ["xg"] = PerMatch(t => t.Xg),
        ["xg5v5"] = PerMatch(t => t.Xg5v5),
        ["shots5v5"] = PerMatch(t => t.Shots5v5),
        ["shots5v5Rush"] = PerMatch(t => t.Shots5v5ByType.GetValueOrDefault(ChanceType.Rush)),
        ["shots5v5Turnover"] = PerMatch(t => t.Shots5v5ByType.GetValueOrDefault(ChanceType.Turnover)),
        ["shots5v5ZonePlay"] = PerMatch(t => t.Shots5v5ByType.GetValueOrDefault(ChanceType.OffensiveZone)),
        ["chances5v5Top"] = PerMatch(t => t.Chances5v5ByClass.GetValueOrDefault(ChanceClass.Top)),
        ["chances5v5Good"] = PerMatch(t => t.Chances5v5ByClass.GetValueOrDefault(ChanceClass.Good)),
        ["chances5v5Moderate"] = PerMatch(t => t.Chances5v5ByClass.GetValueOrDefault(ChanceClass.Moderate)),
        ["chancesAll"] = PerMatch(t => t.ChancesAll),
        ["offensiveZoneTime"] = PerMatch(t => t.ZoneSeconds),
        ["passSuccessAll"] = Ratio(t => t.PassesCompleted, t => t.Passes),
        ["passSuccess5v5"] = Ratio(t => t.Passes5v5Completed, t => t.Passes5v5),
        ["passSuccessUnderPressure"] = Ratio(t => t.PassesUnderPressureCompleted, t => t.PassesUnderPressure),
        ["passesUnderPressureShare"] = Ratio(t => t.PassesUnderPressure, t => t.Passes),
        ["shotsUnderPressureShare"] = Ratio(t => t.ShotsUnderPressure, t => t.Shots),
        ["puckBattleWinShare"] = Ratio(t => t.BattlesWon, Battles),
        ["puckBattleNoWinnerShare"] = Ratio(t => t.BattlesNoWinner, Battles),
        ["puckBattleLossShare"] = Ratio(t => t.BattlesLost, Battles),
        ["zoneEntrySuccessCarry"] = Ratio(t => t.CarryEntriesKept, t => t.CarryEntries),
        ["zoneEntrySuccessPass"] = Ratio(t => t.PassEntriesKept, t => t.PassEntries),
        ["evenStrengthXgShareRush"] = Ratio(t => EvenXg(t, ChanceType.Rush), EvenXgWithoutFaceoffs),
        ["evenStrengthXgShareTurnover"] = Ratio(t => EvenXg(t, ChanceType.Turnover), EvenXgWithoutFaceoffs),
        ["evenStrengthXgShareZonePlay"] = Ratio(t => EvenXg(t, ChanceType.OffensiveZone), EvenXgWithoutFaceoffs),
        ["evenStrengthXgShareFaceoff"] = Ratio(t => EvenXg(t, ChanceType.Faceoff), t => EvenXgWithoutFaceoffs(t) + EvenXg(t, ChanceType.Faceoff)),
        ["shiftLengthForward"] = Ratio(t => t.ForwardSeconds, t => t.ForwardShifts),
        ["shiftLengthDefence"] = Ratio(t => t.PairSeconds, t => t.PairShifts),
    };

    /// <summary>Evaluates every metric of <paramref name="targets"/> against <paramref name="counts"/>, in the targets' name order.</summary>
    public static IReadOnlyList<TargetRow> Evaluate(TargetsConfig targets, TargetCounts counts)
    {
        var rows = new List<TargetRow>();
        foreach (TargetMetric metric in targets.Metrics)
        {
            (MeasuredValue? combined, MeasuredValue?[]? teams, string? note) = Measure(metric.Name, counts);
            TargetStatus status = combined == null ? TargetStatus.NotMeasured : Compare(combined, metric);
            rows.Add(new TargetRow(metric, combined, teams, status, combined == null ? note ?? "no events to compute it from" : null));
        }

        return rows;
    }

    /// <summary>OK / TOO LOW / TOO HIGH when both min and max are set (bounds included), otherwise <see cref="TargetStatus.NoRange"/>.</summary>
    public static TargetStatus Compare(MeasuredValue value, TargetMetric metric)
    {
        if (!metric.Min.HasValue || !metric.Max.HasValue)
        {
            return TargetStatus.NoRange;
        }

        bool low = value.Low < metric.Min.Value;
        bool high = value.High > metric.Max.Value;
        return low && high ? TargetStatus.TooLowAndTooHigh : low ? TargetStatus.TooLow : high ? TargetStatus.TooHigh : TargetStatus.Ok;
    }

    /// <summary>
    /// Percentile with linear interpolation between the closest ranks (the spreadsheet PERCENTILE.INC method);
    /// <paramref name="sorted"/> must be sorted ascending and not empty.
    /// </summary>
    public static double Percentile(IReadOnlyList<double> sorted, double percent)
    {
        double rank = percent / 100.0 * (sorted.Count - 1);
        int lower = (int)Math.Floor(rank);
        int upper = Math.Min(lower + 1, sorted.Count - 1);
        return sorted[lower] + (rank - lower) * (sorted[upper] - sorted[lower]);
    }

    /// <summary>Writes the comparison as text lines; <paramref name="teamLabels"/> names slots 0 and 1.</summary>
    public static string Write(IReadOnlyList<TargetRow> rows, IReadOnlyList<string> teamLabels)
    {
        var sb = new StringBuilder();
        foreach (TargetRow row in rows)
        {
            TargetMetric m = row.Metric;
            sb.Append("  " + m.Name + " [" + Bound(m.Min, m.Unit) + ", " + Bound(m.Max, m.Unit) + "]: ");
            if (row.Combined == null)
            {
                sb.AppendLine("not measured (" + row.Note + ")");
                continue;
            }

            sb.Append(Format(row.Combined, m));
            if (row.Teams != null)
            {
                sb.Append("  (" + string.Join(" / ", row.Teams.Select((t, i) => teamLabels[i] + " " + (t == null ? "-" : Format(t, m)))) + ")");
            }

            sb.AppendLine(row.Status == TargetStatus.NoRange ? string.Empty : "  → " + StatusText(row.Status));
        }

        return sb.ToString();
    }

    /// <summary>OK, TOO LOW, TOO HIGH, TOO LOW and TOO HIGH, or empty.</summary>
    public static string StatusText(TargetStatus status)
    {
        switch (status)
        {
            case TargetStatus.Ok:
                return "OK";
            case TargetStatus.TooLow:
                return "TOO LOW";
            case TargetStatus.TooHigh:
                return "TOO HIGH";
            case TargetStatus.TooLowAndTooHigh:
                return "TOO LOW and TOO HIGH";
            case TargetStatus.NotMeasured:
                return "not measured";
            default:
                return string.Empty;
        }
    }

    private static (MeasuredValue?, MeasuredValue?[]?, string?) Measure(string name, TargetCounts counts)
    {
        if (Ratios.TryGetValue(name, out Func<TeamTargetCounts, double>[]? ratio))
        {
            MeasuredValue?[] teams = counts.Teams.Select(t => RatioOf(ratio, new[] { t })).ToArray();
            return (RatioOf(ratio, counts.Teams), teams, null);
        }

        switch (name)
        {
            case "goalsPerMatch":
                return (PerMatchTotal(counts.Goals, counts.Matches), null, null);
            case "faceoffsPerMatch":
                return (PerMatchTotal(counts.Faceoffs, counts.Matches), null, null);
            case "meanTimeBetweenStoppages":
                return (PerMatchTotal(counts.StoppageGapSum, counts.MatchesWithFaceoffs), null, null);
            case "offensiveZoneTime5v5":
                if (!counts.AllFiveOnFive)
                {
                    return (null, null, "zone time by strength is not tracked and some play was not 5 vs 5");
                }

                // data-schema.md: before special teams the full-match and 5v5 values are the same.
                MeasuredValue?[] zone = counts.Teams.Select(t => RatioOf(Ratios["offensiveZoneTime"], new[] { t })).ToArray();
                return (RatioOf(Ratios["offensiveZoneTime"], counts.Teams), zone, null);
            case "shotXgTypical":
                return (Percentiles(counts.Teams), counts.Teams.Select(t => Percentiles(new[] { t })).ToArray(), null);
            case "smartVsRandomWinRate":
                return (null, null, NoMatchup);
            default:
                return (null, null, "no calculation defined for this metric in the harness");
        }
    }

    private static MeasuredValue? RatioOf(Func<TeamTargetCounts, double>[] ratio, IEnumerable<TeamTargetCounts> teams)
    {
        double num = 0.0;
        double den = 0.0;
        foreach (TeamTargetCounts t in teams)
        {
            num += ratio[0](t);
            den += ratio[1](t);
        }

        return den > 0.0 ? MeasuredValue.Of(num / den) : null;
    }

    private static MeasuredValue? PerMatchTotal(double sum, int matches) => matches > 0 ? MeasuredValue.Of(sum / matches) : null;

    private static MeasuredValue? Percentiles(IEnumerable<TeamTargetCounts> teams)
    {
        List<double> xg = teams.SelectMany(t => t.ShotXg).OrderBy(x => x).ToList();
        if (xg.Count == 0)
        {
            return null;
        }

        double[] values = ShotXgPercentiles.Select(p => Percentile(xg, p)).ToArray();
        return new MeasuredValue(values, values[0], values[values.Length - 1]);
    }

    private static Func<TeamTargetCounts, double>[] PerMatch(Func<TeamTargetCounts, double> sum) => new[] { sum, t => t.Matches };

    private static Func<TeamTargetCounts, double>[] Ratio(Func<TeamTargetCounts, double> num, Func<TeamTargetCounts, double> den) => new[] { num, den };

    private static double Battles(TeamTargetCounts t) => t.BattlesWon + t.BattlesNoWinner + t.BattlesLost;

    private static double EvenXg(TeamTargetCounts t, ChanceType type) => t.EvenStrengthXgByType.GetValueOrDefault(type);

    private static double EvenXgWithoutFaceoffs(TeamTargetCounts t) =>
        EvenXg(t, ChanceType.Rush) + EvenXg(t, ChanceType.Turnover) + EvenXg(t, ChanceType.OffensiveZone);

    private static string Format(MeasuredValue value, TargetMetric metric)
    {
        if (value.Values.Count == ShotXgPercentiles.Length && metric.Name == "shotXgTypical")
        {
            return string.Join(" / ", value.Values.Select((v, i) => "p" + ShotXgPercentiles[i].ToString("0", CultureInfo.InvariantCulture) + " " + Number(v, metric.Unit)));
        }

        return Number(value.Values[0], metric.Unit);
    }

    private static string Bound(double? bound, string unit) => bound.HasValue ? Number(bound.Value, unit) : "-";

    private static string Number(double v, string unit)
    {
        switch (unit)
        {
            case "share":
                return (100.0 * v).ToString("0.0", CultureInfo.InvariantCulture) + " %";
            case "xg":
                return v.ToString("0.000", CultureInfo.InvariantCulture);
            case "seconds":
                return v.ToString("0.0", CultureInfo.InvariantCulture) + " s";
            default:
                return v.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
