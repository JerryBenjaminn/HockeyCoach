using System;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// A defender's movement toward its system target (data-schema.md, Liikkuminen): at most <c>plays.maxNodesPerBeat</c>
    /// steps per event, each step (sign dx, sign dy). A step onto a goal node (D-033) is replaced: with sign dy ≠ 0 by
    /// (sign dx, 0); with sign dy = 0 by (sign dx, s), s = +1 when the puck is on the right (y above the middle lane),
    /// otherwise −1. Coordinates are in the defending team's own view.
    /// </summary>
    public static class SystemMovement
    {
        /// <summary>The node reached after up to <paramref name="maxSteps"/> steps from <paramref name="from"/>.</summary>
        /// <param name="from">Current node.</param>
        /// <param name="target">Target node (never a goal node).</param>
        /// <param name="puck">Puck node, deciding the side step around a goal.</param>
        /// <param name="maxSteps">Most steps (tuning.json <c>plays.maxNodesPerBeat</c>).</param>
        /// <param name="rink">The rink.</param>
        public static GridPoint Advance(GridPoint from, GridPoint target, GridPoint puck, int maxSteps, Rink rink)
        {
            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            GridPoint current = from;
            for (int i = 0; i < maxSteps && !current.Equals(target); i++)
            {
                GridPoint next = Step(current, target, puck, rink);
                if (next.Equals(current))
                {
                    break;
                }

                current = next;
            }

            return current;
        }

        /// <summary>One step toward the target, avoiding goal nodes. Returns <paramref name="from"/> when no step is possible.</summary>
        public static GridPoint Step(GridPoint from, GridPoint target, GridPoint puck, Rink rink)
        {
            int dx = Math.Sign(target.X - from.X);
            int dy = Math.Sign(target.Y - from.Y);
            var next = new GridPoint(from.X + dx, from.Y + dy);
            if (!rink.IsGoalNode(next))
            {
                return next;
            }

            if (dy != 0)
            {
                next = new GridPoint(from.X + dx, from.Y);
            }
            else
            {
                int side = rink.LaneOf(puck.Y) == Lane.Right ? 1 : -1;
                next = new GridPoint(from.X + dx, from.Y + side);
            }

            return rink.Contains(next) && !rink.IsGoalNode(next) ? next : from;
        }
    }
}
