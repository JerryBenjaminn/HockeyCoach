using HockeyCoach.Harness.Report;
using HockeyCoach.Harness.Simulation;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Harness.Batch;

/// <summary>One match from team A's point of view (slot 0 = A, slot 1 = B).</summary>
public sealed record MatchLine(
    int Pair, ulong Seed, bool TeamAIsHome, int GoalsA, int GoalsB, double XgA, double XgB, int ShotsA, int ShotsB, int TimeoutsA, int TimeoutsB, bool Stalled, int Events)
{
    /// <summary>Team A's points: win 1, tie 0.5, loss 0 (D-073).</summary>
    public double PointsA => GoalsA > GoalsB ? 1.0 : GoalsA == GoalsB ? 0.5 : 0.0;
}

/// <summary>Counts and sums keyed by a string, in ordinal key order.</summary>
public sealed class Tally
{
    /// <summary>Count per key.</summary>
    public SortedDictionary<string, int> N { get; } = new(StringComparer.Ordinal);

    /// <summary>Hits per key (successes, under pressure, ...).</summary>
    public SortedDictionary<string, int> Hits { get; } = new(StringComparer.Ordinal);

    /// <summary>Sum per key (probabilities, xG, seconds, ...).</summary>
    public SortedDictionary<string, double> Sum { get; } = new(StringComparer.Ordinal);

    /// <summary>Second sum per key.</summary>
    public SortedDictionary<string, double> Sum2 { get; } = new(StringComparer.Ordinal);

    /// <summary>Adds one item.</summary>
    public void Add(string key, bool hit, double sum = 0.0, double sum2 = 0.0)
    {
        N[key] = N.GetValueOrDefault(key) + 1;
        Hits[key] = Hits.GetValueOrDefault(key) + (hit ? 1 : 0);
        Sum[key] = Sum.GetValueOrDefault(key) + sum;
        Sum2[key] = Sum2.GetValueOrDefault(key) + sum2;
    }
}

/// <summary>
/// Everything the batch report shows for one matchup, accumulated match by match in a fixed order (the output does not
/// depend on the thread count).
/// </summary>
public sealed class MatchupStats
{
    /// <summary>Energy buckets of the fatigue section (lower bounds; report bins, not balance values).</summary>
    public static readonly double[] EnergyBins = { 0.0, 0.5, 0.7, 0.9 };

    /// <summary>Shift length above which a shift counts as long (designer's fatigue section, seconds).</summary>
    public const double LongShiftSeconds = 60.0;

    private readonly double _periodSeconds;

    /// <summary>Creates the accumulator.</summary>
    /// <param name="matchup">The AI pair.</param>
    /// <param name="periods">Periods per match.</param>
    /// <param name="periodSeconds">Period length.</param>
    /// <param name="counterTransition">The transition that counters each system (<c>ai.rulebased.transitionBySystem</c>) for the A-T2 section; none = never countered.</param>
    public MatchupStats(Matchup matchup, int periods, double periodSeconds, IReadOnlyDictionary<string, string>? counterTransition = null)
    {
        Matchup = matchup;
        _periodSeconds = periodSeconds;
        Targets = new TargetCounts(periods, periodSeconds);
        Adaptive = new AdaptiveChoice(counterTransition ?? new Dictionary<string, string>(), periods, periodSeconds);
    }

    /// <summary>Adaptive system choice measurement (A-T2 item 8), both slots.</summary>
    public AdaptiveChoice Adaptive { get; }

    /// <summary>The AI pair.</summary>
    public Matchup Matchup { get; }

    /// <summary>Matches in play order.</summary>
    public List<MatchLine> Lines { get; } = new();

    /// <summary>Target counts with A in slot 0 and B in slot 1.</summary>
    public TargetCounts Targets { get; }

    /// <summary>Event-based team stats, A and B.</summary>
    public TeamStats[] Teams { get; } = { new TeamStats(), new TeamStats() };

    /// <summary>Check success by check id (attacking side), sum = probability.</summary>
    public Tally Checks { get; } = new();

    /// <summary>Attacking-side success by energy bin of the attacking participants (checks with attackers).</summary>
    public Tally EnergyAtChecks { get; } = new();

