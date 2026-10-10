using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// Line nodes and distances (data-schema.md, Määritelmät M-1, M-2, D-049). Integer arithmetic only; the line is the
    /// same in both directions, mirrored and rotated.
    /// </summary>
    public static class LineGeometry
    {
        /// <summary>
        /// The nodes of the line A → B, endpoints included, in a fixed order (by step i, then x, then y). At an exact
        /// half step both neighbouring nodes are included (M-1).
        /// </summary>
        public static IReadOnlyList<GridPoint> Nodes(GridPoint a, GridPoint b)
        {
            int dx = b.X - a.X;
            int dy = b.Y - a.Y;
            int n = Math.Max(Math.Abs(dx), Math.Abs(dy));
            var nodes = new List<GridPoint>();
            if (n == 0)
            {
                nodes.Add(a);
                return nodes;
            }

            var seen = new HashSet<GridPoint>();
            for (int i = 0; i <= n; i++)
            {
                int[] xs = Coordinates(a.X, dx, i, n);
                int[] ys = Coordinates(a.Y, dy, i, n);
                foreach (int x in xs)
                {
                    foreach (int y in ys)
                    {
                        var node = new GridPoint(x, y);
                        if (seen.Add(node))
                        {
                            nodes.Add(node);
                        }
                    }
                }
            }

            return nodes;
        }

        /// <summary>Smallest Chebyshev distance from <paramref name="point"/> to any line node (0 = on the line).</summary>
        public static int DistanceToLine(GridPoint point, IReadOnlyList<GridPoint> line)
        {
            if (line == null || line.Count == 0)
            {
                throw new ArgumentException("A line needs at least one node.", nameof(line));
            }

            int best = int.MaxValue;
            foreach (GridPoint node in line)
            {
                best = Math.Min(best, GridPoint.Chebyshev(point, node));
            }

            return best;
        }

        /// <summary>
        /// The line defender (M-1): the defender closest to the line; ties by Chebyshev, then Manhattan distance to the
        /// reference node, then position order C, LW, RW, LD, RD. Returns false if there are no defenders.
        /// </summary>
        /// <param name="defenders">Defender positions and nodes (same view as the line).</param>
        /// <param name="line">Line nodes.</param>
        /// <param name="reference">Reference node (receiver, skate target or shooter).</param>
        /// <param name="defender">The chosen position.</param>
        /// <param name="distance">Its distance from the line.</param>
        public static bool LineDefender(IReadOnlyDictionary<Position, GridPoint> defenders, IReadOnlyList<GridPoint> line, GridPoint reference, out Position defender, out int distance)
        {
            return Closest(defenders, line, reference, int.MaxValue, out defender, out distance);
        }

        /// <summary>
        /// Like <see cref="LineDefender"/> but only among defenders at most <paramref name="maxDistance"/> from the line
        /// (M-5 blocker candidates).
        /// </summary>
        public static bool Closest(IReadOnlyDictionary<Position, GridPoint> defenders, IReadOnlyList<GridPoint> line, GridPoint reference, int maxDistance, out Position defender, out int distance)
        {
            if (defenders == null)
            {
                throw new ArgumentNullException(nameof(defenders));
            }

            bool found = false;
            defender = default;
            distance = 0;
            int bestCheb = 0;
            int bestMan = 0;
            foreach (Position position in PositionOrder.All)
            {
                if (!defenders.TryGetValue(position, out GridPoint node))
                {
                    continue;
                }

                int d = DistanceToLine(node, line);
                if (d > maxDistance)
                {
                    continue;
                }

                int cheb = GridPoint.Chebyshev(node, reference);
                int man = GridPoint.Manhattan(node, reference);
                if (!found || d < distance || (d == distance && (cheb < bestCheb || (cheb == bestCheb && man < bestMan))))
                {
                    found = true;
                    defender = position;
                    distance = d;
                    bestCheb = cheb;
                    bestMan = man;
                }
            }

            return found;
        }

        /// <summary>M-2: the two ends are on opposite sides of the middle lane.</summary>
        public static bool IsCrossIce(GridPoint a, GridPoint b, Rink rink)
        {
            Lane la = rink.LaneOf(a.Y);
            Lane lb = rink.LaneOf(b.Y);
            return (la == Lane.Left && lb == Lane.Right) || (la == Lane.Right && lb == Lane.Left);
        }

        private static int[] Coordinates(int a, int d, int i, int n)
        {
            int q = i * d;
            int f = FloorDiv(q, n);
            int r = q - (f * n);
            if (r == 0)
            {
                return new[] { a + f };
            }

            if (2 * r < n)
            {
                return new[] { a + f };
            }

            if (2 * r > n)
            {
                return new[] { a + f + 1 };
            }

            return new[] { a + f, a + f + 1 };
        }

        private static int FloorDiv(int q, int n)
        {
            int f = q / n;
            return (q % n != 0 && q < 0) ? f - 1 : f;
        }
    }
}
