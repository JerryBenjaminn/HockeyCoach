using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.AI
{
    /// <summary>
    /// Rule-based lines (data-schema.md AI-2, S-L), the same rule at stoppages and on the fly, trio first then pair:
    /// an ineligible unit stays; otherwise the best bench unit b is the one with the highest estimated energy (ties: next
    /// in rotation order from the current, or the lowest index when no unit is on yet); change to b when the current unit's
    /// estimate is below <c>ai.rulebased.changeEnergyThreshold</c> or b is at least <c>ai.rulebased.freshnessMargin</c>
    /// fresher. No random draws.
    /// </summary>
    public sealed class RuleLines : ILinePolicy
    {
        private readonly AiRulebasedConfig _config;
        private readonly EnergyEstimator _estimator;

        /// <summary>Creates the policy.</summary>
        public RuleLines(TuningConfig tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            _config = tuning.Ai.Rulebased;
            _estimator = new EnergyEstimator(tuning);
        }

        /// <inheritdoc />
        public LineChoice AtStoppage(CoachView view)
        {
            return Choose(view);
        }

        /// <inheritdoc />
        public LineChoice OnTheFly(CoachView view)
        {
            return Choose(view);
        }

        private LineChoice Choose(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            IReadOnlyDictionary<int, double> estimate = _estimator.Estimate(view);
            var trios = new List<IReadOnlyList<Skater>>();
            foreach (ForwardLine line in view.Team.ForwardLines)
            {
                trios.Add(line.Skaters);
            }

            var pairs = new List<IReadOnlyList<Skater>>();
            foreach (DefencePair pair in view.Team.DefencePairs)
            {
                pairs.Add(pair.Skaters);
            }

            int forwards = view.CanChangeForwards ? Pick(view.ForwardIndex, Energies(estimate, trios)) : view.ForwardIndex;
            int defence = view.CanChangeDefence ? Pick(view.PairIndex, Energies(estimate, pairs)) : view.PairIndex;
            return new LineChoice(forwards, defence);
        }

        /// <summary>S-L steps 2 and 3 for one unit type.</summary>
        /// <param name="current">Unit on the ice, −1 before the first faceoff.</param>
        /// <param name="energies">Estimated energy per unit.</param>
        public int Pick(int current, IReadOnlyList<double> energies)
        {
            if (energies == null || energies.Count == 0)
            {
                throw new ArgumentException("No units.", nameof(energies));
            }

            int n = energies.Count;
            int best = -1;
            if (current < 0)
            {
                for (int i = 0; i < n; i++)
                {
                    if (best < 0 || energies[i] > energies[best])
                    {
                        best = i;
                    }
                }

                return best;
            }

            for (int k = 1; k < n; k++)
            {
                int i = (current + k) % n;
                if (best < 0 || energies[i] > energies[best])
                {
                    best = i;
                }
            }

            if (best < 0)
            {
                return current;
            }

            double now = energies[current];
            return now < _config.ChangeEnergyThreshold || energies[best] - now >= _config.FreshnessMargin ? best : current;
        }

        private static double[] Energies(IReadOnlyDictionary<int, double> estimate, List<IReadOnlyList<Skater>> units)
        {
            var result = new double[units.Count];
            for (int i = 0; i < units.Count; i++)
            {
                result[i] = EnergyEstimator.UnitEnergy(estimate, units[i]);
            }

            return result;
        }
    }
}
