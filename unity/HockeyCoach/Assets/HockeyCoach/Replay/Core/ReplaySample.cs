using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>One player's interpolated position in a <see cref="ReplaySample"/>.</summary>
    public sealed class PlayerSample
    {
        /// <summary>Creates a player sample.</summary>
        public PlayerSample(int id, ReplayTeam team, bool isGoalie, RinkPoint position)
        {
            Id = id;
            Team = team;
            IsGoalie = isGoalie;
            Position = position;
        }

        /// <summary>Player id.</summary>
        public int Id { get; }

        /// <summary>Team.</summary>
        public ReplayTeam Team { get; }

        /// <summary>True for goalies.</summary>
        public bool IsGoalie { get; }

        /// <summary>Position in metres.</summary>
        public RinkPoint Position { get; }
    }

    /// <summary>The replay state at the cursor's position: what the view draws.</summary>
    public sealed class ReplaySample
    {
        /// <summary>Creates a sample.</summary>
        public ReplaySample(
            int segmentIndex,
            double fraction,
            double time,
            double clock,
            int period,
            double periodTime,
            int homeScore,
            int awayScore,
            IReadOnlyList<PlayerSample> players,
            IReadOnlyList<PlayerSample> leavingPlayers,
            RinkPoint puck,
            int? puckCarrierId)
        {
            SegmentIndex = segmentIndex;
            Fraction = fraction;
            Time = time;
            Clock = clock;
            Period = period;
            PeriodTime = periodTime;
            HomeScore = homeScore;
            AwayScore = awayScore;
            Players = players;
            LeavingPlayers = leavingPlayers;
            Puck = puck;
            PuckCarrierId = puckCarrierId;
        }

        /// <summary>Index of the current segment (event).</summary>
        public int SegmentIndex { get; }

        /// <summary>
        /// Progress of the current segment's motion, 0–1, from its start to the end of its action window on the
        /// presentation time axis (1 during holds and for empty segments).
        /// </summary>
        public double Fraction { get; }

        /// <summary>Presentation time (seconds on screen at 1×).</summary>
        public double Time { get; }

        /// <summary>True match clock in seconds from the start of the match (what the HUD shows).</summary>
        public double Clock { get; }

        /// <summary>Period shown on the clock.</summary>
        public int Period { get; }

        /// <summary>Seconds into <see cref="Period"/>.</summary>
        public double PeriodTime { get; }

        /// <summary>Home score shown.</summary>
        public int HomeScore { get; }

        /// <summary>Away score shown.</summary>
        public int AwayScore { get; }

        /// <summary>On-ice players (the twelve of the segment) ordered by id, at their drawn positions.</summary>
        public IReadOnlyList<PlayerSample> Players { get; }

        /// <summary>Players skating off to the bench gate (not on the ice any more), ordered by id.</summary>
        public IReadOnlyList<PlayerSample> LeavingPlayers { get; }

        /// <summary>Puck position in metres.</summary>
        public RinkPoint Puck { get; }

        /// <summary>Puck carrier, null while the puck is loose or moving between players.</summary>
        public int? PuckCarrierId { get; }
    }
}
