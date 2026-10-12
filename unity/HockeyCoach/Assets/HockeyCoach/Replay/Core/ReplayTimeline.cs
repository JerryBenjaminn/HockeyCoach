using System;
using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// The whole match as an ordered list of <see cref="TimelineSegment"/>s on the match clock (seconds from the start
    /// of the match: (period − 1) × periodSeconds + time). Built by <see cref="TimelineBuilder"/>; warped to presentation time by
    /// <see cref="PresentationTimeline"/> and played by <see cref="ReplayCursor"/>.
    /// </summary>
    public sealed class ReplayTimeline
    {
        private readonly Dictionary<int, RosterEntry> _roster;

        /// <summary>Creates a timeline (built by <see cref="TimelineBuilder"/>).</summary>
        public ReplayTimeline(ReplayMatch match, RinkMapping mapping, IReadOnlyList<TimelineSegment> segments, IReadOnlyList<TimelineGoal> goals)
        {
            Match = match ?? throw new ArgumentNullException(nameof(match));
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            Segments = segments ?? throw new ArgumentNullException(nameof(segments));
            Goals = goals ?? throw new ArgumentNullException(nameof(goals));
            if (segments.Count == 0)
            {
                throw new ArgumentException("A timeline needs at least one segment.", nameof(segments));
            }

            _roster = new Dictionary<int, RosterEntry>();
            foreach (RosterEntry entry in match.Roster)
            {
                _roster[entry.Id] = entry;
            }
        }

        /// <summary>The source match.</summary>
        public ReplayMatch Match { get; }

        /// <summary>Grid → metres mapping and rink lines.</summary>
        public RinkMapping Mapping { get; }

        /// <summary>Segments in event order.</summary>
        public IReadOnlyList<TimelineSegment> Segments { get; }

        /// <summary>Goals in order.</summary>
        public IReadOnlyList<TimelineGoal> Goals { get; }

        /// <summary>Period length in seconds.</summary>
        public int PeriodSeconds => Match.PeriodSeconds;

        /// <summary>Match clock where the timeline starts (0).</summary>
        public double StartClock => Segments[0].StartClock;

        /// <summary>Match clock of the last event.</summary>
        public double EndClock => Segments[Segments.Count - 1].EndClock;

        /// <summary>Length of the timeline in seconds of match clock.</summary>
        public double Duration => EndClock - StartClock;

        /// <summary>Final home score (from the events).</summary>
        public int HomeScore => Segments[Segments.Count - 1].HomeScore;

        /// <summary>Final away score (from the events).</summary>
        public int AwayScore => Segments[Segments.Count - 1].AwayScore;

        /// <summary>The roster entry of a player, or null when the id is not in the roster.</summary>
        public RosterEntry FindPlayer(int id)
        {
            RosterEntry entry;
            return _roster.TryGetValue(id, out entry) ? entry : null;
        }
    }
}
