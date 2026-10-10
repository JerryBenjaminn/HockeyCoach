using System;
using HockeyCoach.Sim.Random;

namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// Simulates one shift (tech-spec.md, Sim.Shift): faceoff, play mode beat by beat, shots, system mode and loose pucks,
    /// until a stoppage (D-043). Deterministic: the same setup and seed give the same event log.
    /// </summary>
    public static class ShiftSimulator
    {
        /// <summary>Runs the shift.</summary>
        /// <param name="setup">Rink, tuning, teams, opening faceoff spot and the anti-stall cap.</param>
        /// <param name="random">The single random generator of the match.</param>
        public static ShiftResult Run(ShiftSetup setup, IRandom random)
        {
            if (setup == null)
            {
                throw new ArgumentNullException(nameof(setup));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return new ShiftRun(setup, random).Run();
        }
    }
}
