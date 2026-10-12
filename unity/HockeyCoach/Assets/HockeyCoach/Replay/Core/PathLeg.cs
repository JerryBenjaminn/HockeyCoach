using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// One leg of a skater path from <c>P1</c> to <c>P2</c> with neighbours <c>P0</c> and <c>P3</c> (data-schema.md,
    /// replay.json, Reitti): a centripetal Catmull-Rom curve with parameter α = <c>pathAlpha</c>, blended towards the
    /// straight line by <c>pathTension</c>. Positions are looked up by arc length through a table of
    /// chords of about <see cref="ArcResolutionMetres"/>. A leg with P1 = P2 has length 0 (the player stands).
    /// </summary>
    public sealed class PathLeg
    {
        /// <summary>Fewest chords per leg in the arc-length table.</summary>
        public const int MinArcSamples = 16;

        /// <summary>Most chords per leg in the arc-length table.</summary>
        public const int MaxArcSamples = 1024;

        /// <summary>Target chord length of the arc-length table in metres (accuracy of the speed along the curve).</summary>
        public const double ArcResolutionMetres = 0.05;

        private const double MinKnot = 1e-4;

        private readonly RinkPoint _p0;
        private readonly RinkPoint _p1;
        private readonly RinkPoint _p2;
        private readonly RinkPoint _p3;
        private readonly double _t0;
        private readonly double _t1;
        private readonly double _t2;
        private readonly double _t3;
        private readonly double _tension;
        private readonly double[] _arc;

        /// <summary>Creates the leg.</summary>
        public PathLeg(RinkPoint p0, RinkPoint p1, RinkPoint p2, RinkPoint p3, double alpha, double tension)
        {
            _p0 = p0;
            _p1 = p1;
            _p2 = p2;
            _p3 = p3;
            _tension = tension;
            _t0 = 0;
            _t1 = _t0 + Knot(p0, p1, alpha);
            _t2 = _t1 + Knot(p1, p2, alpha);
            _t3 = _t2 + Knot(p2, p3, alpha);
            if (p1 == p2)
            {
                _arc = new double[2];
                return;
            }

            _arc = Table(MinArcSamples);
            int samples = (int)Math.Ceiling(Length / ArcResolutionMetres);
            if (samples > MinArcSamples)
            {
                _arc = Table(Math.Min(MaxArcSamples, samples));
            }
        }

        /// <summary>Start of the leg.</summary>
        public RinkPoint Start => _p1;

        /// <summary>End of the leg.</summary>
        public RinkPoint End => _p2;

        /// <summary>Arc length in metres.</summary>
        public double Length => _arc[_arc.Length - 1];

        /// <summary>The point at arc length <paramref name="s"/> from the start (clamped).</summary>
        public RinkPoint PointAt(double s)
        {
            if (!(Length > 0) || !(s > 0))
            {
                return _p1;
            }

            if (s >= Length)
            {
                return _p2;
            }

            return AtParameter(ParameterAt(s));
        }

        /// <summary>Unit tangent at arc length <paramref name="s"/> (zero for a standing leg).</summary>
        public RinkPoint TangentAt(double s)
        {
            if (!(Length > 0))
            {
                return new RinkPoint(0, 0);
            }

            double u = ParameterAt(Math.Max(0, Math.Min(Length, s)));
            double h = 0.25 / (_arc.Length - 1);
            RinkPoint a = AtParameter(Math.Max(0, u - h));
            RinkPoint b = AtParameter(Math.Min(1, u + h));
            double d = RinkPoint.Distance(a, b);
            return d > 0 ? new RinkPoint((b.X - a.X) / d, (b.Y - a.Y) / d) : new RinkPoint(0, 0);
        }

        /// <summary>The curve point at parameter <paramref name="u"/> in [0, 1].</summary>
        public RinkPoint AtParameter(double u)
        {
            RinkPoint straight = RinkPoint.Lerp(_p1, _p2, u);
            if (_tension >= 1)
            {
                return straight;
            }

            double t = _t1 + ((_t2 - _t1) * u);
            RinkPoint a1 = Blend(_p0, _p1, _t0, _t1, t);
            RinkPoint a2 = Blend(_p1, _p2, _t1, _t2, t);
            RinkPoint a3 = Blend(_p2, _p3, _t2, _t3, t);
            RinkPoint b1 = Blend(a1, a2, _t0, _t2, t);
            RinkPoint b2 = Blend(a2, a3, _t1, _t3, t);
            RinkPoint c = Blend(b1, b2, _t1, _t2, t);
            return new RinkPoint(((1 - _tension) * c.X) + (_tension * straight.X), ((1 - _tension) * c.Y) + (_tension * straight.Y));
        }

        private double[] Table(int samples)
        {
            var arc = new double[samples + 1];
            RinkPoint last = _p1;
            for (int i = 1; i <= samples; i++)
            {
                RinkPoint now = AtParameter((double)i / samples);
                arc[i] = arc[i - 1] + RinkPoint.Distance(last, now);
                last = now;
            }

            return arc;
        }

        private double ParameterAt(double s)
        {
            int lo = 0;
            int hi = _arc.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (_arc[mid] <= s)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            double span = _arc[hi] - _arc[lo];
            double f = span > 0 ? (s - _arc[lo]) / span : 0;
            return (lo + f) / (_arc.Length - 1);
        }

        private static double Knot(RinkPoint a, RinkPoint b, double alpha)
        {
            return Math.Max(MinKnot, Math.Pow(RinkPoint.Distance(a, b), alpha));
        }

        private static RinkPoint Blend(RinkPoint a, RinkPoint b, double ta, double tb, double t)
        {
            double w = (t - ta) / (tb - ta);
            return RinkPoint.Lerp(a, b, w);
        }
    }
}
