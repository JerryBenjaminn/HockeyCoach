using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Match
{
    /// <summary>The coach's own ice and bench time per trio and pair (AI-1, AI-5). A copy; it does not change after it is made.</summary>
    public sealed class IceTimeView
    {
        private readonly UnitIceTime[] _trios;
        private readonly UnitIceTime[] _pairs;

        /// <summary>Creates the view; the lists are copied.</summary>
        /// <param name="trios">One entry per forward trio, by index.</param>
        /// <param name="pairs">One entry per defence pair, by index.</param>
        public IceTimeView(IReadOnlyList<UnitIceTime> trios, IReadOnlyList<UnitIceTime> pairs)
        {
            _trios = Copy(trios, nameof(trios));
            _pairs = Copy(pairs, nameof(pairs));
        }

        /// <summary>Forward trios by index.</summary>
        public IReadOnlyList<UnitIceTime> Trios
        {
            get { return _trios; }
        }

        /// <summary>Defence pairs by index.</summary>
        public IReadOnlyList<UnitIceTime> Pairs
        {
            get { return _pairs; }
        }

        private static UnitIceTime[] Copy(IReadOnlyList<UnitIceTime> source, string name)
        {
            if (source == null)
            {
                throw new ArgumentNullException(name);
            }

            var copy = new UnitIceTime[source.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = source[i] ?? throw new ArgumentException("Null entry.", name);
            }

            return copy;
        }
    }
}
