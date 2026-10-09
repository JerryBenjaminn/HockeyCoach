using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// Simple role-name to player map for a single check. Lookup only; never enumerated, so ordering does not matter.
    /// </summary>
    public sealed class CheckParticipants : ICheckParticipants
    {
        private readonly Dictionary<string, IStatProvider> _byRole = new Dictionary<string, IStatProvider>(StringComparer.Ordinal);

        /// <summary>Assigns a player to a role. Returns this instance for chaining.</summary>
        public CheckParticipants With(string participant, IStatProvider player)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            _byRole[participant] = player ?? throw new ArgumentNullException(nameof(player));
            return this;
        }

        /// <inheritdoc />
        public IStatProvider Get(string participant)
        {
            if (participant != null && _byRole.TryGetValue(participant, out IStatProvider player))
            {
                return player;
            }

            throw new ArgumentException("No player assigned to check participant '" + participant + "'.", nameof(participant));
        }
    }
}
