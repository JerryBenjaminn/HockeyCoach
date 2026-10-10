namespace HockeyCoach.Sim.Config
{
    /// <summary>tuning.json <c>familiarity</c> (data-schema.md O-11, D-066).</summary>
    public sealed class FamiliarityConfig
    {
        /// <summary>Creates the config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="freeUses">Uses without a penalty.</param>
        /// <param name="penaltyPerRepeat">Logit per use beyond the free uses.</param>
        /// <param name="maxPenalty">Largest penalty.</param>
        /// <param name="intermissionMultiplier">Counter multiplier at every intermission.</param>
        public FamiliarityConfig(double freeUses, double penaltyPerRepeat, double maxPenalty, double intermissionMultiplier)
        {
            FreeUses = freeUses;
            PenaltyPerRepeat = penaltyPerRepeat;
            MaxPenalty = maxPenalty;
            IntermissionMultiplier = intermissionMultiplier;
        }

        /// <summary>Uses without a penalty.</summary>
        public double FreeUses { get; }

        /// <summary>Logit per use beyond the free uses.</summary>
        public double PenaltyPerRepeat { get; }

        /// <summary>Largest penalty.</summary>
        public double MaxPenalty { get; }

        /// <summary>Counter multiplier at every intermission.</summary>
        public double IntermissionMultiplier { get; }
    }
}
