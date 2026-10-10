namespace HockeyCoach.Sim.Shift
{
    /// <summary>
    /// How the <c>underPressure</c> field of one shot was decided (M-7 with the D-070 shot-off-a-pass exception).
    /// Diagnostics for tests only; it never affects the simulation.
    /// </summary>
    internal sealed class ShotPressureSample
    {
        internal ShotPressureSample(int shooterId, bool offPass, bool receiverFreeAtPass, bool? receiverFreeAtBeatStart, bool pressureAtShot, bool underPressure)
        {
            ShooterId = shooterId;
            OffPass = offPass;
            ReceiverFreeAtPass = receiverFreeAtPass;
            ReceiverFreeAtBeatStart = receiverFreeAtBeatStart;
            PressureAtShot = pressureAtShot;
            UnderPressure = underPressure;
        }

        /// <summary>The shooter.</summary>
        internal int ShooterId { get; }

        /// <summary>The shot is the next puck action after a completed pass to the shooter (D-070 condition 1).</summary>
        internal bool OffPass { get; }

        /// <summary>No opponent within <c>underPressureNodes</c> of the receiver when the pass was resolved (stored value).</summary>
        internal bool ReceiverFreeAtPass { get; }

        /// <summary>
        /// The same condition evaluated from snapshot S (start of the shooting beat or rush action, before any movement);
        /// null when the shot is not off a pass.
        /// </summary>
        internal bool? ReceiverFreeAtBeatStart { get; }

        /// <summary>The ordinary M-7 rule (presser, D-072) at the moment of the shot.</summary>
        internal bool PressureAtShot { get; }

        /// <summary>The value logged in the shot event.</summary>
        internal bool UnderPressure { get; }
    }

    /// <summary>Receives a <see cref="ShotPressureSample"/> for every shot (tests only).</summary>
    internal interface IShotPressureTrace
    {
        /// <summary>Called once per shot, before the shot event is logged.</summary>
        void OnShot(ShotPressureSample sample);
    }
}
