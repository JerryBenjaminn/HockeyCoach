using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>The fixed tie-break order of positions: C, LW, RW, LD, RD (data-schema.md, Lähin pelaaja).</summary>
    public static class PositionOrder
    {
        /// <summary>All five positions in tie-break order.</summary>
        public static readonly Position[] All =
        {
            Position.Center, Position.LeftWing, Position.RightWing, Position.LeftDefence, Position.RightDefence,
        };

        /// <summary>The three forward positions in tie-break order.</summary>
        public static readonly Position[] Forwards = { Position.Center, Position.LeftWing, Position.RightWing };
    }
}
