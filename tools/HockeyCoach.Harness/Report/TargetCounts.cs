using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;

namespace HockeyCoach.Harness.Report;

/// <summary>
/// Raw sums for the target metrics of one team (docs/data-schema.md, targets.json, "Mittarit ja niiden laskenta").
/// Only regulation time counts (Q-052).
/// </summary>
public sealed class TeamTargetCounts
{
    /// <summary>Team matches added.</summary>
    public int Matches { get; set; }

    /// <summary>Goals (shots with outcome goal).</summary>
    public int Goals { get; set; }

    /// <summary>Shot attempts, all outcomes (Q-050).</summary>
    public int Shots { get; set; }

    /// <summary>Goals and saved shots.</summary>
    public int OnTarget { get; set; }

    /// <summary>Own attempts blocked by the opponent.</summary>
    public int Blocked { get; set; }

    /// <summary>Missed attempts, out of play included.</summary>
    public int Missed { get; set; }

    /// <summary>Shots with <c>underPressure</c> (M-7, D-070 exception included).</summary>
    public int ShotsUnderPressure { get; set; }

    /// <summary>Shots at 5 vs 5.</summary>
    public int Shots5v5 { get; set; }

    /// <summary>Shots at 5 vs 5 by chance type.</summary>
    public SortedDictionary<ChanceType, int> Shots5v5ByType { get; } = new();

    /// <summary>Shots at 5 vs 5 by chance class.</summary>
    public SortedDictionary<ChanceClass, int> Chances5v5ByClass { get; } = new();

    /// <summary>Shots with a chance class, all strengths.</summary>
    public int ChancesAll { get; set; }

    /// <summary>Sum of attempt xG, all strengths.</summary>
    public double Xg { get; set; }

    /// <summary>Sum of attempt xG at 5 vs 5.</summary>
    public double Xg5v5 { get; set; }

    /// <summary>Even-strength attempt xG by chance type.</summary>
    public SortedDictionary<ChanceType, double> EvenStrengthXgByType { get; } = new();

    /// <summary>xG of every attempt, in log order.</summary>
    public List<double> ShotXg { get; } = new();

    /// <summary>Passes.</summary>
    public int Passes { get; set; }

    /// <summary>Completed passes.</summary>
    public int PassesCompleted { get; set; }

    /// <summary>Passes at 5 vs 5.</summary>
    public int Passes5v5 { get; set; }

    /// <summary>Completed passes at 5 vs 5.</summary>
    public int Passes5v5Completed { get; set; }

    /// <summary>Passes under pressure.</summary>
    public int PassesUnderPressure { get; set; }

    /// <summary>Completed passes under pressure.</summary>
    public int PassesUnderPressureCompleted { get; set; }

    /// <summary>Puck battles won (battle and dump-in events).</summary>
    public int BattlesWon { get; set; }

    /// <summary>Puck battles without a winner.</summary>
    public int BattlesNoWinner { get; set; }

    /// <summary>Puck battles lost.</summary>
    public int BattlesLost { get; set; }

    /// <summary>Controlled carry entries.</summary>
    public int CarryEntries { get; set; }

    /// <summary>Carry entries that kept the puck.</summary>
    public int CarryEntriesKept { get; set; }

    /// <summary>Controlled pass entries.</summary>
    public int PassEntries { get; set; }

    /// <summary>Pass entries that kept the puck.</summary>
    public int PassEntriesKept { get; set; }

    /// <summary>Offensive-zone seconds, all strengths.</summary>
    public double ZoneSeconds { get; set; }

    /// <summary>Forward trio ice seconds.</summary>
    public double ForwardSeconds { get; set; }

    /// <summary>Forward trio shifts (a period start begins a new shift, Q-047).</summary>
    public int ForwardShifts { get; set; }

    /// <summary>Defence pair ice seconds.</summary>
    public double PairSeconds { get; set; }

    /// <summary>Defence pair shifts.</summary>
    public int PairShifts { get; set; }
}

