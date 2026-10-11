namespace HockeyCoach.Sim.Random
{
    /// <summary>
    /// Independent random streams derived deterministically from one match seed (tech-spec.md, Determinismi; D-077):
    /// one for the simulation and one for each coach. A coach drawing numbers from its own stream never moves the
    /// simulation stream, so the same seed and the same decisions always give the same event log.
    /// </summary>
    /// <remarks>
    /// The simulation stream is <c>new Pcg32(seed)</c> (PCG stream 0), the same generator a match used before streams
    /// were derived, so existing logs are unchanged. A coach stream uses its own PCG stream id and a seed scrambled with
    /// SplitMix64, so it does not share the simulation's state sequence. The constants below are algorithm constants of
    /// SplitMix64 and stream ids, not tuning values.
    /// </remarks>
    public sealed class MatchStreams
    {
        /// <summary>PCG stream id of the simulation.</summary>
        public const ulong SimulationStreamId = 0UL;

        /// <summary>PCG stream id of the home coach.</summary>
        public const ulong HomeCoachStreamId = 1UL;

        /// <summary>PCG stream id of the away coach.</summary>
        public const ulong AwayCoachStreamId = 2UL;

        // SplitMix64 constants (S. Vigna), algorithm constants.
        private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
        private const ulong MixMultiplier1 = 0xBF58476D1CE4E5B9UL;
        private const ulong MixMultiplier2 = 0x94D049BB133111EBUL;

        /// <summary>Creates the streams of a match.</summary>
        /// <param name="seed">The match seed.</param>
        public MatchStreams(ulong seed)
        {
            Seed = seed;
        }

        /// <summary>The match seed.</summary>
        public ulong Seed { get; }

        /// <summary>A new generator at the start of the simulation stream (identical to <c>new Pcg32(Seed)</c>).</summary>
        public IRandom Simulation()
        {
            return new Pcg32(Seed, SimulationStreamId);
        }

        /// <summary>A new generator at the start of the home coach's stream.</summary>
        public IRandom HomeCoach()
        {
            return Derived(HomeCoachStreamId);
        }

        /// <summary>A new generator at the start of the away coach's stream.</summary>
        public IRandom AwayCoach()
        {
            return Derived(AwayCoachStreamId);
        }

        private IRandom Derived(ulong streamId)
        {
            return new Pcg32(DerivedSeed(streamId), streamId);
        }

        /// <summary>
        /// A seed derived from the match seed for <paramref name="streamId"/> (SplitMix64 of seed + id × golden gamma). Coach
        /// streams use it with their own stream ids; callers may derive other inputs (e.g. harness rosters) with other ids.
        /// </summary>
        public ulong DerivedSeed(ulong streamId)
        {
            return SplitMix64(unchecked(Seed + (streamId * GoldenGamma)));
        }

        /// <summary>The SplitMix64 output function for one state value.</summary>
        internal static ulong SplitMix64(ulong value)
        {
            unchecked
            {
                ulong z = value + GoldenGamma;
                z = (z ^ (z >> 30)) * MixMultiplier1;
                z = (z ^ (z >> 27)) * MixMultiplier2;
                return z ^ (z >> 31);
            }
        }
    }
}
