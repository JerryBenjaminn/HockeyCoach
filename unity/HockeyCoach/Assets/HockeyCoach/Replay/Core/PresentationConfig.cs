namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// The replay presentation values of <c>data/replay.json</c> (D-088 B, data-schema.md, replay.json). Read only by
    /// <see cref="PresentationConfigReader"/>, which checks every key; there are no defaults in code. Speeds are metres per
    /// presentation second at 1×, durations presentation seconds at 1× unless the name says otherwise.
    /// </summary>
    public sealed class PresentationConfig
    {
        internal PresentationConfig(TimingSettings timing, GoalSettings goal, PuckSettings puck, SkaterSettings skaters, GoalieSettings goalies, BenchSettings bench)
        {
            Timing = timing;
            Goal = goal;
            Puck = puck;
            Skaters = skaters;
            Goalies = goalies;
            Bench = bench;
        }

        /// <summary><c>timing</c>: compression, minimum durations and holds.</summary>
        public TimingSettings Timing { get; }

        /// <summary><c>goal</c>: goal slow motion and hold.</summary>
        public GoalSettings Goal { get; }

        /// <summary><c>puck</c>: puck path speeds.</summary>
        public PuckSettings Puck { get; }

        /// <summary><c>skaters</c>: skater motion.</summary>
        public SkaterSettings Skaters { get; }

        /// <summary><c>goalies</c>: goalie positioning.</summary>
        public GoalieSettings Goalies { get; }

        /// <summary><c>bench</c>: bench gate positions.</summary>
        public BenchSettings Bench { get; }
    }

    /// <summary><c>replay.json</c> <c>timing</c>.</summary>
    public sealed class TimingSettings
    {
        /// <summary>Game seconds per presentation second for action.</summary>
        public double ActionCompression { get; internal set; }

        /// <summary>Game seconds per presentation second for dead time (setup, regroup).</summary>
        public double DeadCompression { get; internal set; }

        /// <summary>Minimum presentation duration of an event's gap.</summary>
        public double MinEventSeconds { get; internal set; }

        /// <summary>Minimum presentation duration of a flying line change.</summary>
        public double LineChangeSeconds { get; internal set; }

        /// <summary>Frozen hold after a stoppage.</summary>
        public double StoppageHoldSeconds { get; internal set; }

        /// <summary>Frozen duration of a timeout.</summary>
        public double TimeoutHoldSeconds { get; internal set; }

        /// <summary>Frozen break after a period end (after the stoppage hold).</summary>
        public double PeriodBreakSeconds { get; internal set; }

        /// <summary>Skating speed of the frozen approach before a faceoff (m/s).</summary>
        public double FaceoffApproachSpeed { get; internal set; }

        /// <summary>Shortest faceoff approach.</summary>
        public double FaceoffApproachMinSeconds { get; internal set; }

        /// <summary>Longest faceoff approach.</summary>
        public double FaceoffApproachMaxSeconds { get; internal set; }
    }

    /// <summary><c>replay.json</c> <c>goal</c>.</summary>
    public sealed class GoalSettings
    {
        /// <summary>Presentation speed factor of a goal's action window (duration is divided by it).</summary>
        public double SlowMoRate { get; internal set; }

        /// <summary>Shortest slowed goal action window.</summary>
        public double SlowMoMinSeconds { get; internal set; }

        /// <summary>Frozen hold after a goal.</summary>
        public double HoldSeconds { get; internal set; }

        /// <summary>Depth of the puck in the net behind the goal line (m).</summary>
        public double NetInsideMetres { get; internal set; }
    }

    /// <summary><c>replay.json</c> <c>puck</c>.</summary>
    public sealed class PuckSettings
    {
        /// <summary>Pass speed.</summary>
        public double PassSpeed { get; internal set; }

        /// <summary>Long pass speed.</summary>
        public double LongPassSpeed { get; internal set; }

        /// <summary>Straight distance from which a pass is long (m).</summary>
        public double LongPassMinMetres { get; internal set; }

        /// <summary>Shot speed.</summary>
        public double ShotSpeed { get; internal set; }

        /// <summary>Dump-in speed.</summary>
        public double DumpInSpeed { get; internal set; }

        /// <summary>Rebound speed after a save or a miss.</summary>
        public double ReboundSpeed { get; internal set; }

        /// <summary>Deflection speed after a block.</summary>
        public double BlockDeflectSpeed { get; internal set; }

        /// <summary>Loose puck speed (turnovers, puck battles).</summary>
        public double LoosePuckSpeed { get; internal set; }

        /// <summary>Faceoff draw speed.</summary>
        public double FaceoffDrawSpeed { get; internal set; }

        /// <summary>Time the puck stays on the faceoff spot from the start of the action window.</summary>
        public double FaceoffDrawDelaySeconds { get; internal set; }

        /// <summary>Distance of the miss point outside the post (m).</summary>
        public double MissWideMetres { get; internal set; }

        /// <summary>Carried puck lead in front of the carrier (m).</summary>
        public double CarriedLeadMetres { get; internal set; }

        /// <summary>Below this carrier speed the carried puck points at the opponent goal (m/s).</summary>
        public double CarriedHeadingMinSpeed { get; internal set; }

        /// <summary>How the puck floor of the action window counts the puck path (Q-082, D-089 A3).</summary>
        public PuckFloorMode FloorMode { get; internal set; }
    }

    /// <summary><c>puck.floorMode</c>: which legs of the puck path the puck floor counts (Q-082, D-089 A3).</summary>
    public enum PuckFloorMode
    {
        /// <summary><c>allLegs</c>: the whole path (shot and rebound or deflection).</summary>
        AllLegs,

        /// <summary><c>firstLeg</c>: only the first leg (the shot up to the stopper, the net or the miss point).</summary>
        FirstLeg,
    }

    /// <summary><c>replay.json</c> <c>skaters</c>.</summary>
    public sealed class SkaterSettings
    {
        /// <summary>Top skating speed.</summary>
        public double TopSpeed { get; internal set; }

        /// <summary>Acceleration (m/s²).</summary>
        public double Acceleration { get; internal set; }

        /// <summary>Deceleration (m/s²).</summary>
        public double Deceleration { get; internal set; }

        /// <summary>Catmull-Rom alpha (0.5 = centripetal).</summary>
        public double PathAlpha { get; internal set; }

        /// <summary>Blend towards the straight line (0 = pure Catmull-Rom).</summary>
        public double PathTension { get; internal set; }

        /// <summary>Turns sharper than this stop the skater at the target (degrees).</summary>
        public double StopTurnDegrees { get; internal set; }

        /// <summary>Skaters reach their target this much before the gap ends.</summary>
        public double AnticipationSeconds { get; internal set; }

        /// <summary>Radius of a player's own offset within a node (m).</summary>
        public double NodeOffsetRadiusMetres { get; internal set; }

        /// <summary>Minimum distance between skaters after separation (m).</summary>
        public double SeparationMinMetres { get; internal set; }

        /// <summary>Separation iterations per sample.</summary>
        public int SeparationIterations { get; internal set; }

        /// <summary>Idle motion amplitude (m).</summary>
        public double IdleAmplitudeMetres { get; internal set; }

        /// <summary>Idle motion frequency (Hz).</summary>
        public double IdleFrequencyHz { get; internal set; }

        /// <summary>Ratio of the idle y frequency to the x frequency.</summary>
        public double IdleYFrequencyRatio { get; internal set; }
    }

    /// <summary><c>replay.json</c> <c>goalies</c>.</summary>
    public sealed class GoalieSettings
    {
        /// <summary>Distance in front of the goal line (m).</summary>
        public double DepthMetres { get; internal set; }

        /// <summary>Share of the puck's lateral offset the goalie follows.</summary>
        public double LateralTrackFactor { get; internal set; }

        /// <summary>Largest lateral offset (m).</summary>
        public double LateralMaxMetres { get; internal set; }
    }

    /// <summary><c>replay.json</c> <c>bench</c>.</summary>
    public sealed class BenchSettings
    {
        /// <summary>Home bench gate x, metres from the centre line towards +x.</summary>
        public double HomeGateFromCentreMetres { get; internal set; }

        /// <summary>Away bench gate x, metres from the centre line towards +x.</summary>
        public double AwayGateFromCentreMetres { get; internal set; }

        /// <summary>Gate y, metres from the y = 0 boards.</summary>
        public double GateYMetres { get; internal set; }
    }
}
