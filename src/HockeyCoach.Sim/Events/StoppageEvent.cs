namespace HockeyCoach.Sim.Events
{
    /// <summary>Stoppage (pelikatko): reason.</summary>
    public sealed class StoppageEvent : SimEvent
    {
        /// <summary>Creates the event.</summary>
        /// <param name="context">Common event fields.</param>
        /// <param name="reason">Why play stopped.</param>
        public StoppageEvent(EventContext context, StoppageReason reason)
            : base(context)
        {
            Reason = reason;
        }

        /// <summary>Why play stopped.</summary>
        public StoppageReason Reason { get; }

        /// <inheritdoc />
        public override T Accept<T>(ISimEventVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
