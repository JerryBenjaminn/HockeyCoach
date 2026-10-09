using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// Evaluates a defensive system (data-schema.md, Arviointi and Kohteen laskenta). Everything is in the defending
    /// team's own view; the caller rotates the puck and skaters with <see cref="TeamFrame"/>. No randomness.
    /// </summary>
    public static class SystemTargetResolver
    {
        /// <summary>Evaluates the system for the current puck and skater nodes.</summary>
        /// <param name="system">The defending team's system.</param>
        /// <param name="rink">The rink.</param>
        /// <param name="puck">Puck node (defender's view). Never a goal node (D-033).</param>
        /// <param name="puckState">Controlled or loose.</param>
        /// <param name="skaters">Node of each of the five positions (defender's view).</param>
        public static SystemTargets Resolve(
            DefensiveSystem system,
            Rink rink,
            GridPoint puck,
            PuckState puckState,
            IReadOnlyDictionary<Position, GridPoint> skaters)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            bool mirrored = system.MirrorY && rink.LaneOf(puck.Y) == Lane.Right;
            GridPoint evaluated = mirrored ? rink.MirrorY(puck) : puck;
            RinkZone zone = rink.ZoneAtX(evaluated.X);
            int ruleIndex = -1;
            for (int i = 0; i < system.Rules.Count; i++)
            {
                if (system.Rules[i].When.Matches(evaluated, zone, puckState))
                {
                    ruleIndex = i;
                    break;
                }
            }

            if (ruleIndex < 0)
            {
                throw new InvalidOperationException("No rule of system " + system.Id + " matches; the last rule must be the fallback (D-034).");
            }

            IReadOnlyDictionary<SystemRole, Position> roles = RoleAssigner.Assign(skaters, puck, rink);
            var targets = new SortedDictionary<Position, GridPoint>();
            SystemRule rule = system.Rules[ruleIndex];
            foreach (KeyValuePair<SystemRole, Position> role in roles)
            {
                GridPoint target = TargetNode(rule.Targets[role.Key], evaluated, rink);
                targets.Add(role.Value, mirrored ? rink.MirrorY(target) : target);
            }

            return new SystemTargets(ruleIndex, mirrored, roles, targets);
        }

        /// <summary>
        /// A target's node for the (possibly mirrored) puck: <c>node</c> as is; <c>puckOffset</c> added to the puck, x and y
        /// clamped to the grid separately, and a goal node moved to its net front (D-033).
        /// </summary>
        public static GridPoint TargetNode(SystemTarget target, GridPoint puck, Rink rink)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!target.IsPuckOffset)
            {
                return target.Value;
            }

            int x = Clamp(puck.X + target.Value.X, 0, rink.Length - 1);
            int y = Clamp(puck.Y + target.Value.Y, 0, rink.Width - 1);
            var node = new GridPoint(x, y);
            return rink.IsGoalNode(node) ? rink.NetFrontOf(node) : node;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
