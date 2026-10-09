using System;

namespace HockeyCoach.Sim.Model
{
    /// <summary>A rink grid coordinate (x along the length, y across the width).</summary>
    public readonly struct GridPoint : IEquatable<GridPoint>
    {
        /// <summary>Creates a coordinate.</summary>
        public GridPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Coordinate along the length of the rink.</summary>
        public int X { get; }

        /// <summary>Coordinate across the width of the rink.</summary>
        public int Y { get; }

        /// <inheritdoc />
        public bool Equals(GridPoint other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is GridPoint other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (X * 397) ^ Y;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }
    }
}
