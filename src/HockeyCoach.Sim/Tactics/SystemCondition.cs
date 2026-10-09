using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// A system rule's <c>when</c>: every present condition must hold (AND). An empty condition always holds and is
    /// allowed only on the last rule (D-034 condition 4).
    /// </summary>
    public sealed class SystemCondition
    {
        private readonly RinkZone[] _puckZones;

        /// <summary>Creates a condition. Null arguments mean "not given".</summary>
        /// <param name="puckZones">Zones the puck may be in (defender's view), or null.</param>
        /// <param name="puckX">Range of the puck's x, or null.</param>
        /// <param name="puckY">Range of the puck's y, or null.</param>
        /// <param name="puckState">Required puck state, or null.</param>
        public SystemCondition(IReadOnlyList<RinkZone> puckZones, IntRange puckX, IntRange puckY, PuckState? puckState)
        {
            if (puckZones != null)
            {
                _puckZones = new RinkZone[puckZones.Count];
                for (int i = 0; i < puckZones.Count; i++)
                {
                    _puckZones[i] = puckZones[i];
                }
            }

            PuckX = puckX;
            PuckY = puckY;
            PuckState = puckState;
        }

        /// <summary>The empty condition <c>{}</c> that always holds.</summary>
        public static SystemCondition Always
        {
            get { return new SystemCondition(null, null, null, null); }
        }

        /// <summary>Zones the puck may be in (defender's view), or null when not given.</summary>
        public IReadOnlyList<RinkZone> PuckZones
        {
            get { return _puckZones; }
        }

        /// <summary>Range of the puck's x, or null when not given.</summary>
        public IntRange PuckX { get; }

        /// <summary>Range of the puck's y, or null when not given.</summary>
        public IntRange PuckY { get; }

        /// <summary>Required puck state, or null when not given.</summary>
        public PuckState? PuckState { get; }

        /// <summary>Whether no condition is given (<c>when: {}</c>).</summary>
        public bool IsEmpty
        {
            get { return _puckZones == null && PuckX == null && PuckY == null && !PuckState.HasValue; }
        }

        /// <summary>Whether the condition holds for the puck (defender's view, already mirrored when applicable).</summary>
        /// <param name="puck">Puck node.</param>
        /// <param name="zone">Zone of the puck node.</param>
        /// <param name="state">Puck state.</param>
        public bool Matches(GridPoint puck, RinkZone zone, PuckState state)
        {
            if (_puckZones != null && System.Array.IndexOf(_puckZones, zone) < 0)
            {
                return false;
            }

            if (PuckX != null && !PuckX.Contains(puck.X))
            {
                return false;
            }

            if (PuckY != null && !PuckY.Contains(puck.Y))
            {
                return false;
            }

            return !PuckState.HasValue || PuckState.Value == state;
        }
    }
}
