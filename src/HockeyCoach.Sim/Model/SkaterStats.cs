using System;

namespace HockeyCoach.Sim.Model
{
    /// <summary>Immutable set of the 12 skater stats.</summary>
    public sealed class SkaterStats
    {
        private readonly int[] _values;

        /// <summary>Creates a stat set. Values are on the internal 1–20 scale.</summary>
        public SkaterStats(
            int speed,
            int agility,
            int endurance,
            int hands,
            int passing,
            int shotAccuracy,
            int shotPower,
            int positioning,
            int awareness,
            int strength,
            int discipline,
            int faceoffs)
        {
            _values = new[]
            {
                speed, agility, endurance, hands, passing, shotAccuracy,
                shotPower, positioning, awareness, strength, discipline, faceoffs,
            };
        }

        private SkaterStats(int[] values)
        {
            _values = values;
        }

        /// <summary>Returns the value of one stat.</summary>
        public int this[SkaterStat stat]
        {
            get { return _values[(int)stat]; }
        }

        /// <summary>Creates a stat set where every stat has the same value.</summary>
        public static SkaterStats Uniform(int value)
        {
            var values = new int[StatNames.SkaterStatCount];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = value;
            }

            return new SkaterStats(values);
        }

        /// <summary>Returns a copy with one stat replaced.</summary>
        public SkaterStats With(SkaterStat stat, int value)
        {
            var values = (int[])_values.Clone();
            values[(int)stat] = value;
            return new SkaterStats(values);
        }

        /// <summary>Returns a copy of all values in <see cref="SkaterStat"/> order.</summary>
        public int[] ToArray()
        {
            return (int[])_values.Clone();
        }

        /// <summary>Creates a stat set from values in <see cref="SkaterStat"/> order.</summary>
        public static SkaterStats FromArray(int[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Length != StatNames.SkaterStatCount)
            {
                throw new ArgumentException("Expected " + StatNames.SkaterStatCount + " skater stats, got " + values.Length + ".", nameof(values));
            }

            return new SkaterStats((int[])values.Clone());
        }
    }
}
