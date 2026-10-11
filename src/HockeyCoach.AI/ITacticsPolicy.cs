using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.AI
{
    /// <summary>How a coach picks its tactics at a stoppage (data-schema.md AI-2: R-T, S-T or A-T).</summary>
    public interface ITacticsPolicy
    {
        /// <summary>Whether to call a timeout now (AI-4); true only when <see cref="CoachTimeouts.CanCall"/>. Asked first.</summary>
        bool Timeout(CoachView view);

        /// <summary>The plan until the next stoppage. Asked after the lines.</summary>
        ShiftPlan Plan(CoachView view);
    }
}
