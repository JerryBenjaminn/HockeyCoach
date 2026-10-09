using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>One rule of a defensive system: a condition and a target per role.</summary>
    public sealed class SystemRule
    {
        private readonly SortedDictionary<SystemRole, SystemTarget> _targets;

        /// <summary>Creates a rule. Use <see cref="SystemValidator"/> to check that every role has a target.</summary>
        public SystemRule(SystemCondition when, IEnumerable<KeyValuePair<SystemRole, SystemTarget>> targets)
        {
            When = when ?? throw new ArgumentNullException(nameof(when));
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            _targets = new SortedDictionary<SystemRole, SystemTarget>();
            foreach (KeyValuePair<SystemRole, SystemTarget> target in targets)
            {
                if (_targets.ContainsKey(target.Key))
                {
                    throw new ArgumentException("Role " + target.Key + " is listed twice.", nameof(targets));
                }

                _targets.Add(target.Key, target.Value ?? throw new ArgumentException("Null target.", nameof(targets)));
            }
        }

        /// <summary>The rule's condition.</summary>
        public SystemCondition When { get; }

        /// <summary>Role → target, in role order (F1, F2, F3, D1, D2).</summary>
        public IReadOnlyDictionary<SystemRole, SystemTarget> Targets
        {
            get { return _targets; }
        }
    }
}
