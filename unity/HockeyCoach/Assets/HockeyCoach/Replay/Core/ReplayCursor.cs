using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Deterministic playback controller over a <see cref="ReplayTimeline"/>. Its position is a match clock and the
    /// index of the current segment (needed because several events can share one second). While playing,
    /// <see cref="Advance"/> moves the clock by real seconds × <see cref="Speed"/>; nothing reads the wall clock or a
    /// random source, so the same calls always give the same states. When the clock reaches the end of a segment the
    /// cursor moves on through any zero-length segments at that second, so the sample shows the state after all events
    /// up to the clock.
    /// </summary>
    public sealed class ReplayCursor
    {
        private double _speed = 1;

        /// <summary>Creates a paused cursor at the start of the timeline, speed 1×.</summary>
        public ReplayCursor(ReplayTimeline timeline)
        {
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            Clock = timeline.StartClock;
            SegmentIndex = 0;
            Normalize();
        }

        /// <summary>The timeline played.</summary>
        public ReplayTimeline Timeline { get; }

        /// <summary>Match clock of the cursor.</summary>
        public double Clock { get; private set; }

        /// <summary>Index of the current segment.</summary>
        public int SegmentIndex { get; private set; }

        /// <summary>The current segment.</summary>
        public TimelineSegment Segment => Timeline.Segments[SegmentIndex];

        /// <summary>True while playing.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>True when the cursor is at the end of the last segment.</summary>
        public bool IsAtEnd => SegmentIndex == Timeline.Segments.Count - 1 && Clock >= Timeline.EndClock;

        /// <summary>Match seconds per real second (1×, 2×, 4×, 8×, ...); must be positive.</summary>
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
        public double RemainingWallSeconds => (Timeline.EndClock - Clock) / _speed;

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
        /// Moves the clock by <paramref name="realSeconds"/> × <see cref="Speed"/> while playing (no effect when paused).
        /// Stops at the end of the timeline and pauses there.
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

            Clock = Math.Min(Clock + (realSeconds * _speed), Timeline.EndClock);
            Normalize();
            if (IsAtEnd)
            {
                IsPlaying = false;
            }
        }

        /// <summary>Jumps to the end of the next segment (the state after the next event). Play state is kept.</summary>
        public void StepNext()
        {
            SeekEvent(Math.Min(SegmentIndex + 1, Timeline.Segments.Count - 1));
        }

        /// <summary>Jumps to the end of the previous segment (the state after the previous event). Play state is kept.</summary>
        public void StepPrevious()
        {
            SeekEvent(Math.Max(SegmentIndex - 1, 0));
        }

        /// <summary>Jumps to the end of segment <paramref name="index"/> (the state right after that event). Play state is kept.</summary>
        public void SeekEvent(int index)
        {
            if (index < 0 || index >= Timeline.Segments.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Event index is outside the timeline.");
            }

            SegmentIndex = index;
            Clock = Timeline.Segments[index].EndClock;
            if (IsAtEnd)
            {
                IsPlaying = false;
            }
        }

        /// <summary>Jumps to match clock <paramref name="clock"/> (clamped to the timeline). Play state is kept.</summary>
        public void SeekClock(double clock)
        {
            if (double.IsNaN(clock))
            {
                throw new ArgumentOutOfRangeException(nameof(clock), clock, "Clock must be a number.");
            }

            Clock = Math.Max(Timeline.StartClock, Math.Min(clock, Timeline.EndClock));
            SegmentIndex = 0;
            Normalize();
            if (IsAtEnd)
            {
                IsPlaying = false;
            }
        }

        /// <summary>The interpolated state at the cursor.</summary>
        public ReplaySample Sample()
        {
            TimelineSegment segment = Segment;
            double fraction = segment.Duration > 0 ? (Clock - segment.StartClock) / segment.Duration : 1;
            fraction = Math.Max(0, Math.Min(1, fraction));
            var players = new PlayerSample[segment.Players.Count];
            for (int i = 0; i < players.Length; i++)
            {
                SegmentPlayer p = segment.Players[i];
                players[i] = new PlayerSample(p.Id, p.Team, p.IsGoalie, RinkPoint.Lerp(p.From, p.To, fraction));
            }

            bool done = fraction >= 1;
            int? carrier = done || !segment.Puck.Moves ? segment.Puck.CarrierId : null;
            double periodTime = Math.Max(0, Clock - ((double)(segment.Period - 1) * Timeline.PeriodSeconds));
            return new ReplaySample(
                SegmentIndex,
                fraction,
                Clock,
                segment.Period,
                periodTime,
                done ? segment.HomeScore : segment.HomeScoreBefore,
                done ? segment.AwayScore : segment.AwayScoreBefore,
                players,
                segment.Puck.PositionAt(fraction),
                carrier);
        }

        private void Normalize()
        {
            var segments = Timeline.Segments;
            while (SegmentIndex < segments.Count - 1
                && (Clock > segments[SegmentIndex].EndClock
                    || (Clock >= segments[SegmentIndex].EndClock && segments[SegmentIndex + 1].Duration <= 0)))
            {
                SegmentIndex++;
            }
        }
    }
}
