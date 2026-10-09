using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Append-only, ordered event log. Time never runs backwards: an event earlier than the previous one
    /// (by period, then time in period) is rejected.
    /// </summary>
    public sealed class EventLog
    {
        private readonly List<SimEvent> _events = new List<SimEvent>();

        /// <summary>Events in the order appended.</summary>
        public IReadOnlyList<SimEvent> Events
        {
            get { return _events; }
        }

        /// <summary>Number of events.</summary>
        public int Count
        {
            get { return _events.Count; }
        }

        /// <summary>Appends an event. Throws if it is earlier than the last event.</summary>
        public void Append(SimEvent simEvent)
        {
            if (simEvent == null)
            {
                throw new ArgumentNullException(nameof(simEvent));
            }

            if (_events.Count > 0)
            {
                EventContext last = _events[_events.Count - 1].Context;
                if (simEvent.Context.CompareTime(last) < 0)
                {
                    throw new ArgumentException(
                        "Time runs backwards: period " + simEvent.Context.Period + " at " + simEvent.Context.Time
                        + "s is before period " + last.Period + " at " + last.Time + "s.",
                        nameof(simEvent));
                }
            }

            _events.Add(simEvent);
        }
    }
}
