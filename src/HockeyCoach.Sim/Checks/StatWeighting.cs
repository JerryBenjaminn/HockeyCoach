using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Config;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>Computes a side's rating (H or D) as the weighted sum of its participants' stats. Pure function.</summary>
    public static class StatWeighting
    {
        /// <summary>
        /// Returns Σ weight × stat over <paramref name="terms"/>, summed in list order.
        /// With weights summing to 1 the result stays on the 1–20 stat scale.
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
                sum += term.Weight * participants.Get(term.Participant).GetStat(term.Stat);
            }

            return sum;
        }
    }
}
