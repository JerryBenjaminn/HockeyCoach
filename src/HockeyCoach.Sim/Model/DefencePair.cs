using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Model
{
    /// <summary>
    /// A defence pair (D-023): left and right defence. Rotates independently of forward trios.
    /// </summary>
    public sealed class DefencePair
    {
        /// <summary>Number of skaters in a defence pair.</summary>
        public const int Size = 2;

        private readonly Skater[] _skaters;

        /// <summary>Creates a defence pair. The two skaters must be different players.</summary>
        public DefencePair(string name, Skater leftDefence, Skater rightDefence)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            LeftDefence = leftDefence ?? throw new ArgumentNullException(nameof(leftDefence));
            RightDefence = rightDefence ?? throw new ArgumentNullException(nameof(rightDefence));
            _skaters = new[] { leftDefence, rightDefence };
            Units.RequireDistinct(_skaters, "defence pair " + name);
        }

        /// <summary>Display name, e.g. "D1".</summary>
        public string Name { get; }

        /// <summary>Skater in the LD slot.</summary>
        public Skater LeftDefence { get; }

        /// <summary>Skater in the RD slot.</summary>
        public Skater RightDefence { get; }

        /// <summary>Skaters in slot order LD, RD.</summary>
        public IReadOnlyList<Skater> Skaters
        {
            get { return _skaters; }
        }

        /// <summary>The slot (LD or RD) the skater plays in this pair, or null if he is not in it.</summary>
        public Position? SlotOf(Skater skater)
        {
            if (skater == null)
            {
                return null;
            }

            if (skater.Id == LeftDefence.Id)
            {
                return Position.LeftDefence;
            }

            if (skater.Id == RightDefence.Id)
            {
                return Position.RightDefence;
            }

            return null;
        }
    }
}