/// <summary>
/// Accumulates the target metrics over a set of matches for two teams (slot 0 and slot 1, e.g. team A and team B of a
/// paired series, regardless of venue). Used by <c>match --count</c> and reusable by <c>batch</c>.
/// </summary>
public sealed class TargetCounts
{
    /// <summary>Skaters per team in the "5v5" metrics.</summary>
    public const int FiveOnFiveSkaters = 5;

    private readonly int _regulationPeriods;
    private readonly double _regulationSeconds;

    /// <summary>Creates an empty accumulator.</summary>
    /// <param name="regulationPeriods">Periods of regulation time (tuning <c>time.periods</c>); later events are ignored (Q-052).</param>
    /// <param name="periodSeconds">Length of a period (tuning <c>time.periodSeconds</c>).</param>
    public TargetCounts(int regulationPeriods, double periodSeconds)
    {
        _regulationPeriods = regulationPeriods;
        _regulationSeconds = regulationPeriods * periodSeconds;
    }

    /// <summary>Per-team sums: index 0 and 1 are the two team slots.</summary>
    public TeamTargetCounts[] Teams { get; } = { new TeamTargetCounts(), new TeamTargetCounts() };

    /// <summary>Matches added.</summary>
    public int Matches { get; private set; }

    /// <summary>
    /// The smart AI's record against the random AI (<c>smartVsRandomWinRate</c>, D-073), or null when that matchup was
    /// not played.
    /// </summary>
    public MatchupRecord? SmartVsRandom { get; set; }

    /// <summary>Goals by both teams.</summary>
    public int Goals => Teams[0].Goals + Teams[1].Goals;

    /// <summary>Faceoff events, period openings included.</summary>
    public int Faceoffs { get; private set; }

    /// <summary>Sum over matches of regulation seconds / faceoffs (meanTimeBetweenStoppages).</summary>
    public double StoppageGapSum { get; private set; }

    /// <summary>Matches with at least one faceoff (denominator of the stoppage gap mean).</summary>
    public int MatchesWithFaceoffs { get; private set; }

    /// <summary>True while every regulation event added so far was at 5 vs 5, so zone time equals 5v5 zone time.</summary>
    public bool AllFiveOnFive { get; private set; } = true;

    /// <summary>Adds one match.</summary>
    /// <param name="events">The match event log.</param>
    /// <param name="periods">Period snapshots of the match.</param>
    /// <param name="slotOf">Maps a player id to the team slot (0 or 1).</param>
    /// <param name="slotOfSide">Maps a venue side to the team slot.</param>
    public void AddMatch(IEnumerable<SimEvent> events, IReadOnlyList<PeriodSnapshot> periods, Func<int, int> slotOf, Func<TeamSide, int> slotOfSide)
    {
        Matches++;
        Teams[0].Matches++;
        Teams[1].Matches++;
        int faceoffs = 0;
        bool matchFiveOnFive = true;
        foreach (SimEvent e in events)
        {
            if (e.Context.Period > _regulationPeriods)
            {
                continue;
            }

            bool fiveOnFive = IsFiveOnFive(e.Context.Strength);
            matchFiveOnFive &= fiveOnFive;
            switch (e)
            {
                case ShotEvent shot:
                    AddShot(Teams[slotOf(shot.ShooterId)], shot, fiveOnFive);
                    break;
                case PassEvent pass:
                    AddPass(Teams[slotOf(pass.PasserId)], pass, fiveOnFive);
                    break;
                case ControlledZoneEntryEvent entry:
                    AddEntry(Teams[slotOf(entry.CarrierId)], entry);
                    break;
                case DumpInEvent dump:
                    AddBattle(slotOf(dump.ShooterId), dump.BattleOutcome);
                    break;
                case PuckBattleEvent battle:
                    AddBattle(slotOf(battle.AttackerIds[0]), battle.Outcome);
                    break;
                case FaceoffEvent:
                    faceoffs++;
                    break;
            }
        }

        Faceoffs += faceoffs;
        if (faceoffs > 0)
        {
            StoppageGapSum += _regulationSeconds / faceoffs;
            MatchesWithFaceoffs++;
        }

        AllFiveOnFive &= matchFiveOnFive;
        AddSnapshot(periods, slotOfSide);
    }

