namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// The puck's path through one timeline segment: from <see cref="Start"/>, optionally through <see cref="Via"/>
    /// (the blocker or goalie who stopped a shot, or the goal mouth for a goal), to <see cref="End"/>, the event's
    /// <c>placement.puckNodeId</c>. The puck moves at constant speed along the path.
    /// </summary>
    public sealed class PuckPath
    {
        /// <summary>Creates a path.</summary>
        public PuckPath(int startNodeId, RinkPoint start, int? viaNodeId, RinkPoint? via, int endNodeId, RinkPoint end, int? carrierId)
        {
            StartNodeId = startNodeId;
            Start = start;
            ViaNodeId = viaNodeId;
            Via = via;
            EndNodeId = endNodeId;
            End = end;
            CarrierId = carrierId;
            FirstLeg = via.HasValue ? RinkPoint.Distance(start, via.Value) : RinkPoint.Distance(start, end);
            Length = via.HasValue ? FirstLeg + RinkPoint.Distance(via.Value, end) : FirstLeg;
        }

        /// <summary>Node the puck starts from.</summary>
        public int StartNodeId { get; }

        /// <summary>Start position.</summary>
        public RinkPoint Start { get; }

        /// <summary>Node of the via point, null when there is none.</summary>
        public int? ViaNodeId { get; }

        /// <summary>Via point, null when there is none.</summary>
        public RinkPoint? Via { get; }

        /// <summary>Node the puck ends on (<c>placement.puckNodeId</c>).</summary>
        public int EndNodeId { get; }

        /// <summary>End position.</summary>
        public RinkPoint End { get; }

        /// <summary>Puck carrier after the event (<c>placement.puckCarrierId</c>), null for a loose puck.</summary>
        public int? CarrierId { get; }

        /// <summary>Total path length in metres.</summary>
        public double Length { get; }

        /// <summary>True when the puck moves in this segment.</summary>
        public bool Moves => Length > 0;

        private double FirstLeg { get; }

        /// <summary>Position at <paramref name="fraction"/> of the segment (clamped to 0–1).</summary>
        public RinkPoint PositionAt(double fraction)
        {
            if (!(fraction > 0))
            {
                return Start;
            }

            if (fraction >= 1)
            {
                return End;
            }

            if (Length <= 0)
            {
                return End;
            }

            double distance = fraction * Length;
            if (!Via.HasValue)
            {
                return RinkPoint.Lerp(Start, End, distance / Length);
            }

            if (distance <= FirstLeg)
            {
                return FirstLeg <= 0 ? Via.Value : RinkPoint.Lerp(Start, Via.Value, distance / FirstLeg);
            }

            double second = Length - FirstLeg;
            return RinkPoint.Lerp(Via.Value, End, (distance - FirstLeg) / second);
        }
    }
}
