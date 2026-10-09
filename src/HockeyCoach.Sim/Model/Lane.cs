namespace HockeyCoach.Sim.Model
{
    /// <summary>Side of the rink across its width, seen toward the opponent goal (data-schema.md: Keskikaista).</summary>
    public enum Lane
    {
        /// <summary>Left side (y below the middle lane).</summary>
        Left = 0,

        /// <summary>The middle lane, y = (width - 1) / 2.</summary>
        Middle = 1,

        /// <summary>Right side (y above the middle lane).</summary>
        Right = 2,
    }
}