    private static bool IsFiveOnFive(Strength strength)
    {
        return strength.HomeSkaters == FiveOnFiveSkaters && strength.AwaySkaters == FiveOnFiveSkaters;
    }

    private static void AddShot(TeamTargetCounts t, ShotEvent shot, bool fiveOnFive)
    {
        t.Shots++;
        t.Xg += shot.Xg;
        t.ShotXg.Add(shot.Xg);
        t.ShotsUnderPressure += shot.UnderPressure ? 1 : 0;
        t.ChancesAll += shot.ChanceClass.HasValue ? 1 : 0;
        switch (shot.Outcome)
        {
            case ShotOutcome.Goal:
                t.Goals++;
                t.OnTarget++;
                break;
            case ShotOutcome.Saved:
                t.OnTarget++;
                break;
            case ShotOutcome.Blocked:
                t.Blocked++;
                break;
            default:
                t.Missed++;
                break;
        }

        if (shot.Context.Strength.HomeSkaters == shot.Context.Strength.AwaySkaters)
        {
            t.EvenStrengthXgByType[shot.ChanceType] = t.EvenStrengthXgByType.GetValueOrDefault(shot.ChanceType) + shot.Xg;
        }

        if (fiveOnFive)
        {
            t.Shots5v5++;
            t.Xg5v5 += shot.Xg;
            t.Shots5v5ByType[shot.ChanceType] = t.Shots5v5ByType.GetValueOrDefault(shot.ChanceType) + 1;
            if (shot.ChanceClass.HasValue)
            {
                t.Chances5v5ByClass[shot.ChanceClass.Value] = t.Chances5v5ByClass.GetValueOrDefault(shot.ChanceClass.Value) + 1;
            }
        }
    }

    private static void AddPass(TeamTargetCounts t, PassEvent pass, bool fiveOnFive)
    {
        int completed = pass.Succeeded ? 1 : 0;
        t.Passes++;
        t.PassesCompleted += completed;
        if (fiveOnFive)
        {
            t.Passes5v5++;
            t.Passes5v5Completed += completed;
        }

        if (pass.UnderPressure)
        {
            t.PassesUnderPressure++;
            t.PassesUnderPressureCompleted += completed;
        }
    }

    private static void AddEntry(TeamTargetCounts t, ControlledZoneEntryEvent entry)
    {
        int kept = entry.Outcome == ZoneEntryOutcome.Kept ? 1 : 0;
        if (entry.Method == ZoneEntryMethod.Carry)
        {
            t.CarryEntries++;
            t.CarryEntriesKept += kept;
        }
        else
        {
            t.PassEntries++;
            t.PassEntriesKept += kept;
        }
    }

    private void AddBattle(int attackerSlot, BattleOutcome outcome)
    {
        TeamTargetCounts a = Teams[attackerSlot];
        TeamTargetCounts d = Teams[1 - attackerSlot];
        switch (outcome)
        {
            case BattleOutcome.Win:
                a.BattlesWon++;
                d.BattlesLost++;
                break;
            case BattleOutcome.Loss:
                a.BattlesLost++;
                d.BattlesWon++;
                break;
            default:
                a.BattlesNoWinner++;
                d.BattlesNoWinner++;
                break;
        }
    }

    private void AddSnapshot(IReadOnlyList<PeriodSnapshot> periods, Func<TeamSide, int> slotOfSide)
    {
        // Snapshots are cumulative for the match: the last regulation one holds the regulation totals.
        PeriodSnapshot? last = periods.LastOrDefault(p => p.Period <= _regulationPeriods);
        if (last == null)
        {
            return;
        }

        foreach (TeamSide side in new[] { TeamSide.Home, TeamSide.Away })
        {
            int s = (int)side;
            TeamTargetCounts t = Teams[slotOfSide(side)];
            t.ZoneSeconds += last.OffensiveZoneSeconds[s];
            t.ForwardSeconds += last.ForwardSeconds[s].Sum();
            t.ForwardShifts += last.ForwardShifts[s].Sum();
            t.PairSeconds += last.PairSeconds[s].Sum();
            t.PairShifts += last.PairShifts[s].Sum();
        }
    }
}
