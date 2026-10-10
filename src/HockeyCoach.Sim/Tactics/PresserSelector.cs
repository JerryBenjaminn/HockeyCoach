using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// M-7 presser (D-072, Q-054): the one defender who plays the puck carrier and alone puts him under pressure. Pure
    /// functions, no randomness. Coordinates are in the defending team's own view unless stated otherwise.
    /// </summary>
    public static class PresserSelector
    {
        /// <summary>Order of equally long candidates: F1, D1, D2, F2, F3 (D-058 processing order).</summary>
        private static readonly SystemRole[] CandidateOrder = OccupancyResolver.SystemOrder;

        /// <summary>
        /// The presser when a system directs the defence: the system is evaluated exactly as for movement (rule, roles,
        /// mirror), candidates are the roles whose target in the chosen rule is a <c>puckOffset</c> (and D1 when the pinch
        /// replaced its target, length 0); the shortest offset wins (Chebyshev, then Manhattan, then F1, D1, D2, F2, F3).
        /// Returns false when the rule has no candidate (nobody presses).
        /// </summary>
        /// <param name="system">The defending team's system.</param>
        /// <param name="rink">The rink.</param>
        /// <param name="puck">Puck node (the carrier's node), defending team's view.</param>
        /// <param name="puckState">Controlled or loose.</param>
        /// <param name="skaters">The defending team's five skaters, its own view.</param>
        /// <param name="pinched">Whether the pinch instruction (O-10) replaced D1's target with the puck node.</param>
        /// <param name="presser">The presser's position.</param>
        public static bool FromSystem(
            DefensiveSystem system,
            Rink rink,
            GridPoint puck,
            PuckState puckState,
            IReadOnlyDictionary<Position, GridPoint> skaters,
            bool pinched,
            out Position presser)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            SystemTargets targets = SystemTargetResolver.Resolve(system, rink, puck, puckState, skaters);
            SystemRule rule = system.Rules[targets.RuleIndex];
            bool found = false;
            int bestCheb = 0;
            int bestMan = 0;
            presser = default;
            foreach (SystemRole role in CandidateOrder)
            {
                int cheb;
                int man;
                if (pinched && role == SystemRole.D1)
                {
                    cheb = 0;
                    man = 0;
                }
                else
                {
                    SystemTarget target = rule.Targets[role];
                    if (!target.IsPuckOffset)
                    {
                        continue;
                    }

                    int dx = Math.Abs(target.Value.X);
                    int dy = Math.Abs(target.Value.Y);
                    cheb = Math.Max(dx, dy);
                    man = dx + dy;
                }

                if (!found || cheb < bestCheb || (cheb == bestCheb && man < bestMan))
                {
                    found = true;
                    bestCheb = cheb;
                    bestMan = man;
                    presser = targets.Roles[role];
                }
            }

            return found;
        }

        /// <summary>
        /// The presser when no system directs the defence (rush O-7, second chance O-8): the defender closest to the
        /// carrier (Chebyshev, then Manhattan, then C, LW, RW, LD, RD). Any common view for both arguments.
        /// </summary>
        /// <param name="defenders">The defending skaters' nodes.</param>
        /// <param name="carrier">The carrier's node.</param>
        /// <param name="presser">The presser's position; false only when there are no defenders.</param>
        public static bool Nearest(IReadOnlyDictionary<Position, GridPoint> defenders, GridPoint carrier, out Position presser)
        {
            return LineGeometry.Closest(defenders, new[] { carrier }, carrier, int.MaxValue, out presser, out int unused);
        }
    }
}
