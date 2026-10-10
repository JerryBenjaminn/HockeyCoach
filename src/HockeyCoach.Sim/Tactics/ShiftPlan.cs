using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// A team's tactical choices for a shift (tech-spec.md, rajapinta valmentajalle): plays in priority order (D-036),
    /// the defensive system, the transition instruction (O-7) and the system-mode instructions (O-10).
    /// </summary>
    public sealed class ShiftPlan
    {
        private readonly Play[] _plays;

        /// <summary>Creates the plan with the default transition <c>rush</c> (D-062) and default system-mode instructions.</summary>
        /// <param name="plays">Plays in priority order; may be empty.</param>
        /// <param name="system">The defensive system.</param>
        public ShiftPlan(IReadOnlyList<Play> plays, DefensiveSystem system)
            : this(plays, system, TransitionInstruction.Rush, SystemModeInstructions.Default)
        {
        }

        /// <summary>Creates the plan.</summary>
        /// <param name="plays">Plays in priority order; may be empty.</param>
        /// <param name="system">The defensive system.</param>
        /// <param name="transition">What to do on gaining the puck other than from a faceoff (O-7).</param>
        /// <param name="instructions">System-mode instructions (O-10).</param>
        public ShiftPlan(IReadOnlyList<Play> plays, DefensiveSystem system, TransitionInstruction transition, SystemModeInstructions instructions)
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
            Transition = transition;
            Instructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
        }

        /// <summary>Plays in priority order.</summary>
        public IReadOnlyList<Play> Plays
        {
            get { return _plays; }
        }

        /// <summary>The defensive system.</summary>
        public DefensiveSystem System { get; }

        /// <summary>The transition instruction (O-7).</summary>
        public TransitionInstruction Transition { get; }

        /// <summary>System-mode instructions (O-10).</summary>
        public SystemModeInstructions Instructions { get; }
    }
}
