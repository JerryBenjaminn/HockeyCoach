using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Model
{
    /// <summary>
    /// A line (ketju): an ordered group of skaters that goes on the ice together.
    /// The required composition (e.g. 3 forwards + 2 defensemen) is not yet specified and is not enforced here.
    /// </summary>
    public sealed class Line
    {
        private readonly Skater[] _skaters;

        /// <summary>Creates a line.</summary>
        /// <param name="name">Display name, e.g. "L1".</param>
        /// <param name="skaters">Skaters in a stable order. Must be non-empty and contain no duplicates.</param>
        public Line(string name, IReadOnlyList<Skater> skaters)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (skaters == null)
            {
                throw new ArgumentNullException(nameof(skaters));
            }

            if (skaters.Count == 0)
            {
                throw new ArgumentException("A line needs at least one skater.", nameof(skaters));
            }

            _skaters = new Skater[skaters.Count];
            for (int i = 0; i < skaters.Count; i++)
            {
                Skater skater = skaters[i] ?? throw new ArgumentException("Line contains a null skater.", nameof(skaters));
                for (int j = 0; j < i; j++)
                {
                    if (_skaters[j].Id == skater.Id)
                    {
                        throw new ArgumentException("Skater " + skater + " appears twice in line " + name + ".", nameof(skaters));
                    }
                }

                _skaters[i] = skater;
            }
        }

        /// <summary>Display name.</summary>
        public string Name { get; }

        /// <summary>Skaters in the order given at construction.</summary>
        public IReadOnlyList<Skater> Skaters
        {
            get { return _skaters; }
        }
    }
}
