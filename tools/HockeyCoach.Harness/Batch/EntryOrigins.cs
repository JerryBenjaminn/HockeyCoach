using System.Globalization;
using System.Text;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Harness.Batch;

/// <summary>
/// One successful controlled zone entry with where and how its possession started (data-schema.md, Paluukarvaus
/// suorahyökkäyksessä, report section; D-084, D-085).
/// </summary>
/// <param name="Zone">Zone of the possession's start node, entering team's view: "own", "neutral", "offensive".</param>
/// <param name="Gain">How the possession started: "faceoff", "turnover" (takeaway or interception), "loose" (puck battle or dump-in won), "other".</param>
/// <param name="Mode">"rush" (no play id on the entry) or "play".</param>
/// <param name="Group">M-8 group of the logged numbers.</param>
/// <param name="Behind">Defending skaters behind the puck at the M-8 moment (x &lt; puck x, entering team's view).</param>
/// <param name="Level">Defending skaters level with the puck.</param>
/// <param name="Ahead">Defending skaters between the puck and their goal (x &gt; puck x).</param>
public sealed record EntryOrigin(string Zone, string Gain, string Mode, Report.EntryStrengthGroup Group, int Behind, int Level, int Ahead);

/// <summary>
/// The back-check report section: N vs M group shares and mean defenders behind / level / ahead of the puck for successful
/// controlled zone entries, split by the possession's start zone and gain and by rush vs play. Possession as in O-12: it
/// starts when a team gains the puck from the opponent (turnover, a won battle) or wins a faceoff, and ends when the
/// opponent gains the puck or at a stoppage. Read from the public log only; not a target and not an event.
/// </summary>
public sealed class EntryOrigins
{
    /// <summary>Zone names in report order.</summary>
    public static readonly string[] Zones = { "own", "neutral", "offensive" };

    /// <summary>Gain names in report order.</summary>
    public static readonly string[] Gains = { "turnover", "loose", "faceoff", "other" };

    /// <summary>Mode names in report order.</summary>
    public static readonly string[] Modes = { "rush", "play" };

    /// <summary>
    /// Kept entries by "zone gain mode": N = entries, Hits = N &gt; M, Sum = N &lt; M, Sum2 = N = M.
    /// </summary>
    public Tally Strength { get; } = new();

    /// <summary>Kept entries by "zone gain mode": N = entries, Hits = defenders ahead, Sum = behind, Sum2 = level (totals).</summary>
    public Tally Defenders { get; } = new();

    /// <summary>
    /// Kept carry entries: N = all, Hits = the logged carrier is not the puck holder in the entry's placement (Q-075: a
    /// mirrored play whose entry pass was skipped).
    /// </summary>
    public Tally Carries { get; } = new();

    /// <summary>Adds the kept entries of one match.</summary>
    /// <param name="events">The match log.</param>
    /// <param name="teamOf">Team of a player id.</param>
    /// <param name="rink">The rink.</param>
    /// <param name="carryPuckNodes">Puck node of every <c>zoneEntryCarry</c> check in order (<see cref="Sim.Shift.CheckObservation.PuckNodeId"/>), or null.</param>
    public void Add(IReadOnlyList<SimEvent> events, Func<int, TeamSide> teamOf, Rink rink, IReadOnlyList<int>? carryPuckNodes = null)
    {
        foreach (ControlledZoneEntryEvent e in events.OfType<ControlledZoneEntryEvent>())
        {
            if (e.Method == ZoneEntryMethod.Carry && e.Outcome == ZoneEntryOutcome.Kept)
            {
                Carries.Add("kept carry", e.Context.Placement.PuckCarrierId != e.CarrierId);
            }
        }

        foreach (EntryOrigin o in Classify(events, teamOf, rink, carryPuckNodes))
        {
            string key = o.Zone + " " + o.Gain + " " + o.Mode;
            Strength.Add(
                key,
                o.Group == Report.EntryStrengthGroup.Advantage,
                o.Group == Report.EntryStrengthGroup.Disadvantage ? 1.0 : 0.0,
                o.Group == Report.EntryStrengthGroup.Even ? 1.0 : 0.0);
            Defenders.N[key] = Defenders.N.GetValueOrDefault(key) + 1;
            Defenders.Hits[key] = Defenders.Hits.GetValueOrDefault(key) + o.Ahead;
            Defenders.Sum[key] = Defenders.Sum.GetValueOrDefault(key) + o.Behind;
            Defenders.Sum2[key] = Defenders.Sum2.GetValueOrDefault(key) + o.Level;
        }
    }

