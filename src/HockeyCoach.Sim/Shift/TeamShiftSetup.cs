using System;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>One team's input to a shift: the skaters on the ice, the goalie and the tactical plan.</summary>
    public sealed class TeamShiftSetup
    {
        /// <summary>Creates the team setup.</summary>
        public TeamShiftSetup(OnIceSkaters skaters, Goalie goalie, ShiftPlan plan)
        {
            Skaters = skaters ?? throw new ArgumentNullException(nameof(skaters));
            Goalie = goalie ?? throw new ArgumentNullException(nameof(goalie));
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        /// <summary>The trio and pair on the ice (D-023).</summary>
        public OnIceSkaters Skaters { get; }

        /// <summary>The goalie.</summary>
        public Goalie Goalie { get; }

        /// <summary>Plays in priority order and the defensive system.</summary>
        public ShiftPlan Plan { get; }
    }
}
