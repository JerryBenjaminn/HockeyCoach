using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Deterministic playback controller over a <see cref="PresentationTimeline"/> (D-088 A). Its position is a
    /// presentation time (seconds on screen at 1×) and the index of the current segment (needed because several events
    /// can share one instant). While playing, <see cref="Advance"/> moves the presentation time by real seconds ×
    /// <see cref="Speed"/>; the match <see cref="Clock"/> is derived from it through the warp, shows the true match time
    /// and never runs backwards. Nothing reads the wall clock or a random source, so the same calls always give the same
    /// states, and seeking to a time gives the same sample as playing to it.
    /// </summary>
    public sealed class ReplayCursor
    {
        private double _speed = 1;

        /// <summary>Creates a paused cursor at the start of the match, speed 1×.</summary>
        public ReplayCursor(PresentationTimeline presentation)
        {
            Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            Time = 0;
            SegmentIndex = presentation.Normalize(0, 0);
        }

        /// <summary>The presentation warp played.</summary>
        public PresentationTimeline Presentation { get; }

        /// <summary>The timeline on the match clock.</summary>
        public ReplayTimeline Timeline => Presentation.Timeline;

        /// <summary>Presentation time of the cursor (seconds on screen at 1×).</summary>
        public double Time { get; private set; }

        /// <summary>Match clock (seconds from the start of the match) at the cursor.</summary>
        public double Clock => Presentation.ClockAt(SegmentIndex, Time);

        /// <summary>Index of the current segment.</summary>
        public int SegmentIndex { get; private set; }

        /// <summary>The current segment.</summary>
        public TimelineSegment Segment => Timeline.Segments[SegmentIndex];

        /// <summary>Timing of the current segment.</summary>
        public SegmentTiming Timing => Presentation.Segments[SegmentIndex];

        /// <summary>True while playing.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>True when the cursor is at the end of the last segment.</summary>
        public bool IsAtEnd => SegmentIndex == Timeline.Segments.Count - 1 && Time >= Presentation.Duration;

        /// <summary>Speed multiplier of the presentation time (1×, 2×, 4×, 8×, ...); must be positive.</summary>
        public double Speed
        {
            get => _speed;
            set
            {
                if (!(value > 0) || double.IsInfinity(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Speed must be positive and finite.");
                }

                _speed = value;
            }
        }

        /// <summary>Wall-clock seconds left to the end at the current <see cref="Speed"/>.</summary>
        public double RemainingWallSeconds => (Presentation.Duration - Time) / _speed;

        /// <summary>Starts playing (no effect at the end).</summary>
        public void Play()
        {
            IsPlaying = !IsAtEnd;
        }

        /// <summary>Pauses.</summary>
        public void Pause()
        {
            IsPlaying = false;
        }

        /// <summary>Toggles play and pause.</summary>
        public void TogglePlay()
        {
            if (IsPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        /// <summary>
        /// Moves the presentation time by <paramref name="realSeconds"/> × <see cref="Speed"/> while playing (no effect
        /// when paused). Stops at the end of the match and pauses there.
        /// </summary>
        public void Advance(double realSeconds)
        {
            if (!(realSeconds >= 0) || double.IsInfinity(realSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(realSeconds), realSeconds, "Real seconds must be non-negative and finite.");
            }

            if (!IsPlaying)
            {
                return;
            }

            Time = Math.Min(Time + (realSeconds * _speed), Presentation.Duration);
            SegmentIndex = Presentation.Normalize(SegmentIndex, Time);
            if (IsAtEnd)
            {
                IsPlaying = false;
            }
        }

        /// <summary>Jumps to the end of the next event's action window (the state after the next event). Play state is kept.</summary>
        public void StepNext()
        {
            SeekEvent(Math.Min(SegmentIndex + 1, Timeline.Segments.Count - 1));
        }

        /// <summary>Jumps to the end of the previous event's action window. Play state is kept.</summary>
        public void StepPrevious()
        {
            SeekEvent(Math.Max(SegmentIndex - 1, 0));
        }

        /// <summary>
        /// Jumps to the end of the action window of segment <paramref name="index"/>, where the clock shows that event's
        /// clock and the state is right after it. Play state is kept.
        /// </summary>
        public void SeekEvent(int index)
        {
            if (index < 0 || index >= Timeline.Segments.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Event index is outside the timeline.");
            }

            SegmentIndex = index;
            Time = Presentation.Segments[index].EventEnd;
            StopAtEnd();
        }

        /// <summary>
        /// Jumps to the earliest presentation time that shows the state after every event up to match clock
        /// <paramref name="clock"/> (clamped to the match). Play state is kept.
        /// </summary>
        public void SeekClock(double clock)
        {
            if (double.IsNaN(clock))
            {
                throw new ArgumentOutOfRangeException(nameof(clock), clock, "Clock must be a number.");
            }

            int index;
            Time = Presentation.TimeAtClock(clock, out index);
            SegmentIndex = index;
            StopAtEnd();
        }

        /// <summary>Jumps to presentation time <paramref name="time"/> (clamped to the match). Play state is kept.</summary>
        public void SeekTime(double time)
        {
            if (double.IsNaN(time))
            {
                throw new ArgumentOutOfRangeException(nameof(time), time, "Time must be a number.");
            }

            Time = Math.Max(0, Math.Min(time, Presentation.Duration));
            SegmentIndex = Presentation.SegmentIndexAt(Time);
            StopAtEnd();
        }

        /// <summary>The state at the cursor.</summary>
        public ReplaySample Sample()
        {
            TimelineSegment segment = Segment;
            SegmentTiming timing = Timing;
            double fraction = timing.MotionFraction(Time);
            MotionSample motion = Presentation.Motion.Sample(SegmentIndex, Time);
            bool done = fraction >= 1;
            double clock = Clock;
            double periodTime = Math.Max(0, clock - ((double)(segment.Period - 1) * Timeline.PeriodSeconds));
            return new ReplaySample(
                SegmentIndex,
                fraction,
                Time,
                clock,
                segment.Period,
                periodTime,
                done ? segment.HomeScore : segment.HomeScoreBefore,
                done ? segment.AwayScore : segment.AwayScoreBefore,
                motion.Players,
                motion.Leaving,
                motion.Puck,
                motion.PuckCarrierId);
        }

        private void StopAtEnd()
        {
            if (IsAtEnd)
            {
                IsPlaying = false;
            }
        }
    }
}
