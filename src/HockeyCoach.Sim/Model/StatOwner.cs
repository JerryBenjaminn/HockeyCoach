namespace HockeyCoach.Sim.Model
{
    /// <summary>Whether a stat belongs to the skater or the goalie stat set.</summary>
    public enum StatOwner
    {
        /// <summary>Skater stat (<see cref="SkaterStat"/>).</summary>
        Skater = 0,

        /// <summary>Goalie stat (<see cref="GoalieStat"/>).</summary>
        Goalie = 1,
    }
}
