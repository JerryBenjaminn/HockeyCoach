using System;

namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Shot (laukaus): shooter, goalie or blocker, xG, chance class, chance type, under pressure (D-050), shot speed
    /// (null until milestone 3, D-052), outcome.
    /// </summary>
    public sealed class ShotEvent : SimEvent
    {
        /// <summary>Creates the event.</summary>
        /// <param name="context">Common event fields.</param>
        /// <param name="shooterId">Shooting player.</param>
        /// <param name="stoppedById">Goalie who saved or skater who blocked; null for a goal or a miss.</param>
        /// <param name="xg">Expected goals of the attempt, in [0, 1].</param>
        /// <param name="chanceClass">Chance class by xG, or null below the moderate threshold.</param>
        /// <param name="chanceType">How the chance was created.</param>
        /// <param name="underPressure">Whether the shooter was under pressure (M-7, D-050).</param>
        /// <param name="speed">Shot speed; null until milestone 3 (D-052).</param>
        /// <param name="outcome">Goal, saved, blocked or missed.</param>
        public ShotEvent(
            EventContext context,
            int shooterId,
            int? stoppedById,
            double xg,
            ChanceClass? chanceClass,
            ChanceType chanceType,
            bool underPressure,
            double? speed,
            ShotOutcome outcome)
            : base(context)
        {
            if (double.IsNaN(xg) || xg < 0.0 || xg > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(xg), "xG must be in [0, 1].");
            }

            bool stopped = outcome == ShotOutcome.Saved || outcome == ShotOutcome.Blocked;
            if (stopped != stoppedById.HasValue)
            {
                throw new ArgumentException(
                    stopped ? "A saved or blocked shot needs the goalie or blocker." : "Only a saved or blocked shot has a goalie or blocker.",
                    nameof(stoppedById));
            }

            ShooterId = shooterId;
            StoppedById = stoppedById;
            Xg = xg;
            ChanceClass = chanceClass;
            ChanceType = chanceType;
            UnderPressure = underPressure;
            Speed = speed;
            Outcome = outcome;
        }

        /// <summary>Shooting player.</summary>
        public int ShooterId { get; }

        /// <summary>Goalie who saved or skater who blocked; null for a goal or a miss.</summary>
        public int? StoppedById { get; }

        /// <summary>Expected goals of the attempt.</summary>
        public double Xg { get; }

        /// <summary>Chance class by xG, or null below the moderate threshold.</summary>
        public ChanceClass? ChanceClass { get; }

        /// <summary>How the chance was created.</summary>
        public ChanceType ChanceType { get; }

        /// <summary>Whether the shooter was under pressure (M-7, D-050). Recorded only; no effect on the shot in milestone 2.</summary>
        public bool UnderPressure { get; }

        /// <summary>Shot speed; null until milestone 3 (D-052).</summary>
        public double? Speed { get; }

        /// <summary>Goal, saved, blocked or missed.</summary>
        public ShotOutcome Outcome { get; }

        /// <inheritdoc />
        public override T Accept<T>(ISimEventVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
