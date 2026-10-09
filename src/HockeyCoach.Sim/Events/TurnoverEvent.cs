using System;

namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Turnover / takeaway (kiekonmenetys / riisto): loser, taker, location. A failed carry or deke is logged as this
    /// event (D-045).
    /// </summary>
    public sealed class TurnoverEvent : SimEvent
    {
        /// <summary>Creates the event.</summary>
        /// <param name="context">Common event fields.</param>
        /// <param name="loserId">Player who lost the puck.</param>
        /// <param name="takerId">Player who took the puck.</param>
        /// <param name="nodeId">Node where the puck changed hands (home team's view).</param>
        public TurnoverEvent(EventContext context, int loserId, int takerId, int nodeId)
            : base(context)
        {
            if (nodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeId), "Node id must not be negative.");
            }

            LoserId = loserId;
            TakerId = takerId;
            NodeId = nodeId;
        }

        /// <summary>Player who lost the puck.</summary>
        public int LoserId { get; }

        /// <summary>Player who took the puck.</summary>
        public int TakerId { get; }

        /// <summary>Node where the puck changed hands (home team's view).</summary>
        public int NodeId { get; }

        /// <inheritdoc />
        public override T Accept<T>(ISimEventVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
