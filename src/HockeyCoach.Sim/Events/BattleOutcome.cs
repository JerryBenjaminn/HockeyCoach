namespace HockeyCoach.Sim.Events
{
    /// <summary>Result of a puck battle (event schema: "voitto, ei voittajaa, häviö"), seen from the attacker side.</summary>
    public enum BattleOutcome
    {
        /// <summary>The attacker side won the puck.</summary>
        Win = 0,

        /// <summary>No winner.</summary>
        NoWinner = 1,

        /// <summary>The defender side won the puck.</summary>
        Loss = 2,
    }
}
