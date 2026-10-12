using System.Collections.Generic;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Root of the <c>match --json</c> export (D-087, Q-076): score, replay header, events and goal notes. Plain data
    /// filled by <see cref="ReplayJsonReader"/>; the per-period statistics of the export are not read.
    /// </summary>
    public sealed class ReplayMatch
    {
        /// <summary>Match seed.</summary>
        public ulong Seed { get; set; }

        /// <summary>Home goals of the final score.</summary>
        public int HomeGoals { get; set; }

        /// <summary>Away goals of the final score.</summary>
        public int AwayGoals { get; set; }

        /// <summary>True when the simulator stopped the match by its anti-stall cap.</summary>
        public bool Stalled { get; set; }

        /// <summary>Version of the export format (<see cref="ReplayJsonReader.SupportedFormatVersion"/>).</summary>
        public int FormatVersion { get; set; }

        /// <summary>Length of one period in seconds of match clock.</summary>
        public int PeriodSeconds { get; set; }

        /// <summary>Number of periods.</summary>
        public int PeriodCount { get; set; }

        /// <summary>Every player of both teams, home first, then by id.</summary>
        public List<RosterEntry> Roster { get; set; }

        /// <summary>Rink grid and dimensions, in the home team's frame.</summary>
        public RinkData Rink { get; set; }

        /// <summary>The event log in order.</summary>
        public List<ReplayEvent> Events { get; set; }

        /// <summary>Goal notes of the export (one per goal, in order).</summary>
        public List<GoalNoteData> Goals { get; set; }
    }

    /// <summary>One roster entry of the header.</summary>
    public sealed class RosterEntry
    {
        /// <summary>Player id.</summary>
        public int Id { get; set; }

        /// <summary><c>Home</c> or <c>Away</c>.</summary>
        public string Team { get; set; }

        /// <summary>Slot in the player's unit (C, LW, RW, LD, RD) or <c>G</c> for goalies.</summary>
        public string Position { get; set; }

        /// <summary>1-based forward line, null when not a forward in a line.</summary>
        public int? Line { get; set; }

        /// <summary>1-based defence pair, null when not a defender in a pair.</summary>
        public int? Pair { get; set; }

        /// <summary>True for goalies.</summary>
        public bool Goalie { get; set; }

        /// <summary>True for the starting goalie, false for the backup, null for skaters.</summary>
        public bool? Starter { get; set; }
    }

    /// <summary>Rink data of the header: grid, goals, nodes, faceoff spots, zones and dimensions in metres.</summary>
    public sealed class RinkData
    {
        /// <summary>Grid length (number of node rows along the rink).</summary>
        public int Length { get; set; }

        /// <summary>Grid width (number of lanes across the rink).</summary>
        public int Width { get; set; }

        /// <summary>The home team's own goal (grid point).</summary>
        public GridPointData OwnGoal { get; set; }

        /// <summary>The away team's goal, i.e. the goal the home team attacks (grid point).</summary>
        public GridPointData OpponentGoal { get; set; }

        /// <summary>All nodes in id order (id = x * width + y).</summary>
        public List<RinkNodeData> Nodes { get; set; }

        /// <summary>Faceoff spots.</summary>
        public List<FaceoffSpotData> FaceoffSpots { get; set; }

        /// <summary>Zones named from the home team's view.</summary>
        public List<ZoneData> Zones { get; set; }

        /// <summary>Real dimensions for the replay (Q-077).</summary>
        public RinkMetresData Metres { get; set; }
    }

    /// <summary>A grid point.</summary>
    public sealed class GridPointData
    {
        /// <summary>Row along the rink (0 = home end).</summary>
        public int X { get; set; }

        /// <summary>Lane across the rink.</summary>
        public int Y { get; set; }
    }

    /// <summary>A grid node.</summary>
    public sealed class RinkNodeData
    {
        /// <summary>Node id (x * width + y).</summary>
        public int Id { get; set; }

        /// <summary>Row along the rink.</summary>
        public int X { get; set; }

        /// <summary>Lane across the rink.</summary>
        public int Y { get; set; }

        /// <summary>Zone id from the home team's view (defensive, neutral, offensive).</summary>
        public string Zone { get; set; }
    }

    /// <summary>A faceoff spot of the grid.</summary>
    public sealed class FaceoffSpotData
    {
        /// <summary>Spot id, e.g. <c>center</c> or <c>offensiveLeft</c>.</summary>
        public string Id { get; set; }

        /// <summary>Row along the rink.</summary>
        public int X { get; set; }

        /// <summary>Lane across the rink.</summary>
        public int Y { get; set; }

        /// <summary>Node id of the spot.</summary>
        public int NodeId { get; set; }
    }

    /// <summary>A zone as a range of grid rows.</summary>
    public sealed class ZoneData
    {
        /// <summary>Zone id (defensive, neutral, offensive) from the home team's view.</summary>
        public string Id { get; set; }

        /// <summary>First row of the zone.</summary>
        public int XMin { get; set; }

        /// <summary>Last row of the zone.</summary>
        public int XMax { get; set; }
    }

    /// <summary>Rink dimensions in metres (replay only, Q-077).</summary>
    public sealed class RinkMetresData
    {
        /// <summary>Length end boards to end boards.</summary>
        public double Length { get; set; }

        /// <summary>Width board to board.</summary>
        public double Width { get; set; }

        /// <summary>Distance from the end boards to the goal line.</summary>
        public double GoalLineFromEnd { get; set; }

        /// <summary>Distance from the end boards to the near edge of the blue line.</summary>
        public double BlueLineFromEnd { get; set; }
    }

    /// <summary>A goal note of the export.</summary>
    public sealed class GoalNoteData
    {
        /// <summary>Period of the goal.</summary>
        public int Period { get; set; }

        /// <summary>Seconds into the period.</summary>
        public double Time { get; set; }

        /// <summary>Scoring team, <c>Home</c> or <c>Away</c>.</summary>
        public string Team { get; set; }

        /// <summary>Scorer id.</summary>
        public int ShooterId { get; set; }

        /// <summary>Chance type of the goal.</summary>
        public string ChanceType { get; set; }

        /// <summary>One-line explanation of the goal.</summary>
        public string Explanation { get; set; }
    }
}
