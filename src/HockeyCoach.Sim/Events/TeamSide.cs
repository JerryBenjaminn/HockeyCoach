namespace HockeyCoach.Sim.Events
{
    /// <summary>Which team of the match. The rink state is stored in the home team's view (docs/data-schema.md).</summary>
    public enum TeamSide
    {
        /// <summary>The home team.</summary>
        Home = 0,

        /// <summary>The away team.</summary>
        Away = 1,
    }
}
