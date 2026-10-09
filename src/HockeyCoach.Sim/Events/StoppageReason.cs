namespace HockeyCoach.Sim.Events
{
    /// <summary>Why play stopped (event schema: "maalivahti sulki kiekon, pitkä kiekko, paitsio, jäähy, maali").</summary>
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
    }
}
