using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Match
{
    /// <summary>A read-only snapshot of the match for a coach (O-4). Coaches get no random generator before milestone 4.</summary>
    public sealed class CoachView
    {
        /// <summary>Creates the view.</summary>
        public CoachView(
            TeamSide side,
            Team team,
            int period,
            double time,
            int ownGoals,
            int opponentGoals,
            int forwardIndex,
            int pairIndex,
            bool canChangeForwards,
            bool canChangeDefence,
            IReadOnlyDictionary<string, double> playUses,
            IReadOnlyDictionary<int, double> energy)
        {
            Side = side;
            Team = team ?? throw new ArgumentNullException(nameof(team));
            Period = period;
            Time = time;
            OwnGoals = ownGoals;
            OpponentGoals = opponentGoals;
            ForwardIndex = forwardIndex;
            PairIndex = pairIndex;
            CanChangeForwards = canChangeForwards;
            CanChangeDefence = canChangeDefence;
            PlayUses = playUses ?? throw new ArgumentNullException(nameof(playUses));
            Energy = energy ?? throw new ArgumentNullException(nameof(energy));
        }

        /// <summary>The coach's team side.</summary>
        public TeamSide Side { get; }

        /// <summary>The coach's team.</summary>
        public Team Team { get; }

        /// <summary>Period number.</summary>
        public int Period { get; }

        /// <summary>Period time.</summary>
        public double Time { get; }

        /// <summary>Own goals.</summary>
        public int OwnGoals { get; }

        /// <summary>Opponent goals.</summary>
        public int OpponentGoals { get; }

        /// <summary>Trio on the ice, −1 before the first faceoff.</summary>
        public int ForwardIndex { get; }

        /// <summary>Pair on the ice, −1 before the first faceoff.</summary>
        public int PairIndex { get; }

        /// <summary>The trio may change now (O-2).</summary>
        public bool CanChangeForwards { get; }

        /// <summary>The pair may change now (O-2).</summary>
        public bool CanChangeDefence { get; }

        /// <summary>Play uses this match (O-11 counter), by play id.</summary>
        public IReadOnlyDictionary<string, double> PlayUses { get; }

        /// <summary>Current energy of the team's players, by id.</summary>
        public IReadOnlyDictionary<int, double> Energy { get; }
    }
}
