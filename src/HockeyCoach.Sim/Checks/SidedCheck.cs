using System;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Random;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// Resolves a check whose two sides use the same participant role names (e.g. <c>centre</c> in <c>faceoff</c>,
    /// <c>participant</c> in <c>loosePuck</c>), so each side gets its own participants. Same formula and clamp as
    /// <see cref="CheckResolver"/>; one <see cref="IRandom.Chance"/> draw per resolve.
    /// </summary>
    public static class SidedCheck
    {
        /// <summary>Clamped success probability (attacker side succeeds) without drawing.</summary>
        public static double Probability(CheckFormulaConfig formula, CheckDefinition check, ICheckParticipants attacker, ICheckParticipants defender, double modifier)
        {
            Ratings(formula, check, attacker, defender, out double h, out double d);
            return CheckFormula.Probability(formula, check.P0.Value, h, d, modifier);
        }

        /// <summary>Resolves the check with one draw.</summary>
        public static CheckResult Resolve(CheckFormulaConfig formula, CheckDefinition check, ICheckParticipants attacker, ICheckParticipants defender, double modifier, IRandom random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            Ratings(formula, check, attacker, defender, out double h, out double d);
            double p = CheckFormula.Probability(formula, check.P0.Value, h, d, modifier);
            return new CheckResult(check.Id, random.Chance(p), p, h, d, modifier);
        }

        private static void Ratings(CheckFormulaConfig formula, CheckDefinition check, ICheckParticipants attacker, ICheckParticipants defender, out double h, out double d)
        {
            if (formula == null)
            {
                throw new ArgumentNullException(nameof(formula));
            }

            if (check == null)
            {
                throw new ArgumentNullException(nameof(check));
            }

            if (check.Kind != CheckKind.TwoSided || !check.P0.HasValue)
            {
                throw new InvalidOperationException("Check " + check.Id + " is not a two-sided check with p0.");
            }

            h = StatWeighting.Rating(check.Attacker, attacker);
            d = StatWeighting.Rating(check.Defender, defender);
        }
    }
}
