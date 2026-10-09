namespace HockeyCoach.Sim.Checks
{
    /// <summary>Outcome of one resolved check, with the inputs needed to explain it.</summary>
    public sealed class CheckResult
    {
        /// <summary>Creates a result.</summary>
        public CheckResult(string checkId, bool success, double probability, double attackerRating, double defenderRating, double modifier)
        {
            CheckId = checkId;
            Success = success;
            Probability = probability;
            AttackerRating = attackerRating;
            DefenderRating = defenderRating;
            Modifier = modifier;
        }

        /// <summary>Id of the check definition.</summary>
        public string CheckId { get; }

        /// <summary>Whether the attacking side succeeded.</summary>
        public bool Success { get; }

        /// <summary>Clamped success probability used for the draw.</summary>
        public double Probability { get; }

        /// <summary>H, the attacker's weighted stat sum.</summary>
        public double AttackerRating { get; }

        /// <summary>D, the defender's weighted stat sum.</summary>
        public double DefenderRating { get; }

        /// <summary>M, the modifier sum in logit units.</summary>
        public double Modifier { get; }
    }
}
