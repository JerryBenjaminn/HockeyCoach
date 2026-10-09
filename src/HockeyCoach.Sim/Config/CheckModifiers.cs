using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Named modifiers of a check in logit units (tuning.json <c>checks.&lt;id&gt;.modifiers</c>).
    /// Scalars and tables (indexed by distance in nodes, last value applies beyond) are kept apart.
    /// Both are enumerated in ordinal key order.
    /// </summary>
    public sealed class CheckModifiers
    {
        /// <summary>No modifiers.</summary>
        public static readonly CheckModifiers None = new CheckModifiers(new Dictionary<string, double>(), new Dictionary<string, double[]>());

        private readonly SortedDictionary<string, double> _scalars;
        private readonly SortedDictionary<string, double[]> _tables;

        /// <summary>Creates a modifier set.</summary>
        public CheckModifiers(IEnumerable<KeyValuePair<string, double>> scalars, IEnumerable<KeyValuePair<string, double[]>> tables)
        {
            if (scalars == null)
            {
                throw new ArgumentNullException(nameof(scalars));
            }

            if (tables == null)
            {
                throw new ArgumentNullException(nameof(tables));
            }

            _scalars = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, double> pair in scalars)
            {
                _scalars.Add(pair.Key, pair.Value);
            }

            _tables = new SortedDictionary<string, double[]>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, double[]> pair in tables)
            {
                if (pair.Value == null)
                {
                    throw new ArgumentException("Null modifier table " + pair.Key + ".", nameof(tables));
                }

                _tables.Add(pair.Key, (double[])pair.Value.Clone());
            }
        }

        /// <summary>Scalar modifiers by name, in ordinal key order.</summary>
        public IReadOnlyDictionary<string, double> Scalars
        {
            get { return _scalars; }
        }

        /// <summary>Names of table modifiers, in ordinal order.</summary>
        public IEnumerable<string> TableNames
        {
            get { return _tables.Keys; }
        }

        /// <summary>Returns a scalar modifier; throws if it is not defined.</summary>
        public double Get(string name)
        {
            if (name != null && _scalars.TryGetValue(name, out double value))
            {
                return value;
            }

            throw new KeyNotFoundException("Modifier " + name + " is not defined.");
        }

        /// <summary>Returns entry <paramref name="index"/> of a table modifier; indexes past the end use the last value.</summary>
        public double GetTable(string name, int index)
        {
            double[] table = Table(name);
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative.");
            }

            return table[index < table.Length ? index : table.Length - 1];
        }

        /// <summary>Returns a copy of a table modifier.</summary>
        public double[] GetTable(string name)
        {
            return (double[])Table(name).Clone();
        }

        private double[] Table(string name)
        {
            if (name != null && _tables.TryGetValue(name, out double[] table))
            {
                return table;
            }

            throw new KeyNotFoundException("Modifier table " + name + " is not defined.");
        }
    }
}
