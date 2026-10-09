using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>Computes a side's rating (H or D) as the weighted sum of its participants' stats. Pure function.</summary>
    public static class StatWeighting
    {
        /// <summary>
        /// Returns Σ weight × stat over <paramref name="terms"/>, summed in list order. When a role has several
        /// players, the stat is the arithmetic mean of their values, taken before weighting (D-026), so the role's
        /// share does not grow with the number of players.
        /// </summary>
        public static double Rating(IReadOnlyList<WeightTerm> terms, ICheckParticipants participants)
        {
            if (terms == null)
            {
                throw new ArgumentNullException(nameof(terms));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            double sum = 0.0;
            for (int i = 0; i < terms.Count; i++)
            {
                WeightTerm term = terms[i];
                sum += term.Weight * MeanStat(participants.Get(term.Participant), term.Stat, term.Participant);
            }

            return sum;
        }

        /// <summary>Arithmetic mean of one stat over the players of a role.</summary>
        public static double MeanStat(IReadOnlyList<IStatProvider> players, StatRef stat, string participant)
        {
            if (players == null || players.Count == 0)
            {
                throw new ArgumentException("No players in role " + participant + ".", nameof(players));
            }

            long total = 0;
            for (int i = 0; i < players.Count; i++)
            {
                total += players[i].GetStat(stat);
            }

            return (double)total / players.Count;
        }
    }
}
