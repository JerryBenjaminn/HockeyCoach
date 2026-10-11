using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// Pure rules of the own-zone regroup challenge (data-schema.md O-7, "Karvaus haastaa kokoamisen omassa päässä",
    /// D-078 K1): who counts as a forechecker, when the breakout check is made, who takes the puck on failure and how the
    /// defence's regroup and setup steps split.
    /// </summary>
    internal static class RegroupChallenge
    {
        private static readonly SystemRole[] ForecheckerRoles = { SystemRole.F1, SystemRole.F2, SystemRole.F3 };

        /// <summary>
        /// The defending team's skaters in roles F1-F3 (in role order) whose node is in the attacking team's defensive zone.
        /// The real count: no M-1 fallback.
        /// </summary>
        /// <param name="roles">The defending team's system roles.</param>
        /// <param name="defendersInAttackerView">The defending skaters' nodes in the attacking team's view.</param>
        /// <param name="rink">The rink.</param>
        internal static IReadOnlyList<Position> ForecheckersInZone(
            IReadOnlyDictionary<SystemRole, Position> roles,
            IReadOnlyDictionary<Position, GridPoint> defendersInAttackerView,
            Rink rink)
        {
            var inZone = new List<Position>();
            foreach (SystemRole role in ForecheckerRoles)
            {
                Position position = roles[role];
                if (rink.ZoneAtX(defendersInAttackerView[position].X) == RinkZone.Defensive)
                {
                    inZone.Add(position);
                }
            }

            return inZone;
        }

        /// <summary>The breakout check is made when f exceeds <c>transitions.regroupForecheckersAllowed</c> (O-7 point 4).</summary>
        /// <param name="forecheckers">f: forecheckers in the zone after the regroup move, 0..3.</param>
        /// <param name="allowed"><c>transitions.regroupForecheckersAllowed</c>.</param>
        internal static bool IsChallenged(int forecheckers, int allowed)
        {
            return forecheckers > allowed;
        }

        /// <summary>
        /// O-7 point 5: the forechecker who takes the puck on failure, nearest to the carrier (Chebyshev, then Manhattan, then
        /// C, LW, RW). Only the counted forecheckers are candidates.
        /// </summary>
        /// <param name="forecheckers">The f forecheckers.</param>
        /// <param name="defenderNodes">The defending skaters' nodes (any view common with <paramref name="carrier"/>).</param>
        /// <param name="carrier">The carrier's node.</param>
        internal static Position NearestForechecker(IReadOnlyList<Position> forecheckers, IReadOnlyDictionary<Position, GridPoint> defenderNodes, GridPoint carrier)
        {
            var candidates = new SortedDictionary<Position, GridPoint>();
            foreach (Position position in forecheckers)
            {
                candidates.Add(position, defenderNodes[position]);
            }

            if (!PresserSelector.Nearest(candidates, carrier, out Position nearest))
            {
                throw new InvalidOperationException("No forechecker to take the puck.");
            }

            return nearest;
        }

        /// <summary>
        /// O-7 point 6: the defence's steps in the setup after an own-zone regroup, n_set = n_tot − n_reg, so the regroup
        /// and the setup together move n_tot steps. An unbounded total stays unbounded.
        /// </summary>
        /// <param name="totalSteps">n_tot: the steps of regroupSeconds + setupSeconds (O-6).</param>
        /// <param name="regroupSteps">n_reg: the steps of regroupSeconds.</param>
        /// <param name="unbounded">The step count that means "no limit".</param>
        internal static int SetupStepsAfterRegroup(int totalSteps, int regroupSteps, int unbounded)
        {
            return totalSteps >= unbounded ? unbounded : totalSteps - regroupSteps;
        }
    }
}
