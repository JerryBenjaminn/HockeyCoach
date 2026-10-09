using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>Maps the participant role names of a check (e.g. <c>passer</c>, <c>goalie</c>) to players.</summary>
    public interface ICheckParticipants
    {
        /// <summary>
        /// Returns the player filling the role. Throws <see cref="System.ArgumentException"/> if the role is not assigned.
        /// </summary>
        IStatProvider Get(string participant);
    }
}
