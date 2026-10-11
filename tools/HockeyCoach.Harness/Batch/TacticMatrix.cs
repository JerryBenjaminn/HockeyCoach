using System.Globalization;
using System.Text;
using HockeyCoach.Harness.Data;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Harness.Batch;

/// <summary>One cell of the tactic matrix: team A's transition instruction against team B's defensive system.</summary>
/// <param name="Transition">Team A's fixed transition instruction.</param>
/// <param name="OpponentSystem">Team B's defensive system.</param>
/// <param name="Stats">The played matches, team A in slot 0.</param>
public sealed record TacticCell(TransitionInstruction Transition, string OpponentSystem, MatchupStats Stats)
{
    /// <summary>Skill-vs-luck numbers of the cell (points share and CI).</summary>
    public SkillLuck Luck { get; } = SkillLuck.From(Stats.Lines);
}

/// <summary>The files of a tactic-matrix run (written to <c>--out</c>, git-ignored).</summary>
/// <param name="Markdown">tactic-matrix.md.</param>
/// <param name="Csv">tactic-matrix.csv: one row per cell.</param>
public sealed record TacticMatrixFiles(string Markdown, string Csv);

/// <summary>
/// The tactic matrix (D-078 K3, E-005): team A = rotation coach with the own system <c>--rotation-system</c> and a fixed
/// transition instruction per row; team B = rotation coach with the defensive system of the column and the transition
/// <c>--rotation-transition</c> (default rush). Mirrored rosters, <c>--matches</c> per cell in pairs (seed + i, sides
/// swapped). Rows: every transition instruction; columns: every loaded system in load order.
/// </summary>
public static class TacticMatrix
{
    /// <summary>Rows of the matrix in report order.</summary>
    public static readonly IReadOnlyList<TransitionInstruction> Transitions = new[] { TransitionInstruction.Rush, TransitionInstruction.Regroup };

    /// <summary>Matchup labels of one cell, e.g. <c>rotation/forecheck212/regroup</c> vs <c>rotation/trap122/rush</c>.</summary>
    public static Matchup CellMatchup(BatchOptions options, TransitionInstruction transition, string opponentSystem) =>
        new(
            CoachRegistry.Label(CoachRegistry.Rotation, options.RotationSystem) + "/" + CoachRegistry.TransitionName(transition),
            CoachRegistry.Label(CoachRegistry.Rotation, opponentSystem) + "/" + CoachRegistry.TransitionName(options.RotationTransition));

    /// <summary>Plays every cell in row order (transition), then column order (system); deterministic for any thread count.</summary>
    public static IReadOnlyList<TacticCell> Run(GameData data, BatchOptions options, Action<string>? progress = null)
    {
        HarnessTeams.System(data, options.RotationSystem);
        var cells = new List<(TransitionInstruction Transition, string System)>();
        var entries = new List<BatchEntry>();
        foreach (TransitionInstruction transition in Transitions)
        {
            foreach (string system in data.Systems.Select(s => s.Id))
            {
                cells.Add((transition, system));
                entries.Add(new BatchEntry(
                    CellMatchup(options, transition, system),
                    CoachRegistry.Factory(CoachRegistry.Rotation, data, options.RotationSystem, transition),
                    CoachRegistry.Factory(CoachRegistry.Rotation, data, system, options.RotationTransition)));
            }
        }

        var mirrored = options with { MirrorRosters = true };
        IReadOnlyList<MatchupStats> stats = BatchRunner.Run(data, mirrored, entries, progress);
        return cells.Select((c, i) => new TacticCell(c.Transition, c.System, stats[i])).ToList();
    }

    /// <summary>Team A's xG for, against and difference per 60 minutes of regulation over all matches of the cell.</summary>
    public static (double For, double Against, double Diff) XgPer60(TacticCell cell, double matchSeconds)
    {
        int n = cell.Stats.Lines.Count;
        if (n == 0 || matchSeconds <= 0.0)
        {
            return (0.0, 0.0, 0.0);
        }

        double scale = 3600.0 / (n * matchSeconds);
        double xgFor = cell.Stats.Lines.Sum(l => l.XgA) * scale;
        double xgAgainst = cell.Stats.Lines.Sum(l => l.XgB) * scale;
        return (xgFor, xgAgainst, xgFor - xgAgainst);
    }

    /// <summary>Play xG per match of a slot ("A" or "B"): shots taken while the shooting team runs a play.</summary>
    public static double PlayXgPerMatch(TacticCell cell, string slot)
    {
        int n = cell.Stats.Lines.Count;
        return n > 0 && cell.Stats.PlayXg.Sum.TryGetValue(slot, out double xg) ? xg / n : 0.0;
    }

