using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Puck battle (kamppailu): participants and outcome. Rebounds are logged as this event (D-045).
    /// Participants are split by side so that the outcome has a point of view.
    /// </summary>
    public sealed class PuckBattleEvent : SimEvent
    {
        private readonly int[] _attackerIds;
        private readonly int[] _defenderIds;

        /// <summary>Creates the event.</summary>
        /// <param name="context">Common event fields.</param>
        /// <param name="attackerIds">Participants on the attacker side of the check, at least one.</param>
        /// <param name="defenderIds">Participants on the defender side of the check, at least one.</param>
        /// <param name="outcome">Result seen from the attacker side.</param>
        public PuckBattleEvent(EventContext context, IReadOnlyList<int> attackerIds, IReadOnlyList<int> defenderIds, BattleOutcome outcome)
            : base(context)
        {
            _attackerIds = CopyNonEmpty(attackerIds, nameof(attackerIds));
            _defenderIds = CopyNonEmpty(defenderIds, nameof(defenderIds));
            Outcome = outcome;
        }

        /// <summary>Participants on the attacker side, in the order given.</summary>
        public IReadOnlyList<int> AttackerIds
        {
            get { return _attackerIds; }
        }

        /// <summary>Participants on the defender side, in the order given.</summary>
        public IReadOnlyList<int> DefenderIds
        {
            get { return _defenderIds; }
        }

        /// <summary>Result seen from the attacker side.</summary>
        public BattleOutcome Outcome { get; }

        /// <inheritdoc />
        public override T Accept<T>(ISimEventVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        private static int[] CopyNonEmpty(IReadOnlyList<int> ids, string paramName)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (ids.Count == 0)
            {
                throw new ArgumentException("A battle side needs at least one participant.", paramName);
            }

            var copy = new int[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                copy[i] = ids[i];
            }

            return copy;
        }
    }
}
