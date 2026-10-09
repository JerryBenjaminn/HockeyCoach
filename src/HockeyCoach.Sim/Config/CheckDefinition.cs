using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Definition of one check (tuning.json <c>checks.&lt;id&gt;</c>). Use <see cref="TuningValidator"/> to check it.
    /// Success is always the attacker side's outcome, also in one-sided checks (D-019).
    /// </summary>
    public sealed class CheckDefinition
    {
        private static readonly WeightTerm[] NoTerms = new WeightTerm[0];

        private readonly WeightTerm[] _attacker;
        private readonly WeightTerm[] _defender;
        private readonly SortedDictionary<string, double> _parameters;

        /// <summary>Creates a check definition.</summary>
        /// <param name="id">Check id, e.g. <c>pass</c>.</param>
        /// <param name="kind">Two-sided, one-sided or no check.</param>
        /// <param name="side">The present side of a one-sided check; null otherwise.</param>
        /// <param name="p0">Success probability when H = D and M = 0; null for <see cref="CheckKind.NoCheck"/>.</param>
        /// <param name="attacker">Weighted stats of the attacking side, in data order (empty if absent).</param>
        /// <param name="defender">Weighted stats of the defending side, in data order (empty if absent).</param>
        /// <param name="modifiers">Named modifiers; null means none.</param>
        /// <param name="parameters">Other numeric fields of the check (e.g. <c>noWinnerShare</c>); null means none.</param>
        public CheckDefinition(
            string id,
            CheckKind kind,
            CheckSide? side,
            double? p0,
            IReadOnlyList<WeightTerm> attacker,
            IReadOnlyList<WeightTerm> defender,
            CheckModifiers modifiers = null,
            IEnumerable<KeyValuePair<string, double>> parameters = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Kind = kind;
            Side = side;
            P0 = p0;
            _attacker = CopyTerms(attacker, nameof(attacker));
            _defender = CopyTerms(defender, nameof(defender));
            Modifiers = modifiers ?? CheckModifiers.None;
            _parameters = new SortedDictionary<string, double>(StringComparer.Ordinal);
            if (parameters != null)
            {
                foreach (KeyValuePair<string, double> pair in parameters)
                {
                    _parameters.Add(pair.Key, pair.Value);
                }
            }
        }

        /// <summary>Check id.</summary>
        public string Id { get; }

        /// <summary>Kind of the check.</summary>
        public CheckKind Kind { get; }

        /// <summary>Present side of a one-sided check; null for other kinds.</summary>
        public CheckSide? Side { get; }

        /// <summary>Base rate; null for <see cref="CheckKind.NoCheck"/>.</summary>
        public double? P0 { get; }

        /// <summary>Weighted stats of the attacking side.</summary>
        public IReadOnlyList<WeightTerm> Attacker
        {
            get { return _attacker; }
        }

        /// <summary>Weighted stats of the defending side.</summary>
        public IReadOnlyList<WeightTerm> Defender
        {
            get { return _defender; }
        }

        /// <summary>Named modifiers.</summary>
        public CheckModifiers Modifiers { get; }

        /// <summary>Other numeric fields of the check, in ordinal key order.</summary>
        public IReadOnlyDictionary<string, double> Parameters
        {
            get { return _parameters; }
        }

        /// <summary>Creates a two-sided check without modifiers.</summary>
        public static CheckDefinition TwoSided(string id, double p0, IReadOnlyList<WeightTerm> attacker, IReadOnlyList<WeightTerm> defender)
        {
            return new CheckDefinition(id, CheckKind.TwoSided, null, p0, attacker, defender);
        }

        /// <summary>Creates a one-sided check without modifiers; <paramref name="terms"/> belong to <paramref name="side"/>.</summary>
        public static CheckDefinition OneSided(string id, double p0, CheckSide side, IReadOnlyList<WeightTerm> terms)
        {
            return side == CheckSide.Attacker
                ? new CheckDefinition(id, CheckKind.OneSided, side, p0, terms, NoTerms)
                : new CheckDefinition(id, CheckKind.OneSided, side, p0, NoTerms, terms);
        }

        /// <summary>Returns a numeric parameter; throws if it is not defined.</summary>
        public double GetParameter(string name)
        {
            if (name != null && _parameters.TryGetValue(name, out double value))
            {
                return value;
            }

            throw new KeyNotFoundException("Check " + Id + " has no parameter " + name + ".");
        }

        private static WeightTerm[] CopyTerms(IReadOnlyList<WeightTerm> terms, string paramName)
        {
            if (terms == null)
            {
                throw new ArgumentNullException(paramName);
            }

            var copy = new WeightTerm[terms.Count];
            for (int i = 0; i < terms.Count; i++)
            {
                copy[i] = terms[i] ?? throw new ArgumentException("Null weight term.", paramName);
            }

            return copy;
        }
    }
}
