namespace HockeyCoach.Sim.Tactics
{
    /// <summary>An inclusive integer range <c>[min, max]</c> (system conditions <c>puckX</c>, <c>puckY</c>).</summary>
    public sealed class IntRange
    {
        /// <summary>Creates the range. Use <c>SystemValidator</c> to check min ≤ max.</summary>
        public IntRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>Lowest value (inclusive).</summary>
        public int Min { get; }

        /// <summary>Highest value (inclusive).</summary>
        public int Max { get; }

        /// <summary>Whether <paramref name="value"/> lies in the range.</summary>
        public bool Contains(int value)
        {
            return value >= Min && value <= Max;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "[" + Min + ", " + Max + "]";
        }
    }
}
