using System.Globalization;
using System.Text;
using HockeyCoach.Harness.Data;
using HockeyCoach.Harness.Report;
using HockeyCoach.Harness.Simulation;

namespace HockeyCoach.Harness.Batch;

/// <summary>The files of a batch report: Markdown and CSVs (written to reports/, which is git-ignored).</summary>
/// <param name="Markdown">batch-report.md.</param>
/// <param name="SummaryCsv">batch-summary.csv: one row per matchup.</param>
/// <param name="MatchesCsv">batch-matches.csv: one row per match (summaries only, no logs).</param>
/// <param name="DetailsCsv">batch-details.csv: every section in long format.</param>
public sealed record BatchReportFiles(string Markdown, string SummaryCsv, string MatchesCsv, string DetailsCsv);

/// <summary>
/// Writes the batch report (tech-spec.md, Raportin sisältö; D-073, D-077 items 4 and 8): skill vs luck, the points-share
/// matrix and ablation rows, the target comparison on rulebased vs rulebased, and the designer's sections (fatigue,
/// play × system, rush vs regroup, zone entries by strength, breakouts by forecheckers, checks, pressure, chances, goal
/// variance).
/// </summary>
public static class BatchReport
{
    /// <summary>D-077 item 8: below this points share of rulebased vs random, the variance split goes to Jerry.</summary>
    public const double SkillThreshold = 0.75;

    /// <summary>Matchups whose designer sections are printed in the Markdown (all are in the details CSV).</summary>
    public static readonly IReadOnlyList<(string A, string B)> DetailMatchups = new[]
    {
        (CoachRegistry.RuleBased, CoachRegistry.RuleBased), (CoachRegistry.RuleBased, CoachRegistry.Random), (CoachRegistry.Random, CoachRegistry.Random),
    };

    /// <summary>Builds all report files.</summary>
    /// <param name="data">Loaded data.</param>
    /// <param name="options">Batch options.</param>
    /// <param name="stats">Per-matchup statistics in matchup order.</param>
    /// <param name="header">Header lines (command, commit, tuning hash).</param>
    public static BatchReportFiles Write(GameData data, BatchOptions options, IReadOnlyList<MatchupStats> stats, IReadOnlyList<string> header)
    {
        var luck = stats.Select(s => SkillLuck.From(s.Lines)).ToArray();
        var md = new StringBuilder();
        md.AppendLine("# Batch report");
        md.AppendLine();
        foreach (string line in header)
        {
            md.AppendLine("- " + line);
        }

        md.AppendLine("- Matches per AI pair: " + options.Matches + " (" + (options.Matches / 2) + " pairs; pair i of every matchup uses seed " + options.Seed
            + " + i and its own derived rosters" + (options.MirrorRosters ? ", mirrored" : string.Empty) + "); rotation plays " + options.RotationSystem + ".");
        md.AppendLine("- Points share: win 1, tie 0.5 (D-073). CI: 95 % normal interval over pair means (each pair = same seed and rosters, sides swapped).");
        md.AppendLine();

        WriteSkillVsLuck(md, stats, luck);
        WriteMatrix(md, stats, luck);
        WriteAblation(md, stats, luck);
        AdaptiveChoice.Write(md, stats);
        WriteTargets(md, data, stats);
        foreach ((string a, string b) in DetailMatchups)
        {
            int index = Find(stats, a, b);
            if (index >= 0)
            {
                WriteDetails(md, stats[index], luck[index]);
            }
        }

        return new BatchReportFiles(md.ToString(), SummaryCsv(stats, luck), MatchesCsv(stats), DetailsCsv(stats));
    }

    private static int Find(IReadOnlyList<MatchupStats> stats, string a, string b)
    {
        for (int i = 0; i < stats.Count; i++)
        {
            if (stats[i].Matchup.A == a && stats[i].Matchup.B == b)
            {
                return i;
            }
        }

        return -1;
    }

