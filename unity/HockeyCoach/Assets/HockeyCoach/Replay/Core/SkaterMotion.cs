using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>A sampled skater: drawn position and the path state it came from.</summary>
    public readonly struct SkaterPose
    {
        /// <summary>Creates the pose.</summary>
        public SkaterPose(int id, ReplayTeam team, RinkPoint position, TrackState path, bool isLeaving, bool isWaiting, bool isChanging)
        {
            Id = id;
            Team = team;
            Position = position;
            Path = path;
            IsLeaving = isLeaving;
            IsWaiting = isWaiting;
            IsChanging = isChanging;
        }

        /// <summary>Player id.</summary>
        public int Id { get; }

        /// <summary>Team.</summary>
        public ReplayTeam Team { get; }

        /// <summary>Drawn position: path + idle motion, separated and clamped inside the rink.</summary>
        public RinkPoint Position { get; }

        /// <summary>The path state (before idle motion and separation).</summary>
        public TrackState Path { get; }

        /// <summary>True for a player skating off to his bench (not one of the twelve on the ice).</summary>
        public bool IsLeaving { get; }

        /// <summary>
        /// True for an incoming player of the segment who has not entered yet (D-089 A1: he enters when his outgoing
        /// partner is near the bench); not drawn.
        /// </summary>
        public bool IsWaiting { get; }

        /// <summary>True while the player is changing (leaving, waiting or still on his way in): separation leaves him out.</summary>
        public bool IsChanging { get; }
    }

    /// <summary>
    /// Skater motion of the presentation layer (D-088, D-089, data-schema.md, replay.json, Luistelijat): one
    /// <see cref="SkaterTrack"/> per skater built once from the <see cref="PresentationTimeline"/>, and a stateless
    /// sample at any presentation time: path position (node + own offset targets, centripetal Catmull-Rom, trapezoid
    /// speed profile, anticipation, late arrivals, line changes at the benches, faceoff approach), precomputed contact
    /// displacements (<see cref="SkaterContacts"/>, D-089 A2), idle motion and a clamp inside the rink. Goalies have no
    /// track (see <see cref="ReplayMotion"/>).
    /// </summary>
    public sealed class SkaterMotion
    {
        /// <summary>Time step of the search for the moment an outgoing partner nears his bench (presentation seconds).</summary>
        private const double PartnerScanSeconds = 1.0 / 120;

        /// <summary>Bisection steps of that search.</summary>
        private const int PartnerBisections = 20;

        private readonly SortedDictionary<int, SkaterTrack> _tracks;
        private readonly SortedDictionary<int, RinkPoint> _offsets;
        private readonly SortedDictionary<int, double> _phases;
        private readonly SortedDictionary<int, double> _angles;
        private readonly int[][] _extras;

        private SkaterMotion(PresentationTimeline presentation, SortedDictionary<int, SkaterTrack> tracks, SortedDictionary<int, RinkPoint> offsets, SortedDictionary<int, double> phases, SortedDictionary<int, double> angles, int[][] extras)
        {
            Presentation = presentation;
            _tracks = tracks;
            _offsets = offsets;
            _phases = phases;
            _angles = angles;
            _extras = extras;
        }

        /// <summary>The presentation timeline.</summary>
        public PresentationTimeline Presentation { get; }

        /// <summary>Tracks by player id (ascending).</summary>
        public IReadOnlyDictionary<int, SkaterTrack> Tracks => _tracks;

        /// <summary>The player's own offset (zero for unknown ids and goalies).</summary>
        public RinkPoint OffsetOf(int id)
        {
            RinkPoint o;
            return _offsets.TryGetValue(id, out o) ? o : new RinkPoint(0, 0);
        }

        /// <summary>A node target: the node position + the player's own offset.</summary>
        public RinkPoint Target(int id, int nodeId)
        {
            RinkPoint n = Presentation.Timeline.Mapping.NodePosition(nodeId);
            RinkPoint o = OffsetOf(id);
            return new RinkPoint(n.X + o.X, n.Y + o.Y);
        }

        /// <summary>Builds the tracks.</summary>
        public static SkaterMotion Build(PresentationTimeline presentation)
        {
            if (presentation == null)
            {
                throw new ArgumentNullException(nameof(presentation));
            }

            ReplayTimeline timeline = presentation.Timeline;
            PresentationConfig config = presentation.Config;
            ulong seed = timeline.Match.Seed;
            var teams = new SortedDictionary<int, ReplayTeam>();
            foreach (TimelineSegment s in timeline.Segments)
            {
                foreach (SegmentPlayer p in s.Players)
                {
                    if (!p.IsGoalie)
                    {
                        teams[p.Id] = p.Team;
                    }
                }
            }

            var offsets = new SortedDictionary<int, RinkPoint>();
            var phases = new SortedDictionary<int, double>();
            var angles = new SortedDictionary<int, double>();
            var builders = new SortedDictionary<int, TrackBuilder>();
            foreach (KeyValuePair<int, ReplayTeam> pair in teams)
            {
                offsets[pair.Key] = SkaterOffsets.Offset(seed, pair.Key, config.Skaters.NodeOffsetRadiusMetres);
                phases[pair.Key] = SkaterOffsets.Phase(seed, pair.Key);
                angles[pair.Key] = SkaterOffsets.Angle(seed, pair.Key);
                builders[pair.Key] = new TrackBuilder(presentation, pair.Key, pair.Value, offsets[pair.Key]);
            }

            Walk(presentation, builders);
            var tracks = new SortedDictionary<int, SkaterTrack>();
            foreach (KeyValuePair<int, TrackBuilder> pair in builders)
            {
                tracks[pair.Key] = pair.Value.Finish();
            }

            var motion = new SkaterMotion(presentation, tracks, offsets, phases, angles, Extras(presentation, tracks));
            motion.Contacts = SkaterContacts.Build(motion);
            return motion;
        }

        /// <summary>The precomputed contact displacements (D-089 A2).</summary>
        public SkaterContacts Contacts { get; private set; }

        /// <summary>
        /// The skaters of segment <paramref name="segmentIndex"/> at presentation time <paramref name="t"/>, in id order:
        /// its on-ice skaters (including incoming players still waiting at the bench) and the players still skating off to
        /// their bench. Drawn position = path + the precomputed displacement (contacts and idle motion,
        /// <see cref="SkaterContacts"/>), clamped inside the rink.
        /// </summary>
        public SkaterPose[] Sample(int segmentIndex, double t)
        {
            var visible = new List<Visible>();
            Collect(segmentIndex, t, visible);
            RinkMapping map = Presentation.Timeline.Mapping;
            var poses = new SkaterPose[visible.Count];
            for (int i = 0; i < visible.Count; i++)
            {
                Visible v = visible[i];
                RinkPoint shift = v.Waiting ? new RinkPoint(0, 0) : Contacts.At(v.Id, t);
                double x = v.State.Position.X + shift.X;
                double y = v.State.Position.Y + shift.Y;
                var position = new RinkPoint(Math.Max(0, Math.Min(map.LengthMetres, x)), Math.Max(0, Math.Min(map.WidthMetres, y)));
                poses[i] = new SkaterPose(v.Id, v.Team, position, v.State, v.Leaving, v.Waiting, v.Changing);
            }

            return poses;
        }

        /// <summary>The player's idle phase φ = 2πu₃.</summary>
        internal double PhaseOf(int id)
        {
            double phi;
            return _phases.TryGetValue(id, out phi) ? phi : 0;
        }

        /// <summary>The player's own angle 2πu₂ (fallback axis of a coincident teammate pair).</summary>
        internal double AngleOf(int id)
        {
            double a;
            return _angles.TryGetValue(id, out a) ? a : 0;
        }

        /// <summary>Collects the skaters of segment <paramref name="segmentIndex"/> at <paramref name="t"/> in id order.</summary>
        internal void Collect(int segmentIndex, double t, List<Visible> into)
        {
            into.Clear();
            TimelineSegment segment = Presentation.Timeline.Segments[segmentIndex];
            var ids = new List<int>();
            var leaving = new HashSet<int>();
            foreach (SegmentPlayer p in segment.Players)
            {
                if (!p.IsGoalie && _tracks.ContainsKey(p.Id))
                {
                    ids.Add(p.Id);
                }
            }

            foreach (int id in _extras[segmentIndex])
            {
                if (_tracks[id].IsActive(t))
                {
                    ids.Add(id);
                    leaving.Add(id);
                }
            }

            ids.Sort();
            foreach (int id in ids)
            {
                SkaterTrack track = _tracks[id];
                bool isLeaving = leaving.Contains(id);
                bool waiting = !isLeaving && !track.IsActive(t);
                bool changing = isLeaving || waiting || track.IsEntering(t);
                into.Add(new Visible(id, track.Team, track.StateAt(t), isLeaving, waiting, changing));
            }
        }

        /// <summary>One skater of a sample before contacts and idle motion.</summary>
        internal readonly struct Visible
        {
            public Visible(int id, ReplayTeam team, TrackState state, bool leaving, bool waiting, bool changing)
            {
                Id = id;
                Team = team;
                State = state;
                Leaving = leaving;
                Waiting = waiting;
                Changing = changing;
            }

            public int Id { get; }

            public ReplayTeam Team { get; }

            public TrackState State { get; }

            public bool Leaving { get; }

            public bool Waiting { get; }

            public bool Changing { get; }
        }

        /// <summary>
        /// Walks the segments once for all skaters (D-089 A1): first period ends and players leaving the ice (to their
        /// bench exit), then players entering (at their bench entry when the outgoing partner of the same line change is
        /// within <c>bench.enterWithinMetres</c> of his exit; in a stoppage series during the change phase before the
        /// approach), then the on-ice targets.
        /// </summary>
        private static void Walk(PresentationTimeline p, SortedDictionary<int, TrackBuilder> builders)
        {
            IReadOnlyList<TimelineSegment> segs = p.Timeline.Segments;
            double periodStart = 0;
            for (int i = 0; i < segs.Count; i++)
            {
                TimelineSegment s = segs[i];
                SegmentTiming tm = p.Segments[i];
                var on = new Dictionary<int, SegmentPlayer>();
                foreach (SegmentPlayer sp in s.Players)
                {
                    on[sp.Id] = sp;
                }

                if ((tm.Flags & SegmentTimingFlags.PeriodStart) != 0)
                {
                    periodStart = tm.Start;
                    foreach (TrackBuilder b in builders.Values)
                    {
                        b.EndPeriod(tm.Start);
                    }
                }

                if (s.Type == ReplayEventType.LineChange && s.Event.OutgoingIds != null && s.Event.IncomingIds != null)
                {
                    var outgoing = new List<int>(s.Event.OutgoingIds);
                    var incoming = new List<int>(s.Event.IncomingIds);
                    outgoing.Sort();
                    incoming.Sort();
                    for (int k = 0; k < incoming.Count; k++)
                    {
                        TrackBuilder b;
                        if (builders.TryGetValue(incoming[k], out b))
                        {
                            b.Partner = k < outgoing.Count ? outgoing[k] : (int?)null;
                        }
                    }
                }

                foreach (TrackBuilder b in builders.Values)
                {
                    if (!on.ContainsKey(b.Id))
                    {
                        b.Leave(i);
                    }
                    else
                    {
                        b.Stay();
                    }
                }

                foreach (TrackBuilder b in builders.Values)
                {
                    SegmentPlayer me;
                    if (!on.TryGetValue(b.Id, out me) || b.IsOnIce)
                    {
                        continue;
                    }

                    if (tm.Start == periodStart)
                    {
                        b.Enter(tm.Start, b.NodeTarget(me.NodeId), false);
                    }
                    else if ((tm.Flags & SegmentTimingFlags.StoppageSeries) == 0)
                    {
                        double until = tm.ChangeSeconds > 0 ? tm.ApproachStart : double.PositiveInfinity;
                        TrackBuilder partner;
                        double appear = b.Partner.HasValue && builders.TryGetValue(b.Partner.Value, out partner)
                            ? partner.NearExit(tm.Start, until)
                            : tm.Start;
                        b.Enter(appear, b.Entry, true);
                    }
                }

                foreach (TrackBuilder b in builders.Values)
                {
                    SegmentPlayer me;
                    if (on.TryGetValue(b.Id, out me) && b.IsOnIce)
                    {
                        b.Target(i, me.NodeId);
                    }
                }
            }
        }

        private static int[][] Extras(PresentationTimeline presentation, SortedDictionary<int, SkaterTrack> tracks)
        {
            ReplayTimeline timeline = presentation.Timeline;
            int n = timeline.Segments.Count;
            var extras = new int[n][];
            for (int i = 0; i < n; i++)
            {
                SegmentTiming tm = presentation.Segments[i];
                var on = new HashSet<int>();
                foreach (SegmentPlayer p in timeline.Segments[i].Players)
                {
                    on.Add(p.Id);
                }

                var list = new List<int>();
                foreach (SkaterTrack track in tracks.Values)
                {
                    if (on.Contains(track.Id))
                    {
                        continue;
                    }

                    foreach (SkaterStint st in track.Stints)
                    {
                        if (st.Appear <= tm.End && st.Vanish > tm.Start)
                        {
                            list.Add(track.Id);
                            break;
                        }
                    }
                }

                extras[i] = list.ToArray();
            }

            return extras;
        }

        /// <summary>Builds one skater's stints while <see cref="Walk"/> steps through the segments.</summary>
        private sealed class TrackBuilder
        {
            private readonly PresentationTimeline _p;
            private readonly RinkPoint _offset;
            private readonly RinkPoint _exit;
            private readonly SkaterTrack _track;
            private SkaterStint _stint;
            private List<RinkPoint> _points;
            private List<double> _targets;
            private bool _leavingAtChange;

            public TrackBuilder(PresentationTimeline p, int id, ReplayTeam team, RinkPoint offset)
            {
                _p = p;
                Id = id;
                _offset = offset;
                _exit = PresentationTimeline.BenchExit(p.Timeline.Mapping, p.Config.Bench, team);
                Entry = PresentationTimeline.BenchEntry(p.Timeline.Mapping, p.Config.Bench, team);
                _track = new SkaterTrack(id, team);
            }

            public int Id { get; }

            /// <summary>The bench entry point of the player's team.</summary>
            public RinkPoint Entry { get; }

            /// <summary>The outgoing player of the same line change this player replaces, if any.</summary>
            public int? Partner { get; set; }

            /// <summary>True while a stint is open.</summary>
            public bool IsOnIce => _stint != null;

            /// <summary>The last stint that has ended (the one an incoming partner looks at).</summary>
            private SkaterStint LastEnded { get; set; }

            public RinkPoint NodeTarget(int nodeId)
            {
                RinkPoint n = _p.Timeline.Mapping.NodePosition(nodeId);
                return new RinkPoint(n.X + _offset.X, n.Y + _offset.Y);
            }

            public void EndPeriod(double time)
            {
                if (_stint != null)
                {
                    EndStint(time);
                }
            }

            /// <summary>Called for each segment the player is not on the ice in.</summary>
            public void Leave(int i)
            {
                if (_stint == null)
                {
                    return;
                }

                SegmentTiming tm = _p.Segments[i];
                if (tm.ChangeSeconds > 0 || tm.ApproachSeconds > 0)
                {
                    // The change phase of a stoppage series: out to the bench exit, gone when there.
                    CloseChain();
                    RinkPoint from = _stint.StateAt(tm.Start).Position;
                    _stint.Add(TrackChain.Approach(from, _exit, tm.Start, tm.ApproachStart));
                    EndStint(tm.ApproachStart);
                }
                else if ((tm.Flags & SegmentTimingFlags.StoppageSeries) != 0 && ClosingFaceoffAfter(i))
                {
                    _leavingAtChange = true;
                }
                else if (!_leavingAtChange || (tm.Flags & SegmentTimingFlags.StoppageSeries) == 0)
                {
                    double target = SkaterArrivals.TargetTime(tm, _p.Config.Skaters);
                    AddKey(target, _exit);
                    CloseChain();
                    IReadOnlyList<TrackChain> chains = _stint.Chains;
                    EndStint(chains[chains.Count - 1].End);
                }
            }

            /// <summary>Called for each segment the player is on the ice in.</summary>
            public void Stay()
            {
                _leavingAtChange = false;
            }

            /// <summary>The first time in [<paramref name="from"/>, the end of the last stint] the player is within <c>bench.enterWithinMetres</c> of his exit.</summary>
            public double NearExit(double from, double until)
            {
                SkaterStint s = LastEnded;
                if (s == null || double.IsPositiveInfinity(s.Vanish) || s.Vanish < from)
                {
                    return from;
                }

                double within = _p.Config.Bench.EnterWithinMetres;
                double end = Math.Min(s.Vanish, Math.Max(from, until));
                double before = from;
                for (double t = from; t < end; t += PartnerScanSeconds)
                {
                    if (RinkPoint.Distance(s.StateAt(t).Position, _exit) <= within)
                    {
                        double lo = before;
                        double hi = t;
                        for (int k = 0; k < PartnerBisections && t > from; k++)
                        {
                            double mid = (lo + hi) / 2;
                            if (RinkPoint.Distance(s.StateAt(mid).Position, _exit) <= within)
                            {
                                hi = mid;
                            }
                            else
                            {
                                lo = mid;
                            }
                        }

                        return hi;
                    }

                    before = t;
                }

                return end;
            }

            public void Enter(double time, RinkPoint at, bool entering)
            {
                _stint = new SkaterStint(time) { EntryPoint = entering ? at : (RinkPoint?)null };
                _points = new List<RinkPoint> { at };
                _targets = new List<double> { time };
                _leavingAtChange = false;
            }

            public void Target(int i, int nodeId)
            {
                SegmentTiming tm = _p.Segments[i];
                RinkPoint target = NodeTarget(nodeId);
                if (tm.ApproachSeconds > 0)
                {
                    CloseChain();
                    RinkPoint from = _stint.StateAt(tm.ApproachStart).Position;
                    _stint.Add(TrackChain.Approach(from, target, tm.ApproachStart, tm.GapStart));
                    _points = new List<RinkPoint> { target };
                    _targets = new List<double> { tm.GapStart };
                    AddKey(SkaterArrivals.TargetTime(tm, _p.Config.Skaters), target);
                }
                else if (tm.Duration > 0)
                {
                    AddKey(SkaterArrivals.TargetTime(tm, _p.Config.Skaters), target);
                }
            }

            public SkaterTrack Finish()
            {
                if (_stint != null)
                {
                    EndStint(double.PositiveInfinity);
                }

                return _track;
            }

            private bool ClosingFaceoffAfter(int i)
            {
                IReadOnlyList<SegmentTiming> timings = _p.Segments;
                for (int j = i + 1; j < timings.Count; j++)
                {
                    if (timings[j].ChangeSeconds > 0 || timings[j].ApproachSeconds > 0)
                    {
                        return true;
                    }

                    if ((timings[j].Flags & SegmentTimingFlags.StoppageSeries) == 0)
                    {
                        return false;
                    }
                }

                return false;
            }

            private void AddKey(double time, RinkPoint at)
            {
                if (_points == null)
                {
                    _points = new List<RinkPoint>();
                    _targets = new List<double>();
                }

                _points.Add(at);
                _targets.Add(time);
            }

            private void CloseChain()
            {
                if (_points != null && _points.Count > 0)
                {
                    _stint.Add(TrackChain.Build(_points, _targets, _p.Config.Skaters));
                }

                _points = null;
                _targets = null;
            }

            private void EndStint(double vanish)
            {
                CloseChain();
                _stint.Vanish = Math.Max(_stint.Appear, vanish);
                _stint.Settle();
                _track.Add(_stint);
                LastEnded = _stint;
                _stint = null;
                _leavingAtChange = false;
            }
        }
    }
}
