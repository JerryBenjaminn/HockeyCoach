namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Global parameters of the check formula (tuning.json <c>checkFormula</c>):
    /// P = clamp(sigmoid(logit(p0) + k (H - D) + M), min, max). The clamp applies to checks only,
    /// not to the shot goal probability or xG (D-014), which use <see cref="ShotConfig"/> bounds.
    /// </summary>
    public sealed class CheckFormulaConfig
    {
        /// <summary>Creates the formula parameters. Use <see cref="TuningValidator"/> to check them.</summary>
        /// <param name="k">Sensitivity: logit change per weighted stat point of difference.</param>
        /// <param name="minProbability">Lower clamp of check success probability (D-014).</param>
        /// <param name="maxProbability">Upper clamp of check success probability (D-014).</param>
        /// <param name="referenceValue">Rating of the missing side in one-sided checks and zero point of ...PerPoint modifiers (D-019).</param>
        public CheckFormulaConfig(double k, double minProbability, double maxProbability, double referenceValue)
        {
            K = k;
            MinProbability = minProbability;
            MaxProbability = maxProbability;
            ReferenceValue = referenceValue;
        }

        /// <summary>Sensitivity: logit change per weighted stat point of difference.</summary>
        public double K { get; }

        /// <summary>Lower clamp of check success probability.</summary>
        public double MinProbability { get; }

        /// <summary>Upper clamp of check success probability.</summary>
        public double MaxProbability { get; }

        /// <summary>Rating of the missing side in one-sided checks (D-019).</summary>
        public double ReferenceValue { get; }
    }
}
