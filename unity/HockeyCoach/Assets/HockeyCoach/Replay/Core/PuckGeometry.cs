using System;

namespace HockeyCoach.Replay.Core
{
    /// <summary>
    /// Puck target points of the presentation layer (data-schema.md, replay.json, Kiekko): the point inside the net for
    /// goals and the miss point beside the net for missed shots.
    /// </summary>
    public static class PuckGeometry
    {
        /// <summary>Width of the goal mouth (data-schema.md, replay.json, Ohipiste: 1.83 m).</summary>
        public const double NetWidthMetres = 1.83;

        /// <summary>
        /// The goal's puck point in the own net of <paramref name="conceding"/>: the goal mouth centre moved
        /// <c>goal.netInsideMetres</c> behind the goal line (towards the end boards).
        /// </summary>
        public static RinkPoint NetInside(RinkMapping mapping, ReplayTeam conceding, double netInsideMetres)
        {
            if (mapping == null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            RinkPoint mouth = mapping.NetOf(conceding);
            double x = conceding == ReplayTeam.Home ? mouth.X - netInsideMetres : mouth.X + netInsideMetres;
            return new RinkPoint(x, mouth.Y);
        }

        /// <summary>
        /// The miss point of a shot at the own net of <paramref name="target"/>: on the goal line, half the net width +
        /// <c>puck.missWideMetres</c> from the net centre. The side is the side of <paramref name="puckNodeId"/> relative
        /// to the centre lane; on the centre lane the shooter's side; when the shooter is on it too, the y = 0 side.
        /// </summary>
        public static RinkPoint MissPoint(RinkMapping mapping, ReplayTeam target, int puckNodeId, int shooterNodeId, double missWideMetres)
        {
            if (mapping == null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            RinkPoint mouth = mapping.NetOf(target);
            int side = Side(mapping, puckNodeId);
            if (side == 0)
            {
                side = Side(mapping, shooterNodeId);
            }

            if (side == 0)
            {
                side = -1;
            }

            double offset = (NetWidthMetres / 2) + missWideMetres;
            return new RinkPoint(mouth.X, mouth.Y + (side * offset));
        }

        /// <summary>−1 when the node's lane is on the y = 0 side of the centre lane, +1 on the other side, 0 on it.</summary>
        private static int Side(RinkMapping mapping, int nodeId)
        {
            if (nodeId < 0 || nodeId >= mapping.NodeCount)
            {
                return 0;
            }

            double lane = nodeId % mapping.GridWidth;
            double centre = (mapping.GridWidth - 1) / 2.0;
            return lane < centre ? -1 : lane > centre ? 1 : 0;
        }
    }
}
