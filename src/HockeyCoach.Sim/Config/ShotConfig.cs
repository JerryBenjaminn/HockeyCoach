using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// The shot goal check (tuning.json <c>checks.shot</c>). Base rate and shooter weights depend on the xG zone
    /// of the shooting node; the goal probability is clamped to this config's own bounds, not to the check
    /// formula clamp (D-014).
    /// </summary>
    public sealed class ShotConfig
    {
        /// <summary>Check id used in results.</summary>
        public const string Id = "shot";

        private readonly SortedDictionary<string, WeightTerm[]> _attackerByXgZone;
        private readonly SortedDictionary<string, double> _baseXg;
        private readonly WeightTerm[] _defender;
        private readonly SortedDictionary<string, double> _parameters;

        /// <summary>Creates the shot config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="attackerByXgZone">Shooter weights per xG zone.</param>
        /// <param name="defender">Goalie-side weights.</param>
        /// <param name="baseXg">Base goal probability per xG zone.</param>
        /// <param name="onTargetShare">Share of unblocked shots on target.</param>
        /// <param name="minProbability">Lower bound of goal probability and xG.</param>
        /// <param name="maxProbability">Upper bound of goal probability and xG.</param>
        /// <param name="modifiers">Named modifiers; null means none.</param>
        /// <param name="parameters">Other numeric fields; null means none.</param>
        public ShotConfig(
            IEnumerable<KeyValuePair<string, IReadOnlyList<WeightTerm>>> attackerByXgZone,
            IReadOnlyList<WeightTerm> defender,
            IEnumerable<KeyValuePair<string, double>> baseXg,
            double onTargetShare,
            double minProbability,
            double maxProbability,
            CheckModifiers modifiers = null,
            IEnumerable<KeyValuePair<string, double>> parameters = null)
        {
            if (attackerByXgZone == null)
            {
                throw new ArgumentNullException(nameof(attackerByXgZone));
            }

            if (defender == null)
            {
                throw new ArgumentNullException(nameof(defender));
            }

            if (baseXg == null)
            {
                throw new ArgumentNullException(nameof(baseXg));
            }

            _attackerByXgZone = new SortedDictionary<string, WeightTerm[]>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IReadOnlyList<WeightTerm>> pair in attackerByXgZone)
            {
                _attackerByXgZone.Add(pair.Key, ToArray(pair.Value, nameof(attackerByXgZone)));
            }

            _defender = ToArray(defender, nameof(defender));

            _baseXg = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, double> pair in baseXg)
            {
                _baseXg.Add(pair.Key, pair.Value);
            }

            _parameters = new SortedDictionary<string, double>(StringComparer.Ordinal);
            if (parameters != null)
            {
                foreach (KeyValuePair<string, double> pair in parameters)
                {
                    _parameters.Add(pair.Key, pair.Value);
                }
            }

            OnTargetShare = onTargetShare;
            MinProbability = minProbability;
            MaxProbability = maxProbability;
            Modifiers = modifiers ?? CheckModifiers.None;
        }

        /// <summary>Shooter weights per xG zone, in ordinal zone order.</summary>
        public IReadOnlyDictionary<string, WeightTerm[]> AttackerByXgZone
        {
            get { return _attackerByXgZone; }
        }

        /// <summary>Goalie-side weights.</summary>
        public IReadOnlyList<WeightTerm> Defender
        {
            get { return _defender; }
        }

        /// <summary>Base goal probability per xG zone, in ordinal zone order.</summary>
        public IReadOnlyDictionary<string, double> BaseXg
        {
            get { return _baseXg; }
        }

        /// <summary>Share of unblocked shots that go on target (Q-010).</summary>
        public double OnTargetShare { get; }

        /// <summary>Lower bound of the goal probability and xG (D-014).</summary>
        public double MinProbability { get; }

        /// <summary>Upper bound of the goal probability and xG (D-014).</summary>
        public double MaxProbability { get; }

        /// <summary>Named modifiers.</summary>
        public CheckModifiers Modifiers { get; }

        /// <summary>Other numeric fields, in ordinal key order.</summary>
        public IReadOnlyDictionary<string, double> Parameters
        {
            get { return _parameters; }
        }

        /// <summary>
        /// The two-sided goal check for a shot from the given xG zone: p0 = baseXg[zone], shooter weights for that zone.
        /// </summary>
        public CheckDefinition ForXgZone(string xgZone)
        {
            if (xgZone == null || !_baseXg.TryGetValue(xgZone, out double p0) || !_attackerByXgZone.TryGetValue(xgZone, out WeightTerm[] attacker))
            {
                throw new KeyNotFoundException("Unknown xG zone " + xgZone + ".");
            }

            return new CheckDefinition(Id, CheckKind.TwoSided, null, p0, attacker, _defender, Modifiers);
        }

        private static WeightTerm[] ToArray(IReadOnlyList<WeightTerm> terms, string paramName)
        {
            if (terms == null)
            {
                throw new ArgumentNullException(paramName);
            }

            var copy = new WeightTerm[terms.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = terms[i] ?? throw new ArgumentException("Null weight term.", paramName);
            }

            return copy;
        }
    }
}
