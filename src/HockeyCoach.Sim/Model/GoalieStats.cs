using System;

namespace HockeyCoach.Sim.Model
{
    /// <summary>Immutable set of the 6 goalie stats.</summary>
    public sealed class GoalieStats
    {
        private readonly int[] _values;

        /// <summary>Creates a stat set. Values are on the internal 1–20 scale.</summary>
        public GoalieStats(
            int reflexes,
            int positioning,
            int mobility,
            int reboundControl,
            int puckHandling,
            int mentalToughness)
        {
            _values = new[] { reflexes, positioning, mobility, reboundControl, puckHandling, mentalToughness };
        }

        private GoalieStats(int[] values)
        {
            _values = values;
        }

        /// <summary>Returns the value of one stat.</summary>
        public int this[GoalieStat stat]
        {
            get { return _values[(int)stat]; }
        }

        /// <summary>Creates a stat set where every stat has the same value.</summary>
        public static GoalieStats Uniform(int value)
        {
            var values = new int[StatNames.GoalieStatCount];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = value;
            }

            return new GoalieStats(values);
        }

        /// <summary>Returns a copy with one stat replaced.</summary>
        public GoalieStats With(GoalieStat stat, int value)
        {
            var values = (int[])_values.Clone();
            values[(int)stat] = value;
            return new GoalieStats(values);
        }

        /// <summary>Returns a copy of all values in <see cref="GoalieStat"/> order.</summary>
        public int[] ToArray()
        {
            return (int[])_values.Clone();
        }

        /// <summary>Creates a stat set from values in <see cref="GoalieStat"/> order.</summary>
        public static GoalieStats FromArray(int[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Length != StatNames.GoalieStatCount)
            {
                throw new ArgumentException("Expected " + StatNames.GoalieStatCount + " goalie stats, got " + values.Length + ".", nameof(values));
            }

            return new GoalieStats((int[])values.Clone());
        }
    }
}
