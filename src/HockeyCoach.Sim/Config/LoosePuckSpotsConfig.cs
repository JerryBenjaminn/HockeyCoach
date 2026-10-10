using System.Collections.Generic;

namespace HockeyCoach.Sim.Config
{
    /// <summary>
    /// Where a loose puck ends up per situation (tuning.json <c>loosePuckSpots</c>, D-044, D-049). Every value is a
    /// <see cref="LoosePuckRule"/>.
    /// </summary>
    public sealed class LoosePuckSpotsConfig
    {
        /// <summary>The situation keys, in ordinal order.</summary>
        public static readonly IReadOnlyList<string> Keys = new[] { "blockedShot", "failedPass", "missedShot", "reboundCorner", "reboundSlot" };

        /// <summary>Creates the config.</summary>
        public LoosePuckSpotsConfig(LoosePuckRule reboundSlot, LoosePuckRule reboundCorner, LoosePuckRule missedShot, LoosePuckRule blockedShot, LoosePuckRule failedPass)
        {
            ReboundSlot = reboundSlot;
            ReboundCorner = reboundCorner;
            MissedShot = missedShot;
            BlockedShot = blockedShot;
            FailedPass = failedPass;
        }

        /// <summary>Rebound to the slot (rebound check succeeded).</summary>
        public LoosePuckRule ReboundSlot { get; }

        /// <summary>Rebound to the corner (goalie controlled but did not hold).</summary>
        public LoosePuckRule ReboundCorner { get; }

        /// <summary>Shot missed the net.</summary>
        public LoosePuckRule MissedShot { get; }

        /// <summary>Shot blocked.</summary>
        public LoosePuckRule BlockedShot { get; }

        /// <summary>Failed pass that was not intercepted.</summary>
        public LoosePuckRule FailedPass { get; }

        /// <summary>Parses a rule name (ordinal).</summary>
        public static bool TryParseRule(string name, out LoosePuckRule rule)
        {
            switch (name)
            {
                case "netFront": rule = LoosePuckRule.NetFront; return true;
                case "shooterSideCorner": rule = LoosePuckRule.ShooterSideCorner; return true;
                case "endRowShooterLane": rule = LoosePuckRule.EndRowShooterLane; return true;
                case "blockerNode": rule = LoosePuckRule.BlockerNode; return true;
                case "laneDefenderNode": rule = LoosePuckRule.LaneDefenderNode; return true;
                default: rule = default; return false;
            }
        }
    }
}
