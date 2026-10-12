namespace HockeyCoach.Replay.Core
{
    /// <summary>Team side. The home team attacks towards +x (the away team's net).</summary>
    public enum ReplayTeam
    {
        /// <summary>Home team.</summary>
        Home,

        /// <summary>Away team.</summary>
        Away,
    }

    /// <summary>Helpers for <see cref="ReplayTeam"/>.</summary>
    public static class ReplayTeams
    {
        /// <summary>Parses <c>Home</c> / <c>Away</c>; throws <see cref="ReplayFormatException"/> otherwise.</summary>
        public static ReplayTeam Parse(string value)
        {
            if (value == "Home")
            {
                return ReplayTeam.Home;
            }

            if (value == "Away")
            {
                return ReplayTeam.Away;
            }

            throw new ReplayFormatException("Unknown team '" + value + "' (expected Home or Away).");
        }

        /// <summary>The other team.</summary>
        public static ReplayTeam Opponent(ReplayTeam team)
        {
            return team == ReplayTeam.Home ? ReplayTeam.Away : ReplayTeam.Home;
        }
    }
}
