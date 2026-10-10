using HockeyCoach.Sim.Events;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// The match around a shift segment. The segment asks it at on-the-fly change opportunities (O-2) and applies the
    /// answer itself. Implemented by the match, which asks the team's coach.
    /// </summary>
    public interface IShiftHost
    {
        /// <summary>
        /// An on-the-fly change opportunity for <paramref name="team"/> (the team with the puck, at a safe moment, with at
        /// least one eligible unit). Returns the units to have on the ice; a unit that is not eligible must stay.
        /// </summary>
        /// <param name="team">The team that may change.</param>
        /// <param name="forwardsEligible">The trio has played at least <c>time.forwardShiftSeconds</c>.</param>
        /// <param name="defenceEligible">The pair has played at least <c>time.defenceShiftSeconds</c>.</param>
        /// <param name="time">Period time.</param>
        LineChoice OnTheFly(TeamSide team, bool forwardsEligible, bool defenceEligible, double time);
    }
}
