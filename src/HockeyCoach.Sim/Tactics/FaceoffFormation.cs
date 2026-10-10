using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// The default faceoff formation of a team without a faceoff play for the spot (D-042; layout is the Q-034
    /// default): C on the spot, wingers two lanes to each side on the spot's column (clamped to the boards), defence
    /// two columns back on the lanes next to the middle lane. Team's own view.
    /// </summary>
    public static class FaceoffFormation
    {
        /// <summary>Default start nodes for the spot (team's own view).</summary>
        public static IReadOnlyDictionary<Position, GridPoint> Default(GridPoint spot, Rink rink)
        {
            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            int maxY = rink.Width - 1;
            int dx = Math.Max(0, spot.X - 2);
            return new SortedDictionary<Position, GridPoint>
            {
                { Position.Center, spot },
                { Position.LeftWing, new GridPoint(spot.X, Math.Max(0, spot.Y - 2)) },
                { Position.RightWing, new GridPoint(spot.X, Math.Min(maxY, spot.Y + 2)) },
                { Position.LeftDefence, new GridPoint(dx, rink.CentreLaneY - 1) },
                { Position.RightDefence, new GridPoint(dx, rink.CentreLaneY + 1) },
            };
        }
    }
}
