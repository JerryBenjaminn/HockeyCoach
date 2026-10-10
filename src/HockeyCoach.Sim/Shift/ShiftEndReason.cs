namespace HockeyCoach.Sim.Shift
{
    /// <summary>Why a shift segment ended: a stoppage (O-13) or the anti-stall cap (D-043).</summary>
    public enum ShiftEndReason
    {
        /// <summary>A goal was scored.</summary>
        Goal = 0,

        /// <summary>The goalie froze the puck.</summary>
        GoalieFreeze = 1,

        /// <summary>The anti-stall cap was reached (a stalled shift; should not happen).</summary>
        StepCap = 2,

        /// <summary>A missed shot left the rink (D-063).</summary>
        OutOfPlay = 3,

        /// <summary>The period ended (D-064).</summary>
        PeriodEnd = 4,
    }
}
