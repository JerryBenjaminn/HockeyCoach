using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Line change (vaihto): unit out, unit in. Units are given as player ids (a forward trio or a defence pair, D-023),
    /// which are unique within the match and so also identify the team.
    /// </summary>
    public sealed class LineChangeEvent : SimEvent
    {
        private readonly int[] _outgoingIds;
        private readonly int[] _incomingIds;

        /// <summary>Creates the event.</summary>
        /// <param name="context">Common event fields.</param>
        /// <param name="outgoingIds">Players of the unit leaving the ice.</param>
        /// <param name="incomingIds">Players of the unit coming on, same count as <paramref name="outgoingIds"/>.</param>
        public LineChangeEvent(EventContext context, IReadOnlyList<int> outgoingIds, IReadOnlyList<int> incomingIds)
            : base(context)
        {
            _outgoingIds = Copy(outgoingIds, nameof(outgoingIds));
            _incomingIds = Copy(incomingIds, nameof(incomingIds));
            if (_outgoingIds.Length == 0 || _outgoingIds.Length != _incomingIds.Length)
            {
                throw new ArgumentException("A line change swaps a non-empty unit for one of the same size.", nameof(incomingIds));
            }
        }

        /// <summary>Players of the unit leaving the ice, in the order given.</summary>
        public IReadOnlyList<int> OutgoingIds
        {
            get { return _outgoingIds; }
        }

        /// <summary>Players of the unit coming on, in the order given.</summary>
        public IReadOnlyList<int> IncomingIds
        {
            get { return _incomingIds; }
        }

        /// <inheritdoc />
        public override T Accept<T>(ISimEventVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        private static int[] Copy(IReadOnlyList<int> ids, string paramName)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(paramName);
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
