namespace HockeyCoach.Sim.Events
{
    /// <summary>Pass (syöttö): passer, receiver, under pressure, succeeded.</summary>
    public sealed class PassEvent : SimEvent
    {
        /// <summary>Creates the event.</summary>
        /// <param name="context">Common event fields.</param>
        /// <param name="passerId">Passing player.</param>
        /// <param name="receiverId">Intended receiver.</param>
        /// <param name="underPressure">Whether the pass was made under pressure.</param>
        /// <param name="succeeded">Whether the pass reached the receiver.</param>
        public PassEvent(EventContext context, int passerId, int receiverId, bool underPressure, bool succeeded)
            : base(context)
        {
            PasserId = passerId;
            ReceiverId = receiverId;
            UnderPressure = underPressure;
            Succeeded = succeeded;
        }

        /// <summary>Passing player.</summary>
        public int PasserId { get; }

        /// <summary>Intended receiver.</summary>
        public int ReceiverId { get; }

        /// <summary>Whether the pass was made under pressure.</summary>
        public bool UnderPressure { get; }

        /// <summary>Whether the pass reached the receiver.</summary>
        public bool Succeeded { get; }

        /// <inheritdoc />
        public override T Accept<T>(ISimEventVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
