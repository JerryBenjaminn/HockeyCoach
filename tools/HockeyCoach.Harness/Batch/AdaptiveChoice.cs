using System.Text;
using HockeyCoach.AI;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Harness.Batch;

/// <summary>
/// Report-only measurement of the adaptive system choice (data-schema.md AI-2, A-T2 item 8; D-082), for both teams of
/// every match: (a) own system switches per match and seconds in each system; (b) the share of time the opponent's
/// transition instruction is the counter to the own system (<c>ai.rulebased.transitionBySystem</c>: forecheck212 + rush,
/// trap122 + regroup); (c) the public-log classification of the opponent's transitions (<see cref="TransitionStyle"/>)
/// against the opponent's true instruction in force (known to the harness from the recorded plans).
/// </summary>
public sealed class AdaptiveChoice
{
    private readonly IReadOnlyDictionary<string, string> _counter;
    private readonly double _periodSeconds;
    private readonly int _periods;

    /// <summary>Creates the accumulator.</summary>
    /// <param name="counterTransition">The transition that counters each system (<c>ai.rulebased.transitionBySystem</c>).</param>
    /// <param name="periods">Periods per match.</param>
    /// <param name="periodSeconds">Period length.</param>
    public AdaptiveChoice(IReadOnlyDictionary<string, string> counterTransition, int periods, double periodSeconds)
    {
        _counter = counterTransition;
        _periods = periods;
        _periodSeconds = periodSeconds;
    }

    /// <summary>Own system changes between consecutive decisions, by slot ("A"): N = matches, sum = switches.</summary>
    public Tally Switches { get; } = new();

    /// <summary>Seconds with each own system in force, by "slot system" ("A trap122"): sum = seconds.</summary>
    public Tally SystemSeconds { get; } = new();

    /// <summary>Countered time by slot: sum = seconds the opponent's transition counters the own system, sum2 = all seconds.</summary>
    public Tally Countered { get; } = new();

    /// <summary>
    /// Classified opponent transitions by observing slot and the opponent's true instruction ("A rush"): hit = the
    /// classification matches the instruction.
    /// </summary>
    public Tally Classification { get; } = new();

    /// <summary>Adds one match.</summary>
    public void Add(BatchGame batchGame)
    {
        TeamSide aSide = batchGame.Game.TeamASide;
        IReadOnlyList<SimEvent> events = batchGame.Game.Result.Log.Events;
        string[] slots = { "A", "B" };
        for (int side = 0; side < 2; side++)
        {
            string slot = slots[(TeamSide)side == aSide ? 0 : 1];
            IReadOnlyList<PlanRecord> own = Effective(batchGame.Plans[side]);
            IReadOnlyList<PlanRecord> opponent = Effective(batchGame.Plans[1 - side]);
            AddSwitches(slot, own);
            if (!batchGame.Game.Result.Stalled)
            {
                AddTime(slot, own, opponent);
            }

            Team team = (TeamSide)side == TeamSide.Home ? batchGame.Game.Setup.Home : batchGame.Game.Setup.Away;
            AddClassification(slot, events, new SortedSet<int>(team.Skaters.Select(s => s.Id)), opponent);
        }
    }

    /// <summary>Drops a decision followed by another at the same moment (the after-timeout round replaces it).</summary>
    public static IReadOnlyList<PlanRecord> Effective(IReadOnlyList<PlanRecord> plans)
    {
        var effective = new List<PlanRecord>();
        for (int i = 0; i < plans.Count; i++)
        {
            if (i + 1 < plans.Count && plans[i + 1].Period == plans[i].Period && plans[i + 1].Time == plans[i].Time)
            {
                continue;
            }

            effective.Add(plans[i]);
        }

        return effective;
    }

    /// <summary>The plan in force at (period, time): the last one in that period at or before the time, or null.</summary>
    public static PlanRecord? At(IReadOnlyList<PlanRecord> plans, int period, double time)
    {
        PlanRecord? found = null;
        foreach (PlanRecord plan in plans)
        {
            if (plan.Period == period && plan.Time <= time)
            {
                found = plan;
            }
        }

        return found;
    }

    private void AddSwitches(string slot, IReadOnlyList<PlanRecord> own)
    {
        int switches = 0;
        for (int i = 1; i < own.Count; i++)
        {
            switches += own[i].SystemId != own[i - 1].SystemId ? 1 : 0;
        }

        Switches.Add(slot, false, switches);
    }

