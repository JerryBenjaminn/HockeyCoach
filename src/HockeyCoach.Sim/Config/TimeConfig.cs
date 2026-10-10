using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Time values from tuning.json <c>time</c> (docs/data-schema.md), all in game seconds. Forward trios and defence
    /// pairs rotate separately (D-023). Setup and regroup times follow Q-006 (D-038: setup uses the shift clock).
    /// </summary>
    public sealed class TimeConfig
    {
        /// <summary>
        /// <c>secondsPerAction</c> keys the play actions use (data-schema.md, Kiekkotoiminnot): skate, pass, shoot,
        /// driveNet, dumpIn and loosePuck (after a dump). The validator requires them.
        /// </summary>
        public static readonly IReadOnlyList<string> PlayActionKeys = new[] { "driveNet", "dumpIn", "loosePuck", "pass", "shoot", "skate" };

        private readonly SortedDictionary<string, double> _secondsPerAction;

        /// <summary>Creates the time config. Use <see cref="TuningValidator"/> to check it.</summary>
        /// <param name="periods">Number of periods.</param>
        /// <param name="periodSeconds">Length of a period.</param>
        /// <param name="forwardShiftSeconds">Target shift length of a forward trio.</param>
        /// <param name="defenceShiftSeconds">Target shift length of a defence pair.</param>
        /// <param name="secondsPerAction">Time cost per action or check name.</param>
        /// <param name="setupSeconds">Time for players to reach a play's start nodes (Q-006, D-038).</param>
        /// <param name="regroupSeconds">Time of a regroup transition (Q-006).</param>
        /// <param name="stoppageChangeMinSeconds">A unit may change at a stoppage after this much ice time (O-2).</param>
        public TimeConfig(
            int periods,
            double periodSeconds,
            double forwardShiftSeconds,
            double defenceShiftSeconds,
            IEnumerable<KeyValuePair<string, double>> secondsPerAction,
            double setupSeconds,
            double regroupSeconds,
            double stoppageChangeMinSeconds)
        {
            if (secondsPerAction == null)
            {
                throw new ArgumentNullException(nameof(secondsPerAction));
            }

            Periods = periods;
            PeriodSeconds = periodSeconds;
            ForwardShiftSeconds = forwardShiftSeconds;
            DefenceShiftSeconds = defenceShiftSeconds;
            SetupSeconds = setupSeconds;
            RegroupSeconds = regroupSeconds;
            StoppageChangeMinSeconds = stoppageChangeMinSeconds;
            _secondsPerAction = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, double> entry in secondsPerAction)
            {
                _secondsPerAction[entry.Key] = entry.Value;
            }
        }

        /// <summary>Number of periods.</summary>
        public int Periods { get; }

        /// <summary>Length of a period.</summary>
        public double PeriodSeconds { get; }

        /// <summary>Target shift length of a forward trio.</summary>
        public double ForwardShiftSeconds { get; }

        /// <summary>Target shift length of a defence pair.</summary>
        public double DefenceShiftSeconds { get; }

        /// <summary>Time for players to reach a play's start nodes.</summary>
        public double SetupSeconds { get; }

        /// <summary>Time of a regroup transition.</summary>
        public double RegroupSeconds { get; }

        /// <summary>A unit may change at a stoppage after this much ice time (O-2).</summary>
        public double StoppageChangeMinSeconds { get; }

        /// <summary>Time cost per action or check name, in ordinal key order.</summary>
        public IReadOnlyDictionary<string, double> SecondsPerAction
        {
            get { return _secondsPerAction; }
        }

        /// <summary>Time cost of an action; throws if the key is not in tuning.json.</summary>
        public double GetSecondsPerAction(string action)
        {
            if (action != null && _secondsPerAction.TryGetValue(action, out double seconds))
            {
                return seconds;
            }

            throw new KeyNotFoundException("Unknown time.secondsPerAction." + action + ".");
        }
    }
}
