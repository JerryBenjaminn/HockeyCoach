namespace HockeyCoach.Sim.Model
{
    /// <summary>A rink zone as an inclusive x range (rink.json <c>zones</c>).</summary>
    public sealed class ZoneRange
    {
        /// <summary>Creates a zone range.</summary>
        public ZoneRange(RinkZone zone, int xMin, int xMax)
        {
            Zone = zone;
            XMin = xMin;
            XMax = xMax;
        }

        /// <summary>The zone.</summary>
        public RinkZone Zone { get; }

        /// <summary>First x of the zone (inclusive).</summary>
        public int XMin { get; }

        /// <summary>Last x of the zone (inclusive).</summary>
        public int XMax { get; }
    }
}
