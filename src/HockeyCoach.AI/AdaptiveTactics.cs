using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.AI
{
    /// <summary>
    /// Adaptive tactics (data-schema.md AI-2, A-T): like S-T, but over a window of the last <c>ai.adaptive.windowSeconds</c>
    /// match seconds (1) the play bonus is the expectation over the opponent's system distribution with a uniform prior
    /// of weight <c>priorWeight</c>, and (2) in mode <c>normal</c> the own system follows the opponent's controlled-entry
    /// share with hysteresis. One instance per match (it remembers its previous system).
    /// </summary>
    public sealed class AdaptiveTactics : RuleTactics
    {
        /// <summary>The prior mean of the controlled-entry share in A-T step 2 (data-schema.md: priorWeight × 0.5).</summary>
        public const double PriorEntryShare = 0.5;

        private string _previousSystem;

        /// <summary>Creates the policy.</summary>
        public AdaptiveTactics(IReadOnlyList<Play> library, IReadOnlyList<DefensiveSystem> systems, TuningConfig tuning)
            : base(library, systems, tuning)
        {
        }

        /// <summary>The events of the window: match second &gt; now − windowSeconds.</summary>
        public IEnumerable<SimEvent> Window(CoachView view)
        {
            double periodSeconds = view.Clock.PeriodSeconds;
            double now = PublicLog.MatchSecond(view.Clock.Period, view.Clock.Time, periodSeconds);
            double from = now - Tuning.Ai.Adaptive.WindowSeconds;
            foreach (SimEvent e in view.Events)
            {
                if (PublicLog.MatchSecond(e.Context.Period, e.Context.Time, periodSeconds) > from)
                {
                    yield return e;
                }
            }
        }

        /// <summary>A-T step 1: P(s) = (n_s + prior / S) / (N + prior) over the loaded systems, in ordinal id order.</summary>
        public IReadOnlyDictionary<string, double> OpponentSystemDistribution(CoachView view)
        {
            return OpponentSystemDistribution(view, out _);
        }

        /// <summary>
        /// A-T step 3 (D-078 K2): the most probable opponent system, ties to the first in ordinal id order; null when the
        /// window has no observations (N = 0), since the prior alone gives every system the same P(s).
        /// </summary>
        public string MostProbableOpponentSystem(CoachView view)
        {
            IReadOnlyDictionary<string, double> p = OpponentSystemDistribution(view, out int observations);
            if (observations == 0)
            {
                return null;
            }

            string best = null;
            double bestP = double.NegativeInfinity;
            foreach (KeyValuePair<string, double> entry in p)
            {
                if (entry.Value > bestP)
                {
                    best = entry.Key;
                    bestP = entry.Value;
                }
            }

            return best;
        }

        /// <inheritdoc />
        protected override string TransitionOpponentSystem(CoachView view)
        {
            return MostProbableOpponentSystem(view);
        }

        private IReadOnlyDictionary<string, double> OpponentSystemDistribution(CoachView view, out int observations)
        {
            SortedSet<int> own = PublicLog.OwnSkaters(view);
            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (string id in Systems.Keys)
            {
                counts[id] = 0;
            }

            int total = 0;
            foreach (SimEvent e in Window(view))
            {
                string system = PublicLog.OpponentSystem(e, own);
                if (system != null && counts.ContainsKey(system))
                {
                    counts[system]++;
                    total++;
                }
            }

            double prior = Tuning.Ai.Adaptive.PriorWeight;
            int s = counts.Count;
            var p = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> count in counts)
            {
                p[count.Key] = total + prior > 0.0 ? (count.Value + (prior / s)) / (total + prior) : 1.0 / s;
            }

            observations = total;
            return p;
        }

        /// <inheritdoc />
        protected override IReadOnlyDictionary<string, double> PlayBonuses(CoachView view)
        {
            var bonus = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, double> p in OpponentSystemDistribution(view))
            {
                if (!Tuning.Ai.Rulebased.PlayBonusBySystem.TryGetValue(p.Key, out IReadOnlyDictionary<string, double> plays))
                {
                    continue;
                }

                foreach (KeyValuePair<string, double> play in plays)
                {
                    bonus.TryGetValue(play.Key, out double sum);
                    bonus[play.Key] = sum + (p.Value * play.Value);
                }
            }

            return bonus;
        }

        /// <summary>A-T step 2: q = (c + prior × 0.5) / (c + d + prior) from the opponent's entries and dump-ins.</summary>
        public double ControlledEntryShare(CoachView view)
        {
            SortedSet<int> own = PublicLog.OwnSkaters(view);
            int controlled = 0;
            int dumps = 0;
            foreach (SimEvent e in Window(view))
            {
                if (e is ControlledZoneEntryEvent entry && !own.Contains(entry.CarrierId))
                {
                    controlled++;
                }
                else if (e is DumpInEvent dump && !own.Contains(dump.ShooterId))
                {
                    dumps++;
                }
            }

            double prior = Tuning.Ai.Adaptive.PriorWeight;
            double denominator = controlled + dumps + prior;
            return denominator > 0.0 ? (controlled + (prior * PriorEntryShare)) / denominator : PriorEntryShare;
        }

        /// <inheritdoc />
        protected override string NormalSystem(CoachView view, string configured)
        {
            AiAdaptiveConfig adaptive = Tuning.Ai.Adaptive;
            double q = ControlledEntryShare(view);
            if (q >= adaptive.ControlledEntryShareForPassive)
            {
                _previousSystem = adaptive.PassiveSystem;
            }
            else if (q <= adaptive.ControlledEntryShareForAggressive)
            {
                _previousSystem = adaptive.AggressiveSystem;
            }
            else if (_previousSystem == null)
            {
                _previousSystem = configured;
            }

            return _previousSystem;
        }
    }
}
