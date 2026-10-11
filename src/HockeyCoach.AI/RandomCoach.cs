using System.Collections.Generic;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Random;
using HockeyCoach.Sim.Tactics;

namespace HockeyCoach.AI
{
    /// <summary>
    /// <c>random</c> (D-077 item 2, data-schema.md AI-2): random lines (R-L) and random tactics (R-T). At a stoppage the
    /// draw order is the timeout (only when one may be called), lines (trio, pair), then plays, system, transition and
    /// system-mode instructions. On the fly only the lines are drawn; the plan stays until the next stoppage. All draws
    /// come from the coach's own stream.
    /// </summary>
    public sealed class RandomCoach : ComposedCoach
    {
        /// <summary>Creates the coach.</summary>
        /// <param name="library">All plays in load order.</param>
        /// <param name="systems">All loaded systems.</param>
        /// <param name="config"><c>ai.random</c>.</param>
        /// <param name="random">The coach's own stream (<see cref="MatchStreams"/>).</param>
        public RandomCoach(IReadOnlyList<Play> library, IReadOnlyList<DefensiveSystem> systems, AiRandomConfig config, IRandom random)
            : base(new RandomLines(config, random), new RandomTactics(library, systems, config, random))
        {
        }
    }
}
