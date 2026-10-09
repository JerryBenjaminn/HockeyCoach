using System;

namespace HockeyCoach.Sim.Model
{
    /// <summary>
    /// One node of the rink grid, in the own-team view (attacking toward x = length - 1). The node's zone is not
    /// stored; it is derived from the rink's zone x-ranges (D-028), see <see cref="Rink.ZoneOf(int)"/>.
    /// </summary>
    public sealed class RinkNode
    {
        /// <summary>Creates a node. The id is assigned by <see cref="Rink"/> from the coordinates.</summary>
        /// <param name="x">Coordinate along the length of the rink.</param>
        /// <param name="y">Coordinate across the width of the rink.</param>
        /// <param name="xgZone">Shot location category used to look up the base xG.</param>
        /// <param name="isSlot">Whether the node is in the slot.</param>
        public RinkNode(int x, int y, string xgZone, bool isSlot)
        {
            X = x;
            Y = y;
            XgZone = xgZone ?? throw new ArgumentNullException(nameof(xgZone));
            IsSlot = isSlot;
        }

        /// <summary>Coordinate along the length of the rink (own goal end = 0).</summary>
        public int X { get; }

        /// <summary>Coordinate across the width of the rink.</summary>
        public int Y { get; }

        /// <summary>Shot location category used to look up the base xG.</summary>
        public string XgZone { get; }

        /// <summary>Whether the node is in the slot.</summary>
        public bool IsSlot { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }
    }
}
