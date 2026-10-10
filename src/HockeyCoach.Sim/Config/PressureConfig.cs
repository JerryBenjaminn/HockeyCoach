namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// tuning.json <c>pressure</c>: the team pressure state (momentum, data-schema.md O-9) and the physical pressure
    /// radius (M-7, D-049, D-056).
    /// </summary>
    public sealed class PressureConfig
    {
        /// <summary>Creates the config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="underPressureNodes">A player is under pressure when an opposing skater is within this Chebyshev distance.</param>
        /// <param name="gainOnZoneEntry">Gain per controlled zone entry that kept the puck.</param>
        /// <param name="gainOnShot">Gain per shot attempt.</param>
        /// <param name="gainPerSecondInZone">Gain per second with the puck in the offensive zone.</param>
        /// <param name="decayPerSecond">Decay per second otherwise.</param>
        /// <param name="keepOnStoppage">Multiplier at every stoppage.</param>
        /// <param name="energyDrainPerSecond">Opponent energy drain per second at pressure 1.</param>
        /// <param name="mentalToughnessReductionPerPoint">Goalie factor slope: g = 1 − value × (mental toughness − referenceValue).</param>
        public PressureConfig(
            int underPressureNodes,
            double gainOnZoneEntry,
            double gainOnShot,
            double gainPerSecondInZone,
            double decayPerSecond,
            double keepOnStoppage,
            double energyDrainPerSecond,
            double mentalToughnessReductionPerPoint)
        {
            UnderPressureNodes = underPressureNodes;
            GainOnZoneEntry = gainOnZoneEntry;
            GainOnShot = gainOnShot;
            GainPerSecondInZone = gainPerSecondInZone;
            DecayPerSecond = decayPerSecond;
            KeepOnStoppage = keepOnStoppage;
            EnergyDrainPerSecond = energyDrainPerSecond;
            MentalToughnessReductionPerPoint = mentalToughnessReductionPerPoint;
        }

        /// <summary>A player is under pressure when an opposing skater is within this Chebyshev distance.</summary>
        public int UnderPressureNodes { get; }

        /// <summary>Gain per controlled zone entry that kept the puck.</summary>
        public double GainOnZoneEntry { get; }

        /// <summary>Gain per shot attempt.</summary>
        public double GainOnShot { get; }

        /// <summary>Gain per second with the puck in the offensive zone.</summary>
        public double GainPerSecondInZone { get; }

        /// <summary>Decay per second otherwise.</summary>
        public double DecayPerSecond { get; }

        /// <summary>Multiplier at every stoppage.</summary>
        public double KeepOnStoppage { get; }

        /// <summary>Opponent energy drain per second at pressure 1.</summary>
        public double EnergyDrainPerSecond { get; }

        /// <summary>Goalie factor slope.</summary>
        public double MentalToughnessReductionPerPoint { get; }
    }
}
