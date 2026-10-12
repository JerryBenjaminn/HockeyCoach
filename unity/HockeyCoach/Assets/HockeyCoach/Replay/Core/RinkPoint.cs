using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// A position on the rink in metres, in the home team's frame: <see cref="X"/> along the rink from the home end
    /// boards (the home team attacks towards +x), <see cref="Y"/> across the rink from the board of lane 0.
    /// </summary>
    public readonly struct RinkPoint : IEquatable<RinkPoint>
    {
        /// <summary>Creates a point.</summary>
        public RinkPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Metres along the rink from the home end boards.</summary>
        public double X { get; }

        /// <summary>Metres across the rink from the board of lane 0.</summary>
        public double Y { get; }

        /// <summary>Linear interpolation from <paramref name="a"/> (t = 0) to <paramref name="b"/> (t = 1); t is not clamped.</summary>
        public static RinkPoint Lerp(RinkPoint a, RinkPoint b, double t)
        {
            return new RinkPoint(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
        }

        /// <summary>Euclidean distance in metres.</summary>
        public static double Distance(RinkPoint a, RinkPoint b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>Equality operator (exact comparison).</summary>
        public static bool operator ==(RinkPoint a, RinkPoint b)
        {
            return a.Equals(b);
        }

        /// <summary>Inequality operator (exact comparison).</summary>
        public static bool operator !=(RinkPoint a, RinkPoint b)
        {
            return !a.Equals(b);
        }

        /// <inheritdoc/>
        public bool Equals(RinkPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is RinkPoint other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "(" + X.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ", "
                + Y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ")";
        }
    }
}
