namespace HockeyCoach.Replay.Core
{
    /// <summary>Kinds of presentation phases (data-schema.md, replay.json, Esitysaika).</summary>
    public enum PresentationPhaseKind
    {
        /// <summary>Frozen approach of all skaters to the faceoff positions (after a stoppage series and its changes).</summary>
        FaceoffApproach,

        /// <summary>Frozen line changes of a stoppage series before the approach (D-089 A1): out to the bench, then in.</summary>
        StoppageChange,

        /// <summary>Dead time of a gap (setup, regroup), compressed by <c>deadCompression</c>.</summary>
        DeadTime,

        /// <summary>Other action of a gap, compressed by <c>actionCompression</c>.</summary>
        OtherAction,

        /// <summary>The event's own action window; the puck path starts at its beginning. Slowed for goals.</summary>
        ActionWindow,

        /// <summary>Frozen hold after a goal.</summary>
        GoalHold,

        /// <summary>Frozen hold after a stoppage.</summary>
        StoppageHold,

        /// <summary>Frozen break after a period end.</summary>
        PeriodBreak,

        /// <summary>Frozen timeout.</summary>
        TimeoutHold,
    }

    /// <summary>
    /// One phase of a segment's presentation: a stretch of presentation time [<see cref="Start"/>, <see cref="End"/>]
    /// over which the match clock runs linearly from <see cref="ClockStart"/> to <see cref="ClockEnd"/> (equal when
    /// the phase is frozen).
    /// </summary>
    public sealed class PresentationPhase
    {
        /// <summary>Creates a phase.</summary>
        public PresentationPhase(PresentationPhaseKind kind, int segmentIndex, double start, double end, double clockStart, double clockEnd)
        {
            Kind = kind;
            SegmentIndex = segmentIndex;
            Start = start;
            End = end;
            ClockStart = clockStart;
            ClockEnd = clockEnd;
        }

        /// <summary>Kind of the phase.</summary>
        public PresentationPhaseKind Kind { get; }

        /// <summary>Index of the segment the phase belongs to.</summary>
        public int SegmentIndex { get; }

        /// <summary>Presentation time where the phase starts.</summary>
        public double Start { get; }

        /// <summary>Presentation time where the phase ends.</summary>
        public double End { get; }

        /// <summary>Presentation duration.</summary>
        public double Duration => End - Start;

        /// <summary>Match clock at <see cref="Start"/>.</summary>
        public double ClockStart { get; }

        /// <summary>Match clock at <see cref="End"/>.</summary>
        public double ClockEnd { get; }

        /// <summary>True when the clock does not run in this phase.</summary>
        public bool IsFrozen => ClockEnd <= ClockStart;

        /// <summary>Match clock at presentation time <paramref name="time"/> (clamped to the phase).</summary>
        public double ClockAt(double time)
        {
            if (IsFrozen)
            {
                return ClockStart;
            }

            if (time >= End)
            {
                return ClockEnd;
            }

            if (!(time > Start))
            {
                return ClockStart;
            }

            return ClockStart + ((ClockEnd - ClockStart) * ((time - Start) / (End - Start)));
        }

        /// <summary>The earliest presentation time in the phase at which the clock reaches <paramref name="clock"/> (clamped).</summary>
        public double TimeAt(double clock)
        {
            if (IsFrozen || clock <= ClockStart)
            {
                return Start;
            }

            if (clock >= ClockEnd)
            {
                return End;
            }

            return Start + ((End - Start) * ((clock - ClockStart) / (ClockEnd - ClockStart)));
        }
    }
}
