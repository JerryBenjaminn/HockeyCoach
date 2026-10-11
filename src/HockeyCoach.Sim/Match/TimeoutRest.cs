using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Checks;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.State;

namespace HockeyCoach.Sim.Match
{
    /// <summary>The energy effect of a timeout (AI-4).</summary>
    public static class TimeoutRest
    {
        /// <summary>
        /// Rests every skater: a benched skater's bench recovery up to <paramref name="time"/> is realized first and his
        /// bench clock restarts at <paramref name="time"/>; then E ← 1 − (1 − E) × exp(−rate × rest) for everyone.
        /// </summary>
        /// <param name="energy">Energy state.</param>
        /// <param name="skaters">Skaters of one team (on the ice and on the bench).</param>
        /// <param name="time">Period time of the stoppage.</param>
        /// <param name="rate"><c>energy.benchRecoveryRate</c>.</param>
        /// <param name="restSeconds"><c>time.timeoutRestSeconds</c>.</param>
        public static void Apply(EnergyState energy, IEnumerable<Skater> skaters, double time, double rate, double restSeconds)
        {
            if (energy == null || skaters == null)
            {
                throw new ArgumentNullException(energy == null ? nameof(energy) : nameof(skaters));
            }

            foreach (Skater skater in skaters)
            {
                double value = energy.Get(skater.Id);
                if (energy.IsBenched(skater.Id, out double since))
                {
                    value = StateDynamics.BenchRecovered(value, time - since, rate);
                    energy.Bench(skater.Id, time);
                }

                energy.Set(skater.Id, StateDynamics.BenchRecovered(value, restSeconds, rate));
            }
        }
    }
}
