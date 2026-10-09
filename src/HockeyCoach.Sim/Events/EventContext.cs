using System;

namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Fields every event carries (docs/stats-and-checks.md, Tapahtumaskeema): time, period, strength, placements,
    /// and the play and defensive system ids for the tactics report (D-045).
    /// </summary>
    public sealed class EventContext
    {
        /// <summary>Creates the context.</summary>
        /// <param name="period">Period number, starting at 1.</param>
        /// <param name="time">Game seconds elapsed in the period, finite and not negative.</param>
        /// <param name="strength">Skaters on the ice per team.</param>
        /// <param name="placement">Player and puck positions.</param>
        /// <param name="playId">Id of the play being run by the team in possession, or null when not in a play.</param>
        /// <param name="systemId">Id of the defending team's system, or null when no system applies.</param>
        public EventContext(int period, double time, Strength strength, PlacementSnapshot placement, string playId, string systemId)
        {
            if (period < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(period), "Period starts at 1.");
            }

            if (double.IsNaN(time) || double.IsInfinity(time) || time < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(time), "Time must be a finite number of seconds >= 0.");
            }

            Period = period;
            Time = time;
            Strength = strength;
            Placement = placement ?? throw new ArgumentNullException(nameof(placement));
            PlayId = playId;
            SystemId = systemId;
        }

        /// <summary>Period number, starting at 1.</summary>
        public int Period { get; }

        /// <summary>Game seconds elapsed in the period.</summary>
        public double Time { get; }

        /// <summary>Skaters on the ice per team.</summary>
        public Strength Strength { get; }

        /// <summary>Player and puck positions.</summary>
        public PlacementSnapshot Placement { get; }

        /// <summary>Play id (D-045), or null when the team in possession is not running a play.</summary>
        public string PlayId { get; }

        /// <summary>Defending team's system id (D-045), or null when no system applies.</summary>
        public string SystemId { get; }

        /// <summary>Orders contexts by (period, time). Negative when this is earlier than <paramref name="other"/>.</summary>
        public int CompareTime(EventContext other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return Period != other.Period ? Period.CompareTo(other.Period) : Time.CompareTo(other.Time);
        }
    }
}
