using System;
using System.Collections.Generic;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.Model;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.Sim.Match
{
    /// <summary>
    /// What a coach sees when asked (O-4, D-077): public information only. The own roster, the units on the ice for both
    /// teams, the clock and score, why the coach is asked, the own ice and bench time, the own play-use counter and the public event log. No energy,
    /// organization, pressure, opponent plan or random state. The view is a snapshot: it does not change after it is
    /// made, and both coaches' views at a stoppage are made before either coach decides (simultaneous decisions).
    /// </summary>
    public sealed class CoachView
    {
        /// <summary>Creates the view.</summary>
        /// <param name="side">The coach's team side.</param>
        /// <param name="team">The coach's own roster.</param>
        /// <param name="clock">Period and time.</param>
        /// <param name="call">Why the coach is asked.</param>
        /// <param name="stoppageReason">The stoppage reason when <paramref name="call"/> is <see cref="CoachCall.Stoppage"/>, otherwise null.</param>
        /// <param name="ownGoals">Own goals.</param>
        /// <param name="opponentGoals">Opponent goals.</param>
        /// <param name="onIce">Own trio and pair on the ice (−1 before the first faceoff).</param>
        /// <param name="opponentOnIce">Opponent trio and pair on the ice (−1 before the first faceoff).</param>
        /// <param name="canChangeForwards">The own trio may change now (O-2).</param>
        /// <param name="canChangeDefence">The own pair may change now (O-2).</param>
        /// <param name="iceTime">Own ice time per unit.</param>
        /// <param name="timeouts">Timeouts left for both teams and whether one may be called now (AI-4).</param>
        /// <param name="playUses">Own play uses this match (O-11 counter), by play id; copied.</param>
        /// <param name="events">The public event log so far; must not change afterwards (e.g. <see cref="EventLog.Snapshot"/>).</param>
        public CoachView(
            TeamSide side,
            Team team,
            CoachClock clock,
            CoachCall call,
            StoppageReason? stoppageReason,
            int ownGoals,
            int opponentGoals,
            LineChoice onIce,
            LineChoice opponentOnIce,
            bool canChangeForwards,
            bool canChangeDefence,
            IceTimeView iceTime,
            CoachTimeouts timeouts,
            IReadOnlyDictionary<string, double> playUses,
            IReadOnlyList<SimEvent> events)
        {
            Side = side;
            Team = team ?? throw new ArgumentNullException(nameof(team));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if ((call == CoachCall.Stoppage) != stoppageReason.HasValue)
            {
                throw new ArgumentException("A stoppage reason is given exactly when the coach is asked at a stoppage.", nameof(stoppageReason));
            }

            Call = call;
            StoppageReason = stoppageReason;
            OwnGoals = ownGoals;
            OpponentGoals = opponentGoals;
            OnIce = onIce ?? throw new ArgumentNullException(nameof(onIce));
            OpponentOnIce = opponentOnIce ?? throw new ArgumentNullException(nameof(opponentOnIce));
            CanChangeForwards = canChangeForwards;
            CanChangeDefence = canChangeDefence;
            IceTime = iceTime ?? throw new ArgumentNullException(nameof(iceTime));
            Timeouts = timeouts ?? throw new ArgumentNullException(nameof(timeouts));
            if (playUses == null)
            {
                throw new ArgumentNullException(nameof(playUses));
            }

            var uses = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, double> entry in playUses)
            {
                uses.Add(entry.Key, entry.Value);
            }

            PlayUses = uses;
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        /// <summary>The coach's team side.</summary>
        public TeamSide Side { get; }

        /// <summary>The coach's own roster.</summary>
        public Team Team { get; }

        /// <summary>Period and time.</summary>
        public CoachClock Clock { get; }

        /// <summary>Period number (same as <see cref="CoachClock.Period"/>).</summary>
        public int Period
        {
            get { return Clock.Period; }
        }

        /// <summary>Period time (same as <see cref="CoachClock.Time"/>).</summary>
        public double Time
        {
            get { return Clock.Time; }
        }

        /// <summary>Why the coach is asked.</summary>
        public CoachCall Call { get; }

        /// <summary>The stoppage reason at a stoppage, otherwise null.</summary>
        public StoppageReason? StoppageReason { get; }

        /// <summary>Own goals.</summary>
        public int OwnGoals { get; }

        /// <summary>Opponent goals.</summary>
        public int OpponentGoals { get; }

        /// <summary>Own trio and pair on the ice, −1 before the first faceoff.</summary>
        public LineChoice OnIce { get; }

        /// <summary>Opponent trio and pair on the ice, −1 before the first faceoff.</summary>
        public LineChoice OpponentOnIce { get; }

        /// <summary>Own trio on the ice, −1 before the first faceoff.</summary>
        public int ForwardIndex
        {
            get { return OnIce.ForwardIndex; }
        }

        /// <summary>Own pair on the ice, −1 before the first faceoff.</summary>
        public int PairIndex
        {
            get { return OnIce.PairIndex; }
        }

        /// <summary>The own trio may change now (O-2).</summary>
        public bool CanChangeForwards { get; }

        /// <summary>The own pair may change now (O-2).</summary>
        public bool CanChangeDefence { get; }

        /// <summary>Own ice and bench time per unit.</summary>
        public IceTimeView IceTime { get; }

        /// <summary>Timeouts left for both teams and whether one may be called now (AI-4).</summary>
        public CoachTimeouts Timeouts { get; }

        /// <summary>Own play uses this match (O-11 counter), by play id, in ordinal order.</summary>
        public IReadOnlyDictionary<string, double> PlayUses { get; }

        /// <summary>The public event log up to this moment.</summary>
        public IReadOnlyList<SimEvent> Events { get; }
    }
}
