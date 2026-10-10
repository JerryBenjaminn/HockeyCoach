using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// D-058: at most one skater of a team per node (opponents may share). Resolves a team's simultaneous moves:
    /// movers are handled in the given order; a mover whose ideal end is taken (by an earlier mover's end or a
    /// stationary teammate) backs up along its own path node by node; if the whole path is taken it goes to the nearest
    /// free node (Chebyshev ring 1, then 2, …; ties: closer to the own goal, then closer to the middle lane, then smaller
    /// y, then smaller x). A goal node is never used. Coordinates are in the moving team's own view. Pure, no randomness.
    /// </summary>
    public static class OccupancyResolver
    {
        /// <summary>Order of movers moved by a defensive system: F1, D1, D2, F2, F3 (D-058).</summary>
        public static readonly SystemRole[] SystemOrder = { SystemRole.F1, SystemRole.D1, SystemRole.D2, SystemRole.F2, SystemRole.F3 };

        /// <summary>
        /// Order of attacking movers (D-058): the puck carrier first, then C, LW, RW, LD, RD. Only positions in
        /// <paramref name="movers"/> are returned.
        /// </summary>
        public static IReadOnlyList<Position> AttackOrder(Position? carrier, ICollection<Position> movers)
        {
            if (movers == null)
            {
                throw new ArgumentNullException(nameof(movers));
            }

            var order = new List<Position>();
            if (carrier.HasValue && movers.Contains(carrier.Value))
            {
                order.Add(carrier.Value);
            }

            foreach (Position position in PositionOrder.All)
            {
                if (movers.Contains(position) && !order.Contains(position))
                {
                    order.Add(position);
                }
            }

            return order;
        }

        /// <summary>Resolves the moves; returns the final node of every skater in <paramref name="current"/>.</summary>
        /// <param name="current">Current node of every skater of the team (team's own view).</param>
        /// <param name="moves">The movers in processing order. Each path starts at the mover's current node and ends at its ideal end.</param>
        /// <param name="rink">The rink.</param>
        public static IReadOnlyDictionary<Position, GridPoint> Resolve(
            IReadOnlyDictionary<Position, GridPoint> current,
            IReadOnlyList<OccupancyMove> moves,
            Rink rink)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (moves == null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            var moving = new HashSet<Position>();
            foreach (OccupancyMove move in moves)
            {
                if (move == null || !moving.Add(move.Position))
                {
                    throw new ArgumentException("Each mover must appear once.", nameof(moves));
                }

                if (!current.ContainsKey(move.Position))
                {
                    throw new ArgumentException("Mover " + move.Position + " has no current node.", nameof(moves));
                }
            }

            var result = new SortedDictionary<Position, GridPoint>();
            var occupied = new HashSet<GridPoint>();
            foreach (KeyValuePair<Position, GridPoint> entry in current)
            {
                if (!moving.Contains(entry.Key))
                {
                    result.Add(entry.Key, entry.Value);
                    occupied.Add(entry.Value);
                }
            }

            foreach (OccupancyMove move in moves)
            {
                GridPoint end = BackUp(move.Path, occupied, rink, out bool found);
                if (!found)
                {
                    end = NearestFree(move.Path[move.Path.Count - 1], occupied, rink);
                }

                result.Add(move.Position, end);
                occupied.Add(end);
            }

            return result;
        }

        /// <summary>
        /// The nearest free node around <paramref name="from"/> (excluding it): Chebyshev ring 1, then 2, and so on; within
        /// a ring closer to the own goal (Chebyshev, then Manhattan), then closer to the middle lane, then smaller y, then
        /// smaller x. Never a goal node.
        /// </summary>
        public static GridPoint NearestFree(GridPoint from, ICollection<GridPoint> occupied, Rink rink)
        {
            if (occupied == null)
            {
                throw new ArgumentNullException(nameof(occupied));
            }

            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            int maxRing = Math.Max(rink.Length, rink.Width);
            for (int ring = 1; ring <= maxRing; ring++)
            {
                bool found = false;
                GridPoint best = default;
                for (int x = from.X - ring; x <= from.X + ring; x++)
                {
                    for (int y = from.Y - ring; y <= from.Y + ring; y++)
                    {
                        var node = new GridPoint(x, y);
                        if (GridPoint.Chebyshev(node, from) != ring || !rink.Contains(node) || rink.IsGoalNode(node) || occupied.Contains(node))
                        {
                            continue;
                        }

                        if (!found || Better(node, best, rink))
                        {
                            best = node;
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    return best;
                }
            }

            throw new InvalidOperationException("No free node around " + from + " (D-058).");
        }

        private static GridPoint BackUp(IReadOnlyList<GridPoint> path, ICollection<GridPoint> occupied, Rink rink, out bool found)
        {
            if (path == null || path.Count == 0)
            {
                throw new ArgumentException("A move needs a path with at least the start node.", nameof(path));
            }

            for (int i = path.Count - 1; i >= 0; i--)
            {
                GridPoint node = path[i];
                if (!occupied.Contains(node) && rink.Contains(node) && !rink.IsGoalNode(node))
                {
                    found = true;
                    return node;
                }
            }

            found = false;
            return default;
        }

        private static bool Better(GridPoint a, GridPoint b, Rink rink)
        {
            GridPoint goal = rink.OwnGoal;
            int c = Compare(GridPoint.Chebyshev(a, goal), GridPoint.Chebyshev(b, goal));
            if (c == 0)
            {
                c = Compare(GridPoint.Manhattan(a, goal), GridPoint.Manhattan(b, goal));
            }

            if (c == 0)
            {
                c = Compare(Math.Abs(a.Y - rink.CentreLaneY), Math.Abs(b.Y - rink.CentreLaneY));
            }

            if (c == 0)
            {
                c = Compare(a.Y, b.Y);
            }

            if (c == 0)
            {
                c = Compare(a.X, b.X);
            }

            return c < 0;
        }

        private static int Compare(int a, int b)
        {
            return a < b ? -1 : (a > b ? 1 : 0);
        }
    }
}
