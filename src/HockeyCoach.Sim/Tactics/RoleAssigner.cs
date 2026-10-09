using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// Assigns the system roles (data-schema.md, Roolien jako; Q-011): forwards sorted by distance to the puck become
    /// F1, F2, F3 and the closer defenceman D1. Distance is Chebyshev, then Manhattan; remaining ties go to C, then the
    /// puck-side winger, then the other winger, and for defence the puck-side defenceman first. The puck side is left
    /// for the left and middle lane, right otherwise, in the defending team's view. Deterministic, no randomness.
    /// </summary>
    public static class RoleAssigner
    {
        /// <summary>Role → position for the five skaters.</summary>
        /// <param name="skaters">Node of each of the five positions, in the defending team's own view.</param>
        /// <param name="puck">Puck node in the defending team's own view (not mirrored).</param>
        /// <param name="rink">The rink.</param>
        public static IReadOnlyDictionary<SystemRole, Position> Assign(IReadOnlyDictionary<Position, GridPoint> skaters, GridPoint puck, Rink rink)
        {
            if (skaters == null)
            {
                throw new ArgumentNullException(nameof(skaters));
            }

            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            bool puckRight = rink.LaneOf(puck.Y) == Lane.Right;
            Position[] forwards = puckRight
                ? new[] { Position.Center, Position.RightWing, Position.LeftWing }
                : new[] { Position.Center, Position.LeftWing, Position.RightWing };
            Position[] defence = puckRight
                ? new[] { Position.RightDefence, Position.LeftDefence }
                : new[] { Position.LeftDefence, Position.RightDefence };

            Sort(forwards, skaters, puck);
            Sort(defence, skaters, puck);
            var roles = new SortedDictionary<SystemRole, Position>
            {
                { SystemRole.F1, forwards[0] },
                { SystemRole.F2, forwards[1] },
                { SystemRole.F3, forwards[2] },
                { SystemRole.D1, defence[0] },
                { SystemRole.D2, defence[1] },
            };
            return roles;
        }

        /// <summary>Stable insertion sort by (Chebyshev, Manhattan); equal keys keep the tie-break order given.</summary>
        private static void Sort(Position[] order, IReadOnlyDictionary<Position, GridPoint> skaters, GridPoint puck)
        {
            for (int i = 1; i < order.Length; i++)
            {
                Position current = order[i];
                int j = i - 1;
                while (j >= 0 && Compare(order[j], current, skaters, puck) > 0)
                {
                    order[j + 1] = order[j];
                    j--;
                }

                order[j + 1] = current;
            }
        }

        private static int Compare(Position a, Position b, IReadOnlyDictionary<Position, GridPoint> skaters, GridPoint puck)
        {
            GridPoint pa = NodeOf(a, skaters);
            GridPoint pb = NodeOf(b, skaters);
            int byChebyshev = GridPoint.Chebyshev(pa, puck).CompareTo(GridPoint.Chebyshev(pb, puck));
            return byChebyshev != 0 ? byChebyshev : GridPoint.Manhattan(pa, puck).CompareTo(GridPoint.Manhattan(pb, puck));
        }

        private static GridPoint NodeOf(Position position, IReadOnlyDictionary<Position, GridPoint> skaters)
        {
            if (!skaters.TryGetValue(position, out GridPoint node))
            {
                throw new ArgumentException("No node for position " + Positions.ToName(position) + ".", nameof(skaters));
            }

            return node;
        }
    }
}
