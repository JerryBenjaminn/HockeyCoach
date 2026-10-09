namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Global parameters of the check formula (tuning.json <c>checkFormula</c>):
    /// P = clamp(sigmoid(logit(p0) + k (H - D) + M), min, max).
    /// </summary>
    public sealed class CheckFormulaConfig
    {
        /// <summary>Creates the formula parameters. Use <see cref="TuningValidator"/> to check them.</summary>
        /// <param name="k">Sensitivity: logit change per weighted stat point of difference.</param>
        /// <param name="minProbability">Lower clamp of the success probability (D-014).</param>
        /// <param name="maxProbability">Upper clamp of the success probability (D-014).</param>
        public CheckFormulaConfig(double k, double minProbability, double maxProbability)
        {
            K = k;
            MinProbability = minProbability;
            MaxProbability = maxProbability;
        }

        /// <summary>Sensitivity: logit change per weighted stat point of difference.</summary>
        public double K { get; }

        /// <summary>Lower clamp of the success probability.</summary>
        public double MinProbability { get; }

        /// <summary>Upper clamp of the success probability.</summary>
        public double MaxProbability { get; }
    }
}
