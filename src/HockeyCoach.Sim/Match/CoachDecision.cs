using System;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Match
{
    /// <summary>A coach's decision at a stoppage (O-4): the units for the faceoff and the plan until the next stoppage.</summary>
    public sealed class CoachDecision
    {
        /// <summary>Creates the decision.</summary>
        public CoachDecision(LineChoice units, ShiftPlan plan)
        {
            Units = units ?? throw new ArgumentNullException(nameof(units));
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        /// <summary>The units for the faceoff.</summary>
        public LineChoice Units { get; }

        /// <summary>The plan until the next stoppage.</summary>
        public ShiftPlan Plan { get; }
    }
}
