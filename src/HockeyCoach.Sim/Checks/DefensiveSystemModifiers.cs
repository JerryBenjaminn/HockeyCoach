namespace HockeyCoach.Sim.Checks
{
    /// <summary>
    /// The defensive-system modifiers of data-schema.md M-9 (D-078 K4), computed from positions rather than the system id.
    /// Both are logit modifiers in the attacker's favour (M of the check formula).
    /// </summary>
    public static class DefensiveSystemModifiers
    {
        /// <summary>
        /// <c>zoneEntryCarry</c>: <c>strengthDifferencePerPlayer</c> × (N − M), where N and M are the M-8 numbers of the
        /// entry (the same ones logged in the controlled zone entry event). Not bounded; the check clamp applies.
        /// </summary>
        /// <param name="perPlayer"><c>checks.zoneEntryCarry.modifiers.strengthDifferencePerPlayer</c>.</param>
        /// <param name="attackers">N: the carrier plus attacking skaters ahead of the puck (M-8, D-084).</param>
        /// <param name="defenders">M: defending skaters between the puck and their own goal (M-8).</param>
        public static double StrengthDifference(double perPlayer, int attackers, int defenders)
        {
            return perPlayer * (attackers - defenders);
        }

        /// <summary>
        /// <c>breakout</c>: <c>perForechecker</c> × f, where f is the number of the defending team's forwards (F1–F3) in the
        /// attacking team's defensive zone, without the M-1 fallback.
        /// </summary>
        /// <param name="perForechecker"><c>checks.breakout.modifiers.perForechecker</c>.</param>
        /// <param name="forecheckers">f, 0..3.</param>
        public static double Forecheckers(double perForechecker, int forecheckers)
        {
            return perForechecker * forecheckers;
        }
    }
}
