using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Model
{
    /// <summary>
    /// A forward trio (D-023): left wing, centre, right wing. Rotates independently of defence pairs.
    /// A skater may fill a slot other than his primary position (see <see cref="Positions.IsOffSide"/>).
    /// </summary>
    public sealed class ForwardLine
    {
        /// <summary>Number of skaters in a forward trio.</summary>
        public const int Size = 3;

        private readonly Skater[] _skaters;

        /// <summary>Creates a forward trio. The three skaters must be different players.</summary>
        public ForwardLine(string name, Skater leftWing, Skater center, Skater rightWing)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            LeftWing = leftWing ?? throw new ArgumentNullException(nameof(leftWing));
            Center = center ?? throw new ArgumentNullException(nameof(center));
            RightWing = rightWing ?? throw new ArgumentNullException(nameof(rightWing));
            _skaters = new[] { leftWing, center, rightWing };
            Units.RequireDistinct(_skaters, "forward line " + name);
        }

        /// <summary>Display name, e.g. "F1".</summary>
        public string Name { get; }

        /// <summary>Skater in the LW slot.</summary>
        public Skater LeftWing { get; }

        /// <summary>Skater in the C slot.</summary>
        public Skater Center { get; }

        /// <summary>Skater in the RW slot.</summary>
        public Skater RightWing { get; }

        /// <summary>Skaters in slot order LW, C, RW.</summary>
        public IReadOnlyList<Skater> Skaters
        {
            get { return _skaters; }
        }

        /// <summary>The slot (LW, C or RW) the skater plays in this trio, or null if he is not in it.</summary>
        public Position? SlotOf(Skater skater)
        {
            if (skater == null)
            {
                return null;
            }

            if (skater.Id == LeftWing.Id)
            {
                return Position.LeftWing;
            }

            if (skater.Id == Center.Id)
            {
                return Position.Center;
            }

            if (skater.Id == RightWing.Id)
            {
                return Position.RightWing;
            }

            return null;
        }
    }
}
