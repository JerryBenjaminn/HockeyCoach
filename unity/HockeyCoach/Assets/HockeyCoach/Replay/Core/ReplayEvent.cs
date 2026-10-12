using System.Collections.Generic;
using Newtonsoft.Json;

namespace HockeyCoach.Replay.Core
{
    /// <summary>Event types of the event schema (<c>docs/stats-and-checks.md</c>), as written in the <c>type</c> field.</summary>
    public enum ReplayEventType
    {
        /// <summary>Faceoff.</summary>
        Faceoff,

        /// <summary>Controlled zone entry.</summary>
        ControlledZoneEntry,

        /// <summary>Dump-in.</summary>
        DumpIn,

        /// <summary>Pass.</summary>
        Pass,

        /// <summary>Shot attempt.</summary>
        Shot,

        /// <summary>Turnover.</summary>
        Turnover,

        /// <summary>Puck battle.</summary>
        PuckBattle,

        /// <summary>Timeout.</summary>
        Timeout,

        /// <summary>Stoppage.</summary>
        Stoppage,

        /// <summary>Line change.</summary>
        LineChange,
    }

    /// <summary>
    /// One event of the log: common context, the type-specific fields (null when the type has no such field) and the
    /// placement after the event. Field names follow the export; <see cref="Outcome"/> is shared by zone entries,
    /// shots and puck battles.
    /// </summary>
    public sealed class ReplayEvent
    {
        /// <summary>Event type name (see <see cref="ReplayEventType"/>).</summary>
        public string Type { get; set; }

        /// <summary>The parsed <see cref="Type"/>; set by <see cref="ReplayJsonReader"/>.</summary>
        [JsonIgnore]
        public ReplayEventType Kind { get; set; }

        /// <summary>Period (1-based).</summary>
        public int Period { get; set; }

        /// <summary>Seconds into the period.</summary>
        public double Time { get; set; }

        /// <summary>Strength on ice, e.g. <c>5v5</c>.</summary>
        public string Strength { get; set; }

        /// <summary>Id of the play being run, if any.</summary>
        public string PlayId { get; set; }

        /// <summary>Id of the defensive system, if any.</summary>
        public string SystemId { get; set; }

        /// <summary>Faceoff: home centre id.</summary>
        public int? HomeCentreId { get; set; }

        /// <summary>Faceoff: away centre id.</summary>
        public int? AwayCentreId { get; set; }

        /// <summary>Faceoff: winning team.</summary>
        public string Winner { get; set; }

        /// <summary>Faceoff: zone of the faceoff.</summary>
        public string Location { get; set; }

        /// <summary>Zone entry: carrier id.</summary>
        public int? CarrierId { get; set; }

        /// <summary>Zone entry: numbers, e.g. <c>2v3</c>.</summary>
        public string Numbers { get; set; }

        /// <summary>Zone entry: carry or pass.</summary>
        public string Method { get; set; }

        /// <summary>Outcome of a zone entry (Kept / Lost), shot (Goal / Saved / Blocked / Missed) or puck battle (Win / NoWinner / Loss).</summary>
        public string Outcome { get; set; }

        /// <summary>Dump-in or shot: shooter id.</summary>
        public int? ShooterId { get; set; }

        /// <summary>Dump-in: outcome of the battle for the puck.</summary>
        public string BattleOutcome { get; set; }

        /// <summary>Pass: passer id.</summary>
        public int? PasserId { get; set; }

        /// <summary>Pass: intended receiver id.</summary>
        public int? ReceiverId { get; set; }

        /// <summary>Pass or shot: under pressure.</summary>
        public bool? UnderPressure { get; set; }

        /// <summary>Pass: true when the pass was completed.</summary>
        public bool? Succeeded { get; set; }

        /// <summary>Shot: id of the goalie who saved or the skater who blocked, null otherwise.</summary>
        public int? StoppedById { get; set; }

        /// <summary>Shot: expected goals.</summary>
        public double? Xg { get; set; }

        /// <summary>Shot: chance class, if any.</summary>
        public string ChanceClass { get; set; }

        /// <summary>Shot: chance type.</summary>
        public string ChanceType { get; set; }

        /// <summary>Turnover: id of the player who lost the puck.</summary>
        public int? LoserId { get; set; }

        /// <summary>Turnover: id of the player who took the puck.</summary>
        public int? TakerId { get; set; }

        /// <summary>Turnover: node of the turnover.</summary>
        public int? NodeId { get; set; }

        /// <summary>Puck battle: attacking participants.</summary>
        public List<int> AttackerIds { get; set; }

        /// <summary>Puck battle: defending participants.</summary>
        public List<int> DefenderIds { get; set; }

        /// <summary>Timeout: team that called it.</summary>
        public string Team { get; set; }

        /// <summary>Stoppage: reason, e.g. <c>Goal</c> or <c>PeriodEnd</c>.</summary>
        public string Reason { get; set; }

        /// <summary>Line change: players leaving the ice.</summary>
        public List<int> OutgoingIds { get; set; }

        /// <summary>Line change: players entering the ice.</summary>
        public List<int> IncomingIds { get; set; }

        /// <summary>Positions after the event, in the home team's frame.</summary>
        public PlacementData Placement { get; set; }
    }

    /// <summary>Placement of an event: puck and all on-ice players as grid nodes.</summary>
    public sealed class PlacementData
    {
        /// <summary>Puck carrier id, null for a loose puck.</summary>
        public int? PuckCarrierId { get; set; }

        /// <summary>Node of the puck.</summary>
        public int PuckNodeId { get; set; }

        /// <summary>On-ice players (skaters and goalies).</summary>
        public List<PlacedPlayerData> Players { get; set; }
    }

    /// <summary>One player of a placement.</summary>
    public sealed class PlacedPlayerData
    {
        /// <summary>Player id.</summary>
        public int Id { get; set; }

        /// <summary><c>Home</c> or <c>Away</c>.</summary>
        public string Team { get; set; }

        /// <summary>Node of the player.</summary>
        public int NodeId { get; set; }

        /// <summary>True for goalies.</summary>
        public bool Goalie { get; set; }
    }
}