    private void AddTime(string slot, IReadOnlyList<PlanRecord> own, IReadOnlyList<PlanRecord> opponent)
    {
        for (int period = 1; period <= _periods; period++)
        {
            var bounds = own.Concat(opponent).Where(p => p.Period == period).Select(p => p.Time).Append(_periodSeconds).Distinct().OrderBy(t => t).ToList();
            for (int i = 0; i + 1 < bounds.Count; i++)
            {
                double start = bounds[i];
                double seconds = bounds[i + 1] - start;
                PlanRecord? mine = At(own, period, start);
                PlanRecord? theirs = At(opponent, period, start);
                if (mine == null || theirs == null || seconds <= 0.0)
                {
                    continue;
                }

                SystemSeconds.Add(slot + " " + mine.SystemId, false, seconds);
                bool countered = _counter.TryGetValue(mine.SystemId, out string? counter) && counter == CoachRegistry.TransitionName(theirs.Transition);
                Countered.Add(slot, countered, countered ? seconds : 0.0, seconds);
            }
        }
    }

    private void AddClassification(string slot, IReadOnlyList<SimEvent> events, SortedSet<int> own, IReadOnlyList<PlanRecord> opponent)
    {
        foreach (TransitionObservation o in TransitionStyle.Classify(events, own))
        {
            SimEvent e = events[o.EventIndex];
            PlanRecord? plan = At(opponent, e.Context.Period, e.Context.Time);
            if (plan == null)
            {
                continue;
            }

            string truth = CoachRegistry.TransitionName(plan.Transition);
            string seen = o.Style == TransitionStyleClass.Rush ? "rush" : "regroup";
            Classification.Add(slot + " " + truth, seen == truth);
        }
    }

    /// <summary>Writes the A-T2 section for the adaptive slots of the given matchups.</summary>
    public static void Write(StringBuilder md, IReadOnlyList<MatchupStats> stats)
    {
        md.AppendLine("## Adaptive system choice (A-T2, D-082)");
        md.AppendLine();
        md.AppendLine("Per adaptive team. Switches = own system changes between consecutive stoppage decisions (all modes). Countered = share of time the opponent's transition instruction counters the own system (transitionBySystem: forecheck212 + rush, trap122 + regroup). Classification = public-log class of the opponent's transition (A-T2 item 2) vs its true instruction; target >= 90 %.");
        md.AppendLine();
        md.AppendLine("| Matchup | Slot | Switches / match | Time by system | Countered | Classification hit (n) | vs rush | vs regroup |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (MatchupStats s in stats)
        {
            foreach ((string slot, string coach) in new[] { ("A", s.Matchup.A), ("B", s.Matchup.B) })
            {
                if (coach != CoachRegistry.Adaptive)
                {
                    continue;
                }

                AdaptiveChoice c = s.Adaptive;
                int matches = c.Switches.N.GetValueOrDefault(slot);
                double switches = matches > 0 ? c.Switches.Sum[slot] / matches : 0.0;
                var systemKeys = c.SystemSeconds.Sum.Keys.Where(k => k.StartsWith(slot + " ", StringComparison.Ordinal)).ToList();
                double totalSeconds = systemKeys.Sum(k => c.SystemSeconds.Sum[k]);
                string bySystem = string.Join(", ", systemKeys.Select(k => k[2..] + " " + BatchReport.Pct(totalSeconds > 0 ? c.SystemSeconds.Sum[k] / totalSeconds : 0.0)));
                double countered = c.Countered.Sum2.GetValueOrDefault(slot) > 0 ? c.Countered.Sum[slot] / c.Countered.Sum2[slot] : 0.0;
                Rate rush = Hit(c.Classification, slot + " rush");
                Rate regroup = Hit(c.Classification, slot + " regroup");
                Rate all = rush + regroup;
                md.AppendLine("| " + s.Matchup.Label + " | " + slot + " | " + BatchReport.F2(switches) + " | " + bySystem + " | " + BatchReport.Pct(countered)
                    + " | " + BatchReport.Pct(all.Share) + " (" + all.N + ") | " + BatchReport.Pct(rush.Share) + " (" + rush.N + ") | " + BatchReport.Pct(regroup.Share) + " (" + regroup.N + ") |");
            }
        }

        md.AppendLine();
    }

    private static Rate Hit(Tally tally, string key) => new(tally.N.GetValueOrDefault(key), tally.Hits.GetValueOrDefault(key));
}
