using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// Maps the participant role names of a check (e.g. <c>passer</c>, <c>goalie</c>, <c>forecheckers</c>) to players.
    /// A role may be filled by several players; their stats are averaged (D-026).
    /// </summary>
    public interface ICheckParticipants
    {
        /// <summary>
        /// Returns the players filling the role (at least one), in a stable order.
        /// Throws <see cref="System.ArgumentException"/> if the role is not assigned.
        /// </summary>
        IReadOnlyList<IStatProvider> Get(string participant);
    }
}
