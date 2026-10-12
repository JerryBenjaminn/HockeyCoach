using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>One on-ice player in a timeline segment: moves from <see cref="From"/> to <see cref="To"/> during the segment.</summary>
    public sealed class SegmentPlayer
    {
        /// <summary>Creates a segment player.</summary>
        public SegmentPlayer(int id, ReplayTeam team, bool isGoalie, int fromNodeId, RinkPoint from, int nodeId, RinkPoint to)
        {
            Id = id;
            Team = team;
            IsGoalie = isGoalie;
            FromNodeId = fromNodeId;
            From = from;
            NodeId = nodeId;
            To = to;
        }

        /// <summary>Player id.</summary>
        public int Id { get; }

        /// <summary>Team.</summary>
        public ReplayTeam Team { get; }

        /// <summary>True for goalies.</summary>
        public bool IsGoalie { get; }

        /// <summary>Node at the start of the segment (the end node of the previous segment; = <see cref="NodeId"/> for players who just entered).</summary>
        public int FromNodeId { get; }

        /// <summary>Position at the start of the segment.</summary>
        public RinkPoint From { get; }

        /// <summary>Node at the end of the segment (from the event's placement).</summary>
        public int NodeId { get; }

        /// <summary>Position at the end of the segment.</summary>
        public RinkPoint To { get; }
    }

    /// <summary>
    /// One event as a stretch of match clock: the state moves from the previous event's state
    /// (<see cref="StartClock"/>) to this event's state (<see cref="EndClock"/>, the event's clock). Events at the same
    /// second give zero-length segments.
    /// </summary>
    public sealed class TimelineSegment
    {
        /// <summary>Creates a segment (built by <see cref="TimelineBuilder"/>).</summary>
        public TimelineSegment(
            int index,
            ReplayEvent source,
            double startClock,
            double endClock,
            IReadOnlyList<SegmentPlayer> players,
            IReadOnlyList<int> leaving,
            IReadOnlyList<int> entering,
            PuckPath puck,
            int homeScoreBefore,
            int awayScoreBefore,
            int homeScore,
            int awayScore,
            ReplayTeam? scoringTeam,
            RinkSpot faceoffSpot)
        {
            Index = index;
            Event = source;
            StartClock = startClock;
            EndClock = endClock;
            Players = players;
            Leaving = leaving;
            Entering = entering;
            Puck = puck;
            HomeScoreBefore = homeScoreBefore;
            AwayScoreBefore = awayScoreBefore;
            HomeScore = homeScore;
            AwayScore = awayScore;
            ScoringTeam = scoringTeam;
            FaceoffSpot = faceoffSpot;
        }

        /// <summary>Index in the timeline (= index of the event in the log).</summary>
        public int Index { get; }

        /// <summary>The source event.</summary>
        public ReplayEvent Event { get; }

        /// <summary>Event type.</summary>
        public ReplayEventType Type => Event.Kind;

        /// <summary>Period of the event.</summary>
        public int Period => Event.Period;

        /// <summary>Seconds into the period of the event.</summary>
        public double Time => Event.Time;

        /// <summary>Match clock (seconds from the start of the match) where the segment starts.</summary>
        public double StartClock { get; }

        /// <summary>Match clock of the event, where the segment ends.</summary>
        public double EndClock { get; }

        /// <summary>Length in seconds of match clock (0 for events at the same second as the previous one).</summary>
        public double Duration => EndClock - StartClock;

        /// <summary>The on-ice players after the event, ordered by id.</summary>
        public IReadOnlyList<SegmentPlayer> Players { get; }

        /// <summary>Ids of players who left the ice in this segment, ascending.</summary>
        public IReadOnlyList<int> Leaving { get; }

        /// <summary>Ids of players who entered the ice in this segment, ascending.</summary>
        public IReadOnlyList<int> Entering { get; }

        /// <summary>The puck's path.</summary>
        public PuckPath Puck { get; }

        /// <summary>Home score before the event.</summary>
        public int HomeScoreBefore { get; }

        /// <summary>Away score before the event.</summary>
        public int AwayScoreBefore { get; }

        /// <summary>Home score after the event.</summary>
        public int HomeScore { get; }

        /// <summary>Away score after the event.</summary>
        public int AwayScore { get; }

        /// <summary>The scoring team when the event is a goal, null otherwise.</summary>
        public ReplayTeam? ScoringTeam { get; }

        /// <summary>The faceoff spot of a faceoff event, null otherwise.</summary>
        public RinkSpot FaceoffSpot { get; }
    }

    /// <summary>A goal of the timeline.</summary>
    public sealed class TimelineGoal
    {
        /// <summary>Creates a goal.</summary>
        public TimelineGoal(int segmentIndex, int period, double time, double clock, ReplayTeam team, int shooterId)
        {
            SegmentIndex = segmentIndex;
            Period = period;
            Time = time;
            Clock = clock;
            Team = team;
            ShooterId = shooterId;
        }

        /// <summary>Index of the shot segment.</summary>
        public int SegmentIndex { get; }

        /// <summary>Period.</summary>
        public int Period { get; }

        /// <summary>Seconds into the period.</summary>
        public double Time { get; }

        /// <summary>Match clock.</summary>
        public double Clock { get; }

        /// <summary>Scoring team.</summary>
        public ReplayTeam Team { get; }

        /// <summary>Scorer id.</summary>
        public int ShooterId { get; }
    }
}
