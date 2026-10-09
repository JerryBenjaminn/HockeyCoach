using System;

namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Base of all event log entries. The event types are exactly those of the event schema
    /// (docs/stats-and-checks.md, Tapahtumaskeema); no other types are added (D-045).
    /// </summary>
    public abstract class SimEvent
    {
        /// <summary>Creates the event with its common fields.</summary>
        protected SimEvent(EventContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>Time, period, strength, placements, play and system ids.</summary>
        public EventContext Context { get; }

        /// <summary>Dispatches to the visitor method of the concrete event type.</summary>
        public abstract T Accept<T>(ISimEventVisitor<T> visitor);
    }
}
