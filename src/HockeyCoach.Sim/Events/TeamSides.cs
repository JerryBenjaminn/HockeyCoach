namespace HockeyCoach.Sim.Events
{
    /// <summary>Helpers for <see cref="TeamSide"/>.</summary>
    public static class TeamSides
    {
        /// <summary>The other team.</summary>
        public static TeamSide Opponent(TeamSide side)
        {
            return side == TeamSide.Home ? TeamSide.Away : TeamSide.Home;
        }
    }
}
