using System;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// Play choice when a team gains the puck (D-036). A play fits the puck's zone by its type: breakout ↔ defensive,
    /// zoneEntry ↔ neutral, offensiveZone ↔ offensive (Q-033 default). Faceoff plays are used only at faceoffs and
    /// power-play plays not at 5v5.
    /// </summary>
    public static class PlaySelector
    {
        /// <summary>The play type that fits the zone (team's own view).</summary>
        public static PlayType TypeFor(RinkZone zone)
        {
            switch (zone)
            {
                case RinkZone.Defensive: return PlayType.Breakout;
                case RinkZone.Neutral: return PlayType.ZoneEntry;
                default: return PlayType.OffensiveZone;
            }
        }

        /// <summary>
        /// The play to run, as run (mirrored per D-037 when the puck is on the other side than the play's writing side). The
        /// first play in priority order that fits <paramref name="zone"/> and whose puck carrier as run is
        /// <paramref name="holder"/>; otherwise the first that fits the zone, which then starts with a checked pass to its
        /// puck carrier as run (<paramref name="needsPass"/>). The holder is compared after mirroring (Q-075, D-086). Null
        /// when no play fits the zone (the team keeps the puck in system mode).
        /// </summary>
        /// <param name="plan">The team's shift plan.</param>
        /// <param name="zone">The puck's zone (team's own view).</param>
        /// <param name="holder">The puck holder.</param>
        /// <param name="puck">The puck node (team's own view), deciding the mirroring.</param>
        /// <param name="rink">The rink.</param>
        /// <param name="needsPass">Whether the play starts with a pass to its puck carrier.</param>
        public static Play Select(ShiftPlan plan, RinkZone zone, Position holder, GridPoint puck, Rink rink, out bool needsPass)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            PlayType type = TypeFor(zone);
            Play firstFit = null;
            bool firstFitMirrored = false;
            foreach (Play play in plan.Plays)
            {
                if (play.Type != type)
                {
                    continue;
                }

                bool mirrored = PlayMirror.ShouldMirror(play, puck, rink);
                Position carrier = mirrored ? PlayMirror.MirrorPosition(play.PuckCarrier) : play.PuckCarrier;
                if (carrier == holder)
                {
                    needsPass = false;
                    return mirrored ? PlayMirror.Mirror(play, rink) : play;
                }

                if (firstFit == null)
                {
                    firstFit = play;
                    firstFitMirrored = mirrored;
                }
            }

            needsPass = firstFit != null;
            return firstFit != null && firstFitMirrored ? PlayMirror.Mirror(firstFit, rink) : firstFit;
        }

        /// <summary>
        /// The first faceoff play in priority order for the spot (team's own view id), and whether it runs mirrored
        /// (D-037: used at its own spot and at the mirror-image spot). Null if none.
        /// </summary>
        public static Play FaceoffPlay(ShiftPlan plan, string spotId, out bool mirrored)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            foreach (Play play in plan.Plays)
            {
                if (play.Type != PlayType.Faceoff)
                {
                    continue;
                }

                if (string.Equals(play.FaceoffSpotId, spotId, StringComparison.Ordinal))
                {
                    mirrored = false;
                    return play;
                }

                if (PlayMirror.ShouldMirrorAtSpot(play, spotId))
                {
                    mirrored = true;
                    return play;
                }
            }

            mirrored = false;
            return null;
        }
    }
}
