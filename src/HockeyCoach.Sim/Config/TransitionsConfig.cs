namespace HockeyCoach.Sim.Config
{
    /// <summary>tuning.json <c>transitions</c>: the built-in rush (data-schema.md O-7, D-062).</summary>
    public sealed class TransitionsConfig
    {
        /// <summary>Creates the config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="rushMaxActions">Most rush actions.</param>
        /// <param name="rushShotMinBaseXg">Offensive-zone nodes with at least this base xG are rush shooting nodes.</param>
        public TransitionsConfig(int rushMaxActions, double rushShotMinBaseXg)
        {
            RushMaxActions = rushMaxActions;
            RushShotMinBaseXg = rushShotMinBaseXg;
        }

        /// <summary>Most rush actions.</summary>
        public int RushMaxActions { get; }

        /// <summary>Base xG threshold of rush shooting nodes.</summary>
        public double RushShotMinBaseXg { get; }
    }
}
