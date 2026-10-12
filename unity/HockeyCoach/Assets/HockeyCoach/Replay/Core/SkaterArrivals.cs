using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Target and arrival times of a skater's path (data-schema.md, replay.json, Luistelijat: Solmukohteet,
    /// Myöhästyminen). Computed once when the timeline is built, so sampling never depends on earlier frames.
    /// </summary>
    public static class SkaterArrivals
    {
        /// <summary>
        /// The target time of a gap: the end of its action window (<see cref="SegmentTiming.EventEnd"/>) minus
        /// <c>skaters.anticipationSeconds</c> where the gap uses anticipation (<see cref="SegmentTiming.UsesAnticipation"/>).
        /// </summary>
        public static double TargetTime(SegmentTiming timing, SkaterSettings skaters)
        {
            if (timing == null)
            {
                throw new ArgumentNullException(nameof(timing));
            }

            if (skaters == null)
            {
                throw new ArgumentNullException(nameof(skaters));
            }

            return timing.UsesAnticipation ? timing.EventEnd - skaters.AnticipationSeconds : timing.EventEnd;
        }

        /// <summary>
        /// Arrival times T'(i) = max(target(i), T'(i − 1) + length(i) / <paramref name="topSpeed"/>); the first target and
        /// every target with <paramref name="reset"/> set (the end of a faceoff approach) are reached on time.
        /// </summary>
        /// <param name="targetTimes">Target times in presentation seconds, one per leg end.</param>
        /// <param name="legLengths">Length in metres of the leg ending at each target (the first is ignored).</param>
        /// <param name="topSpeed"><c>skaters.topSpeed</c>.</param>
        /// <param name="reset">Targets where lateness resets, or null for none.</param>
        public static double[] Compute(IReadOnlyList<double> targetTimes, IReadOnlyList<double> legLengths, double topSpeed, IReadOnlyList<bool> reset)
        {
            if (targetTimes == null)
            {
                throw new ArgumentNullException(nameof(targetTimes));
            }

            if (legLengths == null || legLengths.Count != targetTimes.Count)
            {
                throw new ArgumentException("Need one leg length per target.", nameof(legLengths));
            }

            if (reset != null && reset.Count != targetTimes.Count)
            {
                throw new ArgumentException("Need one reset flag per target.", nameof(reset));
            }

            if (!(topSpeed > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(topSpeed), topSpeed, "Top speed must be positive.");
            }

            var arrivals = new double[targetTimes.Count];
            for (int i = 0; i < arrivals.Length; i++)
            {
                bool fresh = i == 0 || (reset != null && reset[i]);
                arrivals[i] = fresh ? targetTimes[i] : Math.Max(targetTimes[i], arrivals[i - 1] + (legLengths[i] / topSpeed));
            }

            return arrivals;
        }
    }
}
