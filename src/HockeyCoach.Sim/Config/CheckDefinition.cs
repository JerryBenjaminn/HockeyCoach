using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Definition of one binary check (tuning.json <c>checks.*</c>): base rate p0 and the weighted stats of each side.
    /// Use <see cref="TuningValidator"/> to check it.
    /// </summary>
    public sealed class CheckDefinition
    {
        private readonly WeightTerm[] _attacker;
        private readonly WeightTerm[] _defender;

        /// <summary>Creates a check definition.</summary>
        /// <param name="id">Check id, e.g. <c>pass</c>.</param>
        /// <param name="p0">Success probability when H = D and M = 0.</param>
        /// <param name="attacker">Weighted stats of the attacking side, in data order.</param>
        /// <param name="defender">Weighted stats of the defending side, in data order.</param>
        public CheckDefinition(string id, double p0, IReadOnlyList<WeightTerm> attacker, IReadOnlyList<WeightTerm> defender)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            P0 = p0;
            _attacker = CopyTerms(attacker, nameof(attacker));
            _defender = CopyTerms(defender, nameof(defender));
        }

        /// <summary>Check id.</summary>
        public string Id { get; }

        /// <summary>Success probability when H = D and M = 0.</summary>
        public double P0 { get; }

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
