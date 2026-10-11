using HockeyCoach.Sim.Events;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// Receives every resolved check for reports (check success, energy at checks). Read-only: an observer must not change
    /// the match, and the simulation draws the same numbers with or without one.
    /// </summary>
    public interface ICheckObserver
    {
        /// <summary>Called once per resolved check, before the participation cost is paid.</summary>
        void OnCheck(CheckObservation observation);
    }

    /// <summary>One resolved check as seen by an <see cref="ICheckObserver"/>.</summary>
    public sealed class CheckObservation
    {
        /// <summary>Creates the observation.</summary>
        /// <param name="period">Period.</param>
        /// <param name="time">Period time.</param>
        /// <param name="checkId">Check id (<c>pass</c>, <c>breakout</c>, <c>zoneEntryCarry</c>, <c>deke</c>, <c>block</c>, <c>shot</c>, <c>rebound</c>, <c>faceoff</c>, <c>loosePuck</c>).</param>
        /// <param name="attackingTeam">The attacking side of the check (faceoff: home).</param>
        /// <param name="success">The attacking side succeeded (block: the shot got through; rebound: the goalie did not control it).</param>
        /// <param name="probability">Success probability of the attacking side.</param>
        /// <param name="attackerEnergy">Mean energy of the attacking participants, NaN when none.</param>
        /// <param name="defenderEnergy">Mean energy of the defending participants (goalie for shot and rebound), NaN when none.</param>
        public CheckObservation(int period, double time, string checkId, TeamSide attackingTeam, bool success, double probability, double attackerEnergy, double defenderEnergy)
        {
            Period = period;
            Time = time;
            CheckId = checkId;
            AttackingTeam = attackingTeam;
            Success = success;
            Probability = probability;
            AttackerEnergy = attackerEnergy;
            DefenderEnergy = defenderEnergy;
        }

        /// <summary>Period.</summary>
        public int Period { get; }

        /// <summary>Period time.</summary>
        public double Time { get; }

        /// <summary>Check id.</summary>
        public string CheckId { get; }

        /// <summary>The attacking side of the check.</summary>
        public TeamSide AttackingTeam { get; }

        /// <summary>The attacking side succeeded.</summary>
        public bool Success { get; }

        /// <summary>Success probability of the attacking side.</summary>
        public double Probability { get; }

        /// <summary>Mean energy of the attacking participants, NaN when none.</summary>
        public double AttackerEnergy { get; }

        /// <summary>Mean energy of the defending participants, NaN when none.</summary>
        public double DefenderEnergy { get; }
    }
}
