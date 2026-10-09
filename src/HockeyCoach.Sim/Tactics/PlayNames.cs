namespace HockeyCoach.Sim.Tactics
{
    /// <summary>Data names of <see cref="PlayType"/> and <see cref="PlayActionType"/> (docs/data-schema.md, Kuviot).</summary>
    public static class PlayNames
    {
        /// <summary>Parses a play type name (ordinal).</summary>
        public static bool TryParseType(string name, out PlayType type)
        {
            switch (name)
            {
                case "breakout": type = PlayType.Breakout; return true;
                case "zoneEntry": type = PlayType.ZoneEntry; return true;
                case "offensiveZone": type = PlayType.OffensiveZone; return true;
                case "faceoff": type = PlayType.Faceoff; return true;
                case "powerPlay": type = PlayType.PowerPlay; return true;
                default: type = default; return false;
            }
        }

        /// <summary>Parses an action type name (ordinal).</summary>
        public static bool TryParseAction(string name, out PlayActionType type)
        {
            switch (name)
            {
                case "skate": type = PlayActionType.Skate; return true;
                case "pass": type = PlayActionType.Pass; return true;
                case "shoot": type = PlayActionType.Shoot; return true;
                case "driveNet": type = PlayActionType.DriveNet; return true;
                case "dump": type = PlayActionType.Dump; return true;
                default: type = default; return false;
            }
        }

        /// <summary>The data name of an action type.</summary>
        public static string ToName(PlayActionType type)
        {
            switch (type)
            {
                case PlayActionType.Skate: return "skate";
                case PlayActionType.Pass: return "pass";
                case PlayActionType.Shoot: return "shoot";
                case PlayActionType.DriveNet: return "driveNet";
                default: return "dump";
            }
        }
    }
}
