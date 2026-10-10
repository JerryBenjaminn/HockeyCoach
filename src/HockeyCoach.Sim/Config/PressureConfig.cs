namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Typed part of tuning.json <c>pressure</c> used in milestone 2: the physical pressure radius (M-7, D-049).
    /// The team pressure state values (milestone 3) are accepted by the loader but not mapped yet.
    /// </summary>
    public sealed class PressureConfig
    {
        /// <summary>Creates the config.</summary>
        /// <param name="underPressureNodes">A player is under pressure when an opposing skater is within this Chebyshev distance.</param>
        public PressureConfig(int underPressureNodes)
        {
            UnderPressureNodes = underPressureNodes;
        }

        /// <summary>A player is under pressure when an opposing skater is within this Chebyshev distance.</summary>
        public int UnderPressureNodes { get; }
    }
}
