namespace HockeyCoach.Sim.Events
{
    /// <summary>Faceoff (aloitus): participants, winner, location.</summary>
    public sealed class FaceoffEvent : SimEvent
    {
        /// <summary>Creates the event.</summary>
        /// <param name="context">Common event fields.</param>
        /// <param name="homeCentreId">Home player taking the draw.</param>
        /// <param name="awayCentreId">Away player taking the draw.</param>
        /// <param name="winner">Team that won the draw.</param>
        /// <param name="location">Zone of the faceoff spot.</param>
        public FaceoffEvent(EventContext context, int homeCentreId, int awayCentreId, TeamSide winner, FaceoffLocation location)
            : base(context)
        {
            HomeCentreId = homeCentreId;
            AwayCentreId = awayCentreId;
            Winner = winner;
            Location = location;
        }

        /// <summary>Home player taking the draw.</summary>
        public int HomeCentreId { get; }

        /// <summary>Away player taking the draw.</summary>
        public int AwayCentreId { get; }

        /// <summary>Team that won the draw.</summary>
        public TeamSide Winner { get; }

        /// <summary>Zone of the faceoff spot.</summary>
        public FaceoffLocation Location { get; }

        /// <inheritdoc />
        public override T Accept<T>(ISimEventVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
