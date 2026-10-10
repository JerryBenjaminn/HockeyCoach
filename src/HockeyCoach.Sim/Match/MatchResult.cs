using System.Collections.Generic;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.State;

namespace HockeyCoach.Sim.Match
{
    /// <summary>Outcome of a match: the event log, the score, period snapshots and the final state.</summary>
    public sealed class MatchResult
    {
        internal MatchResult(EventLog log, int homeGoals, int awayGoals, bool stalled, IReadOnlyList<PeriodSnapshot> periods, GameState state)
        {
            Log = log;
            HomeGoals = homeGoals;
            AwayGoals = awayGoals;
            Stalled = stalled;
            Periods = periods;
            State = state;
        }

        /// <summary>The whole match's event log.</summary>
        public EventLog Log { get; }

        /// <summary>Home goals.</summary>
        public int HomeGoals { get; }

        /// <summary>Away goals.</summary>
        public int AwayGoals { get; }

        /// <summary>The match hit an anti-stall cap and ended early (should not happen).</summary>
        public bool Stalled { get; }

        /// <summary>One snapshot per completed period.</summary>
        public IReadOnlyList<PeriodSnapshot> Periods { get; }

        /// <summary>The final state (pressure samples, lineups, familiarity).</summary>
        public GameState State { get; }
    }
}
