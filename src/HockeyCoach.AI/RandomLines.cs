using System;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.AI
{
    /// <summary>
    /// Random lines (data-schema.md AI-2, R-L). At a stoppage every eligible unit type draws uniformly from all its units
    /// (staying is one outcome); at an on-the-fly opportunity every eligible unit type changes with
    /// <c>ai.random.onTheFlyChangeProbability</c> to a uniformly drawn bench unit. Trio first, then pair. A unit that may
    /// not change stays without a draw.
    /// </summary>
    public sealed class RandomLines
    {
        private readonly AiRandomConfig _config;
        private readonly IRandom _random;

        /// <summary>Creates the line chooser.</summary>
        /// <param name="config"><c>ai.random</c>.</param>
        /// <param name="random">The coach's own stream (D-077).</param>
        public RandomLines(AiRandomConfig config, IRandom random)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Units at a stoppage or period start.</summary>
        public LineChoice AtStoppage(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            int forwards = view.CanChangeForwards ? _random.NextInt(view.Team.ForwardLines.Count) : view.ForwardIndex;
            int pair = view.CanChangeDefence ? _random.NextInt(view.Team.DefencePairs.Count) : view.PairIndex;
            return new LineChoice(forwards, pair);
        }

        /// <summary>Units at an on-the-fly opportunity.</summary>
        public LineChoice OnTheFly(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            int forwards = view.CanChangeForwards ? MaybeChange(view.ForwardIndex, view.Team.ForwardLines.Count) : view.ForwardIndex;
            int pair = view.CanChangeDefence ? MaybeChange(view.PairIndex, view.Team.DefencePairs.Count) : view.PairIndex;
            return new LineChoice(forwards, pair);
        }

        /// <summary>One chance draw; on a change, a uniform draw among the bench units in index order.</summary>
        private int MaybeChange(int current, int count)
        {
            if (!_random.Chance(_config.OnTheFlyChangeProbability))
            {
                return current;
            }

            int bench = current >= 0 && current < count ? count - 1 : count;
            if (bench == 0)
            {
                return current;
            }

            int pick = _random.NextInt(bench);
            return current >= 0 && pick >= current ? pick + 1 : pick;
        }
    }
}
