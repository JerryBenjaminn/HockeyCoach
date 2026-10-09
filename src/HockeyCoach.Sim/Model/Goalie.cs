using System;

namespace HockeyCoach.Sim.Model
{
    /// <summary>An immutable goalie.</summary>
    public sealed class Goalie : IStatProvider
    {
        /// <summary>Creates a goalie.</summary>
        /// <param name="id">Player id, unique within a match.</param>
        /// <param name="name">Display name.</param>
        /// <param name="stats">Stat values.</param>
        public Goalie(int id, string name, GoalieStats stats)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        /// <summary>Player id, unique within a match.</summary>
        public int Id { get; }

        /// <summary>Display name.</summary>
        public string Name { get; }

        /// <summary>Stat values.</summary>
        public GoalieStats Stats { get; }

        /// <inheritdoc />
        public int GetStat(StatRef stat)
        {
            return Stats[stat.GoalieStat];
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name + " (#" + Id + ")";
        }
    }
}
