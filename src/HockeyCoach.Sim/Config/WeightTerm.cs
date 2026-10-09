using System;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Config
{
    /// <summary>One weighted stat of one participant in a check, e.g. passer.passing × 0.6.</summary>
    public sealed class WeightTerm
    {
        /// <summary>Creates a weight term.</summary>
        /// <param name="participant">Participant role name within the check, e.g. <c>passer</c> or <c>goalie</c>.</param>
        /// <param name="stat">The stat read from that participant.</param>
        /// <param name="weight">Weight of the stat; the weights of one side sum to 1 (D-017).</param>
        public WeightTerm(string participant, StatRef stat, double weight)
        {
            Participant = participant ?? throw new ArgumentNullException(nameof(participant));
            Stat = stat;
            Weight = weight;
        }

        /// <summary>Participant role name within the check.</summary>
        public string Participant { get; }

        /// <summary>The stat read from the participant.</summary>
        public StatRef Stat { get; }

        /// <summary>Weight of the stat.</summary>
        public double Weight { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Participant + "." + Stat + " x " + Weight;
        }
    }
}
