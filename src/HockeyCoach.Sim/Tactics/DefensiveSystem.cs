using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// A defensive system (data-schema.md, Puolustusjärjestelmät; D-034): an ordered rule list whose first matching rule
    /// sets every defender's target. Written in the defending team's own view. Evaluation is deterministic.
    /// </summary>
    public sealed class DefensiveSystem
    {
        private readonly SystemRule[] _rules;

        /// <summary>Creates a system. Use <c>SystemValidator</c> to check it against the rules.</summary>
        /// <param name="id">Unique id, equal to the file name.</param>
        /// <param name="name">Display name.</param>
        /// <param name="mirrorY">Rules are written for the puck on the left or middle lane and mirrored for the right.</param>
        /// <param name="rules">Rules in order; the last one is the fallback with an empty condition.</param>
        public DefensiveSystem(string id, string name, bool mirrorY, IReadOnlyList<SystemRule> rules)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MirrorY = mirrorY;
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            _rules = new SystemRule[rules.Count];
            for (int i = 0; i < rules.Count; i++)
            {
                _rules[i] = rules[i] ?? throw new ArgumentException("Null rule.", nameof(rules));
            }
        }

        /// <summary>Unique id, equal to the file name.</summary>
        public string Id { get; }

        /// <summary>Display name.</summary>
        public string Name { get; }

        /// <summary>Whether the rules are written for the left side and mirrored for a puck on the right.</summary>
        public bool MirrorY { get; }

        /// <summary>Rules in order.</summary>
        public IReadOnlyList<SystemRule> Rules
        {
            get { return _rules; }
        }
    }
}
