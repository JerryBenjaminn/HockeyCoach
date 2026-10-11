using System.Text;
using HockeyCoach.Sim.Events;

namespace HockeyCoach.Harness.Batch;

/// <summary>Attempts and successes of one table cell.</summary>
/// <param name="N">Attempts.</param>
/// <param name="Hits">Successes.</param>
public sealed record Rate(int N, int Hits)
{
    /// <summary>Success share, 0 without attempts.</summary>
    public double Share => N > 0 ? Hits / (double)N : 0.0;

    /// <summary>Sum of two rates.</summary>
    public static Rate operator +(Rate a, Rate b) => new(a.N + b.N, a.Hits + b.Hits);
}

/// <summary>One column of a counterplay table: a header and the rates by row key.</summary>
/// <param name="Header">Column header.</param>
/// <param name="Rates">Rates by row key.</param>
public sealed record RateColumn(string Header, IReadOnlyDictionary<string, Rate> Rates);

/// <summary>
/// Tactic counterplay sections (data-schema.md M-9, Kalibrointi; D-078 K4): controlled zone entry success by strength
/// "N v M" (carries and passes apart) and breakout success by forechecker count f, each by the defending system.
/// Keys of <see cref="MatchupStats.Entries"/>: "slot method NvM vs system"; of <see cref="MatchupStats.Breakouts"/>:
/// "slot fF vs system".
/// </summary>
public static class Counterplay
{
    /// <summary>Column header of the total over all columns.</summary>
    public const string AllHeader = "All";

    /// <summary>Entry key, e.g. "A carry 2v3 vs trap122".</summary>
    public static string EntryKey(string slot, ZoneEntryMethod method, EntryNumbers numbers, string system) =>
        slot + " " + MethodName(method) + " " + numbers.Attackers + "v" + numbers.Defenders + " vs " + system;

    /// <summary>Breakout key, e.g. "B f2 vs forecheck212".</summary>
    public static string BreakoutKey(string slot, int forecheckers, string system) => slot + " f" + forecheckers + " vs " + system;

    /// <summary>"carry" or "pass".</summary>
    public static string MethodName(ZoneEntryMethod method) => method == ZoneEntryMethod.Carry ? "carry" : "pass";

    /// <summary>
    /// Rates of a tally keyed "slot rest vs system", grouped by "rest" (e.g. "carry 2v3" or "f2"), keeping only
    /// <paramref name="slot"/> (null = both) and <paramref name="system"/> (null = all).
    /// </summary>
    public static Dictionary<string, Rate> Group(Tally tally, string? slot, string? system)
    {
        var rates = new Dictionary<string, Rate>(StringComparer.Ordinal);
        foreach (string key in tally.N.Keys)
        {
            (string keySlot, string row, string keySystem) = Split(key);
            if ((slot != null && keySlot != slot) || (system != null && keySystem != system))
            {
                continue;
            }

            var rate = new Rate(tally.N[key], tally.Hits[key]);
            rates[row] = rates.TryGetValue(row, out Rate? sum) ? sum + rate : rate;
        }

        return rates;
    }

    /// <summary>The defending systems in a tally, ordinal order.</summary>
    public static IReadOnlyList<string> Systems(Tally tally) =>
        tally.N.Keys.Select(k => Split(k).System).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();

    /// <summary>Entry rows: carries before passes, then N − M descending, then N descending.</summary>
    public static IEnumerable<string> OrderEntryRows(IEnumerable<string> rows) =>
        rows.Distinct()
            .Select(r => (Row: r, Parts: ParseEntryRow(r)))
            .OrderBy(x => x.Parts.Method == "carry" ? 0 : 1)
            .ThenByDescending(x => x.Parts.N - x.Parts.M)
            .ThenByDescending(x => x.Parts.N)
            .Select(x => x.Row);

    /// <summary>Breakout rows f0..f3 in order.</summary>
    public static IEnumerable<string> OrderBreakoutRows(IEnumerable<string> rows) => rows.Distinct().OrderBy(r => r, StringComparer.Ordinal);

    /// <summary>Writes a Markdown table: one row per key, one "success (n)" cell per column.</summary>
    public static void WriteTable(StringBuilder md, string rowHeader, IEnumerable<string> rows, IReadOnlyList<RateColumn> columns)
    {
        md.AppendLine("| " + rowHeader + " | " + string.Join(" | ", columns.Select(c => c.Header)) + " |");
        md.AppendLine("|" + string.Concat(Enumerable.Repeat(" --- |", columns.Count + 1)));
        foreach (string row in rows)
        {
            md.AppendLine("| " + row + " | " + string.Join(" | ", columns.Select(c => Cell(c.Rates, row))) + " |");
        }

        md.AppendLine();
    }

    /// <summary>Entry and breakout tables of one matchup, both slots, by defending system (batch details).</summary>
    public static void WriteMatchup(StringBuilder md, MatchupStats s)
    {
        md.AppendLine("### Zone entries by strength (M-8, M-9)");
        md.AppendLine();
        md.AppendLine("Controlled entries of both teams: success = possession kept; cell = success (attempts). N v M = attackers at or past the centre line vs defenders at or beyond the puck. Columns: defending system.");
        md.AppendLine();
        WriteTable(md, "Entry", OrderEntryRows(Group(s.Entries, null, null).Keys), ColumnsBySystem(s.Entries));

        md.AppendLine("### Breakouts by forecheckers (M-9)");
        md.AppendLine();
        md.AppendLine("Every breakout check of both teams (own-zone carry, pass out of the zone, own-zone regroup check); f = defending forwards (F1–F3) in the zone. Columns: defending system.");
        md.AppendLine();
        WriteTable(md, "f", OrderBreakoutRows(Group(s.Breakouts, null, null).Keys), ColumnsBySystem(s.Breakouts));
    }

    private static List<RateColumn> ColumnsBySystem(Tally tally)
    {
        var columns = Systems(tally).Select(system => new RateColumn("vs " + system, Group(tally, null, system))).ToList();
        columns.Add(new RateColumn(AllHeader, Group(tally, null, null)));
        return columns;
    }

    private static string Cell(IReadOnlyDictionary<string, Rate> rates, string row) =>
        rates.TryGetValue(row, out Rate? r) && r.N > 0 ? BatchReport.Pct(r.Share) + " (" + r.N + ")" : "-";

    private static (string Slot, string Row, string System) Split(string key)
    {
        int space = key.IndexOf(' ', StringComparison.Ordinal);
        int vs = key.LastIndexOf(" vs ", StringComparison.Ordinal);
        return (key[..space], key[(space + 1)..vs], key[(vs + 4)..]);
    }

    private static (string Method, int N, int M) ParseEntryRow(string row)
    {
        string[] parts = row.Split(' ');
        string[] numbers = parts[1].Split('v');
        return (parts[0], int.Parse(numbers[0], System.Globalization.CultureInfo.InvariantCulture), int.Parse(numbers[1], System.Globalization.CultureInfo.InvariantCulture));
    }
}
