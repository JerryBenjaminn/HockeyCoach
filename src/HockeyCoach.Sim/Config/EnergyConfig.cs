namespace HockeyCoach.Sim.Config
{
    /// <summary>tuning.json <c>energy</c> (data-schema.md O-5, D-062). Energy is 0..1.</summary>
    public sealed class EnergyConfig
    {
        /// <summary>Creates the config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="start">Energy at the start of every period.</param>
        /// <param name="drainPerSecondOnIce">Drain per game second on the ice.</param>
        /// <param name="costPerAction">Drain per check a skater takes part in.</param>
        /// <param name="enduranceCostReductionPerPoint">Endurance factor slope: f = 1 − value × (endurance − referenceValue).</param>
        /// <param name="benchRecoveryRate">Exponential bench recovery rate per second.</param>
        /// <param name="checkModifierAtZero">Logit at energy 0 (at most 0), linear in between.</param>
        public EnergyConfig(
            double start,
            double drainPerSecondOnIce,
            double costPerAction,
            double enduranceCostReductionPerPoint,
            double benchRecoveryRate,
            double checkModifierAtZero)
        {
            Start = start;
            DrainPerSecondOnIce = drainPerSecondOnIce;
            CostPerAction = costPerAction;
            EnduranceCostReductionPerPoint = enduranceCostReductionPerPoint;
            BenchRecoveryRate = benchRecoveryRate;
            CheckModifierAtZero = checkModifierAtZero;
        }

        /// <summary>Energy at the start of every period.</summary>
        public double Start { get; }

        /// <summary>Drain per game second on the ice.</summary>
        public double DrainPerSecondOnIce { get; }

        /// <summary>Drain per check a skater takes part in.</summary>
        public double CostPerAction { get; }

        /// <summary>Endurance factor slope.</summary>
        public double EnduranceCostReductionPerPoint { get; }

        /// <summary>Exponential bench recovery rate per second.</summary>
        public double BenchRecoveryRate { get; }

        /// <summary>Logit at energy 0.</summary>
        public double CheckModifierAtZero { get; }
    }
}
