using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// A player's own deterministic offset inside a node and idle phase (Q-080, data-schema.md, replay.json, Pelaajan oma
    /// poikkeama). Depends only on the match seed and the player id, so it is the same for the whole match.
    /// </summary>
    public static class SkaterOffsets
    {
        private const ulong Golden = 0x9E3779B97F4A7C15UL;
        private const double TwoPi = 2 * Math.PI;

        /// <summary>
        /// The three uniform numbers u₁, u₂, u₃ in [0, 1): s = seed + 0x9E3779B97F4A7C15 × (id + 1),
        /// hₖ = mix(s + k × 0x9E3779B97F4A7C15), uₖ = (hₖ &gt;&gt; 11) × 2⁻⁵³ (SplitMix64, unsigned 64-bit arithmetic).
        /// </summary>
        public static double Uniform(ulong seed, int id, int k)
        {
            if (k < 1 || k > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(k), k, "k is 1, 2 or 3.");
            }

            unchecked
            {
                ulong s = seed + (Golden * (ulong)((long)id + 1));
                ulong h = Mix(s + ((ulong)k * Golden));
                return (h >> 11) * (1.0 / 9007199254740992.0);
            }
        }

        /// <summary>SplitMix64 finaliser.</summary>
        public static ulong Mix(ulong z)
        {
            unchecked
            {
                z ^= z >> 30;
                z *= 0xBF58476D1CE4E5B9UL;
                z ^= z >> 27;
                z *= 0x94D049BB133111EBUL;
                z ^= z >> 31;
                return z;
            }
        }

        /// <summary>Offset = <paramref name="radius"/> × √u₁ × (cos 2πu₂, sin 2πu₂), as a displacement in metres.</summary>
        public static RinkPoint Offset(ulong seed, int id, double radius)
        {
            double r = radius * Math.Sqrt(Uniform(seed, id, 1));
            double a = Angle(seed, id);
            return new RinkPoint(r * Math.Cos(a), r * Math.Sin(a));
        }

        /// <summary>The player's angle 2πu₂ (also the separation direction of coincident pairs).</summary>
        public static double Angle(ulong seed, int id)
        {
            return TwoPi * Uniform(seed, id, 2);
        }

        /// <summary>The idle motion phase φ = 2πu₃.</summary>
        public static double Phase(ulong seed, int id)
        {
            return TwoPi * Uniform(seed, id, 3);
        }
    }
}
