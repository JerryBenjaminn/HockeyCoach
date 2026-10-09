using System;

namespace HockeyCoach.Sim.Model
{
    /// <summary>
    /// Identifies one stat of either a skater or a goalie. Needed because some names
    /// (e.g. <c>positioning</c>) exist in both stat sets.
    /// </summary>
    public readonly struct StatRef : IEquatable<StatRef>
    {
        private StatRef(StatOwner owner, int index)
        {
            Owner = owner;
            Index = index;
        }

        /// <summary>The stat set this stat belongs to.</summary>
        public StatOwner Owner { get; }

        /// <summary>Numeric value of the <see cref="Model.SkaterStat"/> or <see cref="Model.GoalieStat"/>.</summary>
        public int Index { get; }

        /// <summary>The skater stat. Throws if this is a goalie stat.</summary>
        public SkaterStat SkaterStat
        {
            get
            {
                if (Owner != StatOwner.Skater)
                {
                    throw new InvalidOperationException("Not a skater stat: " + ToString());
                }

                return (SkaterStat)Index;
            }
        }

        /// <summary>The goalie stat. Throws if this is a skater stat.</summary>
        public GoalieStat GoalieStat
        {
            get
            {
                if (Owner != StatOwner.Goalie)
                {
                    throw new InvalidOperationException("Not a goalie stat: " + ToString());
                }

                return (GoalieStat)Index;
            }
        }

        /// <summary>Creates a reference to a skater stat.</summary>
        public static StatRef Of(SkaterStat stat)
        {
            return new StatRef(StatOwner.Skater, (int)stat);
        }

        /// <summary>Creates a reference to a goalie stat.</summary>
        public static StatRef Of(GoalieStat stat)
        {
            return new StatRef(StatOwner.Goalie, (int)stat);
        }

        /// <inheritdoc />
        public bool Equals(StatRef other)
        {
            return Owner == other.Owner && Index == other.Index;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is StatRef other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ((int)Owner * 397) ^ Index;
        }

        /// <summary>Returns e.g. <c>skater.shotAccuracy</c> or <c>goalie.positioning</c>.</summary>
        public override string ToString()
        {
            return Owner == StatOwner.Skater
                ? "skater." + StatNames.ToName((SkaterStat)Index)
                : "goalie." + StatNames.ToName((GoalieStat)Index);
        }
    }
}
