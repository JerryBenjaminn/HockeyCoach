using System;

namespace HockeyCoach.Sim.Match
{
    /// <summary>The match clock as a coach sees it (D-077): period, period time and the period structure.</summary>
    public sealed class CoachClock
    {
        /// <summary>Creates the clock.</summary>
        /// <param name="period">Current period, starting at 1.</param>
        /// <param name="time">Period time in game seconds.</param>
        /// <param name="periods">Periods in a match.</param>
        /// <param name="periodSeconds">Length of a period in game seconds.</param>
        public CoachClock(int period, double time, int periods, double periodSeconds)
        {
            if (period < 1 || periods < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(period), "Periods start at 1.");
            }

            Period = period;
            Time = time;
            Periods = periods;
            PeriodSeconds = periodSeconds;
        }

        /// <summary>Current period, starting at 1.</summary>
        public int Period { get; }

        /// <summary>Period time in game seconds.</summary>
        public double Time { get; }

        /// <summary>Periods in a match.</summary>
        public int Periods { get; }

        /// <summary>Length of a period in game seconds.</summary>
        public double PeriodSeconds { get; }

        /// <summary>Game seconds left in the current period (never negative).</summary>
        public double SecondsLeftInPeriod
        {
            get { return Math.Max(0.0, PeriodSeconds - Time); }
        }
    }
}
