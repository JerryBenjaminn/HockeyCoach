using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>One skater's intended move for <see cref="OccupancyResolver"/>: the path from its node to its ideal end.</summary>
    public sealed class OccupancyMove
    {
        /// <summary>Creates a move.</summary>
        /// <param name="position">The moving position.</param>
        /// <param name="path">Nodes from the current node (first) to the ideal end (last), team's own view.</param>
        public OccupancyMove(Position position, IReadOnlyList<GridPoint> path)
        {
            if (path == null || path.Count == 0)
            {
                throw new ArgumentException("A move needs a path with at least the start node.", nameof(path));
            }

            Position = position;
            Path = path;
        }

        /// <summary>The moving position.</summary>
        public Position Position { get; }

        /// <summary>Nodes from the current node to the ideal end.</summary>
        public IReadOnlyList<GridPoint> Path { get; }
    }
}
