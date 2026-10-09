using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>Result of evaluating a defensive system: the rule used, the roles and each position's target node.</summary>
    public sealed class SystemTargets
    {
        internal SystemTargets(int ruleIndex, bool mirrored, IReadOnlyDictionary<SystemRole, Position> roles, IReadOnlyDictionary<Position, GridPoint> targets)
        {
            RuleIndex = ruleIndex;
            Mirrored = mirrored;
            Roles = roles;
            Targets = targets;
        }

        /// <summary>Index of the first matching rule.</summary>
        public int RuleIndex { get; }

        /// <summary>Whether the puck was mirrored (mirrorY system, puck on the right).</summary>
        public bool Mirrored { get; }

        /// <summary>Role → position.</summary>
        public IReadOnlyDictionary<SystemRole, Position> Roles { get; }

        /// <summary>Position → target node in the defending team's own view, in position order.</summary>
        public IReadOnlyDictionary<Position, GridPoint> Targets { get; }
    }
}
