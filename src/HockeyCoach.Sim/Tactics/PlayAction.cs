using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>
    /// The one puck action of a beat (D-031, D-032). <see cref="Actor"/> is <c>by</c> (or <c>from</c> for a pass);
    /// <see cref="Receiver"/> is the pass <c>to</c>; <see cref="Target"/> is the node of <c>skate</c> and <c>dump</c>.
    /// Coordinates are in the play's own-team view.
    /// </summary>
    public sealed class PlayAction
    {
        private PlayAction(PlayActionType type, Position actor, Position? receiver, GridPoint? target)
        {
            Type = type;
            Actor = actor;
            Receiver = receiver;
            Target = target;
        }

        /// <summary>Action type.</summary>
        public PlayActionType Type { get; }

        /// <summary>The acting position: <c>by</c>, or <c>from</c> of a pass.</summary>
        public Position Actor { get; }

        /// <summary>Pass receiver (<c>to</c>); null for other actions.</summary>
        public Position? Receiver { get; }

        /// <summary>Target node of <c>skate</c> and <c>dump</c>; null for other actions.</summary>
        public GridPoint? Target { get; }

        /// <summary>Puck carrier skates to <paramref name="to"/>.</summary>
        public static PlayAction Skate(Position by, GridPoint to)
        {
            return new PlayAction(PlayActionType.Skate, by, null, to);
        }

        /// <summary>Puck carrier <paramref name="from"/> passes to <paramref name="to"/>.</summary>
        public static PlayAction Pass(Position from, Position to)
        {
            return new PlayAction(PlayActionType.Pass, from, to, null);
        }

        /// <summary>Puck carrier shoots.</summary>
        public static PlayAction Shoot(Position by)
        {
            return new PlayAction(PlayActionType.Shoot, by, null, null);
        }

        /// <summary>Player without the puck drives to the net front.</summary>
        public static PlayAction DriveNet(Position by)
        {
            return new PlayAction(PlayActionType.DriveNet, by, null, null);
        }

        /// <summary>Puck carrier dumps the puck to <paramref name="to"/>.</summary>
        public static PlayAction Dump(Position by, GridPoint to)
        {
            return new PlayAction(PlayActionType.Dump, by, null, to);
        }

        /// <summary>Whether the action ends the play (<c>shoot</c>, <c>dump</c>).</summary>
        public bool EndsPlay
        {
            get { return Type == PlayActionType.Shoot || Type == PlayActionType.Dump; }
        }

        /// <summary>Whether the actor must be the puck carrier at the start of the beat (all but <c>driveNet</c>).</summary>
        public bool ActorHasPuck
        {
            get { return Type != PlayActionType.DriveNet; }
        }
    }
}
