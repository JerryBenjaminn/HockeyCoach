using System;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Match
{
    /// <summary>Everything a match needs besides the coaches and the random generator.</summary>
    public sealed class MatchSetup
    {
        /// <summary>Creates the setup.</summary>
        /// <param name="rink">The rink.</param>
        /// <param name="tuning">Balance values.</param>
        /// <param name="home">Home team (4 trios, 3 pairs and a goalie, D-062).</param>
        /// <param name="away">Away team.</param>
        /// <param name="maxStepsPerSegment">Anti-stall cap per segment (D-043, a caller parameter).</param>
        /// <param name="maxSegments">Anti-stall cap on segments per match (D-043).</param>
        public MatchSetup(Rink rink, TuningConfig tuning, Team home, Team away, int maxStepsPerSegment, int maxSegments)
        {
            Rink = rink ?? throw new ArgumentNullException(nameof(rink));
            Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            Home = home ?? throw new ArgumentNullException(nameof(home));
            Away = away ?? throw new ArgumentNullException(nameof(away));
            if (home.Goalies.Count == 0 || away.Goalies.Count == 0)
            {
                throw new ArgumentException("Both teams need a goalie.");
            }

            if (maxStepsPerSegment < 1 || maxSegments < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSegments), "The caps must be at least 1.");
            }

            MaxStepsPerSegment = maxStepsPerSegment;
            MaxSegments = maxSegments;
        }

        /// <summary>The rink.</summary>
        public Rink Rink { get; }

        /// <summary>Balance values.</summary>
        public TuningConfig Tuning { get; }

        /// <summary>Home team; its first goalie plays the whole match.</summary>
        public Team Home { get; }

        /// <summary>Away team.</summary>
        public Team Away { get; }

        /// <summary>Anti-stall cap per segment.</summary>
        public int MaxStepsPerSegment { get; }

        /// <summary>Anti-stall cap on segments per match.</summary>
        public int MaxSegments { get; }
    }
}
