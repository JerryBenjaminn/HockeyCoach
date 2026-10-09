namespace HockeyCoach.Sim.Model
{
    /// <summary>The 6 goalie stats (docs/stats-and-checks.md). Values are on the internal 1–20 scale.</summary>
    public enum GoalieStat
    {
        /// <summary>Reflexes (data name <c>reflexes</c>).</summary>
        Reflexes = 0,

        /// <summary>Positioning (data name <c>positioning</c>).</summary>
        Positioning = 1,

        /// <summary>Mobility (data name <c>mobility</c>).</summary>
        Mobility = 2,

        /// <summary>Rebound control (data name <c>reboundControl</c>).</summary>
        ReboundControl = 3,

        /// <summary>Puck handling (data name <c>puckHandling</c>).</summary>
        PuckHandling = 4,

        /// <summary>Mental toughness (data name <c>mentalToughness</c>).</summary>
        MentalToughness = 5,
    }
}
