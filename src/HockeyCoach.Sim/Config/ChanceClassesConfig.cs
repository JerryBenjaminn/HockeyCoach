namespace HockeyCoach.Sim.Config
{
    /// <summary>xG thresholds of the chance classes from tuning.json <c>chanceClasses</c> (Q-004).</summary>
    public sealed class ChanceClassesConfig
    {
        /// <summary>Creates the config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="topMinXg">Lowest xG of a top chance.</param>
        /// <param name="goodMinXg">Lowest xG of a good chance.</param>
        /// <param name="moderateMinXg">Lowest xG of a moderate chance.</param>
        public ChanceClassesConfig(double topMinXg, double goodMinXg, double moderateMinXg)
        {
            TopMinXg = topMinXg;
            GoodMinXg = goodMinXg;
            ModerateMinXg = moderateMinXg;
        }

        /// <summary>Lowest xG of a top chance.</summary>
        public double TopMinXg { get; }

        /// <summary>Lowest xG of a good chance.</summary>
        public double GoodMinXg { get; }

        /// <summary>Lowest xG of a moderate chance.</summary>
        public double ModerateMinXg { get; }
    }
}
