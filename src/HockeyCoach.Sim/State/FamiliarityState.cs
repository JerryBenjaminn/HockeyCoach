using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Events;

namespace HockeyCoach.Sim.State
{
    /// <summary>
    /// Play uses per team and play id for one match (O-11, D-066). A play and its mirror image share the id. Counts are
    /// real numbers because they are multiplied at every intermission.
    /// </summary>
    public sealed class FamiliarityState
    {
        private readonly SortedDictionary<string, double>[] _uses =
        {
            new SortedDictionary<string, double>(StringComparer.Ordinal), new SortedDictionary<string, double>(StringComparer.Ordinal),
        };

        /// <summary>Counts a play start and returns the uses including it.</summary>
        public double Start(TeamSide team, string playId)
        {
            if (playId == null)
            {
                throw new ArgumentNullException(nameof(playId));
            }

            double uses = Uses(team, playId) + 1.0;
            _uses[(int)team][playId] = uses;
            return uses;
        }

        /// <summary>Uses of a play so far (0 if never started).</summary>
        public double Uses(TeamSide team, string playId)
        {
            return playId != null && _uses[(int)team].TryGetValue(playId, out double uses) ? uses : 0.0;
        }

        /// <summary>All uses of a team, in ordinal play id order.</summary>
        public IReadOnlyDictionary<string, double> UsesOf(TeamSide team)
        {
            return _uses[(int)team];
        }

        /// <summary>Multiplies every counter (intermission, O-11). No rounding.</summary>
        public void Multiply(double factor)
        {
            foreach (SortedDictionary<string, double> team in _uses)
            {
                var ids = new List<string>(team.Keys);
                foreach (string id in ids)
                {
                    team[id] *= factor;
                }
            }
        }
    }
}
