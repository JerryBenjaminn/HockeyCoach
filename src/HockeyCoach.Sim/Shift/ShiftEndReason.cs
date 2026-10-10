namespace HockeyCoach.Sim.Shift
{
    /// <summary>Why a shift ended (D-043: in milestone 2 only a stoppage ends a shift).</summary>
    public enum ShiftEndReason
    {
        /// <summary>A goal was scored.</summary>
        Goal = 0,

        /// <summary>The goalie froze the puck.</summary>
        GoalieFreeze = 1,

        /// <summary>The anti-stall cap was reached (a stalled shift; should not happen).</summary>
        StepCap = 2,
    }
}
