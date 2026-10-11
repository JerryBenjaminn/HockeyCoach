using System.Collections.Generic;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// The zone entry strength "N vs M" of data-schema.md M-8 (D-080), in the attacking team's view. The same numbers are
    /// logged in the controlled zone entry event and feed the M-9 strength modifier of carries.
    /// </summary>
    internal static class EntryStrength
    {
        /// <summary>
        /// N = the carrier (or passer) always, plus attacking skaters with x ≥ the puck's x; M = defending skaters with
        /// x &gt; the puck's x (strictly between the puck and their own goal). Players level with the puck count for the
        /// attack only.
        /// </summary>
        /// <param name="puck">The carrier's or passer's node, attacker's view.</param>
        /// <param name="carrier">The carrier's or passer's position; always counted in N.</param>
        /// <param name="attackers">The attacking team's five skaters, attacker's view.</param>
        /// <param name="defenders">The defending team's five skaters, attacker's view.</param>
        public static EntryNumbers Count(
            GridPoint puck,
            Position carrier,
            IReadOnlyDictionary<Position, GridPoint> attackers,
            IReadOnlyDictionary<Position, GridPoint> defenders)
        {
            int n = 0;
            int m = 0;
            foreach (Position position in PositionOrder.All)
            {
                if (position == carrier || attackers[position].X >= puck.X)
                {
                    n++;
                }

                if (defenders[position].X > puck.X)
                {
                    m++;
                }
            }

            return new EntryNumbers(n, m);
        }
    }
}
