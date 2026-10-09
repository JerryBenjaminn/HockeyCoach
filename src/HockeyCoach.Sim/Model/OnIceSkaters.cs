using System;
using System.Collections.Generic;

namespace HockeyCoach.Sim.Model
{
    /// <summary>
    /// The five skaters of one team on the ice at 5v5: always one forward trio and one defence pair (D-023).
    /// Any trio can play with any pair, but the same player cannot be in both.
    /// </summary>
    public sealed class OnIceSkaters
    {
        /// <summary>Number of skaters on the ice at even strength.</summary>
        public const int Count = ForwardLine.Size + DefencePair.Size;

        private readonly Skater[] _skaters;

        /// <summary>Combines a trio and a pair. Throws if they share a player.</summary>
        public OnIceSkaters(ForwardLine forwards, DefencePair defence)
        {
            Forwards = forwards ?? throw new ArgumentNullException(nameof(forwards));
            Defence = defence ?? throw new ArgumentNullException(nameof(defence));
            _skaters = new[]
            {
                forwards.LeftWing, forwards.Center, forwards.RightWing, defence.LeftDefence, defence.RightDefence,
            };
            Units.RequireDistinct(_skaters, "on-ice unit " + forwards.Name + "+" + defence.Name);
        }

        /// <summary>The forward trio on the ice.</summary>
        public ForwardLine Forwards { get; }

        /// <summary>The defence pair on the ice.</summary>
        public DefencePair Defence { get; }

        /// <summary>The five skaters in slot order LW, C, RW, LD, RD.</summary>
        public IReadOnlyList<Skater> Skaters
        {
            get { return _skaters; }
        }

        /// <summary>The slot the skater plays, or null if he is not on the ice.</summary>
        public Position? SlotOf(Skater skater)
        {
            return Forwards.SlotOf(skater) ?? Defence.SlotOf(skater);
        }

        /// <summary>Whether the skater is on the ice and playing the opposite side of his primary position.</summary>
        public bool IsOffSide(Skater skater)
        {
            Position? slot = SlotOf(skater);
            return slot.HasValue && Positions.IsOffSide(skater.PrimaryPosition, slot.Value);
        }
    }
}
