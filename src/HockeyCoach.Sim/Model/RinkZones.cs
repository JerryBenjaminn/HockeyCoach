namespace HockeyCoach.Sim.Model
{
    /// <summary>Data names of <see cref="RinkZone"/>: <c>defensive</c>, <c>neutral</c>, <c>offensive</c>.</summary>
    public static class RinkZones
    {
        /// <summary>Parses a zone data name (ordinal).</summary>
        public static bool TryParse(string name, out RinkZone zone)
        {
            switch (name)
            {
                case "defensive": zone = RinkZone.Defensive; return true;
                case "neutral": zone = RinkZone.Neutral; return true;
                case "offensive": zone = RinkZone.Offensive; return true;
                default: zone = default; return false;
            }
        }

        /// <summary>The zone as seen by the other team (180° rotation swaps defensive and offensive).</summary>
        public static RinkZone Flip(RinkZone zone)
        {
            switch (zone)
            {
                case RinkZone.Defensive: return RinkZone.Offensive;
                case RinkZone.Offensive: return RinkZone.Defensive;
                default: return zone;
            }
        }
    }
}
