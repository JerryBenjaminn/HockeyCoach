using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>A sampled skater: drawn position and the path state it came from.</summary>
    public readonly struct SkaterPose
    {
        /// <summary>Creates the pose.</summary>
        public SkaterPose(int id, ReplayTeam team, RinkPoint position, TrackState path, bool isLeaving)
        {
            Id = id;
            Team = team;
            Position = position;
            Path = path;
            IsLeaving = isLeaving;
        }

        /// <summary>Player id.</summary>
        public int Id { get; }

        /// <summary>Team.</summary>
        public ReplayTeam Team { get; }

        /// <summary>Drawn position: path + idle motion, separated and clamped inside the rink.</summary>
        public RinkPoint Position { get; }

        /// <summary>The path state (before idle motion and separation).</summary>
        public TrackState Path { get; }

        /// <summary>True for a player skating off to the bench gate (not one of the twelve on the ice).</summary>
        public bool IsLeaving { get; }
    }

    /// <summary>
    /// Skater motion of the presentation layer (D-088, data-schema.md, replay.json, Luistelijat): one
    /// <see cref="SkaterTrack"/> per skater built once from the <see cref="PresentationTimeline"/>, and a stateless
    /// sample at any presentation time: path position (node + own offset targets, centripetal Catmull-Rom, trapezoid
    /// speed profile, anticipation, late arrivals, bench gates, faceoff approach), idle motion, separation in id order
    /// and a clamp inside the rink. Goalies have no track (see <see cref="ReplayMotion"/>).
    /// </summary>
    public sealed class SkaterMotion
    {
        private const double CoincidentMetres = 1e-6;
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
            foreach (int id in teams.Keys)
            {
                offsets[id] = SkaterOffsets.Offset(seed, id, config.Skaters.NodeOffsetRadiusMetres);
                phases[id] = SkaterOffsets.Phase(seed, id);
                angles[id] = SkaterOffsets.Angle(seed, id);
            }

            var tracks = new SortedDictionary<int, SkaterTrack>();
            foreach (KeyValuePair<int, ReplayTeam> pair in teams)
            {
                tracks[pair.Key] = new TrackBuilder(presentation, pair.Key, pair.Value, offsets[pair.Key]).Build();
            }

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

            return new SkaterMotion(presentation, tracks, offsets, phases, angles, extras);
        }

        /// <summary>
        /// The skaters shown in segment <paramref name="segmentIndex"/> at presentation time <paramref name="t"/>, in id order:
        /// the on-ice skaters of the segment and the players still skating to the bench gate.
        /// </summary>
        public SkaterPose[] Sample(int segmentIndex, double t)
        {
            TimelineSegment segment = Presentation.Timeline.Segments[segmentIndex];
            SkaterSettings sk = Presentation.Config.Skaters;
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
            int count = ids.Count;
            var states = new TrackState[count];
            var x = new double[count];
            var y = new double[count];
            double w = 2 * Math.PI * sk.IdleFrequencyHz;
            for (int i = 0; i < count; i++)
            {
                TrackState state = _tracks[ids[i]].StateAt(t);
                states[i] = state;
                double amplitude = sk.IdleAmplitudeMetres * (1 - Math.Min(1, state.Speed / sk.TopSpeed));
                double phi = _phases[ids[i]];
                x[i] = state.Position.X + (amplitude * Math.Sin((w * t) + phi));
                y[i] = state.Position.Y + (amplitude * Math.Sin((w * sk.IdleYFrequencyRatio * t) + (2 * phi)));
            }

            Separate(x, y, ids, sk.SeparationMinMetres, sk.SeparationIterations);
            RinkMapping map = Presentation.Timeline.Mapping;
            var poses = new SkaterPose[count];
            for (int i = 0; i < count; i++)
            {
                var position = new RinkPoint(Math.Max(0, Math.Min(map.LengthMetres, x[i])), Math.Max(0, Math.Min(map.WidthMetres, y[i])));
                poses[i] = new SkaterPose(ids[i], _tracks[ids[i]].Team, position, states[i], leaving.Contains(ids[i]));
            }

            return poses;
        }

        /// <summary>
        /// Stateless separation (data-schema.md, replay.json, Erottelu): <paramref name="iterations"/> passes over the pairs
        /// i &lt; j in id order; a pair closer than <paramref name="minimum"/> is pushed apart by (minimum − d) / 2 each;
        /// coincident pairs use the smaller id's angle 2πu₂.
        /// </summary>
        public void Separate(double[] x, double[] y, IReadOnlyList<int> ids, double minimum, int iterations)
        {
            if (x == null || y == null || ids == null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            for (int it = 0; it < iterations; it++)
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    for (int j = i + 1; j < ids.Count; j++)
                    {
                        double dx = x[i] - x[j];
                        double dy = y[i] - y[j];
                        double d = Math.Sqrt((dx * dx) + (dy * dy));
                        if (!(d < minimum))
                        {
                            continue;
                        }

                        double ux;
                        double uy;
                        if (d < CoincidentMetres)
                        {
                            double a = _angles[Math.Min(ids[i], ids[j])];
                            ux = Math.Cos(a);
                            uy = Math.Sin(a);
                        }
                        else
                        {
                            ux = dx / d;
                            uy = dy / d;
                        }

                        double push = (minimum - d) / 2;
                        x[i] += ux * push;
                        y[i] += uy * push;
                        x[j] -= ux * push;
                        y[j] -= uy * push;
                    }
                }
            }
        }

        /// <summary>Builds one skater's stints by walking the segments.</summary>
        private sealed class TrackBuilder
        {
            private readonly PresentationTimeline _p;
            private readonly int _id;
            private readonly RinkPoint _offset;
            private readonly RinkPoint _gate;
            private readonly SkaterTrack _track;
            private SkaterStint _stint;
            private List<RinkPoint> _points;
            private List<double> _targets;
            private bool _leavingAtApproach;

            public TrackBuilder(PresentationTimeline p, int id, ReplayTeam team, RinkPoint offset)
            {
                _p = p;
                _id = id;
                _offset = offset;
                _gate = PresentationTimeline.Gate(p.Timeline.Mapping, p.Config.Bench, team);
                _track = new SkaterTrack(id, team);
            }

            public SkaterTrack Build()
            {
                IReadOnlyList<TimelineSegment> segs = _p.Timeline.Segments;
                IReadOnlyList<SegmentTiming> timings = _p.Segments;
                double periodStart = 0;
                for (int i = 0; i < segs.Count; i++)
                {
                    TimelineSegment s = segs[i];
                    SegmentTiming tm = timings[i];
                    if ((tm.Flags & SegmentTimingFlags.PeriodStart) != 0)
                    {
                        periodStart = tm.Start;
                        if (_stint != null)
                        {
                            EndStint(tm.Start);
                        }
                    }

                    SegmentPlayer me = Find(s);
                    if (_leavingAtApproach && me == null && tm.ApproachSeconds > 0)
                    {
                        Approach(tm.Start, tm.GapStart, _gate);
                        EndStint(tm.GapStart);
                    }
                    else if (_leavingAtApproach && me == null && (tm.Flags & SegmentTimingFlags.StoppageSeries) == 0)
                    {
                        LeaveFlying(SkaterArrivals.TargetTime(tm, _p.Config.Skaters));
                    }
                    else if (_leavingAtApproach && me != null)
                    {
                        _leavingAtApproach = false;
                    }

                    if (me != null && _stint == null)
                    {
                        RinkPoint node = Target(me.NodeId);
                        if (tm.Start == periodStart)
                        {
                            StartStint(tm.Start, node);
                        }
                        else if ((tm.Flags & SegmentTimingFlags.StoppageSeries) != 0)
                        {
                            StartStint(ApproachStartAfter(i), _gate);
                        }
                        else
                        {
                            StartStint(tm.Start, _gate);
                        }
                    }
                    else if (me == null && _stint != null && !_leavingAtApproach)
                    {
                        if ((tm.Flags & SegmentTimingFlags.StoppageSeries) != 0 && ApproachStartAfter(i) > tm.Start)
                        {
                            _leavingAtApproach = true;
                        }
                        else
                        {
                            LeaveFlying(SkaterArrivals.TargetTime(tm, _p.Config.Skaters));
                        }
                    }

                    if (me == null)
                    {
                        continue;
                    }

                    RinkPoint target = Target(me.NodeId);
                    if (tm.ApproachSeconds > 0)
                    {
                        Approach(tm.Start, tm.GapStart, target);
                        AddKey(SkaterArrivals.TargetTime(tm, _p.Config.Skaters), target);
                    }
                    else if (tm.Duration > 0)
                    {
                        AddKey(SkaterArrivals.TargetTime(tm, _p.Config.Skaters), target);
                    }
                }

                if (_stint != null)
                {
                    EndStint(double.PositiveInfinity);
                }

                return _track;
            }

            private SegmentPlayer Find(TimelineSegment s)
            {
                foreach (SegmentPlayer p in s.Players)
                {
                    if (p.Id == _id)
                    {
                        return p;
                    }
                }

                return null;
            }

            private RinkPoint Target(int nodeId)
            {
                RinkPoint n = _p.Timeline.Mapping.NodePosition(nodeId);
                return new RinkPoint(n.X + _offset.X, n.Y + _offset.Y);
            }

            /// <summary>Start of the approach that ends the stoppage series containing segment <paramref name="i"/>, else its start.</summary>
            private double ApproachStartAfter(int i)
            {
                IReadOnlyList<SegmentTiming> timings = _p.Segments;
                for (int j = i; j < timings.Count; j++)
                {
                    if (timings[j].ApproachSeconds > 0)
                    {
                        return timings[j].Start;
                    }

                    if (j > i && (timings[j].Flags & SegmentTimingFlags.StoppageSeries) == 0)
                    {
                        break;
                    }
                }

                return timings[i].Start;
            }

            private void StartStint(double time, RinkPoint at)
            {
                _stint = new SkaterStint(time);
                _points = new List<RinkPoint> { at };
                _targets = new List<double> { time };
                _leavingAtApproach = false;
            }

            private void AddKey(double time, RinkPoint at)
            {
                if (_points == null)
                {
                    _points = new List<RinkPoint>();
                    _targets = new List<double>();
                }
                else if (_targets.Count > 0 && time < _targets[_targets.Count - 1])
                {
                    return;
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

            private void Approach(double start, double end, RinkPoint to)
            {
                CloseChain();
                RinkPoint from = _stint.StateAt(start).Position;
                _stint.Add(TrackChain.Approach(from, to, start, end));
                _points = new List<RinkPoint> { to };
                _targets = new List<double> { end };
            }

            private void LeaveFlying(double targetTime)
            {
                double last = _targets != null && _targets.Count > 0 ? _targets[_targets.Count - 1] : targetTime;
                AddKey(Math.Max(targetTime, last), _gate);
                CloseChain();
                IReadOnlyList<TrackChain> chains = _stint.Chains;
                EndStint(chains[chains.Count - 1].End);
            }

            private void EndStint(double vanish)
            {
                CloseChain();
                _stint.Vanish = Math.Max(_stint.Appear, vanish);
                _track.Add(_stint);
                _stint = null;
                _leavingAtApproach = false;
            }
        }
    }
}
