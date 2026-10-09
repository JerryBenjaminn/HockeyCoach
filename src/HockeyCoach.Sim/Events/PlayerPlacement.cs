using System;

namespace HockeyCoach.Sim.Events
{
    /// <summary>One player's node at the time of an event (for replay). Primitive ids only: Events sits below Model.</summary>
    public sealed class PlayerPlacement
    {
        /// <summary>Creates a placement.</summary>
        /// <param name="playerId">Player id, unique within the match.</param>
        /// <param name="team">The player's team.</param>
        /// <param name="nodeId">Rink node id in the home team's view (id = x * width + y).</param>
        /// <param name="isGoalie">Whether the player is a goalie.</param>
        public PlayerPlacement(int playerId, TeamSide team, int nodeId, bool isGoalie)
        {
            if (nodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeId), "Node id must not be negative.");
            }

            PlayerId = playerId;
            Team = team;
            NodeId = nodeId;
            IsGoalie = isGoalie;
        }

        /// <summary>Player id, unique within the match.</summary>
        public int PlayerId { get; }

        /// <summary>The player's team.</summary>
        public TeamSide Team { get; }

        /// <summary>Rink node id in the home team's view.</summary>
        public int NodeId { get; }

        /// <summary>Whether the player is a goalie.</summary>
        public bool IsGoalie { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return "#" + PlayerId + "@" + NodeId;
        }
    }
}
