using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// The presentation time warp of a <see cref="ReplayTimeline"/> (D-088 A, data-schema.md, replay.json, Esitysaika):
    /// one <see cref="SegmentTiming"/> per segment on a presentation time axis (seconds on screen at 1×) and a monotone
    /// mapping between presentation time and the true match clock. Built once from the timeline, the header's time
    /// costs and the <see cref="PresentationConfig"/>; pure data afterwards, so sampling never depends on earlier frames.
    /// </summary>
    public sealed class PresentationTimeline
    {
        private readonly SegmentTiming[] _segments;
        private readonly double[] _ends;
        private ReplayMotion _motion;

        private PresentationTimeline(ReplayTimeline timeline, PresentationConfig config, SegmentTiming[] segments)
        {
            Timeline = timeline;
            Config = config;
            _segments = segments;
            _ends = new double[segments.Length];
            for (int i = 0; i < segments.Length; i++)
            {
                _ends[i] = segments[i].End;
            }
        }

        /// <summary>The timeline on the match clock.</summary>
        public ReplayTimeline Timeline { get; }

        /// <summary>The presentation config.</summary>
        public PresentationConfig Config { get; }

        /// <summary>The motion layer (skaters, puck, goalies), built on first use.</summary>
        public ReplayMotion Motion => _motion ?? (_motion = ReplayMotion.Build(this));

        /// <summary>Timing of every segment, in segment order.</summary>
        public IReadOnlyList<SegmentTiming> Segments => _segments;

        /// <summary>Presentation duration of the whole match at 1×.</summary>
        public double Duration => _segments[_segments.Length - 1].End;

        /// <summary>Wall-clock seconds to play the whole match at <paramref name="speed"/>×.</summary>
        public double WallSecondsAt(double speed)
        {
            if (!(speed > 0) || double.IsInfinity(speed))
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "Speed must be positive and finite.");
            }

            return Duration / speed;
        }

        /// <summary>
        /// The segment shown at presentation time <paramref name="time"/> (clamped): the first segment that ends at or
        /// after it, moved on through the empty segments that start exactly there.
        /// </summary>
        public int SegmentIndexAt(double time)
        {
            int index = Array.BinarySearch(_ends, time);
            if (index < 0)
            {
                index = ~index;
            }
            else
            {
                while (index > 0 && _ends[index - 1] >= time)
                {
                    index--;
                }
            }

            return Normalize(Math.Min(index, _segments.Length - 1), time);
        }

        /// <summary>
        /// Moves <paramref name="index"/> forward while <paramref name="time"/> is past its segment, or at its end and the
        /// next segment is empty (several events at one instant show the state after all of them).
        /// </summary>
        public int Normalize(int index, double time)
        {
            int last = _segments.Length - 1;
            while (index < last && (time > _segments[index].End || (time >= _segments[index].End && !(_segments[index + 1].Duration > 0))))
            {
                index++;
            }

            return index;
        }

        /// <summary>Match clock at presentation time <paramref name="time"/> in segment <paramref name="index"/>.</summary>
        public double ClockAt(int index, double time)
        {
            return _segments[index].ClockAt(time);
        }

        /// <summary>Match clock at presentation time <paramref name="time"/>.</summary>
        public double ClockAt(double time)
        {
            return ClockAt(SegmentIndexAt(time), time);
        }

        /// <summary>
        /// The earliest presentation time that shows the state after every event up to match clock
        /// <paramref name="clock"/>, and its segment: the end of the action window of the last event at or before the
        /// clock, or the point inside the next gap where the clock reaches it. Clamped to the match.
        /// </summary>
        public double TimeAtClock(double clock, out int index)
        {
            ReplayTimeline t = Timeline;
            int last = _segments.Length - 1;
            if (clock >= t.Segments[last].EndClock)
            {
                index = last;
                return Duration;
            }

            int j = -1;
            int lo = 0;
            int hi = last;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (t.Segments[mid].EndClock <= clock)
                {
                    j = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (j >= 0 && t.Segments[j].EndClock == clock)
            {
                index = j;
                return _segments[j].EventEnd;
            }

            index = j + 1;
            return _segments[index].TimeAtClock(clock);
        }

        /// <summary>Builds the warp.</summary>
        /// <exception cref="ReplayFormatException">The match header has no time costs.</exception>
        /// <exception cref="PresentationConfigException">The config does not fit the rink (bench gates).</exception>
        public static PresentationTimeline Build(ReplayTimeline timeline, PresentationConfig config)
        {
            if (timeline == null)
            {
                throw new ArgumentNullException(nameof(timeline));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            TimeCostsData costs = timeline.Match.Time;
            if (costs == null)
            {
                throw new ReplayFormatException("Match JSON header has no time costs (formatVersion 2, D-088). Re-export the match with the current Harness.");
            }

            PresentationConfigReader.CheckRink(config, timeline.Match.Rink.Metres);
            IReadOnlyList<TimelineSegment> segs = timeline.Segments;
            var result = new SegmentTiming[segs.Count];
            var phases = new List<PresentationPhase>();
            double now = 0;
            bool seriesOpen = false;
            double seriesClock = 0;
            int seriesStoppage = -1;
            bool openingRun = false;
            for (int i = 0; i < segs.Count; i++)
            {
                TimelineSegment s = segs[i];
                ReplayEvent e = s.Event;
                TimelineSegment prev = i > 0 ? segs[i - 1] : null;
                bool firstOfPeriod = prev == null || prev.Period != s.Period;
                if (firstOfPeriod)
                {
                    seriesOpen = false;
                    openingRun = true;
                }

                openingRun = openingRun && e.Kind == ReplayEventType.LineChange && e.Time == 0;
                bool member = seriesOpen && (e.Kind == ReplayEventType.LineChange || e.Kind == ReplayEventType.Timeout) && s.EndClock == seriesClock;
                bool closing = !member && seriesOpen && e.Kind == ReplayEventType.Faceoff;
                int stoppageIndex = seriesStoppage;
                if (!member)
                {
                    seriesOpen = false;
                }

                double gap = firstOfPeriod ? e.Time : s.EndClock - prev.EndClock;
                double clockStart = s.EndClock - gap;
                GameSplit split = PresentationTiming.SplitGame(gap, PresentationTiming.ActionCost(e, prev != null ? prev.Event : null, costs), costs);
                GapDurations d = PresentationTiming.Split(gap, split.Action, costs, config.Timing);
                bool goal = e.Kind == ReplayEventType.Shot && e.Outcome == "Goal";
                bool instantChange = e.Kind == ReplayEventType.LineChange && (member || openingRun);
                bool flyingChange = e.Kind == ReplayEventType.LineChange && !instantChange;
                SegmentTimingFlags flags = SegmentTimingFlags.None;
                flags |= firstOfPeriod ? SegmentTimingFlags.PeriodStart : 0;
                flags |= member ? SegmentTimingFlags.StoppageSeries : 0;
                flags |= closing ? SegmentTimingFlags.FaceoffApproach : 0;
                flags |= instantChange ? SegmentTimingFlags.InstantLineChange : 0;
                flags |= flyingChange ? SegmentTimingFlags.FlyingLineChange : 0;
                flags |= goal ? SegmentTimingFlags.Goal : 0;
                if (e.Kind == ReplayEventType.Stoppage || member || e.Kind == ReplayEventType.Faceoff || firstOfPeriod || instantChange)
                {
                    flags |= SegmentTimingFlags.NoAnticipation;
                }

                double puckFloor = 0;
                double skaterFloor = 0;
                if (instantChange)
                {
                    d = new GapDurations(0, 0, 0);
                }
                else if (e.Kind != ReplayEventType.Timeout)
                {
                    puckFloor = PuckFloor.Seconds(s, prev, timeline, config);
                    d = d.WithWindowAtLeast(puckFloor);
                    if (!firstOfPeriod && !member && !closing)
                    {
                        skaterFloor = ActorDisplacement(s, prev) / config.Skaters.TopSpeed;
                        d = d.WithTotalAtLeast(skaterFloor);
                    }

                    d = d.WithTotalAtLeast(flyingChange ? config.Timing.LineChangeSeconds : config.Timing.MinEventSeconds);
                    if (goal)
                    {
                        d = d.WithWindow(PresentationTiming.GoalWindow(d.Window, config.Goal));
                    }
                }

                double start = now;
                phases.Clear();
                if (closing)
                {
                    bool changes;
                    double outgoing = ChangeDisplacement(segs[stoppageIndex], s, timeline.Mapping, config.Bench, out changes);
                    if (changes)
                    {
                        now = Add(phases, PresentationPhaseKind.StoppageChange, i, now, PresentationTiming.ChangeSeconds(outgoing, config.Timing), clockStart, clockStart);
                    }
                }

                double approachStart = now;
                if (closing)
                {
                    double approach = PresentationTiming.ApproachSeconds(ApproachDisplacement(segs[stoppageIndex], s, timeline.Mapping, config.Bench), config.Timing);
                    now = Add(phases, PresentationPhaseKind.FaceoffApproach, i, now, approach, clockStart, clockStart);
                }

                double gapStart = now;
                double clockAfterDead = clockStart + split.Dead;
                double clockWindow = Math.Max(clockAfterDead, s.EndClock - split.Action);
                now = Add(phases, PresentationPhaseKind.DeadTime, i, now, d.Dead, clockStart, clockAfterDead);
                now = Add(phases, PresentationPhaseKind.OtherAction, i, now, d.Other, clockAfterDead, clockWindow);
                double windowStart = now;
                now = Add(phases, PresentationPhaseKind.ActionWindow, i, now, d.Window, clockWindow, s.EndClock);
                double eventEnd = now;
                if (goal)
                {
                    now = Add(phases, PresentationPhaseKind.GoalHold, i, now, config.Goal.HoldSeconds, s.EndClock, s.EndClock);
                }

                if (e.Kind == ReplayEventType.Stoppage)
                {
                    now = Add(phases, PresentationPhaseKind.StoppageHold, i, now, config.Timing.StoppageHoldSeconds, s.EndClock, s.EndClock);
                    if (e.Reason == "PeriodEnd")
                    {
                        now = Add(phases, PresentationPhaseKind.PeriodBreak, i, now, config.Timing.PeriodBreakSeconds, s.EndClock, s.EndClock);
                    }

                    seriesOpen = true;
                    seriesClock = s.EndClock;
                    seriesStoppage = i;
                }

                if (e.Kind == ReplayEventType.Timeout)
                {
                    now = Add(phases, PresentationPhaseKind.TimeoutHold, i, now, config.Timing.TimeoutHoldSeconds, s.EndClock, s.EndClock);
                }

                result[i] = new SegmentTiming(i, start, approachStart, gapStart, windowStart, eventEnd, now, clockStart, s.EndClock, split, d, puckFloor, skaterFloor, flags, phases);
            }

            return new PresentationTimeline(timeline, config, result);
        }

        private static double Add(List<PresentationPhase> phases, PresentationPhaseKind kind, int index, double now, double duration, double clockStart, double clockEnd)
        {
            if (!(duration > 0))
            {
                return now;
            }

            phases.Add(new PresentationPhase(kind, index, now, now + duration, clockStart, clockEnd));
            return now + duration;
        }

        /// <summary>
        /// Largest displacement of the event's actors between the previous and this event's nodes (no offsets): the
        /// players named in the event's fields and the puck carrier before and after it.
        /// </summary>
        internal static double ActorDisplacement(TimelineSegment s, TimelineSegment prev)
        {
            if (prev == null)
            {
                return 0;
            }

            var before = new Dictionary<int, RinkPoint>();
            foreach (SegmentPlayer p in prev.Players)
            {
                before[p.Id] = p.To;
            }

            var after = new Dictionary<int, RinkPoint>();
            foreach (SegmentPlayer p in s.Players)
            {
                after[p.Id] = p.To;
            }

            double max = 0;
            foreach (int id in Actors(s.Event, prev.Event))
            {
                RinkPoint from;
                RinkPoint to;
                if (before.TryGetValue(id, out from) && after.TryGetValue(id, out to))
                {
                    max = Math.Max(max, RinkPoint.Distance(from, to));
                }
            }

            return max;
        }

        /// <summary>
        /// The actor ids of an event (data-schema.md, replay.json, Luistelijan alaraja): <c>passerId</c>,
        /// <c>receiverId</c>, <c>shooterId</c>, <c>loserId</c>, <c>takerId</c>, <c>carrierId</c>, <c>homeCentreId</c>,
        /// <c>awayCentreId</c>, <c>attackerIds</c>, <c>defenderIds</c> and <c>placement.puckCarrierId</c> before and after.
        /// </summary>
        public static IEnumerable<int> Actors(ReplayEvent e, ReplayEvent previous)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            var ids = new SortedSet<int>();
            AddId(ids, e.PasserId);
            AddId(ids, e.ReceiverId);
            AddId(ids, e.ShooterId);
            AddId(ids, e.LoserId);
            AddId(ids, e.TakerId);
            AddId(ids, e.CarrierId);
            AddId(ids, e.HomeCentreId);
            AddId(ids, e.AwayCentreId);
            if (e.AttackerIds != null)
            {
                foreach (int id in e.AttackerIds)
                {
                    ids.Add(id);
                }
            }

            if (e.DefenderIds != null)
            {
                foreach (int id in e.DefenderIds)
                {
                    ids.Add(id);
                }
            }

            if (previous != null && previous.Placement != null)
            {
                AddId(ids, previous.Placement.PuckCarrierId);
            }

            if (e.Placement != null)
            {
                AddId(ids, e.Placement.PuckCarrierId);
            }

            return ids;
        }

        private static void AddId(SortedSet<int> ids, int? id)
        {
            if (id.HasValue)
            {
                ids.Add(id.Value);
            }
        }

        /// <summary>
        /// Largest skater displacement of the faceoff approach (D-089 A1: after the changes): players on the ice at the
        /// stoppage and at the faceoff skate from their stoppage nodes to their faceoff nodes, incoming players from their
        /// bench's entry point. Outgoing players have left in the change phase. Goalies do not count.
        /// </summary>
        internal static double ApproachDisplacement(TimelineSegment stoppage, TimelineSegment faceoff, RinkMapping mapping, BenchSettings bench)
        {
            SortedDictionary<int, SegmentPlayer> before = FieldPlayers(stoppage);
            double max = 0;
            foreach (SegmentPlayer p in FieldPlayers(faceoff).Values)
            {
                SegmentPlayer was;
                RinkPoint from = before.TryGetValue(p.Id, out was) ? was.To : BenchEntry(mapping, bench, p.Team);
                max = Math.Max(max, RinkPoint.Distance(from, p.To));
            }

            return max;
        }

        /// <summary>
        /// The change phase of a stoppage series (D-089 A1): the largest distance from an outgoing player's stoppage node to
        /// his bench's exit point, and whether the series changes anyone at all.
        /// </summary>
        internal static double ChangeDisplacement(TimelineSegment stoppage, TimelineSegment faceoff, RinkMapping mapping, BenchSettings bench, out bool changes)
        {
            SortedDictionary<int, SegmentPlayer> before = FieldPlayers(stoppage);
            SortedDictionary<int, SegmentPlayer> after = FieldPlayers(faceoff);
            changes = false;
            double max = 0;
            foreach (SegmentPlayer p in before.Values)
            {
                if (!after.ContainsKey(p.Id))
                {
                    changes = true;
                    max = Math.Max(max, RinkPoint.Distance(p.To, BenchExit(mapping, bench, p.Team)));
                }
            }

            foreach (int id in after.Keys)
            {
                changes |= !before.ContainsKey(id);
            }

            return max;
        }

        private static SortedDictionary<int, SegmentPlayer> FieldPlayers(TimelineSegment s)
        {
            var players = new SortedDictionary<int, SegmentPlayer>();
            foreach (SegmentPlayer p in s.Players)
            {
                if (!p.IsGoalie)
                {
                    players[p.Id] = p;
                }
            }

            return players;
        }

        /// <summary>
        /// The exit point of <paramref name="team"/>'s bench (data-schema.md, replay.json, Penkit): x = centre line +
        /// <c>bench.homeFromCentreMetres</c> or <c>awayFromCentreMetres</c> - <c>doorSpacingMetres</c> / 2,
        /// y = <c>bench.frontYMetres</c>. Outgoing players skate here and leave the ice.
        /// </summary>
        public static RinkPoint BenchExit(RinkMapping mapping, BenchSettings bench, ReplayTeam team)
        {
            return BenchDoor(mapping, bench, team, -0.5);
        }

        /// <summary>The entry point of <paramref name="team"/>'s bench: as <see cref="BenchExit"/> but + <c>doorSpacingMetres</c> / 2.</summary>
        public static RinkPoint BenchEntry(RinkMapping mapping, BenchSettings bench, ReplayTeam team)
        {
            return BenchDoor(mapping, bench, team, 0.5);
        }

        private static RinkPoint BenchDoor(RinkMapping mapping, BenchSettings bench, ReplayTeam team, double side)
        {
            if (mapping == null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            if (bench == null)
            {
                throw new ArgumentNullException(nameof(bench));
            }

            double fromCentre = team == ReplayTeam.Home ? bench.HomeFromCentreMetres : bench.AwayFromCentreMetres;
            return new RinkPoint(mapping.CentreLineX + fromCentre + (side * bench.DoorSpacingMetres), bench.FrontYMetres);
        }
    }
}
