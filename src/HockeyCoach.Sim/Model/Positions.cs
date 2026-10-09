using System;

namespace HockeyCoach.Sim.Model
{
    /// <summary>Helpers for <see cref="Position"/> (D-024, docs/data-schema.md: Pelaajat, pelipaikat ja ketjut).</summary>
    public static class Positions
    {
        /// <summary>Returns the data name: C, LW, RW, LD or RD.</summary>
        public static string ToName(Position position)
        {
            switch (position)
            {
                case Position.Center: return "C";
                case Position.LeftWing: return "LW";
                case Position.RightWing: return "RW";
                case Position.LeftDefence: return "LD";
                case Position.RightDefence: return "RD";
                default: throw new ArgumentOutOfRangeException(nameof(position), position, "Unknown position.");
            }
        }

        /// <summary>Parses a data name (C, LW, RW, LD, RD; ordinal).</summary>
        public static bool TryParse(string name, out Position position)
        {
            switch (name)
            {
                case "C": position = Position.Center; return true;
                case "LW": position = Position.LeftWing; return true;
                case "RW": position = Position.RightWing; return true;
                case "LD": position = Position.LeftDefence; return true;
                case "RD": position = Position.RightDefence; return true;
                default: position = default; return false;
            }
        }

        /// <summary>Whether the position is a forward position (C, LW, RW).</summary>
        public static bool IsForward(Position position)
        {
            return position == Position.Center || position == Position.LeftWing || position == Position.RightWing;
        }

        /// <summary>Whether the position is a defence position (LD, RD).</summary>
        public static bool IsDefence(Position position)
        {
            return position == Position.LeftDefence || position == Position.RightDefence;
        }

        /// <summary>
        /// True when a player whose primary position is <paramref name="primary"/> plays the opposite side
        /// <paramref name="played"/> (LW ↔ RW, LD ↔ RD). The centre has no side. Other position changes are
        /// not defined yet (Q-019) and return false.
        /// </summary>
        public static bool IsOffSide(Position primary, Position played)
        {
            return (primary == Position.LeftWing && played == Position.RightWing)
                || (primary == Position.RightWing && played == Position.LeftWing)
                || (primary == Position.LeftDefence && played == Position.RightDefence)
                || (primary == Position.RightDefence && played == Position.LeftDefence);
        }
    }
}
