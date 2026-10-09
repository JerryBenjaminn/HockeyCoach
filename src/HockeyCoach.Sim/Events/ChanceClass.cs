namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Chance quality class by xG (event schema: "paikkaluokka"; thresholds in tuning.json <c>chanceClasses</c>, Q-004).
    /// A shot below the moderate threshold has no class.
    /// </summary>
    public enum ChanceClass
    {
        /// <summary>Top chance (huippu).</summary>
        Top = 0,

        /// <summary>Good chance (hyvä).</summary>
        Good = 1,

        /// <summary>Moderate chance (kohtalainen).</summary>
        Moderate = 2,
    }
}
