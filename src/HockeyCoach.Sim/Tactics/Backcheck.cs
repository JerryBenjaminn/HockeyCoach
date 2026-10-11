using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// Back-check during the opponent's rush (D-085, data-schema.md "Paluukarvaus suorahyökkäyksessä"). Coordinates are in
    /// the defending team's own view: its own goal is at <see cref="Rink.OwnGoal"/>, so "toward the own goal" is x decreasing.
    /// Pure, no randomness.
    /// </summary>
    public static class Backcheck
    {
        /// <summary>
        /// Whether the defender is on the wrong side of the puck: x_p &lt; x_k in the attacking team's view, i.e.
        /// x_p &gt; x_k in the defender's own view. A defender level with the puck is not.
        /// </summary>
        /// <param name="defender">The defender's node (defending team's view).</param>
        /// <param name="puck">The puck node (defending team's view).</param>
        public static bool IsBehindPuck(GridPoint defender, GridPoint puck)
        {
            return defender.X > puck.X;
        }

        /// <summary>
        /// The back-check target (x_t, y_p): the puck's row (x_t = x_k, D-086), in the defender's own lane. The back-checker
        /// catches the carrier but does not pass him. The step is always toward the own goal, so a defender already at or
        /// past that row keeps his row (Q-074). A target on a goal node is never reached: the movement circles the goal
        /// (D-053).
        /// </summary>
        /// <param name="defender">The defender's node (defending team's view).</param>
        /// <param name="puck">The puck node (defending team's view).</param>
        public static GridPoint Target(GridPoint defender, GridPoint puck)
        {
            return new GridPoint(Math.Min(puck.X, defender.X), defender.Y);
        }

        /// <summary>
        /// The back-checker's path (data-schema.md "Paluukarvaus suorahyökkäyksessä", Liike 3): straight steps toward the own
        /// goal, at most <paramref name="maxSteps"/>, until x = x_t. A step onto a goal node circles it per D-053 (one of the
        /// steps); the circling step reaches row x_t when the target itself is a goal node, so the path never ends on one.
        /// </summary>
        /// <param name="defender">The defender's node (defending team's view).</param>
        /// <param name="puck">The puck node (defending team's view).</param>
        /// <param name="maxSteps">Most steps (<c>transitions.backcheckNodesPerBeat</c>, or less for the delayed presser).</param>
        /// <param name="rink">The rink.</param>
        /// <returns>The visited nodes, <paramref name="defender"/> first.</returns>
        public static IReadOnlyList<GridPoint> Path(GridPoint defender, GridPoint puck, int maxSteps, Rink rink)
        {
            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            GridPoint target = Target(defender, puck);
            var path = new List<GridPoint> { defender };
            GridPoint current = defender;
            for (int i = 0; i < maxSteps && current.X > target.X; i++)
            {
                GridPoint next = SystemMovement.Step(current, target, puck, rink);
                if (next.Equals(current))
                {
                    break;
                }

                current = next;
                path.Add(current);
            }

            return path;
        }
    }
}
