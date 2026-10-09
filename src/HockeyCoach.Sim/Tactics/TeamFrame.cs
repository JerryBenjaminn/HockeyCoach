using System;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// Conversion between a team's own view (data files, D-017) and the fixed rink frame of the simulation, which is the
    /// home team's view. The away team's coordinates are rotated by 180°; the rotation is its own inverse.
    /// </summary>
    public static class TeamFrame
    {
        /// <summary>A point in <paramref name="team"/>'s own view converted to the home view.</summary>
        public static GridPoint ToRink(GridPoint teamView, TeamSide team, Rink rink)
        {
            return Convert(teamView, team, rink);
        }

        /// <summary>A point in the home view converted to <paramref name="team"/>'s own view.</summary>
        public static GridPoint ToTeamView(GridPoint rinkPoint, TeamSide team, Rink rink)
        {
            return Convert(rinkPoint, team, rink);
        }

        private static GridPoint Convert(GridPoint point, TeamSide team, Rink rink)
        {
            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            return team == TeamSide.Home ? point : rink.Rotate(point);
        }
    }
}