    /// <summary>
    /// Every kept controlled zone entry of the log with its possession origin, in log order. Pure. Defenders are counted
    /// around the M-8 puck (D-084): the passer's node for a pass entry (the pass moves nobody, so the entry's placement is
    /// that moment), the carrier's node before the carry for a carry entry (from <paramref name="carryPuckNodes"/>; the
    /// carry moves only the carrier). Without carry nodes a carry uses the entry's puck node (after the carry).
    /// </summary>
    /// <param name="events">The match log.</param>
    /// <param name="teamOf">Team of a player id.</param>
    /// <param name="rink">The rink (node ids, zones).</param>
    /// <param name="carryPuckNodes">Puck node of every <c>zoneEntryCarry</c> check in log order, or null.</param>
    public static IReadOnlyList<EntryOrigin> Classify(
        IReadOnlyList<SimEvent> events, Func<int, TeamSide> teamOf, Rink rink, IReadOnlyList<int>? carryPuckNodes = null)
    {
        int carries = 0;
        PassEvent? lastPass = null;
        var origins = new List<EntryOrigin>();
        TeamSide? holder = null;
        string zone = "neutral";
        string gain = "other";
        int period = 0;
        void Start(TeamSide team, int nodeId, string how)
        {
            holder = team;
            zone = ZoneName(rink, nodeId, team);
            gain = how;
        }

        foreach (SimEvent e in events)
        {
            if (e.Context.Period != period)
            {
                period = e.Context.Period;
                holder = null;
            }

            switch (e)
            {
                case StoppageEvent:
                    holder = null;
                    break;
                case FaceoffEvent faceoff:
                    Start(faceoff.Winner, e.Context.Placement.PuckNodeId, "faceoff");
                    break;
                case TurnoverEvent turnover:
                    Start(teamOf(turnover.TakerId), turnover.NodeId, "turnover");
                    break;
                case PuckBattleEvent battle:
                    Battle(teamOf(battle.AttackerIds[0]), battle.Outcome, e);
                    break;
                case DumpInEvent dump:
                    Battle(teamOf(dump.ShooterId), dump.BattleOutcome, e);
                    break;
                case ControlledZoneEntryEvent entry:
                    TeamSide team = teamOf(entry.CarrierId);
                    if (holder != team)
                    {
                        Start(team, e.Context.Placement.PuckNodeId, "other");
                    }

                    int puckNode = e.Context.Placement.PuckNodeId;
                    if (entry.Method == ZoneEntryMethod.Carry)
                    {
                        if (carryPuckNodes != null && carries < carryPuckNodes.Count)
                        {
                            puckNode = carryPuckNodes[carries];
                        }

                        carries++;
                    }
                    else if (lastPass != null)
                    {
                        puckNode = NodeOf(entry.Context.Placement, lastPass.PasserId) ?? puckNode;
                    }

                    if (entry.Outcome == ZoneEntryOutcome.Kept)
                    {
                        origins.Add(Origin(entry, team, zone, gain, rink, puckNode));
                    }

                    break;
                default:
                    lastPass = e as PassEvent ?? lastPass;
                    int? actor = AI.PublicLog.Actor(e);
                    if (actor.HasValue && holder != teamOf(actor.Value))
                    {
                        Start(teamOf(actor.Value), e.Context.Placement.PuckNodeId, "other");
                    }

                    break;
            }
        }

        return origins;

        void Battle(TeamSide attacker, BattleOutcome outcome, SimEvent e)
        {
            if (outcome == BattleOutcome.NoWinner)
            {
                return;
            }

            TeamSide winner = outcome == BattleOutcome.Win ? attacker : TeamSides.Opponent(attacker);
            if (holder != winner)
            {
                Start(winner, e.Context.Placement.PuckNodeId, "loose");
            }
        }
    }

    private static int? NodeOf(PlacementSnapshot placement, int playerId)
    {
        foreach (PlayerPlacement p in placement.Players)
        {
            if (p.PlayerId == playerId)
            {
                return p.NodeId;
            }
        }

        return null;
    }

    private static EntryOrigin Origin(ControlledZoneEntryEvent entry, TeamSide team, string zone, string gain, Rink rink, int puckNode)
    {
        EntryNumbers n = entry.Numbers;
        var group = n.Attackers < n.Defenders ? Report.EntryStrengthGroup.Disadvantage
            : n.Attackers == n.Defenders ? Report.EntryStrengthGroup.Even
            : Report.EntryStrengthGroup.Advantage;
        int puckX = ViewX(rink, puckNode, team);
        int behind = 0;
        int level = 0;
        int ahead = 0;
        foreach (PlayerPlacement p in entry.Context.Placement.Players)
        {
            if (p.IsGoalie || p.Team == team)
            {
                continue;
            }

            int x = ViewX(rink, p.NodeId, team);
            if (x < puckX)
            {
                behind++;
            }
            else if (x == puckX)
            {
                level++;
            }
            else
            {
                ahead++;
            }
        }

        string mode = string.IsNullOrEmpty(entry.Context.PlayId) ? "rush" : "play";
        return new EntryOrigin(zone, gain, mode, group, behind, level, ahead);
    }

