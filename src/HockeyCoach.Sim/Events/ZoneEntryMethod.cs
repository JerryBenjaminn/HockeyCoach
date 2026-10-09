namespace HockeyCoach.Sim.Events
{
    /// <summary>How the puck was brought into the offensive zone (event schema: "tapa").</summary>
    public enum ZoneEntryMethod
    {
        /// <summary>Carried in (kuljetus).</summary>
        Carry = 0,

        /// <summary>Passed in (syöttö).</summary>
        Pass = 1,
    }
}