    /// <summary>Mean energy of every participant side at checks, by slot (A / B): N and sum.</summary>
    public Tally ParticipantEnergy { get; } = new();

    /// <summary>Ice time per unit ("A F1", "B D3"): sum = seconds over all matches.</summary>
    public Tally IceTime { get; } = new();

    /// <summary>Shifts by unit type and slot ("A trio"): hit = longer than <see cref="LongShiftSeconds"/>, sum = seconds.</summary>
    public Tally Shifts { get; } = new();

    /// <summary>Play × system ("play vs system"): N = play instances, hit = with a shot attempt, sum = xG, sum2 = goals.</summary>
    public Tally PlaySystem { get; } = new();

    /// <summary>
    /// Transition instruction in force by the opponent's defensive system ("rush vs trap122"): sum = xG for, sum2 = xG
    /// against; N = team segments.
    /// </summary>
    public Tally Transitions { get; } = new();

    /// <summary>Transition seconds in force (same keys).</summary>
    public Tally TransitionSeconds { get; } = new();

    /// <summary>Under-pressure share by action and zone ("pass offensive"): hit = under pressure.</summary>
    public Tally PressureByZone { get; } = new();

    /// <summary>Under-pressure share by action and defending system ("shot trap122").</summary>
    public Tally PressureBySystem { get; } = new();

    /// <summary>xG of shots taken while the shooting team runs a play (event play id set), by slot ("A" / "B"): hit = goal, sum = xG.</summary>
    public Tally PlayXg { get; } = new();

    /// <summary>Shots by chance type: hit = goal, sum = xG.</summary>
    public Tally ChanceTypes { get; } = new();

    /// <summary>Shots by chance class ("none" when unclassed): hit = goal, sum = xG.</summary>
    public Tally ChanceClasses { get; } = new();

    /// <summary>
    /// Controlled zone entries by entering slot, method, strength and defending system ("A carry 2v3 vs trap122", M-8,
    /// M-9): hit = possession kept.
    /// </summary>
    public Tally Entries { get; } = new();

    /// <summary>
    /// Breakout checks by attacking slot, forechecker count f and the defending team's system in force ("A f2 vs
    /// forecheck212", M-9): hit = success.
    /// </summary>
    public Tally Breakouts { get; } = new();

    /// <summary>Kept controlled entries by possession origin and rush vs play (D-085 report section).</summary>
    public EntryOrigins EntryOrigins { get; } = new();

    /// <summary>Adds one match.</summary>
    public void Add(BatchGame batchGame, Rink rink)
    {
        PairedGame game = batchGame.Game;
        MatchResult result = game.Result;
        Func<int, TeamSide> teamOf = MatchCommand.TeamOf(game.Setup);
        TeamSide aSide = game.TeamASide;
        int SlotOfSide(TeamSide side) => side == aSide ? 0 : 1;
        int SlotOf(int id) => SlotOfSide(teamOf(id));
        IReadOnlyList<SimEvent> events = result.Log.Events;

        TeamStats[] stats = TeamStats.From(events, teamOf);
        TeamStats a = stats[(int)aSide];
        TeamStats b = stats[1 - (int)aSide];
        Teams[0].Add(a);
        Teams[1].Add(b);
        int timeoutsA = events.OfType<TimeoutEvent>().Count(t => t.Team == aSide);
        int timeoutsB = events.OfType<TimeoutEvent>().Count(t => t.Team != aSide);
        Lines.Add(new MatchLine(
            game.Pair, game.Seed, game.TeamAIsHome, game.TeamAGoals, game.TeamBGoals, a.Xg, b.Xg, a.Shots, b.Shots, timeoutsA, timeoutsB, result.Stalled, events.Count));
        MatchCommand.AddTargets(Targets, game);

        AddChecks(batchGame.Checks, SlotOfSide);
        AddIceTime(game, aSide);
        AddShifts(game, events, SlotOfSide);
        AddPlays(events, SlotOf);
        AddTransitions(batchGame, events, SlotOf);
        AddPressure(events, teamOf, rink);
        AddChances(events);
        AddPlayXg(events, SlotOf);
        AddEntries(events, SlotOf);
        AddBreakouts(batchGame, SlotOfSide);
        EntryOrigins.Add(events, teamOf, rink, batchGame.Checks.Where(c => c.CheckId == "zoneEntryCarry").Select(c => c.PuckNodeId).ToList());
        Adaptive.Add(batchGame);
    }

