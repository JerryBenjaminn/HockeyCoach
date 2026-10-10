namespace HockeyCoach.Sim.Tactics
{
    /// <summary>What a team does on gaining the puck other than from a faceoff (D-007, data-schema.md O-7).</summary>
    public enum TransitionInstruction
    {
        /// <summary>Built-in rush without a play or setup (default, D-062).</summary>
        Rush = 0,

        /// <summary>Regroup in the own or neutral zone, then a play with setup.</summary>
        Regroup = 1,
    }
}
