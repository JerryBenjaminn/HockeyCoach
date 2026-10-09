using System;

namespace HockeyCoach.Sim.Random
{
    /// <summary>
    /// PCG32 (PCG-XSH-RR 64/32) random number generator by M. E. O'Neill, following the
    /// reference implementation <c>pcg32_random_r</c> / <c>pcg32_srandom_r</c> from pcg-c-basic.
    /// Same seed and stream always produce the same sequence on the same runtime.
    /// </summary>
    public sealed class Pcg32 : IRandom
    {
        // Algorithm constant of PCG's 64-bit LCG (not a tuning value).
        private const ulong Multiplier = 6364136223846793005UL;

        // 2^-53, used to map 53 random bits to [0, 1).
        private const double DoubleUnit = 1.0 / 9007199254740992.0;

        private ulong _state;
        private readonly ulong _increment;

        /// <summary>
        /// Creates a generator. Equivalent to <c>pcg32_srandom_r(rng, seed, stream)</c>.
        /// </summary>
        /// <param name="seed">Initial state (the match seed).</param>
        /// <param name="stream">Stream selector; different streams give independent sequences for the same seed.</param>
        public Pcg32(ulong seed, ulong stream = 0UL)
        {
            _state = 0UL;
            _increment = (stream << 1) | 1UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        /// <inheritdoc />
        public uint NextUInt()
        {
            ulong oldState = _state;
            _state = unchecked(oldState * Multiplier + _increment);
            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rotation = (int)(oldState >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <inheritdoc />
        /// <remarks>Uses the rejection method of <c>pcg32_boundedrand_r</c>.</remarks>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Upper bound must be greater than zero.");
            }

            uint bound = (uint)maxExclusive;
            uint threshold = unchecked(0U - bound) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold)
                {
                    return (int)(r % bound);
                }
            }
        }

        /// <inheritdoc />
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Upper bound must be greater than the lower bound.");
            }

            long range = (long)maxExclusive - minInclusive;
            if (range > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Range is too large.");
            }

            return minInclusive + NextInt((int)range);
        }

        /// <inheritdoc />
        public double NextDouble()
        {
            ulong high = NextUInt() >> 5; // 27 bits
            ulong low = NextUInt() >> 6;  // 26 bits
            return ((high << 26) | low) * DoubleUnit;
        }

        /// <inheritdoc />
        public bool Chance(double probability)
        {
            return NextDouble() < probability;
        }
    }
}
