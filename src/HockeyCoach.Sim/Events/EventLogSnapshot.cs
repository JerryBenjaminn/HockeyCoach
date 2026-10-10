using System;
using System.Collections;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// A read-only view of the first <see cref="Count"/> events of an <see cref="EventLog"/>, fixed when it was taken. The
    /// log is append-only and events are immutable, so later appends do not change the snapshot. Used for the public
    /// event log a coach sees (D-077).
    /// </summary>
    public sealed class EventLogSnapshot : IReadOnlyList<SimEvent>
    {
        private readonly IReadOnlyList<SimEvent> _events;

        internal EventLogSnapshot(IReadOnlyList<SimEvent> events, int count)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            if (count < 0 || count > events.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Count = count;
        }

        /// <summary>Number of events in the snapshot.</summary>
        public int Count { get; }

        /// <summary>The event at <paramref name="index"/>.</summary>
        public SimEvent this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _events[index];
            }
        }

        /// <inheritdoc />
        public IEnumerator<SimEvent> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return _events[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
