using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// A role's target in a system rule: a fixed <c>node</c> [x, y], or a <c>puckOffset</c> [dx, dy] from the puck
    /// node (data-schema.md, Puolustusjärjestelmät). Coordinates are in the defending team's own view.
    /// </summary>
    public sealed class SystemTarget
    {
        private SystemTarget(bool isPuckOffset, GridPoint value)
        {
            IsPuckOffset = isPuckOffset;
            Value = value;
        }

        /// <summary>True for <c>puckOffset</c>, false for <c>node</c>.</summary>
        public bool IsPuckOffset { get; }

        /// <summary>The node (for <c>node</c>) or the offset dx, dy (for <c>puckOffset</c>).</summary>
        public GridPoint Value { get; }

        /// <summary>A fixed node.</summary>
        public static SystemTarget Node(GridPoint node)
        {
            return new SystemTarget(false, node);
        }

        /// <summary>The puck node plus (dx, dy).</summary>
        public static SystemTarget PuckOffset(int dx, int dy)
        {
            return new SystemTarget(true, new GridPoint(dx, dy));
        }

        /// <summary>
        /// The target mirrored for a mirrorY system: <c>node</c> [x, y] → [x, width - 1 - y],
        /// <c>puckOffset</c> [dx, dy] → [dx, -dy].
        /// </summary>
        public SystemTarget Mirror(Rink rink)
        {
            return IsPuckOffset ? PuckOffset(Value.X, -Value.Y) : Node(rink.MirrorY(Value));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return (IsPuckOffset ? "puckOffset " : "node ") + Value;
        }
    }
}
