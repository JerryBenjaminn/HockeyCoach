namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Timeout (aikalisä, D-077; data-schema.md AI-4): the team that called it. Period and clock are in the common fields;
    /// placements are those at the stoppage. Logged only at a stoppage.
    /// </summary>
    public sealed class TimeoutEvent : SimEvent
    {
        /// <summary>Creates the event.</summary>
        /// <param name="context">Common event fields (the stoppage moment).</param>
        /// <param name="team">The team that called the timeout.</param>
        public TimeoutEvent(EventContext context, TeamSide team)
            : base(context)
        {
            Team = team;
        }

        /// <summary>The team that called the timeout.</summary>
        public TeamSide Team { get; }

        /// <inheritdoc />
        public override T Accept<T>(ISimEventVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