    private void AddEntries(IReadOnlyList<SimEvent> events, Func<int, int> slotOf)
    {
        string[] slots = { "A", "B" };
        foreach (ControlledZoneEntryEvent e in events.OfType<ControlledZoneEntryEvent>())
        {
            Entries.Add(Counterplay.EntryKey(slots[slotOf(e.CarrierId)], e.Method, e.Numbers, e.Context.SystemId ?? "-"), e.Outcome == ZoneEntryOutcome.Kept);
        }
    }

    private void AddBreakouts(BatchGame batchGame, Func<TeamSide, int> slotOfSide)
    {
        string[] slots = { "A", "B" };
        foreach (CheckObservation c in batchGame.Checks)
        {
            if (c.CheckId == "breakout" && c.Forecheckers.HasValue)
            {
                string system = OpponentSystemAt(batchGame.Plans[1 - (int)c.AttackingTeam], c.Period, c.Time);
                Breakouts.Add(Counterplay.BreakoutKey(slots[slotOfSide(c.AttackingTeam)], c.Forecheckers.Value, system), c.Success, c.Probability);
            }
        }
    }

    private void AddPlayXg(IReadOnlyList<SimEvent> events, Func<int, int> slotOf)
    {
        string[] slots = { "A", "B" };
        foreach (ShotEvent s in events.OfType<ShotEvent>())
        {
            if (s.Context.PlayId != null)
            {
                PlayXg.Add(slots[slotOf(s.ShooterId)], s.Outcome == ShotOutcome.Goal, s.Xg);
            }
        }
    }

    /// <summary>
    /// The opponent's defensive system in force at (<paramref name="period"/>, <paramref name="time"/>): its last decision
    /// in that period at or before the time; "-" when it has none.
    /// </summary>
    public static string OpponentSystemAt(IReadOnlyList<PlanRecord> opponentPlans, int period, double time)
    {
        string system = "-";
        foreach (PlanRecord plan in opponentPlans)
        {
            if (plan.Period == period && plan.Time <= time)
            {
                system = plan.SystemId;
            }
        }

        return system;
    }

