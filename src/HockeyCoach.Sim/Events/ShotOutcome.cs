namespace HockeyCoach.Sim.Events
{
    /// <summary>Result of a shot (event schema: "maali, torjuttu, blokattu, ohi").</summary>
    public enum ShotOutcome
    {
        /// <summary>Goal (maali).</summary>
        Goal = 0,

        /// <summary>Saved by the goalie (torjuttu).</summary>
        Saved = 1,

        /// <summary>Blocked by a skater (blokattu).</summary>
        Blocked = 2,

        /// <summary>Missed the net (ohi).</summary>
        Missed = 3,
    }
}
