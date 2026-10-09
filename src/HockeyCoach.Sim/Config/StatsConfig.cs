using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary>Stat scale and grades (tuning.json <c>stats</c>).</summary>
    public sealed class StatsConfig
    {
        private readonly StatGrade[] _grades;

        /// <summary>Creates the stat scale.</summary>
        public StatsConfig(int min, int max, IReadOnlyList<StatGrade> grades)
        {
            Min = min;
            Max = max;
            if (grades == null)
            {
                throw new ArgumentNullException(nameof(grades));
            }

            _grades = new StatGrade[grades.Count];
            for (int i = 0; i < grades.Count; i++)
            {
                _grades[i] = grades[i] ?? throw new ArgumentException("Null grade.", nameof(grades));
            }
        }

        /// <summary>Lowest stat value.</summary>
        public int Min { get; }

        /// <summary>Highest stat value.</summary>
        public int Max { get; }

        /// <summary>Grades in the order given (the loader passes them in ordinal name order).</summary>
        public IReadOnlyList<StatGrade> Grades
        {
            get { return _grades; }
        }
    }
}
