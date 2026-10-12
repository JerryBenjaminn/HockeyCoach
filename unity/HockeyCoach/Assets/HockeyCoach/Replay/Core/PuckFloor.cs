using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// The puck floor of an action window (data-schema.md, replay.json, Esitysaika 5 and Kiekko): the duration of the
    /// event's puck path, all its legs (or only the first one with <c>puck.floorMode</c> <c>firstLeg</c>, Q-082) over
    /// their speeds (+ <c>faceoffDrawDelaySeconds</c> for a faceoff). The path
    /// ends are the players' presentation positions at the start of the window, which are not known before the timing
    /// is fixed; they are approximated by grid node positions without offsets. The puck starts from the actor's node in
    /// this event's placement (passer, shooter, or the previous carrier for loose-puck events; the actor is near it at
    /// the end of the gap, where the window lies), else from the previous puck node; it goes through the stopper's node,
    /// the net point of a goal or the miss point of a missed shot to <c>placement.puckNodeId</c>. See docs/questions.md.
    /// </summary>
    public static class PuckFloor
    {
        /// <summary>Puck path duration of <paramref name="segment"/> in presentation seconds (0 when the puck does not travel).</summary>
        /// <param name="segment">The segment.</param>
        /// <param name="previous">The previous segment, null for the first.</param>
        /// <param name="timeline">The timeline (mapping and roster).</param>
        /// <param name="config">The presentation config.</param>
        public static double Seconds(TimelineSegment segment, TimelineSegment previous, ReplayTimeline timeline, PresentationConfig config)
        {
            if (segment == null)
            {
                throw new ArgumentNullException(nameof(segment));
            }

            if (timeline == null)
            {
                throw new ArgumentNullException(nameof(timeline));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            PuckSettings p = config.Puck;
            ReplayEvent e = segment.Event;
            PuckPath path = segment.Puck;
            switch (e.Kind)
            {
                case ReplayEventType.Pass:
                {
                    double d = RinkPoint.Distance(StartOf(segment, e.PasserId, path.Start), path.End);
                    return d / (d >= p.LongPassMinMetres ? p.LongPassSpeed : p.PassSpeed);
                }

                case ReplayEventType.Shot:
                    return ShotSeconds(segment, previous, timeline, config);
                case ReplayEventType.DumpIn:
                {
                    return RinkPoint.Distance(StartOf(segment, e.ShooterId, path.Start), path.End) / p.DumpInSpeed;
                }

                case ReplayEventType.Faceoff:
                {
                    RinkPoint spot = segment.FaceoffSpot != null ? segment.FaceoffSpot.Position : path.Start;
                    return p.FaceoffDrawDelaySeconds + (RinkPoint.Distance(spot, path.End) / p.FaceoffDrawSpeed);
                }

                case ReplayEventType.Turnover:
                case ReplayEventType.PuckBattle:
                    return RinkPoint.Distance(LooseStart(segment, previous), path.End) / p.LoosePuckSpeed;
                case ReplayEventType.ControlledZoneEntry:
                    if (e.Method == "Carry")
                    {
                        return 0;
                    }

                    return CarrierChanged(segment, previous) ? RinkPoint.Distance(LooseStart(segment, previous), path.End) / p.LoosePuckSpeed : 0;
                default:
                    return 0;
            }
        }

        private static double ShotSeconds(TimelineSegment segment, TimelineSegment previous, ReplayTimeline timeline, PresentationConfig config)
        {
            PuckSettings p = config.Puck;
            RinkMapping map = timeline.Mapping;
            ReplayEvent e = segment.Event;
            PuckPath path = segment.Puck;
            RinkPoint start = StartOf(segment, e.ShooterId, path.Start);
            if (e.Outcome == "Goal")
            {
                ReplayTeam scorer = segment.ScoringTeam ?? TeamOf(timeline, e.ShooterId.Value);
                RinkPoint net = PuckGeometry.NetInside(map, ReplayTeams.Opponent(scorer), config.Goal.NetInsideMetres);
                return RinkPoint.Distance(start, net) / p.ShotSpeed;
            }

            if (e.Outcome == "Missed")
            {
                ReplayTeam shooter = TeamOf(timeline, e.ShooterId.Value);
                RinkPoint miss = PuckGeometry.MissPoint(map, ReplayTeams.Opponent(shooter), path.EndNodeId, path.StartNodeId, p.MissWideMetres);
                return (RinkPoint.Distance(start, miss) / p.ShotSpeed) + SecondLeg(config, RinkPoint.Distance(miss, path.End) / p.ReboundSpeed);
            }

            if (path.Via.HasValue)
            {
                double second = e.Outcome == "Blocked" ? p.BlockDeflectSpeed : p.ReboundSpeed;
                return (RinkPoint.Distance(start, path.Via.Value) / p.ShotSpeed) + SecondLeg(config, RinkPoint.Distance(path.Via.Value, path.End) / second);
            }

            return RinkPoint.Distance(start, path.End) / p.ShotSpeed;
        }

        /// <summary>The second leg's duration, or 0 when <c>puck.floorMode</c> is <c>firstLeg</c> (Q-082, D-089 A3).</summary>
        private static double SecondLeg(PresentationConfig config, double seconds)
        {
            return config.Puck.FloorMode == PuckFloorMode.FirstLeg ? 0 : seconds;
        }

        private static bool CarrierChanged(TimelineSegment segment, TimelineSegment previous)
        {
            int? before = previous != null ? previous.Event.Placement.PuckCarrierId : null;
            return before != segment.Event.Placement.PuckCarrierId;
        }

        /// <summary>The previous carrier's node in this placement, or the previous puck node for a loose puck.</summary>
        private static RinkPoint LooseStart(TimelineSegment segment, TimelineSegment previous)
        {
            int? carrier = previous != null ? previous.Event.Placement.PuckCarrierId : null;
            return StartOf(segment, carrier, segment.Puck.Start);
        }

        /// <summary>The node position of <paramref name="id"/> in this event's placement, or <paramref name="fallback"/>.</summary>
        private static RinkPoint StartOf(TimelineSegment segment, int? id, RinkPoint fallback)
        {
            if (id.HasValue)
            {
                foreach (SegmentPlayer player in segment.Players)
                {
                    if (player.Id == id.Value)
                    {
                        return player.To;
                    }
                }
            }

            return fallback;
        }

        private static ReplayTeam TeamOf(ReplayTimeline timeline, int id)
        {
            RosterEntry entry = timeline.FindPlayer(id);
            if (entry == null)
            {
                throw new ReplayFormatException("Shooter " + id + " is not in the roster.");
            }

            return ReplayTeams.Parse(entry.Team);
        }
    }
}
