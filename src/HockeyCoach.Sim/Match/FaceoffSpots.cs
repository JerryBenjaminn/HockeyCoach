using System;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Match
{
    /// <summary>Faceoff spot after each stoppage (data-schema.md O-3). Spot ids are in the home team's view (rink.json).</summary>
    public static class FaceoffSpots
    {
        /// <summary>Period start and after a goal.</summary>
        public const string Center = "center";

        /// <summary>Own-zone spot on the left in a team's own view.</summary>
        public const string DefensiveLeft = "defensiveLeft";

        /// <summary>Own-zone spot on the right in a team's own view.</summary>
        public const string DefensiveRight = "defensiveRight";

        /// <summary>The spot for the faceoff after <paramref name="result"/>'s stoppage.</summary>
        public static string After(Rink rink, ShiftResult result)
        {
            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            switch (result.EndReason)
            {
                case ShiftEndReason.GoalieFreeze:
                    return GoalieFreeze(rink, result.LastShotTeam, result.LastShotNode);
                case ShiftEndReason.OutOfPlay:
                    return OutOfPlay(rink, result.LastShotTeam, result.LastShotNode);
                default:
                    return Center;
            }
        }

        /// <summary>
        /// O-3 goalie freeze: the goalie team's own-zone spot on the shooter's side in the goalie team's view (middle lane →
        /// left).
        /// </summary>
        public static string GoalieFreeze(Rink rink, TeamSide shooterTeam, GridPoint shooterNode)
        {
            TeamSide goalieTeam = TeamSides.Opponent(shooterTeam);
            GridPoint view = TeamFrame.ToTeamView(shooterNode, goalieTeam, rink);
            string ownViewId = view.Y > rink.CentreLaneY ? DefensiveRight : DefensiveLeft;
            return SpotInHomeView(rink, ownViewId, goalieTeam);
        }

        /// <summary>
        /// O-3 puck out of play (Q-040 default): the nearest spot in the shooter node's zone (shooter's view), Chebyshev then
        /// Manhattan; ties to the shooter's side, from the middle lane to the left (shooter's view).
        /// </summary>
        public static string OutOfPlay(Rink rink, TeamSide shooterTeam, GridPoint shooterNode)
        {
            GridPoint shooter = TeamFrame.ToTeamView(shooterNode, shooterTeam, rink);
            RinkZone zone = rink.ZoneAtX(shooter.X);
            FaceoffSpot best = null;
            GridPoint bestPoint = default;
            foreach (bool sameZoneOnly in new[] { true, false })
            {
                foreach (FaceoffSpot spot in rink.FaceoffSpots)
                {
                    GridPoint point = TeamFrame.ToTeamView(spot.Point, shooterTeam, rink);
                    if (sameZoneOnly && rink.ZoneAtX(point.X) != zone)
                    {
                        continue;
                    }

                    if (best == null || Better(rink, shooter, point, bestPoint))
                    {
                        best = spot;
                        bestPoint = point;
                    }
                }

                if (best != null)
                {
                    break;
                }
            }

            return best.Id;
        }

        private static bool Better(Rink rink, GridPoint shooter, GridPoint a, GridPoint b)
        {
            int c = GridPoint.Chebyshev(shooter, a).CompareTo(GridPoint.Chebyshev(shooter, b));
            if (c == 0)
            {
                c = GridPoint.Manhattan(shooter, a).CompareTo(GridPoint.Manhattan(shooter, b));
            }

            if (c == 0)
            {
                c = SideRank(rink, shooter, a).CompareTo(SideRank(rink, shooter, b));
            }

            if (c == 0)
            {
                c = a.Y.CompareTo(b.Y);
            }

            return c < 0;
        }

        /// <summary>0 for a spot on the shooter's side (middle lane counts as left), 1 otherwise.</summary>
        private static int SideRank(Rink rink, GridPoint shooter, GridPoint spot)
        {
            bool shooterRight = shooter.Y > rink.CentreLaneY;
            bool spotRight = spot.Y > rink.CentreLaneY;
            bool spotLeft = spot.Y < rink.CentreLaneY;
            return shooterRight ? (spotRight ? 0 : 1) : (spotLeft ? 0 : 1);
        }

        /// <summary>The home-view id of the spot that is <paramref name="ownViewId"/> in <paramref name="team"/>'s own view.</summary>
        private static string SpotInHomeView(Rink rink, string ownViewId, TeamSide team)
        {
            foreach (FaceoffSpot spot in rink.FaceoffSpots)
            {
                if (string.Equals(spot.Id, ownViewId, StringComparison.Ordinal))
                {
                    GridPoint rinkPoint = TeamFrame.ToRink(spot.Point, team, rink);
                    foreach (FaceoffSpot candidate in rink.FaceoffSpots)
                    {
                        if (candidate.Point.Equals(rinkPoint))
                        {
                            return candidate.Id;
                        }
                    }
                }
            }

            throw new InvalidOperationException("No faceoff spot " + ownViewId + " for " + team + ".");
        }
    }
}
