using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.AI
{
    /// <summary>How a coach picks its units (data-schema.md AI-2: rotation, R-L or S-L).</summary>
    public interface ILinePolicy
    {
        /// <summary>Units at a stoppage or period start; only eligible units may change.</summary>
        LineChoice AtStoppage(CoachView view);

        /// <summary>Units at an on-the-fly opportunity; only eligible units may change.</summary>
        LineChoice OnTheFly(CoachView view);
    }
}
