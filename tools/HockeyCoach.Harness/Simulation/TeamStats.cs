using HockeyCoach.Sim.Events;

namespace HockeyCoach.Harness.Simulation;

/// <summary>One team's counts over a set of events (a period, a match or a batch), aggregated from the event log.</summary>
public sealed class TeamStats
{
    /// <summary>Goals.</summary>
    public int Goals { get; set; }

    /// <summary>Shot attempts (all outcomes).</summary>
    public int Shots { get; set; }

    /// <summary>Shots on target (goal or saved).</summary>
    public int OnTarget { get; set; }

    /// <summary>Blocked shots.</summary>
    public int Blocked { get; set; }

    /// <summary>Missed shots (in play or out of play).</summary>
    public int Missed { get; set; }

    /// <summary>Shots with the shooter under pressure (M-7).</summary>
    public int ShotsUnderPressure { get; set; }

    /// <summary>Sum of xG of all attempts.</summary>
    public double Xg { get; set; }

    /// <summary>Attempts by chance type.</summary>
    public Dictionary<ChanceType, int> ShotsByType { get; } = new();

    /// <summary>xG by chance type.</summary>
    public Dictionary<ChanceType, double> XgByType { get; } = new();

    /// <summary>Goals by chance type.</summary>
    public Dictionary<ChanceType, int> GoalsByType { get; } = new();

    /// <summary>Attempts by chance class (top, good, moderate; unclassed not counted).</summary>
    public Dictionary<ChanceClass, int> ShotsByClass { get; } = new();

    /// <summary>Passes.</summary>
    public int Passes { get; set; }

    /// <summary>Completed passes.</summary>
    public int PassesCompleted { get; set; }

    /// <summary>Passes under pressure.</summary>
    public int PassesUnderPressure { get; set; }

    /// <summary>Completed passes under pressure.</summary>
    public int PassesUnderPressureCompleted { get; set; }

    /// <summary>Carry entries.</summary>
    public int CarryEntries { get; set; }

    /// <summary>Carry entries that kept the puck.</summary>
    public int CarryEntriesKept { get; set; }

    /// <summary>Pass entries.</summary>
    public int PassEntries { get; set; }

    /// <summary>Pass entries that kept the puck.</summary>
    public int PassEntriesKept { get; set; }

    /// <summary>Entries by "N vs M".</summary>
    public SortedDictionary<string, int> EntryNumbers { get; } = new(StringComparer.Ordinal);

    /// <summary>Dumps.</summary>
    public int Dumps { get; set; }

    /// <summary>Puck battles won (incl. dump battles).</summary>
    public int BattlesWon { get; set; }

    /// <summary>Puck battles without a winner.</summary>
    public int BattlesNoWinner { get; set; }

    /// <summary>Puck battles lost.</summary>
    public int BattlesLost { get; set; }

    /// <summary>Faceoffs taken.</summary>
    public int Faceoffs { get; set; }

    /// <summary>Faceoffs won.</summary>
    public int FaceoffsWon { get; set; }

    /// <summary>Takeaways and interceptions made.</summary>
    public int Takeaways { get; set; }

    /// <summary>Adds another team's counts.</summary>
    public void Add(TeamStats other)
    {
        Goals += other.Goals;
        Shots += other.Shots;
        OnTarget += other.OnTarget;
        Blocked += other.Blocked;
        Missed += other.Missed;
        ShotsUnderPressure += other.ShotsUnderPressure;
        Xg += other.Xg;
        Merge(ShotsByType, other.ShotsByType);
        Merge(XgByType, other.XgByType);
        Merge(GoalsByType, other.GoalsByType);
        Merge(ShotsByClass, other.ShotsByClass);
        Passes += other.Passes;
        PassesCompleted += other.PassesCompleted;
        PassesUnderPressure += other.PassesUnderPressure;
        PassesUnderPressureCompleted += other.PassesUnderPressureCompleted;
        CarryEntries += other.CarryEntries;
        CarryEntriesKept += other.CarryEntriesKept;
        PassEntries += other.PassEntries;
        PassEntriesKept += other.PassEntriesKept;
        foreach (KeyValuePair<string, int> entry in other.EntryNumbers)
        {
            EntryNumbers[entry.Key] = EntryNumbers.GetValueOrDefault(entry.Key) + entry.Value;
        }

        Dumps += other.Dumps;
        BattlesWon += other.BattlesWon;
        BattlesNoWinner += other.BattlesNoWinner;
        BattlesLost += other.BattlesLost;
        Faceoffs += other.Faceoffs;
        FaceoffsWon += other.FaceoffsWon;
        Takeaways += other.Takeaways;
    }

