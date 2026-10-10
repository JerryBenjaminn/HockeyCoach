namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// The shift clock. <see cref="Spend(double)"/> is the only place where game time moves, so per-second effects and
    /// the period boundary (milestone 3) have one home.
    /// </summary>
    internal sealed partial class ShiftRun
    {
        /// <summary>Spends the time of one action (<c>time.secondsPerAction.&lt;key&gt;</c>).</summary>
        private void Spend(string actionKey)
        {
            Spend(_tuning.Time.GetSecondsPerAction(actionKey));
        }

        /// <summary>Advances the clock by <paramref name="seconds"/>.</summary>
        private void Spend(double seconds)
        {
            _time += seconds;
        }
    }
}
