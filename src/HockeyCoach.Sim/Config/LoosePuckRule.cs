namespace HockeyCoach.Sim.Config
{
    /// <summary>Loose-puck spot rules (data-schema.md, Irtokiekon paikat; D-044, D-049).</summary>
    public enum LoosePuckRule
    {
        /// <summary><c>netFront</c>: the net front of the goal shot at.</summary>
        NetFront = 0,

        /// <summary><c>shooterSideCorner</c>: goal-line corner on the shooter's side (middle lane: 50/50 draw).</summary>
        ShooterSideCorner = 1,

        /// <summary><c>endRowShooterLane</c>: end row behind the goal on the shooter's lane.</summary>
        EndRowShooterLane = 2,

        /// <summary><c>blockerNode</c>: the blocker's node (M-5).</summary>
        BlockerNode = 3,

        /// <summary><c>laneDefenderNode</c>: the pass lane defender's node (M-1).</summary>
        LaneDefenderNode = 4,
    }
}
