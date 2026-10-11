namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// tuning.json <c>transitions</c>: the built-in rush (data-schema.md O-7, D-062) and the own-zone regroup challenge
    /// (O-7, D-078 K1).
    /// </summary>
    public sealed class TransitionsConfig
    {
        /// <summary>Creates the config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="rushMaxActions">Most rush actions.</param>
        /// <param name="rushShotMinBaseXg">Offensive-zone nodes with at least this base xG are rush shooting nodes.</param>
        /// <param name="regroupForecheckersAllowed">
        /// Forecheckers (F1-F3) in the own zone that an own-zone regroup tolerates without a breakout check (D-078 K1).
        /// </param>
        public TransitionsConfig(int rushMaxActions, double rushShotMinBaseXg, int regroupForecheckersAllowed)
        {
            RushMaxActions = rushMaxActions;
            RushShotMinBaseXg = rushShotMinBaseXg;
            RegroupForecheckersAllowed = regroupForecheckersAllowed;
        }

        /// <summary>Most rush actions.</summary>
        public int RushMaxActions { get; }

        /// <summary>Base xG threshold of rush shooting nodes.</summary>
        public double RushShotMinBaseXg { get; }

        /// <summary>
        /// An own-zone regroup faces one breakout check when more forecheckers than this are in the zone after the defence's
        /// regroup move (O-7, D-078 K1). 0..3; 3 = never checked.
        /// </summary>
        public int RegroupForecheckersAllowed { get; }
    }
}