    private static void Merge<TKey>(Dictionary<TKey, int> into, Dictionary<TKey, int> from)
        where TKey : notnull
    {
        foreach (KeyValuePair<TKey, int> entry in from)
        {
            into[entry.Key] = into.GetValueOrDefault(entry.Key) + entry.Value;
        }
    }

    private static void Merge<TKey>(Dictionary<TKey, double> into, Dictionary<TKey, double> from)
        where TKey : notnull
    {
        foreach (KeyValuePair<TKey, double> entry in from)
        {
            into[entry.Key] = into.GetValueOrDefault(entry.Key) + entry.Value;
        }
    }

    /// <summary>Aggregates events into home and away stats; <paramref name="teamOf"/> maps a player id to its team.</summary>
    public static TeamStats[] From(IEnumerable<SimEvent> events, Func<int, TeamSide> teamOf)
    {
        var stats = new[] { new TeamStats(), new TeamStats() };
        foreach (SimEvent e in events)
        {
            switch (e)
            {
                case ShotEvent shot:
                    AddShot(stats[(int)teamOf(shot.ShooterId)], shot);
                    break;
                case PassEvent pass:
                    TeamStats p = stats[(int)teamOf(pass.PasserId)];
                    p.Passes++;
                    p.PassesCompleted += pass.Succeeded ? 1 : 0;
                    p.PassesUnderPressure += pass.UnderPressure ? 1 : 0;
                    p.PassesUnderPressureCompleted += pass.UnderPressure && pass.Succeeded ? 1 : 0;
                    break;
                case ControlledZoneEntryEvent entry:
                    TeamStats z = stats[(int)teamOf(entry.CarrierId)];
                    bool kept = entry.Outcome == ZoneEntryOutcome.Kept;
                    if (entry.Method == ZoneEntryMethod.Carry)
                    {
                        z.CarryEntries++;
                        z.CarryEntriesKept += kept ? 1 : 0;
                    }
                    else
                    {
                        z.PassEntries++;
                        z.PassEntriesKept += kept ? 1 : 0;
                    }

                    string key = entry.Numbers.ToString();
                    z.EntryNumbers[key] = z.EntryNumbers.GetValueOrDefault(key) + 1;
                    break;
                case DumpInEvent dump:
                    TeamSide dumper = teamOf(dump.ShooterId);
                    stats[(int)dumper].Dumps++;
                    AddBattle(stats, dumper, dump.BattleOutcome);
                    break;
                case PuckBattleEvent battle:
                    AddBattle(stats, teamOf(battle.AttackerIds[0]), battle.Outcome);
                    break;
                case FaceoffEvent faceoff:
                    stats[0].Faceoffs++;
                    stats[1].Faceoffs++;
                    stats[(int)faceoff.Winner].FaceoffsWon++;
                    break;
                case TurnoverEvent turnover:
                    stats[(int)teamOf(turnover.TakerId)].Takeaways++;
                    break;
            }
        }

        return stats;
    }

    private static void AddShot(TeamStats s, ShotEvent shot)
    {
        s.Shots++;
        s.Xg += shot.Xg;
        s.ShotsUnderPressure += shot.UnderPressure ? 1 : 0;
        s.ShotsByType[shot.ChanceType] = s.ShotsByType.GetValueOrDefault(shot.ChanceType) + 1;
        s.XgByType[shot.ChanceType] = s.XgByType.GetValueOrDefault(shot.ChanceType) + shot.Xg;
        if (shot.ChanceClass.HasValue)
        {
            s.ShotsByClass[shot.ChanceClass.Value] = s.ShotsByClass.GetValueOrDefault(shot.ChanceClass.Value) + 1;
        }

        switch (shot.Outcome)
        {
            case ShotOutcome.Goal:
                s.Goals++;
                s.OnTarget++;
                s.GoalsByType[shot.ChanceType] = s.GoalsByType.GetValueOrDefault(shot.ChanceType) + 1;
                break;
            case ShotOutcome.Saved:
                s.OnTarget++;
                break;
            case ShotOutcome.Blocked:
                s.Blocked++;
                break;
            default:
                s.Missed++;
                break;
        }
    }

    private static void AddBattle(TeamStats[] stats, TeamSide attacker, BattleOutcome outcome)
    {
        TeamStats a = stats[(int)attacker];
        TeamStats d = stats[1 - (int)attacker];
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
}
