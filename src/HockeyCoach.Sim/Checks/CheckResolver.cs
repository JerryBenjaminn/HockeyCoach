using System;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Random;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// Resolves a binary check: weighs both sides, applies the clamped check formula and draws the outcome.
    /// Consumes exactly one <see cref="IRandom.Chance"/> draw per call.
    /// </summary>
    public static class CheckResolver
    {
        /// <summary>Computes the clamped success probability without drawing.</summary>
        /// <param name="formula">Global formula parameters (k, clamp bounds).</param>
        /// <param name="check">Check definition (p0 and weights).</param>
        /// <param name="participants">Players filling the participant roles named in the check.</param>
        /// <param name="modifier">M, the sum of all modifiers in logit units.</param>
        public static double Probability(CheckFormulaConfig formula, CheckDefinition check, ICheckParticipants participants, double modifier)
        {
            Require(formula, check);
            double h = StatWeighting.Rating(check.Attacker, participants);
            double d = StatWeighting.Rating(check.Defender, participants);
            return CheckFormula.Probability(formula, check.P0, h, d, modifier);
        }

        /// <summary>Resolves the check with one draw from <paramref name="random"/>.</summary>
        /// <param name="formula">Global formula parameters (k, clamp bounds).</param>
        /// <param name="check">Check definition (p0 and weights).</param>
        /// <param name="participants">Players filling the participant roles named in the check.</param>
        /// <param name="modifier">M, the sum of all modifiers in logit units.</param>
        /// <param name="random">The match's random generator.</param>
        public static CheckResult Resolve(CheckFormulaConfig formula, CheckDefinition check, ICheckParticipants participants, double modifier, IRandom random)
        {
            Require(formula, check);
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            double h = StatWeighting.Rating(check.Attacker, participants);
            double d = StatWeighting.Rating(check.Defender, participants);
            double p = CheckFormula.Probability(formula, check.P0, h, d, modifier);
            bool success = random.Chance(p);
            return new CheckResult(check.Id, success, p, h, d, modifier);
        }

        private static void Require(CheckFormulaConfig formula, CheckDefinition check)
        {
            if (formula == null)
            {
                throw new ArgumentNullException(nameof(formula));
            }

            if (check == null)
            {
                throw new ArgumentNullException(nameof(check));
            }
        }
    }
}
