namespace HockeyCoach.Sim.Config
{
    /// <summary>Position-related balance values (tuning.json <c>positions</c>, D-024).</summary>
    public sealed class PositionsConfig
    {
        /// <summary>Creates the positions config.</summary>
        /// <param name="offSideCheckModifier">Logit (negative) applied once per check side that has an off-side player.</param>
        public PositionsConfig(double offSideCheckModifier)
        {
            OffSideCheckModifier = offSideCheckModifier;
        }

        /// <summary>Logit (negative) applied once per check side that has an off-side player.</summary>
        public double OffSideCheckModifier { get; }
    }
}
