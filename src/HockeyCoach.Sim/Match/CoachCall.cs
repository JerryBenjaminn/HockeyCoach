namespace HockeyCoach.Sim.Match
{
    /// <summary>Why the coach is asked (AI-5).</summary>
    public enum CoachCall
    {
        /// <summary>The start of a period.</summary>
        PeriodStart = 0,

        /// <summary>A stoppage; <see cref="CoachView.StoppageReason"/> tells which.</summary>
        Stoppage = 1,

        /// <summary>An on-the-fly change opportunity (O-2).</summary>
        OnTheFly = 2,
    }
}
