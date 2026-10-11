using HockeyCoach.Sim.Events;

namespace HockeyCoach.Sim.Match
{
    /// <summary>Timeout information in a coach's view (AI-4, AI-5).</summary>
    public sealed class CoachTimeouts
    {
        /// <summary>Creates the information.</summary>
        /// <param name="ownLeft">Own timeouts still available this match.</param>
        /// <param name="opponentLeft">Opponent timeouts still available this match.</param>
        /// <param name="afterTimeoutRound">This is the round of decisions after a timeout at the same stoppage.</param>
        /// <param name="canCall">The coach may call a timeout in this decision.</param>
        public CoachTimeouts(int ownLeft, int opponentLeft, bool afterTimeoutRound, bool canCall)
        {
            OwnLeft = ownLeft;
            OpponentLeft = opponentLeft;
            AfterTimeoutRound = afterTimeoutRound;
            CanCall = canCall;
        }

        /// <summary>No timeouts and none callable (e.g. on the fly).</summary>
        public static CoachTimeouts None(int ownLeft, int opponentLeft)
        {
            return new CoachTimeouts(ownLeft, opponentLeft, false, false);
        }

        /// <summary>Own timeouts still available this match.</summary>
        public int OwnLeft { get; }

        /// <summary>Opponent timeouts still available this match.</summary>
        public int OpponentLeft { get; }

        /// <summary>This is the round of decisions after a timeout at the same stoppage (no new timeout allowed).</summary>
        public bool AfterTimeoutRound { get; }

        /// <summary>
        /// The coach may call a timeout now: one is available, this is a stoppage whose reason allows it
        /// (<see cref="ReasonAllows"/>) and not the round after a timeout.
        /// </summary>
        public bool CanCall { get; }

        /// <summary>
        /// Stoppage reasons at which a timeout may be called (AI-4): goal, goalie freeze, puck out of play, icing and offside.
        /// Not at a period start or a period end.
        /// </summary>
        public static bool ReasonAllows(StoppageReason reason)
        {
            switch (reason)
            {
                case StoppageReason.Goal:
                case StoppageReason.GoalieFreeze:
                case StoppageReason.OutOfPlay:
                case StoppageReason.Icing:
                case StoppageReason.Offside:
                    return true;
                default:
                    return false;
            }
        }
    }
}
