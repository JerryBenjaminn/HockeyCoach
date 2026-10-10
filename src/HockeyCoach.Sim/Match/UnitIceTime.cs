namespace HockeyCoach.Sim.Match
{
    /// <summary>
    /// Ice and bench time of one own unit (trio or pair) as a coach sees it (AI-1, AI-5): accumulated ice time and shifts
    /// this match, and the current shift or bench time in this period.
    /// </summary>
    public sealed class UnitIceTime
    {
        /// <summary>Creates the entry.</summary>
        /// <param name="totalSeconds">Accumulated ice time this match, in game seconds.</param>
        /// <param name="shifts">Shifts this match.</param>
        /// <param name="onIce">The unit is on the ice now.</param>
        /// <param name="currentShiftSeconds">Game seconds of the current shift; 0 when on the bench.</param>
        /// <param name="benchSeconds">Game seconds on the bench in this period (since it left the ice, or since the period start); 0 when on the ice.</param>
        public UnitIceTime(double totalSeconds, int shifts, bool onIce, double currentShiftSeconds, double benchSeconds)
        {
            TotalSeconds = totalSeconds;
            Shifts = shifts;
            OnIce = onIce;
            CurrentShiftSeconds = currentShiftSeconds;
            BenchSeconds = benchSeconds;
        }

        /// <summary>Accumulated ice time this match, in game seconds.</summary>
        public double TotalSeconds { get; }

        /// <summary>Shifts this match.</summary>
        public int Shifts { get; }

        /// <summary>The unit is on the ice now.</summary>
        public bool OnIce { get; }

        /// <summary>Game seconds of the current shift; 0 when on the bench.</summary>
        public double CurrentShiftSeconds { get; }

        /// <summary>Game seconds on the bench in this period; 0 when on the ice.</summary>
        public double BenchSeconds { get; }
    }
}
