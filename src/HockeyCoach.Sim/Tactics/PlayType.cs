namespace HockeyCoach.Sim.Tactics
{
    /// <summary>Play type (docs/data-schema.md, Kuviot: <c>type</c>).</summary>
    public enum PlayType
    {
        /// <summary><c>breakout</c>: breakout from the own zone.</summary>
        Breakout = 0,

        /// <summary><c>zoneEntry</c>: entry into the offensive zone.</summary>
        ZoneEntry = 1,

        /// <summary><c>offensiveZone</c>: offensive-zone play.</summary>
        OffensiveZone = 2,

        /// <summary><c>faceoff</c>: faceoff play at a named spot.</summary>
        Faceoff = 3,

        /// <summary><c>powerPlay</c>: power play (later).</summary>
        PowerPlay = 4,
    }
}
