using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// A team's tactical choices for a shift (tech-spec.md, rajapinta valmentajalle): plays in priority order (D-036)
    /// and the defensive system. Transition and system-mode instructions come with milestone 3 (D-046).
    /// </summary>
    public sealed class ShiftPlan
    {
        private readonly Play[] _plays;

        /// <summary>Creates the plan.</summary>
        /// <param name="plays">Plays in priority order; may be empty.</param>
        /// <param name="system">The defensive system.</param>
        public ShiftPlan(IReadOnlyList<Play> plays, DefensiveSystem system)
        {
            if (plays == null)
            {
                throw new ArgumentNullException(nameof(plays));
            }

            _plays = new Play[plays.Count];
            for (int i = 0; i < plays.Count; i++)
            {
                _plays[i] = plays[i] ?? throw new ArgumentException("Null play.", nameof(plays));
            }

            System = system ?? throw new ArgumentNullException(nameof(system));
        }

        /// <summary>Plays in priority order.</summary>
        public IReadOnlyList<Play> Plays
        {
            get { return _plays; }
        }

        /// <summary>The defensive system.</summary>
        public DefensiveSystem System { get; }
    }
}
