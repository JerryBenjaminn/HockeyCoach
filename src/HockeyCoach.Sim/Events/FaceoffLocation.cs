namespace HockeyCoach.Sim.Events
{
    /// <summary>
    /// Where a faceoff takes place (event schema: "oma alue, keskialue, vierasjoukkueen alue"). The ends are named by
    /// the team that defends them.
    /// </summary>
    public enum FaceoffLocation
    {
        /// <summary>The home team's defensive zone.</summary>
        HomeZone = 0,

        /// <summary>The neutral zone.</summary>
        NeutralZone = 1,

        /// <summary>The away team's defensive zone.</summary>
        AwayZone = 2,
    }
}
