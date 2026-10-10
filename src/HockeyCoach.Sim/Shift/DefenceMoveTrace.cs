using System.Collections.Generic;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>Which kind of placement moved the defence (diagnostics for tests only).</summary>
    internal enum DefenceMoveKind
    {
        /// <summary>D-041 phase 2 of a play beat or a rush action: the only move the presser reaction delay can apply to (D-074).</summary>
        Action,

        /// <summary>Any other system move: setup, regroup setup, system step, loose-puck battle.</summary>
        Other,
    }

    /// <summary>
    /// One system move of a defending team and whether the presser reaction delay (D-074) applied to it. Diagnostics for
    /// tests only; it never affects the simulation. Nodes are in the defending team's own view.
    /// </summary>
    internal sealed class DefenceMoveSample
    {
        internal DefenceMoveSample(
            DefenceMoveKind kind,
            TeamSide defending,
            bool rush,
            int logCount,
            GridPoint puck,
            IReadOnlyDictionary<Position, GridPoint> before,
            IReadOnlyDictionary<Position, GridPoint> after,
            Position? delayedPresser)
        {
            Kind = kind;
            Defending = defending;
            Rush = rush;
            LogCount = logCount;
            Puck = puck;
            Before = before;
            After = after;
            DelayedPresser = delayedPresser;
        }

        /// <summary>The kind of move.</summary>
        internal DefenceMoveKind Kind { get; }

        /// <summary>The moving (defending) team.</summary>
        internal TeamSide Defending { get; }

        /// <summary>A rush action (the nearest defender is the presser, M-7 point 2) rather than a play beat.</summary>
        internal bool Rush { get; }

        /// <summary>Events in the log when the move was made.</summary>
        internal int LogCount { get; }

        /// <summary>The puck node during the move (the carrier does not move in phase 2).</summary>
        internal GridPoint Puck { get; }

        /// <summary>Defenders' nodes before the move.</summary>
        internal IReadOnlyDictionary<Position, GridPoint> Before { get; }

        /// <summary>Defenders' nodes after the move.</summary>
        internal IReadOnlyDictionary<Position, GridPoint> After { get; }

        /// <summary>The presser whose steps were limited to <c>pressure.presserReactionNodes</c>, or null when no delay applied.</summary>
        internal Position? DelayedPresser { get; }
    }

    /// <summary>Receives a <see cref="DefenceMoveSample"/> for every defence system move (tests only).</summary>
    internal interface IDefenceMoveTrace
    {
        /// <summary>Called once per defence system move, right after it.</summary>
        void OnDefenceMove(DefenceMoveSample sample);
    }
}
