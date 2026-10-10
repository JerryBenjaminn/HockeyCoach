using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// Validates a defensive system against docs/data-schema.md (Puolustusjärjestelmät → Validointi; D-033, D-034).
    /// File-level rules (schema version, unknown fields and keys, unique ids, unknown zone, role or puck state names,
    /// exactly one of node and puckOffset) are checked by the loader.
    /// </summary>
    public static class SystemValidator
    {
        /// <summary>Returns every problem found; empty means valid.</summary>
        public static IReadOnlyList<string> Validate(DefensiveSystem system, Rink rink)
        {
            var errors = new List<string>();
            if (system == null || rink == null)
            {
                errors.Add("(root): system and rink are required");
                return errors;
            }

            if (system.Rules.Count == 0)
            {
                errors.Add("rules: must not be empty");
                return errors;
            }

            for (int i = 0; i < system.Rules.Count; i++)
            {
                string path = "rules[" + i + "]";
                SystemRule rule = system.Rules[i];
                bool last = i == system.Rules.Count - 1;
                if (last && !rule.When.IsEmpty)
                {
                    errors.Add(path + ".when: the last rule is the fallback and must be empty {} (D-034)");
                }
                else if (!last && rule.When.IsEmpty)
                {
                    errors.Add(path + ".when: only the last rule may be empty; the rules after it would never match");
                }

                ValidateCondition(path + ".when", rule.When, system.MirrorY, rink, errors);
                ValidateTargets(path + ".targets", rule, rink, errors);
                ValidateDistinctTargets(path + ".targets", rule, errors);
            }

            return errors;
        }

        private static void ValidateCondition(string path, SystemCondition when, bool mirrorY, Rink rink, List<string> errors)
        {
            if (when.PuckZones != null && when.PuckZones.Count == 0)
            {
                errors.Add(path + ".puckZones: must not be empty");
            }

            ValidateRange(path + ".puckX", when.PuckX, rink.Length - 1, errors);
            ValidateRange(path + ".puckY", when.PuckY, rink.Width - 1, errors);
            if (mirrorY && when.PuckY != null && when.PuckY.Max > rink.CentreLaneY)
            {
                errors.Add(path + ".puckY: a mirrorY system is written for the puck at y <= " + rink.CentreLaneY + ", range " + when.PuckY + " reaches the right side");
            }
        }

        private static void ValidateRange(string path, IntRange range, int max, List<string> errors)
        {
            if (range == null)
            {
                return;
            }

            if (range.Min > range.Max)
            {
                errors.Add(path + ": min (" + range.Min + ") is greater than max (" + range.Max + ")");
            }

            if (range.Min < 0 || range.Max > max)
            {
                errors.Add(path + ": " + range + " is outside the grid (0.." + max + ")");
            }
        }

        /// <summary>
        /// D-058 (D-062): two roles with the same fixed <c>node</c>, or the same <c>puckOffset</c>, would always put two
        /// teammates on one node. Targets that meet only for some puck nodes are resolved at run time (OccupancyResolver).
        /// </summary>
        private static void ValidateDistinctTargets(string path, SystemRule rule, List<string> errors)
        {
            var seen = new List<KeyValuePair<SystemRole, SystemTarget>>();
            foreach (SystemRole role in SystemNames.AllRoles)
            {
                if (!rule.Targets.TryGetValue(role, out SystemTarget target))
                {
                    continue;
                }

                foreach (KeyValuePair<SystemRole, SystemTarget> other in seen)
                {
                    if (other.Value.IsPuckOffset == target.IsPuckOffset && other.Value.Value.Equals(target.Value))
                    {
                        errors.Add(path + ": " + SystemNames.ToName(other.Key) + " and " + SystemNames.ToName(role) + " have the same target ("
                            + target + "); one skater per team per node (D-058)");
                    }
                }

                seen.Add(new KeyValuePair<SystemRole, SystemTarget>(role, target));
            }
        }

        private static void ValidateTargets(string path, SystemRule rule, Rink rink, List<string> errors)
        {
            foreach (SystemRole role in SystemNames.AllRoles)
            {
                if (!rule.Targets.TryGetValue(role, out SystemTarget target))
                {
                    errors.Add(path + "." + SystemNames.ToName(role) + ": missing (all of F1, F2, F3, D1, D2 are required)");
                    continue;
                }

                if (target.IsPuckOffset)
                {
                    continue;
                }

                string nodePath = path + "." + SystemNames.ToName(role) + ".node";
                if (!rink.Contains(target.Value))
                {
                    errors.Add(nodePath + ": " + target.Value + " is outside the " + rink.Length + "x" + rink.Width + " grid");
                }
                else if (rink.IsGoalNode(target.Value))
                {
                    errors.Add(nodePath + ": " + target.Value + " is a goal node; skaters never stand there (D-033)");
                }
            }
        }
    }
}
