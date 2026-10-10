using System.Collections.Generic;

namespace HockeyCoach.Sim.Match
{
    /// <summary>
    /// State values at the end of a period that the event log does not carry (period summary, O-1): energy, ice time and
    /// shifts per unit, offensive-zone time and play uses. Ice time, shifts and zone time are cumulative for the match.
    /// </summary>
    public sealed class PeriodSnapshot
    {
        /// <summary>Creates the snapshot.</summary>
        public PeriodSnapshot(
            int period,
            IReadOnlyDictionary<int, double> energy,
            double[][] forwardSeconds,
            double[][] pairSeconds,
            int[][] forwardShifts,
            int[][] pairShifts,
            double[] offensiveZoneSeconds,
            IReadOnlyDictionary<string, double>[] playUses)
        {
            Period = period;
            Energy = energy;
            ForwardSeconds = forwardSeconds;
            PairSeconds = pairSeconds;
            ForwardShifts = forwardShifts;
            PairShifts = pairShifts;
            OffensiveZoneSeconds = offensiveZoneSeconds;
            PlayUses = playUses;
        }

        /// <summary>Period number.</summary>
        public int Period { get; }

        /// <summary>Energy of every player at the period end (bench recovery applied), by id.</summary>
        public IReadOnlyDictionary<int, double> Energy { get; }

        /// <summary>[team][trio] cumulative ice seconds.</summary>
        public double[][] ForwardSeconds { get; }

        /// <summary>[team][pair] cumulative ice seconds.</summary>
        public double[][] PairSeconds { get; }

        /// <summary>[team][trio] cumulative shifts.</summary>
        public int[][] ForwardShifts { get; }

        /// <summary>[team][pair] cumulative shifts.</summary>
        public int[][] PairShifts { get; }

        /// <summary>[team] cumulative offensive-zone seconds.</summary>
        public double[] OffensiveZoneSeconds { get; }

        /// <summary>[team] play uses at the period end, before any intermission multiplier.</summary>
        public IReadOnlyDictionary<string, double>[] PlayUses { get; }
    }
}
