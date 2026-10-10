namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Shot classification by origin from tuning.json <c>chanceTypes</c> (data-schema.md O-12). The rush threshold is
    /// <c>organization.organizedThreshold</c>.
    /// </summary>
    public sealed class ChanceTypesConfig
    {
        /// <summary>Creates the config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="turnoverWindowSeconds">A shot this soon after a gain from the opponent is a turnover chance.</param>
        public ChanceTypesConfig(double turnoverWindowSeconds)
        {
            TurnoverWindowSeconds = turnoverWindowSeconds;
        }

        /// <summary>A shot this soon after a gain from the opponent is a turnover chance.</summary>
        public double TurnoverWindowSeconds { get; }
    }
}
