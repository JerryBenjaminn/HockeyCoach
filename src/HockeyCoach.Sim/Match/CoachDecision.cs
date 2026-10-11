using System;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Match
{
    /// <summary>
    /// A coach's decision at a stoppage (O-4, AI-5): the units for the faceoff, the plan until the next stoppage and
    /// whether to call a timeout.
    /// </summary>
    public sealed class CoachDecision
    {
        /// <summary>Creates the decision without a timeout.</summary>
        public CoachDecision(LineChoice units, ShiftPlan plan)
            : this(units, plan, false)
        {
        }

        /// <summary>Creates the decision.</summary>
        /// <param name="units">The units for the faceoff.</param>
        /// <param name="plan">The plan until the next stoppage.</param>
        /// <param name="callTimeout">Call a timeout at this stoppage; only when <see cref="CoachTimeouts.CanCall"/> (AI-4).</param>
        public CoachDecision(LineChoice units, ShiftPlan plan, bool callTimeout)
        {
            Units = units ?? throw new ArgumentNullException(nameof(units));
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            CallTimeout = callTimeout;
        }

        /// <summary>Call a timeout at this stoppage (AI-4).</summary>
        public bool CallTimeout { get; }

        /// <summary>The units for the faceoff.</summary>
        public LineChoice Units { get; }

        /// <summary>The plan until the next stoppage.</summary>
        public ShiftPlan Plan { get; }
    }
}
