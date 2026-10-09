namespace HockeyCoach.Sim.Random
{
    /// <summary>
    /// Deterministic source of randomness. A single instance is passed into a match as a parameter;
    /// all random decisions in the simulation are drawn from it, in a fixed order.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Returns the next uniformly distributed 32-bit unsigned integer.</summary>
        uint NextUInt();

        /// <summary>
        /// Returns an unbiased uniformly distributed integer in [0, <paramref name="maxExclusive"/>).
        /// </summary>
        /// <param name="maxExclusive">Exclusive upper bound; must be greater than zero.</param>
        int NextInt(int maxExclusive);

        /// <summary>
        /// Returns an unbiased uniformly distributed integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
        /// </summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>Returns a uniformly distributed double in [0, 1) with 53 bits of precision.</summary>
        double NextDouble();

        /// <summary>
        /// Returns true with probability <paramref name="probability"/>. Always consumes exactly one
        /// <see cref="NextDouble"/> draw, so the stream position does not depend on the probability value.
        /// </summary>
        bool Chance(double probability);
    }
}
