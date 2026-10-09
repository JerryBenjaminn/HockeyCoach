using System;

namespace HockeyCoach.Sim.Events
{
    /// <summary>Skaters on the ice per team, e.g. 5 vs 5 (event schema: "voimasuhteet kentällä").</summary>
    public readonly struct Strength : IEquatable<Strength>
    {
        /// <summary>Creates a strength. Counts must not be negative.</summary>
        public Strength(int homeSkaters, int awaySkaters)
        {
            if (homeSkaters < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(homeSkaters), "Skater count must not be negative.");
            }

            if (awaySkaters < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(awaySkaters), "Skater count must not be negative.");
            }

            HomeSkaters = homeSkaters;
            AwaySkaters = awaySkaters;
        }

        /// <summary>Home team skaters on the ice (goalie not counted).</summary>
        public int HomeSkaters { get; }

        /// <summary>Away team skaters on the ice (goalie not counted).</summary>
        public int AwaySkaters { get; }

        /// <summary>Skaters of <paramref name="side"/>.</summary>
        public int Of(TeamSide side)
        {
            return side == TeamSide.Home ? HomeSkaters : AwaySkaters;
        }

        /// <inheritdoc />
        public bool Equals(Strength other)
        {
            return HomeSkaters == other.HomeSkaters && AwaySkaters == other.AwaySkaters;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Strength other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (HomeSkaters * 397) ^ AwaySkaters;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return HomeSkaters + "v" + AwaySkaters;
        }
    }
}
