using System;

namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// Pure formulas of the milestone 3 states (data-schema.md O-5, O-6, O-9, O-11): energy, organization, pressure and
    /// familiarity, and their check modifiers. Plain numbers in and out; all math through <see cref="CheckMath"/>.
    /// </summary>
    public static class StateDynamics
    {
        /// <summary>
        /// A stat-based reduction factor: 1 − slope × (stat − reference). Endurance factor f (O-5) and the goalie's mental
        /// toughness factor g (O-5, O-9).
        /// </summary>
        public static double ReductionFactor(double slope, double stat, double reference)
        {
            return 1.0 - (slope * (stat - reference));
        }

        /// <summary>Bench recovery in closed form (O-5): 1 − (1 − e) × exp(−rate × seconds), clamped to 0..1.</summary>
        public static double BenchRecovered(double energy, double seconds, double rate)
        {
            if (seconds <= 0.0)
            {
                return Clamp01(energy);
            }

            return Clamp01(1.0 - ((1.0 - energy) * CheckMath.Exp(-rate * seconds)));
        }

        /// <summary>One side's energy term of a check (O-5): cz × (1 − mean energy). The attacker term adds, the defender term subtracts.</summary>
        public static double EnergyTerm(double checkModifierAtZero, double meanEnergy)
        {
            return checkModifierAtZero * (1.0 - meanEnergy);
        }

        /// <summary>
        /// The energy modifier of a check (O-5): cz × (1 − Ē_attacker) − cz × (1 − Ē_defender). A missing side
        /// (<see cref="double.NaN"/>) contributes 0.
        /// </summary>
        public static double EnergyModifier(double checkModifierAtZero, double meanAttacker, double meanDefender)
        {
            double attacker = double.IsNaN(meanAttacker) ? 0.0 : EnergyTerm(checkModifierAtZero, meanAttacker);
            double defender = double.IsNaN(meanDefender) ? 0.0 : EnergyTerm(checkModifierAtZero, meanDefender);
            return attacker - defender;
        }

        /// <summary>
        /// Organization after a turnover (O-6): max(0, min(O, 1 − drop × factor) − perCommitted × committed).
        /// </summary>
        public static double OrganizationAfterTurnover(double organization, double drop, double factor, double perCommitted, int committed)
        {
            return Math.Max(0.0, Math.Min(organization, 1.0 - (drop * factor)) - (perCommitted * committed));
        }

        /// <summary>Organization recovery per second (O-6): rate × (1 + perPoint × (S̄ − reference)).</summary>
        public static double OrganizationRecoveryPerSecond(double rate, double perPoint, double meanStat, double reference)
        {
            return rate * (1.0 + (perPoint * (meanStat - reference)));
        }

        /// <summary>The organization modifier (O-6): coefficient × (1 − O_defender), in the attacker's favour.</summary>
        public static double OrganizationModifier(double coefficient, double defenderOrganization)
        {
            return coefficient * (1.0 - defenderOrganization);
        }

        /// <summary>The shot's pressure modifier (O-9): coefficient × P_shooter × g_goalie.</summary>
        public static double PressureModifier(double coefficient, double pressure, double goalieFactor)
        {
            return coefficient * pressure * goalieFactor;
        }

        /// <summary>Familiarity penalty (O-11): min(max, perRepeat × max(0, uses − freeUses)), uses including the current start.</summary>
        public static double FamiliarityPenalty(double uses, double freeUses, double perRepeat, double maxPenalty)
        {
            return Math.Min(maxPenalty, perRepeat * Math.Max(0.0, uses - freeUses));
        }

        /// <summary>Clamps to 0..1.</summary>
        public static double Clamp01(double value)
        {
            return CheckMath.Clamp(value, 0.0, 1.0);
        }
    }
}
