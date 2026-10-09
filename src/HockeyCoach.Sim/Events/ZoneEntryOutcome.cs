namespace HockeyCoach.Sim.Events
{
    /// <summary>Result of a controlled zone entry (event schema: "lopputulos").</summary>
    public enum ZoneEntryOutcome
    {
        /// <summary>Possession kept (hallinta säilytetty).</summary>
        Kept = 0,

        /// <summary>Possession lost (menetetty).</summary>
        Lost = 1,
    }
}
