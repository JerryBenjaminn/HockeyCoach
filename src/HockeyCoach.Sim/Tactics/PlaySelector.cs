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
        /// The first play in priority order that fits <paramref name="zone"/> and starts with <paramref name="holder"/>;
        /// otherwise the first that fits the zone, which then starts with a checked pass to its puck carrier
        /// (<paramref name="needsPass"/>). Null when no play fits the zone (the team keeps the puck in system mode).
        /// </summary>
        public static Play Select(ShiftPlan plan, RinkZone zone, Position holder, out bool needsPass)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            PlayType type = TypeFor(zone);
            Play firstFit = null;
            foreach (Play play in plan.Plays)
            {
                if (play.Type != type)
                {
                    continue;
                }

                if (play.PuckCarrier == holder)
                {
                    needsPass = false;
                    return play;
                }

                if (firstFit == null)
                {
                    firstFit = play;
                }
            }

            needsPass = firstFit != null;
            return firstFit;
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
