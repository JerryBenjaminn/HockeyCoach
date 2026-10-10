using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.AI
{
    /// <summary>
    /// Random tactics (data-schema.md AI-2, R-T, steps 3–8): the whole library in a Fisher–Yates order from load order,
    /// a uniform system (systems in ordinal id order), a uniform transition (<c>rush</c>, <c>regroup</c>) and uniform
    /// system-mode instructions (<c>netFrontAfterShot</c>, <c>looseChasers</c>, <c>pinch</c>), drawn in that order.
    /// </summary>
    /// <remarks>
    /// Step 1 (the timeout draw) is not drawn yet: the timeout (AI-4) is not implemented, so no timeout is available and
    /// the spec draws nothing. When it is implemented, that draw comes before the lines and changes random coaches' logs.
    /// </remarks>
    public sealed class RandomTactics
    {
        private static readonly TransitionInstruction[] Transitions = { TransitionInstruction.Rush, TransitionInstruction.Regroup };

        private readonly Play[] _library;
        private readonly DefensiveSystem[] _systems;
        private readonly IRandom _random;

        /// <summary>Creates the tactics chooser.</summary>
        /// <param name="library">All plays in load order.</param>
        /// <param name="systems">All loaded systems (any order; sorted by id here).</param>
        /// <param name="random">The coach's own stream (D-077).</param>
        public RandomTactics(IReadOnlyList<Play> library, IReadOnlyList<DefensiveSystem> systems, IRandom random)
        {
            if (library == null)
            {
                throw new ArgumentNullException(nameof(library));
            }

            if (systems == null || systems.Count == 0)
            {
                throw new ArgumentException("At least one system is needed.", nameof(systems));
            }

            _library = new Play[library.Count];
            for (int i = 0; i < library.Count; i++)
            {
                _library[i] = library[i] ?? throw new ArgumentException("Null play.", nameof(library));
            }

            var sorted = new List<DefensiveSystem>();
            foreach (DefensiveSystem system in systems)
            {
                sorted.Add(system ?? throw new ArgumentException("Null system.", nameof(systems)));
            }

            sorted.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            _systems = sorted.ToArray();
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Draws a plan: plays, system, transition, netFrontAfterShot, looseChasers, pinch.</summary>
        public ShiftPlan Draw()
        {
            var plays = (Play[])_library.Clone();
            for (int i = plays.Length - 1; i >= 1; i--)
            {
                int j = _random.NextInt(i + 1);
                Play swap = plays[i];
                plays[i] = plays[j];
                plays[j] = swap;
            }

            DefensiveSystem system = _systems[_random.NextInt(_systems.Length)];
            TransitionInstruction transition = Transitions[_random.NextInt(Transitions.Length)];
            bool netFront = _random.NextInt(2) == 1;
            int chasers = 1 + _random.NextInt(2);
            bool pinch = _random.NextInt(2) == 1;
            return new ShiftPlan(plays, system, transition, new SystemModeInstructions(netFront, chasers, pinch));
        }
    }
}
