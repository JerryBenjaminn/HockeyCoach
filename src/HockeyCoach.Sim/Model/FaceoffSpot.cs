using System;

namespace HockeyCoach.Sim.Model
{
    /// <summary>A named faceoff spot in the own-team view (rink.json <c>faceoffSpots</c>).</summary>
    public sealed class FaceoffSpot
    {
        /// <summary>Creates a faceoff spot.</summary>
        public FaceoffSpot(string id, GridPoint point)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Point = point;
        }

        /// <summary>Spot id, e.g. <c>center</c> or <c>offensiveLeft</c>.</summary>
        public string Id { get; }

        /// <summary>Grid coordinate of the spot.</summary>
        public GridPoint Point { get; }
    }
}
