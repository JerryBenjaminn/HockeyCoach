using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>A skater's position, path velocity and path speed at one instant (before idle motion and separation).</summary>
    public readonly struct TrackState
    {
        /// <summary>Creates the state.</summary>
        public TrackState(RinkPoint position, RinkPoint direction, double speed, bool isApproach)
        {
            Position = position;
            Direction = direction;
            Speed = speed;
            IsApproach = isApproach;
        }

        /// <summary>Position in metres.</summary>
        public RinkPoint Position { get; }

        /// <summary>Unit direction of motion (zero when standing).</summary>
        public RinkPoint Direction { get; }

        /// <summary>Speed from the speed profile (m per presentation second).</summary>
        public double Speed { get; }

        /// <summary>True during a frozen faceoff approach.</summary>
        public bool IsApproach { get; }
    }

    /// <summary>
    /// A run of targets reached in one go: either a Catmull-Rom chain with trapezoid speed profiles and precomputed
    /// arrival times (<see cref="SkaterArrivals"/>), or a faceoff approach (straight, starts and ends at rest, arrives
    /// exactly at its end). A chain starts and ends at rest.
    /// </summary>
    public sealed class TrackChain
    {
        private readonly RinkPoint[] _points;
        private readonly double[] _targets;
        private readonly double[] _arrivals;
        private readonly PathLeg[] _legs;
        private readonly SpeedProfile[] _profiles;

        private TrackChain(RinkPoint[] points, double[] targets, double[] arrivals, PathLeg[] legs, SpeedProfile[] profiles, bool isApproach)
        {
            _points = points;
            _targets = targets;
            _arrivals = arrivals;
            _legs = legs;
            _profiles = profiles;
            IsApproach = isApproach;
        }

        /// <summary>True for a faceoff approach.</summary>
        public bool IsApproach { get; }

        /// <summary>Targets (positions) in order.</summary>
        public IReadOnlyList<RinkPoint> Points => _points;

        /// <summary>Target times in order.</summary>
        public IReadOnlyList<double> Targets => _targets;

        /// <summary>Arrival times in order (≥ the target times; later when the skater is late).</summary>
        public IReadOnlyList<double> Arrivals => _arrivals;

        /// <summary>Legs; leg k runs from point k to point k + 1.</summary>
        public IReadOnlyList<PathLeg> Legs => _legs;

        /// <summary>Speed profile of each leg.</summary>
        public IReadOnlyList<SpeedProfile> Profiles => _profiles;

        /// <summary>Time of the first point.</summary>
        public double Start => _arrivals[0];

        /// <summary>Arrival at the last point.</summary>
        public double End => _arrivals[_arrivals.Length - 1];

        /// <summary>
        /// Builds a chain through <paramref name="points"/> with target times <paramref name="targets"/>: centripetal
        /// Catmull-Rom legs, arrival times with lateness, speed 0 at both ends, at a standing target and at turns sharper
        /// than <c>stopTurnDegrees</c>, otherwise the smaller of the two neighbouring average speeds.
        /// </summary>
        public static TrackChain Build(IReadOnlyList<RinkPoint> points, IReadOnlyList<double> targets, SkaterSettings skaters)
        {
            if (points == null || targets == null || points.Count == 0 || points.Count != targets.Count)
            {
                throw new ArgumentException("A chain needs one target time per point.");
            }

            if (skaters == null)
            {
                throw new ArgumentNullException(nameof(skaters));
            }

            int n = points.Count;
            var p = new RinkPoint[n];
            var tau = new double[n];
            for (int i = 0; i < n; i++)
            {
                p[i] = points[i];
                tau[i] = targets[i];
            }

            var legs = new PathLeg[Math.Max(0, n - 1)];
            var lengths = new double[n];
            for (int k = 0; k < legs.Length; k++)
            {
                RinkPoint before = k > 0 ? p[k - 1] : p[k];
                RinkPoint after = k + 2 < n ? p[k + 2] : p[k + 1];
                legs[k] = new PathLeg(before, p[k], p[k + 1], after, skaters.PathAlpha, skaters.PathTension);
                lengths[k + 1] = legs[k].Length;
            }

            double[] arrivals = SkaterArrivals.Compute(tau, lengths, skaters.TopSpeed, null);
            var speeds = new double[n];
            for (int i = 1; i < n - 1; i++)
            {
                double inLength = lengths[i];
                double outLength = lengths[i + 1];
                double inTime = arrivals[i] - arrivals[i - 1];
                double outTime = arrivals[i + 1] - arrivals[i];
                if (!(inLength > 0) || !(outLength > 0) || !(inTime > 0) || !(outTime > 0) || Turn(p[i - 1], p[i], p[i + 1]) > skaters.StopTurnDegrees)
                {
                    continue;
                }

                speeds[i] = Math.Min(inLength / inTime, outLength / outTime);
            }

            var profiles = new SpeedProfile[legs.Length];
            for (int k = 0; k < legs.Length; k++)
            {
                profiles[k] = SpeedProfile.Trapezoid(lengths[k + 1], arrivals[k + 1] - arrivals[k], speeds[k], speeds[k + 1], skaters);
            }

            return new TrackChain(p, tau, arrivals, legs, profiles, false);
        }

        /// <summary>A faceoff approach from <paramref name="from"/> at <paramref name="start"/> to <paramref name="to"/> at <paramref name="end"/>.</summary>
        public static TrackChain Approach(RinkPoint from, RinkPoint to, double start, double end)
        {
            var leg = new PathLeg(from, from, to, to, 0.5, 1);
            double duration = Math.Max(0, end - start);
            return new TrackChain(
                new[] { from, to },
                new[] { start, end },
                new[] { start, start + duration },
                new[] { leg },
                new[] { SpeedProfile.Smooth(leg.Length, duration) },
                true);
        }

        /// <summary>The state at time <paramref name="t"/> (the first point before the start, the last after the end).</summary>
        public TrackState StateAt(double t)
        {
            if (_legs.Length == 0 || t <= _arrivals[0])
            {
                return new TrackState(_points[0], new RinkPoint(0, 0), 0, false);
            }

            if (t >= End)
            {
                return new TrackState(_points[_points.Length - 1], new RinkPoint(0, 0), 0, false);
            }

            int lo = 0;
            int hi = _arrivals.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (_arrivals[mid] <= t)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            double local = t - _arrivals[lo];
            SpeedProfile profile = _profiles[lo];
            double s = profile.DistanceAt(local);
            double speed = profile.SpeedAt(local);
            PathLeg leg = _legs[lo];
            return new TrackState(leg.PointAt(s), speed > 0 ? leg.TangentAt(s) : new RinkPoint(0, 0), speed, IsApproach);
        }

        /// <summary>Turn in degrees at <paramref name="b"/> between the chords a → b and b → c.</summary>
        public static double Turn(RinkPoint a, RinkPoint b, RinkPoint c)
        {
            double ux = b.X - a.X;
            double uy = b.Y - a.Y;
            double vx = c.X - b.X;
            double vy = c.Y - b.Y;
            double nu = Math.Sqrt((ux * ux) + (uy * uy));
            double nv = Math.Sqrt((vx * vx) + (vy * vy));
            if (!(nu > 0) || !(nv > 0))
            {
                return 0;
            }

            double cos = ((ux * vx) + (uy * vy)) / (nu * nv);
            return Math.Acos(Math.Max(-1, Math.Min(1, cos))) * 180 / Math.PI;
        }
    }

    /// <summary>One stay of a skater on the ice: from <see cref="Appear"/> until <see cref="Vanish"/>, through its chains.</summary>
    public sealed class SkaterStint
    {
        private readonly List<TrackChain> _chains = new List<TrackChain>();

        /// <summary>Creates a stint.</summary>
        public SkaterStint(double appear)
        {
            Appear = appear;
            Vanish = double.PositiveInfinity;
        }

        /// <summary>Presentation time the skater appears (at his first point).</summary>
        public double Appear { get; }

        /// <summary>Presentation time the skater disappears (reaches the bench gate, or the period ends).</summary>
        public double Vanish { get; internal set; }

        /// <summary>Chains in time order.</summary>
        public IReadOnlyList<TrackChain> Chains => _chains;

        internal void Add(TrackChain chain)
        {
            _chains.Add(chain);
        }

        /// <summary>The state at time <paramref name="t"/>: the last chain that has started, or the first one before it starts.</summary>
        public TrackState StateAt(double t)
        {
            if (_chains.Count == 0)
            {
                throw new InvalidOperationException("A stint has at least one chain.");
            }

            int index = 0;
            for (int i = 1; i < _chains.Count; i++)
            {
                if (_chains[i].Start <= t)
                {
                    index = i;
                }
                else
                {
                    break;
                }
            }

            return _chains[index].StateAt(t);
        }
    }

    /// <summary>All stints of one skater in time order.</summary>
    public sealed class SkaterTrack
    {
        private readonly List<SkaterStint> _stints = new List<SkaterStint>();

        /// <summary>Creates an empty track.</summary>
        public SkaterTrack(int id, ReplayTeam team)
        {
            Id = id;
            Team = team;
        }

        /// <summary>Player id.</summary>
        public int Id { get; }

        /// <summary>Team.</summary>
        public ReplayTeam Team { get; }

        /// <summary>Stints in time order.</summary>
        public IReadOnlyList<SkaterStint> Stints => _stints;

        internal void Add(SkaterStint stint)
        {
            _stints.Add(stint);
        }

        /// <summary>True when a stint is active at <paramref name="t"/> (appeared and not vanished).</summary>
        public bool IsActive(double t)
        {
            foreach (SkaterStint s in _stints)
            {
                if (s.Appear <= t && t < s.Vanish)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The state at <paramref name="t"/>: from the active stint, else from the next stint (standing at its first point),
        /// else from the last one.
        /// </summary>
        public TrackState StateAt(double t)
        {
            SkaterStint next = null;
            for (int i = 0; i < _stints.Count; i++)
            {
                SkaterStint s = _stints[i];
                if (s.Appear <= t && t < s.Vanish)
                {
                    return s.StateAt(t);
                }

                if (next == null && s.Appear > t)
                {
                    next = s;
                }
            }

            if (next != null)
            {
                return next.StateAt(next.Appear);
            }

            return _stints[_stints.Count - 1].StateAt(t);
        }
    }
}
