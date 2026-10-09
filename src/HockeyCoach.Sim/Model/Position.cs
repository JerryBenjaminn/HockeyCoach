namespace HockeyCoach.Sim.Model
{
    /// <summary>Skater position (D-024). Data names: <c>C</c>, <c>LW</c>, <c>RW</c>, <c>LD</c>, <c>RD</c>.</summary>
    public enum Position
    {
        /// <summary>Centre (<c>C</c>). Has no side.</summary>
        Center = 0,

        /// <summary>Left wing (<c>LW</c>).</summary>
        LeftWing = 1,

        /// <summary>Right wing (<c>RW</c>).</summary>
        RightWing = 2,

        /// <summary>Left defence (<c>LD</c>).</summary>
        LeftDefence = 3,

        /// <summary>Right defence (<c>RD</c>).</summary>
        RightDefence = 4,
    }
}
