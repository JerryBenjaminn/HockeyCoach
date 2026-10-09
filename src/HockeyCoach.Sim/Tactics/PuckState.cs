namespace HockeyCoach.Sim.Tactics
{
    /// <summary>Puck state for system rule conditions (<c>when.puckState</c>).</summary>
    public enum PuckState
    {
        /// <summary><c>controlled</c>: someone has the puck.</summary>
        Controlled = 0,

        /// <summary><c>loose</c>: loose puck.</summary>
        Loose = 1,
    }
}
