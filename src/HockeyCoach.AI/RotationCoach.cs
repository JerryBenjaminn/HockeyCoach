using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Shift;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.AI
{
    /// <summary>
    /// The default coach (data-schema.md O-4, D-062): changes every eligible unit, trios 1 → 2 → 3 → 4 → 1 and pairs
    /// 1 → 2 → 3 → 1; orders the playbook least-used-this-match first (O-11 counter), ties in load order; transition
    /// <c>rush</c>; system-mode instructions at their milestone 2 defaults. Deterministic, no random generator.
    /// </summary>
    public sealed class RotationCoach : ICoach
    {
        private readonly Play[] _library;
        private readonly DefensiveSystem _system;

        /// <summary>Creates the coach.</summary>
        /// <param name="library">All plays the coach may use, in load order.</param>
        /// <param name="system">The defensive system (a harness parameter).</param>
        public RotationCoach(IReadOnlyList<Play> library, DefensiveSystem system)
        {
            if (library == null)
            {
                throw new ArgumentNullException(nameof(library));
            }

            _library = new Play[library.Count];
            for (int i = 0; i < library.Count; i++)
            {
                _library[i] = library[i] ?? throw new ArgumentException("Null play.", nameof(library));
            }

            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        /// <inheritdoc />
        public CoachDecision AtStoppage(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            return new CoachDecision(Rotate(view), new ShiftPlan(Playbook(view), _system, TransitionInstruction.Rush, SystemModeInstructions.Default));
        }

        /// <inheritdoc />
        public LineChoice OnTheFly(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            return Rotate(view);
        }

        /// <summary>Next unit for every eligible unit; −1 (no unit yet) becomes the first.</summary>
        public static LineChoice Rotate(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            int forwards = view.CanChangeForwards ? (view.ForwardIndex + 1) % view.Team.ForwardLines.Count : view.ForwardIndex;
            int pair = view.CanChangeDefence ? (view.PairIndex + 1) % view.Team.DefencePairs.Count : view.PairIndex;
            return new LineChoice(forwards, pair);
        }

        /// <summary>The library ordered by uses this match (ascending), ties in load order.</summary>
        public IReadOnlyList<Play> Playbook(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            var order = new List<int>();
            for (int i = 0; i < _library.Length; i++)
            {
                order.Add(i);
            }

            order.Sort((a, b) =>
            {
                int c = Uses(view, _library[a]).CompareTo(Uses(view, _library[b]));
                return c != 0 ? c : a.CompareTo(b);
            });
            var plays = new List<Play>();
            foreach (int index in order)
            {
                plays.Add(_library[index]);
            }

            return plays;
        }

        private static double Uses(CoachView view, Play play)
        {
            return view.PlayUses.TryGetValue(play.Id, out double uses) ? uses : 0.0;
        }
    }
}
