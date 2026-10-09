namespace HockeyCoach.Sim.Model
{
    /// <summary>Rink zone (alue) from the point of view of the team whose coordinates are used.</summary>
    public enum RinkZone
    {
        /// <summary>Own end (data id <c>defensive</c>).</summary>
        Defensive = 0,

        /// <summary>Neutral zone (data id <c>neutral</c>).</summary>
        Neutral = 1,

        /// <summary>Attacking end (data id <c>offensive</c>).</summary>
        Offensive = 2,
    }
}
