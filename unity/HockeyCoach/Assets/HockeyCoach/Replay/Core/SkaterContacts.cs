using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Contact displacements of the skaters (D-089 A2, data-schema.md, replay.json, Kontaktit), precomputed once over the
    /// whole match at <c>skaters.contactStepsPerSecond</c> and interpolated linearly between steps, so sampling stays
    /// stateless and seeking gives the same result as playing. Pairs carry state from step to step, which is what keeps
    /// them from orbiting:
    /// <list type="bullet">
    /// <item>Teammates in contact (see <see cref="SkaterAvoidance.TeammateWeight"/>) get a fixed axis when the contact
    /// starts and are pushed apart along it only: they sidestep early, pass side by side and continue.</item>
    /// <item>Opponents within <c>opponentContactMetres</c> turn into a fixed placement, the defender on his own goal
    /// side, <c>opponentShoulderMetres</c> apart; the direction between them turns at most
    /// <c>opponentTurnDegreesPerSecond</c>. No radial separation.</item>
    /// </list>
    /// Changing players (leaving, waiting or entering, D-089 A1) take no part: they are neither moved nor move others, so
    /// nothing pushes them through each other. Goalies are not skaters here.
    /// </summary>
    public sealed class SkaterContacts
    {
        private readonly double _step;
        private readonly int _count;
        private readonly Dictionary<int, float[]> _dx = new Dictionary<int, float[]>();
        private readonly Dictionary<int, float[]> _dy = new Dictionary<int, float[]>();

        private SkaterContacts(double step, int count)
        {
            _step = step;
            _count = count;
        }

        /// <summary>Length of one precomputed step in presentation seconds.</summary>
        public double StepSeconds => _step;

        /// <summary>
        /// The drawn displacement of player <paramref name="id"/> from his path at <paramref name="t"/>: contacts and idle
        /// motion, interpolated between the precomputed steps (zero where he was not drawn).
        /// </summary>
        public RinkPoint At(int id, double t)
        {
            float[] dx;
            if (!_dx.TryGetValue(id, out dx))
            {
                return new RinkPoint(0, 0);
            }

            float[] dy = _dy[id];
            double k = Math.Max(0, t / _step);
            int a = Math.Min(_count - 1, (int)Math.Floor(k));
            int b = Math.Min(_count - 1, a + 1);
            double f = Math.Min(1, k - a);
            return new RinkPoint(dx[a] + ((dx[b] - dx[a]) * f), dy[a] + ((dy[b] - dy[a]) * f));
        }

        /// <summary>
        /// Runs the pair rules over the whole match. Each step starts from the path positions, decides each pair's rule from
        /// the path positions and velocities (weights, the teammates' fixed axis, the opponents' turned direction), then
        /// relaxes the drawn positions <c>skaters.contactIterations</c> times over the pairs in id order, keeping them
        /// inside the rink after each pass, and stores drawn − path.
        /// </summary>
        internal static SkaterContacts Build(SkaterMotion motion)
        {
            PresentationTimeline p = motion.Presentation;
            SkaterSettings sk = p.Config.Skaters;
            RinkMapping map = p.Timeline.Mapping;
            double step = 1.0 / sk.ContactStepsPerSecond;
            int count = (int)Math.Ceiling(p.Duration / step) + 2;
            var contacts = new SkaterContacts(step, count);
            var pairs = new Dictionary<long, RinkPoint>();
            var lastShift = new Dictionary<int, RinkPoint>();
            var lastDrawn = new Dictionary<int, RinkPoint>();
            var seen = new HashSet<long>();
            var rules = new List<Rule>();
            double turn = sk.ContactTurnDegreesPerSecond * Math.PI / 180 * step;
            int segment = 0;
            var visible = new List<SkaterMotion.Visible>();
            for (int k = 0; k < count; k++)
            {
                double t = Math.Min(k * step, p.Duration);
                segment = p.Normalize(segment, t);
                motion.Collect(segment, t, visible);
                seen.Clear();
                rules.Clear();
                int n = visible.Count;
                var weight = new double[n];
                for (int i = 0; i < n; i++)
                {
                    SkaterMotion.Visible a = visible[i];
                    if (a.Changing)
                    {
                        continue;
                    }

                    for (int j = i + 1; j < n; j++)
                    {
                        SkaterMotion.Visible b = visible[j];
                        if (b.Changing)
                        {
                            continue;
                        }

                        long key = ((long)a.Id << 32) | (uint)b.Id;
                        var r = new RinkPoint(a.State.Position.X - b.State.Position.X, a.State.Position.Y - b.State.Position.Y);
                        RinkPoint state;
                        bool has = pairs.TryGetValue(key, out state);
                        if (a.Team == b.Team)
                        {
                            RinkPoint va = Velocity(a.State);
                            RinkPoint vb = Velocity(b.State);
                            var u = new RinkPoint(va.X - vb.X, va.Y - vb.Y);
                            double distance;
                            double w = SkaterAvoidance.TeammateWeight(r, u, sk, out distance);
                            if (!(w > 0))
                            {
                                continue;
                            }

                            if (!has)
                            {
                                state = SkaterAvoidance.TeammateAxis(r, u, motion.AngleOf(a.Id), sk);
                            }

                            pairs[key] = state;
                            seen.Add(key);
                            rules.Add(new Rule(i, j, false, state, sk.SeparationMinMetres, w));
                            weight[i] = Math.Max(weight[i], distance);
                            weight[j] = Math.Max(weight[j], distance);
                        }
                        else
                        {
                            bool aHome = a.Team == ReplayTeam.Home;
                            RinkPoint rel = aHome ? r : new RinkPoint(-r.X, -r.Y);
                            double d = SkaterAvoidance.Length(rel);
                            double w = SkaterAvoidance.OpponentWeight(d, sk);
                            if (!(w > 0) && !has)
                            {
                                continue;
                            }

                            RinkPoint along = d > SkaterAvoidance.Coincident ? new RinkPoint(rel.X / d, rel.Y / d) : (has ? state : new RinkPoint(-1, 0));
                            if (!has)
                            {
                                state = along;
                            }

                            var mid = new RinkPoint((a.State.Position.X + b.State.Position.X) / 2, (a.State.Position.Y + b.State.Position.Y) / 2);
                            RinkPoint axis = SkaterAvoidance.Axis(map, mid);
                            double from = Math.Atan2(along.Y, along.X);
                            double angle = from + (w * Math.IEEERemainder(Math.Atan2(axis.Y, axis.X) - from, 2 * Math.PI));
                            RinkPoint target = w > 0 ? new RinkPoint(Math.Cos(angle), Math.Sin(angle)) : along;
                            state = SkaterAvoidance.Turn(state, target, turn);
                            if (!(w > 0) && state == along)
                            {
                                pairs.Remove(key);
                                continue;
                            }

                            pairs[key] = state;
                            seen.Add(key);
                            double length = d + (w * w * (sk.OpponentShoulderMetres - d));
                            RinkPoint home = aHome ? state : new RinkPoint(-state.X, -state.Y);
                            rules.Add(new Rule(i, j, true, home, length, 1));
                            weight[i] = Math.Max(weight[i], w);
                            weight[j] = Math.Max(weight[j], w);
                        }
                    }
                }

                RemoveUnseen(pairs, seen);
                var x = new double[n];
                var y = new double[n];
                for (int i = 0; i < n; i++)
                {
                    x[i] = visible[i].State.Position.X;
                    y[i] = visible[i].State.Position.Y;
                }

                for (int it = 0; it < sk.ContactIterations && rules.Count > 0; it++)
                {
                    foreach (Rule rule in rules)
                    {
                        rule.Apply(x, y);
                    }

                    Clamp(x, y, map);
                }

                // Idle motion, calmed by the strongest contact weight; nothing for players waiting at the bench.
                double idle = 2 * Math.PI * sk.IdleFrequencyHz;
                for (int i = 0; i < n; i++)
                {
                    SkaterMotion.Visible v = visible[i];
                    if (v.Waiting)
                    {
                        continue;
                    }

                    double amplitude = sk.IdleAmplitudeMetres * (1 - Math.Min(1, v.State.Speed / sk.TopSpeed)) * (1 - Math.Min(1, weight[i]));
                    double phi = motion.PhaseOf(v.Id);
                    x[i] += amplitude * Math.Sin((idle * t) + phi);
                    y[i] += amplitude * Math.Sin((idle * sk.IdleYFrequencyRatio * t) + (2 * phi));
                }

                // The displacement from the path changes at most contactShiftMetresPerSecond, so contacts never pop.
                double shift = sk.ContactShiftMetresPerSecond * step;
                var present = new HashSet<int>();
                for (int i = 0; i < n; i++)
                {
                    SkaterMotion.Visible v = visible[i];
                    if (v.Waiting)
                    {
                        continue;
                    }

                    present.Add(v.Id);
                    RinkPoint before;
                    if (!lastShift.TryGetValue(v.Id, out before))
                    {
                        before = new RinkPoint(0, 0);
                    }

                    double cx = x[i] - v.State.Position.X - before.X;
                    double cy = y[i] - v.State.Position.Y - before.Y;
                    double c = Math.Sqrt((cx * cx) + (cy * cy));
                    if (c > shift)
                    {
                        x[i] = v.State.Position.X + before.X + (cx * shift / c);
                        y[i] = v.State.Position.Y + before.Y + (cy * shift / c);
                    }
                }

                // Close pairs turn slowly: within contactTurnMetres the direction between two drawn skaters turns at most
                // contactTurnDegreesPerSecond from the previous step, so nobody orbits or passes through another.
                double maxTurn = sk.ContactTurnDegreesPerSecond * Math.PI / 180 * step;
                for (int it = 0; it < sk.ContactIterations; it++)
                {
                    for (int i = 0; i < n; i++)
                    {
                        RinkPoint pi;
                        if (visible[i].Waiting || !lastDrawn.TryGetValue(visible[i].Id, out pi))
                        {
                            continue;
                        }

                        for (int j = i + 1; j < n; j++)
                        {
                            RinkPoint pj;
                            if (visible[j].Waiting || !lastDrawn.TryGetValue(visible[j].Id, out pj))
                            {
                                continue;
                            }

                            var before = new RinkPoint(pi.X - pj.X, pi.Y - pj.Y);
                            var now = new RinkPoint(x[i] - x[j], y[i] - y[j]);
                            double db = SkaterAvoidance.Length(before);
                            double dn = SkaterAvoidance.Length(now);
                            if (!(db < sk.ContactTurnMetres) || !(dn < sk.ContactTurnMetres) || !(db > SkaterAvoidance.Coincident) || !(dn > SkaterAvoidance.Coincident))
                            {
                                continue;
                            }

                            RinkPoint from = new RinkPoint(before.X / db, before.Y / db);
                            RinkPoint turned = SkaterAvoidance.Turn(from, new RinkPoint(now.X / dn, now.Y / dn), maxTurn);
                            double mx = (x[i] + x[j]) / 2;
                            double my = (y[i] + y[j]) / 2;
                            x[i] = mx + (turned.X * dn / 2);
                            y[i] = my + (turned.Y * dn / 2);
                            x[j] = mx - (turned.X * dn / 2);
                            y[j] = my - (turned.Y * dn / 2);
                        }
                    }

                    Clamp(x, y, map);
                }

                foreach (int id in new List<int>(lastDrawn.Keys))
                {
                    if (!present.Contains(id))
                    {
                        lastDrawn.Remove(id);
                        lastShift.Remove(id);
                    }
                }

                for (int i = 0; i < n; i++)
                {
                    SkaterMotion.Visible v = visible[i];
                    if (v.Waiting)
                    {
                        continue;
                    }

                    double dx = x[i] - v.State.Position.X;
                    double dy = y[i] - v.State.Position.Y;
                    contacts.Add(v.Id, k, dx, dy);
                    lastShift[v.Id] = new RinkPoint(dx, dy);
                    lastDrawn[v.Id] = new RinkPoint(x[i], y[i]);
                }
            }

            return contacts;
        }

        private static void RemoveUnseen(Dictionary<long, RinkPoint> pairs, HashSet<long> seen)
        {
            if (pairs.Count == seen.Count)
            {
                return;
            }

            var gone = new List<long>();
            foreach (long key in pairs.Keys)
            {
                if (!seen.Contains(key))
                {
                    gone.Add(key);
                }
            }

            foreach (long key in gone)
            {
                pairs.Remove(key);
            }
        }

        private static RinkPoint Velocity(TrackState state)
        {
            return new RinkPoint(state.Direction.X * state.Speed, state.Direction.Y * state.Speed);
        }

        /// <summary>One pair rule of a step, applied to the drawn positions.</summary>
        private sealed class Rule
        {
            private readonly int _i;
            private readonly int _j;
            private readonly bool _opponents;
            private readonly RinkPoint _axis;
            private readonly double _length;
            private readonly double _weight;

            public Rule(int i, int j, bool opponents, RinkPoint axis, double length, double weight)
            {
                _i = i;
                _j = j;
                _opponents = opponents;
                _axis = axis;
                _length = length;
                _weight = weight;
            }

            /// <summary>
            /// Teammates: at least <c>length</c> apart along the fixed axis (i on its + side), by the weight. Opponents: the
            /// vector from j to i becomes <c>length</c> along the axis (the turned direction), by the weight.
            /// </summary>
            public void Apply(double[] x, double[] y)
            {
                double rx = x[_i] - x[_j];
                double ry = y[_i] - y[_j];
                double cx;
                double cy;
                if (_opponents)
                {
                    cx = ((_axis.X * _length) - rx) * _weight / 2;
                    cy = ((_axis.Y * _length) - ry) * _weight / 2;
                }
                else
                {
                    double q = (rx * _axis.X) + (ry * _axis.Y);
                    double push = _weight * Math.Max(0, _length - q) / 2;
                    cx = _axis.X * push;
                    cy = _axis.Y * push;
                }

                x[_i] += cx;
                y[_i] += cy;
                x[_j] -= cx;
                y[_j] -= cy;
            }
        }

        private static void Clamp(double[] x, double[] y, RinkMapping map)
        {
            for (int i = 0; i < x.Length; i++)
            {
                x[i] = Math.Max(0, Math.Min(map.LengthMetres, x[i]));
                y[i] = Math.Max(0, Math.Min(map.WidthMetres, y[i]));
            }
        }

        private void Add(int id, int k, double x, double y)
        {
            float[] dx;
            if (!_dx.TryGetValue(id, out dx))
            {
                dx = new float[_count];
                _dx[id] = dx;
                _dy[id] = new float[_count];
            }

            dx[k] = (float)x;
            _dy[id][k] = (float)y;
        }
    }
}
