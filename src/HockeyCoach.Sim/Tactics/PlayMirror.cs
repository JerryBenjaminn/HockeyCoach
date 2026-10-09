using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// Mirroring of plays across the long axis (D-034 condition 3, data-schema.md: Peilaus) and the deterministic
    /// choice of the mirrored side (D-037).
    /// </summary>
    public static class PlayMirror
    {
        private const string LeftSuffix = "Left";
        private const string RightSuffix = "Right";

        /// <summary>The mirrored position: LW ↔ RW, LD ↔ RD, C stays.</summary>
        public static Position MirrorPosition(Position position)
        {
            switch (position)
            {
                case Position.LeftWing: return Position.RightWing;
                case Position.RightWing: return Position.LeftWing;
                case Position.LeftDefence: return Position.RightDefence;
                case Position.RightDefence: return Position.LeftDefence;
                default: return position;
            }
        }

        /// <summary>The mirrored faceoff spot id: <c>...Left</c> ↔ <c>...Right</c>, others (e.g. <c>center</c>) stay.</summary>
        public static string MirrorFaceoffSpotId(string spotId)
        {
            if (spotId == null)
            {
                return null;
            }

            if (spotId.EndsWith(LeftSuffix, StringComparison.Ordinal))
            {
                return spotId.Substring(0, spotId.Length - LeftSuffix.Length) + RightSuffix;
            }

            if (spotId.EndsWith(RightSuffix, StringComparison.Ordinal))
            {
                return spotId.Substring(0, spotId.Length - RightSuffix.Length) + LeftSuffix;
            }

            return spotId;
        }

        /// <summary>
        /// The play mirrored: nodes y → width - 1 - y, positions LW ↔ RW and LD ↔ RD, faceoff spot Left ↔ Right.
        /// The id and name stay the same. Mirroring twice gives back an equal play.
        /// </summary>
        public static Play Mirror(Play play, Rink rink)
        {
            if (play == null)
            {
                throw new ArgumentNullException(nameof(play));
            }

            if (rink == null)
            {
                throw new ArgumentNullException(nameof(rink));
            }

            var beats = new List<Beat>(play.Beats.Count);
            foreach (Beat beat in play.Beats)
            {
                beats.Add(new Beat(MirrorMap(beat.Moves, rink), MirrorAction(beat.Action, rink)));
            }

            return new Play(
                play.Id,
                play.Name,
                play.Type,
                MirrorFaceoffSpotId(play.FaceoffSpotId),
                MirrorPosition(play.PuckCarrier),
                MirrorMap(play.StartPositions, rink),
                beats);
        }

        /// <summary>The play's writing side: the lane of the puck carrier's start node.</summary>
        public static Lane WritingLane(Play play, Rink rink)
        {
            if (play == null)
            {
                throw new ArgumentNullException(nameof(play));
            }

            if (!play.StartPositions.TryGetValue(play.PuckCarrier, out GridPoint start))
            {
                throw new ArgumentException("Play " + play.Id + " has no start node for its puck carrier.", nameof(play));
            }

            return rink.LaneOf(start.Y);
        }

        /// <summary>
        /// Whether a non-faceoff play is run mirrored (D-037): the puck's node when the play starts (attacking team's
        /// view) is on the other side than the writing side. If either is the middle lane, the play is run as written.
        /// </summary>
        public static bool ShouldMirror(Play play, GridPoint puckNode, Rink rink)
        {
            Lane written = WritingLane(play, rink);
            Lane puck = rink.LaneOf(puckNode.Y);
            return written != Lane.Middle && puck != Lane.Middle && written != puck;
        }

        /// <summary>
        /// Whether a faceoff play is run mirrored at <paramref name="spotId"/>: the spot is the mirror image of the play's
        /// own spot (and not the spot itself, e.g. <c>center</c>).
        /// </summary>
        public static bool ShouldMirrorAtSpot(Play play, string spotId)
        {
            if (play == null)
            {
                throw new ArgumentNullException(nameof(play));
            }

            return play.FaceoffSpotId != null
                && !string.Equals(spotId, play.FaceoffSpotId, StringComparison.Ordinal)
                && string.Equals(spotId, MirrorFaceoffSpotId(play.FaceoffSpotId), StringComparison.Ordinal);
        }

        private static List<KeyValuePair<Position, GridPoint>> MirrorMap(IReadOnlyDictionary<Position, GridPoint> map, Rink rink)
        {
            var mirrored = new List<KeyValuePair<Position, GridPoint>>(map.Count);
            foreach (KeyValuePair<Position, GridPoint> entry in map)
            {
                mirrored.Add(new KeyValuePair<Position, GridPoint>(MirrorPosition(entry.Key), rink.MirrorY(entry.Value)));
            }

            return mirrored;
        }

        private static PlayAction MirrorAction(PlayAction action, Rink rink)
        {
            Position actor = MirrorPosition(action.Actor);
            switch (action.Type)
            {
                case PlayActionType.Skate: return PlayAction.Skate(actor, rink.MirrorY(action.Target.Value));
                case PlayActionType.Pass: return PlayAction.Pass(actor, MirrorPosition(action.Receiver.Value));
                case PlayActionType.Shoot: return PlayAction.Shoot(actor);
                case PlayActionType.DriveNet: return PlayAction.DriveNet(actor);
                default: return PlayAction.Dump(actor, rink.MirrorY(action.Target.Value));
            }
        }
    }
}
