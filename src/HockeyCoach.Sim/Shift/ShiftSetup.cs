using System;
using HockeyCoach.Sim.Config;
using HockeyCoach.Sim.Model;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>Everything a shift needs besides the random generator.</summary>
    public sealed class ShiftSetup
    {
        /// <summary>Creates the setup.</summary>
        /// <param name="rink">The rink.</param>
        /// <param name="tuning">Balance values.</param>
        /// <param name="home">Home team.</param>
        /// <param name="away">Away team.</param>
        /// <param name="faceoffSpotId">Opening faceoff spot id in the home team's view (D-042: <c>center</c> first).</param>
        /// <param name="period">Period number, from 1.</param>
        /// <param name="startTime">Game seconds elapsed in the period when the shift starts.</param>
        /// <param name="maxSteps">
        /// Anti-stall cap (D-043, a caller parameter, not a balance value): the shift stops after this many events or
        /// simulation steps, whichever comes first.
        /// </param>
        public ShiftSetup(Rink rink, TuningConfig tuning, TeamShiftSetup home, TeamShiftSetup away, string faceoffSpotId, int period, double startTime, int maxSteps)
            : this(rink, tuning, home, away, faceoffSpotId, period, startTime, maxSteps, null)
        {
        }

        /// <summary>Creates the setup of a segment inside a match.</summary>
        /// <param name="rink">The rink.</param>
        /// <param name="tuning">Balance values.</param>
        /// <param name="home">Home team (its <see cref="TeamShiftSetup.Skaters"/> are the units after the context's start changes).</param>
        /// <param name="away">Away team.</param>
        /// <param name="faceoffSpotId">Faceoff spot id in the home team's view.</param>
        /// <param name="period">Period number, from 1.</param>
        /// <param name="startTime">Period time when the segment starts.</param>
        /// <param name="maxSteps">Anti-stall cap (D-043).</param>
        /// <param name="context">Match context; null runs a standalone segment with fresh state (the <c>shift</c> command).</param>
        public ShiftSetup(Rink rink, TuningConfig tuning, TeamShiftSetup home, TeamShiftSetup away, string faceoffSpotId, int period, double startTime, int maxSteps, ShiftContext context)
        {
            Context = context;
            Rink = rink ?? throw new ArgumentNullException(nameof(rink));
            Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            Home = home ?? throw new ArgumentNullException(nameof(home));
            Away = away ?? throw new ArgumentNullException(nameof(away));
            FaceoffSpotId = faceoffSpotId ?? throw new ArgumentNullException(nameof(faceoffSpotId));
            if (period < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(period), "Period starts at 1.");
            }

            if (maxSteps < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSteps), "The cap must be at least 1.");
            }

            Period = period;
            StartTime = startTime;
            MaxSteps = maxSteps;
        }

        /// <summary>Match context, or null for a standalone segment.</summary>
        public ShiftContext Context { get; }

        /// <summary>The rink.</summary>
        public Rink Rink { get; }

        /// <summary>Balance values.</summary>
        public TuningConfig Tuning { get; }

        /// <summary>Home team.</summary>
        public TeamShiftSetup Home { get; }

        /// <summary>Away team.</summary>
        public TeamShiftSetup Away { get; }

        /// <summary>Opening faceoff spot id (home team's view).</summary>
        public string FaceoffSpotId { get; }

        /// <summary>Period number.</summary>
        public int Period { get; }

        /// <summary>Game seconds elapsed in the period at the start.</summary>
        public double StartTime { get; }

        /// <summary>Anti-stall cap on events and simulation steps (D-043).</summary>
        public int MaxSteps { get; }
    }
}
