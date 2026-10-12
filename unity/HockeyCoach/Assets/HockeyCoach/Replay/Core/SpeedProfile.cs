using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>How a <see cref="SpeedProfile"/> moves along its leg.</summary>
    public enum SpeedProfileKind
    {
        /// <summary>No movement (length 0).</summary>
        Stand,

        /// <summary>Accelerate (or brake) from the start speed to the cruise speed, cruise, then change to the end speed.</summary>
        Trapezoid,

        /// <summary>Constant speed length / duration (the trapezoid does not fit, or the skater is late).</summary>
        Constant,

        /// <summary>Starts and ends at rest with a smooth cubic ease (the faceoff approach).</summary>
        Smooth,
    }

    /// <summary>
    /// Distance along a leg as a function of time (data-schema.md, replay.json, Nopeusprofiili): a trapezoid with
    /// <c>acceleration</c>, cruise speed at most <c>topSpeed</c> and <c>deceleration</c> between the given end speeds;
    /// when it does not fit, the constant speed length / duration.
    /// </summary>
    public sealed class SpeedProfile
    {
        private const double Epsilon = 1e-9;
        private readonly double _v0;
        private readonly double _v1;
        private readonly double _rate1;
        private readonly double _rate2;
        private readonly double _t1;
        private readonly double _t2;

        private SpeedProfile(SpeedProfileKind kind, double length, double duration, double v0, double cruise, double v1, double rate1, double t1, double rate2, double t2)
        {
            Kind = kind;
            Length = length;
            Duration = duration;
            _v0 = v0;
            Cruise = cruise;
            _v1 = v1;
            _rate1 = rate1;
            _t1 = t1;
            _rate2 = rate2;
            _t2 = t2;
        }

        /// <summary>Kind of profile.</summary>
        public SpeedProfileKind Kind { get; }

        /// <summary>Leg length in metres.</summary>
        public double Length { get; }

        /// <summary>Leg duration in presentation seconds.</summary>
        public double Duration { get; }

        /// <summary>Cruise speed (the constant speed for <see cref="SpeedProfileKind.Constant"/>).</summary>
        public double Cruise { get; }

        /// <summary>
        /// The trapezoid from <paramref name="startSpeed"/> to <paramref name="endSpeed"/> over <paramref name="length"/>
        /// metres in <paramref name="duration"/> seconds, or the constant speed when it does not fit.
        /// </summary>
        public static SpeedProfile Trapezoid(double length, double duration, double startSpeed, double endSpeed, SkaterSettings skaters)
        {
            if (skaters == null)
            {
                throw new ArgumentNullException(nameof(skaters));
            }

            if (!(length > 0))
            {
                return Stand(duration);
            }

            if (!(duration > 0))
            {
                return new SpeedProfile(SpeedProfileKind.Constant, length, 0, 0, 0, 0, 0, 0, 0, 0);
            }

            double a = skaters.Acceleration;
            double d = skaters.Deceleration;
            double v0 = startSpeed;
            double v1 = endSpeed;
            double qa = (1 / (2 * a)) + (1 / (2 * d));
            double qb = duration + (v0 / a) + (v1 / d);
            double qc = ((v0 * v0) / (2 * a)) + ((v1 * v1) / (2 * d)) + length;
            double disc = (qb * qb) - (4 * qa * qc);
            if (disc >= 0)
            {
                double vc = (qb - Math.Sqrt(disc)) / (2 * qa);
                double t1 = (vc - v0) / a;
                double t2 = (vc - v1) / d;
                if (vc >= Math.Max(v0, v1) - Epsilon && vc <= skaters.TopSpeed + Epsilon && t1 + t2 <= duration + Epsilon)
                {
                    return new SpeedProfile(SpeedProfileKind.Trapezoid, length, duration, v0, vc, v1, a, Math.Max(0, t1), -d, Math.Max(0, t2));
                }
            }

            return Constant(length, duration);
        }

        /// <summary>Constant speed length / duration.</summary>
        public static SpeedProfile Constant(double length, double duration)
        {
            double v = duration > 0 ? length / duration : 0;
            return new SpeedProfile(SpeedProfileKind.Constant, length, duration, v, v, v, 0, 0, 0, 0);
        }

        /// <summary>Starts and ends at rest: s = L (3x² − 2x³), x = time / duration.</summary>
        public static SpeedProfile Smooth(double length, double duration)
        {
            if (!(length > 0))
            {
                return Stand(duration);
            }

            return new SpeedProfile(SpeedProfileKind.Smooth, length, duration, 0, 1.5 * length / Math.Max(duration, Epsilon), 0, 0, 0, 0, 0);
        }

        /// <summary>No movement.</summary>
        public static SpeedProfile Stand(double duration)
        {
            return new SpeedProfile(SpeedProfileKind.Stand, 0, Math.Max(0, duration), 0, 0, 0, 0, 0, 0, 0);
        }

        /// <summary>Distance covered at time <paramref name="t"/> from the start of the leg (clamped).</summary>
        public double DistanceAt(double t)
        {
            if (Kind == SpeedProfileKind.Stand || !(t > 0))
            {
                return 0;
            }

            if (t >= Duration)
            {
                return Length;
            }

            switch (Kind)
            {
                case SpeedProfileKind.Constant:
                    return Math.Min(Length, Cruise * t);
                case SpeedProfileKind.Smooth:
                {
                    double x = t / Duration;
                    return Length * x * x * (3 - (2 * x));
                }

                default:
                {
                    double cruiseEnd = Duration - _t2;
                    if (t <= _t1)
                    {
                        return (_v0 * t) + (0.5 * _rate1 * t * t);
                    }

                    double s1 = (_v0 * _t1) + (0.5 * _rate1 * _t1 * _t1);
                    if (t <= cruiseEnd)
                    {
                        return s1 + (Cruise * (t - _t1));
                    }

                    double tau = t - cruiseEnd;
                    double s2 = s1 + (Cruise * (cruiseEnd - _t1));
                    return Math.Min(Length, s2 + (Cruise * tau) + (0.5 * _rate2 * tau * tau));
                }
            }
        }

        /// <summary>Speed at time <paramref name="t"/> from the start of the leg (0 outside it).</summary>
        public double SpeedAt(double t)
        {
            if (Kind == SpeedProfileKind.Stand || t < 0 || t > Duration)
            {
                return 0;
            }

            switch (Kind)
            {
                case SpeedProfileKind.Constant:
                    return Cruise;
                case SpeedProfileKind.Smooth:
                {
                    double x = t / Duration;
                    return 6 * Length * x * (1 - x) / Duration;
                }

                default:
                    if (t <= _t1)
                    {
                        return _v0 + (_rate1 * t);
                    }

                    if (t <= Duration - _t2)
                    {
                        return Cruise;
                    }

                    return Math.Max(0, Cruise + (_rate2 * (t - (Duration - _t2))));
            }
        }
    }
}