    private static void WriteSkillVsLuck(StringBuilder md, IReadOnlyList<MatchupStats> stats, SkillLuck[] luck)
    {
        md.AppendLine("## Skill vs luck");
        md.AppendLine();
        int primary = Find(stats, CoachRegistry.RuleBased, CoachRegistry.Random);
        if (primary >= 0)
        {
            SkillLuck p = luck[primary];
            md.AppendLine("**Primary (D-077 item 4): rulebased vs random:** points share " + Pct(p.PointsShare) + " [95 % CI " + Pct(p.CiLow) + " – " + Pct(p.CiHigh) + "], "
                + p.Wins + " W / " + p.Ties + " T / " + p.Losses + " L of " + p.Matches + ".");
            md.AppendLine();
            md.AppendLine(p.PointsShare < SkillThreshold
                ? "Below " + Pct(SkillThreshold) + " (D-077 item 8): the goal-difference variance split below (chance creation vs finishing luck) goes to Jerry."
                : "At or above " + Pct(SkillThreshold) + " (D-077 item 8).");
            md.AppendLine();
        }

        md.AppendLine("Team A = first AI. Variance split: Var(goal diff) = Var(xG diff) + Var(finishing luck) + 2 Cov; finishing luck = goal diff − xG diff.");
        md.AppendLine();
        md.AppendLine("| Matchup | N | A W / T / L | A points [95 % CI] | Upsets | Pairs swept / split | Home W / Away W | Mean GD / xGD | Var GD | Chance / finishing / 2 Cov | xG winner wins (ties) | Goals/match | Stalled |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        for (int i = 0; i < stats.Count; i++)
        {
            SkillLuck s = luck[i];
            md.AppendLine("| " + stats[i].Matchup.Label + " | " + s.Matches + " | " + s.Wins + " / " + s.Ties + " / " + s.Losses
                + " | " + Pct(s.PointsShare) + " [" + Pct(s.CiLow) + " – " + Pct(s.CiHigh) + "]"
                + " | " + Pct(s.UpsetRate) + " | " + Pct(s.SweepShare) + " / " + Pct(s.SplitShare)
                + " | " + Pct(s.HomeWinShare) + " / " + Pct(s.AwayWinShare)
                + " | " + F2(s.MeanGoalDiff) + " / " + F2(s.MeanXgDiff) + " | " + F2(s.VarGoalDiff)
                + " | " + Pct(s.ChanceShare) + " / " + Pct(s.FinishingShare) + " / " + Pct(s.CovarianceShare)
                + " | " + Pct(s.XgWinnerWins) + " (" + Pct(s.XgWinnerTies) + ")"
                + " | " + F2(s.GoalsPerMatch) + " | " + s.Stalled + " |");
        }

        md.AppendLine();
        md.AppendLine("Upsets: matches lost by the side with the higher points share. Pairs swept: one team won both matches of the pair; split: each won one.");
        md.AppendLine();
    }

    private static void WriteMatrix(StringBuilder md, IReadOnlyList<MatchupStats> stats, SkillLuck[] luck)
    {
        var names = stats.SelectMany(s => new[] { s.Matchup.A, s.Matchup.B }).Distinct().ToList();
        var order = BatchOptions.AllCoaches.Where(names.Contains).Concat(names.Where(n => !BatchOptions.AllCoaches.Contains(n))).ToList();
        md.AppendLine("## Points-share matrix (row AI against column AI)");
        md.AppendLine();
        md.AppendLine("| | " + string.Join(" | ", order) + " |");
        md.AppendLine("|" + string.Concat(Enumerable.Repeat(" --- |", order.Count + 1)));
        foreach (string row in order)
        {
            var cells = new List<string>();
            foreach (string col in order)
            {
                int direct = Find(stats, row, col);
                int reverse = Find(stats, col, row);
                cells.Add(direct >= 0 ? Pct(luck[direct].PointsShare) : reverse >= 0 ? Pct(1.0 - luck[reverse].PointsShare) : "-");
            }

            md.AppendLine("| " + row + " | " + string.Join(" | ", cells) + " |");
        }

        md.AppendLine();
    }

    private static void WriteAblation(StringBuilder md, IReadOnlyList<MatchupStats> stats, SkillLuck[] luck)
    {
        md.AppendLine("## Ablation: rulebased against weakened opponents");
        md.AppendLine();
        md.AppendLine("random = random lines + random tactics; randomTactics = rotation lines + random tactics; randomLines = random lines + rule-based tactics.");
        md.AppendLine();
        md.AppendLine("| Opponent | rulebased points [95 % CI] | adaptive points [95 % CI] |");
        md.AppendLine("| --- | --- | --- |");
        foreach (string opponent in new[] { CoachRegistry.Random, CoachRegistry.RandomTactics, CoachRegistry.RandomLines, CoachRegistry.Rotation, CoachRegistry.RuleBased })
        {
            md.AppendLine("| " + opponent + " | " + ShareText(stats, luck, CoachRegistry.RuleBased, opponent) + " | " + ShareText(stats, luck, CoachRegistry.Adaptive, opponent) + " |");
        }

        md.AppendLine();
    }

    private static string ShareText(IReadOnlyList<MatchupStats> stats, SkillLuck[] luck, string a, string b)
    {
        int direct = Find(stats, a, b);
        if (direct >= 0)
        {
            return Pct(luck[direct].PointsShare) + " [" + Pct(luck[direct].CiLow) + " – " + Pct(luck[direct].CiHigh) + "]";
        }

        int reverse = Find(stats, b, a);
        return reverse >= 0
            ? Pct(1.0 - luck[reverse].PointsShare) + " [" + Pct(1.0 - luck[reverse].CiHigh) + " – " + Pct(1.0 - luck[reverse].CiLow) + "]"
            : "-";
    }

    private static void WriteTargets(StringBuilder md, GameData data, IReadOnlyList<MatchupStats> stats)
    {
        int realism = Find(stats, CoachRegistry.RuleBased, CoachRegistry.RuleBased);
        md.AppendLine("## Targets on rulebased vs rulebased (D-073)");
        md.AppendLine();
        if (realism < 0)
        {
            md.AppendLine("Not played in this batch.");
            md.AppendLine();
            return;
        }

        TargetCounts counts = stats[realism].Targets;
        int primary = Find(stats, CoachRegistry.RuleBased, CoachRegistry.Random);
        if (primary >= 0)
        {
            var record = new MatchupRecord();
            foreach (MatchLine line in stats[primary].Lines)
            {
                record.Add(line.GoalsA, line.GoalsB);
            }

            counts.SmartVsRandom = record;
        }

        md.AppendLine("```");
        md.Append(TargetComparison.Write(TargetComparison.Evaluate(data.Targets, counts), new[] { "A", "B" }));
        md.Append(TargetComparison.WriteDiagnostics(counts));
        md.AppendLine("```");
        md.AppendLine();
    }

    private static void WriteDetails(StringBuilder md, MatchupStats s, SkillLuck luck)
    {
        md.AppendLine("## Details: " + s.Matchup.Label);
        md.AppendLine();

        md.AppendLine("### Fatigue");
        md.AppendLine();
        md.AppendLine("Mean participant energy at checks: A " + F3(Mean(s.ParticipantEnergy, "A")) + ", B " + F3(Mean(s.ParticipantEnergy, "B")) + ".");
        md.AppendLine();
        md.AppendLine("| Attacker energy at check | Checks | Share | Success | Mean probability |");
        md.AppendLine("| --- | --- | --- | --- | --- |");
        int energyTotal = s.EnergyAtChecks.N.Values.Sum();
        foreach (string key in s.EnergyAtChecks.N.Keys)
        {
            md.AppendLine("| " + key + " | " + s.EnergyAtChecks.N[key] + " | " + Pct(s.EnergyAtChecks.N[key] / (double)Math.Max(1, energyTotal))
                + " | " + Pct(HitShare(s.EnergyAtChecks, key)) + " | " + Pct(Mean(s.EnergyAtChecks, key)) + " |");
        }

        md.AppendLine();
        int matches = Math.Max(1, s.Lines.Count);
        md.AppendLine("Ice time per unit per match: " + string.Join(", ", s.IceTime.Sum.Select(e => e.Key + " " + MatchText.Clock(e.Value / matches))) + ".");
        md.AppendLine();
        md.AppendLine("Shifts: " + string.Join("; ", s.Shifts.N.Keys.Select(k => k + " mean " + F1(Mean(s.Shifts, k)) + " s, > " + F0(MatchupStats.LongShiftSeconds) + " s " + Pct(HitShare(s.Shifts, k))))
            + ". Timeouts per match: A " + F2(s.Lines.Average(l => l.TimeoutsA)) + ", B " + F2(s.Lines.Average(l => l.TimeoutsB)) + ".");
        md.AppendLine();

        md.AppendLine("### Play × system");
        md.AppendLine();
        md.AppendLine("Play instance = consecutive events of one team with the same play id; success = the instance has a shot attempt.");
        md.AppendLine();
        md.AppendLine("| Play vs defending system | Instances | With shot | xG per instance | Goals |");
        md.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (string key in s.PlaySystem.N.Keys)
        {
            md.AppendLine("| " + key + " | " + s.PlaySystem.N[key] + " | " + Pct(HitShare(s.PlaySystem, key)) + " | " + F3(Mean(s.PlaySystem, key)) + " | " + F0(s.PlaySystem.Sum2[key]) + " |");
        }

        md.AppendLine();
        md.AppendLine("### Rush vs regroup");
        md.AppendLine();
        md.AppendLine("Split by the opponent's defensive system at the decision (D-078 K3).");
        md.AppendLine();
        md.AppendLine("| Transition in force | vs system | Team segments | Minutes | xG for / 60 min | xG against / 60 min | xG diff / 60 min | Goals for |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (string key in s.Transitions.N.Keys)
        {
            double minutes = s.TransitionSeconds.Sum[key] / 60.0;
            double xgFor = minutes > 0 ? s.Transitions.Sum[key] / minutes * 60.0 : 0.0;
            double xgAgainst = minutes > 0 ? s.Transitions.Sum2[key] / minutes * 60.0 : 0.0;
            string[] parts = key.Split(" vs ");
            md.AppendLine("| " + parts[0] + " | " + (parts.Length > 1 ? parts[1] : "-") + " | " + s.Transitions.N[key] + " | " + F0(minutes) + " | " + F2(xgFor)
                + " | " + F2(xgAgainst) + " | " + Signed(xgFor - xgAgainst) + " | " + F0(s.TransitionSeconds.Sum2[key]) + " |");
        }

        md.AppendLine();
        Counterplay.WriteMatchup(md, s);
        md.AppendLine("### Checks");
        md.AppendLine();
        md.AppendLine("| Check | N per match | Attacker success | Mean probability |");
        md.AppendLine("| --- | --- | --- | --- |");
        foreach (string key in s.Checks.N.Keys)
        {
            md.AppendLine("| " + key + " | " + F1(s.Checks.N[key] / (double)matches) + " | " + Pct(HitShare(s.Checks, key)) + " | " + Pct(Mean(s.Checks, key)) + " |");
        }

        md.AppendLine();
        md.AppendLine("block: success = the shot got through; rebound: success = the goalie did not control it; shot: goals on target attempts.");
        md.AppendLine();
        md.AppendLine("### Pressure shares");
        md.AppendLine();
        md.AppendLine("By the actor's zone (own view): " + string.Join(", ", s.PressureByZone.N.Keys.Select(k => k + " " + Pct(HitShare(s.PressureByZone, k)) + " (" + s.PressureByZone.N[k] + ")")) + ".");
        md.AppendLine();
        md.AppendLine("By defending system: " + string.Join(", ", s.PressureBySystem.N.Keys.Select(k => k + " " + Pct(HitShare(s.PressureBySystem, k)) + " (" + s.PressureBySystem.N[k] + ")")) + ".");
        md.AppendLine();
        md.AppendLine("### xG by chance type and class");
        md.AppendLine();
        md.AppendLine("| Chance | Attempts per team-match | Share of attempts | xG share | Goals / xG |");
        md.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (Tally tally in new[] { s.ChanceTypes, s.ChanceClasses })
        {
            int total = tally.N.Values.Sum();
            double xg = tally.Sum.Values.Sum();
            foreach (string key in tally.N.Keys)
            {
                md.AppendLine("| " + key + " | " + F2(tally.N[key] / (2.0 * matches)) + " | " + Pct(tally.N[key] / (double)Math.Max(1, total)) + " | "
                    + Pct(xg > 0 ? tally.Sum[key] / xg : 0.0) + " | " + F2(tally.Sum[key] > 0 ? tally.Hits[key] / tally.Sum[key] : 0.0) + " |");
            }
        }

        md.AppendLine();
        md.AppendLine("### Goal variance");
        md.AppendLine();
        md.AppendLine("Goals per match " + F2(luck.GoalsPerMatch) + " (variance " + F2(luck.GoalsVariance) + "); team goals variance / mean " + F2(luck.TeamGoalsDispersion)
            + " (1 = Poisson). Goal difference variance " + F2(luck.VarGoalDiff) + ": chance creation " + Pct(luck.ChanceShare) + ", finishing luck " + Pct(luck.FinishingShare)
            + ", 2 Cov " + Pct(luck.CovarianceShare) + ".");
        md.AppendLine();
    }

    private static string SummaryCsv(IReadOnlyList<MatchupStats> stats, SkillLuck[] luck)
    {
        var sb = new StringBuilder();
        sb.AppendLine("matchup,a,b,matches,winsA,ties,lossesA,pointsShareA,ciLow,ciHigh,upsetRate,sweepShare,splitShare,homeWinShare,awayWinShare,meanGoalDiff,meanXgDiff,varGoalDiff,chanceShare,finishingShare,covarianceShare,xgWinnerWins,xgWinnerTies,goalsPerMatch,goalsVariance,stalled");
        for (int i = 0; i < stats.Count; i++)
        {
            SkillLuck s = luck[i];
            Matchup m = stats[i].Matchup;
            sb.AppendLine(string.Join(",", m.Key, m.A, m.B, s.Matches, s.Wins, s.Ties, s.Losses, R(s.PointsShare), R(s.CiLow), R(s.CiHigh), R(s.UpsetRate), R(s.SweepShare), R(s.SplitShare),
                R(s.HomeWinShare), R(s.AwayWinShare), R(s.MeanGoalDiff), R(s.MeanXgDiff), R(s.VarGoalDiff), R(s.ChanceShare), R(s.FinishingShare), R(s.CovarianceShare),
                R(s.XgWinnerWins), R(s.XgWinnerTies), R(s.GoalsPerMatch), R(s.GoalsVariance), s.Stalled));
        }

        return sb.ToString();
    }

    private static string MatchesCsv(IReadOnlyList<MatchupStats> stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("matchup,pair,seed,teamAIsHome,goalsA,goalsB,xgA,xgB,shotsA,shotsB,timeoutsA,timeoutsB,stalled,events");
        foreach (MatchupStats s in stats)
        {
            foreach (MatchLine l in s.Lines)
            {
                sb.AppendLine(string.Join(",", s.Matchup.Key, l.Pair, l.Seed, l.TeamAIsHome ? 1 : 0, l.GoalsA, l.GoalsB, R(l.XgA), R(l.XgB), l.ShotsA, l.ShotsB, l.TimeoutsA, l.TimeoutsB, l.Stalled ? 1 : 0, l.Events));
            }
        }

        return sb.ToString();
    }

    private static string DetailsCsv(IReadOnlyList<MatchupStats> stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("matchup,section,key,n,hits,sum,sum2");
        foreach (MatchupStats s in stats)
        {
            foreach ((string section, Tally tally) in new[]
            {
                ("checks", s.Checks), ("energyAtChecks", s.EnergyAtChecks), ("participantEnergy", s.ParticipantEnergy), ("iceTime", s.IceTime), ("shifts", s.Shifts),
                ("playSystem", s.PlaySystem), ("transitions", s.Transitions), ("transitionSeconds", s.TransitionSeconds), ("pressureByZone", s.PressureByZone),
                ("pressureBySystem", s.PressureBySystem), ("chanceTypes", s.ChanceTypes), ("chanceClasses", s.ChanceClasses), ("playXg", s.PlayXg),
                ("entries", s.Entries), ("breakouts", s.Breakouts), ("adaptiveSwitches", s.Adaptive.Switches), ("adaptiveSystemSeconds", s.Adaptive.SystemSeconds),
                ("adaptiveCountered", s.Adaptive.Countered), ("transitionClassification", s.Adaptive.Classification),
            })
            {
                foreach (string key in tally.N.Keys)
                {
                    sb.AppendLine(string.Join(",", s.Matchup.Key, section, "\"" + key + "\"", tally.N[key], tally.Hits[key], R(tally.Sum[key]), R(tally.Sum2[key])));
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>Two decimals with an explicit sign (+0.47, -0.10, 0.00).</summary>
    public static string Signed(double v)
    {
        string text = Math.Abs(v).ToString("0.00", CultureInfo.InvariantCulture);
        return text == "0.00" ? text : (v > 0 ? "+" : "-") + text;
    }

    private static double Mean(Tally tally, string key) => tally.N.TryGetValue(key, out int n) && n > 0 ? tally.Sum[key] / n : 0.0;

    private static double HitShare(Tally tally, string key) => tally.N.TryGetValue(key, out int n) && n > 0 ? tally.Hits[key] / (double)n : 0.0;

    internal static string Pct(double v) => (100.0 * v).ToString("0.0", CultureInfo.InvariantCulture) + " %";

    private static string F0(double v) => v.ToString("0", CultureInfo.InvariantCulture);

    private static string F1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    internal static string F2(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);

    private static string F3(double v) => v.ToString("0.000", CultureInfo.InvariantCulture);

    internal static string R(double v) => v.ToString("R", CultureInfo.InvariantCulture);
}
