namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Why play stopped (event schema: maalivahti sulki kiekon, pitkä kiekko, paitsio, jäähy, maali, kiekko ulos
    /// kaukalosta (D-063), erän loppu (D-064)).
    /// </summary>
    public enum StoppageReason
    {
        /// <summary>The goalie froze the puck.</summary>
        GoalieFreeze = 0,

        /// <summary>Icing (pitkä kiekko).</summary>
        Icing = 1,

        /// <summary>Offside (paitsio).</summary>
        Offside = 2,

        /// <summary>Penalty (jäähy).</summary>
        Penalty = 3,

        /// <summary>Goal (maali).</summary>
        Goal = 4,

        /// <summary>Puck out of play (kiekko ulos kaukalosta, D-063).</summary>
        OutOfPlay = 5,

        /// <summary>End of period (erän loppu, D-064).</summary>
        PeriodEnd = 6,
    }
}
