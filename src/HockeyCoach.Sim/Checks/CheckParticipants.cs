using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// Simple role-name to players map for a single check. Lookup only; never enumerated, so ordering does not matter.
    /// </summary>
    public sealed class CheckParticipants : ICheckParticipants
    {
        private readonly Dictionary<string, IStatProvider[]> _byRole = new Dictionary<string, IStatProvider[]>(StringComparer.Ordinal);

        /// <summary>Assigns one or more players to a role. Returns this instance for chaining.</summary>
        public CheckParticipants With(string participant, params IStatProvider[] players)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            if (players == null || players.Length == 0)
            {
                throw new ArgumentException("Role " + participant + " needs at least one player.", nameof(players));
            }

            var copy = new IStatProvider[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                copy[i] = players[i] ?? throw new ArgumentException("Null player in role " + participant + ".", nameof(players));
            }

            _byRole[participant] = copy;
            return this;
        }

        /// <inheritdoc />
        public IReadOnlyList<IStatProvider> Get(string participant)
        {
            if (participant != null && _byRole.TryGetValue(participant, out IStatProvider[] players))
            {
                return players;
            }

            throw new ArgumentException("No player assigned to check participant " + participant + ".", nameof(participant));
        }
    }
}
