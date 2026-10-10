using HockeyCoach.Sim.Events;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>The outcome of one simulated shift.</summary>
    public sealed class ShiftResult
    {
        internal ShiftResult(EventLog log, ShiftEndReason endReason, int homeGoals, int awayGoals, double endTime, int steps)
        {
            Log = log;
            EndReason = endReason;
            HomeGoals = homeGoals;
            AwayGoals = awayGoals;
            EndTime = endTime;
            Steps = steps;
        }

        /// <summary>The event log.</summary>
        public EventLog Log { get; }

        /// <summary>Why the shift ended.</summary>
        public ShiftEndReason EndReason { get; }

        /// <summary>Goals by the home team.</summary>
        public int HomeGoals { get; }

        /// <summary>Goals by the away team.</summary>
        public int AwayGoals { get; }

        /// <summary>Game seconds elapsed in the period when the shift ended.</summary>
        public double EndTime { get; }

        /// <summary>Simulation steps taken.</summary>
        public int Steps { get; }
    }
}