    private static int ViewX(Rink rink, int nodeId, TeamSide team)
    {
        RinkNode node = rink.GetNode(nodeId);
        return TeamFrame.ToTeamView(new GridPoint(node.X, node.Y), team, rink).X;
    }

    private static string ZoneName(Rink rink, int nodeId, TeamSide team)
    {
        switch (rink.ZoneAtX(ViewX(rink, nodeId, team)))
        {
            case RinkZone.Defensive: return "own";
            case RinkZone.Neutral: return "neutral";
            default: return "offensive";
        }
    }

    /// <summary>Writes the section (all entering teams of the matchup together).</summary>
    public void Write(StringBuilder md)
    {
        md.AppendLine("### Entry strength by possession origin (D-085)");
        md.AppendLine();
        md.AppendLine("Successful controlled zone entries (carry + pass). N vs M from the logged numbers (M-8); mean defenders behind / level with / ahead of the puck (x vs puck x, entering team's view) at the M-8 moment (carrier's node before the carry, passer's node), so Ahead = mean M. "
            + "Possession start zone in the entering team's view; gain: turnover = takeaway or interception, loose = puck battle or dump-in won, other = puck changed teams without such an event.");
        md.AppendLine();
        md.AppendLine("| Start zone | Gain | Mode | Entries | N < M | N = M | N > M | Behind | Level | Ahead |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        Row(md, "all", "all", "all", k => true);
        foreach (string mode in Modes)
        {
            Row(md, "all", "all", mode, k => k.Mode == mode);
        }

        foreach (string zone in Zones)
        {
            Row(md, zone, "all", "all", k => k.Zone == zone);
            foreach (string gain in Gains)
            {
                Row(md, zone, gain, "all", k => k.Zone == zone && k.Gain == gain);
                foreach (string mode in Modes)
                {
                    Row(md, zone, gain, mode, k => k.Zone == zone && k.Gain == gain && k.Mode == mode);
                }
            }
        }

        md.AppendLine();
        int carries = Carries.N.GetValueOrDefault("kept carry");
        int nonHolder = Carries.Hits.GetValueOrDefault("kept carry");
        md.AppendLine("Kept carries whose logged carrier is not the puck holder (Q-075): " + nonHolder + " of " + carries + " (" + BatchReport.Pct(carries > 0 ? nonHolder / (double)carries : 0.0) + ").");
        md.AppendLine();
    }

    /// <summary>Totals over the keys matching <paramref name="match"/>: entries, N &lt; M, N = M, N &gt; M, behind, level, ahead.</summary>
    public (int Entries, double Below, double Even, int Above, double Behind, double Level, int Ahead) Totals(Func<(string Zone, string Gain, string Mode), bool> match)
    {
        int entries = 0;
        double below = 0;
        double even = 0;
        int above = 0;
        double behind = 0;
        double level = 0;
        int ahead = 0;
        foreach (string key in Strength.N.Keys)
        {
            string[] parts = key.Split(' ');
            if (!match((parts[0], parts[1], parts[2])))
            {
                continue;
            }

            entries += Strength.N[key];
            above += Strength.Hits[key];
            below += Strength.Sum[key];
            even += Strength.Sum2[key];
            ahead += Defenders.Hits[key];
            behind += Defenders.Sum[key];
            level += Defenders.Sum2[key];
        }

        return (entries, below, even, above, behind, level, ahead);
    }

    private void Row(StringBuilder md, string zone, string gain, string mode, Func<(string Zone, string Gain, string Mode), bool> match)
    {
        var t = Totals(match);
        if (t.Entries == 0)
        {
            return;
        }

        double n = t.Entries;
        md.AppendLine("| " + zone + " | " + gain + " | " + mode + " | " + t.Entries + " | " + BatchReport.Pct(t.Below / n) + " | " + BatchReport.Pct(t.Even / n) + " | "
            + BatchReport.Pct(t.Above / n) + " | " + F2(t.Behind / n) + " | " + F2(t.Level / n) + " | " + F2(t.Ahead / n) + " |");
    }

    private static string F2(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);
}
