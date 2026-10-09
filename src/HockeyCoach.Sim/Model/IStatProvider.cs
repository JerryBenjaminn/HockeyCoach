namespace HockeyCoach.Sim.Model
{
    /// <summary>Anything a check can read stats from (a skater or a goalie).</summary>
    public interface IStatProvider
    {
        /// <summary>
        /// Returns the value of the referenced stat.
        /// Throws <see cref="System.InvalidOperationException"/> if the stat belongs to the other stat set.
        /// </summary>
        int GetStat(StatRef stat);
    }
}
