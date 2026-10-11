using System;
using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.AI
{
    /// <summary>
    /// A coach made of a line policy and a tactics policy (data-schema.md AI-2). At a stoppage the order is: timeout
    /// (tactics), lines, plan (tactics), which is the R-T draw order. On the fly only the lines are asked.
    /// </summary>
    public class ComposedCoach : ICoach
    {
        private readonly ILinePolicy _lines;
        private readonly ITacticsPolicy _tactics;

        /// <summary>Creates the coach.</summary>
        public ComposedCoach(ILinePolicy lines, ITacticsPolicy tactics)
        {
            _lines = lines ?? throw new ArgumentNullException(nameof(lines));
            _tactics = tactics ?? throw new ArgumentNullException(nameof(tactics));
        }

        /// <inheritdoc />
        public CoachDecision AtStoppage(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            bool timeout = _tactics.Timeout(view);
            LineChoice units = _lines.AtStoppage(view);
            return new CoachDecision(units, _tactics.Plan(view), timeout);
        }

        /// <inheritdoc />
        public LineChoice OnTheFly(CoachView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            return _lines.OnTheFly(view);
        }
    }
}
