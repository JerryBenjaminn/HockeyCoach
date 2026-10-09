namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// Off-side position penalty (D-024, docs/data-schema.md: Pelaajat, pelipaikat ja ketjut). The configured logit
    /// (negative) is applied once per check side that has at least one off-side player: added to M for the attacker
    /// side, subtracted for the defender side, so it always hurts the player's own side. Pure function.
    /// Wiring into the shift (who plays which slot) comes with Milestone 2.
    /// </summary>
    public static class OffSideModifier
    {
        /// <summary>Contribution to M.</summary>
        /// <param name="offSideCheckModifier">tuning.json <c>positions.offSideCheckModifier</c>.</param>
        /// <param name="attackerSideHasOffSidePlayer">Whether any attacker-side participant is off-side.</param>
        /// <param name="defenderSideHasOffSidePlayer">Whether any defender-side participant is off-side.</param>
        public static double Contribution(double offSideCheckModifier, bool attackerSideHasOffSidePlayer, bool defenderSideHasOffSidePlayer)
        {
            double m = 0.0;
            if (attackerSideHasOffSidePlayer)
            {
                m += offSideCheckModifier;
            }

            if (defenderSideHasOffSidePlayer)
            {
                m -= offSideCheckModifier;
            }

            return m;
        }
    }
}
