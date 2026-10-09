namespace HockeyCoach.Sim.Tactics
{
    /// <summary>The five puck actions of a beat (D-031).</summary>
    public enum PlayActionType
    {
        /// <summary><c>skate</c>: the puck carrier skates with the puck to a node (D-032).</summary>
        Skate = 0,

        /// <summary><c>pass</c>: the puck carrier passes to another position.</summary>
        Pass = 1,

        /// <summary><c>shoot</c>: the puck carrier shoots at the opponent goal. Ends the play.</summary>
        Shoot = 2,

        /// <summary><c>driveNet</c>: a player without the puck goes to the net front (D-020, D-033).</summary>
        DriveNet = 3,

        /// <summary><c>dump</c>: the puck carrier dumps the puck to a node. Ends the play (E-001).</summary>
        Dump = 4,
    }
}
