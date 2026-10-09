using System;

namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Attackers vs defenders taking part in a zone entry, e.g. 2 vs 2 (event schema: controlled zone entry,
    /// "voimasuhteet"). How players are counted is defined in docs/data-schema.md (D-048).
    /// </summary>
    public readonly struct EntryNumbers : IEquatable<EntryNumbers>
    {
        /// <summary>Creates the numbers. Counts must not be negative.</summary>
        public EntryNumbers(int attackers, int defenders)
        {
            if (attackers < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackers), "Count must not be negative.");
            }

            if (defenders < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(defenders), "Count must not be negative.");
            }

            Attackers = attackers;
            Defenders = defenders;
        }

        /// <summary>Attacking players in the entry.</summary>
        public int Attackers { get; }

        /// <summary>Defending players in the entry.</summary>
        public int Defenders { get; }

        /// <inheritdoc />
        public bool Equals(EntryNumbers other)
        {
            return Attackers == other.Attackers && Defenders == other.Defenders;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is EntryNumbers other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (Attackers * 397) ^ Defenders;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Attackers + "v" + Defenders;
        }
    }
}
