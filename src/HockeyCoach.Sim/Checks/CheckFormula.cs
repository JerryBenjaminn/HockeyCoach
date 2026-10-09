using HockeyCoach.Sim.Config;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// The check formula of docs/stats-and-checks.md (Tarkistukset):
    /// P = 1 / (1 + e^-(a + k (H - D) + M)), a = ln(p0 / (1 - p0)), then clamped to [min, max] (D-014).
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

        /// <summary>Success probability clamped to [<see cref="CheckFormulaConfig.MinProbability"/>, <see cref="CheckFormulaConfig.MaxProbability"/>].</summary>
        public static double Probability(CheckFormulaConfig formula, double p0, double attackerRating, double defenderRating, double modifier)
        {
            double raw = RawProbability(p0, formula.K, attackerRating, defenderRating, modifier);
            return CheckMath.Clamp(raw, formula.MinProbability, formula.MaxProbability);
        }
    }
}
