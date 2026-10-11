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
    /// of weight <c>priorWeight</c>, and (2) in mode <c>normal</c> the own system follows the opponent's transition style
    /// against the current own system (A-T2, D-080 item 3, D-082), switching only at a stoppage. One instance per match
    /// (it remembers its current system).
    /// </summary>
    public sealed class AdaptiveTactics : RuleTactics
    {
        private string _current;
        private int _currentSince;

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

        /// <summary>
        /// The own system choice c of mode <c>normal</c> (A-T2 item 3); null before the first decision, when it starts as
        /// <c>modes.normal.system</c>.
        /// </summary>
        public string CurrentSystem
        {
            get { return _current; }
        }

        /// <summary>
        /// A-T2 item 3: the window of classified observations against <paramref name="current"/>: the last
        /// <paramref name="windowEvents"/> observations whose system id is <paramref name="current"/> and whose event index
        /// is at least <paramref name="sinceEventIndex"/> (the log length when <paramref name="current"/> was chosen, so
        /// the window is empty right after a switch; Q-069 default).
        /// </summary>
        public static IReadOnlyList<TransitionObservation> StyleWindow(
            IReadOnlyList<TransitionObservation> observations, string current, int sinceEventIndex, int windowEvents)
        {
            if (observations == null)
            {
                throw new ArgumentNullException(nameof(observations));
            }

            var window = new List<TransitionObservation>();
            for (int i = observations.Count - 1; i >= 0 && window.Count < windowEvents; i--)
            {
                TransitionObservation o = observations[i];
                if (o.EventIndex < sinceEventIndex)
                {
                    break;
                }

                if (string.Equals(o.SystemId, current, StringComparison.Ordinal))
                {
                    window.Add(o);
                }
            }

            window.Reverse();
            return window;
        }

        /// <summary>
        /// A-T2 item 4: the own system after a stoppage decision in mode <c>normal</c>. Fewer than <c>styleMinEvents</c>
        /// observations keep <paramref name="current"/>; otherwise rush share r ≥ <c>rushShareForPassive</c> → passive
        /// system, r ≤ <c>rushShareForAggressive</c> → aggressive system, else <paramref name="current"/> (hysteresis).
        /// </summary>
        public static string DecideSystem(IReadOnlyList<TransitionObservation> window, string current, AiAdaptiveConfig adaptive)
        {
            if (window == null || adaptive == null)
            {
                throw new ArgumentNullException(window == null ? nameof(window) : nameof(adaptive));
            }

            int n = window.Count;
            if (n < adaptive.StyleMinEvents)
            {
                return current;
            }

            int rushes = 0;
            foreach (TransitionObservation o in window)
            {
                rushes += o.Style == TransitionStyleClass.Rush ? 1 : 0;
            }

            double r = rushes / (double)n;
            if (r >= adaptive.RushShareForPassive)
            {
                return adaptive.PassiveSystem;
            }

            return r <= adaptive.RushShareForAggressive ? adaptive.AggressiveSystem : current;
        }

        /// <inheritdoc />
        protected override string NormalSystem(CoachView view, string configured)
        {
            AiAdaptiveConfig adaptive = Tuning.Ai.Adaptive;
            if (_current == null)
            {
                _current = configured;
                _currentSince = 0;
            }

            IReadOnlyList<TransitionObservation> observations = TransitionStyle.Classify(view.Events, PublicLog.OwnSkaters(view));
            IReadOnlyList<TransitionObservation> window = StyleWindow(observations, _current, _currentSince, adaptive.StyleWindowEvents);
            string next = DecideSystem(window, _current, adaptive);
            if (!string.Equals(next, _current, StringComparison.Ordinal))
            {
                _current = next;
                _currentSince = view.Events.Count;
            }

            return _current;
        }
    }
}
