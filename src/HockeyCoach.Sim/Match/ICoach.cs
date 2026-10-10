using HockeyCoach.Sim.Shift;

namespace HockeyCoach.Sim.Match
{
    /// <summary>
    /// A coach (tech-spec.md, rajapinta valmentajalle; O-4). AI coaches and later the human player implement the same
    /// interface. The match calls it at every stoppage and period start, and at on-the-fly change opportunities.
    /// </summary>
    public interface ICoach
    {
        /// <summary>
        /// At a stoppage or the start of a period: the units for the faceoff (only eligible units may change, O-2) and the
        /// plan (plays in priority order, system, transition and system-mode instructions).
        /// </summary>
        CoachDecision AtStoppage(CoachView view);

        /// <summary>
        /// An on-the-fly change opportunity (O-2): the units to have on the ice. Returning the current indices keeps them.
        /// Only eligible units may change.
        /// </summary>
        LineChoice OnTheFly(CoachView view);
    }
}
