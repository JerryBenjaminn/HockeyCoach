using System;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Random;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// The shot goal check: same formula as other checks with p0 = baseXg of the shooting node's xG zone, but the
    /// result (goal probability, also reported as xG) is clamped to <see cref="ShotConfig.MinProbability"/> ..
    /// <see cref="ShotConfig.MaxProbability"/>, not to the check clamp (D-014). Block and on-target steps are
    /// separate (Q-010) and belong to the shift logic.
    /// </summary>
    public static class ShotResolver
    {
        /// <summary>Goal probability (= xG of the attempt at this step) without drawing.</summary>
        /// <param name="formula">Global formula parameters; only <see cref="CheckFormulaConfig.K"/> is used.</param>
        /// <param name="shot">Shot config.</param>
        /// <param name="xgZone">xG zone of the shooting node in the shooter's view.</param>
        /// <param name="participants">Players for the roles <c>shooter</c> and <c>goalie</c>.</param>
        /// <param name="modifier">M, the sum of all modifiers in logit units.</param>
        public static double GoalProbability(CheckFormulaConfig formula, ShotConfig shot, string xgZone, ICheckParticipants participants, double modifier)
        {
            Require(formula, shot);
            CheckDefinition check = shot.ForXgZone(xgZone);
            double h = StatWeighting.Rating(check.Attacker, participants);
            double d = StatWeighting.Rating(check.Defender, participants);
            return CheckFormula.Probability(check.P0.Value, formula.K, h, d, modifier, shot.MinProbability, shot.MaxProbability);
        }

        /// <summary>Resolves the goal check with one draw from <paramref name="random"/>. Success = goal.</summary>
        public static CheckResult Resolve(CheckFormulaConfig formula, ShotConfig shot, string xgZone, ICheckParticipants participants, double modifier, IRandom random)
        {
            Require(formula, shot);
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            CheckDefinition check = shot.ForXgZone(xgZone);
            double h = StatWeighting.Rating(check.Attacker, participants);
            double d = StatWeighting.Rating(check.Defender, participants);
            double p = CheckFormula.Probability(check.P0.Value, formula.K, h, d, modifier, shot.MinProbability, shot.MaxProbability);
            bool goal = random.Chance(p);
            return new CheckResult(ShotConfig.Id, goal, p, h, d, modifier);
        }

        private static void Require(CheckFormulaConfig formula, ShotConfig shot)
        {
            if (formula == null)
            {
                throw new ArgumentNullException(nameof(formula));
            }

            if (shot == null)
            {
                throw new ArgumentNullException(nameof(shot));
            }
        }
    }
}
