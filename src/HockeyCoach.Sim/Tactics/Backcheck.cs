using System;
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
        /// The back-check target (x_t, y_p): the row one past the puck toward the defender's own goal, at most the net-front
        /// row (attacking view x_t = min(x_k + 1, opponentGoal.x − 1)), in the defender's own lane. A defender already at or
        /// past that row (only possible with the puck behind his goal) keeps his row: the step is always toward the own goal
        /// (Q-074 default).
        /// </summary>
        /// <param name="defender">The defender's node (defending team's view).</param>
        /// <param name="puck">The puck node (defending team's view).</param>
        /// <param name="rink">The rink.</param>
        public static GridPoint Target(GridPoint defender, GridPoint puck, Rink rink)
        {
            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            int row = Math.Max(puck.X - 1, rink.OwnGoal.X + 1);
            return new GridPoint(Math.Min(row, defender.X), defender.Y);
        }
    }
}
