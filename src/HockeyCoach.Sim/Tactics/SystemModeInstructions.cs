using System;

namespace HockeyCoach.Sim.Tactics
{
    /// <summary>System-mode instructions of a coach's decision (D-046, data-schema.md O-10). Defaults are the milestone 2 values.</summary>
    public sealed class SystemModeInstructions
    {
        /// <summary>netFrontAfterShot false, looseChasers 1, pinch false.</summary>
        public static readonly SystemModeInstructions Default = new SystemModeInstructions(false, 1, false);

        /// <summary>Creates the instructions.</summary>
        /// <param name="netFrontAfterShot">After a shot attempt the nearest other forward goes toward the net front.</param>
        /// <param name="looseChasers">1 or 2: with 2 the second-nearest skater moves toward every loose puck.</param>
        /// <param name="pinch">D1 takes the puck on the offensive-zone boards high when defending.</param>
        public SystemModeInstructions(bool netFrontAfterShot, int looseChasers, bool pinch)
        {
            if (looseChasers != 1 && looseChasers != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(looseChasers), "looseChasers must be 1 or 2 (O-10).");
            }

            NetFrontAfterShot = netFrontAfterShot;
            LooseChasers = looseChasers;
            Pinch = pinch;
        }

        /// <summary>After a shot attempt the nearest other forward goes toward the net front.</summary>
        public bool NetFrontAfterShot { get; }

        /// <summary>1 or 2 loose-puck chasers.</summary>
        public int LooseChasers { get; }

        /// <summary>D1 pinches on the offensive-zone boards high.</summary>
        public bool Pinch { get; }
    }
}
