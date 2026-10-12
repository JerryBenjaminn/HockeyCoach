using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// The playback speeds the view offers (e.g. 1×, 2×, 4×, 8×, 16×) and the selected one. <see cref="Faster"/> and
    /// <see cref="Slower"/> move one step and stop at the ends.
    /// </summary>
    public sealed class SpeedLadder
    {
        private readonly double[] _speeds;

        /// <summary>Creates a ladder; <paramref name="speeds"/> must be positive, finite and strictly ascending.</summary>
        /// <exception cref="ArgumentException">The speeds are empty, not positive or not ascending, or the index is outside them.</exception>
        public SpeedLadder(IReadOnlyList<double> speeds, int initialIndex)
        {
            if (speeds == null || speeds.Count == 0)
            {
                throw new ArgumentException("A speed ladder needs at least one speed.", nameof(speeds));
            }

            _speeds = new double[speeds.Count];
            for (int i = 0; i < speeds.Count; i++)
            {
                double s = speeds[i];
                if (!(s > 0) || double.IsInfinity(s) || (i > 0 && !(s > speeds[i - 1])))
                {
                    throw new ArgumentException("Speeds must be positive, finite and strictly ascending.", nameof(speeds));
                }

                _speeds[i] = s;
            }

            if (initialIndex < 0 || initialIndex >= _speeds.Length)
            {
                throw new ArgumentException("Initial speed index " + initialIndex + " is outside the ladder.", nameof(initialIndex));
            }

            Index = initialIndex;
        }

        /// <summary>The speeds, ascending.</summary>
        public IReadOnlyList<double> Speeds => _speeds;

        /// <summary>Index of the selected speed.</summary>
        public int Index { get; private set; }

        /// <summary>The selected speed.</summary>
        public double Current => _speeds[Index];

        /// <summary>Selects the next higher speed (stays at the top) and returns the selected speed.</summary>
        public double Faster()
        {
            Index = Math.Min(Index + 1, _speeds.Length - 1);
            return Current;
        }

        /// <summary>Selects the next lower speed (stays at the bottom) and returns the selected speed.</summary>
        public double Slower()
        {
            Index = Math.Max(Index - 1, 0);
            return Current;
        }
    }
}
