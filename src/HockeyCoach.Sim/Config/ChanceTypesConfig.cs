namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Shot classification by origin from tuning.json <c>chanceTypes</c> (docs/stats-and-checks.md, Paikkatyypit).
    /// </summary>
    public sealed class ChanceTypesConfig
    {
        /// <summary>Creates the config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="rushOrganizationBelow">A shot is a rush chance while the defence's organization is below this (0..1).</param>
        /// <param name="turnoverWindowSeconds">A shot this soon after a takeaway is a turnover chance.</param>
        public ChanceTypesConfig(double rushOrganizationBelow, double turnoverWindowSeconds)
        {
            RushOrganizationBelow = rushOrganizationBelow;
            TurnoverWindowSeconds = turnoverWindowSeconds;
        }

        /// <summary>A shot is a rush chance while the defence's organization is below this (0..1).</summary>
        public double RushOrganizationBelow { get; }

        /// <summary>A shot this soon after a takeaway is a turnover chance.</summary>
        public double TurnoverWindowSeconds { get; }
    }
}
