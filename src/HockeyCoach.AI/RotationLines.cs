using HockeyCoach.Sim.Match;
using HockeyCoach.Sim.Shift;

namespace HockeyCoach.AI
{
    /// <summary>The <see cref="RotationCoach"/> line rotation as a line policy (O-4), used by <c>randomTactics</c>.</summary>
    public sealed class RotationLines : ILinePolicy
    {
        /// <inheritdoc />
        public LineChoice AtStoppage(CoachView view)
        {
            return RotationCoach.Rotate(view);
        }

        /// <inheritdoc />
        public LineChoice OnTheFly(CoachView view)
        {
            return RotationCoach.Rotate(view);
        }
    }
}
