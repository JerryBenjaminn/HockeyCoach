using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Model
{
    /// <summary>
    /// The rink node grid (D-017). Size comes from data; node id = x * <see cref="Width"/> + y.
    /// Coordinates are in the own-team view; <see cref="Flip"/> converts to the opponent's view (180° rotation).
    /// The constructor guards the grid itself; data-level rules (zones, xG zones, faceoff spots) are checked by
    /// <c>HockeyCoach.Sim.Config.RinkValidator</c>.
    /// </summary>
    public sealed class Rink
    {
        private readonly RinkNode[] _nodes;
        private readonly ZoneRange[] _zones;
        private readonly string[] _xgZones;
        private readonly FaceoffSpot[] _faceoffSpots;

        /// <summary>Creates a rink and validates that every grid coordinate has exactly one node.</summary>
        /// <param name="length">Number of node columns along the rink (x range 0..length-1).</param>
        /// <param name="width">Number of node rows across the rink (y range 0..width-1).</param>
        /// <param name="nodes">All nodes, in any order.</param>
        /// <param name="zones">Zones as x ranges.</param>
        /// <param name="xgZones">Names of the shot xG zones.</param>
        /// <param name="ownGoal">Own goal node.</param>
        /// <param name="opponentGoal">Opponent goal node.</param>
        /// <param name="faceoffSpots">Faceoff spots in the own-team view.</param>
        public Rink(
            int length,
            int width,
            IReadOnlyList<RinkNode> nodes,
            IReadOnlyList<ZoneRange> zones,
            IReadOnlyList<string> xgZones,
            GridPoint ownGoal,
            GridPoint opponentGoal,
            IReadOnlyList<FaceoffSpot> faceoffSpots)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Rink length must be positive.");
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Rink width must be positive.");
            }

            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            _zones = Copy(zones, nameof(zones));
            _xgZones = Copy(xgZones, nameof(xgZones));
            _faceoffSpots = Copy(faceoffSpots, nameof(faceoffSpots));
            OwnGoal = ownGoal;
            OpponentGoal = opponentGoal;
            Length = length;
            Width = width;
            _nodes = new RinkNode[length * width];

            foreach (RinkNode node in nodes)
            {
                if (node == null)
                {
                    throw new ArgumentException("Rink contains a null node.", nameof(nodes));
                }

                if (!Contains(node.X, node.Y))
                {
                    throw new ArgumentException("Node " + node + " is outside the " + length + "x" + width + " grid.", nameof(nodes));
                }

                int id = IdOf(node.X, node.Y);
                if (_nodes[id] != null)
                {
                    throw new ArgumentException("Duplicate node " + node + ".", nameof(nodes));
                }

                _nodes[id] = node;
            }

            for (int id = 0; id < _nodes.Length; id++)
            {
                if (_nodes[id] == null)
                {
                    throw new ArgumentException("Missing node (" + XOf(id) + "," + YOf(id) + ").", nameof(nodes));
                }
            }
        }

        /// <summary>Number of node columns along the rink.</summary>
        public int Length { get; }

        /// <summary>Number of node rows across the rink.</summary>
        public int Width { get; }

        /// <summary>Zones as x ranges, in data order.</summary>
        public IReadOnlyList<ZoneRange> Zones
        {
            get { return _zones; }
        }

        /// <summary>Names of the shot xG zones, in data order.</summary>
        public IReadOnlyList<string> XgZones
        {
            get { return _xgZones; }
        }

        /// <summary>Own goal node (own-team view).</summary>
        public GridPoint OwnGoal { get; }

        /// <summary>Opponent goal node (own-team view).</summary>
        public GridPoint OpponentGoal { get; }

        /// <summary>Faceoff spots, in data order.</summary>
        public IReadOnlyList<FaceoffSpot> FaceoffSpots
        {
            get { return _faceoffSpots; }
        }

        /// <summary>Total number of nodes.</summary>
        public int NodeCount
        {
            get { return _nodes.Length; }
        }

        /// <summary>Whether (x, y) is inside the grid.</summary>
        public bool Contains(int x, int y)
        {
            return x >= 0 && x < Length && y >= 0 && y < Width;
        }

        /// <summary>Node id of (x, y): x * Width + y.</summary>
        public int IdOf(int x, int y)
        {
            if (!Contains(x, y))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "(" + x + "," + y + ") is outside the rink.");
            }

            return x * Width + y;
        }

        /// <summary>x coordinate of a node id.</summary>
        public int XOf(int id)
        {
            return id / Width;
        }

        /// <summary>y coordinate of a node id.</summary>
        public int YOf(int id)
        {
            return id % Width;
        }

        /// <summary>Returns the node with the given id.</summary>
        public RinkNode GetNode(int id)
        {
            if (id < 0 || id >= _nodes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Node id " + id + " is outside the rink.");
            }

            return _nodes[id];
        }

        /// <summary>Returns the node at (x, y).</summary>
        public RinkNode GetNode(int x, int y)
        {
            return _nodes[IdOf(x, y)];
        }

        /// <summary>
        /// Converts a node id to the opponent's view: (x, y) becomes (Length - 1 - x, Width - 1 - y).
        /// Applying it twice returns the original id.
        /// </summary>
        public int Flip(int id)
        {
            RinkNode node = GetNode(id);
            return IdOf(Length - 1 - node.X, Width - 1 - node.Y);
        }

        private static T[] Copy<T>(IReadOnlyList<T> items, string paramName)
            where T : class
        {
            if (items == null)
            {
                throw new ArgumentNullException(paramName);
            }

            var copy = new T[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                copy[i] = items[i] ?? throw new ArgumentException("List contains a null entry.", paramName);
            }

            return copy;
        }
    }
}
