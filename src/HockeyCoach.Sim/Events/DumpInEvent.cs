namespace HockeyCoach.Sim.Events
{
    /// <summary>Dump-in (kiekko päätyyn): shooter and the result of the following puck battle.</summary>
    public sealed class DumpInEvent : SimEvent
    {
        /// <summary>Creates the event.</summary>
        /// <param name="context">Common event fields.</param>
        /// <param name="shooterId">Player who dumped the puck.</param>
        /// <param name="battleOutcome">Result of the loose-puck battle, seen from the dumping team.</param>
        public DumpInEvent(EventContext context, int shooterId, BattleOutcome battleOutcome)
            : base(context)
        {
            ShooterId = shooterId;
            BattleOutcome = battleOutcome;
        }

        /// <summary>Player who dumped the puck.</summary>
        public int ShooterId { get; }

        /// <summary>Result of the loose-puck battle, seen from the dumping team.</summary>
        public BattleOutcome BattleOutcome { get; }

        /// <inheritdoc />
        public override T Accept<T>(ISimEventVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
