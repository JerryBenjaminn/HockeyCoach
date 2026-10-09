using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// One beat of a play: moves of players without the puck, then exactly one puck action (D-032, D-041).
    /// A position missing from <see cref="Moves"/> stays where it is.
    /// </summary>
    public sealed class Beat
    {
        private readonly SortedDictionary<Position, GridPoint> _moves;

        /// <summary>Creates a beat. Use <c>PlayValidator</c> to check it against the rules.</summary>
        public Beat(IEnumerable<KeyValuePair<Position, GridPoint>> moves, PlayAction action)
        {
            _moves = PositionMap.Copy(moves, nameof(moves));
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>Position → target node, in position order (C, LW, RW, LD, RD).</summary>
        public IReadOnlyDictionary<Position, GridPoint> Moves
        {
            get { return _moves; }
        }

        /// <summary>The beat's puck action.</summary>
        public PlayAction Action { get; }
    }
}
