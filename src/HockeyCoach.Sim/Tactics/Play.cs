using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// A playbook play (docs/data-schema.md, Kuviot; D-034). Refers to positions, never to players. Coordinates are in
    /// the attacking team's own view. Any play can be run mirrored (<see cref="PlayMirror"/>).
    /// </summary>
    public sealed class Play
    {
        private readonly SortedDictionary<Position, GridPoint> _startPositions;
        private readonly Beat[] _beats;

        /// <summary>Creates a play. Use <c>PlayValidator</c> to check it against the rules.</summary>
        /// <param name="id">Unique id, equal to the file name.</param>
        /// <param name="name">Display name.</param>
        /// <param name="type">Play type.</param>
        /// <param name="faceoffSpotId">Faceoff spot id for <see cref="PlayType.Faceoff"/>, otherwise null.</param>
        /// <param name="puckCarrier">Position holding the puck at the start.</param>
        /// <param name="startPositions">Start node per position.</param>
        /// <param name="beats">Beats in order.</param>
        public Play(
            string id,
            string name,
            PlayType type,
            string faceoffSpotId,
            Position puckCarrier,
            IEnumerable<KeyValuePair<Position, GridPoint>> startPositions,
            IReadOnlyList<Beat> beats)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            FaceoffSpotId = faceoffSpotId;
            PuckCarrier = puckCarrier;
            _startPositions = PositionMap.Copy(startPositions, nameof(startPositions));
            if (beats == null)
            {
                throw new ArgumentNullException(nameof(beats));
            }

            _beats = new Beat[beats.Count];
            for (int i = 0; i < beats.Count; i++)
            {
                _beats[i] = beats[i] ?? throw new ArgumentException("Null beat.", nameof(beats));
            }
        }

        /// <summary>Unique id, equal to the file name.</summary>
        public string Id { get; }

        /// <summary>Display name.</summary>
        public string Name { get; }

        /// <summary>Play type.</summary>
        public PlayType Type { get; }

        /// <summary>Faceoff spot id for faceoff plays, otherwise null.</summary>
        public string FaceoffSpotId { get; }

        /// <summary>Position holding the puck at the start.</summary>
        public Position PuckCarrier { get; }

        /// <summary>Start node per position, in position order.</summary>
        public IReadOnlyDictionary<Position, GridPoint> StartPositions
        {
            get { return _startPositions; }
        }

        /// <summary>Beats in order.</summary>
        public IReadOnlyList<Beat> Beats
        {
            get { return _beats; }
        }
    }
}
