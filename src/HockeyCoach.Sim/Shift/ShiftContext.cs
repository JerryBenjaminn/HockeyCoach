using System;
using HockeyCoach.Sim.Events;
using HockeyCoach.Sim.State;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// What a shift segment shares with the match around it: the persistent game state, the event log the segment
    /// appends to, the host for on-the-fly changes, and the units each team puts on the ice at the faceoff (stoppage
    /// changes are applied and logged by the segment before the faceoff, O-2).
    /// </summary>
    public sealed class ShiftContext
    {
        private readonly LineChoice[] _startUnits = new LineChoice[2];

        /// <summary>Creates the context.</summary>
        /// <param name="state">The match state.</param>
        /// <param name="log">The match event log.</param>
        /// <param name="host">On-the-fly change host; null = no on-the-fly changes.</param>
        /// <param name="homeUnits">Home units at the faceoff.</param>
        /// <param name="awayUnits">Away units at the faceoff.</param>
        public ShiftContext(GameState state, EventLog log, IShiftHost host, LineChoice homeUnits, LineChoice awayUnits)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Log = log ?? throw new ArgumentNullException(nameof(log));
            Host = host;
            _startUnits[(int)TeamSide.Home] = homeUnits ?? throw new ArgumentNullException(nameof(homeUnits));
            _startUnits[(int)TeamSide.Away] = awayUnits ?? throw new ArgumentNullException(nameof(awayUnits));
        }

        /// <summary>The match state.</summary>
        public GameState State { get; }

        /// <summary>The match event log.</summary>
        public EventLog Log { get; }

        /// <summary>On-the-fly change host, or null.</summary>
        public IShiftHost Host { get; }

        /// <summary>The units <paramref name="team"/> puts on the ice at the faceoff.</summary>
        public LineChoice StartUnits(TeamSide team)
        {
            return _startUnits[(int)team];
        }
    }
}
