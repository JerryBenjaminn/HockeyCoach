using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Model
{
    /// <summary>An immutable team: roster of skaters and goalies plus the lines built from the roster.</summary>
    public sealed class Team
    {
        private readonly Skater[] _skaters;
        private readonly Goalie[] _goalies;
        private readonly Line[] _lines;

        /// <summary>Creates a team and validates that ids are unique and every line member is on the roster.</summary>
        public Team(string name, IReadOnlyList<Skater> skaters, IReadOnlyList<Goalie> goalies, IReadOnlyList<Line> lines)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _skaters = Copy(skaters, nameof(skaters));
            _goalies = Copy(goalies, nameof(goalies));
            _lines = Copy(lines, nameof(lines));

            var ids = new HashSet<int>();
            foreach (Skater skater in _skaters)
            {
                if (!ids.Add(skater.Id))
                {
                    throw new ArgumentException("Duplicate player id " + skater.Id + " in team " + name + ".", nameof(skaters));
                }
            }

            foreach (Goalie goalie in _goalies)
            {
                if (!ids.Add(goalie.Id))
                {
                    throw new ArgumentException("Duplicate player id " + goalie.Id + " in team " + name + ".", nameof(goalies));
                }
            }

            foreach (Line line in _lines)
            {
                foreach (Skater member in line.Skaters)
                {
                    if (Array.IndexOf(_skaters, member) < 0)
                    {
                        throw new ArgumentException("Line " + line.Name + " contains " + member + " who is not on the roster of " + name + ".", nameof(lines));
                    }
                }
            }
        }

        /// <summary>Team name.</summary>
        public string Name { get; }

        /// <summary>All skaters on the roster, in the order given.</summary>
        public IReadOnlyList<Skater> Skaters
        {
            get { return _skaters; }
        }

        /// <summary>All goalies on the roster, in the order given.</summary>
        public IReadOnlyList<Goalie> Goalies
        {
            get { return _goalies; }
        }

        /// <summary>Lines in the order given (first line first).</summary>
        public IReadOnlyList<Line> Lines
        {
            get { return _lines; }
        }

        private static T[] Copy<T>(IReadOnlyList<T> items, string paramName)
            where T : class
        {
            if (items == null)
            {
                throw new ArgumentNullException(paramName);
            }

            var copy = new T[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                copy[i] = items[i] ?? throw new ArgumentException("List contains a null entry.", paramName);
            }

            return copy;
        }
    }
}
