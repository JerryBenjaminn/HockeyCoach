namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Controlled zone entry (hallittu alueelletuonti): carrier, numbers, method, outcome. A carry into the offensive
    /// zone is logged as this event (D-045).
    /// </summary>
    public sealed class ControlledZoneEntryEvent : SimEvent
    {
        /// <summary>Creates the event.</summary>
        /// <param name="context">Common event fields.</param>
        /// <param name="carrierId">Player bringing the puck in.</param>
        /// <param name="numbers">Attackers vs defenders in the entry.</param>
        /// <param name="method">Carry or pass.</param>
        /// <param name="outcome">Possession kept or lost.</param>
        public ControlledZoneEntryEvent(EventContext context, int carrierId, EntryNumbers numbers, ZoneEntryMethod method, ZoneEntryOutcome outcome)
            : base(context)
        {
            CarrierId = carrierId;
            Numbers = numbers;
            Method = method;
            Outcome = outcome;
        }

        /// <summary>Player bringing the puck in.</summary>
        public int CarrierId { get; }

        /// <summary>Attackers vs defenders in the entry.</summary>
        public EntryNumbers Numbers { get; }

        /// <summary>Carry or pass.</summary>
        public ZoneEntryMethod Method { get; }

        /// <summary>Possession kept or lost.</summary>
        public ZoneEntryOutcome Outcome { get; }

        /// <inheritdoc />
        public override T Accept<T>(ISimEventVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
