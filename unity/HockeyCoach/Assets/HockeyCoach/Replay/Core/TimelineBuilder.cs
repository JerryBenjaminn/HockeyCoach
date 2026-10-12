using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Turns the event log into a <see cref="ReplayTimeline"/>. Segment i runs on the match clock from event i − 1
    /// (segment 0 from 0) to event i and moves every on-ice player from his previous node to his node in event i's
    /// placement. The puck path ends on <c>placement.puckNodeId</c> and starts from:
    /// <list type="bullet">
    /// <item>pass: the passer's node, shot: the shooter's node (both from the previous state);</item>
    /// <item>faceoff: the faceoff spot under the centres;</item>
    /// <item>otherwise: where the puck was after the previous event.</item>
    /// </list>
    /// A shot stopped by a goalie or blocker goes through the stopper's node; a goal goes through the goal mouth of the
    /// conceding team's net. On-ice players follow the placements, except that a line change applies exactly its own
    /// <c>outgoingIds</c> / <c>incomingIds</c> to the previous on-ice set (several changes at one stoppage share the
    /// final placement); a player who is on ice but not in the placement keeps his previous node.
    /// </summary>
    public static class TimelineBuilder
    {
        /// <summary>Builds the timeline.</summary>
        /// <exception cref="ReplayFormatException">The events are inconsistent (clock goes backwards, unknown actor).</exception>
        public static ReplayTimeline Build(ReplayMatch match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            var mapping = new RinkMapping(match.Rink);
            var segments = new List<TimelineSegment>(match.Events.Count);
            var goals = new List<TimelineGoal>();
            var teamOf = new Dictionary<int, ReplayTeam>();
            foreach (RosterEntry entry in match.Roster)
            {
                teamOf[entry.Id] = ReplayTeams.Parse(entry.Team);
            }

            var previous = new SortedDictionary<int, SegmentPlayer>();
            double previousClock = 0;
            int previousPuckNode = -1;
            int home = 0;
            int away = 0;
            for (int i = 0; i < match.Events.Count; i++)
            {
                ReplayEvent e = match.Events[i];
                double clock = ((double)(e.Period - 1) * match.PeriodSeconds) + e.Time;
                if (clock < previousClock)
                {
                    throw new ReplayFormatException("Event " + i + " (" + e.Type + ") at period " + e.Period + " " + e.Time + " s is earlier than the previous event.");
                }

                var placed = new SortedDictionary<int, PlacedPlayerData>();
                foreach (PlacedPlayerData p in e.Placement.Players)
                {
                    placed[p.Id] = p;
                    teamOf[p.Id] = ReplayTeams.Parse(p.Team);
                }

                SortedSet<int> onIce = OnIce(i, e, previous, placed);
                var players = new List<SegmentPlayer>(onIce.Count);
                var current = new SortedDictionary<int, SegmentPlayer>();
                foreach (int id in onIce)
                {
                    SegmentPlayer before;
                    bool wasOn = previous.TryGetValue(id, out before);
                    PlacedPlayerData p;
                    bool inPlacement = placed.TryGetValue(id, out p);
                    int node = inPlacement ? p.NodeId : before.NodeId;
                    bool goalie = inPlacement ? p.Goalie : before.IsGoalie;
                    RinkPoint to = mapping.NodePosition(node);
                    var player = new SegmentPlayer(id, teamOf[id], goalie, wasOn ? before.NodeId : node, wasOn ? before.To : to, node, to);
                    players.Add(player);
                    current[id] = player;
                }

                var leaving = new List<int>();
                foreach (int id in previous.Keys)
                {
                    if (!current.ContainsKey(id))
                    {
                        leaving.Add(id);
                    }
                }

                var entering = new List<int>();
                foreach (int id in current.Keys)
                {
                    if (!previous.ContainsKey(id))
                    {
                        entering.Add(id);
                    }
                }

                RinkSpot spot = null;
                int startNode = PuckStart(e, i == 0 ? current : previous, previousPuckNode, e.Placement.PuckNodeId, mapping, current, out spot);
                int? viaNode = null;
                ReplayTeam? scorer = null;
                int homeBefore = home;
                int awayBefore = away;
                if (e.Kind == ReplayEventType.Shot)
                {
                    int shooter = e.ShooterId.Value;
                    if (!teamOf.ContainsKey(shooter))
                    {
                        throw new ReplayFormatException("Event " + i + ": shooter " + shooter + " is not in the roster or the placement.");
                    }

                    if (e.Outcome == "Goal")
                    {
                        ReplayTeam team = teamOf[shooter];
                        scorer = team;
                        viaNode = mapping.NetNodeOf(ReplayTeams.Opponent(team));
                        if (team == ReplayTeam.Home)
                        {
                            home++;
                        }
                        else
                        {
                            away++;
                        }

                        goals.Add(new TimelineGoal(i, e.Period, e.Time, clock, team, shooter));
                    }
                    else if (e.StoppedById.HasValue)
                    {
                        viaNode = NodeOf(e.StoppedById.Value, current, previous);
                    }
                }

                RinkPoint? via = null;
                if (viaNode.HasValue)
                {
                    via = scorer.HasValue ? mapping.NetOf(ReplayTeams.Opponent(scorer.Value)) : mapping.NodePosition(viaNode.Value);
                }

                var puck = new PuckPath(
                    startNode,
                    mapping.NodePosition(startNode),
                    viaNode,
                    via,
                    e.Placement.PuckNodeId,
                    mapping.NodePosition(e.Placement.PuckNodeId),
                    e.Placement.PuckCarrierId);
                segments.Add(new TimelineSegment(
                    i, e, previousClock, clock, players.ToArray(), leaving.ToArray(), entering.ToArray(), puck, homeBefore, awayBefore, home, away, scorer, spot));
                previous = current;
                previousClock = clock;
                previousPuckNode = e.Placement.PuckNodeId;
            }

            return new ReplayTimeline(match, mapping, segments.ToArray(), goals.ToArray());
        }

        private static SortedSet<int> OnIce(int index, ReplayEvent e, SortedDictionary<int, SegmentPlayer> previous, SortedDictionary<int, PlacedPlayerData> placed)
        {
            var fromPlacement = new SortedSet<int>(placed.Keys);
            if (index == 0 || e.Kind != ReplayEventType.LineChange)
            {
                return fromPlacement;
            }

            var set = new SortedSet<int>(previous.Keys);
            foreach (int id in e.OutgoingIds)
            {
                if (!set.Remove(id))
                {
                    return fromPlacement;
                }
            }

            foreach (int id in e.IncomingIds)
            {
                if (!set.Add(id) || !placed.ContainsKey(id))
                {
                    return fromPlacement;
                }
            }

            return set;
        }

        private static int PuckStart(
            ReplayEvent e,
            SortedDictionary<int, SegmentPlayer> before,
            int previousPuckNode,
            int endNode,
            RinkMapping mapping,
            SortedDictionary<int, SegmentPlayer> after,
            out RinkSpot spot)
        {
            spot = null;
            int fallback = previousPuckNode >= 0 ? previousPuckNode : endNode;
            switch (e.Kind)
            {
                case ReplayEventType.Pass:
                    return NodeOrFallback(e.PasserId.Value, before, fallback);
                case ReplayEventType.Shot:
                    return NodeOrFallback(e.ShooterId.Value, before, fallback);
                case ReplayEventType.Faceoff:
                    spot = FaceoffSpot(e, after, mapping);
                    return spot != null ? spot.NodeId : fallback;
                default:
                    return fallback;
            }
        }

        private static RinkSpot FaceoffSpot(ReplayEvent e, SortedDictionary<int, SegmentPlayer> after, RinkMapping mapping)
        {
            foreach (int? centre in new[] { e.HomeCentreId, e.AwayCentreId })
            {
                SegmentPlayer p;
                if (centre.HasValue && after.TryGetValue(centre.Value, out p))
                {
                    RinkSpot spot = mapping.FaceoffSpotAt(p.NodeId);
                    if (spot != null)
                    {
                        return spot;
                    }
                }
            }

            return null;
        }

        private static int NodeOrFallback(int id, SortedDictionary<int, SegmentPlayer> players, int fallback)
        {
            SegmentPlayer p;
            return players.TryGetValue(id, out p) ? p.NodeId : fallback;
        }

        private static int NodeOf(int id, SortedDictionary<int, SegmentPlayer> current, SortedDictionary<int, SegmentPlayer> previous)
        {
            SegmentPlayer p;
            if (current.TryGetValue(id, out p) || previous.TryGetValue(id, out p))
            {
                return p.NodeId;
            }

            throw new ReplayFormatException("Player " + id + " who stopped a shot is not on the ice.");
        }
    }
}
