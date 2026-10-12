using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Presentation timing of one <see cref="TimelineSegment"/> (D-088 A, data-schema.md, replay.json, Esitysaika): its
    /// phases on the presentation time axis and how the match clock runs through them. Order inside the segment:
    /// optional frozen faceoff approach, dead time, other action, the action window (ends at <see cref="EventEnd"/>,
    /// where the clock equals the event's clock), then frozen holds (goal, stoppage, period break, timeout).
    /// </summary>
    public sealed class SegmentTiming
    {
        private readonly PresentationPhase[] _phases;

        /// <summary>Creates the timing (built by <see cref="PresentationTimeline"/>).</summary>
        public SegmentTiming(
            int index,
            double start,
            double gapStart,
            double windowStart,
            double eventEnd,
            double end,
            double clockStart,
            double clockEnd,
            GameSplit split,
            GapDurations durations,
            double puckFloorSeconds,
            double skaterFloorSeconds,
            SegmentTimingFlags flags,
            IReadOnlyList<PresentationPhase> phases)
        {
            if (phases == null)
            {
                throw new ArgumentNullException(nameof(phases));
            }

            Index = index;
            Start = start;
            GapStart = gapStart;
            WindowStart = windowStart;
            EventEnd = eventEnd;
            End = end;
            ClockStart = clockStart;
            ClockEnd = clockEnd;
            Split = split;
            Durations = durations;
            PuckFloorSeconds = puckFloorSeconds;
            SkaterFloorSeconds = skaterFloorSeconds;
            Flags = flags;
            _phases = new PresentationPhase[phases.Count];
            for (int i = 0; i < _phases.Length; i++)
            {
                _phases[i] = phases[i];
            }
        }

        /// <summary>Index of the segment (= event index).</summary>
        public int Index { get; }

        /// <summary>Presentation time where the segment starts (= end of the previous segment).</summary>
        public double Start { get; }

        /// <summary>Presentation time where the gap starts (after the faceoff approach, = <see cref="Start"/> without one).</summary>
        public double GapStart { get; }

        /// <summary>Presentation time where the action window starts; the puck path starts here.</summary>
        public double WindowStart { get; }

        /// <summary>Presentation time where the action window ends: the clock shows the event's clock from here on.</summary>
        public double EventEnd { get; }

        /// <summary>Presentation time where the segment ends (after the holds).</summary>
        public double End { get; }

        /// <summary>Presentation duration of the whole segment.</summary>
        public double Duration => End - Start;

        /// <summary>Presentation duration of the faceoff approach, 0 without one.</summary>
        public double ApproachSeconds => GapStart - Start;

        /// <summary>Match clock where the gap starts: event clock − g (the period start for a period's first event).</summary>
        public double ClockStart { get; }

        /// <summary>Match clock of the event.</summary>
        public double ClockEnd { get; }

        /// <summary>The gap split in game seconds (dead k, other r − k, action a).</summary>
        public GameSplit Split { get; }

        /// <summary>Presentation durations of the gap's phases after the floors (and goal slow motion).</summary>
        public GapDurations Durations { get; }

        /// <summary>The puck floor of the action window (presentation seconds, before slow motion).</summary>
        public double PuckFloorSeconds { get; }

        /// <summary>The skater floor of the gap (presentation seconds), 0 where it does not apply.</summary>
        public double SkaterFloorSeconds { get; }

        /// <summary>How the segment was classified.</summary>
        public SegmentTimingFlags Flags { get; }

        /// <summary>True when skater targets use <c>anticipationSeconds</c> in this gap (not into a stoppage, not in a stoppage series, not a faceoff gap, not at a period start).</summary>
        public bool UsesAnticipation => (Flags & SegmentTimingFlags.NoAnticipation) == 0;

        /// <summary>The non-empty phases in order.</summary>
        public IReadOnlyList<PresentationPhase> Phases => _phases;

        /// <summary>Match clock at presentation time <paramref name="time"/> (clamped to the segment).</summary>
        public double ClockAt(double time)
        {
            if (_phases.Length == 0 || time >= End)
            {
                return ClockEnd;
            }

            if (!(time > Start))
            {
                return ClockStart;
            }

            for (int i = 0; i < _phases.Length; i++)
            {
                if (time < _phases[i].End)
                {
                    return _phases[i].ClockAt(time);
                }
            }

            return ClockEnd;
        }

        /// <summary>
        /// Progress 0–1 of the segment's motion, from <see cref="Start"/> to <see cref="EventEnd"/> (1 during the holds
        /// and for empty segments).
        /// </summary>
        public double MotionFraction(double time)
        {
            if (!(EventEnd > Start) || time >= EventEnd)
            {
                return 1;
            }

            return time <= Start ? 0 : (time - Start) / (EventEnd - Start);
        }

        /// <summary>The earliest presentation time in the segment at which the clock reaches <paramref name="clock"/> (clamped).</summary>
        public double TimeAtClock(double clock)
        {
            if (clock >= ClockEnd)
            {
                return EventEnd;
            }

            foreach (PresentationPhase phase in _phases)
            {
                if (!phase.IsFrozen && clock <= phase.ClockEnd)
                {
                    return phase.TimeAt(clock);
                }
            }

            return GapStart;
        }
    }

    /// <summary>Classification of a segment by the presentation timing.</summary>
    [Flags]
    public enum SegmentTimingFlags
    {
        /// <summary>An ordinary gap.</summary>
        None = 0,

        /// <summary>First event of a period: players start at its positions, no skater floor.</summary>
        PeriodStart = 1,

        /// <summary>A <c>LineChange</c> or <c>Timeout</c> inside a stoppage series (same clock as the stoppage).</summary>
        StoppageSeries = 2,

        /// <summary>The <c>Faceoff</c> that ends a stoppage series, preceded by the frozen approach.</summary>
        FaceoffApproach = 4,

        /// <summary>A line change at a stoppage or at clock 0 of a period: lasts 0 s.</summary>
        InstantLineChange = 8,

        /// <summary>A flying line change (minimum <c>lineChangeSeconds</c>).</summary>
        FlyingLineChange = 16,

        /// <summary>A shot that ends in a goal: slowed window and goal hold.</summary>
        Goal = 32,

        /// <summary>Skater targets do not use anticipation in this gap.</summary>
        NoAnticipation = 64,
    }
}
