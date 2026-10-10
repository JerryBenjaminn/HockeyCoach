using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>Outcome of one shift segment: the log it appended to, why it ended, its goals and the last shot.</summary>
    public sealed class ShiftResult
    {
        internal ShiftResult(
            EventLog log,
            ShiftEndReason endReason,
            int homeGoals,
            int awayGoals,
            double endTime,
            int steps,
            bool hasLastShot,
            TeamSide lastShotTeam,
            GridPoint lastShotNode)
        {
            Log = log;
            EndReason = endReason;
            HomeGoals = homeGoals;
            AwayGoals = awayGoals;
            EndTime = endTime;
            Steps = steps;
            HasLastShot = hasLastShot;
            LastShotTeam = lastShotTeam;
            LastShotNode = lastShotNode;
        }

        /// <summary>The event log (in a match, the whole match's log).</summary>
        public EventLog Log { get; }

        /// <summary>Why the segment ended.</summary>
        public ShiftEndReason EndReason { get; }

        /// <summary>Home goals in this segment.</summary>
        public int HomeGoals { get; }

        /// <summary>Away goals in this segment.</summary>
        public int AwayGoals { get; }

        /// <summary>Period time at the end.</summary>
        public double EndTime { get; }

        /// <summary>Simulation steps taken.</summary>
        public int Steps { get; }

        /// <summary>Whether a shot was taken in this segment.</summary>
        public bool HasLastShot { get; }

        /// <summary>The team of the last shot.</summary>
        public TeamSide LastShotTeam { get; }

        /// <summary>The last shooter's node (home view), for the next faceoff spot (O-3).</summary>
        public GridPoint LastShotNode { get; }
    }
}