    private static string EnergyBin(double energy)
    {
        for (int i = EnergyBins.Length - 1; i >= 0; i--)
        {
            if (energy >= EnergyBins[i])
            {
                return i == EnergyBins.Length - 1 ? ">= " + EnergyBins[i].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                    : EnergyBins[i].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "-" + EnergyBins[i + 1].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return "< 0";
    }

    private void AddChecks(IReadOnlyList<CheckObservation> checks, Func<TeamSide, int> slotOfSide)
    {
        foreach (CheckObservation c in checks)
        {
            Checks.Add(c.CheckId, c.Success, c.Probability);
            if (!double.IsNaN(c.AttackerEnergy) && c.CheckId != ShotConfigId && c.CheckId != "rebound")
            {
                EnergyAtChecks.Add(EnergyBin(c.AttackerEnergy), c.Success, c.Probability);
            }

            string[] slots = { "A", "B" };
            int attacker = slotOfSide(c.AttackingTeam);
            if (!double.IsNaN(c.AttackerEnergy))
            {
                ParticipantEnergy.Add(slots[attacker], false, c.AttackerEnergy);
            }

            if (!double.IsNaN(c.DefenderEnergy) && c.CheckId != ShotConfigId && c.CheckId != "rebound")
            {
                ParticipantEnergy.Add(slots[1 - attacker], false, c.DefenderEnergy);
            }
        }
    }

    private const string ShotConfigId = "shot";

    private void AddIceTime(PairedGame game, TeamSide aSide)
    {
        if (game.Result.Periods.Count == 0)
        {
            return;
        }

        PeriodSnapshot last = game.Result.Periods[game.Result.Periods.Count - 1];
        foreach ((TeamSide side, string slot) in new[] { (aSide, "A"), (TeamSides.Opponent(aSide), "B") })
        {
            for (int i = 0; i < last.ForwardSeconds[(int)side].Length; i++)
            {
                IceTime.Add(slot + " F" + (i + 1), false, last.ForwardSeconds[(int)side][i]);
            }

            for (int i = 0; i < last.PairSeconds[(int)side].Length; i++)
            {
                IceTime.Add(slot + " D" + (i + 1), false, last.PairSeconds[(int)side][i]);
            }
        }
    }

    /// <summary>
    /// Shift lengths from the public log: the units on the ice at a period's first event (on since the period start), then
    /// every line change; a shift ends at the change or at the period end (data-schema.md perShift: a period start starts a new shift).
    /// </summary>
    private void AddShifts(PairedGame game, IReadOnlyList<SimEvent> events, Func<TeamSide, int> slotOfSide)
    {
        var unitOf = new Dictionary<int, (TeamSide Side, bool Trio, int Index)>();
        foreach ((Team team, TeamSide side) in new[] { (game.Setup.Home, TeamSide.Home), (game.Setup.Away, TeamSide.Away) })
        {
            for (int i = 0; i < team.ForwardLines.Count; i++)
            {
                foreach (Skater s in team.ForwardLines[i].Skaters)
                {
                    unitOf[s.Id] = (side, true, i);
                }
            }

            for (int i = 0; i < team.DefencePairs.Count; i++)
            {
                foreach (Skater s in team.DefencePairs[i].Skaters)
                {
                    unitOf[s.Id] = (side, false, i);
                }
            }
        }

        string[] slots = { "A", "B" };
        int period = 0;
        var start = new Dictionary<(TeamSide, bool), (int Index, double Time)>();
        void Close(double time)
        {
            foreach (KeyValuePair<(TeamSide, bool), (int Index, double Time)> open in start.OrderBy(o => (int)o.Key.Item1).ThenBy(o => o.Key.Item2))
            {
                AddShift(slots[slotOfSide(open.Key.Item1)], open.Key.Item2, time - open.Value.Time);
            }

            start.Clear();
        }

        foreach (SimEvent e in events)
        {
            if (e.Context.Period != period)
            {
                if (period != 0)
                {
                    Close(_periodSeconds);
                }

                period = e.Context.Period;
                foreach (PlayerPlacement p in e.Context.Placement.Players)
                {
                    if (!p.IsGoalie && unitOf.TryGetValue(p.PlayerId, out var unit))
                    {
                        start[(unit.Side, unit.Trio)] = (unit.Index, 0.0);
                    }
                }
            }

            if (e is LineChangeEvent change && change.IncomingIds.Count > 0 && unitOf.TryGetValue(change.IncomingIds[0], out var incoming))
            {
                var key = (incoming.Side, incoming.Trio);
                if (start.TryGetValue(key, out var current) && current.Index != incoming.Index)
                {
                    AddShift(slots[slotOfSide(incoming.Side)], incoming.Trio, e.Context.Time - current.Time);
                    start[key] = (incoming.Index, e.Context.Time);
                }
                else if (!start.ContainsKey(key))
                {
                    start[key] = (incoming.Index, e.Context.Time);
                }
            }
        }

        if (period != 0 && !game.Result.Stalled)
        {
            Close(_periodSeconds);
        }
    }

    private void AddShift(string slot, bool trio, double seconds)
    {
        if (seconds > 0.0)
        {
            Shifts.Add(slot + (trio ? " trio" : " pair"), seconds > LongShiftSeconds, seconds);
        }
    }

    /// <summary>
    /// Play × system: a play instance is a run of consecutive events with the same play id for the same attacking team
    /// (the team of the event's acting player, <see cref="AI.PublicLog.Actor"/>); success = the instance has a shot attempt.
    /// </summary>
    private void AddPlays(IReadOnlyList<SimEvent> events, Func<int, int> slotOf)
    {
        string? currentKey = null;
        int currentSlot = -1;
        bool shot = false;
        double xg = 0.0;
        int goals = 0;
        void Flush()
        {
            if (currentKey != null)
            {
                PlaySystem.Add(currentKey, shot, xg, goals);
            }

            currentKey = null;
            shot = false;
            xg = 0.0;
            goals = 0;
        }

        foreach (SimEvent e in events)
        {
            int? actor = AI.PublicLog.Actor(e);
            string? play = e.Context.PlayId;
            if (play == null || !actor.HasValue)
            {
                if (play == null && !(e is LineChangeEvent))
                {
                    Flush();
                }

                continue;
            }

            string key = play + " vs " + (e.Context.SystemId ?? "-");
            int slot = slotOf(actor.Value);
            if (key != currentKey || slot != currentSlot)
            {
                Flush();
                currentKey = key;
                currentSlot = slot;
            }

            if (e is ShotEvent s)
            {
                shot = true;
                xg += s.Xg;
                goals += s.Outcome == ShotOutcome.Goal ? 1 : 0;
            }
        }

        Flush();
    }

    /// <summary>
    /// Rush vs regroup: each team's transition instruction in force from a decision to the next decision or period end, split
    /// by the opponent's defensive system at the decision (D-078 K3); xG and goals for and against while it is in force.
    /// Several decisions at the same moment (after a timeout): the last.
    /// </summary>
    private void AddTransitions(BatchGame batchGame, IReadOnlyList<SimEvent> events, Func<int, int> slotOf)
    {
        TeamSide aSide = batchGame.Game.TeamASide;
        for (int side = 0; side < 2; side++)
        {
            IReadOnlyList<PlanRecord> plans = batchGame.Plans[side];
            IReadOnlyList<PlanRecord> opponentPlans = batchGame.Plans[1 - side];
            int slot = (TeamSide)side == aSide ? 0 : 1;
            for (int i = 0; i < plans.Count; i++)
            {
                PlanRecord plan = plans[i];
                if (i + 1 < plans.Count && plans[i + 1].Period == plan.Period && plans[i + 1].Time == plan.Time)
                {
                    continue;
                }

                double end = i + 1 < plans.Count && plans[i + 1].Period == plan.Period ? plans[i + 1].Time : _periodSeconds;
                string key = CoachRegistry.TransitionName(plan.Transition) + " vs " + OpponentSystemAt(opponentPlans, plan.Period, plan.Time);
                double xgFor = 0.0;
                double xgAgainst = 0.0;
                int goalsFor = 0;
                foreach (SimEvent e in events)
                {
                    if (e is ShotEvent s && e.Context.Period == plan.Period && e.Context.Time >= plan.Time && e.Context.Time < end)
                    {
                        if (slotOf(s.ShooterId) == slot)
                        {
                            xgFor += s.Xg;
                            goalsFor += s.Outcome == ShotOutcome.Goal ? 1 : 0;
                        }
                        else
                        {
                            xgAgainst += s.Xg;
                        }
                    }
                }

                Transitions.Add(key, false, xgFor, xgAgainst);
                TransitionSeconds.Add(key, false, Math.Max(0.0, end - plan.Time), goalsFor);
            }
        }
    }

    private void AddPressure(IReadOnlyList<SimEvent> events, Func<int, TeamSide> teamOf, Rink rink)
    {
        foreach (SimEvent e in events)
        {
            (string Kind, int Actor, bool Under)? item = e switch
            {
                PassEvent p => ("pass", p.PasserId, p.UnderPressure),
                ShotEvent s => ("shot", s.ShooterId, s.UnderPressure),
                _ => null,
            };
            if (item == null)
            {
                continue;
            }

            PlayerPlacement? placement = e.Context.Placement.Find(item.Value.Actor);
            if (placement == null)
            {
                continue;
            }

            GridPoint rinkPoint = new(rink.XOf(placement.NodeId), rink.YOf(placement.NodeId));
            GridPoint view = TeamFrame.ToTeamView(rinkPoint, teamOf(item.Value.Actor), rink);
            string zone = rink.ZoneAtX(view.X).ToString().ToLowerInvariant();
            PressureByZone.Add(item.Value.Kind + " " + zone, item.Value.Under);
            PressureBySystem.Add(item.Value.Kind + " " + (e.Context.SystemId ?? "-"), item.Value.Under);
        }
    }

    private void AddChances(IReadOnlyList<SimEvent> events)
    {
        foreach (ShotEvent s in events.OfType<ShotEvent>())
        {
            bool goal = s.Outcome == ShotOutcome.Goal;
            ChanceTypes.Add(s.ChanceType.ToString(), goal, s.Xg);
            ChanceClasses.Add(s.ChanceClass?.ToString() ?? "none", goal, s.Xg);
        }
    }
}