    /// <summary>Builds tactic-matrix.md and tactic-matrix.csv.</summary>
    public static TacticMatrixFiles Write(GameData data, BatchOptions options, IReadOnlyList<TacticCell> cells, IReadOnlyList<string> header)
    {
        double matchSeconds = data.Tuning.Time.Periods * data.Tuning.Time.PeriodSeconds;
        var systems = cells.Select(c => c.OpponentSystem).Distinct().ToList();
        TacticCell? Cell(TransitionInstruction t, string system) => cells.FirstOrDefault(c => c.Transition == t && c.OpponentSystem == system);

        var md = new StringBuilder();
        md.AppendLine("# Tactic matrix (D-078 K3)");
        md.AppendLine();
        foreach (string line in header)
        {
            md.AppendLine("- " + line);
        }

        md.AppendLine("- Team A: rotation, own system " + options.RotationSystem + ", fixed transition per row. Team B: rotation, system per column, transition "
            + CoachRegistry.TransitionName(options.RotationTransition) + ".");
        md.AppendLine("- Matches per cell: " + options.Matches + " (" + (options.Matches / 2) + " pairs; pair i uses seed " + options.Seed
            + " + i, mirrored rosters, sides swapped). Points share: win 1, tie 0.5 (D-073); CI 95 % over pair means.");
        md.AppendLine("- Cell: team A's points share · xG for / against / difference per 60 min.");
        md.AppendLine();

        md.AppendLine("## Matrix");
        md.AppendLine();
        md.AppendLine("| A transition | " + string.Join(" | ", systems.Select(s => "B plays " + s)) + " |");
        md.AppendLine("|" + string.Concat(Enumerable.Repeat(" --- |", systems.Count + 1)));
        foreach (TransitionInstruction t in Transitions)
        {
            var texts = systems.Select(system =>
            {
                TacticCell? c = Cell(t, system);
                if (c == null)
                {
                    return "-";
                }

                (double f, double a, double d) = XgPer60(c, matchSeconds);
                return BatchReport.Pct(c.Luck.PointsShare) + " · " + BatchReport.F2(f) + " / " + BatchReport.F2(a) + " / " + BatchReport.Signed(d);
            });
            md.AppendLine("| " + CoachRegistry.TransitionName(t) + " | " + string.Join(" | ", texts) + " |");
        }

        md.AppendLine();
        md.AppendLine("## Cells");
        md.AppendLine();
        md.AppendLine("| A transition | B system | N | A W / T / L | A points [95 % CI] | xGF / 60 | xGA / 60 | xG diff / 60 | Goals/match | Play xG/match A / B | Stalled |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (TacticCell c in cells)
        {
            SkillLuck s = c.Luck;
            (double f, double a, double d) = XgPer60(c, matchSeconds);
            md.AppendLine("| " + CoachRegistry.TransitionName(c.Transition) + " | " + c.OpponentSystem + " | " + s.Matches + " | " + s.Wins + " / " + s.Ties + " / " + s.Losses
                + " | " + BatchReport.Pct(s.PointsShare) + " [" + BatchReport.Pct(s.CiLow) + " – " + BatchReport.Pct(s.CiHigh) + "]"
                + " | " + BatchReport.F2(f) + " | " + BatchReport.F2(a) + " | " + BatchReport.Signed(d) + " | " + BatchReport.F2(s.GoalsPerMatch)
                + " | " + BatchReport.F2(PlayXgPerMatch(c, "A")) + " / " + BatchReport.F2(PlayXgPerMatch(c, "B")) + " | " + s.Stalled + " |");
        }

        md.AppendLine();
        md.AppendLine("Play xG: xG of shots taken while the shooting team runs a play (event play id set).");
        md.AppendLine();
        md.AppendLine("## Rush minus regroup per opponent system");
        md.AppendLine();
        md.AppendLine("| B system | Points: rush - regroup | xG diff / 60: rush - regroup |");
        md.AppendLine("| --- | --- | --- |");
        foreach (string system in systems)
        {
            TacticCell? rush = Cell(TransitionInstruction.Rush, system);
            TacticCell? regroup = Cell(TransitionInstruction.Regroup, system);
            if (rush == null || regroup == null)
            {
                continue;
            }

            double points = 100.0 * (rush.Luck.PointsShare - regroup.Luck.PointsShare);
            md.AppendLine("| " + system + " | " + (points >= 0 ? "+" : "-") + Math.Abs(points).ToString("0.0", CultureInfo.InvariantCulture) + " pp | "
                + BatchReport.Signed(XgPer60(rush, matchSeconds).Diff - XgPer60(regroup, matchSeconds).Diff) + " |");
        }

        md.AppendLine();

        var csv = new StringBuilder();
        csv.AppendLine("transition,opponentSystem,matches,winsA,ties,lossesA,pointsShareA,ciLow,ciHigh,xgForPer60,xgAgainstPer60,xgDiffPer60,goalsPerMatch,playXgA,playXgB,stalled");
        foreach (TacticCell c in cells)
        {
            SkillLuck s = c.Luck;
            (double f, double a, double d) = XgPer60(c, matchSeconds);
            csv.AppendLine(string.Join(",", CoachRegistry.TransitionName(c.Transition), c.OpponentSystem, s.Matches, s.Wins, s.Ties, s.Losses, BatchReport.R(s.PointsShare),
                BatchReport.R(s.CiLow), BatchReport.R(s.CiHigh), BatchReport.R(f), BatchReport.R(a), BatchReport.R(d), BatchReport.R(s.GoalsPerMatch),
                BatchReport.R(PlayXgPerMatch(c, "A")), BatchReport.R(PlayXgPerMatch(c, "B")), s.Stalled));
        }

        return new TacticMatrixFiles(md.ToString(), csv.ToString());
    }
}
