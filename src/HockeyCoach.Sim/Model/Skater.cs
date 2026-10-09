using System;

namespace HockeyCoach.Sim.Model
{
    /// <summary>An immutable skater (field player).</summary>
    public sealed class Skater : IStatProvider
    {
        /// <summary>Creates a skater.</summary>
        /// <param name="id">Player id, unique within a match.</param>
        /// <param name="name">Display name.</param>
        /// <param name="position">Position.</param>
        /// <param name="stats">Stat values.</param>
        public Skater(int id, string name, Position position, SkaterStats stats)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Position = position;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        /// <summary>Player id, unique within a match.</summary>
        public int Id { get; }

        /// <summary>Display name.</summary>
        public string Name { get; }

        /// <summary>Position.</summary>
        public Position Position { get; }

        /// <summary>Stat values.</summary>
        public SkaterStats Stats { get; }

        /// <inheritdoc />
        public int GetStat(StatRef stat)
        {
            return Stats[stat.SkaterStat];
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name + " (#" + Id + ")";
        }
    }
}
