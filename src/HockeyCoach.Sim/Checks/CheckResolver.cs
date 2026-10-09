using System;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Random;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// Resolves a check: weighs both sides, applies the clamped check formula and draws the outcome.
    /// Consumes exactly one <see cref="IRandom.Chance"/> draw per <see cref="Resolve"/> call.
    /// </summary>
    public static class CheckResolver
    {
        /// <summary>
        /// Returns H and D for the check. In a one-sided check the missing side is
        /// <see cref="CheckFormulaConfig.ReferenceValue"/> (D-019).
        /// </summary>
        public static void Ratings(CheckFormulaConfig formula, CheckDefinition check, ICheckParticipants participants, out double attackerRating, out double defenderRating)
        {
            Require(formula, check);
            switch (check.Kind)
            {
                case CheckKind.TwoSided:
                    attackerRating = StatWeighting.Rating(check.Attacker, participants);
                    defenderRating = StatWeighting.Rating(check.Defender, participants);
                    return;

                case CheckKind.OneSided:
                    if (check.Side == CheckSide.Attacker)
                    {
                        attackerRating = StatWeighting.Rating(check.Attacker, participants);
                        defenderRating = formula.ReferenceValue;
                    }
                    else if (check.Side == CheckSide.Defender)
                    {
                        attackerRating = formula.ReferenceValue;
                        defenderRating = StatWeighting.Rating(check.Defender, participants);
                    }
                    else
                    {
                        throw new InvalidOperationException("One-sided check " + check.Id + " has no side.");
                    }

                    return;

                default:
                    throw new InvalidOperationException("Check " + check.Id + " is a " + check.Kind + " and cannot be rolled.");
            }
        }

        /// <summary>Computes the clamped success probability without drawing.</summary>
        /// <param name="formula">Global formula parameters (k, clamp bounds, reference value).</param>
        /// <param name="check">Check definition (kind, p0 and weights).</param>
        /// <param name="participants">Players filling the participant roles named in the check.</param>
        /// <param name="modifier">M, the sum of all modifiers in logit units.</param>
        public static double Probability(CheckFormulaConfig formula, CheckDefinition check, ICheckParticipants participants, double modifier)
        {
            Ratings(formula, check, participants, out double h, out double d);
            return CheckFormula.Probability(formula, BaseRate(check), h, d, modifier);
        }

        /// <summary>Resolves the check with one draw from <paramref name="random"/>.</summary>
        /// <param name="formula">Global formula parameters (k, clamp bounds, reference value).</param>
        /// <param name="check">Check definition (kind, p0 and weights).</param>
        /// <param name="participants">Players filling the participant roles named in the check.</param>
        /// <param name="modifier">M, the sum of all modifiers in logit units.</param>
        /// <param name="random">The match's random generator.</param>
        public static CheckResult Resolve(CheckFormulaConfig formula, CheckDefinition check, ICheckParticipants participants, double modifier, IRandom random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            Ratings(formula, check, participants, out double h, out double d);
            double p = CheckFormula.Probability(formula, BaseRate(check), h, d, modifier);
            bool success = random.Chance(p);
            return new CheckResult(check.Id, success, p, h, d, modifier);
        }

        private static double BaseRate(CheckDefinition check)
        {
            if (!check.P0.HasValue)
            {
                throw new InvalidOperationException("Check " + check.Id + " has no p0.");
            }

            return check.P0.Value;
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
