namespace HockeyCoach.Sim.State
{
    /// <summary>Both teams' pressure state at a moment of a period (momentum chart).</summary>
    public sealed class PressureSample
    {
        /// <summary>Creates the sample.</summary>
        public PressureSample(int period, double time, double home, double away)
        {
            Period = period;
            Time = time;
            Home = home;
            Away = away;
        }

        /// <summary>Period number.</summary>
        public int Period { get; }

        /// <summary>Period time.</summary>
        public double Time { get; }

        /// <summary>Home pressure.</summary>
        public double Home { get; }

        /// <summary>Away pressure.</summary>
        public double Away { get; }
    }
}
