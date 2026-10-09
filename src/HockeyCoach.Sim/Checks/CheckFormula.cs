using HockeyCoach.Sim.Config;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// The check formula of docs/stats-and-checks.md (Tarkistukset):
    /// P = 1 / (1 + e^-(a + k (H - D) + M)), a = ln(p0 / (1 - p0)), then clamped (D-014).
    /// Pure functions.
    /// </summary>
    public static class CheckFormula
    {
        /// <summary>Unclamped success probability.</summary>
        /// <param name="p0">Base rate: the probability when H = D and M = 0. Must be in (0, 1).</param>
        /// <param name="k">Sensitivity per weighted stat point.</param>
        /// <param name="attackerRating">H, the attacker's weighted stat sum.</param>
        /// <param name="defenderRating">D, the defender's weighted stat sum.</param>
        /// <param name="modifier">M, the sum of modifiers in logit units.</param>
        public static double RawProbability(double p0, double k, double attackerRating, double defenderRating, double modifier)
        {
            double logit = CheckMath.Logit(p0) + k * (attackerRating - defenderRating) + modifier;
            return CheckMath.Sigmoid(logit);
        }

        /// <summary>Success probability clamped to [<paramref name="min"/>, <paramref name="max"/>].</summary>
        public static double Probability(double p0, double k, double attackerRating, double defenderRating, double modifier, double min, double max)
        {
            return CheckMath.Clamp(RawProbability(p0, k, attackerRating, defenderRating, modifier), min, max);
        }

        /// <summary>
        /// Check success probability clamped to the check bounds
        /// [<see cref="CheckFormulaConfig.MinProbability"/>, <see cref="CheckFormulaConfig.MaxProbability"/>].
        /// Not for the shot goal probability, which uses <see cref="ShotConfig"/> bounds (D-014).
        /// </summary>
        public static double Probability(CheckFormulaConfig formula, double p0, double attackerRating, double defenderRating, double modifier)
        {
            return Probability(p0, formula.K, attackerRating, defenderRating, modifier, formula.MinProbability, formula.MaxProbability);
        }
    }
}
