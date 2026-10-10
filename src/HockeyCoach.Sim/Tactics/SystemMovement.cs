using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// A defender's movement toward its system target (data-schema.md, Liikkuminen): at most <c>plays.maxNodesPerBeat</c>
    /// steps per event, each step (sign dx, sign dy). A step onto a goal node is replaced by circling the goal (D-053,
    /// Maalin kierto). Coordinates are in the defending team's own view.
    /// </summary>
    public static class SystemMovement
    {
        /// <summary>The node reached after up to <paramref name="maxSteps"/> steps from <paramref name="from"/>.</summary>
        /// <param name="from">Current node.</param>
        /// <param name="target">Target node (never a goal node).</param>
        /// <param name="puck">Puck node, deciding the side used to circle a goal.</param>
        /// <param name="maxSteps">Most steps (tuning.json <c>plays.maxNodesPerBeat</c>).</param>
        /// <param name="rink">The rink.</param>
        public static GridPoint Advance(GridPoint from, GridPoint target, GridPoint puck, int maxSteps, Rink rink)
        {
            IReadOnlyList<GridPoint> path = Path(from, target, puck, maxSteps, rink);
            return path[path.Count - 1];
        }

        /// <summary>
        /// The nodes visited by <see cref="Advance"/>: <paramref name="from"/> first, the reached node last (D-058 backs
        /// up along this path).
        /// </summary>
        public static IReadOnlyList<GridPoint> Path(GridPoint from, GridPoint target, GridPoint puck, int maxSteps, Rink rink)
        {
            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            var path = new List<GridPoint> { from };
            GridPoint current = from;
            for (int i = 0; i < maxSteps && !current.Equals(target); i++)
            {
                GridPoint next = Step(current, target, puck, rink);
                if (next.Equals(current))
                {
                    break;
                }

                current = next;
                path.Add(current);
            }

            return path;
        }

        /// <summary>
        /// One step toward the target. A step onto a goal node G circles it (D-053):
        /// side s from the puck's y against G (puck on G's row: the defender's own side, middle lane counts as −1);
        /// with sign dx ≠ 0 the replacement is (gx, gy + s) when one step away, otherwise (0, s);
        /// with sign dx = 0 the step is (c, sign dy), c toward the centre line, via the net front.
        /// Returns <paramref name="from"/> only if no legal step exists (never with valid data).
        /// </summary>
        public static GridPoint Step(GridPoint from, GridPoint target, GridPoint puck, Rink rink)
        {
            int sx = Math.Sign(target.X - from.X);
            int sy = Math.Sign(target.Y - from.Y);
            var next = new GridPoint(from.X + sx, from.Y + sy);
            if (!rink.IsGoalNode(next))
            {
                return next;
            }

            GridPoint goal = next;
            GridPoint replacement;
            if (sx != 0)
            {
                int s = CircleSide(goal, puck, from);
                var beside = new GridPoint(goal.X, goal.Y + s);
                replacement = Math.Abs(beside.Y - from.Y) <= 1 ? beside : new GridPoint(from.X, from.Y + s);
            }
            else
            {
                int c = goal.X > rink.CentreLineX ? -1 : 1;
                replacement = new GridPoint(from.X + c, from.Y + sy);
            }

            return rink.Contains(replacement) && !rink.IsGoalNode(replacement) ? replacement : from;
        }

        private static int CircleSide(GridPoint goal, GridPoint puck, GridPoint defender)
        {
            if (puck.Y != goal.Y)
            {
                return puck.Y < goal.Y ? -1 : 1;
            }

            return defender.Y > goal.Y ? 1 : -1;
        }
    }
}
