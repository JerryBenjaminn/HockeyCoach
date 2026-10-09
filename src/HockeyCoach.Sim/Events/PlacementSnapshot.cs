using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Where every player and the puck are at the time of an event (event schema: "pelaajien sijainnit toistoa varten").
    /// Exactly one of: a puck carrier, or a loose puck at <see cref="PuckNodeId"/>. Players are kept in player id order.
    /// </summary>
    public sealed class PlacementSnapshot
    {
        private readonly PlayerPlacement[] _players;

        /// <summary>Creates a snapshot and checks its invariants.</summary>
        /// <param name="players">Every player on the ice. Player ids must be unique.</param>
        /// <param name="puckCarrierId">The player holding the puck, or null for a loose puck.</param>
        /// <param name="puckNodeId">Node of the puck; must equal the carrier's node when there is a carrier.</param>
        public PlacementSnapshot(IReadOnlyList<PlayerPlacement> players, int? puckCarrierId, int puckNodeId)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            if (puckNodeId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(puckNodeId), "Node id must not be negative.");
            }

            _players = new PlayerPlacement[players.Count];
            for (int i = 0; i < players.Count; i++)
            {
                _players[i] = players[i] ?? throw new ArgumentException("Null placement.", nameof(players));
            }

            Array.Sort(_players, (a, b) => a.PlayerId.CompareTo(b.PlayerId));
            for (int i = 1; i < _players.Length; i++)
            {
                if (_players[i].PlayerId == _players[i - 1].PlayerId)
                {
                    throw new ArgumentException("Player #" + _players[i].PlayerId + " is placed twice.", nameof(players));
                }
            }

            if (puckCarrierId.HasValue)
            {
                PlayerPlacement carrier = Find(puckCarrierId.Value);
                if (carrier == null)
                {
                    throw new ArgumentException("Puck carrier #" + puckCarrierId.Value + " is not on the ice.", nameof(puckCarrierId));
                }

                if (carrier.NodeId != puckNodeId)
                {
                    throw new ArgumentException("Puck node " + puckNodeId + " differs from the carrier's node " + carrier.NodeId + ".", nameof(puckNodeId));
                }
            }

            PuckCarrierId = puckCarrierId;
            PuckNodeId = puckNodeId;
        }

        /// <summary>Every player on the ice, in player id order.</summary>
        public IReadOnlyList<PlayerPlacement> Players
        {
            get { return _players; }
        }

        /// <summary>The player holding the puck, or null when the puck is loose.</summary>
        public int? PuckCarrierId { get; }

        /// <summary>Node of the puck (the carrier's node when held).</summary>
        public int PuckNodeId { get; }

        /// <summary>Whether the puck is loose (no carrier).</summary>
        public bool IsPuckLoose
        {
            get { return !PuckCarrierId.HasValue; }
        }

        /// <summary>The placement of a player, or null if the player is not on the ice.</summary>
        public PlayerPlacement Find(int playerId)
        {
            foreach (PlayerPlacement placement in _players)
            {
                if (placement.PlayerId == playerId)
                {
                    return placement;
                }
            }

            return null;
        }

        /// <summary>Number of skaters (goalies excluded) of <paramref name="team"/> on the ice.</summary>
        public int SkaterCount(TeamSide team)
        {
            int count = 0;
            foreach (PlayerPlacement placement in _players)
            {
                if (placement.Team == team && !placement.IsGoalie)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
