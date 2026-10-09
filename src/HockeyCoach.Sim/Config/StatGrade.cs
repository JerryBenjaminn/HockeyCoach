using System;

namespace HockeyCoach.Sim.Config
{
    /// <summary>One letter grade and its inclusive stat range (tuning.json <c>stats.grades</c>).</summary>
    public sealed class StatGrade
    {
        /// <summary>Creates a grade.</summary>
        public StatGrade(string name, int min, int max)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Min = min;
            Max = max;
        }

        /// <summary>Grade letter, e.g. "A".</summary>
        public string Name { get; }

        /// <summary>Lowest stat value of the grade.</summary>
        public int Min { get; }

        /// <summary>Highest stat value of the grade.</summary>
        public int Max { get; }
    }
}
