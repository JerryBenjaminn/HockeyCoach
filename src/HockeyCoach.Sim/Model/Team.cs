using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Model
{
    /// <summary>
    /// An immutable team: roster of skaters and goalies plus forward trios and defence pairs built from the roster.
    /// Trios and pairs are separate units that rotate at different rates (D-023).
    /// </summary>
    public sealed class Team
    {
        private readonly Skater[] _skaters;
        private readonly Goalie[] _goalies;
        private readonly ForwardLine[] _forwardLines;
        private readonly DefencePair[] _defencePairs;

        /// <summary>Creates a team and validates that ids are unique and every unit member is on the roster.</summary>
        public Team(
            string name,
            IReadOnlyList<Skater> skaters,
            IReadOnlyList<Goalie> goalies,
            IReadOnlyList<ForwardLine> forwardLines,
            IReadOnlyList<DefencePair> defencePairs)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _skaters = Copy(skaters, nameof(skaters));
            _goalies = Copy(goalies, nameof(goalies));
            _forwardLines = Copy(forwardLines, nameof(forwardLines));
            _defencePairs = Copy(defencePairs, nameof(defencePairs));

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

            foreach (ForwardLine line in _forwardLines)
            {
                RequireOnRoster(line.Skaters, "Forward line " + line.Name, nameof(forwardLines));
            }

            foreach (DefencePair pair in _defencePairs)
            {
                RequireOnRoster(pair.Skaters, "Defence pair " + pair.Name, nameof(defencePairs));
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

        /// <summary>Forward trios in the order given (first trio first).</summary>
        public IReadOnlyList<ForwardLine> ForwardLines
        {
            get { return _forwardLines; }
        }

        /// <summary>Defence pairs in the order given (first pair first).</summary>
        public IReadOnlyList<DefencePair> DefencePairs
        {
            get { return _defencePairs; }
        }

        private void RequireOnRoster(IReadOnlyList<Skater> members, string unit, string paramName)
        {
            foreach (Skater member in members)
            {
                if (Array.IndexOf(_skaters, member) < 0)
                {
                    throw new ArgumentException(unit + " contains " + member + " who is not on the roster of " + Name + ".", paramName);
                }
            }
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
